using UninstallLab.Core;

namespace UninstallLab.App;

/// <summary>Tarayıcı eklentisinin liste görünümü.</summary>
public sealed record ExtensionRow(BrowserExtension Extension)
{
    public string Browser => Extension.Browser.ToString();

    public string Name => Extension.Name;

    public string Version => Extension.Version ?? string.Empty;

    public string Profile => Extension.Profile;

    public string Id => Extension.Id;

    public string StateText => Extension.Enabled switch
    {
        true => "Açık",
        false => "Kapalı",
        null => "-",
    };
}
