using UninstallLab.Core;

namespace UninstallLab.App;

/// <summary>Başlangıç girdisinin liste görünümü.</summary>
public sealed record StartupRow(StartupEntry Entry)
{
    public string Name => Entry.Name;

    public string Command => Entry.Command;

    public string StateText => Entry.Enabled ? "Açık" : "Kapalı";

    public string SourceText => Entry.Source switch
    {
        StartupSource.CurrentUserRun => "Kullanıcı (Run)",
        StartupSource.LocalMachineRun => "Sistem (Run)",
        StartupSource.LocalMachineRun32 => "Sistem 32-bit (Run)",
        StartupSource.CurrentUserFolder => "Kullanıcı klasörü",
        StartupSource.CommonFolder => "Ortak klasör",
        _ => "?",
    };
}
