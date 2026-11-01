using UninstallLab.Core;

namespace UninstallLab.App;

/// <summary>Kalıntı penceresindeki satır; <see cref="Selected"/> onay kutusuna bağlıdır.</summary>
public sealed class LeftoverRow
{
    public LeftoverRow(LeftoverItem item)
    {
        Item = item;
        Selected = item.SafeByDefault;
    }

    public LeftoverItem Item { get; }

    public bool Selected { get; set; }

    public string KindText => Item.Kind == LeftoverKind.Folder ? "Klasör" : "Registry";

    public int Risk => Item.Risk;

    public string Path => Item.Path;

    public string Reason => Item.Reason;
}
