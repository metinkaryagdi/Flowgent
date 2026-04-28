# -*- coding: utf-8 -*-
"""Deterministic mock-data generator for the three fine-tune features.

Produces synthetic records that match the JSONL schema of synthetic_gen.py
without calling any external LLM provider. Used to top up the dataset when
provider quotas are exhausted.

Usage:
    python -m tools.ai_data_collector.mock_gen --feature generate-plan --count 200
    python -m tools.ai_data_collector.mock_gen --all --target 350
"""
from __future__ import annotations

import argparse
import json
import os
import random
import string
from pathlib import Path
from typing import Any

OUT_DIR = Path("tools/ai_data_collector/output")
OUT_DIR.mkdir(parents=True, exist_ok=True)

FILES = {
    "scaffold-project": OUT_DIR / "synthetic-scaffold-project.jsonl",
    "enrich-issue": OUT_DIR / "synthetic-enrich-issue.jsonl",
    "generate-plan": OUT_DIR / "synthetic-generate-plan.jsonl",
}

# ---------------------------------------------------------------- Pools ---

DOMAINS: list[dict[str, Any]] = [
    {"area": "e-ticaret", "subs": ["dijital ürün pazarı", "moda satış platformu",
                                    "kitap satış sitesi", "elektronik mağaza",
                                    "ikinci el pazaryeri"]},
    {"area": "sağlık", "subs": ["randevu yönetim sistemi", "telesağlık platformu",
                                 "hasta takip uygulaması", "diyetisyen takip uygulaması",
                                 "ilaç hatırlatma sistemi"]},
    {"area": "eğitim", "subs": ["uzaktan eğitim platformu", "kurs satış pazaryeri",
                                 "öğrenci takip sistemi", "online sınav sistemi",
                                 "dil öğrenme uygulaması"]},
    {"area": "finans", "subs": ["bütçe takip uygulaması", "kripto borsası paneli",
                                 "fatura yönetim sistemi", "kredi başvuru portalı",
                                 "yatırım danışmanlık platformu"]},
    {"area": "lojistik", "subs": ["kargo takip sistemi", "depo yönetim sistemi",
                                   "filo yönetim platformu", "kurye atama sistemi",
                                   "stok takip uygulaması"]},
    {"area": "sosyal medya", "subs": ["mikroblog platformu", "fotoğraf paylaşım uygulaması",
                                       "video paylaşım sitesi", "topluluk forumu",
                                       "etkinlik paylaşım uygulaması"]},
    {"area": "üretim", "subs": ["üretim hattı izleme", "kalite kontrol sistemi",
                                 "bakım yönetim sistemi", "iş emri takip platformu",
                                 "tedarikçi yönetim portalı"]},
    {"area": "gayrimenkul", "subs": ["emlak ilan platformu", "kiralama yönetim sistemi",
                                      "site yönetim uygulaması", "ofis kiralama platformu",
                                      "tatil evi rezervasyon sistemi"]},
    {"area": "turizm", "subs": ["otel rezervasyon platformu", "tur planlama uygulaması",
                                 "uçuş arama motoru", "araç kiralama sistemi",
                                 "rehber atama uygulaması"]},
    {"area": "spor", "subs": ["antrenman takip uygulaması", "fitness rezervasyon sistemi",
                               "lig sonuç takip platformu", "spor okulu yönetimi",
                               "saha kiralama uygulaması"]},
    {"area": "tarım", "subs": ["tarla takip platformu", "sera otomasyon sistemi",
                                "hayvan takip uygulaması", "hasat planlama platformu",
                                "tarım kooperatifi yönetimi"]},
    {"area": "IoT", "subs": ["akıllı ev paneli", "endüstriyel sensör izleme",
                              "akıllı tarım platformu", "araç telemetri sistemi",
                              "akıllı şehir konsolu"]},
    {"area": "yapay zeka", "subs": ["müşteri hizmetleri chatbotu",
                                     "doküman özetleme sistemi",
                                     "görüntü etiketleme platformu",
                                     "sesli asistan altyapısı",
                                     "öneri motoru paneli"]},
    {"area": "kamu", "subs": ["belediye e-randevu sistemi", "vatandaş bildirim platformu",
                               "evrak takip uygulaması", "trafik ihbar sistemi",
                               "su arıza takip platformu"]},
    {"area": "eğlence", "subs": ["bilet satış platformu", "etkinlik takvim sistemi",
                                  "müzik paylaşım uygulaması", "podcast yayın platformu",
                                  "oyun topluluğu portalı"]},
    {"area": "insan kaynakları", "subs": ["başvuru takip sistemi", "izin yönetim portalı",
                                            "performans değerlendirme platformu",
                                            "bordro yönetim uygulaması",
                                            "iç eğitim takip sistemi"]},
]

