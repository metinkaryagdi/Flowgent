"""AiSessions tablosundan eğitim datası çekici.

Kaynak: ai-db (PostgreSQL), Docker port 5439 (lokal), container adı ai-db.
Şema (ref: AiService.Domain.Entities):
  AiSessions(Id, ProjectId, UserId, OrganizationId, Type, Status, ...)
  AiPlanResults(Id, SessionId, Prompt, RawResponse, ParsedJson, WasApplied)

Seçim kriterleri (kaliteli örnek):
  - Status = 2 (Completed)
  - WasApplied = true  → kullanıcı sonucu onayladı, en yüksek kalite sinyali
  - ParsedJson IS NOT NULL
  - Type ∈ {0 PlanGeneration, 1 IssueEnrichment}

Çağrı:
  python -m tools.ai_data_collector.collect_sessions --limit 300

Çıktı: tools/ai_data_collector/output/sessions-<feature>.jsonl

**PII:** Her örnek `validation.pii.scrub` üzerinden geçer.
"""
from __future__ import annotations

import argparse
import json
import os
import sys
from pathlib import Path

from . import config
from .validation import scrub_json_values
from .validation.schema import SchemaError, validate

# Session Type enum → feature id
TYPE_TO_FEATURE = {
    0: "generate-plan",       # PlanGeneration (scaffold zamanı henüz üretimde yok)
    1: "enrich-issue",        # IssueEnrichment
    # Diğerleri training'e girmiyor (chat/retro/balance/risk farklı özellikler)
}


def _connect(dsn: str):
    try:
        import psycopg  # psycopg3
    except ImportError:
        print("ERROR: psycopg yok. `pip install psycopg[binary]` ile kur.", file=sys.stderr)
        sys.exit(3)
    return psycopg.connect(dsn)


def _default_dsn() -> str:
    # Docker lokal external: 5439 → internal 5432.
    pw = os.getenv("AI_DB_PASS", "ai_pass")
    host = os.getenv("AI_DB_HOST", "localhost")
    port = os.getenv("AI_DB_PORT", "5439")
    return f"postgresql://ai_user:{pw}@{host}:{port}/aidb"


def _extract_user_input(prompt: str, feature: str) -> str | None:
    """Handler prompt template'leri son satırlarda 'Project description: ...' /
    'Issue title: ...' kalıbıyla bitiyor. O son satırı yakala."""
    if not prompt:
        return None
    tail = prompt.strip().splitlines()[-1].strip()
    for marker in ("Project description:", "Issue title:"):
        if tail.startswith(marker):
            return tail[len(marker):].strip()
    # fallback: tüm prompt son 500 karakter
    return prompt.strip()[-500:]


def _build_example(row, feature: str) -> dict | None:
    session_id, project_id, org_id, parsed, prompt = row
    if not parsed:
        return None
    try:
        data = json.loads(parsed)
    except json.JSONDecodeError:
        return None
    try:
        validate(feature, data)
    except SchemaError:
        return None

    user_text = _extract_user_input(prompt or "", feature)
    if not user_text or len(user_text) < 10:
        return None

    if feature == "enrich-issue":
        input_obj = {"title": user_text[:120], "projectContext": ""}
    elif feature == "generate-plan":
        input_obj = {
            "projectId": "00000000-0000-0000-0000-000000000000",
            "projectName": "",
            "description": user_text[:1200],
        }
    else:
        return None

    out = {
        "feature": feature,
        "source": "sessions",
        "input": scrub_json_values(input_obj),
        "output": scrub_json_values(data),
    }
    return out


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--limit", type=int, default=300, help="her feature için max kayıt")
    ap.add_argument("--dsn", default=None, help="PostgreSQL DSN (opsiyonel; env AI_DB_PASS/HOST/PORT kullanır)")
    args = ap.parse_args()

    dsn = args.dsn or _default_dsn()
    print(f"[sessions] connecting -> {dsn.split('@')[1] if '@' in dsn else dsn}")

    try:
        conn = _connect(dsn)
    except Exception as e:
        print(f"ERROR: DB bağlantı başarısız ({e}). Docker ayakta mı? ai-db port 5439?", file=sys.stderr)
        return 2

    sql = """
        SELECT s."Id", s."ProjectId", s."OrganizationId", r."ParsedJson", r."Prompt"
        FROM "AiSessions" s
        JOIN "AiPlanResults" r ON r."SessionId" = s."Id"
        WHERE s."Status" = 2
          AND r."WasApplied" = true
          AND r."ParsedJson" IS NOT NULL
          AND s."Type" = %s
        ORDER BY s."CreatedAt" DESC
        LIMIT %s
    """

    total = 0
    with conn, conn.cursor() as cur:
        for type_val, feature in TYPE_TO_FEATURE.items():
            cur.execute(sql, (type_val, args.limit))
            rows = cur.fetchall()
            out_path = config.OUTPUT_DIR / f"sessions-{feature}.jsonl"
            n = 0
            with out_path.open("w", encoding="utf-8") as f:
                for row in rows:
                    ex = _build_example(row, feature)
                    if ex is None:
                        continue
                    f.write(json.dumps(ex, ensure_ascii=False) + "\n")
                    n += 1
            print(f"[{feature}] {n} örnek -> {out_path.name}")
            total += n

    print(f"[done] toplam {total} örnek")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
