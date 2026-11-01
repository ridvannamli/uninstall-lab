using UninstallLab.Core;
using Xunit;

namespace UninstallLab.Core.Tests;

public class LeftoverPlannerTests
{
    private static LeftoverItem Folder(string path, int risk) => new(LeftoverKind.Folder, path, risk, "t");

    private static ProgramScan Scan(string name, bool stillInstalled, params LeftoverItem[] items) =>
        new(new InstalledProgram(name), items, stillInstalled);

    [Fact]
    public void AutoCleanSet_IncludesOnlySafeItems_OfRemovedPrograms()
    {
        var scans = new[]
        {
            Scan("Gone", false, Folder(@"C:\ProgramData\Gone", 20), Folder(@"C:\ProgramData\Acme", 60)),
        };

        var auto = LeftoverPlanner.AutoCleanSet(scans);

        Assert.Single(auto);
        Assert.Equal(@"C:\ProgramData\Gone", auto[0].Path);
    }

    [Fact]
    public void AutoCleanSet_NeverTouchesStillInstalledPrograms()
    {
        var scans = new[]
        {
            Scan("Stays", true, Folder(@"C:\Program Files\Stays", 10)),
        };

        Assert.Empty(LeftoverPlanner.AutoCleanSet(scans));
    }

    [Fact]
    public void AutoCleanSet_ExcludesPathsAlsoOwnedByStillInstalledProgram()
    {
        var scans = new[]
        {
            Scan("Gone", false, Folder(@"C:\ProgramData\Shared", 20), Folder(@"C:\ProgramData\Gone", 20)),
            Scan("Stays", true, Folder(@"c:\programdata\shared", 20)),
        };

        var auto = LeftoverPlanner.AutoCleanSet(scans);

        Assert.Single(auto);
        Assert.Equal(@"C:\ProgramData\Gone", auto[0].Path);
    }

    [Fact]
    public void AutoCleanSet_RemovesDuplicatesAcrossPrograms()
    {
        var scans = new[]
        {
            Scan("A", false, Folder(@"C:\ProgramData\Both", 20)),
            Scan("B", false, Folder(@"C:\ProgramData\both", 20)),
        };

        Assert.Single(LeftoverPlanner.AutoCleanSet(scans));
    }

    [Fact]
    public void Combine_DeduplicatesKeepingLowestRisk_AndSorts()
    {
        var scans = new[]
        {
            Scan("A", false, Folder(@"C:\ProgramData\Zed", 20), Folder(@"C:\ProgramData\Dup", 60)),
            Scan("B", true, Folder(@"C:\ProgramData\Dup", 10)),
        };

        var all = LeftoverPlanner.Combine(scans);

        Assert.Equal(2, all.Count);
        Assert.Equal(10, all[0].Risk);
        Assert.Equal(@"C:\ProgramData\Dup", all[0].Path);
        Assert.Equal(@"C:\ProgramData\Zed", all[1].Path);
    }

    [Fact]
    public void AutoCleanSet_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(LeftoverPlanner.AutoCleanSet(Array.Empty<ProgramScan>()));
    }
}
