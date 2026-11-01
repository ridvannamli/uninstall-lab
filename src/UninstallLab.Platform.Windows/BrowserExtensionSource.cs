using System.Diagnostics;
using System.Text.Json;
using UninstallLab.Core;

namespace UninstallLab.Platform.Windows;

/// <summary>Chrome, Edge, Brave ve Firefox eklentilerini disk üzerinden okur (salt okunur).</summary>
public sealed class BrowserExtensionSource : IBrowserExtensionSource
{
    public IReadOnlyList<BrowserExtension> GetExtensions()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var list = new List<BrowserExtension>();

        ReadChromium(BrowserKind.Chrome, Path.Combine(local, "Google", "Chrome", "User Data"), list);
        ReadChromium(BrowserKind.Edge, Path.Combine(local, "Microsoft", "Edge", "User Data"), list);
        ReadChromium(BrowserKind.Brave, Path.Combine(local, "BraveSoftware", "Brave-Browser", "User Data"), list);
        ReadFirefox(Path.Combine(roaming, "Mozilla", "Firefox", "Profiles"), list);
        return list;
    }

    private static void ReadChromium(BrowserKind browser, string userData, List<BrowserExtension> into)
    {
        if (!Directory.Exists(userData)) return;

        try
        {
            foreach (var profileDir in Directory.EnumerateDirectories(userData))
            {
                var profile = Path.GetFileName(profileDir);
                if (profile != "Default" && !profile.StartsWith("Profile ", StringComparison.Ordinal)) continue;

                var extensionsRoot = Path.Combine(profileDir, "Extensions");
                if (!Directory.Exists(extensionsRoot)) continue;

                foreach (var extensionDir in Directory.EnumerateDirectories(extensionsRoot))
                {
                    var versionDir = Directory.EnumerateDirectories(extensionDir)
                        .OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase)
                        .FirstOrDefault();
                    if (versionDir is null) continue;

                    var info = ExtensionManifest.Parse(TryRead(Path.Combine(versionDir, "manifest.json")));
                    if (info is null) continue;

                    var name = ExtensionManifest.ResolveName(
                        info.Name, key => LookupLocale(versionDir, info.DefaultLocale, key));
                    into.Add(new BrowserExtension(
                        browser, Path.GetFileName(extensionDir), name, info.Version, profile, extensionDir, null));
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Erişilemeyen tarayıcı verisini atla.
        }
    }

    private static void ReadFirefox(string profilesRoot, List<BrowserExtension> into)
    {
        if (!Directory.Exists(profilesRoot)) return;

        try
        {
            foreach (var profileDir in Directory.EnumerateDirectories(profilesRoot))
            {
                var json = TryRead(Path.Combine(profileDir, "extensions.json"));
                foreach (var addon in FirefoxAddons.Parse(json))
                    into.Add(new BrowserExtension(
                        BrowserKind.Firefox, addon.Id, addon.Name, addon.Version,
                        Path.GetFileName(profileDir), profileDir, addon.Active));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Erişilemeyen profili atla.
        }
    }

    private static string? LookupLocale(string versionDir, string? defaultLocale, string key)
    {
        var locale = string.IsNullOrEmpty(defaultLocale) ? "en" : defaultLocale;
        var json = TryRead(Path.Combine(versionDir, "_locales", locale, "messages.json"));
        return ExtensionManifest.LookupMessage(json, key);
    }

    private static string? TryRead(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}

/// <summary>Tarayıcının eklenti yönetim sayfasını veya eklenti klasörünü açar.</summary>
public static class BrowserLauncher
{
    public static void OpenExtensionsPage(BrowserKind browser)
    {
        var (exe, url) = browser switch
        {
            BrowserKind.Chrome => ("chrome.exe", "chrome://extensions"),
            BrowserKind.Edge => ("msedge.exe", "edge://extensions"),
            BrowserKind.Brave => ("brave.exe", "brave://extensions"),
            BrowserKind.Firefox => ("firefox.exe", "about:addons"),
            _ => throw new ArgumentOutOfRangeException(nameof(browser)),
        };

        Process.Start(new ProcessStartInfo(exe, url) { UseShellExecute = true });
    }

    public static void OpenFolder(string path)
    {
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException(path);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
