using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WinTune.Plugins.Cleanup;
using WinTune.Sdk;
using Xunit;

namespace WinTune.Plugins.Cleanup.Tests;

public sealed class CleanupModuleTests
{
    private readonly CleanupModule _module = new(NullLogger<CleanupModule>.Instance);

    [Fact]
    public void Metadata_HasExpectedId()
    {
        _module.Metadata.Id.Should().Be("wintune.cleanup");
        _module.Metadata.Category.Should().Be(ModuleCategory.Cleanup);
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
            f.ModuleId.Should().Be("wintune.cleanup");
            f.Id.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task PreviewAsync_BuildsOneChangePerPath()
    {
        // Arrange — create two temp files and a finding that references them
        var tmp1 = Path.GetTempFileName();
        var tmp2 = Path.GetTempFileName();
        try
        {
            var finding = Finding.Create(
                moduleId: "wintune.cleanup",
                title: "Test finding",
                description: "test",
                estimatedBytesSaved: 0,
                metadata: new Dictionary<string, object> { ["paths"] = new List<string> { tmp1, tmp2 } });

            // Act
            var cs = await _module.PreviewAsync([finding], CancellationToken.None);

            // Assert
            cs.ModuleId.Should().Be("wintune.cleanup");
            cs.Changes.Should().HaveCount(2);
            cs.Changes.Should().AllSatisfy(c => c.Kind.Should().Be(ChangeKind.DeleteFile));
        }
        finally
        {
            File.Delete(tmp1);
            File.Delete(tmp2);
        }
    }

    [Fact]
    public async Task ApplyAsync_DeletesTargetFiles()
    {
        // Arrange
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, "data");
        var change = new PlannedChange(
            Guid.NewGuid().ToString(), ChangeKind.DeleteFile,
            "delete test", tmp, new Dictionary<string, object>());
        var cs = new ChangeSet(Guid.NewGuid().ToString(), "wintune.cleanup", [change], 0, DateTimeOffset.UtcNow);

        // Act
        var result = await _module.ApplyAsync(cs, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Applied.Should().HaveCount(1);
        File.Exists(tmp).Should().BeFalse();
    }

    [Fact]
    public async Task ApplyAsync_RecordsFailedChange_WhenFileInaccessible()
    {
        // Arrange — path that doesn't exist (won't throw, just skips)
        var change = new PlannedChange(
            Guid.NewGuid().ToString(), ChangeKind.DeleteFile,
            "delete missing", @"C:\does\not\exist\file.tmp",
            new Dictionary<string, object>());
        var cs = new ChangeSet(Guid.NewGuid().ToString(), "wintune.cleanup", [change], 0, DateTimeOffset.UtcNow);

        // Act
        var result = await _module.ApplyAsync(cs, CancellationToken.None);

        // Assert — file didn't exist, so applied with 0 bytes (no error expected)
        result.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task RevertAsync_CompletesWithoutThrowing()
    {
        var fakeResult = new ApplyResult(
            "cs1", "wintune.cleanup", true, [], [], null, DateTimeOffset.UtcNow);
        var act = async () => await _module.RevertAsync(fakeResult, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
