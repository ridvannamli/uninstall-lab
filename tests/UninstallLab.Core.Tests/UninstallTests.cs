using UninstallLab.Core;
using Xunit;

namespace UninstallLab.Core.Tests;

public class UninstallTests
{
    private sealed class FakeRunner : IProcessRunner
    {
        public int ExitCode { get; init; }
        public Exception? Throw { get; init; }
        public int Calls { get; private set; }

        public Task<int> RunAsync(UninstallCommand command, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Throw is not null) throw Throw;
            return Task.FromResult(ExitCode);
        }
    }

    private static InstalledProgram Prog(string? uninstall = null, string? quiet = null) =>
        new("Demo", UninstallString: uninstall, QuietUninstallString: quiet);

    [Fact]
    public void Build_ReturnsNull_WhenNoString()
    {
        Assert.Null(UninstallCommandBuilder.Build(Prog(), quiet: false));
    }

    [Fact]
    public void Build_ParsesQuotedPath_WithArgs()
    {
        var c = UninstallCommandBuilder.Build(Prog("\"C:\\Program Files\\Demo\\unins000.exe\" /SILENT"), false);
        Assert.NotNull(c);
        Assert.Equal("C:\\Program Files\\Demo\\unins000.exe", c!.FileName);
        Assert.Equal("/SILENT", c.Arguments);
        Assert.False(c.IsMsi);
    }

    [Fact]
    public void Build_ParsesUnquotedPathWithSpaces()
    {
        var c = UninstallCommandBuilder.Build(Prog("C:\\Program Files\\Demo\\uninst.exe /S"), false);
        Assert.Equal("C:\\Program Files\\Demo\\uninst.exe", c!.FileName);
        Assert.Equal("/S", c.Arguments);
    }

    [Fact]
    public void Build_PrefersQuietString_WhenQuietRequested()
    {
        var c = UninstallCommandBuilder.Build(Prog("a.exe", "b.exe /S"), quiet: true);
        Assert.Equal("b.exe", c!.FileName);
    }

    [Fact]
    public void Build_FallsBackToNormalString_WhenNoQuietString()
    {
        var c = UninstallCommandBuilder.Build(Prog("a.exe /u"), quiet: true);
        Assert.Equal("a.exe", c!.FileName);
    }

    [Fact]
    public void Build_Msi_ConvertsInstallToUninstall_AndAddsQuietFlags()
    {
        var c = UninstallCommandBuilder.Build(Prog("MsiExec.exe /I{ABC-123}"), quiet: true);
        Assert.True(c!.IsMsi);
        Assert.Contains("/X{ABC-123}", c.Arguments);
        Assert.Contains("/qn", c.Arguments);
        Assert.Contains("/norestart", c.Arguments);
    }

    [Fact]
    public void Build_Msi_NotQuiet_DoesNotAddFlags()
    {
        var c = UninstallCommandBuilder.Build(Prog("MsiExec.exe /X{ABC-123}"), quiet: false);
        Assert.True(c!.IsMsi);
        Assert.Equal("/X{ABC-123}", c.Arguments);
    }

    [Fact]
    public async Task Uninstall_DryRun_DoesNotRunProcess()
    {
        var runner = new FakeRunner();
        var r = await new UninstallService(runner).UninstallAsync(Prog("a.exe"), false, dryRun: true);
        Assert.True(r.Success);
        Assert.True(r.DryRun);
        Assert.Equal(0, runner.Calls);
    }

    [Fact]
    public async Task Uninstall_NoCommand_Fails()
    {
        var r = await new UninstallService(new FakeRunner()).UninstallAsync(Prog(), false, dryRun: false);
        Assert.False(r.Success);
        Assert.NotNull(r.Error);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(3010, true)]
    [InlineData(1641, true)]
    [InlineData(1603, false)]
    public async Task Uninstall_MapsExitCodes(int code, bool expected)
    {
        var runner = new FakeRunner { ExitCode = code };
        var r = await new UninstallService(runner).UninstallAsync(Prog("a.exe"), false, dryRun: false);
        Assert.Equal(expected, r.Success);
        Assert.Equal(code, r.ExitCode);
        Assert.Equal(1, runner.Calls);
    }

    [Fact]
    public async Task Uninstall_RunnerException_ReturnsFailure()
    {
        var runner = new FakeRunner { Throw = new InvalidOperationException("boom") };
        var r = await new UninstallService(runner).UninstallAsync(Prog("a.exe"), false, dryRun: false);
        Assert.False(r.Success);
        Assert.Equal("boom", r.Error);
    }
}
