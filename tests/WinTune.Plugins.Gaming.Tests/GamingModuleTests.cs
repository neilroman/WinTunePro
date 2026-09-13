using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WinTune.Plugins.Gaming;
using WinTune.Sdk;
using Xunit;

namespace WinTune.Plugins.Gaming.Tests;

public sealed class GamingModuleTests
{
    private readonly GamingModule _module = new(NullLogger<GamingModule>.Instance);

    [Fact]
    public void Metadata_HasExpectedId()
    {
        _module.Metadata.Id.Should().Be("wintune.gaming");
        _module.Metadata.Category.Should().Be(ModuleCategory.Gaming);
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
    public async Task ScanAsync_ReturnsListOfFindings()
    {
        var findings = await _module.ScanAsync(new ScanContext(), CancellationToken.None);
        findings.Should().NotBeNull();
        findings.Should().AllSatisfy(f =>
        {
            f.ModuleId.Should().Be("wintune.gaming");
            f.Id.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task PreviewAsync_WithFindingHavingRegistryKey_BuildsRegistrySetChange()
    {
        var finding = Finding.Create(
            moduleId: "wintune.gaming",
            title: "test",
            description: "desc",
            estimatedBytesSaved: 0,
            metadata: new Dictionary<string, object>
            {
                ["registryKey"] = @"HKCU\Software\WinTune\Test",
                ["valueName"] = "TestVal",
                ["recommendedValue"] = 1
            });

        var cs = await _module.PreviewAsync([finding], CancellationToken.None);

        cs.ModuleId.Should().Be("wintune.gaming");
        cs.Changes.Count.Should().BeGreaterThanOrEqualTo(1);
        cs.Changes[0].Kind.Should().Be(ChangeKind.RegistrySet);
        cs.Changes[0].TargetPath.Should().Be(@"HKCU\Software\WinTune\Test");
    }

    [Fact]
    public async Task PreviewAsync_WithNoRegistryKeyFindings_ReturnsEmptyChangeSet()
    {
        var finding = Finding.Create(
            moduleId: "wintune.gaming",
            title: "test",
            description: "desc",
            estimatedBytesSaved: 0,
            metadata: new Dictionary<string, object>());

        var cs = await _module.PreviewAsync([finding], CancellationToken.None);

        cs.ModuleId.Should().Be("wintune.gaming");
        cs.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_EmptyChangeSet_ReturnsSuccess()
    {
        var cs = ChangeSet.Empty("wintune.gaming");

        var result = await _module.ApplyAsync(cs, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task RevertAsync_DoesNotThrow()
    {
        var fakeResult = new ApplyResult(
            "cs1", "wintune.gaming", true, [], [], null, DateTimeOffset.UtcNow);
        var act = async () => await _module.RevertAsync(fakeResult, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
