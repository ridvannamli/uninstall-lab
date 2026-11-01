# PLAN (tur başına ilk `[ ]`)

- [x] M0 İskelet: Core, App, Tests, CI
- [x] M1 Yüklü programları listele: `IProgramSource` + registry gerçeklemesi (Platform.Windows), 32/64 bit + kullanıcı bazlı; Core'da ayrıştırma/süzme testleri
- [x] M2 Standart kaldırma: `UninstallString`/`QuietUninstallString` ayrıştırma, MSI (`msiexec /x`) desteği, süreç çalıştırma soyutlaması, dry-run
- [x] M3 Kalıntı tarama: kural tabanlı (klasör, registry), risk puanı, korumalı yol listesi, önizleme raporu (silmeden)
- [x] M4 Güvenli temizlik: `.reg` yedeği, silme yürütücüsü, geri alma, geri yükleme noktası denemesi
- [x] M5 Toplu kaldırma
- [x] M6 Windows Store (UWP) uygulamaları (PowerShell `Get-AppxPackage`)
- [ ] M7 Başlangıç programları yöneticisi
- [ ] M8 Tarayıcı eklentileri yöneticisi
- [ ] M9 Arayüz cilası, ayarlar, Inno Setup installer, sürüm etiketi
