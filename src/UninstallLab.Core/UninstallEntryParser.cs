namespace UninstallLab.Core;

/// <summary>Bir Uninstall kayıt anahtarının değerlerinden <see cref="InstalledProgram"/> üretir.</summary>
public static class UninstallEntryParser
{
    /// <param name="getValue">Değer adına göre ham değeri döndürür (yoksa null).</param>
    /// <returns>Listelenmemesi gereken girdilerde (adsız, sistem bileşeni, güncelleme) null.</returns>
    public static InstalledProgram? Parse(Func<string, object?> getValue)
    {
        var name = AsText(getValue("DisplayName"));
        if (name is null) return null;
        if (getValue("SystemComponent") is int systemComponent && systemComponent == 1) return null;
        if (AsText(getValue("ParentKeyName")) is not null) return null;

        return new InstalledProgram(
            name,
            AsText(getValue("Publisher")),
            AsText(getValue("DisplayVersion")),
            AsText(getValue("UninstallString")),
            AsText(getValue("QuietUninstallString")),
            AsPath(getValue("InstallLocation")));
    }

    private static string? AsText(object? value)
    {
        var text = value as string;
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    // Tırnakları ve sondaki ayırıcıyı temizler; boşsa null.
    private static string? AsPath(object? value)
    {
        var text = AsText(value)?.Trim('"').TrimEnd('\\', '/').Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }
}
