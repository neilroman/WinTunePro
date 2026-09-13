using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WinTune.Plugins.Network;
using WinTune.Sdk;
using Xunit;

namespace WinTune.Plugins.Network.Tests;

public sealed class NetworkModuleTests
{
    private readonly NetworkModule _module = new(NullLogger<NetworkModule>.Instance);

    [Fact]
    public void Metadata_HasExpectedId()
    {
        _module.Metadata.Id.Should().Be("wintune.network");
        _module.Metadata.Category.Should().Be(ModuleCategory.Network);
    }

    [Fact]
    public void Metadata_DisplayName_IsRed()
    {
        _module.Metadata.DisplayName.Should().Be("Red");
    }

    [Fact]
    public void RequestedCapabilities_ContainsReadNetworkConfig()
    {
        _module.RequestedCapabilities.Should().Contain(c => c.Name == WellKnownCapabilities.ReadNetworkConfig);
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
    public async Task PreviewAsync_AlwaysReturnsEmptyChangeSet()
    {
        var findings = new List<Finding>
        {
            Finding.Create(
                moduleId: "wintune.network",
                title: "Test finding",
                description: "test",
                estimatedBytesSaved: 0,
                metadata: new Dictionary<string, object>())
        };

        var cs = await _module.PreviewAsync(findings, CancellationToken.None);

        cs.ModuleId.Should().Be("wintune.network");
        cs.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_AlwaysReturnsSuccess()
    {
        var cs = ChangeSet.Empty("wintune.network");

        var result = await _module.ApplyAsync(cs, CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RevertAsync_DoesNotThrow()
    {
        var fakeResult = new ApplyResult(
            "cs1", "wintune.network", true, [], [], null, DateTimeOffset.UtcNow);
        var act = async () => await _module.RevertAsync(fakeResult, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
