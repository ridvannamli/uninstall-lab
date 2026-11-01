using System.Text.Json;
using UninstallLab.Core;
using Xunit;

namespace UninstallLab.Core.Tests;

public class StoreAppTests
{
    private const string Good = "A.App_1.0.0.0_x64__8wekyb3d8bbwe";

    private sealed class FakeRemover : IStoreAppRemover
    {
        public int Calls { get; private set; }
        public StoreRemoveOutcome Outcome { get; init; } = new(true, null);
        public Task<StoreRemoveOutcome> RemoveAsync(string packageFullName, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Outcome);
        }
    }

    private static StoreApp App(string full = Good, bool framework = false, bool nonRemovable = false) =>
        new("A.App", full, "CN=A", "1.0.0.0", framework, nonRemovable);

    [Fact]
    public void Parse_Array()
    {
        var json = "[{\"Name\":\"A.App\",\"PackageFullName\":\"" + Good + "\",\"Publisher\":\"CN=A\",\"Version\":\"1.0.0.0\",\"IsFramework\":false,\"NonRemovable\":false},"
                 + "{\"Name\":\"Fx\",\"PackageFullName\":\"Fx_1.0.0.0_x64__8wekyb3d8bbwe\",\"IsFramework\":true,\"NonRemovable\":false}]";

        var apps = StoreAppParser.Parse(json);

        Assert.Equal(2, apps.Count);
        Assert.False(apps[0].IsFramework);
        Assert.True(apps[1].IsFramework);
        Assert.Null(apps[1].Publisher);
    }

    [Fact]
    public void Parse_SingleObject_ReturnsOne()
    {
        var json = "{\"Name\":\"A.App\",\"PackageFullName\":\"" + Good + "\"}";
        Assert.Single(StoreAppParser.Parse(json));
    }

    [Fact]
    public void Parse_EmptyOrBlank_ReturnsEmpty()
    {
        Assert.Empty(StoreAppParser.Parse(null));
        Assert.Empty(StoreAppParser.Parse("  "));
    }

    [Fact]
    public void Parse_SkipsEntriesWithoutNameOrPackage_AndHandlesBom()
    {
        var json = "\uFEFF[{\"Publisher\":\"x\"},{\"Name\":\"A\"},{\"Name\":\"A.App\",\"PackageFullName\":\"" + Good + "\"}]";
        Assert.Single(StoreAppParser.Parse(json));
    }

    [Fact]
    public void Parse_InvalidJson_Throws()
    {
        Assert.ThrowsAny<JsonException>(() => StoreAppParser.Parse("not json"));
    }

    [Fact]
    public void Filter_HidesFrameworkAndNonRemovable_ByDefault_AndSorts()
    {
        var apps = new[]
        {
            new StoreApp("Zeta", Good, null, null, false, false),
            new StoreApp("Alpha", Good, null, null, false, false),
            new StoreApp("Fx", Good, null, null, true, false),
            new StoreApp("Sys", Good, null, null, false, true),
        };

        var result = StoreAppFilter.Apply(apps);

        Assert.Equal(new[] { "Alpha", "Zeta" }, result.Select(a => a.Name).ToArray());
        Assert.Equal(4, StoreAppFilter.Apply(apps, includeSystem: true).Count);
    }

    [Fact]
    public void Filter_Query_MatchesNameOrPublisher()
    {
        var apps = new[]
        {
            new StoreApp("Calc", Good, "CN=Acme", null, false, false),
            new StoreApp("Notes", Good, "CN=Other", null, false, false),
        };

        Assert.Single(StoreAppFilter.Apply(apps, "calc"));
        Assert.Single(StoreAppFilter.Apply(apps, "acme"));
    }

    [Theory]
    [InlineData("Microsoft.WindowsCalculator_11.2.0.0_x64__8wekyb3d8bbwe", true)]
    [InlineData("Pkg.Name_1.2.3.4_neutral_en-us_8wekyb3d8bbwe", true)]
    [InlineData("x'; Remove-Item C:\\ -Recurse; '", false)]
    [InlineData("Name_1.0.0.0_x64__short", false)]
    [InlineData("", false)]
    public void Validator_AcceptsOnlyRealPackageNames(string name, bool expected)
    {
        Assert.Equal(expected, StoreAppValidator.IsValidPackageFullName(name));
    }

    [Fact]
    public async Task Remove_RefusesFrameworkAndNonRemovable()
    {
        var remover = new FakeRemover();
        var svc = new StoreAppService(remover);

        Assert.False((await svc.RemoveAsync(App(framework: true), false)).Success);
        Assert.False((await svc.RemoveAsync(App(nonRemovable: true), false)).Success);
        Assert.Equal(0, remover.Calls);
    }

    [Fact]
    public async Task Remove_RefusesInvalidPackageName()
    {
        var remover = new FakeRemover();
        var r = await new StoreAppService(remover).RemoveAsync(App(full: "bad'; name"), false);

        Assert.False(r.Success);
        Assert.Equal(0, remover.Calls);
    }

    [Fact]
    public async Task Remove_DryRun_ReturnsCommand_WithoutRunning()
    {
        var remover = new FakeRemover();
        var r = await new StoreAppService(remover).RemoveAsync(App(), dryRun: true);

        Assert.True(r.Success);
        Assert.True(r.DryRun);
        Assert.Contains(Good, r.Command);
        Assert.Equal(0, remover.Calls);
    }

    [Fact]
    public async Task Remove_Real_CallsRemover_AndPropagatesFailure()
    {
        var ok = new FakeRemover();
        Assert.True((await new StoreAppService(ok).RemoveAsync(App(), false)).Success);
        Assert.Equal(1, ok.Calls);

        var bad = new FakeRemover { Outcome = new StoreRemoveOutcome(false, "denied") };
        var r = await new StoreAppService(bad).RemoveAsync(App(), false);
        Assert.False(r.Success);
        Assert.Equal("denied", r.Error);
    }
}
