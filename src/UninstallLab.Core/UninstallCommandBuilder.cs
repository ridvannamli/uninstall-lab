using System.Text.RegularExpressions;

namespace UninstallLab.Core;

/// <summary>Registry'deki UninstallString değerinden çalıştırılabilir komut üretir.</summary>
public static class UninstallCommandBuilder
{
    public static UninstallCommand? Build(InstalledProgram program, bool quiet)
    {
        var raw = quiet && !string.IsNullOrWhiteSpace(program.QuietUninstallString)
            ? program.QuietUninstallString
            : program.UninstallString;
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var (file, args) = Split(raw.Trim());
        var name = LastSegment(file);
        var isMsi = name.Equals("msiexec.exe", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("msiexec", StringComparison.OrdinalIgnoreCase);

        if (isMsi)
        {
            // Çoğu ARP girdisi /I ile gelir; kaldırma için /X gerekir.
            args = Regex.Replace(args, @"/I(?=\s*\{)", "/X", RegexOptions.IgnoreCase);
            if (quiet && !Regex.IsMatch(args, @"/q[nb]?\b|/quiet", RegexOptions.IgnoreCase))
                args = (args + " /qn /norestart").Trim();
        }

        return new UninstallCommand(file, args, isMsi);
    }

    private static (string File, string Args) Split(string raw)
    {
        if (raw.StartsWith('"'))
        {
            var end = raw.IndexOf('"', 1);
            if (end > 0) return (raw[1..end], raw[(end + 1)..].Trim());
        }

        var exe = raw.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (exe >= 0)
        {
            var cut = exe + 4;
            return (raw[..cut], raw[cut..].Trim());
        }

        var space = raw.IndexOf(' ');
        return space < 0 ? (raw, string.Empty) : (raw[..space], raw[(space + 1)..].Trim());
    }

    // Platformdan bağımsız dosya adı (Linux testlerinde '\' ayırıcıdır).
    private static string LastSegment(string path)
    {
        var i = Math.Max(path.LastIndexOf('\\'), path.LastIndexOf('/'));
        return i < 0 ? path : path[(i + 1)..];
    }
}
