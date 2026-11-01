# Proje Durumu

## Mevcut aşama
- M8 kodu yazıldı, CI bekleniyor. Sıradaki: `PLAN.md` M9.

## Tamamlananlar
- M0-M7 CI yeşil: liste, kaldırma, kalıntı tarama, yedekli temizlik + geri alma, toplu kaldırma, Mağaza uygulamaları, başlangıç yöneticisi.
- M8: Tarayıcı eklentileri sekmesi: Chrome/Edge/Brave (manifest + _locales ad çözümü) ve Firefox (extensions.json, yalnızca kullanıcı eklentileri) listeleme, arama; tarayıcının eklenti sayfasını/klasörünü açma. Çalışan tarayıcının ayar dosyalarına dokunulmaz.

## Son doğrulama
- M0-M7: CI başarılı. M8: CI bekleniyor.

## Bilinen sorunlar
- Container'da .NET SDK yok; doğrulama yalnızca CI'da. Gerçek Windows davranışı elle denenmedi (SANDBOX'ta deneyin).
- Chromium eklenti açık/kapalı durumu okunmuyor (Durum "-").

## Sonraki aşama
- M9: Arayüz cilası, ayarlar, Inno Setup installer, sürüm etiketi.

## Son güncelleme
- 2026-11-01: M8 uygulandı.
