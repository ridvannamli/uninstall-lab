using UninstallLab.Core;
using Xunit;

namespace UninstallLab.Core.Tests;

public class StartupTests
{
    private sealed class FakeStore : IStartupStore
    {
        public List<StartupEntry> Entries { get; } = new();
        public List<(StartupEntry Entry, bool Enabled)> Sets { get; } = new();
        public Exception? Throw { get; init; }

        public IReadOnlyList<StartupEntry> GetEntries() => Entries;

        public void SetEnabled(StartupEntry entry, bool enabled)
        {
            if (Throw is not null) throw Throw;
            Sets.Add((entry, enabled));
        }
    }

    private static StartupEntry E(string name, bool enabled = true, StartupSource source = StartupSource.CurrentUserRun) =>
        new(name, name + ".exe", source, enabled);

    [Theory]
    [InlineData(null, true)]
    [InlineData(new byte[0], true)]
    [InlineData(new byte[] { 0x02, 0, 0, 0 }, true)]
    [InlineData(new byte[] { 0x06, 0, 0, 0 }, true)]
    [InlineData(new byte[] { 0x03, 0, 0, 0 }, false)]
    [InlineData(new byte[] { 0x07, 0, 0, 0 }, false)]
    public void IsEnabled_DecodesFirstByte(byte[]? data, bool expected)
    {
        Assert.Equal(expected, StartupApproval.IsEnabled(data));
    }

    [Fact]
    public void Encode_Enabled_Is12Bytes_StartsWith02()
    {
        var b = StartupApproval.Encode(true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.Equal(12, b.Length);
        Assert.Equal(0x02, b[0]);
        Assert.True(StartupApproval.IsEnabled(b));
    }

    [Fact]
    public void Encode_Disabled_StartsWith03_AndStoresFileTime()
    {
        var when = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var b = StartupApproval.Encode(false, when);

        Assert.Equal(12, b.Length);
        Assert.Equal(0x03, b[0]);
        Assert.False(StartupApproval.IsEnabled(b));
        Assert.Equal(when.ToFileTimeUtc(), BitConverter.ToInt64(b, 4));
    }

    [Fact]
    public void List_FiltersAndSorts()
    {
        var store = new FakeStore();
        store.Entries.AddRange(new[] { E("zeta"), E("Alpha"), E("Beta") });
        var manager = new StartupManager(store);

        Assert.Equal(new[] { "Alpha", "Beta", "zeta" }, manager.List().Select(e => e.Name).ToArray());
        Assert.Single(manager.List("alp"));
    }

    [Fact]
    public void SetEnabled_NoOp_WhenAlreadyInState()
    {
        var store = new FakeStore();
        var r = new StartupManager(store).SetEnabled(E("a", enabled: true), true);

        Assert.True(r.Success);
        Assert.Empty(store.Sets);
    }

    [Fact]
    public void SetEnabled_CallsStore_WhenChanging()
    {
        var store = new FakeStore();
        var entry = E("a", enabled: true);

        var r = new StartupManager(store).SetEnabled(entry, false);

        Assert.True(r.Success);
        Assert.Single(store.Sets);
        Assert.False(store.Sets[0].Enabled);
    }

    [Fact]
    public void SetEnabled_Failure_ReportsError_WithAdminHintForSystemEntries()
    {
        var store = new FakeStore { Throw = new UnauthorizedAccessException("denied") };
        var manager = new StartupManager(store);

        var user = manager.SetEnabled(E("a"), false);
        var system = manager.SetEnabled(E("b", source: StartupSource.LocalMachineRun), false);

        Assert.False(user.Success);
        Assert.DoesNotContain("yönetici", user.Error);
        Assert.False(system.Success);
        Assert.Contains("yönetici", system.Error);
    }
}
