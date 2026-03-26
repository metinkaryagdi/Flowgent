IdentityService — Mimari İnceleme Notları
Kritik Problemler

Auth API ile user-management API çakışıyor

Aynı kullanıcı oluşturma işi iki ayrı akıştan yapılıyor:

public auth register

user-management register

Semantik fark var, bu da ileride tutarsızlık üretir.

Kritik endpoint’ler açık olabilir

User update/delete, role create, public user lookup gibi endpoint’lerde yetkilendirme zaafı olabilir.

Bu konu en yüksek öncelikli güvenlik notlarından biri.

HS256 shared secret riski

Aynı symmetric key birden fazla yerde/paylaşımlı kullanılıyorsa tek servis compromise olduğunda tüm platform için token forge riski oluşur.

Refresh token’lar plaintext saklanıyor olabilir

DB/back-up sızıntısında doğrudan oturum ele geçirme riski doğar.

Revocation / invalidation zayıf

Role/status/deletion sonrası aktif access token’lar yaşamaya devam ediyor olabilir.

Katmanlama Sorunları

Update akışı domain method’ları yerine AutoMapper ile entity mutate ediyor olabilir

Bu durum invariant ve audit davranışlarını application tarafına kaçırabilir.

JWT ve password hashing doğru katmanda gibi görünüyor

Şimdilik bu olumlu not.

Ama session/revocation/audit modeli eksik olabilir.

Bounded Context / Ownership Notları

Identity bounded context genel olarak doğru görünüyor

User, Role, RefreshToken burada

profile/preferences burada değil

bu iyi

Credential ayrı model değil

PasswordHash doğrudan User içine gömülü olabilir.

Şimdilik MVP için tolere edilebilir ama ileride MFA/passkey/password-history gibi ihtiyaçlarda zorlayabilir.

Permission/scope modeli eksik olabilir

Sadece role bazlı authorization varsa servisler kendi kafasına göre yetki semantiği üretmeye başlayabilir.

Event Tasarımı Notları

Identity lifecycle event’leri görünmüyor

UserCreated

UserDeactivated

UserRoleChanged

PasswordChanged
gibi event’ler yoksa diğer servisler stale auth state ile çalışabilir.

Outbox / event publishing Identity tarafında da gerekli olabilir

Özellikle user lifecycle değişiklikleri için.

Olumlu taraf

Event yoksa password hash sızıntısı da event payload ile olmuyordur.

Ama bu “iyi tasarım” değil, “eksik entegrasyon” sonucu olabilir.

Veri Tutarlılığı Notları

Register / login / refresh aynı DbContext içinde işleniyorsa olumlu

Bu yerel tutarlılık açısından iyi.

Refresh token tablosunda FK/session integrity zayıf olabilir

Sadece index varsa yeterli olmayabilir.

Role/status değişimlerinin token tarafına etkisi zayıf

Token expire olana kadar eski yetkiyle yaşamaya devam etme riski var.

Token / Session Mimari Notları

Refresh flow eksik veya yarım olabilir

Handler var ama controller endpoint yok gibi bir tutarsızlık olabilir.

Cookie lifetime ile DB lifetime tutarsız olabilir

Session davranışı öngörülemez hale gelir.

Logout gerçek revoke yapmıyor olabilir

Sadece cookie temizlemek yetmeyebilir.

Secure cookie davranışı reverse proxy arkasında riskli olabilir

Request.IsHttps yaklaşımı forwarded header ayarı olmadan yanlış karar verebilir.

Güvenlik Notları

Brute force / rate limit / lockout görünmüyor

Login tarafında temel korumalar eksik olabilir.

Audit logging zayıf olabilir

Özellikle security event’ler için.

PII sızıntısı olabilir

Public user lookup email döndürüyorsa servis bir identity directory gibi davranmaya başlayabilir.

Normalization eksik olabilir

Username/email/role canonicalization yoksa:

Alice@example.com

alice@example.com
gibi çakışmalı kimlikler oluşabilir.

DDD Notları

Identity vs Profile ayrımı büyük ölçüde temiz

Bu iyi bir mimari işaret.

User aggregate zayıf olabilir

failed login count

lockout

security stamp

password changed at

session version
gibi güvenlik odaklı state’ler görünmüyor olabilir.

RefreshToken session surrogate gibi kullanılıyor ama zengin değil

device/IP/user-agent/token-family/reuse-detection metadata eksik olabilir.

User lifecycle modeli eksik tamamlanmış olabilir

Pending/Suspended/Deactivated enum/model seviyesinde var ama gerçek akışta kullanılmıyor olabilir.

IdentityService için geçici hüküm

Bounded context genel olarak doğru

Ama security architecture zayıf olabilir

özellikle endpoint authorization, token architecture, refresh storage, revocation ve lifecycle events tarafında ciddi riskler var

major refactor adayı, ama diğer servisler görüldükten sonra ortak plan çıkarılacak

Şu ana kadar ortak temalar oluşmaya başladı

IssueService + IdentityService birlikte bakınca şimdiden bazı pattern’ler görünüyor:

katman sınırları bazı yerlerde bulanık

güvenilir event yayını eksik veya zayıf

lifecycle değişikliklerinin diğer servislere yansıması zayıf

güvenlik ve dağıtık tutarlılık production seviyesine tam çıkmamış