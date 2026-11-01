using UninstallLab.Core;
using Xunit;

namespace UninstallLab.Core.Tests;

public class LeftoverTests
{
    private sealed class FakeFs : IFileSystemProbe
    {
        private readonly Dictionary<string, string[]> _map;
        public FakeFs(Dictionary<string, string[]> map) => _map = map;
        public IEnumerable<string> EnumerateDirectories(string parent) =>
            _map.TryGetValue(parent, out var v) ? v : Array.Empty<string>();
    }

    private sealed class FakeReg : IRegistryProbe
    {
        private readonly Dictionary<string, string[]> _map;
        public FakeReg(Dictionary<string, string[]> map) => _map = map;
        public IEnumerable<string> EnumerateSubKeys(string keyPath) =>
            _map.TryGetValue(keyPath, out var v) ? v : Array.Empty<string>();
    }

    [Theory]
    [InlineData("Demo App 1.2.3 (x64)", "demo app")]
    [InlineData("7-Zip 23.01 (x64 edition)", "7-zip")]
    [InlineData("Notepad++ (64-bit x64)", "notepad++")]
    [InlineData("  Tool   v2.0 ", "tool")]
    [InlineData("", "")]
    public void Normalize_Works(string input, string expected)
    {
        Assert.Equal(expected, NameNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData(@"C:\Windows", true)]
    [InlineData(@"C:\Windows\System32\Foo", true)]
    [InlineData(@"C:\Program Files", true)]
    [InlineData(@"C:\Program Files (x86)", true)]
    [InlineData(@"C:\ProgramData\Microsoft", true)]
    [InlineData(@"C:\Users\x\AppData\Roaming\Microsoft", true)]
    [InlineData(@"HKLM\SOFTWARE\Microsoft", true)]
    [InlineData(@"HKLM\SOFTWARE\WOW6432Node\Microsoft", true)]
    [InlineData(@"HKLM\SYSTEM\CurrentControlSet", true)]
    [InlineData(@"HKCU\Software", true)]
    [InlineData(@"C:\Program Files\Demo", false)]
    [InlineData(@"HKCU\Software\Demo", false)]
    public void ProtectedPaths_Detects(string path, bool expected)
    {
        Assert.Equal(expected, ProtectedPaths.IsProtected(path));
    }

    [Fact]
    public void Scan_FindsFolderAndRegistry_ByName_WithLowRisk()
    {
        var fs = new FakeFs(new()
        {
            [@"C:\Program Files"] = new[] { @"C:\Program Files\Demo App", @"C:\Program Files\Other" },
        });
        var reg = new FakeReg(new()
        {
            [@"HKCU\Software"] = new[] { @"HKCU\Software\Demo App", @"HKCU\Software\Else" },
        });
        var loc = new ScanLocations(new[] { @"C:\Program Files" }, new[] { @"HKCU\Software" });

        var items = new LeftoverScanner(fs, reg, loc).Scan(new InstalledProgram("Demo App 1.0"));

        Assert.Equal(2, items.Count);
        Assert.All(items, i => Assert.True(i.SafeByDefault));
        Assert.Contains(items, i => i.Kind == LeftoverKind.Folder);
        Assert.Contains(items, i => i.Kind == LeftoverKind.RegistryKey);
    }

    [Fact]
    public void Scan_PublisherMatch_IsHighRisk_NotSafeByDefault()
    {
        var fs = new FakeFs(new() { [@"C:\ProgramData"] = new[] { @"C:\ProgramData\Acme" } });
        var loc = new ScanLocations(new[] { @"C:\ProgramData" }, Array.Empty<string>());

        var items = new LeftoverScanner(fs, new FakeReg(new()), loc)
            .Scan(new InstalledProgram("Editor", Publisher: "Acme"));

        Assert.Single(items);
        Assert.False(items[0].SafeByDefault);
    }

    [Fact]
    public void Scan_SkipsProtectedPaths_EvenIfNameMatches()
    {
        var reg = new FakeReg(new() { [@"HKLM\SOFTWARE"] = new[] { @"HKLM\SOFTWARE\Microsoft" } });
        var loc = new ScanLocations(Array.Empty<string>(), new[] { @"HKLM\SOFTWARE" });

        var items = new LeftoverScanner(new FakeFs(new()), reg, loc)
            .Scan(new InstalledProgram("Microsoft"));

        Assert.Empty(items);
    }

    [Fact]
    public void Scan_IgnoresVeryShortNames()
    {
        var fs = new FakeFs(new() { [@"C:\Program Files"] = new[] { @"C:\Program Files\Ab" } });
        var loc = new ScanLocations(new[] { @"C:\Program Files" }, Array.Empty<string>());

        Assert.Empty(new LeftoverScanner(fs, new FakeReg(new()), loc).Scan(new InstalledProgram("Ab")));
    }
}
