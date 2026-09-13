using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WinTune.Plugins.Privacy;
using WinTune.Sdk;
using Xunit;

namespace WinTune.Plugins.Privacy.Tests;

public sealed class PrivacyModuleTests
{
    private readonly PrivacyModule _module = new(NullLogger<PrivacyModule>.Instance);

    [Fact]
    public void Metadata_HasExpectedId()
    {
        _module.Metadata.Id.Should().Be("wintune.privacy");
        _module.Metadata.Category.Should().Be(ModuleCategory.Privacy);
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
    public async Task PreviewAsync_WithRegistryFinding_BuildsRegistrySetChange()
    {
        var finding = Finding.Create(
            moduleId: "wintune.privacy",
            title: "test",
            description: "desc",
            metadata: new Dictionary<string, object>
            {
                ["registryKey"] = @"HKLM\SOFTWARE\WinTune\Test",
                ["valueName"]   = "TestVal",
                ["targetValue"] = 0,
            });

        var cs = await _module.PreviewAsync([finding], CancellationToken.None);

        cs.ModuleId.Should().Be("wintune.privacy");
        cs.Changes.Should().HaveCount(1);
        cs.Changes[0].Kind.Should().Be(ChangeKind.RegistrySet);
    }

    [Fact]
    public async Task PreviewAsync_FindingWithoutRegistryKey_DoesNotThrow()
    {
        var finding = Finding.Create("wintune.privacy", "test", "desc");
        var act = async () => await _module.PreviewAsync([finding], CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ApplyAsync_EmptyChangeSet_ReturnsSuccess()
    {
        var cs = ChangeSet.Empty("wintune.privacy");
        var result = await _module.ApplyAsync(cs, CancellationToken.None);
        result.Success.Should().BeTrue();
        result.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task RevertAsync_DoesNotThrow()
    {
        var fakeResult = new ApplyResult("cs1", "wintune.privacy", true, [], [], null, DateTimeOffset.UtcNow);
        var act = async () => await _module.RevertAsync(fakeResult, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
