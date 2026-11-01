using Microsoft.Win32;
using UninstallLab.Core;

namespace UninstallLab.Platform.Windows;

public sealed class FileSystemProbe : IFileSystemProbe
{
    public bool DirectoryExists(string path)
    {
        try
        {
            return Directory.Exists(path);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }

    public IEnumerable<string> EnumerateDirectories(string parent)
    {
        try
        {
            return Directory.Exists(parent)
                ? Directory.EnumerateDirectories(parent).ToList()
                : Array.Empty<string>();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return Array.Empty<string>();
        }
    }
}

public sealed class RegistryProbe : IRegistryProbe
{
    public IEnumerable<string> EnumerateSubKeys(string keyPath)
    {
        var sep = keyPath.IndexOf('\\');
        if (sep <= 0 || sep == keyPath.Length - 1) return Array.Empty<string>();

        var hive = keyPath[..sep].ToUpperInvariant() switch
        {
            "HKCU" => RegistryHive.CurrentUser,
            "HKLM" => RegistryHive.LocalMachine,
            _ => (RegistryHive?)null,
        };
        if (hive is null) return Array.Empty<string>();

        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive.Value, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(keyPath[(sep + 1)..]);
            return key is null
                ? Array.Empty<string>()
                : key.GetSubKeyNames().Select(n => keyPath + "\\" + n).ToList();
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return Array.Empty<string>();
        }
    }
}

public static class WindowsScanLocations
{
    public static ScanLocations Default()
    {
        var folders = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        }
        .Where(p => !string.IsNullOrEmpty(p))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

        var registry = new[] { @"HKCU\Software", @"HKLM\SOFTWARE", @"HKLM\SOFTWARE\WOW6432Node" };
        return new ScanLocations(folders, registry);
    }
}
