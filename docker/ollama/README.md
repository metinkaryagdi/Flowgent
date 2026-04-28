# Ollama özel model dağıtımı

Bu klasör Faz 4 (deploy) için Ollama Modelfile ve adapter dosyalarını barındırır.

## Şu anki durum

- `Modelfile` — base + `ADAPTER` placeholder (yorumlu).
- Adapter `.gguf` dosyası **henüz üretilmedi**. Faz 2 Colab eğitiminden sonra
  buraya kopyalanacak: `gemma3-4b-bp-v1-q4_k_m.gguf`.

## Üretim adımları (adapter hazır olduğunda)

```bash
# 1) Drive'dan adapter'ı çek (lokalde)
cp ~/Downloads/gemma3-4b-bp-v1-q4_k_m.gguf docker/ollama/

# 2) Modelfile'da ADAPTER satırının yorumunu aç (head ile kontrol)
sed -i 's|# ADAPTER|ADAPTER|' docker/ollama/Modelfile

# 3) Ollama container'a volume mount + custom model create
#    docker-compose'a aşağıdaki ekleme yapılır:
#
#    services:
#      ollama:
#        volumes:
#          - ./docker/ollama:/modelfiles:ro
#        # init container veya entrypoint:
#        command: >
#          sh -c "ollama serve & sleep 3 &&
#                 ollama create bp-agent -f /modelfiles/Modelfile && wait"
#
# 4) Feature flag açma:
#    appsettings.Production.json → Ollama:UseFinetuned: true
#    docker-compose env → Ollama__UseFinetuned: "true"
```

## Rollback planı

`Ollama:UseFinetuned: false` → AiService anında `gemma3:4b` base modeline döner.
Adapter dosyası silinmesi gerekmez; tek deploy ile rollback.

## Shadow / kademeli geçiş

Plan dökümanında (Faz 4) önerilen sıra:
1. **Shadow mode:** Hem base hem fine-tuned çağrılır, base kullanıcıya gider, fine-tuned loglanır.
2. **%10 → %50 → %100:** Trafik kademeli yönlendirme (örn. user.Id hash mod 10).
3. **Tam geçiş:** Flag `true`, base sadece fallback.

Mevcut iskelet sadece on/off; kademeli rollout ileride controller seviyesinde
eklenebilir.
