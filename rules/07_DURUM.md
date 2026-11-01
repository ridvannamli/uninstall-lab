# Proje Durumu

## Mevcut aşama
- M3 kodu yazıldı, CI bekleniyor. Sıradaki: `PLAN.md` M4.

## Tamamlananlar
- M0 iskelet, M1 program listesi, M2 standart kaldırma (dry-run varsayılan, CI yeşil).
- M3: `LeftoverScanner` (klasör + registry, ad/yayıncı eşleşmesi, risk 20/60), `ProtectedPaths` (sistem yolları/anahtarları korumalı), `NameNormalizer`, Windows probe'ları, arayüzde "Kalıntıları tara" (yalnızca rapor, silme yok).

## Son doğrulama
- M0-M2: CI başarılı. M3: CI bekleniyor.

## Bilinen sorunlar
- Container'da .NET SDK yok; doğrulama yalnızca CI'da. Gerçek registry/dosya taraması Windows'ta elle denenmedi (sandbox önerilir).

## Sonraki aşama
- M4: Güvenli temizlik (.reg yedeği, silme yürütücüsü, geri alma, geri yükleme noktası). Silme yalnızca yedek + onay + dry-run varsayılanıyla.

## Son güncelleme
- 2026-11-01: M3 uygulandı.
