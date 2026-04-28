"""Enrich-issue için prompt template varyantları."""
from __future__ import annotations

import random
from dataclasses import dataclass

from ..domains import DomainCtx, render


@dataclass
class Template:
    id: str
    input_prompt: str
    output_prompt: str


_OUTPUT_SPEC = """
Yalnızca aşağıdaki JSON şemasında çıktı üret. Markdown fence, açıklama, başlık yasak.

{
  "description": "string, 50-400 karakter",
  "acceptanceCriteria": "string, '- ' ile başlayan 3-6 satır, her satır sonunda \\n",
  "edgeCases": "string, '- ' ile başlayan 2-5 satır, her satır sonunda \\n",
  "storyPoints": 1|2|3|5|8|13
}

Kısıtlar:
- Türkçe çıktı. İngilizce sızıntısı yasak.
- Kabul kriterleri test edilebilir olmalı (ölçülebilir eylem), 'güzel/kolay' gibi subjektif ifade yasak.
- Edge case'ler generic değil, somut teknik senaryo.
"""


def templates() -> list[Template]:
    return [
        Template(
            id="ei-short",
            input_prompt="Bir yazılım projesinde issue başlığı yaz — 3-8 kelime Türkçe, fiille başlasın, içerik açık olmasın detaylı tarif gerektirsin. Tek satır.",
            output_prompt=f"Aşağıdaki issue başlığını zenginleştir.{_OUTPUT_SPEC}",
        ),
        Template(
            id="ei-vague",
            input_prompt="Çok muğlak bir issue başlığı yaz ('giriş yap', 'rapor ekle' gibi). Türkçe, tek satır.",
            output_prompt=f"Aşağıdaki muğlak başlıktan detaylı issue spesifikasyonu çıkar.{_OUTPUT_SPEC}",
        ),
        Template(
            id="ei-technical",
            input_prompt="Teknik terimler içeren issue başlığı yaz (API, webhook, cache, job gibi). Türkçe, 3-10 kelime.",
            output_prompt=f"Aşağıdaki teknik issue başlığını zenginleştir.{_OUTPUT_SPEC}",
        ),
        Template(
            id="ei-ui",
            input_prompt="UI odaklı issue başlığı yaz (form, ekran, buton, modal, liste gibi). Türkçe, 4-9 kelime.",
            output_prompt=f"Aşağıdaki UI issue başlığını zenginleştir. Kabul kriterlerinde responsive ve erişilebilirlik değin.{_OUTPUT_SPEC}",
        ),
        Template(
            id="ei-integration",
            input_prompt="Bir dış servisle entegrasyon issue başlığı yaz (ödeme, email, SMS, harita, storage). Türkçe, 5-10 kelime.",
            output_prompt=f"Aşağıdaki entegrasyon issue başlığını zenginleştir. Hata/timeout/retry senaryolarını edge case'e koy.{_OUTPUT_SPEC}",
        ),
        Template(
            id="ei-permission",
            input_prompt="Yetkilendirme odaklı issue başlığı yaz (rol, erişim, 403, yetki). Türkçe, 4-8 kelime.",
            output_prompt=f"Aşağıdaki yetki issue başlığını zenginleştir. Kabul kriterlerinde rol matrisini belirt.{_OUTPUT_SPEC}",
        ),
        Template(
            id="ei-migration",
            input_prompt="Migrasyon/veritabanı değişikliği odaklı issue başlığı yaz. Türkçe, 5-10 kelime.",
            output_prompt=f"Aşağıdaki migrasyon issue başlığını zenginleştir. Rollback ve downtime edge case'e girsin.{_OUTPUT_SPEC}",
        ),
        Template(
            id="ei-refactor",
            input_prompt="Refactor/temizlik odaklı issue başlığı yaz. Türkçe, 4-9 kelime.",
            output_prompt=f"Aşağıdaki refactor issue başlığını zenginleştir. Regression riskini edge case'e al.{_OUTPUT_SPEC}",
        ),
    ]


def build(template: Template, ctx: DomainCtx, rng: random.Random) -> tuple[str, str]:
    context_line = render(ctx)
    input_p = f"{template.input_prompt}\n\nBağlam: {context_line}"
    return input_p, template.output_prompt