SCALES = [
    "küçük (10-50 kullanıcı)",
    "orta (100-500 kullanıcı)",
    "büyük (1000-5000 kullanıcı)",
    "kurumsal (10.000+ kullanıcı)",
]

USERS = ["yönetici", "kullanıcı", "müşteri", "çalışan", "operatör",
         "danışman", "sağlayıcı", "tedarikçi", "öğretmen", "öğrenci",
         "doktor", "hasta", "kurye", "satıcı", "alıcı"]

# Each entry: ("Modül adı", "kısa açıklama", [issue başlık şablonları])
FEATURE_BANK: list[tuple[str, str, list[str]]] = [
    ("Kullanıcı Yönetimi",
     "Kullanıcı kayıt, giriş, profil ve rol işlemlerinin yönetilmesi",
     ["Kayıt formu", "Giriş ekranı", "Şifre sıfırlama akışı",
      "Profil düzenleme sayfası", "Rol bazlı yetki kontrolü"]),
    ("Bildirim Sistemi",
     "E-posta, push ve uygulama içi bildirim kanallarının kurulması",
     ["Bildirim tercih ekranı", "E-posta şablon yönetimi",
      "Push bildirim entegrasyonu", "Uygulama içi bildirim merkezi",
      "Bildirim okundu/okunmadı işaretleme"]),
    ("Dashboard ve Raporlama",
     "Ana sayfa istatistikleri ve dışa aktarılabilir raporlar",
     ["Ana sayfa istatistik kartları", "Tarih aralığı filtresi",
      "Grafik bileşeni entegrasyonu", "PDF dışa aktarım",
      "CSV dışa aktarım"]),
    ("Arama ve Filtreleme",
     "Liste sayfalarında gelişmiş arama ve filtreleme",
     ["Genel arama kutusu", "Çoklu filtre paneli", "Sıralama seçenekleri",
      "Kayıtlı filtre yönetimi", "Sayfalama altyapısı"]),
    ("Ödeme Entegrasyonu",
     "Kart ve sanal cüzdan tabanlı ödeme akışı",
     ["Ödeme sağlayıcı entegrasyonu", "3D Secure akışı",
      "Fatura PDF üretimi", "İade ve iptal işlemi",
      "Ödeme geçmişi sayfası"]),
    ("Sipariş Yönetimi",
     "Sipariş oluşturma, takip ve durum yönetimi",
     ["Sipariş oluşturma formu", "Sipariş listeleme ekranı",
      "Sipariş detay sayfası", "Kargo entegrasyonu",
      "Sipariş iptal akışı"]),
    ("Stok Yönetimi",
     "Ürün stok seviyesi ve hareket takibi",
     ["Stok giriş ekranı", "Stok çıkış ekranı",
      "Düşük stok uyarısı", "Stok sayım modülü",
      "Stok hareket raporu"]),
    ("Doküman Yönetimi",
     "Dosya yükleme, sürüm ve paylaşım işlemleri",
     ["Dosya yükleme bileşeni", "Sürüm geçmişi",
      "Doküman paylaşım linki", "İzin tabanlı görüntüleme",
      "Toplu indirme"]),
    ("Mesajlaşma",
     "Kullanıcılar arası anlık mesajlaşma altyapısı",
     ["Birebir mesajlaşma ekranı", "Grup sohbeti", "Yazıyor göstergesi",
      "Okundu bilgisi", "Mesaj arşivleme"]),
    ("Yorum ve Değerlendirme",
     "Ürün/içerik yorum ve puanlama sistemi",
     ["Yorum yazma formu", "Yıldız bazlı puanlama",
      "Yorum onay akışı", "Yorum bildirim entegrasyonu",
      "Yorum spam filtresi"]),
    ("Kategori Yönetimi",
     "Hiyerarşik kategori ve etiket yapısı",
     ["Kategori CRUD ekranı", "Alt kategori desteği",
      "Etiket yönetimi", "Kategori bazlı filtreleme",
      "Kategori sıralama"]),
    ("Randevu/Rezervasyon",
     "Takvim üzerinden randevu/rezervasyon yönetimi",
     ["Takvim görünümü", "Müsaitlik tanımlama",
      "Randevu oluşturma akışı", "Hatırlatma e-postası",
      "Randevu iptal/erteleme"]),
    ("Mobil Uyumlu Arayüz",
     "Tüm ekranların mobil cihazlarda kullanılabilir hale getirilmesi",
     ["Responsive layout düzeni", "Mobil menü davranışı",
      "Dokunmatik etkileşim iyileştirmeleri",
      "Tablet kırılımı testleri", "Mobil performans optimizasyonu"]),
    ("Çok Dillilik",
     "Türkçe ve İngilizce dil desteğinin sağlanması",
     ["i18n altyapısı kurulumu", "Çeviri kataloğu yönetimi",
      "Dil seçici bileşeni", "Tarih ve sayı format yerelleştirmesi",
      "Sağdan sola yazım denemesi"]),
    ("Erişilebilirlik (A11y)",
     "WCAG kriterlerine uygun erişilebilir arayüz",
     ["Klavye gezinme desteği", "Ekran okuyucu etiketleri",
      "Renk kontrast kontrolü", "Form hata mesajları",
      "Erişilebilirlik denetim raporu"]),
    ("Güvenlik ve Yetkilendirme",
     "Kimlik doğrulama, yetki ve güvenlik politikalarının uygulanması",
     ["JWT tabanlı kimlik doğrulama", "Rol/izin matrisi",
      "Şifre politikası kontrolü", "Oturum yönetim",
      "İki faktörlü doğrulama"]),
    ("Audit ve Loglama",
     "Kritik işlemlerin denetim kaydı ve log akışı",
     ["Denetim kaydı tablosu", "Kullanıcı eylem geçmişi",
      "Yapılandırılmış log altyapısı", "Hata izleme entegrasyonu",
      "Log arama ekranı"]),
    ("API ve Entegrasyon",
     "Üçüncü parti servislerle entegrasyon ve dış API",
     ["REST API dokümantasyonu", "API anahtar yönetimi",
      "Webhook altyapısı", "Üçüncü parti SSO",
      "Rate limiting"]),
    ("Performans ve Önbellek",
     "Sık okunan verilerin önbelleklenmesi ve performans iyileştirmesi",
     ["Redis önbellek entegrasyonu", "Sayfalama optimizasyonu",
      "Sorgu indeksleme", "CDN üzerinden statik içerik",
      "Yük testi"]),
    ("Onboarding ve Yardım",
     "Yeni kullanıcı yönlendirme ve uygulama içi yardım",
     ["Karşılama akışı", "Etkileşimli tur",
      "Yardım merkezi", "Sıkça sorulan sorular",
      "Geri bildirim formu"]),
    ("Abonelik ve Faturalandırma",
     "Abonelik planları ve fatura üretimi",
     ["Plan seçim ekranı", "Otomatik yenileme",
      "Plan yükseltme/düşürme", "Fatura geçmişi",
      "Vergi hesaplama"]),
    ("Geri Bildirim",
     "Kullanıcı geri bildirimlerinin toplanması ve yönetilmesi",
     ["Geri bildirim formu", "NPS anketi",
      "Geri bildirim panosu", "Etiket bazlı sınıflandırma",
      "Yanıtlama akışı"]),
]

