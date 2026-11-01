# Proje Durumu

## Mevcut aşama
- M0-M9 bitti. Kullanıcı isteğiyle ek iş (M10): Yenile + kaldırma sonrası kalıntı penceresi. CI bekleniyor.

## Tamamlananlar
- M0-M9 CI yeşil (liste, kaldırma, kalıntı tarama, yedekli temizlik + geri alma, toplu kaldırma, Mağaza, başlangıç, tarayıcı eklentileri, ayarlar, installer).
- M10: sağ tık "Yenile (F5)" (tüm listeler); kaldırma ve toplu kaldırma sonrası otomatik kalıntı penceresi (klasör/registry yolları, risk, neden, onay kutuları, canlı silme günlüğü); tarayıcıdan InstallLocation, yayıncı\program iç içe klasör/anahtar tarama; AppData/kullanıcı profili korumaları.

## Son doğrulama
- M0-M9: CI başarılı. M10: CI bekleniyor.

## Bilinen sorunlar
- Hiçbir kod Windows'ta elle çalıştırılmadı (yalnızca CI derleme + Core birim testleri). SANDBOX'ta deneyin.
- Tarama yalnızca klasör ve registry ANAHTARI bulur; tek tek registry DEĞERLERİ (dosya ilişkilendirmeleri, App Paths vb.) ve tek dosyalar taranmaz.
- Chromium eklenti açık/kapalı durumu okunmuyor.

## Sonraki adım
- Kullanıcı: yeni installer'ı indir, Sandbox'ta dene, sorunları bildir.

## Son güncelleme
- 2026-11-01: M10 uygulandı.
