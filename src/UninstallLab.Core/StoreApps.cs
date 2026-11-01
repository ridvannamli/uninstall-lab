using System.Text.Json;
using System.Text.RegularExpressions;

namespace UninstallLab.Core;

public sealed record StoreApp(
    string Name,
    string PackageFullName,
    string? Publisher,
    string? Version,
    bool IsFramework,
    bool NonRemovable);

public sealed record StoreRemoveOutcome(bool Success, string? Error);

public sealed record StoreRemoveResult(bool Success, bool DryRun, string? Command, string? Error);

public interface IStoreAppRemover
{
    Task<StoreRemoveOutcome> RemoveAsync(string packageFullName, CancellationToken cancellationToken = default);
}

/// <summary>PowerShell'in ConvertTo-Json çıktısını (dizi ya da tek nesne) <see cref="StoreApp"/> listesine çevirir.</summary>
public static class StoreAppParser
{
    public static IReadOnlyList<StoreApp> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<StoreApp>();

        using var doc = JsonDocument.Parse(json.TrimStart('﻿'));
        var list = new List<StoreApp>();

        switch (doc.RootElement.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var element in doc.RootElement.EnumerateArray()) Add(element, list);
                break;
            case JsonValueKind.Object:
                Add(doc.RootElement, list);
                break;
        }

        return list;
    }

    private static void Add(JsonElement element, List<StoreApp> into)
    {
        if (element.ValueKind != JsonValueKind.Object) return;

        var name = Text(element, "Name");
        var full = Text(element, "PackageFullName");
        if (name is null || full is null) return;

        into.Add(new StoreApp(
            name,
            full,
            Text(element, "Publisher"),
            Text(element, "Version"),
            Flag(element, "IsFramework"),
            Flag(element, "NonRemovable")));
    }

    private static string? Text(JsonElement e, string property) =>
        e.TryGetProperty(property, out var v)
        && v.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(v.GetString())
            ? v.GetString()
            : null;

    private static bool Flag(JsonElement e, string property) =>
        e.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.True;
}

public static class StoreAppFilter
{
    /// <summary>Varsayılan olarak çatı (framework) ve kaldırılamaz sistem paketlerini gizler.</summary>
    public static IReadOnlyList<StoreApp> Apply(
        IEnumerable<StoreApp> apps,
        string? query = null,
        bool includeSystem = false)
    {
        var q = query?.Trim();

        return apps
            .Where(a => includeSystem || (!a.IsFramework && !a.NonRemovable))
            .Where(a => string.IsNullOrEmpty(q)
                || a.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (a.Publisher?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

public static class StoreAppValidator
{
    // Ad_Sürüm_Mimari_KaynakKimliği_YayıncıKimliği (yayıncı kimliği 13 karakter). Komut enjeksiyonunu önler.
    private static readonly Regex Pattern = new(
        @"^[A-Za-z0-9.\-]+_\d+(\.\d+){1,3}_[A-Za-z0-9]+_[A-Za-z0-9.\-]*_[a-z0-9]{13}$",
        RegexOptions.CultureInvariant);

    public static bool IsValidPackageFullName(string? name) =>
        !string.IsNullOrEmpty(name) && Pattern.IsMatch(name);
}

/// <summary>Mağaza uygulamasını kaldırır; çatı/sistem paketlerini ve geçersiz adları reddeder.</summary>
public sealed class StoreAppService
{
    private readonly IStoreAppRemover _remover;

    public StoreAppService(IStoreAppRemover remover) => _remover = remover;

    public static string BuildCommand(string packageFullName) =>
        $"Remove-AppxPackage -Package '{packageFullName}' -ErrorAction Stop";

    public async Task<StoreRemoveResult> RemoveAsync(
        StoreApp app,
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        if (app.IsFramework || app.NonRemovable)
            return new StoreRemoveResult(false, dryRun, null, "Sistem veya çatı paketi kaldırılamaz.");

        if (!StoreAppValidator.IsValidPackageFullName(app.PackageFullName))
            return new StoreRemoveResult(false, dryRun, null, "Geçersiz paket adı.");

        var command = BuildCommand(app.PackageFullName);
        if (dryRun) return new StoreRemoveResult(true, true, command, null);

        try
        {
            var outcome = await _remover.RemoveAsync(app.PackageFullName, cancellationToken);
            return new StoreRemoveResult(outcome.Success, false, command, outcome.Error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new StoreRemoveResult(false, false, command, ex.Message);
        }
    }
}