PRIORITIES = ["Critical", "High", "Medium", "Low"]
PRIORITY_WEIGHTS = [0.10, 0.40, 0.35, 0.15]
STORY_POINTS = [1, 2, 3, 5, 8, 13]


# ---------------------------------------------------------- Helpers ------

def rand_key(rng: random.Random, length: int = 4) -> str:
    return "".join(rng.choice(string.ascii_uppercase) for _ in range(length))


def project_id(rng: random.Random) -> str:
    a = rand_key(rng, 4)
    b = rand_key(rng, 4)
    return f"{a}-{b}"


def project_acronym(name: str) -> str:
    parts = [p for p in name.replace("/", " ").split() if p]
    if not parts:
        return "PRJ"
    if len(parts) == 1:
        return parts[0][:3].upper()
    return ("".join(p[0] for p in parts[:4])).upper()


def pick_priority(rng: random.Random) -> str:
    return rng.choices(PRIORITIES, PRIORITY_WEIGHTS, k=1)[0]


def pick_story_points(rng: random.Random, priority: str) -> int:
    if priority == "Critical":
        return rng.choice([8, 13])
    if priority == "High":
        return rng.choice([5, 8, 13])
    if priority == "Medium":
        return rng.choice([3, 5, 8])
    return rng.choice([1, 2, 3])


def pick_domain(rng: random.Random) -> dict[str, str]:
    d = rng.choice(DOMAINS)
    return {
        "area": d["area"],
        "sub": rng.choice(d["subs"]),
        "scale": rng.choice(SCALES),
        "user": rng.choice(USERS),
    }


