Risk Analizi ve Yük Analizi ne işe yarar?

Risk Analizi (✦ Risk Analizi): Aktif sprint'in gecikme riskini tahmin eder. Sprint'teki toplam issue sayısına, tamamlanan issue oranına ve kalan süreye bakarak LLM (Ollama/gemma3) "Low / Medium / High" risk seviyesi verir + neden riskli olduğunu ve ne yapılması gerektiğini açıklar.

Yük Analizi (✦ Yük Analizi): Sprint'teki işlerin dağılımını analiz eder. Hangi issue'ların önceliği değiştirilmeli, hangisi bir sonraki sprint'e atılmalı gibi iş yükü dengeleme önerileri sunar.

İkisi de şu an "Sprint verisi bulunamadı" diyordu çünkü AI servisi sprint verilerini çekemiyordu (401 hatası). Az önce düzelttiğimiz middleware sırası sorunu yüzündeydi. Şimdi çalışmalı.