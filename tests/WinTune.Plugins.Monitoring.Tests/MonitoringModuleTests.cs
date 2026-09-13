using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WinTune.Plugins.Monitoring;
using WinTune.Sdk;
using Xunit;

namespace WinTune.Plugins.Monitoring.Tests;

public sealed class MonitoringModuleTests
{
    private readonly MonitoringModule _module = new(NullLogger<MonitoringModule>.Instance);

    [Fact]
    public void Metadata_HasExpectedId()
    {
        _module.Metadata.Id.Should().Be("wintune.monitoring");
        _module.Metadata.Category.Should().Be(ModuleCategory.Monitoring);
    }

    [Fact]
    public void RequestedCapabilities_NotEmpty()
    {
        _module.RequestedCapabilities.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ScanAsync_DoesNotThrow()
    {
        var act = async () => await _module.ScanAsync(new ScanContext(), CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ScanAsync_ReturnsNonNullList()
    {
        var findings = await _module.ScanAsync(new ScanContext(), CancellationToken.None);
        findings.Should().NotBeNull();
    }

    [Fact]
    public async Task ScanAsync_AllFindingsBelongToModule()
    {
        var findings = await _module.ScanAsync(new ScanContext(), CancellationToken.None);
        findings.Should().AllSatisfy(f => f.ModuleId.Should().Be("wintune.monitoring"));
    }

    [Fact]
    public async Task PreviewAsync_WithFinding_BuildsKillProcessChange()
    {
        var finding = Finding.Create(
            moduleId: "wintune.monitoring",
            title: "test",
            description: "desc",
            metadata: new Dictionary<string, object> { ["pid"] = 9999, ["processName"] = "testproc" });

        var cs = await _module.PreviewAsync([finding], CancellationToken.None);

        cs.ModuleId.Should().Be("wintune.monitoring");
        cs.Changes.Should().HaveCount(1);
        cs.Changes[0].Kind.Should().Be(ChangeKind.KillProcess);
    }

    [Fact]
    public async Task PreviewAsync_EmptyFindings_ReturnsEmpty()
    {
        var cs = await _module.PreviewAsync([], CancellationToken.None);
        cs.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_EmptyChangeSet_ReturnsSuccess()
    {
        var cs = ChangeSet.Empty("wintune.monitoring");
        var result = await _module.ApplyAsync(cs, CancellationToken.None);
        result.Success.Should().BeTrue();
        result.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task RevertAsync_DoesNotThrow()
    {
        var fakeResult = new ApplyResult("cs1", "wintune.monitoring", true, [], [], null, DateTimeOffset.UtcNow);
        var act = async () => await _module.RevertAsync(fakeResult, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