def pick_features(rng: random.Random, n: int) -> list[tuple[str, str, list[str]]]:
    return rng.sample(FEATURE_BANK, k=min(n, len(FEATURE_BANK)))


# ---------------------------------------------------- Feature: scaffold ---

PROJECT_NAME_PATTERNS = [
    "{Sub} Yönetim Platformu",
    "{Sub} Otomasyonu",
    "{Sub} Portalı",
    "{Sub} Konsolu",
    "Akıllı {Sub}",
    "{Sub} Asistanı",
]


def make_project_name(rng: random.Random, sub: str) -> str:
    sub_cap = " ".join(w.capitalize() for w in sub.split())
    return rng.choice(PROJECT_NAME_PATTERNS).format(Sub=sub_cap)


def build_scaffold_record(rng: random.Random) -> dict[str, Any]:
    domain = pick_domain(rng)
    feats = pick_features(rng, rng.randint(4, 6))

    bullets = []
    for i, (name, desc, _) in enumerate(feats, start=1):
        bullets.append(f"{i}. **{name}**: {desc}.")
    description = "\n\n".join(bullets)

    project_name = make_project_name(rng, domain["sub"])
    project_key = project_acronym(project_name)

    sprints = []
    for s_idx, (name, desc, issues) in enumerate(feats[: rng.randint(2, 4)],
                                                  start=1):
        sample_issues = rng.sample(issues, k=min(rng.randint(3, 4), len(issues)))
        sprint_issues = []
        for it in sample_issues:
            pr = pick_priority(rng)
            sp = pick_story_points(rng, pr)
            sprint_issues.append({
                "title": it,
                "description": f"{it} adımının {domain['user']} için "
                               f"{domain['area']} ortamında gerçekleştirilmesi.",
                "priority": pr,
                "storyPoints": sp,
            })
        sprints.append({
            "name": f"Sprint {s_idx}: {name}",
            "goal": f"{name} modülünü çalışır hale getirmek.",
            "issues": sprint_issues,
        })

    return {
        "feature": "scaffold-project",
        "template_id": rng.choice(["mock-sp-bullet", "mock-sp-numeric",
                                    "mock-sp-list"]),
        "domain": domain,
        "temperature": round(rng.uniform(0.5, 0.9), 2),
        "input": {
            "description": description,
            "context": {
                "area": domain["area"],
                "sub": domain["sub"],
                "scale": domain["scale"],
            },
        },
        "output": {
            "project": {
                "name": project_name,
                "key": project_key,
                "description": (
                    f"{domain['scale'].split()[0].capitalize()} ölçekli bir "
                    f"{domain['sub']} için temel modüllerin geliştirilmesi."
                ),
            },
            "sprints": sprints,
        },
    }


# ---------------------------------------------------- Feature: enrich ---

VAGUE_TITLES = [
    "Yeni özellik geliştir", "Sayfa düzenle", "Hata düzelt", "Performans iyileştir",
    "Modülü güncelle", "Ekrana filtre ekle", "Liste sayfasını yenile",
    "Form alanlarını gözden geçir", "Detay sayfasını zenginleştir",
    "API ucu hazırla", "Loglama ekle", "Bildirim entegrasyonu",
]


def build_enrich_record(rng: random.Random) -> dict[str, Any]:
    domain = pick_domain(rng)
    feat_name, feat_desc, sub_issues = rng.choice(FEATURE_BANK)
    base_title = rng.choice(sub_issues + VAGUE_TITLES)
    title = f"\"{feat_name} - {base_title}\""

    # Acceptance criteria — 4-6 items
    ac_items = [
        f"{base_title} için {domain['user']} ekranında ilgili form alanları doğrulanır.",
        f"İşlem başarılı olduğunda kullanıcıya bilgi mesajı gösterilir.",
        f"Kayıt başarısız olduğunda hata mesajı net biçimde sunulur.",
        f"Eylem audit (denetim) kaydına eklenir.",
        f"Mobil ve masaüstü görünümde aynı davranış sergilenir.",
        f"İlgili API ucu yetki kontrolünden geçer.",
    ]
    ec_items = [
        f"Veri yoksa boş durum (empty state) ekranı gösterilir.",
        f"Aynı anda iki kullanıcı düzenleme yaparsa son kayıt geçerli olur.",
        f"Geçersiz girdilerde işlem reddedilir ve kullanıcı yönlendirilir.",
        f"Bağlantı kesilirse işlem yeniden denenebilir.",
    ]
    rng.shuffle(ac_items)
    rng.shuffle(ec_items)
    ac_items = ac_items[: rng.randint(4, 6)]
    ec_items = ec_items[: rng.randint(2, 3)]

    pr = pick_priority(rng)
    sp = pick_story_points(rng, pr)

    return {
        "feature": "enrich-issue",
        "template_id": rng.choice(["mock-ei-vague", "mock-ei-feature",
                                    "mock-ei-list"]),
        "domain": domain,
        "temperature": round(rng.uniform(0.5, 0.9), 2),
        "input": {
            "title": title,
            "projectContext": f"{domain['area']} / {domain['sub']}",
        },
        "output": {
            "description": (
                f"{feat_name} modülü kapsamında {base_title.lower()} "
                f"eyleminin {domain['user']} için "
                f"{domain['scale'].split()[0]} ölçekli {domain['sub']} "
                f"projesinde gerçekleştirilmesi. {feat_desc}."
            ),
            "acceptanceCriteria": "\n".join(f"- {x}" for x in ac_items),
            "edgeCases": "\n".join(f"- {x}" for x in ec_items),
            "storyPoints": sp,
        },
    }


