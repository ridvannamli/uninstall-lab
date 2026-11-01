# Kararlar (tek satır: karar — gerekçe)

- C# + .NET 8 + WPF — Windows API, registry ve kaldırma akışı için en doğal seçenek.
- Core/Platform ayrımı — mantık Linux CI'da ucuz test edilsin, Windows runner dakikası az harcansın.
- Windows CI yalnızca `dev` push'unda — ücretsiz Actions kotası (private repo, $0 bütçe).
- DI çatısı yok — minimum bağımlılık.
- İskelette `.sln` yok; CI proje yollarını doğrudan kullanır — container'da SDK yok, elle sln yazmak hata riski.
- MIT lisansı, telif: Rıdvan Namlı; repo public — gizli bilgi commit edilmez, .env/anahtar dosyaları .gitignore'da.
