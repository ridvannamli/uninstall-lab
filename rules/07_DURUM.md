# Proje Durumu

## Mevcut aşama
- M9 tamamlandı; CI ve release iş akışı (installer) doğrulandı. PLAN.md tamamlandı (M0-M9).

## Tamamlananlar
- M0-M8 CI yeşil: liste, kaldırma, kalıntı tarama, yedekli temizlik + geri alma, toplu kaldırma, Mağaza uygulamaları, başlangıç yöneticisi, tarayıcı eklentileri.
- M9: `AppSettings` (QuietByDefault, ShowSystemStoreApps; dry-run kalıcı kapatılamaz), Hakkında, app.manifest (highestAvailable), sürüm 0.1.0, exe adı UninstallLab.exe, Inno Setup betiği, `release.yml` (installer + etiketle taslak release), README.

## Son doğrulama
- M0-M9: CI başarılı; release.yml installer üretti.

## Bilinen sorunlar
- Hiçbir kod Windows'ta elle çalıştırılmadı; yalnızca CI derleme + Core birim testleri doğrulandı. SANDBOX'ta deneyin.
- Chromium eklenti açık/kapalı durumu okunmuyor.
- GitHub release için etiket (örn. v0.1.0) elle itilmeli; yalnızca taslak ön sürüm oluşur.

## Sonraki adım
- Kullanıcı: sandbox testi (artifact'ı indir, dry-run ile dene), sorunları bildir.
- Plan bitti: zamanlanmış görev kapatılabilir.

## Son güncelleme
- 2026-11-01: M9 uygulandı.
