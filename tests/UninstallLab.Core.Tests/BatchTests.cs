using UninstallLab.Core;
using Xunit;

namespace UninstallLab.Core.Tests;

public class BatchTests
{
    private sealed class ScriptedRunner : IProcessRunner
    {
        private readonly CancellationTokenSource? _cancelAfterFirst;
        public List<string> Ran { get; } = new();

        public ScriptedRunner(CancellationTokenSource? cancelAfterFirst = null) => _cancelAfterFirst = cancelAfterFirst;

        public Task<int> RunAsync(UninstallCommand command, CancellationToken cancellationToken = default)
        {
            Ran.Add(command.FileName);
            _cancelAfterFirst?.Cancel();
            return Task.FromResult(command.FileName == "bad.exe" ? 1603 : 0);
        }
    }

    private sealed class ListProgress : IProgress<BatchProgress>
    {
        public List<BatchProgress> Reports { get; } = new();
        public void Report(BatchProgress value) => Reports.Add(value);
    }

    private static InstalledProgram P(string exe) => new(exe, UninstallString: exe);

    [Fact]
    public async Task Run_ContinuesAfterFailure()
    {
        var runner = new ScriptedRunner();
        var batch = new BatchUninstaller(new UninstallService(runner));

        var results = await batch.RunAsync(new[] { P("a.exe"), P("bad.exe"), P("c.exe") }, quiet: true, dryRun: false);

        Assert.Equal(new[] { true, false, true }, results.Select(r => r.Result.Success).ToArray());
        Assert.Equal(3, runner.Ran.Count);
    }

    [Fact]
    public async Task Run_DryRun_RunsNothing()
    {
        var runner = new ScriptedRunner();
        var batch = new BatchUninstaller(new UninstallService(runner));

        var results = await batch.RunAsync(new[] { P("a.exe"), P("b.exe") }, quiet: false, dryRun: true);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.Result.DryRun));
        Assert.Empty(runner.Ran);
    }

    [Fact]
    public async Task Run_Cancelled_ReturnsPartialResults()
    {
        using var cts = new CancellationTokenSource();
        var runner = new ScriptedRunner(cts);
        var batch = new BatchUninstaller(new UninstallService(runner));

        var results = await batch.RunAsync(new[] { P("a.exe"), P("b.exe"), P("c.exe") }, false, false, null, cts.Token);

        Assert.Single(results);
        Assert.Single(runner.Ran);
    }

    [Fact]
    public async Task Run_ReportsProgressInOrder()
    {
        var progress = new ListProgress();
        var batch = new BatchUninstaller(new UninstallService(new ScriptedRunner()));

        await batch.RunAsync(new[] { P("a.exe"), P("b.exe") }, false, true, progress);

        Assert.Equal(new[] { 1, 2 }, progress.Reports.Select(r => r.Index).ToArray());
        Assert.All(progress.Reports, r => Assert.Equal(2, r.Total));
    }

    [Fact]
    public async Task Run_EmptyList_ReturnsEmpty()
    {
        var batch = new BatchUninstaller(new UninstallService(new ScriptedRunner()));
        Assert.Empty(await batch.RunAsync(Array.Empty<InstalledProgram>(), false, false));
    }
}
