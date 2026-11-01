namespace UninstallLab.Core;

/// <summary>Bir programın standart kaldırmasını yürütür; dry-run modunda hiçbir şey çalıştırmaz.</summary>
public sealed class UninstallService
{
    private readonly IProcessRunner _runner;

    public UninstallService(IProcessRunner runner) => _runner = runner;

    public async Task<UninstallResult> UninstallAsync(
        InstalledProgram program,
        bool quiet,
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        var command = UninstallCommandBuilder.Build(program, quiet);
        if (command is null)
            return new UninstallResult(false, dryRun, null, null, "Kaldırma komutu bulunamadı.");

        if (dryRun)
            return new UninstallResult(true, true, command, null, null);

        try
        {
            var code = await _runner.RunAsync(command, cancellationToken);
            // 3010 ve 1641: başarılı, yeniden başlatma gerekli.
            var ok = code is 0 or 3010 or 1641;
            return new UninstallResult(ok, false, command, code, ok ? null : $"Çıkış kodu: {code}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new UninstallResult(false, false, command, null, ex.Message);
        }
    }
}
