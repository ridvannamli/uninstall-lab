using System.Text.RegularExpressions;

namespace UninstallLab.Core;

/// <summary>Asla silinmemesi/raporlanmaması gereken sistem yolları ve registry anahtarları.</summary>
public static class ProtectedPaths
{
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    private static readonly Regex[] Patterns =
    {
        new(@"^[a-z]:\\windows(\\|$)", Options),
        new(@"^[a-z]:\\program files( \(x86\))?$", Options),
        new(@"^[a-z]:\\programdata$", Options),
        new(@"^[a-z]:\\users$", Options),
        new(@"^[a-z]:\\?$", Options),
        // Kullanıcı profili ve ortak kullanıcı klasörleri.
        new(@"^[a-z]:\\users\\[^\\]+$", Options),
        new(@"^[a-z]:\\users\\[^\\]+\\(desktop|documents|downloads|pictures|music|videos|onedrive)$", Options),
        new(@"\\appdata(\\(local|locallow|roaming))?$", Options),
        new(@"\\(microsoft|windows nt|common files|windowsapps|windows defender|internet explorer)$", Options),
        // Registry.
        new(@"^hk(lm|cu)\\(software\\)?(wow6432node\\)?microsoft$", Options),
        new(@"^hklm\\(software\\)?(wow6432node\\)?(classes|policies)$", Options),
        new(@"^hklm\\system(\\|$)", Options),
        new(@"^hk(lm|cu)\\software$", Options),
        new(@"^hklm\\software\\wow6432node$", Options),
    };

    public static bool IsProtected(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return true;
        var p = path.Replace('/', '\\').TrimEnd('\\');
        return Patterns.Any(r => r.IsMatch(p));
    }
}
