# ADR-004: Tespit Edilen Authorization Açıkları ve Çözüm Planı

**Tarih:** 2026-04-05  
**Durum:** Tespit Edildi — Çözüm Bekliyor  
**Karar Veren:** Metin Karyağdı

---

## Bağlam

Organization + multi-tenant yapısı eklendikten sonra mevcut servislerin authorization kontrollerinin yeterliliği incelendi. Sistemde üç katmanda sorun tespit edildi: cross-org veri sızıntısı, org rol enforcement eksikliği ve gateway-downstream kopukluk.

---

## 🔴 Kritik — Cross-Org Veri Sızıntısı

### 1. IssueService — Org Kontrolü Yok

**Endpoint:** `GET /api/v1/issues/project/{projectId}`  
**Mevcut koruma:** `[Authorize]` — login olmak yeterli  
**Sorun:** `projectId` bilen her authenticate kullanıcı, farklı bir org'dan bile olsa o projenin tüm issue'larını çekebilir.

Gateway `X-Organization-Id` header'ını downstream'e iletmekte ancak IssueService bu header'ı hiç okumamaktadır.

**Çözüm:** `projectId` → org doğrulaması. Proje hangi org'a aitse, caller'ın `X-Organization-Id`'si eşleşmeli.

---

### 2. SprintService — Org Kontrolü Yok

**Endpoint:** `GET /api/v1/sprints/project/{projectId}` (ve diğer proje bazlı endpointler)  
**Sorun:** IssueService ile aynı pattern. `projectId` üzerinden çalışıyor, org doğrulaması yok.

**Çözüm:** IssueService ile aynı yaklaşım.

---

### 3. ProjectService `GetById` — Org Kontrolü Yok

**Endpoint:** `GET /api/v1/projects/{id}`  
**Mevcut koruma:** `[Authorize]`  
**Sorun:** Proje ID'si bilen herhangi bir authenticate kullanıcı başka org'un proje detaylarını okuyabilir.  
`GetProjectsByUser` org'a göre filtrelenmiş ama `GetById` değil — tutarsızlık.

**Çözüm:** Handler'da `project.OrganizationId == callerOrgId` kontrolü ekle.

---

## 🟡 Orta — Org Rolü Uygulanmıyor

### 4. ProjectService Update/Delete — Sadece Owner Kontrolü

**Mevcut kod:**
```csharp
if (project.OwnerUserId != callerId) return Forbid();
```

**Sorun:** Org `Manager` rolündeki kullanıcı kendi organizasyonundaki bir projeyi güncelleyemiyor veya silemez — sadece projeyi oluşturan kişi yapabilir.  
ADR-003'te kararlaştırılan rol modelinde Manager bu yetkiye sahip olmalı.

**Çözüm:**
```csharp
var orgRole = User.FindFirst("org_role")?.Value;
var isOwnerOrManager = orgRole is "Owner" or "Manager";
if (!isAdmin && project.OwnerUserId != callerId && !isOwnerOrManager)
    return Forbid();
```

---

### 5. InvitesController — `SendInvite` Sadece `[Authorize]`

**Sorun:** Controller seviyesinde org üyelik veya rol kontrolü yok. Herhangi bir kullanıcı, herhangi bir `organizationId` ile davet göndermeye çalışabilir. Savunma tamamen handler katmanına bırakılmış.

**Çözüm:** Handler'daki org üyelik + rol kontrolünü doğrula; gerekirse controller'a ön guard ekle.

---

### 6. OrganizationsController — `RemoveMember` / `ChangeMemberRole`

**Sorun:** Sadece `[Authorize]`. Org üyesi olmayan bir kullanıcı bu endpoint'leri çağırabilir. Savunma handler'da.

**Çözüm:** Handler'daki kontrolleri doğrula; `org_id` claim ile istek yapılan org'un eşleştiğini controller'da da kontrol et.

---

### 7. `org_role` Hiçbir Yerde Enforce Edilmiyor

**Sorun:** JWT'de `org_role` claim (`Owner`, `Manager`, `Member`) mevcut ancak tüm backend'de tek bir endpoint bile bu claim'e göre karar vermiyor. Sadece frontend görüntüleme için kullanılıyor.

**Çözüm:** ADR-003'te kararlaştırılan kademeli geçiş kapsamında tüm ilgili endpoint'lere `org_role` kontrolü eklenecek.

---

## 🔵 Düşük — Tasarım Tutarsızlığı

### 8. Gateway → Downstream Kopukluk

**Sorun:** Gateway aşağıdaki header'ları downstream servislere iletiyor:
- `X-Organization-Id`
- `X-Org-Role`

Ancak IssueService ve SprintService bu header'ları hiç okumuyor. Header'lar iletiliyor ama kullanılmıyor — güvenlik açığının "kapatıldığı" yanılsaması yaratıyor.

**Çözüm:** IssueService ve SprintService'te `X-Organization-Id` header'ını okuyarak org doğrulaması yap.

---

## Özet ve Öncelik Sırası

| # | Sorun | Servis | Önem | Durum |
|---|-------|--------|------|-------|
| 1 | Issue'lara cross-org erişim | IssueService | 🔴 Kritik | Açık |
| 2 | Sprint'lere cross-org erişim | SprintService | 🔴 Kritik | Açık |
| 3 | Projeye cross-org erişim (`GetById`) | ProjectService | 🔴 Kritik | Açık |
| 4 | Manager güncelleme/silme yapamıyor | ProjectService | 🟡 Orta | Açık |
| 5 | SendInvite handler kontrolü | IdentityService | 🟡 Orta | Doğrulanmadı |
| 6 | org_role hiç enforce edilmiyor | Tüm servisler | 🟡 Orta | Açık |
| 7 | Header kullanılmıyor | IssueService, SprintService | 🔵 Düşük | Açık |

---

## Teknik Not: Cross-Org Kontrolü İçin Yaklaşım Seçenekleri

IssueService ve SprintService `projectId` bazlı çalışıyor. Proje → org doğrulaması için iki yaklaşım:

**Seçenek A — Denormalizasyon:** IssueService'in DB'sindeki `Issues` tablosuna `OrganizationId` kolonu ekle. Proje oluşturulunca event ile bu servise de yazılır.
- Avantaj: Ekstra servis çağrısı yok, hızlı  
- Dezavantaj: Veri çoğaltma, event tutarlılığı gerekir

**Seçenek B — Internal HTTP çağrısı:** IssueService, proje ID'si için ProjectService'e internal çağrı atar, org'u öğrenir.
- Avantaj: Tek kaynak (ProjectService)  
- Dezavantaj: Her istekte +1 network hop, latency

**Seçenek C — Gateway'de proje → org doğrulaması:** Gateway route middleware'i proje bazlı isteklerde org kontrolü yapar.
- Avantaj: Downstream servislere dokunmak gerekmez  
- Dezavantaj: Gateway'e iş yükü biner, sorumluluğu aşar

**Öneri:** Seçenek C — kısa vadede en az iş. Uzun vadede Seçenek A daha sağlıklı.
