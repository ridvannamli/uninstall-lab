using System.Text.Json;
using System.Text.RegularExpressions;

namespace UninstallLab.Core;

public enum BrowserKind { Chrome, Edge, Brave, Firefox }

/// <param name="Enabled">Bilinmiyorsa null (Chromium ayarları çalışan tarayıcıdan okunmaz).</param>
public sealed record BrowserExtension(
    BrowserKind Browser,
    string Id,
    string Name,
    string? Version,
    string Profile,
    string Path,
    bool? Enabled);

public interface IBrowserExtensionSource
{
    IReadOnlyList<BrowserExtension> GetExtensions();
}

public sealed record ManifestInfo(string Name, string? Version, string? DefaultLocale);

public sealed record FirefoxAddon(string Id, string Name, string? Version, bool Active);

/// <summary>Chromium tabanlı tarayıcı eklenti manifestlerini okur.</summary>
public static class ExtensionManifest
{
    private static readonly JsonDocumentOptions Lenient = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static ManifestInfo? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var doc = JsonDocument.Parse(json.TrimStart('﻿'), Lenient);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;

            var name = Text(root, "name");
            return name is null ? null : new ManifestInfo(name, Text(root, "version"), Text(root, "default_locale"));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>"__MSG_anahtar__" biçimindeki yerelleştirilmiş adı çözer; çözülemezse ham adı döndürür.</summary>
    public static string ResolveName(string name, Func<string, string?> lookupMessage)
    {
        var match = Regex.Match(name, @"^__MSG_(?<key>.+)__$");
        if (!match.Success) return name;
        return lookupMessage(match.Groups["key"].Value) ?? name;
    }

    /// <summary>_locales/xx/messages.json içinde anahtarı (büyük/küçük harf duyarsız) arar.</summary>
    public static string? LookupMessage(string? messagesJson, string key)
    {
        if (string.IsNullOrWhiteSpace(messagesJson)) return null;

        try
        {
            using var doc = JsonDocument.Parse(messagesJson.TrimStart('﻿'), Lenient);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;

            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (!property.Name.Equals(key, StringComparison.OrdinalIgnoreCase)) continue;
                if (property.Value.ValueKind == JsonValueKind.Object
                    && property.Value.TryGetProperty("message", out var message)
                    && message.ValueKind == JsonValueKind.String)
                    return message.GetString();
            }
        }
        catch (JsonException)
        {
            // Bozuk dosya: ham ada geri dön.
        }

        return null;
    }

    private static string? Text(JsonElement e, string property) =>
        e.TryGetProperty(property, out var v)
        && v.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(v.GetString())
            ? v.GetString()
            : null;
}

/// <summary>Firefox profilindeki extensions.json dosyasını okur (yalnızca kullanıcı eklentileri).</summary>
public static class FirefoxAddons
{
    public static IReadOnlyList<FirefoxAddon> Parse(string? json)
    {
        var result = new List<FirefoxAddon>();
        if (string.IsNullOrWhiteSpace(json)) return result;

        try
        {
            using var doc = JsonDocument.Parse(json.TrimStart('﻿'));
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty("addons", out var addons)
                || addons.ValueKind != JsonValueKind.Array)
                return result;

            foreach (var addon in addons.EnumerateArray())
            {
                if (addon.ValueKind != JsonValueKind.Object) continue;
                if (Text(addon, "type") != "extension") continue;
                if (Text(addon, "location") != "app-profile") continue;

                var id = Text(addon, "id");
                if (id is null) continue;

                string? name = null;
                if (addon.TryGetProperty("defaultLocale", out var locale) && locale.ValueKind == JsonValueKind.Object)
                    name = Text(locale, "name");

                var active = Flag(addon, "active") && !Flag(addon, "userDisabled");
                result.Add(new FirefoxAddon(id, name ?? id, Text(addon, "version"), active));
            }
        }
        catch (JsonException)
        {
            return new List<FirefoxAddon>();
        }

        return result;
    }

    private static string? Text(JsonElement e, string property) =>
        e.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static bool Flag(JsonElement e, string property) =>
        e.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.True;
}

public static class ExtensionFilter
{
    public static IReadOnlyList<BrowserExtension> Apply(IEnumerable<BrowserExtension> extensions, string? query = null)
    {
        var q = query?.Trim();

        return extensions
            .Where(x => string.IsNullOrEmpty(q)
                || x.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || x.Id.Contains(q, StringComparison.OrdinalIgnoreCase)
                || x.Browser.ToString().Contains(q, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Browser)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
