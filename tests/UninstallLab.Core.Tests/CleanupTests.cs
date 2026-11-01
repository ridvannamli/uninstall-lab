using UninstallLab.Core;
using Xunit;

namespace UninstallLab.Core.Tests;

public class CleanupTests
{
    private const string Root = @"C:\Backups";

    private sealed class FakeSystem : ICleanupSystem
    {
        public List<string> Calls { get; } = new();
        public Dictionary<string, string> Files { get; } = new();
        public HashSet<string> Dirs { get; } = new();
        public HashSet<string> Existing { get; } = new();
        public bool FailExport { get; set; }

        public void CreateDirectory(string path) { Calls.Add("mkdir " + path); Dirs.Add(path); }
        public bool DirectoryExists(string path) => Existing.Contains(path);
        public void MoveDirectory(string source, string destination) => Calls.Add($"move {source}->{destination}");
        public void ExportRegistryKey(string keyPath, string destinationFile)
        {
            if (FailExport) throw new IOException("export failed");
            Calls.Add("export " + keyPath);
        }
        public void DeleteRegistryKey(string keyPath) => Calls.Add("delete " + keyPath);
        public void ImportRegistryFile(string file) => Calls.Add("import " + file);
        public void WriteAllText(string path, string text) => Files[path] = text;
        public string ReadAllText(string path) =>
            Files.TryGetValue(path, out var t) ? t : throw new FileNotFoundException(path);
        public IEnumerable<string> ListDirectoryNames(string path) =>
            Dirs.Where(d => d.StartsWith(path + "\\"))
                .Select(d => d[(path.Length + 1)..])
                .Where(n => !n.Contains('\\'));
    }

    private sealed class FakeRestore : IRestorePoint
    {
        public int Calls { get; private set; }
        public bool TryCreate(string description) { Calls++; return true; }
    }

    private static readonly DateTime Now = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);

    private static CleanupExecutor Make(FakeSystem sys, IRestorePoint? rp = null) =>
        new(sys, Root, () => Now, rp);

    private static LeftoverItem Folder(string p, int risk = 20) => new(LeftoverKind.Folder, p, risk, "t");
    private static LeftoverItem Key(string p, int risk = 20) => new(LeftoverKind.RegistryKey, p, risk, "t");

    [Fact]
    public void DryRun_TouchesNothing()
    {
        var sys = new FakeSystem();
        var r = Make(sys).Execute(new[] { Folder(@"C:\ProgramData\Demo"), Key(@"HKCU\Software\Demo") }, dryRun: true);

        Assert.True(r.DryRun);
        Assert.Equal(2, r.Done.Count);
        Assert.Empty(sys.Calls);
        Assert.Null(r.SessionId);
    }

    [Fact]
    public void Execute_MovesFolder_ExportsThenDeletesKey_AndWritesManifest()
    {
        var sys = new FakeSystem();
        var r = Make(sys).Execute(new[] { Folder(@"C:\ProgramData\Demo"), Key(@"HKCU\Software\Demo") }, dryRun: false);

        Assert.Equal("20261101-100000", r.SessionId);
        Assert.Empty(r.Failed);
        Assert.Contains(sys.Calls, c => c.StartsWith(@"move C:\ProgramData\Demo->"));
        Assert.True(sys.Calls.IndexOf(@"export HKCU\Software\Demo") < sys.Calls.IndexOf(@"delete HKCU\Software\Demo"));
        Assert.True(sys.Files.ContainsKey(@"C:\Backups\20261101-100000\manifest.json"));
    }

    [Fact]
    public void Execute_SkipsProtectedAndRisky_ByDefault()
    {
        var sys = new FakeSystem();
        var items = new[] { Folder(@"C:\Windows"), Folder(@"C:\ProgramData\Acme", risk: 60) };

        var r = Make(sys).Execute(items, dryRun: false);

        Assert.Equal(2, r.Skipped.Count);
        Assert.Empty(sys.Calls);
    }

    [Fact]
    public void Execute_IncludeRisky_ProcessesRiskyButStillSkipsProtected()
    {
        var sys = new FakeSystem();
        var items = new[] { Folder(@"C:\Windows"), Folder(@"C:\ProgramData\Acme", risk: 60) };

        var r = Make(sys).Execute(items, dryRun: false, includeRisky: true);

        Assert.Single(r.Skipped);
        Assert.Single(r.Done);
    }

    [Fact]
    public void Execute_ExportFails_DoesNotDeleteKey()
    {
        var sys = new FakeSystem { FailExport = true };
        var r = Make(sys).Execute(new[] { Key(@"HKCU\Software\Demo") }, dryRun: false);

        Assert.Single(r.Failed);
        Assert.DoesNotContain(sys.Calls, c => c.StartsWith("delete"));
    }

    [Fact]
    public void Execute_CreatesRestorePointOnce()
    {
        var rp = new FakeRestore();
        var r = Make(new FakeSystem(), rp)
            .Execute(new[] { Folder(@"C:\ProgramData\A1x"), Folder(@"C:\ProgramData\B2x") }, dryRun: false);

        Assert.Equal(1, rp.Calls);
        Assert.True(r.RestorePointCreated);
    }

    [Fact]
    public void Undo_RestoresFolderAndKey()
    {
        var sys = new FakeSystem();
        var exec = Make(sys);
        var r = exec.Execute(new[] { Folder(@"C:\ProgramData\Demo"), Key(@"HKCU\Software\Demo") }, dryRun: false);
        sys.Calls.Clear();

        var undo = exec.Undo(r.SessionId!);

        Assert.Equal(2, undo.Restored);
        Assert.Empty(undo.Failed);
        Assert.Contains(sys.Calls, c => c.EndsWith(@"->C:\ProgramData\Demo"));
        Assert.Contains(sys.Calls, c => c.StartsWith("import "));
    }

    [Fact]
    public void Undo_SkipsFolder_WhenOriginalAlreadyExists()
    {
        var sys = new FakeSystem();
        var exec = Make(sys);
        var r = exec.Execute(new[] { Folder(@"C:\ProgramData\Demo") }, dryRun: false);
        sys.Existing.Add(@"C:\ProgramData\Demo");

        var undo = exec.Undo(r.SessionId!);

        Assert.Equal(0, undo.Restored);
        Assert.Single(undo.Failed);
    }

    [Fact]
    public void Undo_MissingManifest_ReturnsFailure()
    {
        var undo = Make(new FakeSystem()).Undo("nope");
        Assert.Equal(0, undo.Restored);
        Assert.Single(undo.Failed);
    }

    [Fact]
    public void LatestSessionId_ReturnsNewest()
    {
        var sys = new FakeSystem();
        sys.Dirs.Add(@"C:\Backups\20260101-000000");
        sys.Dirs.Add(@"C:\Backups\20261101-100000");
        Assert.Equal("20261101-100000", Make(sys).LatestSessionId());
    }
}
