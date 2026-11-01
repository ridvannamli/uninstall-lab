using UninstallLab.Core;
using Xunit;

namespace UninstallLab.Core.Tests;

public class ProgramListingTests
{
    private static Func<string, object?> Values(params (string Key, object Value)[] items)
    {
        var map = items.ToDictionary(i => i.Key, i => i.Value);
        return key => map.TryGetValue(key, out var v) ? v : null;
    }

    [Fact]
    public void Parse_ReturnsNull_WhenNoDisplayName()
    {
        Assert.Null(UninstallEntryParser.Parse(Values(("Publisher", "X"))));
    }

    [Fact]
    public void Parse_ReturnsNull_ForSystemComponent()
    {
        var p = UninstallEntryParser.Parse(Values(("DisplayName", "Sys"), ("SystemComponent", 1)));
        Assert.Null(p);
    }

    [Fact]
    public void Parse_ReturnsNull_ForUpdateWithParentKey()
    {
        var p = UninstallEntryParser.Parse(Values(("DisplayName", "Patch"), ("ParentKeyName", "OperatingSystem")));
        Assert.Null(p);
    }

    [Fact]
    public void Parse_MapsFields_AndTrims()
    {
        var p = UninstallEntryParser.Parse(Values(
            ("DisplayName", "  Demo  "),
            ("Publisher", "Acme"),
            ("DisplayVersion", "1.2.3"),
            ("UninstallString", "demo.exe /u")));

        Assert.NotNull(p);
        Assert.Equal("Demo", p!.Name);
        Assert.Equal("Acme", p.Publisher);
        Assert.Equal("1.2.3", p.Version);
        Assert.True(p.CanUninstall);
    }

    [Theory]
    [InlineData("C:\\Program Files\\Demo\\", "C:\\Program Files\\Demo")]
    [InlineData("\"C:\\Program Files\\Demo\"", "C:\\Program Files\\Demo")]
    [InlineData("  C:\\Apps\\Demo  ", "C:\\Apps\\Demo")]
    public void Parse_InstallLocation_TrimsQuotesAndTrailingSeparator(string raw, string expected)
    {
        var p = UninstallEntryParser.Parse(Values(("DisplayName", "Demo"), ("InstallLocation", raw)));
        Assert.Equal(expected, p!.InstallLocation);
    }

    [Fact]
    public void Parse_InstallLocation_NullWhenMissingOrBlank()
    {
        Assert.Null(UninstallEntryParser.Parse(Values(("DisplayName", "Demo")))!.InstallLocation);
        Assert.Null(UninstallEntryParser.Parse(Values(("DisplayName", "Demo"), ("InstallLocation", "  ")))!.InstallLocation);
        Assert.Null(UninstallEntryParser.Parse(Values(("DisplayName", "Demo"), ("InstallLocation", "\"\"")))!.InstallLocation);
    }

    [Fact]
    public void Apply_RemovesDuplicates_AndSortsByName()
    {
        var input = new[]
        {
            new InstalledProgram("beta", Version: "1"),
            new InstalledProgram("Alpha", Version: "1"),
            new InstalledProgram("ALPHA", Version: "1"),
        };

        var result = ProgramFilter.Apply(input);

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("beta", result[1].Name);
    }

    [Fact]
    public void Apply_FiltersByQuery_CaseInsensitive_NameOrPublisher()
    {
        var input = new[]
        {
            new InstalledProgram("Editor", Publisher: "Acme"),
            new InstalledProgram("Player", Publisher: "Other"),
        };

        Assert.Single(ProgramFilter.Apply(input, "edit"));
        Assert.Single(ProgramFilter.Apply(input, "ACME"));
    }

    [Fact]
    public void Apply_OnlyUninstallable_HidesEntriesWithoutUninstallString()
    {
        var input = new[]
        {
            new InstalledProgram("A", UninstallString: "a.exe"),
            new InstalledProgram("B"),
        };

        var result = ProgramFilter.Apply(input, onlyUninstallable: true);

        Assert.Single(result);
        Assert.Equal("A", result[0].Name);
    }
}
