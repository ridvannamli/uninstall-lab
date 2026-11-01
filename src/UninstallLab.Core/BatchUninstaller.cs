namespace UninstallLab.Core;

public sealed record BatchItemResult(InstalledProgram Program, UninstallResult Result);

public sealed record BatchProgress(int Index, int Total, string Name);

/// <summary>Seçili programları sırayla kaldırır. Bir hata sonrakileri durdurmaz; iptalde o ana kadarki sonuçları döndürür.</summary>
public sealed class BatchUninstaller
{
    private readonly UninstallService _service;

    public BatchUninstaller(UninstallService service) => _service = service;

    public async Task<IReadOnlyList<BatchItemResult>> RunAsync(
        IReadOnlyList<InstalledProgram> programs,
        bool quiet,
        bool dryRun,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<BatchItemResult>();

        for (var i = 0; i < programs.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            progress?.Report(new BatchProgress(i + 1, programs.Count, programs[i].Name));
            try
            {
                var result = await _service.UninstallAsync(programs[i], quiet, dryRun, cancellationToken);
                results.Add(new BatchItemResult(programs[i], result));
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        return results;
    }
}
