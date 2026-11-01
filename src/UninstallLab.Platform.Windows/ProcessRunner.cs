using System.Diagnostics;
using UninstallLab.Core;

namespace UninstallLab.Platform.Windows;

/// <summary>Kaldırma komutunu çalıştırır. ShellExecute kullanır, böylece UAC yükseltme istemi çıkabilir.</summary>
public sealed class ProcessRunner : IProcessRunner
{
    public async Task<int> RunAsync(UninstallCommand command, CancellationToken cancellationToken = default)
    {
        var info = new ProcessStartInfo
        {
            FileName = command.FileName,
            Arguments = command.Arguments,
            UseShellExecute = true,
        };

        using var process = Process.Start(info)
            ?? throw new InvalidOperationException("Süreç başlatılamadı.");
        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }
}
