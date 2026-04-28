"""Generate-plan için prompt template varyantları.

scaffold-project ile fark: mevcut projeye yeni özellik planı. Proje adı + yeni
özellik açıklaması input. Çıktıda sadece sprints[] — project alanı yok.
"""
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
Yalnızca aşağıdaki JSON şemasında çıktı üret. Markdown fence yasak.

{
  "sprints": [
    {
      "name": "string, 'Sprint N: ...' formatı",
      "goal": "string, tek cümle",
      "issues": [
        {"title": "string, fiille başlar", "description": "string", "priority": "Low|Medium|High|Critical", "storyPoints": 1|2|3|5|8|13}
      ]
    }
  ]
}

Kısıtlar:
- 1-3 sprint (yeni özellik kapsamına uygun). Sprint başına 3-6 issue.
- Çıktı Türkçe.
- storyPoints Fibonacci: 1,2,3,5,8,13.
"""


def templates() -> list[Template]:
    return [
        Template(
            id="gp-module",
            input_prompt="Mevcut bir projeye eklenecek yeni modül talebi yaz. Önce proje adı (1 satır), sonra 3-5 cümle yeni modülün ne yapacağını Türkçe anlat.",
            output_prompt=f"Aşağıdaki projede yapılacak yeni modül için sprint planı üret.{_OUTPUT_SPEC}",
        ),
        Template(
            id="gp-feature-list",
            input_prompt="Mevcut proje adı + eklenecek 3-6 özellik maddesi yaz. Format: ilk satır proje adı, sonrası '- ' başlı maddeler. Türkçe.",
            output_prompt=f"Aşağıdaki özellik listesi için sprint planı üret.{_OUTPUT_SPEC}",
        ),
        Template(
            id="gp-reporting",
            input_prompt="Raporlama/analytics odaklı bir talep yaz. 'Proje adı + hangi raporların ekleneceği' formatında, Türkçe 3-5 cümle.",
            output_prompt=f"Aşağıdaki raporlama talebi için sprint planı üret. Veri katmanı ve UI'yi farklı sprintlere dağıt.{_OUTPUT_SPEC}",
        ),
        Template(
            id="gp-mobile",
            input_prompt="Mevcut web projesine mobil uygulama veya mobil uyum ekleme talebi yaz. Proje adı + 3-5 cümle açıklama.",
            output_prompt=f"Aşağıdaki mobilleşme talebi için sprint planı üret.{_OUTPUT_SPEC}",
        ),
        Template(
            id="gp-integration",
            input_prompt="Mevcut projeye bir dış servis entegrasyonu ekleme talebi yaz. Proje adı + entegrasyon detayı, Türkçe 3-5 cümle.",
            output_prompt=f"Aşağıdaki entegrasyon talebi için sprint planı üret; auth, endpoint mapping, hata yönetimi ayrı issue'lar olsun.{_OUTPUT_SPEC}",
        ),
        Template(
            id="gp-performance",
            input_prompt="Performans/ölçek sorunu için iyileştirme talebi yaz. Proje adı + mevcut sıkıntı + hedef. Türkçe 3-5 cümle.",
            output_prompt=f"Aşağıdaki performans talebi için sprint planı üret; önce ölçüm/profilleme, sonra optimizasyon.{_OUTPUT_SPEC}",
        ),
        Template(
            id="gp-security",
            input_prompt="Güvenlik/uyumluluk odaklı iyileştirme talebi yaz. Proje adı + hangi güvenlik boşlukları/ihtiyaçlar. Türkçe 3-5 cümle.",
            output_prompt=f"Aşağıdaki güvenlik talebi için sprint planı üret.{_OUTPUT_SPEC}",
        ),
        Template(
            id="gp-admin",
            input_prompt="Mevcut projeye admin panel/yönetim ekranları ekleme talebi yaz. Proje adı + hangi yönetim işlevleri. Türkçe 3-5 cümle.",
            output_prompt=f"Aşağıdaki admin panel talebi için sprint planı üret. Yetki ve audit log ayrı issue.{_OUTPUT_SPEC}",
        ),
    ]


def build(template: Template, ctx: DomainCtx, rng: random.Random) -> tuple[str, str]:
    context_line = render(ctx)
    input_p = f"{template.input_prompt}\n\nBağlam: {context_line}"
    return input_p, template.output_prompt
