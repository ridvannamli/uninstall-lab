using System.Diagnostics;
using Microsoft.Win32;
using UninstallLab.Core;

namespace UninstallLab.Platform.Windows;

/// <summary>Temizlik işlemlerinin Windows gerçeklemesi.</summary>
public sealed class CleanupSystem : ICleanupSystem
{
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public void MoveDirectory(string source, string destination)
    {
        var parent = Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);

        var sameVolume = string.Equals(
            Path.GetPathRoot(source), Path.GetPathRoot(destination), StringComparison.OrdinalIgnoreCase);
        if (sameVolume)
        {
            Directory.Move(source, destination);
            return;
        }

        // Farklı sürücü: önce tam kopya, sonra kaynağı sil.
        CopyDirectory(source, destination);
        Directory.Delete(source, recursive: true);
    }

    public bool RegistryKeyExists(string keyPath)
    {
        var sep = keyPath.IndexOf('\\');
        if (sep <= 0 || sep == keyPath.Length - 1) return false;

        var hive = keyPath[..sep].ToUpperInvariant() switch
        {
            "HKCU" => RegistryHive.CurrentUser,
            "HKLM" => RegistryHive.LocalMachine,
            _ => (RegistryHive?)null,
        };
        if (hive is null) return false;

        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive.Value, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(keyPath[(sep + 1)..]);
            return key is not null;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return true; // Okunamıyorsa var say; gerçek hata dışa aktarma/silme adımında görünür.
        }
    }

    public void ExportRegistryKey(string keyPath, string destinationFile) =>
        RunReg("export", keyPath, destinationFile, "/y");

    public void ImportRegistryFile(string file) => RunReg("import", file);

    public void DeleteRegistryKey(string keyPath)
    {
        if (ProtectedPaths.IsProtected(keyPath))
            throw new InvalidOperationException($"Korumalı anahtar silinemez: {keyPath}");

        var sep = keyPath.IndexOf('\\');
        if (sep <= 0 || sep == keyPath.Length - 1)
            throw new ArgumentException($"Geçersiz anahtar yolu: {keyPath}");

        var hive = keyPath[..sep].ToUpperInvariant() switch
        {
            "HKCU" => RegistryHive.CurrentUser,
            "HKLM" => RegistryHive.LocalMachine,
            _ => throw new ArgumentException($"Desteklenmeyen kök: {keyPath}"),
        };

        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        baseKey.DeleteSubKeyTree(keyPath[(sep + 1)..], throwOnMissingSubKey: false);
    }

    public void WriteAllText(string path, string text) => File.WriteAllText(path, text);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public IEnumerable<string> ListDirectoryNames(string path) =>
        Directory.Exists(path)
            ? Directory.EnumerateDirectories(path).Select(d => Path.GetFileName(d)).ToList()
            : Array.Empty<string>();

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: false);
        foreach (var dir in Directory.EnumerateDirectories(source))
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
    }

    private static void RunReg(params string[] args)
    {
        var info = new ProcessStartInfo("reg.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var arg in args) info.ArgumentList.Add(arg);

        using var process = Process.Start(info)
            ?? throw new InvalidOperationException("reg.exe başlatılamadı.");
        process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new IOException($"reg.exe hata verdi ({process.ExitCode}): {error.Trim()}");
    }
}

/// <summary>Geri yükleme noktası oluşturmayı dener (yönetici hakkı ve Sistem Geri Yükleme açık olmalı).</summary>
public sealed class RestorePointCreator : IRestorePoint
{
    public bool TryCreate(string description)
    {
        try
        {
            var safe = description.Replace("'", string.Empty);
            var info = new ProcessStartInfo("powershell.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            info.ArgumentList.Add("-NoProfile");
            info.ArgumentList.Add("-Command");
            info.ArgumentList.Add(
                $"Checkpoint-Computer -Description '{safe}' -RestorePointType MODIFY_SETTINGS -ErrorAction Stop");

            using var process = Process.Start(info);
            if (process is null) return false;
            return process.WaitForExit(60_000) && process.ExitCode == 0;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }
}
