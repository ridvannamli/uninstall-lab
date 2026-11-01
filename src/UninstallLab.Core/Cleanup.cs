using System.Text.Json;

namespace UninstallLab.Core;

/// <summary>Temizliğin dokunduğu sistem işlemleri (Windows'ta gerçeklenir, testlerde sahtelenir).</summary>
public interface ICleanupSystem
{
    void CreateDirectory(string path);
    bool DirectoryExists(string path);
    void MoveDirectory(string source, string destination);
    void ExportRegistryKey(string keyPath, string destinationFile);
    void DeleteRegistryKey(string keyPath);
    void ImportRegistryFile(string file);
    void WriteAllText(string path, string text);
    string ReadAllText(string path);
    IEnumerable<string> ListDirectoryNames(string path);
}

public interface IRestorePoint
{
    bool TryCreate(string description);
}

public sealed record CleanupRecord(LeftoverKind Kind, string OriginalPath, string BackupPath);

public sealed record CleanupManifest(string SessionId, DateTime CreatedUtc, List<CleanupRecord> Entries);

public sealed record CleanupReport(
    bool DryRun,
    string? SessionId,
    bool RestorePointCreated,
    IReadOnlyList<string> Done,
    IReadOnlyList<string> Skipped,
    IReadOnlyList<string> Failed);

public sealed record UndoReport(int Restored, IReadOnlyList<string> Failed);

/// <summary>
/// Kalıntıları geri alınabilir şekilde temizler: klasörler yedek konuma taşınır,
/// registry anahtarları .reg olarak dışa aktarılıp silinir. Varsayılan olarak yalnızca düşük riskli öğeler işlenir.
/// </summary>
public sealed class CleanupExecutor
{
    private readonly ICleanupSystem _system;
    private readonly string _backupRoot;
    private readonly Func<DateTime> _utcNow;
    private readonly IRestorePoint? _restorePoint;

    public CleanupExecutor(
        ICleanupSystem system,
        string backupRoot,
        Func<DateTime>? utcNow = null,
        IRestorePoint? restorePoint = null)
    {
        _system = system;
        _backupRoot = backupRoot;
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
        _restorePoint = restorePoint;
    }

    /// <param name="progress">Her adımda canlı satır bildirir (arayüzde günlük için).</param>
    public CleanupReport Execute(
        IReadOnlyList<LeftoverItem> items,
        bool dryRun,
        bool includeRisky = false,
        IProgress<string>? progress = null)
    {
        var done = new List<string>();
        var skipped = new List<string>();
        var failed = new List<string>();
        var actionable = new List<LeftoverItem>();

        void Note(List<string> list, string line)
        {
            list.Add(line);
            progress?.Report(line);
        }

        foreach (var item in items)
        {
            if (ProtectedPaths.IsProtected(item.Path))
                Note(skipped, $"Korumalı, atlandı: {item.Path}");
            else if (!includeRisky && !item.SafeByDefault)
                Note(skipped, $"Yüksek risk, atlandı: {item.Path}");
            else
                actionable.Add(item);
        }

        if (dryRun)
        {
            foreach (var item in actionable)
                Note(done, item.Kind == LeftoverKind.Folder
                    ? $"[önizleme] Taşınacak: {item.Path}"
                    : $"[önizleme] Yedeklenip silinecek: {item.Path}");
            return new CleanupReport(true, null, false, done, skipped, failed);
        }

        if (actionable.Count == 0)
            return new CleanupReport(false, null, false, done, skipped, failed);

        var now = _utcNow();
        var sessionId = now.ToString("yyyyMMdd-HHmmss");
        var sessionDir = Join(_backupRoot, sessionId);
        _system.CreateDirectory(sessionDir);
        progress?.Report($"Oturum: {sessionId}. Geri yükleme noktası deneniyor...");
        var restoreOk = _restorePoint?.TryCreate($"UninstallLab {sessionId}") ?? false;
        progress?.Report(restoreOk
            ? "Geri yükleme noktası oluşturuldu."
            : "Geri yükleme noktası oluşturulamadı (yedekler yine alınır).");

        var entries = new List<CleanupRecord>();
        for (var n = 0; n < actionable.Count; n++)
        {
            var item = actionable[n];
            progress?.Report($"[{n + 1}/{actionable.Count}] İşleniyor: {item.Path}");
            try
            {
                if (item.Kind == LeftoverKind.Folder)
                {
                    var destination = Join(Join(sessionDir, "files"), n.ToString());
                    _system.MoveDirectory(item.Path, destination);
                    entries.Add(new CleanupRecord(item.Kind, item.Path, destination));
                    Note(done, $"Taşındı (yedeğe): {item.Path}");
                }
                else
                {
                    var file = Join(sessionDir, $"reg-{n}.reg");
                    _system.ExportRegistryKey(item.Path, file);
                    _system.DeleteRegistryKey(item.Path); // yalnızca yedek başarılıysa
                    entries.Add(new CleanupRecord(item.Kind, item.Path, file));
                    Note(done, $"Silindi (yedekli): {item.Path}");
                }
            }
            catch (Exception ex)
            {
                Note(failed, $"HATA {item.Path}: {ex.Message}");
            }
        }

        var manifest = new CleanupManifest(sessionId, now, entries);
        _system.WriteAllText(Join(sessionDir, "manifest.json"), JsonSerializer.Serialize(manifest));
        return new CleanupReport(false, sessionId, restoreOk, done, skipped, failed);
    }

    public UndoReport Undo(string sessionId)
    {
        CleanupManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<CleanupManifest>(
                _system.ReadAllText(Join(Join(_backupRoot, sessionId), "manifest.json")));
        }
        catch (Exception ex)
        {
            return new UndoReport(0, new[] { $"Manifest okunamadı: {ex.Message}" });
        }

        if (manifest is null) return new UndoReport(0, new[] { "Manifest boş." });

        var restored = 0;
        var failed = new List<string>();
        foreach (var entry in Enumerable.Reverse(manifest.Entries))
        {
            try
            {
                if (entry.Kind == LeftoverKind.Folder)
                {
                    if (_system.DirectoryExists(entry.OriginalPath))
                    {
                        failed.Add($"Zaten var, atlandı: {entry.OriginalPath}");
                        continue;
                    }
                    _system.MoveDirectory(entry.BackupPath, entry.OriginalPath);
                }
                else
                {
                    _system.ImportRegistryFile(entry.BackupPath);
                }
                restored++;
            }
            catch (Exception ex)
            {
                failed.Add($"{entry.OriginalPath}: {ex.Message}");
            }
        }

        return new UndoReport(restored, failed);
    }

    /// <summary>En son temizlik oturumunun kimliği (yoksa null).</summary>
    public string? LatestSessionId() =>
        _system.ListDirectoryNames(_backupRoot)
            .OrderByDescending(n => n, StringComparer.Ordinal)
            .FirstOrDefault();

    private static string Join(string a, string b) => a.TrimEnd('\\', '/') + "\\" + b;
}
