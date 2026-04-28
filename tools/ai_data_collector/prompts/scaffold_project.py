"""Scaffold-project için 8 prompt template varyantı.

Strateji: her template 2 rolde yazı üretir:
1. INPUT prompt — kullanıcının yazacağı serbest metni model üretir (çeşit için).
2. OUTPUT prompt — o metinden ProjectDraft JSON'u model üretir.

Üretim iki adımda yapılır: önce (1), sonra (2). Böylece input-output çifti
fine-tune datasetine (prompt, completion) olarak girer.
"""
from __future__ import annotations

import random
from dataclasses import dataclass

from ..domains import DomainCtx, render


@dataclass
class Template:
    id: str
    input_prompt: str   # sistem LLM'ine "bana kullanıcı talebi gibi bir metin yaz" diyor
    output_prompt: str  # sonra aynı veya başka LLM'e "bu talepten ProjectDraft JSON üret" diyor


_OUTPUT_SPEC = """
Yalnızca aşağıdaki JSON şemasında çıktı üret. Markdown fence, açıklama, başlık ekleme.

{
  "project": {"name": "string, 3-60 karakter", "key": "string, 2-8 büyük harf", "description": "string, 20-500 karakter"},
  "sprints": [
    {
      "name": "string, 'Sprint N: ...' formatı",
      "goal": "string, tek cümle",
      "issues": [
        {"title": "string, fiille başlar", "description": "string, 20-300 karakter", "priority": "Low|Medium|High|Critical", "storyPoints": 1|2|3|5|8|13}
      ]
    }
  ]
}

Kısıtlar:
- 2-4 sprint, her sprintte 3-6 issue.
- Çıktı Türkçe. İngilizce sızıntısı yasak.
- Alanları uydurma: storyPoints için Fibonacci (1,2,3,5,8,13) dışı değer üretme.
"""


def templates() -> list[Template]:
    return [
        Template(
            id="sp-direct",
            input_prompt="Kendi işini kurmak isteyen biri gibi, yapılmasını istediği yazılım projesini 3-6 cümle Türkçe anlat. Teknik detay verme, ne istediğini söyle.",
            output_prompt=f"Aşağıdaki proje talebinden ProjectDraft JSON'u üret.{_OUTPUT_SPEC}",
        ),
        Template(
            id="sp-bullet",
            input_prompt="Bir proje fikri yaz: 3-6 maddelik ihtiyaç listesi şeklinde, Türkçe, tek paragraf değil madde madde.",
            output_prompt=f"Aşağıdaki madde listesi bir proje talebidir. ProjectDraft JSON üret.{_OUTPUT_SPEC}",
        ),
        Template(
            id="sp-stakeholder",
            input_prompt="Bir kurum yöneticisinin yazılım ekibine yazdığı resmi bir e-posta tonunda proje talebi yaz, 4-7 cümle Türkçe. İmza ekleme.",
            output_prompt=f"Aşağıdaki talep e-postasındaki projeyi ProjectDraft JSON'a dönüştür.{_OUTPUT_SPEC}",
        ),
        Template(
            id="sp-problem-first",
            input_prompt="Bir sorun tarifiyle başlayan proje fikri yaz. 'Şu an şöyle bir sıkıntı var, bunun için yazılım istiyorum' yapısında, 3-5 cümle.",
            output_prompt=f"Aşağıdaki sorun tanımından yola çıkıp ProjectDraft JSON üret. Çözüm proje olsun.{_OUTPUT_SPEC}",
        ),
        Template(
            id="sp-minimal",
            input_prompt="Çok kısa bir proje fikri: tek cümle başlık + 2 cümle açıklama. Toplam 3 cümleyi geçmesin. Türkçe.",
            output_prompt=f"Aşağıdaki kısa talepten ProjectDraft JSON üret. Eksikleri mantıklı varsayımlarla doldur.{_OUTPUT_SPEC}",
        ),
        Template(
            id="sp-comparative",
            input_prompt="'X gibi ama farkı Y' tarzında bir proje fikri yaz. Türkçe, 3-4 cümle. Gerçek şirket/marka ismi kullanma.",
            output_prompt=f"Aşağıdaki karşılaştırmalı talepten ProjectDraft JSON üret.{_OUTPUT_SPEC}",
        ),
        Template(
            id="sp-features",
            input_prompt="Bir proje fikrini 'şu özellikler olsun' formatında yaz. 4-7 özellik listele, Türkçe, her satırda bir özellik.",
            output_prompt=f"Aşağıdaki özellik listesini bir ProjectDraft'a dönüştür.{_OUTPUT_SPEC}",
        ),
        Template(
            id="sp-constrained",
            input_prompt="3 ay içinde bitmesi gereken bir MVP proje fikri yaz. Kısıtı belirt. Türkçe, 4-5 cümle.",
            output_prompt=f"Aşağıdaki süre-kısıtlı MVP talebinden ProjectDraft JSON üret; sprint sayısı MVP kapsamına uygun olsun.{_OUTPUT_SPEC}",
        ),
    ]


def build(template: Template, ctx: DomainCtx, rng: random.Random) -> tuple[str, str]:
    """Return (input_generation_prompt, output_generation_prompt)."""
    context_line = render(ctx)
    input_p = f"{template.input_prompt}\n\nBağlam: {context_line}"
    # output_prompt gerçek user metinini alır — runtime'da format edilir.
    return input_p, template.output_prompt
