# Kararlar (tek satır: karar — gerekçe)

- C# + .NET 8 + WPF — Windows API, registry ve kaldırma akışı için en doğal seçenek.
- Core/Platform ayrımı — mantık Linux CI'da ucuz test edilsin, Windows runner dakikası az harcansın.
- Windows CI yalnızca `dev` push'unda — ücretsiz Actions kotası (private repo, $0 bütçe).
- DI çatısı yok — minimum bağımlılık.
- İskelette `.sln` yok; CI proje yollarını doğrudan kullanır — container'da SDK yok, elle sln yazmak hata riski.
- MIT lisansı, telif: Rıdvan Namlı; repo public — gizli bilgi commit edilmez, .env/anahtar dosyaları .gitignore'da.
- Klasör temizliği silme değil yedek konuma taşıma; registry .reg dışa aktarılıp silinir; geri alma manifest ile — her şey geri alınabilir. Farklı sürücüde kopyala+sil kullanılır (silme yarıda kalırsa kopya yedekte durur).
- Dry-run ("Sadece göster") her açılışta açık, ayarlarda kalıcı kapatılamaz — kazara gerçek silme riskini azaltmak için.
- app.manifest `highestAvailable` — yönetici hesabında HKLM işlemleri için yükseltme, standart kullanıcıda normal çalışma.
- Release iş akışı etiket itilince yalnızca TASLAK + ön sürüm oluşturur; gerçek bilgisayar/sandbox denemesi öncesi otomatik yayın yok.
- Installer: Inno Setup, self-contained tek dosya publish (kullanıcıda .NET gerekmez); .iss yalnızca ASCII.
- WPF projelerinde `System.IO` örtük using değildir; App kodunda açıkça eklenir/tam ad kullanılır.
- Kaldırma sonrası kalıntı penceresi: yollar listelenir, düşük riskli olanlar önceden seçili, silme adımları canlı günlükte; liste yenilenince hâlâ yüklü görünen programa uyarı. Dry-run'da otomatik tarama yok (program hâlâ yüklü).
- InstallLocation yalnızca bilinen kök klasörlerin (Program Files, ProgramData, AppData...) ALTINDAYSA, korumalı değilse ve hâlâ varsa kalıntı sayılır (AppData\Local gibi üst klasörlerin taşınmasını engellemek için).
- Sağ tık "Yenile" ve F5: etkin sekmeyi yeniden okur.
- Kaldırma sonrası düşük riskli kalıntılar OTOMATİK (yedekli) temizlenir (ayar: AutoCleanLeftovers, varsayılan açık); yalnızca artık listede olmayan programlar için, gerçek modda. Hâlâ yüklü görünen programa ve onunla ortak yollara asla otomatik dokunulmaz (LeftoverPlanner). Riskli öğeler listede kalır, elle seçilir.
- Uygulama simgesi: kullanıcı görselinden kırpılıp üretilen çok boyutlu .ico (16-256 px); exe, pencereler ve installer'da kullanılır.
