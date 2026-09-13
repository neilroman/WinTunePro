using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WinTune.Plugins.Storage;
using WinTune.Sdk;
using Xunit;

namespace WinTune.Plugins.Storage.Tests;

public sealed class StorageModuleTests
{
    private readonly StorageModule _module = new(NullLogger<StorageModule>.Instance);

    [Fact]
    public void Metadata_HasExpectedId()
    {
        _module.Metadata.Id.Should().Be("wintune.storage");
        _module.Metadata.Category.Should().Be(ModuleCategory.Storage);
    }

    [Fact]
    public void RequestedCapabilities_ContainsRunDism()
    {
        _module.RequestedCapabilities.Should().Contain(c => c.Name == WellKnownCapabilities.RunDism);
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
    public async Task PreviewAsync_WithPathsMetadata_BuildsDeleteChanges()
    {
        var tmp1 = Path.GetTempFileName();
        var tmp2 = Path.GetTempFileName();
        try
        {
            var finding = Finding.Create(
                moduleId: "wintune.storage",
                title: "test",
                description: "desc",
                estimatedBytesSaved: 1024,
                metadata: new Dictionary<string, object> { ["paths"] = new List<string> { tmp1, tmp2 } });

            var cs = await _module.PreviewAsync([finding], CancellationToken.None);

            cs.Changes.Count.Should().Be(2);
            cs.Changes.Should().AllSatisfy(c => c.Kind.Should().Be(ChangeKind.DeleteFile));
        }
        finally
        {
            File.Delete(tmp1);
            File.Delete(tmp2);
        }
    }

    [Fact]
    public async Task PreviewAsync_WithNoPathsMetadata_ReturnsEmptyChangeSet()
    {
        var finding = Finding.Create(
            moduleId: "wintune.storage",
            title: "test",
            description: "desc",
            estimatedBytesSaved: 1024,
            metadata: new Dictionary<string, object>());

        var cs = await _module.PreviewAsync([finding], CancellationToken.None);

        cs.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_DeletesFiles_AndReportsBytes()
    {
        var tmp = Path.GetTempFileName();
        File.WriteAllBytes(tmp, new byte[100]);
        try
        {
            var finding = Finding.Create(
                moduleId: "wintune.storage",
                title: "test",
                description: "desc",
                estimatedBytesSaved: 1024,
                metadata: new Dictionary<string, object> { ["paths"] = new List<string> { tmp } });

            var cs = await _module.PreviewAsync([finding], CancellationToken.None);
            var result = await _module.ApplyAsync(cs, CancellationToken.None);

            File.Exists(tmp).Should().BeFalse();
            result.Applied[0].BytesFreed.Should().BeGreaterThanOrEqualTo(0);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    [Fact]
    public async Task RevertAsync_DoesNotThrow()
    {
        var fakeResult = new ApplyResult("cs1", "wintune.storage", true, [], [], null, DateTimeOffset.UtcNow);
        var act = async () => await _module.RevertAsync(fakeResult, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
