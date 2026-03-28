# Frontend Geliştirme Planı

**Tarih:** 2026-03-27
**Durum:** Backend tamamlandı (210/210 test geçiyor, 0 build hatası). Frontend düzenleme aşaması başlıyor.

---

## Mevcut Durum

- React 18 + TypeScript + Vite + Zustand + React Router v6
- `@dnd-kit` ile drag-drop Kanban board
- `VITE_USE_MOCK_API=true` ile çalışan 912 satırlık mock API (localStorage-backed)
- SignalR entegrasyonu mevcut ama token sorunu nedeniyle çalışmıyor (HttpOnly cookie vs localStorage çakışması)
- Temel sayfalar: Login, Register, Dashboard, ProjectDetailPage (kısmi), IssueDetailPanel, SprintBoard, NotificationsPage

---

## Faz F1 — Backend Bağlantısı ve Tip Hizalaması

**Hedef:** `VITE_USE_MOCK_API=false` yapıldığında uygulama sorunsuz çalışmalı.

### Görevler

1. **Axios interceptor** — 401 yanıtında `/auth/refresh` ile token yenileme, başarısız olursa logout
2. **API base URL** — Tüm servis URL'leri `appsettings` ile eşleştirilmeli (Gateway üzerinden mi, doğrudan mı?)
3. **TypeScript tip hizalaması** — `src/types/index.ts` içindeki tipler backend DTO'larıyla karşılaştırılmalı:
   - `IssueDto`, `SprintDto`, `ProjectDto`, `NotificationDto`, `UserDto`, `StoredFileDto`
   - Eksik/fazla alanlar düzeltilmeli
4. **SignalR token sorunu** — HttpOnly cookie kullanıldığı için `accessTokenFactory` localStorage'dan okuyor ama token orada yok. Çözüm seçenekleri:
   - SignalR hub'ı query string token ile desteklemek (backend `OnMessageReceived` kancası)
   - Veya SignalR yerine polling fallback
5. **Error boundary** — Global hata yakalama componenti ekle
6. **Mock API geçiş testi** — Her API çağrısının gerçek endpoint'e gittiğini kontrol et

---

## Faz F2 — Eksik Temel Özellikler

**Hedef:** Kullanıcının yapabileceği temel işlemler tam olmalı.

### Görevler

1. **Issue düzenleme** — `IssueDetailPanel` içinde başlık, açıklama, öncelik, durum düzenleme (şu an read-only görünüyor)
2. **File upload UI** — StorageService entegrasyonu: dosya seçme, yükleme progress bar, ekli dosyaları listeleme/indirme
3. **Sprint yönetimi** — Sprint oluşturma, başlatma, tamamlama butonları + backlog'dan sprint'e issue taşıma
4. **ProjectDetailPage tamamlama** — Üyeler listesi, sprint listesi, proje ayarları
5. **Kullanıcı yönetimi (Admin)** — Kullanıcı listeleme, rol atama (IdentityService `/users` endpoint'i mevcut)
6. **Yorum ekleme** — Issue yorumları için `AddComment` UI

---

## Faz F3 — UX ve Hata Yönetimi

**Hedef:** Kullanıcı deneyimi production kalitesinde olmalı.

### Görevler

1. **Loading skeleton** — API çağrıları süresince içerik yerine skeleton göster (Kanban board, issue listesi, sprint listesi)
2. **Toast bildirimleri** — Başarı/hata işlemlerinde toast mesajları (react-hot-toast veya benzeri)
3. **Form validasyon mesajları** — Backend'den gelen FluentValidation hatalarını form alanlarının altında göster
4. **Pagination** — Issue listesi, notification listesi için sayfalama
5. **Notification deep-link** — Bildirime tıklandığında ilgili issue sayfasına yönlendirme
6. **Boş durum ekranları** — Sprint içinde issue yoksa, proje yoksa vb. için boş durum componentleri
7. **Confirm dialog** — Silme işlemleri için onay kutusu

---

## Faz F4 — Real-time ve Polish

**Hedef:** Gerçek zamanlı özellikler ve görsel iyileştirmeler.

### Görevler

1. **SignalR board güncellemeleri** — Başka kullanıcı issue taşıdığında board otomatik güncellenmeli
2. **SignalR bildirim sayacı** — Yeni bildirim geldiğinde header'daki zil ikonu güncellenmeli
3. **Dark mode** — CSS custom properties ile light/dark tema geçişi
4. **Mobile responsiveness** — Kanban board mobile görünümde column'lar yatay scroll ile
5. **Keyboard shortcuts** — Issue oluşturma (N), arama (/) gibi kısayollar
6. **Accessibility** — aria-label, focus management, renk kontrastı

---

## Öncelik Sırası

```
F1 (Backend bağlantısı) → F2 (Temel özellikler) → F3 (UX) → F4 (Real-time/Polish)
```

F1 tamamlanmadan F2-F4 gerçek verilerle test edilemez. F1'e öncelik ver.

---

## Teknik Notlar

- Mock API toggle: `src/api/mock.ts` içindeki `VITE_USE_MOCK_API` env değişkeni
- Zustand store'lar: `src/store/` altında servis bazlı ayrılmış
- API client: `src/api/client.ts` — Axios instance burada tanımlı
- SignalR hub: `src/api/signalr.ts` — `accessTokenFactory` düzeltilmeli
- Route yapısı: `src/App.tsx` içinde React Router v6 ile tanımlı
