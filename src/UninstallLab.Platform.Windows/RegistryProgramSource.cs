using System.Security;
using Microsoft.Win32;
using UninstallLab.Core;

namespace UninstallLab.Platform.Windows;

/// <summary>Yüklü programları Windows registry Uninstall anahtarlarından okur (salt okunur).</summary>
public sealed class RegistryProgramSource : IProgramSource
{
    private const string UninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

    public IReadOnlyList<InstalledProgram> GetPrograms()
    {
        var found = new List<InstalledProgram>();
        Read(RegistryHive.LocalMachine, RegistryView.Registry64, found);
        Read(RegistryHive.LocalMachine, RegistryView.Registry32, found);
        Read(RegistryHive.CurrentUser, RegistryView.Default, found);
        return ProgramFilter.Apply(found);
    }

    private static void Read(RegistryHive hive, RegistryView view, List<InstalledProgram> into)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var root = baseKey.OpenSubKey(UninstallPath);
            if (root is null) return;

            foreach (var subName in root.GetSubKeyNames())
            {
                try
                {
                    using var key = root.OpenSubKey(subName);
                    if (key is null) continue;
                    var program = UninstallEntryParser.Parse(valueName => key.GetValue(valueName));
                    if (program is not null) into.Add(program);
                }
                catch (Exception ex) when (IsAccessError(ex))
                {
                    // Okunamayan tek bir anahtar listeyi bozmamalı.
                }
            }
        }
        catch (Exception ex) when (IsAccessError(ex))
        {
            // Kök anahtar okunamıyorsa bu kaynağı atla.
        }
    }

    private static bool IsAccessError(Exception ex) =>
        ex is SecurityException or UnauthorizedAccessException or IOException;
}
