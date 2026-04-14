# ADR-002: Organizasyon Route Guard ve Admin Kapsam Kararları

**Tarih:** 2026-04-05  
**Durum:** Kabul Edildi — Henüz Uygulanmadı  
**Karar Veren:** Metin Karyağdı

---

## Bağlam

Sidebar'da "Projeler" ve "Bildirimler" nav item'ları, kullanıcının aktif bir organizasyonu olup olmadığından bağımsız olarak her zaman görünüyor. Bu iki sorunu doğuruyor:

1. Org'u olmayan kullanıcı "Projeler"e girdiğinde neden boş olduğunu anlamıyor — ProjectService `org_id` claim olmadan hiçbir şey döndürüyor.
2. Admin paneli tüm sistemi görüyor; sistem admini ile organizasyon yöneticisi aynı role sığdırılamaz.

---

## Karar 1: Admin Rol Ayrımı

**Admin = Sistem genelinde superadmin.** Ayrı bir kullanıcı olarak seed edilir, tüm kullanıcıları ve organizasyonları görür. Mevcut davranış **doğru** — değiştirilmez.

**Manager = Organizasyon admini.** Sadece kendi organizasyonundaki üyeleri yönetir. Admin paneline erişimi yoktur; yönetim arayüzü `/settings/organization` ile sınırlıdır.

### Gerekçe

Sistem admini ile organizasyon yöneticisini aynı role sıkıştırmak multi-tenant güvenlik modelini bozar. Bir Manager başka organizasyonların verilerini görememelidir. Mevcut `[Authorize(Roles = "Admin")]` koruması bu ayrımı zaten backend'de sağlıyor; frontend'de de aynı ayrım korunacak.

---

## Karar 2: Route Guard — Org Olmadan Korunan Sayfalara Erişim

### Kullanıcı Tipleri ve Akışlar

| Kullanıcı Tipi | Giriş Yolu | Org Durumu | Route Guard Davranışı |
|----------------|-----------|------------|----------------------|
| **Owner** | Normal kayıt | Org yok → onboarding yapmalı | `/onboarding`'e yönlendir |
| **Davetli Üye** | Email linki → AcceptInvite | Org var (token'a eklendi) | Guard tetiklenmez |
| **Sistem Admini** | Manuel / seed | Org olmayabilir | Guard'dan muaf |

### Route Guard Mantığı

```
if (roles.includes('Admin'))  → geçir
if (activeOrg !== null)       → geçir
else                          → /onboarding'e yönlendir
```

### Muaf Tutulan Route'lar

- `/notifications` — Bildirimler `user_id`'ye bağlı, `org_id`'ye değil. Org olmadan da davet bildirimi gelebilir.
- `/onboarding` — Sonsuz döngü olmaması için kendisi muaf.
- `/invite/accept` — Davet kabul akışı org oluşturmadan önce çalışır.
- `/login`, `/register` — Auth sayfaları zaten korumasız.

---

## Neden Org Kurmak Zorunlu? (Owner için)

| Zorunlu Onboarding | İsteğe Bağlı |
|--------------------|--------------|
| Sistemde "org'suz proje" state'i hiç oluşmaz | Boş proje listesi neden boş? Kullanıcı anlamaz |
| ProjectService her zaman geçerli `org_id` ile çalışır | Backend'de `org_id null` edge case yönetimi gerekir |
| Slack, Notion, Linear — hepsi zorunlu workspace kurulumu | Ekstra karmaşıklık, sıfır kazanım |
| Onboarding sayfası zaten mevcut | — |

---

## Neden Davetli Üye İçin İstisna Yazmak Gerekmez?

`AcceptInvite` command handler yeni bir JWT üretiyor ve bu token'ın içine `org_id` + `org_role` claim'leri ekleniyor. Frontend bu token'ı cookie'ye set ettiği anda `activeOrg` store'u dolacak. Route guard `activeOrg !== null` kontrolünde geçirecek — özel bir istisna mantığı yazmaya gerek yok. Sıfır ekstra iş.

---

## Uygulama Notu

Bu kararlar **onaylandı, henüz uygulanmadı.** Uygulamaya geçildiğinde:

1. Frontend: `ProtectedRoute` veya layout-level guard bileşeni
2. Guard içinde `useAuthStore`'dan `activeOrg` ve `roles` okunur
3. Admin bypass → org check → onboarding redirect sırası korunur
4. Backend `[Authorize(Roles = "Admin")]` kontrolü zaten mevcut, dokunulmaz