# ---------------------------------------------------- Feature: plan ---

def build_plan_record(rng: random.Random) -> dict[str, Any]:
    domain = pick_domain(rng)
    feats = pick_features(rng, rng.randint(5, 7))

    description = "\n".join(
        f"- Özellik {i}: {name} - {desc}."
        for i, (name, desc, _) in enumerate(feats, start=1)
    )

    project_name = make_project_name(rng, domain["sub"])

    n_sprints = rng.randint(2, 4)
    sprint_groups = [feats[i::n_sprints] for i in range(n_sprints)]

    sprints = []
    for s_idx, group in enumerate(sprint_groups, start=1):
        if not group:
            continue
        primary = group[0]
        sprint_issues = []
        for (name, desc, sub_issues) in group:
            chosen = rng.sample(sub_issues, k=min(2, len(sub_issues)))
            for it in chosen:
                pr = pick_priority(rng)
                sp = pick_story_points(rng, pr)
                sprint_issues.append({
                    "title": f"{name}: {it}",
                    "description": f"{it} adımı, {desc.lower()}.",
                    "priority": pr,
                    "storyPoints": sp,
                })
        sprints.append({
            "name": f"Sprint {s_idx}: {primary[0]}",
            "goal": f"{primary[0]} modülünün temel akışını çalışır hale getirmek.",
            "issues": sprint_issues,
        })

    return {
        "feature": "generate-plan",
        "template_id": rng.choice(["mock-gp-feature-list", "mock-gp-bullet",
                                    "mock-gp-numbered"]),
        "domain": domain,
        "temperature": round(rng.uniform(0.5, 0.9), 2),
        "input": {
            "projectId": project_id(rng),
            "projectName": project_name,
            "description": description,
        },
        "output": {"sprints": sprints},
    }


BUILDERS = {
    "scaffold-project": build_scaffold_record,
    "enrich-issue": build_enrich_record,
    "generate-plan": build_plan_record,
}


# -------------------------------------------------------------- Driver ---

def append_jsonl(path: Path, items: list[dict[str, Any]]) -> None:
    with path.open("a", encoding="utf-8") as f:
        for it in items:
            f.write(json.dumps(it, ensure_ascii=False) + "\n")


def count_lines(path: Path) -> int:
    if not path.exists():
        return 0
    with path.open("r", encoding="utf-8") as f:
        return sum(1 for _ in f)


def generate(feature: str, count: int, seed: int = 0) -> int:
    if count <= 0:
        return 0
    builder = BUILDERS[feature]
    rng = random.Random(seed if seed else None)
    out = [builder(rng) for _ in range(count)]
    append_jsonl(FILES[feature], out)
    return len(out)


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--feature", choices=list(BUILDERS.keys()))
    ap.add_argument("--count", type=int, default=0)
    ap.add_argument("--all", action="store_true",
                    help="top up all three features to --target")
    ap.add_argument("--target", type=int, default=350,
                    help="target line count per feature when --all")
    ap.add_argument("--seed", type=int, default=0)
    args = ap.parse_args()

    if args.all:
        total = 0
        for feat, path in FILES.items():
            cur = count_lines(path)
            need = max(0, args.target - cur)
            written = generate(feat, need, seed=args.seed)
            total += written
            print(f"{feat}: {cur} -> {cur + written} (+{written})")
        print(f"TOTAL written: {total}")
        return

    if not args.feature or args.count <= 0:
        ap.error("either --all or both --feature and --count required")
    cur = count_lines(FILES[args.feature])
    written = generate(args.feature, args.count, seed=args.seed)
    print(f"{args.feature}: {cur} -> {cur + written} (+{written})")


if __name__ == "__main__":
    main()
