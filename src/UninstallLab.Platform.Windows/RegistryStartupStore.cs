using System.Security;
using Microsoft.Win32;
using UninstallLab.Core;

namespace UninstallLab.Platform.Windows;

/// <summary>Başlangıç girdilerini Run anahtarları ve Başlangıç klasörlerinden okur; açma/kapama StartupApproved üzerinden yapılır.</summary>
public sealed class RegistryStartupStore : IStartupStore
{
    private const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string Run32Path = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedBase = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\";

    public IReadOnlyList<StartupEntry> GetEntries()
    {
        var list = new List<StartupEntry>();
        ReadRun(StartupSource.CurrentUserRun, list);
        ReadRun(StartupSource.LocalMachineRun, list);
        ReadRun(StartupSource.LocalMachineRun32, list);
        ReadFolder(StartupSource.CurrentUserFolder, Environment.GetFolderPath(Environment.SpecialFolder.Startup), list);
        ReadFolder(StartupSource.CommonFolder, Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), list);
        return list;
    }

    public void SetEnabled(StartupEntry entry, bool enabled)
    {
        var (hive, _, approvedPath) = Locate(entry.Source);
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var approved = baseKey.CreateSubKey(approvedPath, writable: true)
            ?? throw new IOException("StartupApproved anahtarı açılamadı.");
        approved.SetValue(entry.Name, StartupApproval.Encode(enabled, DateTime.UtcNow), RegistryValueKind.Binary);
    }

    private static (RegistryHive Hive, string RunKey, string ApprovedKey) Locate(StartupSource source) => source switch
    {
        StartupSource.CurrentUserRun => (RegistryHive.CurrentUser, RunPath, ApprovedBase + "Run"),
        StartupSource.LocalMachineRun => (RegistryHive.LocalMachine, RunPath, ApprovedBase + "Run"),
        StartupSource.LocalMachineRun32 => (RegistryHive.LocalMachine, Run32Path, ApprovedBase + "Run32"),
        StartupSource.CurrentUserFolder => (RegistryHive.CurrentUser, string.Empty, ApprovedBase + "StartupFolder"),
        StartupSource.CommonFolder => (RegistryHive.LocalMachine, string.Empty, ApprovedBase + "StartupFolder"),
        _ => throw new ArgumentOutOfRangeException(nameof(source)),
    };

    private static void ReadRun(StartupSource source, List<StartupEntry> into)
    {
        var (hive, runPath, approvedPath) = Locate(source);
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var run = baseKey.OpenSubKey(runPath);
            if (run is null) return;
            using var approved = baseKey.OpenSubKey(approvedPath);

            foreach (var name in run.GetValueNames())
            {
                if (run.GetValue(name) is not string command || string.IsNullOrWhiteSpace(command)) continue;
                var enabled = StartupApproval.IsEnabled(approved?.GetValue(name) as byte[]);
                into.Add(new StartupEntry(name, command, source, enabled));
            }
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            // Okunamayan kaynağı atla.
        }
    }

    private static void ReadFolder(StartupSource source, string folder, List<StartupEntry> into)
    {
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;
        var (hive, _, approvedPath) = Locate(source);

        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var approved = baseKey.OpenSubKey(approvedPath);

            foreach (var file in Directory.EnumerateFiles(folder))
            {
                var name = System.IO.Path.GetFileName(file);
                if (name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;
                var enabled = StartupApproval.IsEnabled(approved?.GetValue(name) as byte[]);
                into.Add(new StartupEntry(name, file, source, enabled));
            }
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            // Okunamayan kaynağı atla.
        }
    }
}
