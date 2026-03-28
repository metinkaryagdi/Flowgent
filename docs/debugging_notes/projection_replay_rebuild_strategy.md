# Projection Replay / Rebuild Strategy

## Scope

Bu belge read-side projection bozuldugunda veya yeni projection eklendiginde yeniden kurulum akisini tanimlar.
Amaç write model'i bozmadan projection'lari kontrollu sekilde tekrar insa etmektir.

Kapsamli projection ornekleri:

- `ProjectSummary`
- `SprintIssue`
- `IssueBoardItem`
- notification delivery/read read-model turevleri

## Kaynak Gercekler

Projection replay icin temel veri kaynaklari:

- outbox veya event archive uzerindeki integration event kayitlari
- mevcut write model snapshot'lari
- servis-ici audit tabloları veya immutable tarihce kayitlari

Kural:

- Event varsa event stream kaynak kabul edilir.
- Event yoksa projection yeniden insasi write model snapshot'indan yapilir.
- Aynı projection icin iki farkli kaynak ayni rebuild isinde karistirilmaz.

## Replay Modlari

### 1. Full Rebuild

Kullanım:

- projection tablosu bozulduysa
- yeni index/sekil degisimi varsa
- ordering veya duplicate bug'i olduktan sonra temiz reset gerekiyorsa

Akis:

1. Projection tablo veya koleksiyonu maintenance moda alinır.
2. Hedef projection truncate edilir veya yeni shadow table olusturulur.
3. Kaynak eventler `OccurredOn`, sonra `EventId` sirasi ile yeniden uygulanir.
4. Replay tamamlaninca checksum veya row-count dogrulamasi yapilir.
5. Shadow table kullanildiysa atomik cutover yapilir.

### 2. Incremental Catch-up

Kullanım:

- projection bir noktaya kadar saglikli ama geri kalmis
- consumer downtime sonrasi gap kapatilacak

Akis:

1. Projection icin son uygulanan watermark okunur.
2. Sadece watermark sonrasi eventler replay edilir.
3. Duplicate korumasi icin `(EventId, ConsumerName)` benzeri inbox kaydi korunur.

## Ordering ve Idempotency

Replay sirasinda su kurallar zorunludur:

- Aynı aggregate veya entity icin olaylar olusma sirasinda uygulanir.
- `OccurredOn` esit ise `EventId` deterministic tie-breaker olarak kullanilir.
- Projection handler'lari idempotent olmalidir.
- Replay pipeline canli consumer ile ayni inbox/processed-event kurallarini kullanmalidir.

## Operasyon Modeli

Onerilen calisma bicimi:

1. Replay komutu veya admin endpoint'i projection adini alir.
2. Sistem ilgili projection icin lock olusturur.
3. Replay batch'ler halinde calisir.
4. Her batch sonunda ilerleme metriği ve watermark yazilir.
5. Hata olursa replay durur ve son watermark'tan tekrar baslatilabilir.

## Shadow Table Stratejisi

Buyuk projection'lar icin dogrudan truncate yerine shadow table tercih edilir:

- `ProjectSummary_Rebuild`
- `IssueBoardItem_Rebuild`

Avantaj:

- canli okuma trafigi kesilmez
- yarim rebuild kullaniciya yansimaz
- cutover tek DDL/rename adimina indirgenir

## Dogrulama

Replay sonrasi en az su kontroller yapilir:

- beklenen satir sayisi
- aggregate bazli checksum veya count
- son watermark ile kaynak event stream sonu esitligi
- spot-check API sorgulari

## Riskler

- Gecmis event payload'lari yeni contract ile birebir uyumlu olmayabilir.
- Replay sirasinda eski bug'li eventler ayni hatali state'i tekrar uretebilir.
- Buyuk projection rebuild isleri uzun surebilir; shadow table ve batch checkpoint bu riski azaltir.

## Faz 6 Sonrasi Uygulama Notu

Bu repo icin pratik yol:

- ilk adimda projection rebuild komutunu dokuman seviyesinde standardize et
- ikinci adimda `ProjectSummary` ve `SprintIssue` icin batch replay runner ekle
- `IssueBoardItem` projection'a tamamen tasindiginda ayni runner'a dahil et
