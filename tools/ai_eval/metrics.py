"""Evaluation metrikleri — her eval örneği için 0/1 puan üretir.

Plan: docs/ai-agent-fine-tuning-plan.md Faz 3.

Metrikler:
1. Format compliance — çıktı parse edilebilen JSON mu? (bool)
2. Schema compliance — tools/ai_data_collector/validation/schema.py geçer mi? (bool)
3. Field accuracy — must_contain hit oranı × must_not_contain miss oranı (float 0-1)
4. Latency — ms cinsinden (float, bilgi amaçlı)

Ortak istatistikler runner.py tarafından toplanır.
"""
from __future__ import annotations

import json
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPO_ROOT))

from tools.ai_data_collector.validation.schema import SchemaError, validate  # noqa: E402


@dataclass
class SampleResult:
    """Tek bir eval örneğinin tüm metrikleri."""
    id: str
    feature: str
    model: str
    latency_ms: float
    format_ok: bool = False
    schema_ok: bool = False
    field_accuracy: float = 0.0
    must_contain_hit: int = 0
    must_contain_total: int = 0
    must_not_contain_clean: int = 0
    must_not_contain_total: int = 0
    raw_output: str = ""
    parsed: dict | None = None
    error: str = ""


def _extract_json(raw: str) -> str | None:
    """Model çıktısından JSON bloğunu çıkart. Markdown fence / açıklama metnini tolere et."""
    s = raw.strip()
    if s.startswith("```"):
        s = s.strip("`")
        if s.lower().startswith("json"):
            s = s[4:]
    start = s.find("{")
    end = s.rfind("}")
    if start == -1 or end == -1 or end < start:
        return None
    return s[start:end + 1]


def evaluate_format(raw: str) -> tuple[bool, dict | None, str]:
    """Format compliance: çıktı parse edilebiliyor mu?"""
    snippet = _extract_json(raw)
    if snippet is None:
        return False, None, "JSON sınırı bulunamadı"
    try:
        return True, json.loads(snippet), ""
    except json.JSONDecodeError as e:
        return False, None, f"json parse: {e}"


def evaluate_schema(feature: str, data: dict) -> tuple[bool, str]:
    """Schema compliance: data_collector şema doğrulayıcısı geçer mi?"""
    try:
        validate(feature, data)
        return True, ""
    except SchemaError as e:
        return False, str(e)


def evaluate_fields(data: dict, must_contain: list[str], must_not_contain: list[str]) -> tuple[float, int, int, int, int]:
    """Field accuracy:
    - must_contain: içerik case-insensitive olarak bu string'leri içermeli
    - must_not_contain: içerik case-insensitive olarak bu string'leri İÇERMEMELİ

    Return: (combined_score, mc_hit, mc_total, mnc_clean, mnc_total)
    combined_score = avg(mc_hit/mc_total, mnc_clean/mnc_total); 0 kategorisi 1.0 sayılır.
    """
    haystack = json.dumps(data, ensure_ascii=False).lower()

    mc_total = len(must_contain)
    mc_hit = sum(1 for s in must_contain if s.lower() in haystack)

    mnc_total = len(must_not_contain)
    mnc_clean = sum(1 for s in must_not_contain if s.lower() not in haystack)

    mc_ratio = (mc_hit / mc_total) if mc_total else 1.0
    mnc_ratio = (mnc_clean / mnc_total) if mnc_total else 1.0
    score = (mc_ratio + mnc_ratio) / 2

    return score, mc_hit, mc_total, mnc_clean, mnc_total


@dataclass
class AggregateStats:
    """Model × feature grubu için toplu istatistik."""
    model: str
    feature: str = "all"
    n: int = 0
    format_ok: int = 0
    schema_ok: int = 0
    field_accuracy_sum: float = 0.0
    latency_ms: list[float] = field(default_factory=list)

    def add(self, r: SampleResult) -> None:
        self.n += 1
        self.format_ok += int(r.format_ok)
        self.schema_ok += int(r.schema_ok)
        self.field_accuracy_sum += r.field_accuracy
        self.latency_ms.append(r.latency_ms)

    def as_dict(self) -> dict:
        lat = sorted(self.latency_ms) if self.latency_ms else [0]
        p50 = lat[len(lat) // 2]
        p95 = lat[int(len(lat) * 0.95)] if len(lat) > 1 else lat[0]
        return {
            "model": self.model,
            "feature": self.feature,
            "n": self.n,
            "format_compliance_pct": round(100 * self.format_ok / self.n, 1) if self.n else 0,
            "schema_compliance_pct": round(100 * self.schema_ok / self.n, 1) if self.n else 0,
            "field_accuracy_pct": round(100 * self.field_accuracy_sum / self.n, 1) if self.n else 0,
            "latency_p50_ms": round(p50, 0),
            "latency_p95_ms": round(p95, 0),
        }
