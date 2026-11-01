namespace UninstallLab.Core;

/// <summary>Bir programın tarama sonucu ve (liste yenilendikten sonra) hâlâ yüklü görünüp görünmediği.</summary>
public sealed record ProgramScan(InstalledProgram Program, IReadOnlyList<LeftoverItem> Items, bool StillInstalled);

/// <summary>Tarama sonuçlarını birleştirir ve otomatik temizlenecek güvenli kümeyi belirler.</summary>
public static class LeftoverPlanner
{
    /// <summary>Tüm programların kalıntıları: tekrarsız, düşük riskten yükseğe sıralı.</summary>
    public static IReadOnlyList<LeftoverItem> Combine(IEnumerable<ProgramScan> scans) =>
        DistinctSorted(scans.SelectMany(s => s.Items));

    /// <summary>
    /// Otomatik temizlenecek öğeler: yalnızca kaldırılmış (artık listede olmayan) programlara ait,
    /// düşük riskli ve hâlâ yüklü bir programın da kalıntısı olarak görünmeyen öğeler.
    /// </summary>
    public static IReadOnlyList<LeftoverItem> AutoCleanSet(IEnumerable<ProgramScan> scans)
    {
        var list = scans.ToList();

        var shared = list
            .Where(s => s.StillInstalled)
            .SelectMany(s => s.Items)
            .Select(Key)
            .ToHashSet();

        return DistinctSorted(list.Where(s => !s.StillInstalled).SelectMany(s => s.Items))
            .Where(i => i.SafeByDefault && !shared.Contains(Key(i)))
            .ToList();
    }

    private static (LeftoverKind Kind, string Path) Key(LeftoverItem item) =>
        (item.Kind, item.Path.ToLowerInvariant());

    private static IReadOnlyList<LeftoverItem> DistinctSorted(IEnumerable<LeftoverItem> items) => items
        .GroupBy(Key)
        .Select(g => g.OrderBy(i => i.Risk).First())
        .OrderBy(i => i.Risk)
        .ThenBy(i => i.Path, StringComparer.OrdinalIgnoreCase)
        .ToList();
}
