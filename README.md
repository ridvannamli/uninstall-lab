# Uninstall Lab

Windows için açık kaynaklı program kaldırıcı ve kalıntı temizleyici. Özgün bir projedir; başka bir ürünün kodu, adı veya arayüzü kullanılmamıştır.

> **Uyarı:** Bu yazılım kaldırma ve silme işlemleri yapar. Önce **Windows Sandbox veya sanal makinede** deneyin. "Sadece göster" kutusu her açılışta işaretli başlar. Yazılım "olduğu gibi" sunulur (MIT).

## Özellikler

- **Programlar:** Yüklü programları listeler (32/64 bit, kullanıcı bazlı), arar, standart kaldırır (MSI dahil), toplu kaldırır.
- **Kalıntı temizliği:** Kaldırma sonrası artık klasör ve registry anahtarlarını bulur. Yalnızca düşük riskli öğeler işlenir, sistem yolları korumalıdır. Klasörler yedeğe taşınır, registry anahtarları `.reg` olarak yedeklenir. **Son temizlik geri alınabilir**; temizlik öncesi geri yükleme noktası denenir.
- **Mağaza uygulamaları:** UWP/MSIX paketlerini listeler ve kaldırır (sistem/çatı paketleri reddedilir).
- **Başlangıç programları:** Run anahtarları ve Başlangıç klasörleri. Açma/kapama Görev Yöneticisi ile aynı yolla yapılır, girdi silinmez.
- **Tarayıcı eklentileri:** Chrome, Edge, Brave ve Firefox eklentilerini listeler; tarayıcının kendi yönetim sayfasını açar.

## Güvenlik tasarımı

- Varsayılan mod önizlemedir (dry-run).
- Korumalı yollar (Windows, Program Files kökü, `Microsoft` anahtarları vb.) asla silinmez.
- Registry yedeği alınmadan anahtar silinmez.
- Komut enjeksiyonuna karşı Mağaza paket adları doğrulanır.

## Geliştirme

Gereksinim: .NET 8 SDK.

```bash
dotnet test tests/UninstallLab.Core.Tests
```

Arayüz yalnızca Windows'ta çalışır:

```bash
dotnet run --project src/UninstallLab.App
```

Yapı: `UninstallLab.Core` (platformdan bağımsız mantık, Linux'ta test edilir), `UninstallLab.Platform.Windows` (registry, dosya, PowerShell), `UninstallLab.App` (WPF). CI: `.github/workflows/ci.yml`; installer: `installer/` ve `.github/workflows/release.yml`.

## Lisans

[MIT](LICENSE) - Copyright (c) 2026 Rıdvan Namlı
