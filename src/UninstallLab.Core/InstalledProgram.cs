namespace UninstallLab.Core;

/// <summary>Yüklü bir programın görüntülenebilir özeti.</summary>
public sealed record InstalledProgram(
    string Name,
    string? Publisher = null,
    string? Version = null,
    string? UninstallString = null,
    string? QuietUninstallString = null,
    string? InstallLocation = null)
{
    public bool CanUninstall =>
        !string.IsNullOrWhiteSpace(UninstallString) ||
        !string.IsNullOrWhiteSpace(QuietUninstallString);
}
