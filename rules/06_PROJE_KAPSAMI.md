# Proje Kapsamı

## Teknoloji ve çalışma ortamı
- Dil/çatı: C# 12, .NET 8; arayüz WPF (`net8.0-windows`); çekirdek `net8.0` (platformdan bağımsız).
- Paket yöneticisi: NuGet. Test: xUnit.
- Veri deposu/dış servis: Yok. Ayarlar yerel JSON. Telemetri yok.
- Çalıştırma: `dotnet run --project src/UninstallLab.App` (yalnızca Windows).
- Doğrulama: `dotnet test tests/UninstallLab.Core.Tests` (Linux/Windows), CI: `.github/workflows/ci.yml`.

## Hedef
- Mevcut hedef: Yüklü programları listeleyen, kaldıran ve kalıntıları güvenle temizleyen Windows uygulaması.
- Bu aşamanın hedefi: `PLAN.md`'deki ilk tamamlanmamış aşama.

## Mimari sınırlar
- `UninstallLab.Core`: saf mantık, Windows API yok. Registry/dosya/süreç erişimi arayüzlerle soyutlanır.
- `UninstallLab.Platform.Windows` (M1'de eklenir): arayüzlerin Windows gerçeklemeleri.
- `UninstallLab.App`: WPF, MVVM, iş mantığı yok.
- Eklenmeyecek: ağır UI kütüphanesi, veritabanı, ağ erişimi, DI çatısı (elle kompozisyon yeterli).
- Arayüz dili: Türkçe. Kod ve tanımlayıcılar İngilizce.

## Güvenlik kuralları
- Silme/değiştirme öncesi önizleme ve onay; varsayılan `dry-run`.
- Registry silmeden önce `.reg` yedeği; geri alma desteği.
- Korumalı yollar listesi (Windows, Program Files kökü, sistem anahtarları) asla silinmez.

## Kapsam dışı
- Reklam, telemetri, otomatik güncelleme sunucusu, "sistem hızlandırıcı" iddiaları.
- Üçüncü taraf ürün kodu/adı/arayüzü kopyalama.
