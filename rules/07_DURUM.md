# Proje Durumu

## Mevcut aşama
- M4 kodu yazıldı, CI bekleniyor. Sıradaki: `PLAN.md` M5.

## Tamamlananlar
- M0-M3 (iskelet, liste, standart kaldırma, kalıntı tarama) CI yeşil.
- M4: `CleanupExecutor` (dry-run varsayılan, yalnızca düşük risk, korumalı yollar atlanır, klasör→yedek konuma taşı, registry→.reg yedek sonra sil, manifest, `Undo`, geri yükleme noktası denemesi), Windows gerçeklemesi (`CleanupSystem`, `RestorePointCreator`), arayüzde "Kalıntıları temizle" ve "Son temizliği geri al".

## Son doğrulama
- M0-M3: CI başarılı. M4: CI bekleniyor.

## Bilinen sorunlar
- Container'da .NET SDK yok; doğrulama yalnızca CI'da. Gerçek taşıma/registry silme/geri alma Windows'ta elle denenmedi (SANDBOX'ta deneyin, gerçek bilgisayarda değil).

## Sonraki aşama
- M5: Toplu kaldırma.

## Son güncelleme
- 2026-11-01: M4 uygulandı.
