# AI Agent Hedef Davranışları (v1)

**Tarih:** 2026-04-24
**Durum:** Faz 0 çıktısı — fine-tune öncesi referans
**Bağlı:** [`ai-agent-fine-tuning-plan.md`](./ai-agent-fine-tuning-plan.md)

---

## Amaç

Bu döküman, fine-tune edilmiş `bp-agent` modelinden **beklenen davranışı**
örneklerle sabitler. Fine-tune dataset'i bu örneklere hizalı üretilir,
eval seti bu davranışları ölçer, savunma sunumunda "hedef bu, ulaştık /
ulaşamadık" argümanının anchor'ıdır.

**Kapsam (v1):** 3 özellik — `scaffold-project`, `enrich-issue`, `generate-plan`.
**Dil:** Türkçe (input ve output).
**Format:** Her özellik için tek bir JSON şeması, format kayması yok.

---

## Ortak Kurallar (Tüm özellikler)

1. **Yalnızca JSON** döndür. Markdown fence (```), açıklama metni, "Elbette,
   işte..." girişi, trailing yorum yok.
2. Alan adları ve tipler şemaya birebir uymalı. Eksik alan → parse hatası
   → eval metriğinde başarısız.
3. Türkçe karakterler (ç, ğ, ı, ö, ş, ü) düz string olarak; unicode
   escape (`ç`) üretilmez.
4. Alana değer zorla girmek için uydurma yapma: yeterli bilgi yoksa
   konservatif default (örn. `storyPoints: 3`, `priority: "Medium"`).
5. Kullanıcı girdisinde başka bir organizasyonun verisi, kişi adı, email
   geçse bile çıktıya **kopyalanmaz** (PII leakage önlemi).

---

## Özellik 1 — `scaffold-project`

**Kullanıcı hikâyesi:** AI Asistan sayfasında kullanıcı serbest metinle
projesini anlatır ("E-ticaret sitesi kuracağım, ödeme + sepet + ürün
katalogu olsun"). Sistem `ProjectDraft` JSON'u üretir; kullanıcı onaylayınca
Project + Sprint + Issue gerçek kayıtlar olarak yaratılır.

### Girdi formatı

```json
{
  "description": "Serbest metin, en az 20 en fazla 1000 karakter.",
  "context": {
    "organizationName": "Opsiyonel — üslubu kalibre etmek için",
    "preferredSprintCount": 3
  }
}
```

### Çıktı şeması

```json
{
  "project": {
    "name": "string, 3-60 karakter, Title Case",
    "key": "string, 2-8 karakter, UPPERCASE, proje name'inden türet",
    "description": "string, 20-500 karakter, projenin 1-2 cümlelik özeti"
  },
  "sprints": [
    {
      "name": "string, 'Sprint N: Tema' formatında",
      "goal": "string, tek cümle, sprint sonunda ulaşılacak durum",
      "issues": [
        {
          "title": "string, 5-80 karakter, fiille başlar (örn. 'Kullanıcı kaydı ekranı oluştur')",
          "description": "string, 20-300 karakter",
          "priority": "Low | Medium | High | Critical",
          "storyPoints": "int, Fibonacci: 1 | 2 | 3 | 5 | 8 | 13"
        }
      ]
    }
  ]
}
```

### Kısıtlar

- `sprints.length`: 2-4
- `issues.length` (sprint başına): 3-6
- Issue title'da jargon yok ("login flow'u handle et" ❌ → "giriş akışını uygula" ✅)
- Toplam story point sprint başına 15-30 arası hedef

### Örnek

**Girdi:**
```json
{
  "description": "Üniversite kulüplerini yönetmek için bir sistem. Etkinlik oluşturma, üye kaydı, yoklama alma özellikleri olsun. Başkanın yetkisi üyeden farklı olmalı.",
  "context": { "preferredSprintCount": 2 }
}
```

**Beklenen çıktı:**
```json
{
  "project": {
    "name": "Kulüp Yönetim Sistemi",
    "key": "KYS",
    "description": "Üniversite kulüplerinin üye, etkinlik ve yoklama süreçlerini dijitalleştiren web uygulaması. Başkan ve üye rollerinde ayrı yetkiler sunar."
  },
  "sprints": [
    {
      "name": "Sprint 1: Temel Altyapı ve Üyelik",
      "goal": "Üye kaydı ve rol bazlı giriş çalışır durumda olsun.",
      "issues": [
        { "title": "Üye kayıt formunu oluştur", "description": "Ad, soyad, öğrenci no ve email alanları; doğrulama kodlu email onayı.", "priority": "High", "storyPoints": 5 },
        { "title": "Rol bazlı giriş akışını uygula", "description": "Başkan ve üye rolleri, JWT tabanlı oturum, rol bazlı sayfa erişimi.", "priority": "High", "storyPoints": 8 },
        { "title": "Kulüp profili düzenleme ekranını oluştur", "description": "Başkanın kulüp adını, logosunu ve açıklamasını güncelleyebileceği form.", "priority": "Medium", "storyPoints": 3 }
      ]
    },
    {
      "name": "Sprint 2: Etkinlik ve Yoklama",
      "goal": "Etkinlik yayınlanabilsin ve yoklama dijital alınabilsin.",
      "issues": [
        { "title": "Etkinlik oluşturma ekranını oluştur", "description": "Tarih, konum, kapasite ve açıklama alanları; sadece başkan erişebilir.", "priority": "High", "storyPoints": 5 },
        { "title": "Etkinliğe katılım kaydını uygula", "description": "Üye bir butonla katılım bildirir, kapasite dolunca kayıt kapanır.", "priority": "Medium", "storyPoints": 3 },
        { "title": "QR kodlu yoklama akışını uygula", "description": "Etkinlik anında üretilen QR üye telefonundan okutulur, yoklama listesine eklenir.", "priority": "Medium", "storyPoints": 5 }
      ]
    }
  ]
}
```

---

## Özellik 2 — `enrich-issue`

**Kullanıcı hikâyesi:** Issue listesinde "AI ile Zenginleştir" butonu. Kullanıcı
sadece title yazmış ("Parola sıfırlama ekranı"); AI description + acceptance
criteria + edge case + story point önerisi üretir.

### Girdi formatı

```json
{
  "title": "string, 3-120 karakter",
  "projectContext": "opsiyonel, 'e-ticaret mobil app' gibi kısa etiket"
}
```

### Çıktı şeması (mevcut `OllamaEnrichResponse` ile birebir)

```json
{
  "description": "string, 50-400 karakter, ne yapılacağının net tarifi",
  "acceptanceCriteria": "string, '- ' ile başlayan satır listesi, 3-6 madde",
  "edgeCases": "string, '- ' ile başlayan satır listesi, 2-5 madde",
  "storyPoints": "int, Fibonacci: 1 | 2 | 3 | 5 | 8 | 13"
}
```

### Kısıtlar

- Acceptance criteria **test edilebilir** olmalı ("güzel görünmeli" ❌ → "mobil 375px'de taşma yok" ✅).
- Edge case'ler gerçek senaryo ("network kopunca", "aynı anda iki istek" gibi) — generic değil ("hata olursa" ❌).
- Description ve acceptance criteria arasında çakışma minimum.

### Örnek

**Girdi:**
```json
{ "title": "Parola sıfırlama ekranı", "projectContext": "web SaaS" }
```

**Beklenen çıktı:**
```json
{
  "description": "Kullanıcı email'ini girdiğinde, eğer hesap varsa tek kullanımlık sıfırlama linki email ile gönderilir. Link 30 dakika geçerlidir ve tıklandığında yeni parola belirleme ekranına yönlendirir.",
  "acceptanceCriteria": "- Email boş veya geçersiz formatta ise inline hata gösterilir\n- Kayıtlı olmayan email için de 'Eğer hesap varsa link gönderildi' mesajı gösterilir (enumeration saldırısını engeller)\n- Link tıklanınca yeni parola ekranı açılır, token query param'dan okunur\n- Yeni parola minimum 8 karakter, 1 büyük harf, 1 rakam kuralına uyar\n- Parola başarıyla değişince kullanıcı login ekranına yönlendirilir",
  "edgeCases": "- Token süresi dolmuşsa kullanıcıya 'Link süresi doldu, yeniden talep edin' mesajı\n- Aynı token iki kez kullanılmaya çalışılırsa ikincisi reddedilir\n- Kullanıcı link gönderildikten sonra email adresini değiştirirse eski link çalışmaya devam etmemeli",
  "storyPoints": 5
}
```

---

## Özellik 3 — `generate-plan`

**Kullanıcı hikâyesi:** Proje zaten var; kullanıcı "bu projeye şu özelliklerin
sprint planını çıkar" der. `scaffold-project`'ten farkı: `project` alanı
üretilmez, sadece `sprints[]`.

### Girdi formatı

```json
{
  "projectId": "guid",
  "projectName": "Mevcut proje adı (model bağlamı için)",
  "description": "Yeni özelliklerin serbest metin tarifi"
}
```

### Çıktı şeması (mevcut `OllamaPlanResponse` ile birebir)

```json
{
  "sprints": [
    {
      "name": "string, 'Sprint N: Tema' formatı",
      "goal": "string, tek cümle",
      "issues": [
        { "title": "...", "description": "...", "priority": "Low|Medium|High|Critical", "storyPoints": 1|2|3|5|8|13 }
      ]
    }
  ]
}
```

### Kısıtlar

- `scaffold-project` ile aynı sprint/issue kuralları.
- Mevcut proje context'i varsa sprint tema'ları mevcut işi tekrar etmemeli.

### Örnek

**Girdi:**
```json
{
  "projectId": "9f3a...",
  "projectName": "Kulüp Yönetim Sistemi",
  "description": "Raporlama modülü ekle: aylık üye grafiği, etkinlik katılım oranı, kulüpler arası karşılaştırma."
}
```

**Beklenen çıktı:**
```json
{
  "sprints": [
    {
      "name": "Sprint 1: Raporlama Altyapısı",
      "goal": "Veri katmanı ve temel metrik sorguları hazır olsun.",
      "issues": [
        { "title": "Metrik sorgu katmanını oluştur", "description": "Üye sayısı, etkinlik sayısı, katılım oranı için read-only view veya materialize sorgular.", "priority": "High", "storyPoints": 5 },
        { "title": "Rapor API endpoint'lerini uygula", "description": "GET /reports/members-monthly, /reports/event-attendance, /reports/club-comparison.", "priority": "High", "storyPoints": 5 },
        { "title": "Rapor yetkilendirmesini uygula", "description": "Sadece başkan rolü raporlara erişebilir; üye 403 alır.", "priority": "Medium", "storyPoints": 3 }
      ]
    },
    {
      "name": "Sprint 2: Raporlama UI",
      "goal": "Başkan dashboard'ında grafikler görüntülenir.",
      "issues": [
        { "title": "Aylık üye grafiği bileşenini oluştur", "description": "Son 12 ay çubuk grafiği, tooltip ile net sayı.", "priority": "High", "storyPoints": 5 },
        { "title": "Etkinlik katılım oranı görselini oluştur", "description": "Etkinlik bazında kayıtlı / katılan oranı, liste + yüzde.", "priority": "Medium", "storyPoints": 3 },
        { "title": "Kulüp karşılaştırma ekranını oluştur", "description": "Başkanın kendi kulübünü seçili diğerleriyle yan yana karşılaştırabildiği tablo.", "priority": "Medium", "storyPoints": 5 }
      ]
    }
  ]
}
```

---

## Negatif Örnekler (Model yapmamalı)

Fine-tune datasetine negatif örnek olarak konulmaz; eval setinde
`must_not_contain` alanına girer.

1. **Markdown fence:** ` ```json ... ``` ` → parse fail.
2. **Açıklama metni:**
   ```
   Elbette, işte projeniz için oluşturduğum sprint planı:
   { "sprints": ... }
   ```
3. **İngilizce çıktı:** Girdi Türkçe ise çıktı Türkçe olmalı.
4. **Uydurma alan:** `"estimatedHours": 40` gibi şemada olmayan alanlar.
5. **Şemada olan alanı atlama:** `storyPoints` yok → default 3 değil, parse fail.
6. **Generic issue title:** "Frontend geliştir", "Backend yap" → çok geniş, story point'le uyuşmaz.
7. **Priority değeri dışında:** `"priority": "Yüksek"` (Türkçe) veya `"Urgent"` → enum dışı.

---

## Değişiklik Geçmişi

- **2026-04-24:** v1 dökümanı. Faz 0 çıktısı olarak oluşturuldu.
