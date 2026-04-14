# ADR-001: Organization Yönetiminin IdentityService İçinde Tutulması

**Tarih:** 2026-04-05  
**Durum:** Kabul Edildi  
**Karar Veren:** Metin Karyağdı

---

## Bağlam

Çok kiracılı (multi-tenant) yapı eklenirken `Organization`, `OrganizationMember` ve `InviteToken` domain modelleri tasarlandı. Bu modellerin hangi microservice'e ait olacağı sorgulandı:

- **Seçenek A:** Mevcut `IdentityService` içinde tutmak
- **Seçenek B:** Bağımsız bir `OrganizationService` oluşturmak

---

## Karar

**Seçenek A — Organization yönetimi IdentityService içinde kalır.**

---

## Gerekçe

### 1. JWT ile Yapısal Bağımlılık

JWT token'ına `org_id` ve `org_role` claim'leri eklenmesi gerekiyor. Token üretimi IdentityService'in çekirdeğinde gerçekleşiyor. Ayrı bir servis kurulursa token üretimi sırasında IdentityService'in OrganizationService'e HTTP çağrısı atması gerekir; bu da her login/register akışına ağ gecikmesi ve partial failure riski ekler.

### 2. Tek DB Transaction Zorunluluğu

`AcceptInvite` (davet kabul) ve `Register` (yeni kullanıcı kaydı) akışları atomik olmak zorunda: kullanıcı kaydı başarılı olup org üyeliği başarısız olursa tutarsız bir state oluşur. Aynı veritabanı ve aynı `DbContext` içinde kalmak ACID garantisi sağlar. Servisler ayrılırsa Saga/Outbox pattern ile distributed transaction yönetimi gerekir — bu, proje kapsamını ve hata olasılığını önemli ölçüde artırır.

### 3. "Identity Bounded Context" Savunulabilirliği

Domain-Driven Design perspektifinden bakıldığında "bir kullanıcının hangi organizasyona ait olduğu ve bu organizasyondaki rolü" kimlik bilgisinin bir parçasıdır. Auth0, Okta, Keycloak ve GitHub gibi endüstri ölçeğindeki sistemler de organization/tenant yönetimini authentication servisiyle birleşik tutar. Bu kararın akademik ve sektörel referansı mevcuttur.

### 4. SwitchOrganization Akışı

Kullanıcı aktif organizasyonunu değiştirdiğinde (`POST /organizations/{id}/switch`) yeni bir JWT üretilmesi gerekir. Bu işlem aynı servis içinde tek bir handler çağrısıyla tamamlanır. Ayrı servis senaryosunda OrganizationService → IdentityService iletişimi (2 hop) ve token'ın geri iletilmesi gerekir.

### 5. Maliyet-Fayda Dengesi

Ayrı bir servis; yeni bir PostgreSQL container'ı, ayrı migration yönetimi, Gateway route eklemesi, servisler arası iletişim protokolü ve ek test kapsamı gerektirir. Bu yükün sağlayacağı tek somut kazanım "servis sayısının artması"dır — işlevsel bir fayda sunmaz.

---

## Kabul Edilen Ödünleşim

IdentityService, bu kararla birlikte hem authentication hem de organization yönetiminden sorumlu olur; bu Single Responsibility Principle'ı hafifçe esnetir. Ancak organization yönetiminin kullanıcı kimliğiyle olan yapısal bağı göz önüne alındığında bu ödünleşim bilinçli ve savunulabilir kabul edilmiştir.

---

## Alternatifin Reddedilme Nedeni (Seçenek B)

| Risk | Açıklama |
|------|----------|
| Distributed transaction | AcceptInvite atomikliği için Saga pattern gerekir |
| Login gecikme | Her token üretiminde +1 servis çağrısı |
| Partial failure | OrganizationService düşerse login akışı bozulur |
| Geliştirme maliyeti | ~2-3 gün ek çalışma, yeni container, yeni testler |
| Gereksiz karmaşıklık | Fonksiyonel kazanım olmadan mimari yük artar |

---

## Referanslar

- Auth0 Organizations: organization yönetimi auth platformu içinde
- Keycloak Realms: tenant izolasyonu authentication servisinin bir özelliği
- Martin Fowler — *Patterns of Enterprise Application Architecture*: transaction boundary'nin servis sınırını belirlediği prensip
