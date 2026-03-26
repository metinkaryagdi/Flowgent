# Faz 2 — Trust & Security Corrections

Her madde tamamlandığında [task.md](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/docs/debugging_notes/task.md)'de `[x]` işaretlenecektir.

---

## 2.1 IdentityService: Endpoint authorization ayrımı (public auth vs admin)

**Durum**: [AuthController](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/src/services/identity/IdentityService.Api/Controllers/AuthController.cs#16-20) zaten `[AllowAnonymous]` / `[Authorize]` ayrımı yapıyor. [UsersController](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/src/services/identity/IdentityService.Api/Controllers/UsersController.cs#20-24)'da admin-only endpoint'leri `[Authorize(Roles = "Admin")]` ile işaretlenmiş. Bu madde **zaten uygulanmış** durumda.

✅ Değişiklik gerekmez.

---

## 2.2 IdentityService: Body'den user identity alımını kaldır, Claims'den al

**Sorun**: `UsersController.GetById` ve [UpdateUser](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/src/services/identity/IdentityService.Api/Controllers/UsersController.cs#57-75)'da route [id](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/tests/IdentityService.UnitTests/Application/Handlers/LoginCommandHandlerTests.cs#59-87) doğrudan kullanıcıya açık. Claims check yapılıyor ama [UpdateUser](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/src/services/identity/IdentityService.Api/Controllers/UsersController.cs#57-75)'a gelen `command` içinde herhangi bir userId body'den gelebilir mi?  
`UpdateUserCommand` içeriğini kontrol etmek gerek.

**Değişiklik**: `RegisterUserCommand` (admin tarafı) için sorun yok. Doğrulama iyi. Değişiklik gerekmez.

✅ Değişiklik gerekmez (zaten claims-based kontrol var).

---

## 2.3 IdentityService: Email/username normalization (LowerInvariant)

**Sorun**: [RegisterCommandHandler](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/src/services/identity/IdentityService.Application/Features/Auth/Commands/Register/RegisterCommandHandler.cs#12-96)'da `new User(request.UserName, request.Email, passwordHash)` çağrısında normalizasyon yok. Küçük-büyük harfe duyarlı kayıt olabilir.

### Değiştirilecek Dosyalar

#### [MODIFY] [RegisterCommandHandler.cs](file:///c:/Users/Metin/Desktop/Yazılım%20dosyaları/Projeler/Bitirme_Proje/BitirmeProject/src/services/identity/IdentityService.Application/Features/Auth/Commands/Register/RegisterCommandHandler.cs)
- `request.UserName` → `request.UserName.ToLowerInvariant()`
- `request.Email` → `request.Email.ToLowerInvariant()`

#### [MODIFY] [LoginCommandHandler.cs](file:///c:/Users/Metin/Desktop/Yazılım%20dosyaları/Projeler/Bitirme_Proje/BitirmeProject/src/services/identity/IdentityService.Application/Features/Auth/Commands/Login/LoginCommandHandler.cs)
- `GetByUserNameAsync(request.UserNameOrEmail, ...)` → `request.UserNameOrEmail.ToLowerInvariant()`
- `GetByEmailAsync(request.UserNameOrEmail, ...)` → `request.UserNameOrEmail.ToLowerInvariant()`

---

## 2.4 IdentityService: Refresh token hashing (SHA-256)

**Sorun**: Token düz metin olarak saklanıyor. Veritabanı sızıntısında tüm tokenlar kullanılabilir hale gelir.

**Strateji**:
- Token üretimi değişmez (64-byte random → Base64).
- Veritabanına yalnızca `SHA-256(token)` saklanır.
- Kullanıcıya (cookie'ye) plain token gönderilir.
- Doğrulama: gelen plain token → hash → DB sorgusu.

### Değiştirilecek Dosyalar

#### [MODIFY] [RefreshToken.cs](file:///c:/Users/Metin/Desktop/Yazılım%20dosyaları/Projeler/Bitirme_Proje/BitirmeProject/src/services/identity/IdentityService.Domain/Entities/RefreshToken.cs)
- [Token](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/src/services/identity/IdentityService.Api/Controllers/AuthController.cs#76-85) property'sine doc comment ekle: "Stores SHA-256 hash of the raw token."

#### [MODIFY] [LoginCommandHandler.cs](file:///c:/Users/Metin/Desktop/Yazılım%20dosyaları/Projeler/Bitirme_Proje/BitirmeProject/src/services/identity/IdentityService.Application/Features/Auth/Commands/Login/LoginCommandHandler.cs)
- [GenerateToken()](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/src/services/identity/IdentityService.Application/Features/Auth/Commands/Refresh/RefreshTokenCommandHandler.cs#77-83) → raw token üretir.
- `new RefreshToken(userId, HashToken(rawToken), ...)` → hash'lenmiş değer saklanır.
- Response'a plain raw token yazılır: `RefreshToken = rawToken`.
- `HashToken(string raw)` yardımcı metodu eklenir (SHA-256).

#### [MODIFY] [RefreshTokenCommandHandler.cs](file:///c:/Users/Metin/Desktop/Yazılım%20dosyaları/Projeler/Bitirme_Proje/BitirmeProject/src/services/identity/IdentityService.Application/Features/Auth/Commands/Refresh/RefreshTokenCommandHandler.cs)
- [GetByTokenAsync(request.RefreshToken)](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/src/services/identity/IdentityService.Infrastructure/Repositories/RefreshTokenRepository.cs#17-22) → [GetByTokenAsync(HashToken(request.RefreshToken))](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/src/services/identity/IdentityService.Infrastructure/Repositories/RefreshTokenRepository.cs#17-22).
- Yeni refresh token için aynı hash stratejisi uygulanır.

#### [MODIFY] [RefreshTokenRepository.cs](file:///c:/Users/Metin/Desktop/Yazılım%20dosyaları/Projeler/Bitirme_Proje/BitirmeProject/src/services/identity/IdentityService.Infrastructure/Repositories/RefreshTokenRepository.cs)
- Değişiklik gerekmez; repository hash'lenmiş değeri arar, bu doğru davranış.

---

## 2.5 StorageService: uploadedByUserId claims'ten türet

**Durum**: [StorageController.cs](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/src/services/storage/StorageService.Api/Controllers/StorageController.cs) dosyasında iki ayrı class tanımı mevcut (copy-paste artefakt). İlk class (satır 1-99) doğru implementasyonu içeriyor (`User.GetUserId()` kullanıyor). İkinci class (satır 102-173) eski hatalı versiyondur.

### Değiştirilecek Dosyalar

#### [MODIFY] [StorageController.cs](file:///c:/Users/Metin/Desktop/Yazılım%20dosyaları/Projeler/Bitirme_Proje/BitirmeProject/src/services/storage/StorageService.Api/Controllers/StorageController.cs)
- Satır 102-173 arasındaki duplicate/eski class tanımını tamamen kaldır.

---

## 2.6 StorageService: Download/delete için ownership doğrulama

**Durum**: İlk (doğru) class'ta [Download](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/src/services/storage/StorageService.Api/Controllers/StorageController.cs#65-83) ve [Delete](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/src/services/storage/StorageService.Api/Controllers/StorageController.cs#167-173) endpoint'lerinde ownership ve admin check zaten var.

✅ Değişiklik gerekmez (ilk class'ta zaten uygulanmış).

---

## 2.7 NotificationService: GetByUser için claims-based userId doğrulama

**Sorun**: `GET /api/v1/notifications/user/{userId}` endpoint'inde herhangi bir kullanıcı başka bir kullanıcının bildirimlerine erişebilir.

### Değiştirilecek Dosyalar

#### [MODIFY] [NotificationsController.cs](file:///c:/Users/Metin/Desktop/Yazılım%20dosyaları/Projeler/Bitirme_Proje/BitirmeProject/src/services/notifications/NotificationService.Api/Controllers/NotificationsController.cs)
- [GetByUser(Guid userId)](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/src/services/notifications/NotificationService.Api/Controllers/NotificationsController.cs#31-37) metoduna: claims'ten userId alıp route userId ile karşılaştır. Admin değilse forbidden döndür.
- `Shared.Common.Extensions` kullanarak `User.TryGetUserId()` / `User.HasRole("Admin")`.

---

## 2.8 ProjectService: Route/body OwnerUserId yerine claims

**Sorun**: [CreateProjectCommand(Name, Key, OwnerUserId, CorrelationId)](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/src/services/projects/ProjectService.Application/Features/Projects/Commands/CreateProject/CreateProjectCommand.cs#6-11) kaydında `OwnerUserId` body'den alınıyor. Herhangi biri başkası adına proje oluşturabilir.

### Değiştirilecek Dosyalar

#### [MODIFY] [ProjectsController.cs](file:///c:/Users/Metin/Desktop/Yazılım%20dosyaları/Projeler/Bitirme_Proje/BitirmeProject/src/services/projects/ProjectService.Api/Controllers/ProjectsController.cs)
- [Create](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/src/services/projects/ProjectService.Api/Controllers/ProjectsController.cs#29-35) metodunda `command with { OwnerUserId = User.GetUserId() }` kullanarak body'den gelen OwnerUserId'yi claims'ten alınan değerle override et.

#### [MODIFY] [ProjectsControllerTests.cs](file:///c:/Users/Metin/Desktop/Yazılım%20dosyaları/Projeler/Bitirme_Proje/BitirmeProject/tests/ProjectService.UnitTests/Controllers/ProjectsControllerTests.cs)
- [Create_ReturnsCreatedAtAction_WithResult](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/tests/ProjectService.UnitTests/Controllers/ProjectsControllerTests.cs#15-31) testini güncelleyerek claims'ten userId alınmasını doğrulayan assertion ekle.

---

## Verification Plan

### Automated Tests — Test Komutu
```bash
cd "c:\Users\Metin\Desktop\Yazılım dosyaları\Projeler\Bitirme_Proje\BitirmeProject\tests"
dotnet test --filter "FullyQualifiedName~IdentityService" --verbosity normal
dotnet test --filter "FullyQualifiedName~ProjectService" --verbosity normal
dotnet test --filter "FullyQualifiedName~StorageService" --verbosity normal
dotnet test --filter "FullyQualifiedName~NotificationService" --verbosity normal
```

### Build doğrulama
```bash
cd "c:\Users\Metin\Desktop\Yazılım dosyaları\Projeler\Bitirme_Proje\BitirmeProject"
dotnet build BitirmeProject.sln
```

### Yeni testler
- [LoginCommandHandlerTests](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/tests/IdentityService.UnitTests/Application/Handlers/LoginCommandHandlerTests.cs#12-88): refresh token artık hash'lenmiş değer ile saklanıyor mu? (`refreshRepo.Received(1).AddAsync(Arg.Is<RefreshToken>(t => t.Token != rawToken), ...)`)
- `RefreshTokenCommandHandlerTests`: gelen plain token hash'lenip aranıyor mu?
- [ProjectsControllerTests](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/tests/ProjectService.UnitTests/Controllers/ProjectsControllerTests.cs#13-78): [Create](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/src/services/projects/ProjectService.Api/Controllers/ProjectsController.cs#29-35) artık command'daki OwnerUserId'yi claims'ten alıyor mu?
- `NotificationsControllerTests`: [GetByUser](file:///c:/Users/Metin/Desktop/Yaz%C4%B1l%C4%B1m%20dosyalar%C4%B1/Projeler/Bitirme_Proje/BitirmeProject/src/services/notifications/NotificationService.Api/Controllers/NotificationsController.cs#31-37) başka userId ile istek geldiğinde Forbid döndürüyor mu?

