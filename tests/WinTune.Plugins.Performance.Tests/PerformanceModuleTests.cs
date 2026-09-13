using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WinTune.Plugins.Performance;
using WinTune.Sdk;
using Xunit;

namespace WinTune.Plugins.Performance.Tests;

public sealed class PerformanceModuleTests
{
    private readonly PerformanceModule _module = new(NullLogger<PerformanceModule>.Instance);

    [Fact]
    public void Metadata_HasExpectedId()
    {
        _module.Metadata.Id.Should().Be("wintune.performance");
        _module.Metadata.Category.Should().Be(ModuleCategory.Performance);
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
    public async Task PreviewAsync_WithRegistryFinding_BuildsRegistryDeleteChange()
    {
        var finding = Finding.Create(
            moduleId: "wintune.performance",
            title: "test",
            description: "desc",
            severity: FindingSeverity.Medium,
            metadata: new Dictionary<string, object>
            {
                ["registryKey"] = @"HKLM\SOFTWARE\WinTune\Test",
                ["valueName"]   = "TestVal",
            });

        var cs = await _module.PreviewAsync([finding], CancellationToken.None);

        cs.ModuleId.Should().Be("wintune.performance");
        cs.Changes.Should().HaveCount(1);
        cs.Changes[0].Kind.Should().Be(ChangeKind.RegistryDelete);
    }

    [Fact]
    public async Task PreviewAsync_LowSeverityFinding_ExcludedFromChangeSet()
    {
        var finding = Finding.Create(
            moduleId: "wintune.performance",
            title: "test",
            description: "desc",
            severity: FindingSeverity.Low,
            metadata: new Dictionary<string, object>
            {
                ["registryKey"] = @"HKLM\SOFTWARE\WinTune\Test",
                ["valueName"]   = "TestVal",
            });

        var cs = await _module.PreviewAsync([finding], CancellationToken.None);

        cs.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_EmptyChangeSet_ReturnsSuccess()
    {
        var cs = ChangeSet.Empty("wintune.performance");
        var result = await _module.ApplyAsync(cs, CancellationToken.None);
        result.Success.Should().BeTrue();
        result.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task RevertAsync_DoesNotThrow()
    {
        var fakeResult = new ApplyResult("cs1", "wintune.performance", true, [], [], null, DateTimeOffset.UtcNow);
        var act = async () => await _module.RevertAsync(fakeResult, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
