using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WinTune.Plugins.Power;
using WinTune.Sdk;
using Xunit;

namespace WinTune.Plugins.Power.Tests;

public sealed class PowerModuleTests
{
    private readonly PowerModule _module = new(NullLogger<PowerModule>.Instance);

    [Fact]
    public void Metadata_HasExpectedId()
    {
        _module.Metadata.Id.Should().Be("wintune.power");
        _module.Metadata.Category.Should().Be(ModuleCategory.Power);
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
    public async Task PreviewAsync_WithFinding_BuildsConfigChangeEntry()
    {
        var finding = Finding.Create(
            moduleId: "wintune.power",
            title: "test",
            description: "desc",
            metadata: new Dictionary<string, object>
            {
                ["targetSchemeGuid"]  = "381b4222-f694-41f0-9685-ff5bb260df2e",
                ["currentSchemeGuid"] = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
            });

        var cs = await _module.PreviewAsync([finding], CancellationToken.None);

        cs.ModuleId.Should().Be("wintune.power");
        cs.Changes.Should().HaveCount(1);
        cs.Changes[0].Kind.Should().Be(ChangeKind.ConfigChange);
        cs.Changes[0].TargetPath.Should().Be("power://scheme");
    }

    [Fact]
    public async Task PreviewAsync_EmptyFindings_ReturnsEmptyChangeSet()
    {
        var cs = await _module.PreviewAsync([], CancellationToken.None);
        cs.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_EmptyChangeSet_ReturnsSuccess()
    {
        var cs = ChangeSet.Empty("wintune.power");
        var result = await _module.ApplyAsync(cs, CancellationToken.None);
        result.Success.Should().BeTrue();
        result.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task RevertAsync_DoesNotThrow()
    {
        var fakeResult = new ApplyResult("cs1", "wintune.power", true, [], [], null, DateTimeOffset.UtcNow);
        var act = async () => await _module.RevertAsync(fakeResult, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
