using System.IO;
using UninstallLab.Core;

namespace UninstallLab.App;

/// <summary>Ayarları %AppData%\UninstallLab\settings.json dosyasında saklar.</summary>
internal static class SettingsFile
{
    public static string FilePath { get; } = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UninstallLab", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            return File.Exists(FilePath)
                ? AppSettingsSerializer.Deserialize(File.ReadAllText(FilePath))
                : AppSettings.Default;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return AppSettings.Default;
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, AppSettingsSerializer.Serialize(settings));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Ayar kaydedilemezse uygulama çalışmaya devam eder.
        }
    }
}
