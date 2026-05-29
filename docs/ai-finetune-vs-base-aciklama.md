# AI Servisi — Fine-tune Neden Başarısız Oldu, Neden Base Model Kullanıyoruz

## Bağlam

Flowgent AI asistanı yerel Ollama üzerinde çalışıyor. İki model var: base `gemma3:4b` (Google'ın talimat-eğitimli açık modeli) ve `bp-agent` (gemma3:4b üzerine kendi domain'imiz için eğittiğimiz LoRA adaptörü). UI'daki toggle ile geçiş yapılıyor; varsayılan base.

## Fine-tune neden başarısız oldu

v1 adaptöründe iki arıza gözlendi: agent mode'da **format çöküşü** (model `{"tool_calls": ...}` yerine `{"agent_name": "PlayMatch"...}` gibi eğitim setinden rastgele JSON kusuyor) ve scaffold mode'da **içerik halüsinasyonu** (JSON yapısı doğru ama "e-ticaret" istendiğinde "YENİ KARE / B2B süzgeç" dönüyor). Üç kök sebep: (1) `r=8` LoRA rank'i çıktı biçimini öğretmeye yetiyor ama görevi öğretmeye yetmiyor; (2) Colab T4 ücretsiz katmanında eğitim 125 step'te disconnect olup yarıda kaldı; (3) dataset tool-calling örneği içermiyordu. Sonuç teknik adıyla *format'a overfit + task'a underfit* — model JSON nasıl yazılır biliyor ama ne yazacağına karar veremiyor.

## Base model neden iyi çalışıyor

`gemma3:4b` milyonlarca örnekle instruction-tuning'den geçmiş; domain'imizi bilmiyor ama talimat takip etme yeteneği zaten kalibre. Biz de her istekte tam bağlamı (tool catalog + format spec) prompt'a koyup *in-context learning* yapıyoruz. Bedeli ~200-400 ms ek latency; karşılığında fine-tune'un sunamadığı tutarlı çıktıyı alıyoruz. v2 adaptörü (`r=32`, 1417 örnek, tam 3 epoch) düzgün donanımda eğitilince karşılaştırma tekrarlanacak; UI toggle'ı bu hikâyeyi jüriye canlı göstermek için bırakıldı.

## Tek cümlelik özet

> Fine-tune adaptörümüz çıktı *biçimini* öğrendi ama görevi *anlamayı* öğrenemedi (yetersiz rank + yarım eğitim); base model talimat takibi ile geldiği için domain'i prompt üzerinden vermek yetiyor.
