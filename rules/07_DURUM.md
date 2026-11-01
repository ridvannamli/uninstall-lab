# Proje Durumu

## Mevcut aşama
- M1 tamamlandı ve CI ile doğrulandı. Sıradaki: `PLAN.md` M2.

## Tamamlananlar
- M0 iskelet; M1: `IProgramSource`, `UninstallEntryParser`, `ProgramFilter` (Core, testli), `RegistryProgramSource` (Platform.Windows, salt okunur), WPF liste + arama.

## Son doğrulama
- M0 ve M1: CI başarılı (Linux test + Windows build).

## Bilinen sorunlar
- Container'da .NET SDK yok; doğrulama yalnızca CI'da. Gerçek registry okuması Windows'ta elle denenmedi.

## Sonraki aşama
- M2: Standart kaldırma (UninstallString ayrıştırma, MSI, süreç soyutlaması, dry-run).

## Son güncelleme
- 2026-11-01: M1 uygulandı.
