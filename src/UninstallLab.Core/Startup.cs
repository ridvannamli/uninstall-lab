namespace UninstallLab.Core;

public enum StartupSource
{
    CurrentUserRun,
    LocalMachineRun,
    LocalMachineRun32,
    CurrentUserFolder,
    CommonFolder,
}

public sealed record StartupEntry(string Name, string Command, StartupSource Source, bool Enabled);

public sealed record StartupToggleResult(bool Success, string? Error);

public interface IStartupStore
{
    IReadOnlyList<StartupEntry> GetEntries();
    void SetEnabled(StartupEntry entry, bool enabled);
}

/// <summary>
/// Windows'un StartupApproved ikili değeri: ilk bayt çift ise açık, tek ise kapalı.
/// Kapalıyken baytlar 4-11 devre dışı bırakma zamanını (FILETIME) tutar. Girdi silinmez, her zaman geri açılabilir.
/// </summary>
public static class StartupApproval
{
    public static bool IsEnabled(byte[]? data) =>
        data is null || data.Length == 0 || (data[0] & 1) == 0;

    public static byte[] Encode(bool enabled, DateTime utcNow)
    {
        var bytes = new byte[12];
        if (enabled)
        {
            bytes[0] = 0x02;
            return bytes;
        }

        bytes[0] = 0x03;
        BitConverter.GetBytes(utcNow.ToFileTimeUtc()).CopyTo(bytes, 4);
        return bytes;
    }
}

public static class StartupFilter
{
    public static IReadOnlyList<StartupEntry> Apply(IEnumerable<StartupEntry> entries, string? query = null)
    {
        var q = query?.Trim();

        return entries
            .Where(e => string.IsNullOrEmpty(q)
                || e.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || e.Command.Contains(q, StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.Source)
            .ToList();
    }
}

public sealed class StartupManager
{
    private readonly IStartupStore _store;

    public StartupManager(IStartupStore store) => _store = store;

    public IReadOnlyList<StartupEntry> List(string? query = null) =>
        StartupFilter.Apply(_store.GetEntries(), query);

    public StartupToggleResult SetEnabled(StartupEntry entry, bool enabled)
    {
        if (entry.Enabled == enabled) return new StartupToggleResult(true, null);

        try
        {
            _store.SetEnabled(entry, enabled);
            return new StartupToggleResult(true, null);
        }
        catch (Exception ex)
        {
            var systemWide = entry.Source is StartupSource.LocalMachineRun
                or StartupSource.LocalMachineRun32
                or StartupSource.CommonFolder;
            var hint = systemWide ? " (Sistem geneli girdi: programı yönetici olarak çalıştırmanız gerekebilir.)" : string.Empty;
            return new StartupToggleResult(false, ex.Message + hint);
        }
    }
}
