# Proje Durumu

## Mevcut aşama
- M6 kodu yazıldı, CI bekleniyor. Sıradaki: `PLAN.md` M7.

## Tamamlananlar
- M0-M5 CI yeşil: liste, kaldırma, kalıntı tarama, yedekli temizlik + geri alma, toplu kaldırma.
- M6: Mağaza uygulamaları sekmesi: `StoreAppParser` (PowerShell JSON), `StoreAppFilter` (çatı/sistem paketleri gizli), `StoreAppValidator` (paket adı doğrulama, enjeksiyon koruması), `StoreAppService` (dry-run), `PowerShellStoreAppSource/Remover`.

## Son doğrulama
- M0-M5: CI başarılı. M6: CI bekleniyor.

## Bilinen sorunlar
- Container'da .NET SDK yok; doğrulama yalnızca CI'da. Gerçek kaldırma/temizleme/Get-AppxPackage Windows'ta elle denenmedi (SANDBOX'ta deneyin).

## Sonraki aşama
- M7: Başlangıç programları yöneticisi.

## Son güncelleme
- 2026-11-01: M6 uygulandı.
