namespace UninstallLab.Core;

/// <summary>Çalıştırılacak kaldırma komutu (dosya + argümanlar).</summary>
public sealed record UninstallCommand(string FileName, string Arguments, bool IsMsi);

/// <summary>Kaldırma işleminin sonucu.</summary>
public sealed record UninstallResult(
    bool Success,
    bool DryRun,
    UninstallCommand? Command,
    int? ExitCode,
    string? Error);

/// <summary>Kaldırma komutunu çalıştıran süreç soyutlaması.</summary>
public interface IProcessRunner
{
    Task<int> RunAsync(UninstallCommand command, CancellationToken cancellationToken = default);
}
