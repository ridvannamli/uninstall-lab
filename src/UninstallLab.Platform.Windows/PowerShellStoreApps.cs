using System.Diagnostics;
using System.Text;
using UninstallLab.Core;

namespace UninstallLab.Platform.Windows;

internal static class PowerShell
{
    public static async Task<(int ExitCode, string Output, string Error)> RunAsync(
        string script,
        CancellationToken cancellationToken)
    {
        var info = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        info.ArgumentList.Add("-NoProfile");
        info.ArgumentList.Add("-NonInteractive");
        info.ArgumentList.Add("-Command");
        info.ArgumentList.Add("[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; " + script);

        using var process = Process.Start(info)
            ?? throw new InvalidOperationException("PowerShell başlatılamadı.");

        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return (process.ExitCode, await output, await error);
    }
}

/// <summary>Yüklü Mağaza (UWP/MSIX) uygulamalarını Get-AppxPackage ile listeler.</summary>
public sealed class PowerShellStoreAppSource
{
    private const string Script =
        "Get-AppxPackage | Select-Object Name,PackageFullName,Publisher,Version,IsFramework,NonRemovable | ConvertTo-Json -Compress";

    public async Task<IReadOnlyList<StoreApp>> GetAppsAsync(CancellationToken cancellationToken = default)
    {
        var (exitCode, output, error) = await PowerShell.RunAsync(Script, cancellationToken);
        if (exitCode != 0)
            throw new InvalidOperationException($"Get-AppxPackage başarısız ({exitCode}): {error.Trim()}");

        return StoreAppParser.Parse(output);
    }
}

public sealed class PowerShellStoreAppRemover : IStoreAppRemover
{
    public async Task<StoreRemoveOutcome> RemoveAsync(
        string packageFullName,
        CancellationToken cancellationToken = default)
    {
        if (!StoreAppValidator.IsValidPackageFullName(packageFullName))
            return new StoreRemoveOutcome(false, "Geçersiz paket adı.");

        var (exitCode, _, error) = await PowerShell.RunAsync(
            StoreAppService.BuildCommand(packageFullName), cancellationToken);

        return exitCode == 0
            ? new StoreRemoveOutcome(true, null)
            : new StoreRemoveOutcome(false, error.Trim());
    }
}
