using System.Text.Json;

namespace UninstallLab.Core;

/// <summary>
/// Kullanıcı ayarları. Güvenlik için "Sadece göster" (dry-run) her açılışta açık başlar ve kalıcı olarak kapatılamaz.
/// </summary>
public sealed record AppSettings(bool QuietByDefault = false, bool ShowSystemStoreApps = false)
{
    public static AppSettings Default { get; } = new();
}

public static class AppSettingsSerializer
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string Serialize(AppSettings settings) => JsonSerializer.Serialize(settings, Options);

    /// <summary>Boş veya bozuk içerikte varsayılan ayarları döndürür.</summary>
    public static AppSettings Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return AppSettings.Default;

        try
        {
            return JsonSerializer.Deserialize<AppSettings>(json.TrimStart('﻿')) ?? AppSettings.Default;
        }
        catch (JsonException)
        {
            return AppSettings.Default;
        }
    }
}
