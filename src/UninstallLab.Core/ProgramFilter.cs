namespace UninstallLab.Core;

/// <summary>Program listesini süzer, tekrarları kaldırır ve ada göre sıralar.</summary>
public static class ProgramFilter
{
    public static IReadOnlyList<InstalledProgram> Apply(
        IEnumerable<InstalledProgram> programs,
        string? query = null,
        bool onlyUninstallable = false)
    {
        var q = query?.Trim();

        return programs
            .Where(p => !onlyUninstallable || p.CanUninstall)
            .Where(p => string.IsNullOrEmpty(q)
                || p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (p.Publisher?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
            .GroupBy(p => (Name: p.Name.ToLowerInvariant(), p.Version))
            .Select(g => g.First())
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
