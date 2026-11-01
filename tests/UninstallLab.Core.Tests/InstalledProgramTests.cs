using UninstallLab.Core;
using Xunit;

namespace UninstallLab.Core.Tests;

public class InstalledProgramTests
{
    [Fact]
    public void CanUninstall_FalseWhenNoUninstallString()
    {
        Assert.False(new InstalledProgram("Demo").CanUninstall);
    }

    [Fact]
    public void CanUninstall_TrueWhenQuietStringPresent()
    {
        var p = new InstalledProgram("Demo", QuietUninstallString: "demo.exe /S");
        Assert.True(p.CanUninstall);
    }
}
