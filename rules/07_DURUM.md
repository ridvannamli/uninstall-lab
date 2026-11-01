# Proje Durumu

## Mevcut aşama
- M2 kodu yazıldı, CI bekleniyor. Sıradaki: `PLAN.md` M3.

## Tamamlananlar
- M0 iskelet. M1 program listesi (registry, filtre, arama).
- M2: `UninstallCommandBuilder` (tırnaklı/tırnaksız yol, MSI /I→/X, sessiz bayraklar), `UninstallService` (dry-run, çıkış kodları 0/3010/1641), `ProcessRunner`, arayüzde "Kaldır" ve varsayılan açık "Sadece göster" kutusu.

## Son doğrulama
- M0 ve M1: CI başarılı. M2: CI bekleniyor.

## Bilinen sorunlar
- Container'da .NET SDK yok; doğrulama yalnızca CI'da. Gerçek kaldırma ve registry okuması Windows'ta elle denenmedi (sandbox önerilir).

## Sonraki aşama
- M3: Kalıntı tarama (kural tabanlı, risk puanı, korumalı yol listesi, önizleme; silme yok).

## Son güncelleme
- 2026-11-01: M2 uygulandı.
