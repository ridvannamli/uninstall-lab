using System.Text.RegularExpressions;

namespace UninstallLab.Core;

/// <summary>Program/klasör adlarını eşleştirme için sadeleştirir.</summary>
public static class NameNormalizer
{
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var s = Regex.Replace(text, @"\([^)]*\)", " ");
        s = Regex.Replace(s, @"\b(x64|x86|64-bit|32-bit)\b", " ", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\s+", " ").Trim();
        s = Regex.Replace(s, @"\s+v?\d+(\.\d+)*$", string.Empty, RegexOptions.IgnoreCase);
        return s.Trim().ToLowerInvariant();
    }
}
