# Proje Durumu

## Mevcut aşama
- M5 kodu yazıldı, CI bekleniyor. Sıradaki: `PLAN.md` M6.

## Tamamlananlar
- M0-M4 CI yeşil: liste, standart kaldırma, kalıntı tarama, yedekli temizlik + geri alma.
- M5: `BatchUninstaller` (sıralı, hata sonrakini durdurmaz, iptal, ilerleme), arayüzde çoklu seçim + "Seçilenleri toplu kaldır" + "Sessiz kaldır".

## Son doğrulama
- M0-M4: CI başarılı. M5: CI bekleniyor.

## Bilinen sorunlar
- Container'da .NET SDK yok; doğrulama yalnızca CI'da. Gerçek kaldırma/temizleme/geri alma Windows'ta elle denenmedi (SANDBOX'ta deneyin).

## Sonraki aşama
- M6: Windows Store (UWP) uygulamaları.

## Son güncelleme
- 2026-11-01: M5 uygulandı.
