# Proje Durumu

## Mevcut aşama
- M7 kodu yazıldı, CI bekleniyor. Sıradaki: `PLAN.md` M8.

## Tamamlananlar
- M0-M6 CI yeşil: liste, kaldırma, kalıntı tarama, yedekli temizlik + geri alma, toplu kaldırma, Mağaza uygulamaları.
- M7: Başlangıç sekmesi: Run anahtarları (HKCU, HKLM, WOW6432) ve Başlangıç klasörleri; açma/kapama StartupApproved ile (girdi silinmez, geri alınabilir); `StartupApproval`, `StartupFilter`, `StartupManager`, `RegistryStartupStore`.

## Son doğrulama
- M0-M6: CI başarılı. M7: CI bekleniyor.

## Bilinen sorunlar
- Container'da .NET SDK yok; doğrulama yalnızca CI'da. Gerçek Windows davranışı (kaldırma, temizlik, Get-AppxPackage, StartupApproved) elle denenmedi (SANDBOX'ta deneyin).

## Sonraki aşama
- M8: Tarayıcı eklentileri yöneticisi.

## Son güncelleme
- 2026-11-01: M7 uygulandı.
