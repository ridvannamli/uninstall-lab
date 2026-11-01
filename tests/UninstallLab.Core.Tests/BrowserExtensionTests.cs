using UninstallLab.Core;
using Xunit;

namespace UninstallLab.Core.Tests;

public class BrowserExtensionTests
{
    [Fact]
    public void Manifest_Parse_ReadsNameVersionLocale()
    {
        var info = ExtensionManifest.Parse(
            "{\"name\":\"__MSG_appName__\",\"version\":\"1.2\",\"default_locale\":\"en\"}");

        Assert.NotNull(info);
        Assert.Equal("__MSG_appName__", info!.Name);
        Assert.Equal("1.2", info.Version);
        Assert.Equal("en", info.DefaultLocale);
    }

    [Fact]
    public void Manifest_Parse_AllowsCommentsAndBom()
    {
        var info = ExtensionManifest.Parse("﻿{ // yorum\n \"name\": \"X\", \"version\": \"1\", }");
        Assert.Equal("X", info!.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{\"version\":\"1\"}")]
    public void Manifest_Parse_ReturnsNull_ForBadInput(string? json)
    {
        Assert.Null(ExtensionManifest.Parse(json));
    }

    [Fact]
    public void ResolveName_PlainName_Unchanged()
    {
        Assert.Equal("Plain", ExtensionManifest.ResolveName("Plain", _ => "never"));
    }

    [Fact]
    public void ResolveName_UsesLookup_AndFallsBackToRawName()
    {
        Assert.Equal("Çeviri", ExtensionManifest.ResolveName("__MSG_appName__", k => k == "appName" ? "Çeviri" : null));
        Assert.Equal("__MSG_x__", ExtensionManifest.ResolveName("__MSG_x__", _ => null));
    }

    [Fact]
    public void LookupMessage_IsCaseInsensitive()
    {
        var json = "{\"AppName\":{\"message\":\"Demo\"},\"other\":{\"message\":\"Y\"}}";
        Assert.Equal("Demo", ExtensionManifest.LookupMessage(json, "appname"));
        Assert.Null(ExtensionManifest.LookupMessage(json, "missing"));
        Assert.Null(ExtensionManifest.LookupMessage("bad", "appname"));
    }

    [Fact]
    public void Firefox_Parse_KeepsOnlyUserExtensions_AndReadsState()
    {
        var json = "{\"addons\":["
            + "{\"id\":\"a@x\",\"type\":\"extension\",\"location\":\"app-profile\",\"version\":\"1.0\",\"active\":true,\"defaultLocale\":{\"name\":\"Alpha\"}},"
            + "{\"id\":\"b@x\",\"type\":\"extension\",\"location\":\"app-profile\",\"active\":true,\"userDisabled\":true},"
            + "{\"id\":\"theme@x\",\"type\":\"theme\",\"location\":\"app-profile\",\"active\":true},"
            + "{\"id\":\"sys@x\",\"type\":\"extension\",\"location\":\"app-builtin\",\"active\":true}"
            + "]}";

        var addons = FirefoxAddons.Parse(json);

        Assert.Equal(2, addons.Count);
        Assert.Equal("Alpha", addons[0].Name);
        Assert.True(addons[0].Active);
        Assert.Equal("b@x", addons[1].Name);
        Assert.False(addons[1].Active);
    }

    [Fact]
    public void Firefox_Parse_BadInput_ReturnsEmpty()
    {
        Assert.Empty(FirefoxAddons.Parse(null));
        Assert.Empty(FirefoxAddons.Parse("nope"));
        Assert.Empty(FirefoxAddons.Parse("{}"));
    }

    [Fact]
    public void Filter_SortsByBrowserThenName_AndMatchesQuery()
    {
        var items = new[]
        {
            new BrowserExtension(BrowserKind.Edge, "id2", "Zed", "1", "Default", "p", null),
            new BrowserExtension(BrowserKind.Chrome, "id1", "Beta", "1", "Default", "p", null),
            new BrowserExtension(BrowserKind.Chrome, "id3", "Alpha", "1", "Default", "p", null),
        };

        var all = ExtensionFilter.Apply(items);
        Assert.Equal(new[] { "Alpha", "Beta", "Zed" }, all.Select(x => x.Name).ToArray());

        Assert.Single(ExtensionFilter.Apply(items, "zed"));
        Assert.Equal(2, ExtensionFilter.Apply(items, "chrome").Count);
        Assert.Single(ExtensionFilter.Apply(items, "id3"));
    }
}
