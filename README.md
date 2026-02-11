# BitirmeProject\r\n## Hızlı Test Akışı (Gateway)

Ön koşul:
- `docker compose up --build`
- Gateway: `http://localhost:5000`

Test dosyası:
- `docs/requests.http`

Adımlar:
1. Register isteğini gönder.
2. Login isteğini gönder, dönen `accessToken` ve `user.id` değerlerini kopyala.
3. `{{token}}` ve `{{userId}}` değişkenlerini doldur.
4. Create Project isteğini gönder, dönen `id` değerini `{{projectId}}` olarak koy.
5. Create Issue isteğini gönder, dönen `id` değerini `{{issueId}}` olarak koy.
6. Assign Issue isteğini gönder.
7. Change Issue Status isteğini gönder.

Notlar:
- `priority`: 0=Low,1=Medium,2=High,3=Critical
- `newStatus`: 0=Open,1=InProgress,2=Done
- `correlationId` zorunlu değil; istersen boş bırakabilirsin.