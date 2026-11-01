namespace UninstallLab.Core;

public enum LeftoverKind { Folder, RegistryKey }

/// <summary>Bulunan bir kalıntı. Risk: 0 (güvenli) - 100 (riskli).</summary>
public sealed record LeftoverItem(LeftoverKind Kind, string Path, int Risk, string Reason)
{
    public bool SafeByDefault => Risk <= 30;
}

/// <summary>Taranacak üst klasörler ve registry kökleri (ör. "HKCU\Software").</summary>
public sealed record ScanLocations(IReadOnlyList<string> FolderRoots, IReadOnlyList<string> RegistryRoots);

public interface IFileSystemProbe
{
    /// <summary>Üst klasörün alt klasörlerinin tam yollarını döndürür (yoksa boş).</summary>
    IEnumerable<string> EnumerateDirectories(string parent);

    bool DirectoryExists(string path);
}

public interface IRegistryProbe
{
    /// <summary>Anahtarın alt anahtarlarının tam yollarını döndürür (yoksa boş).</summary>
    IEnumerable<string> EnumerateSubKeys(string keyPath);
}

/// <summary>Kaldırılmış bir programın olası kalıntılarını bulur. Yalnızca okur, hiçbir şeyi silmez.</summary>
public sealed class LeftoverScanner
{
    private const int MinNameLength = 3;
    private const int RiskInstallFolder = 10;
    private const int RiskNameMatch = 20;
    private const int RiskPublisherMatch = 60;

    private readonly IFileSystemProbe _fs;
    private readonly IRegistryProbe _registry;
    private readonly ScanLocations _locations;

    public LeftoverScanner(IFileSystemProbe fs, IRegistryProbe registry, ScanLocations locations)
    {
        _fs = fs;
        _registry = registry;
        _locations = locations;
    }

    public IReadOnlyList<LeftoverItem> Scan(InstalledProgram program)
    {
        var name = NameNormalizer.Normalize(program.Name);
        var publisher = NameNormalizer.Normalize(program.Publisher);
        var found = new List<LeftoverItem>();

        AddInstallLocation(program, name, publisher, found);

        foreach (var root in _locations.FolderRoots)
            foreach (var dir in _fs.EnumerateDirectories(root))
                Consider(dir, LeftoverKind.Folder, name, publisher, found);

        foreach (var root in _locations.RegistryRoots)
            foreach (var key in _registry.EnumerateSubKeys(root))
                Consider(key, LeftoverKind.RegistryKey, name, publisher, found);

        return found
            .GroupBy(i => (i.Kind, Path: i.Path.ToLowerInvariant()))
            .Select(g => g.OrderBy(i => i.Risk).First())
            .OrderBy(i => i.Risk)
            .ThenBy(i => i.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // Registry'deki InstallLocation: yalnızca bilinen kök klasörlerin ALTINDAYSA ve hâlâ varsa kalıntıdır.
    private void AddInstallLocation(InstalledProgram program, string name, string publisher, List<LeftoverItem> into)
    {
        var path = program.InstallLocation?.Trim().Trim('"').TrimEnd('\\', '/');
        if (string.IsNullOrEmpty(path)) return;
        if (ProtectedPaths.IsProtected(path)) return;
        if (!IsStrictlyUnderRoot(path)) return;
        if (!_fs.DirectoryExists(path)) return;

        var segment = NameNormalizer.Normalize(PathText.LastSegment(path));
        // Yayıncı klasörü başka ürünlerle paylaşılıyor olabilir: güvenli sayılmaz.
        if (segment.Length >= MinNameLength && segment == publisher && segment != name)
            into.Add(new LeftoverItem(LeftoverKind.Folder, path, RiskPublisherMatch,
                "Kurulum klasörü yayıncı klasörü gibi görünüyor (başka ürünlerle paylaşılıyor olabilir)"));
        else
            into.Add(new LeftoverItem(LeftoverKind.Folder, path, RiskInstallFolder,
                "Kurulum klasörü (kaldırma sonrası kalmış)"));
    }

    private bool IsStrictlyUnderRoot(string path) =>
        _locations.FolderRoots.Any(root =>
        {
            var r = root.TrimEnd('\\', '/');
            return r.Length > 0
                && path.Length > r.Length + 1
                && path.StartsWith(r + "\\", StringComparison.OrdinalIgnoreCase);
        });

    private void Consider(string path, LeftoverKind kind, string name, string publisher, List<LeftoverItem> into)
    {
        if (ProtectedPaths.IsProtected(path)) return;

        var segment = NameNormalizer.Normalize(PathText.LastSegment(path));
        if (segment.Length < MinNameLength) return;

        if (segment == name)
        {
            into.Add(new LeftoverItem(kind, path, RiskNameMatch, "Program adıyla eşleşiyor"));
            return;
        }

        if (segment != publisher) return;

        into.Add(new LeftoverItem(kind, path, RiskPublisherMatch,
            "Yayıncı adıyla eşleşiyor (başka ürünlerle paylaşılıyor olabilir)"));

        // Yayıncı\Program iç içe düzeni (ör. ...\Acme\Demo App): iç klasör/anahtar program adıyla eşleşiyorsa
        // yalnızca o güvenli kalıntıdır; yayıncı klasörü riskli kalır.
        var children = kind == LeftoverKind.Folder
            ? _fs.EnumerateDirectories(path)
            : _registry.EnumerateSubKeys(path);

        foreach (var child in children)
        {
            if (ProtectedPaths.IsProtected(child)) continue;
            var childSegment = NameNormalizer.Normalize(PathText.LastSegment(child));
            if (childSegment.Length >= MinNameLength && childSegment == name)
                into.Add(new LeftoverItem(kind, child, RiskNameMatch,
                    "Yayıncı klasörü/anahtarı içinde program adıyla eşleşiyor"));
        }
    }
}
