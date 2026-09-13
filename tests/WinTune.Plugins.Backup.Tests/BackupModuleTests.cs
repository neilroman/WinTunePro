using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WinTune.Plugins.Backup;
using WinTune.Sdk;
using Xunit;

namespace WinTune.Plugins.Backup.Tests;

public sealed class BackupModuleTests
{
    private readonly BackupModule _module = new(NullLogger<BackupModule>.Instance);

    [Fact]
    public void Metadata_HasExpectedId()
    {
        _module.Metadata.Id.Should().Be("wintune.backup");
        _module.Metadata.Category.Should().Be(ModuleCategory.Backup);
    }

    [Fact]
    public void RequestedCapabilities_NotEmpty()
    {
        _module.RequestedCapabilities.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ScanAsync_DoesNotThrow()
    {
        // WMI may not be available in CI; module handles this gracefully
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
        var finding = Finding.Create("wintune.backup", "t", "d");
        var cs = await _module.PreviewAsync([finding], CancellationToken.None);

        cs.ModuleId.Should().Be("wintune.backup");
        cs.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_AlwaysReturnsFailure()
    {
        var cs = ChangeSet.Empty("wintune.backup");
        var result = await _module.ApplyAsync(cs, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Failed.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RevertAsync_DoesNotThrow()
    {
        var fakeResult = new ApplyResult("cs1", "wintune.backup", true, [], [], null, DateTimeOffset.UtcNow);
        var act = async () => await _module.RevertAsync(fakeResult, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
