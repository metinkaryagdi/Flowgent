"""Domain + bağlam varyasyonları — sentetik üretimde çeşitlilik kaynağı.

Her feature örneği rastgele bir (alan, alt_tür, ölçek, kullanıcı_tipi) tuple'ıyla
şartlanır. Bu yaklaşım aynı prompt template'in farklı çıktılar üretmesini sağlar.
"""
from __future__ import annotations

import random
from dataclasses import dataclass


@dataclass(frozen=True)
class DomainCtx:
    area: str          # e.g. "e-ticaret"
    sub: str           # e.g. "el yapımı takı"
    scale: str         # küçük | orta | kurumsal
    user: str          # nihai kullanıcı tipi


AREAS: list[tuple[str, list[str]]] = [
    ("e-ticaret", ["el yapımı takı", "ikinci el kitap", "butik giyim", "organik gıda", "hobi malzemeleri", "dijital ürün pazarı"]),
    ("eğitim", ["lise matematik", "YKS hazırlık", "yabancı dil", "çocuk kodlama", "kurumsal eğitim", "akademik makale yönetimi"]),
    ("fintech", ["küçük işletme fatura", "kişisel bütçe", "kripto portföy takibi", "kurumsal harcama onayı", "taksit hesaplayıcı"]),
    ("sağlık", ["fizyoterapi klinik", "diyetisyen takip", "veteriner", "psikolog online", "laboratuvar sonuç paylaşımı"]),
    ("oyun", ["kelime oyunu", "online bulmaca", "çok oyunculu strateji", "geliştirici portalı", "turnuva yönetimi"]),
    ("devops", ["dağıtım paneli", "log viewer", "feature flag", "incident tracker", "kapasite planlayıcı"]),
    ("mobil app", ["koşu takip", "meditasyon", "yemek tarifi", "not alma", "ikinci el eşya pazarı"]),
    ("kurumsal içi", ["izin yönetimi", "zimmet takibi", "toplantı odası rezervasyonu", "iç bildiri duyuruları", "anket"]),
    ("SaaS B2B", ["ajans proje takibi", "müşteri destek helpdesk", "abonelik yönetimi", "referans programı", "CRM hafif sürümü"]),
    ("IoT", ["sera sensörleri", "akıllı ev enerji", "flottilya araç takibi", "gıda soğuk zincir", "fabrika makine sağlığı"]),
    ("lojistik", ["kargo rotalama", "depo sayım", "kurye atama", "rota optimizasyonu", "teslimat kanıtı fotoğraf"]),
    ("sosyal", ["kulüp yönetimi", "etkinlik paylaşımı", "kitap kulübü", "dernek üye takibi", "gönüllü organizasyon"]),
]

SCALES = ["küçük (5-20 kullanıcı)", "orta (100-500 kullanıcı)", "kurumsal (binlerce kullanıcı)"]
USERS = ["son kullanıcı", "yönetici", "iç ekip", "dış müşteri", "operatör"]


def sample(rng: random.Random | None = None) -> DomainCtx:
    r = rng or random
    area, subs = r.choice(AREAS)
    return DomainCtx(area=area, sub=r.choice(subs), scale=r.choice(SCALES), user=r.choice(USERS))


def render(ctx: DomainCtx) -> str:
    """Prompt içine gömülecek kısa bağlam paragrafı."""
    return (
        f"Alan: {ctx.area} / {ctx.sub}. "
        f"Ölçek: {ctx.scale}. "
        f"Birincil kullanıcı: {ctx.user}."
    )
