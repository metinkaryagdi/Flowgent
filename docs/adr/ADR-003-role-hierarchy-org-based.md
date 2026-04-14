# ADR-003: Rol Hiyerarşisinin Organizasyon Bazlı Yapıya Geçişi

**Tarih:** 2026-04-05  
**Durum:** Kabul Edildi — Kademeli Geçiş Planlandı  
**Karar Veren:** Metin Karyağdı

---

## Bağlam

Sistemde iki ayrı rol mekanizması eş zamanlı çalışıyor:

**Sistem Rolleri (ASP.NET Identity — `AspNetRoles` tablosu):**
- `Admin` — sistem geneli superadmin
- `Manager` — "elevated project permissions" (belirsiz)
- `Member` — yeni kayıt default

**Organizasyon Rolleri (`OrganizationMember.Role` + JWT `org_role` claim):**
- `Owner` — org kurucusu
- `Manager` — org admini
- `Member` — standart üye

`Manager` ve `Member` her iki sistemde de mevcut fakat farklı anlama geliyor.  
Bir kullanıcı sistem rolü `Manager` iken org rolü `Member` olabilir — bu durumun ne anlama geldiği belirsiz ve tutarsız.

---

## Değerlendirilen Seçenekler

---

### Seçenek A — Hibrit (Mevcut Durum: İkisi Birden)

Sistem rolleri (`Admin`, `Manager`, `Member`) + Org rolleri (`Owner`, `Manager`, `Member`) eş zamanlı kullanılır.

#### Kazanımlar

| # | Kazanım | Açıklama |
|---|---------|----------|
| 1 | Hızlı backend kontrolü | `[Authorize(Roles = "Manager")]` ile tek satır yetkilendirme |
| 2 | Mevcut kod çalışıyor | Refactor iş yükü yok |
| 3 | ASP.NET Identity entegrasyonu | Framework desteği tam |

#### Kayıplar

| # | Kayıp | Açıklama |
|---|-------|----------|
| 1 | İki `Manager` kavramı çakışıyor | Sistem `Manager` mı, org `Manager` mı geçerli? Belirsiz |
| 2 | JWT tutarsızlığı | `role: Manager` + `org_role: Member` aynı token'da olabilir |
| 3 | Org izolasyonu zayıf | Sistem `Manager` tüm org'larda mı etkili? Hayır — ama kod bunu garanti etmiyor |
| 4 | Admin panelde rol karmaşası | Rol atarken "Manager" sistem mi org mu, kullanıcı bilmiyor |
| 5 | Multi-tenant güvenlik riski | Yanlış `[Authorize(Roles)]` kullanımı veri sızıntısına yol açabilir |
| 6 | Ölçeklenemiyor | Yeni bir org özelliği eklenince hangi rol sistemi kullanılacak belirsiz |

---

### Seçenek B — Tamamen Sistem Bazlı

Org rolleri kaldırılır. Tek sistem: `Admin > Manager > Member` — hepsi sistem geneli.

#### Kazanımlar

| # | Kazanım | Açıklama |
|---|---------|----------|
| 1 | Tek kaynak | Rol karmaşası tamamen ortadan kalkar |
| 2 | Basit yetkilendirme | `[Authorize(Roles = "X")]` her yerde tutarlı çalışır |
| 3 | Mevcut Identity altyapısı yeterli | Ekstra tablo/claim yok |

#### Kayıplar

| # | Kayıp | Açıklama |
|---|-------|----------|
| 1 | Multi-tenant anlamsız hale gelir | A şirketinin Manager'ı B şirketinin verilerini yönetemez — sistem bunu engelleyemez |
| 2 | Org izolasyonu çöker | Tüm org'lar aynı rol havuzunu paylaşır |
| 3 | Organization feature'ı yarım kalır | OrganizationMember, InviteToken tabloları var ama rolleri yok |
| 4 | Gerçek SaaS modeline aykırı | Hiçbir modern çok kiracılı sistem bu modeli kullanmıyor |
| 5 | Sunum zayıflığı | "Multi-tenant mimariniz var ama rol izolasyonu yok" sorusuna cevap yok |

---

### Seçenek C — Tamamen Org Bazlı ✅ SEÇİLDİ

Sistem rolü: **sadece `Admin`**  
Kullanıcı yetkileri tamamen org rolüne (`org_role` JWT claim) taşınır: `Owner > Manager > Member`

