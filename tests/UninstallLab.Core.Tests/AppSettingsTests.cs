using UninstallLab.Core;
using Xunit;

namespace UninstallLab.Core.Tests;

public class AppSettingsTests
{
    [Fact]
    public void RoundTrip_PreservesValues()
    {
        var original = new AppSettings(QuietByDefault: true, ShowSystemStoreApps: true);

        var restored = AppSettingsSerializer.Deserialize(AppSettingsSerializer.Serialize(original));

        Assert.Equal(original, restored);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    public void Deserialize_BadInput_ReturnsDefaults(string? json)
    {
        Assert.Equal(AppSettings.Default, AppSettingsSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_MissingProperties_UseDefaults()
    {
        var s = AppSettingsSerializer.Deserialize("{\"QuietByDefault\":true}");

        Assert.True(s.QuietByDefault);
        Assert.False(s.ShowSystemStoreApps);
    }

    [Fact]
    public void Deserialize_Bom_IsIgnored()
    {
        Assert.True(AppSettingsSerializer.Deserialize("﻿{\"ShowSystemStoreApps\":true}").ShowSystemStoreApps);
    }
}
