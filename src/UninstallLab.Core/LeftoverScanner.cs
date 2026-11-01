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

        foreach (var root in _locations.FolderRoots)
            foreach (var dir in _fs.EnumerateDirectories(root))
                Consider(dir, LeftoverKind.Folder, name, publisher, found);

        foreach (var root in _locations.RegistryRoots)
            foreach (var key in _registry.EnumerateSubKeys(root))
                Consider(key, LeftoverKind.RegistryKey, name, publisher, found);

        return found
            .OrderBy(i => i.Risk)
            .ThenBy(i => i.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void Consider(string path, LeftoverKind kind, string name, string publisher, List<LeftoverItem> into)
    {
        if (ProtectedPaths.IsProtected(path)) return;

        var segment = NameNormalizer.Normalize(PathText.LastSegment(path));
        if (segment.Length < MinNameLength) return;

        if (segment == name)
            into.Add(new LeftoverItem(kind, path, RiskNameMatch, "Program adıyla eşleşiyor"));
        else if (segment == publisher)
            into.Add(new LeftoverItem(kind, path, RiskPublisherMatch,
                "Yayıncı adıyla eşleşiyor (başka ürünlerle paylaşılıyor olabilir)"));
    }
}