```
JWT içeriği (hedef):
  role:     "Member"   ← admin dışındaki herkese anlamsız, sadece Admin kontrolü için
  org_id:   "abc-123"
  org_role: "Manager"  ← asıl yetki kaynağı
```

#### Kazanımlar

| # | Kazanım | Açıklama |
|---|---------|----------|
| 1 | Tek gerçek yetki kaynağı | `org_role` claim — net, tutarsızlık yok |
| 2 | Gerçek org izolasyonu | A org Manager'ı B org'unda Member olabilir, sistem bunu doğal olarak destekler |
| 3 | Sektör standardı | Slack, GitHub, Linear, Auth0, Keycloak — hepsi bu modeli kullanıyor |
| 4 | JWT tutarlı | `role` ve `org_role` asla çelişmez |
| 5 | Sunum güçlü | "Yetki sisteminiz nasıl çalışıyor?" sorusuna net cevap |
| 6 | Ölçeklenebilir | Yeni org özelliği eklenince `org_role` yeterli |

#### Kayıplar

| # | Kayıp | Açıklama |
|---|-------|----------|
| 1 | `[Authorize(Roles)]` yetersiz | `org_role` claim kontrolü için custom middleware veya helper gerekir |
| 2 | Refactor iş yükü | `DefaultIdentityRoles.Manager/Member` kullanılan yerlerin gözden geçirilmesi gerekir |
| 3 | Kademeli geçiş karmaşıklığı | Geçiş tamamlanana kadar iki sistem eş zamanlı yaşar |

---

## Karşılaştırmalı Özet

| Kriter | A (Hibrit) | B (Sistem Bazlı) | C (Org Bazlı) |
|--------|-----------|-----------------|--------------|
| Org izolasyonu | ❌ Zayıf | ❌ Yok | ✅ Tam |
| JWT tutarlılığı | ❌ Çelişki riski | ✅ Tutarlı | ✅ Tutarlı |
| Yetkilendirme netliği | ❌ İki kaynak | ✅ Tek kaynak | ✅ Tek kaynak |
| Multi-tenant güvenlik | ⚠️ Riskli | ❌ Yok | ✅ Güvenli |
| Refactor maliyeti | ✅ Sıfır | ❌ Yüksek (org kaldırılır) | ⚠️ Orta (kademeli) |
| Sektör uyumu | ❌ Hayır | ❌ Hayır | ✅ Evet |
| Sunum savunulabilirliği | ❌ Zayıf | ❌ Zayıf | ✅ Güçlü |

---

## Karar: Seçenek C — Kademeli Geçiş Planı

### Geçiş Stratejisi

**Kırılmayan geçiş:** `DefaultIdentityRoles.Manager` ve `Member` silinmez, yeni yetkilendirme kodunda **kullanılmaz**.

1. Yeni endpoint'ler `org_role` claim'e göre yetkilendirilir
2. Eski `[Authorize(Roles = "Manager")]` kullanımları `org_role` kontrolüne taşınır
3. Admin panelde atanan `Manager`/`Member` sistem rolleri görünür ama fiilen işlevsiz hale gelir
4. İleride `DefaultIdentityRoles` sadece `Admin` içerecek şekilde temizlenir

### Hedef Yetkilendirme Modeli

```csharp
// Eski (kaldırılacak):
[Authorize(Roles = "Manager")]

// Yeni (org bazlı):
[Authorize]
var orgRole = User.FindFirst("org_role")?.Value;
if (orgRole is not "Owner" and not "Manager")
    return Forbid();
```

### Rol Sorumlulukları (Hedef)

| Rol | Kapsam | Yetkiler |
|-----|--------|----------|
| `Admin` | Sistem geneli | Tüm kullanıcılar, tüm org'lar, rol atamaları |
| `Owner` | Org içi | Org ayarları, üye yönetimi, tüm projeler, davet gönder |
| `Manager` | Org içi | Proje oluştur, üye davet et, sprint/issue yönet |
| `Member` | Org içi | Atanan issue'ları gör/güncelle, yorum yap |

---

## Referanslar

- GitHub Organizations: sistem admin ayrı, org role ayrı
- Slack Workspaces: `workspace_admin`, `owner`, `member` — sistem geneli admin yok, her workspace kendi rollerini yönetir
- Auth0 Organizations: `org_role` claim tabanlı yetkilendirme — bu sistemle birebir örtüşüyor
