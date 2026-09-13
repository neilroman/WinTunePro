using Microsoft.Extensions.Logging;
using WinTune.Sdk;

namespace WinTune.Plugins.Cleanup;

public sealed class CleanupModule : IOptimizerModule
{
    private readonly ILogger<CleanupModule> _log;

    public CleanupModule(ILogger<CleanupModule> log) => _log = log;

    public ModuleMetadata Metadata { get; } = new(
        Id: "wintune.cleanup",
        DisplayName: "System Cleanup",
        Description: "Removes temp files, browser caches, thumbnails, WinSxS and more.",
        Category: ModuleCategory.Cleanup,
        Version: new Version(0, 1, 0),
        MinHostVersion: new Version(0, 1, 0)
    );

    public IReadOnlyList<Capability> RequestedCapabilities { get; } =
    [
        new(WellKnownCapabilities.ReadTempFiles, CapabilityLevel.ReadOnly),
        new(WellKnownCapabilities.DeleteUserFiles, CapabilityLevel.UserScope),
        new(WellKnownCapabilities.DeleteSystemFiles, CapabilityLevel.ElevatedRequired),
        new(WellKnownCapabilities.RunDism, CapabilityLevel.ElevatedRequired),
    ];

    private static readonly IScanTask[] _scanTasks =
    [
        new TempFileScanner(),
        new BrowserCacheScanner(),
        new ThumbnailCacheScanner(),
        new WindowsLogScanner(),
    ];

    public async Task<IReadOnlyList<Finding>> ScanAsync(ScanContext ctx, CancellationToken ct)
    {
        var findings = new List<Finding>();
        int i = 0;
        foreach (var task in _scanTasks)
        {
            ct.ThrowIfCancellationRequested();
            var partial = await task.ScanAsync(ct);
            findings.AddRange(partial);
            i++;
            ctx.Progress?.Report(new ScanProgress(i * 100 / _scanTasks.Length, task.Name));
        }
        return findings;
    }

    public Task<ChangeSet> PreviewAsync(IReadOnlyList<Finding> findings, CancellationToken ct)
    {
        var changes = findings
            .SelectMany(f =>
            {
                if (!f.Metadata.TryGetValue("paths", out var raw) || raw is not IEnumerable<string> paths)
                    return Enumerable.Empty<PlannedChange>();
                return paths.Select(p => new PlannedChange(
                    Guid.NewGuid().ToString(),
                    ChangeKind.DeleteFile,
                    $"Delete {Path.GetFileName(p)}",
                    p,
                    new Dictionary<string, object> { ["findingId"] = f.Id }
                ));
            })
            .ToList();

        var cs = new ChangeSet(
            Guid.NewGuid().ToString(),
            Metadata.Id,
            changes,
            findings.Sum(f => f.EstimatedBytesSaved),
            DateTimeOffset.UtcNow);

        return Task.FromResult(cs);
    }

    public async Task<ApplyResult> ApplyAsync(ChangeSet changeSet, CancellationToken ct)
    {
        var applied = new List<AppliedChange>();
        var failed = new List<FailedChange>();

        foreach (var change in changeSet.Changes)
        {
            ct.ThrowIfCancellationRequested();
            if (change.TargetPath is null) continue;
            try
            {
                long size = 0;
                if (File.Exists(change.TargetPath))
                {
                    size = new FileInfo(change.TargetPath).Length;
                    File.Delete(change.TargetPath);
                }
                else if (Directory.Exists(change.TargetPath))
                {
                    size = DirSize(change.TargetPath);
                    Directory.Delete(change.TargetPath, recursive: true);
                }
                applied.Add(new AppliedChange(change.Id, change.Description, size));
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to delete {Path}", change.TargetPath);
                failed.Add(new FailedChange(change.Id, change.Description, ex.Message));
            }
        }

        return new ApplyResult(
            changeSet.Id, Metadata.Id,
            failed.Count == 0,
            applied, failed,
            ReversalToken: null,
            DateTimeOffset.UtcNow);
    }

    public Task RevertAsync(ApplyResult result, CancellationToken ct)
    {
        _log.LogWarning("Cleanup operations cannot be reverted (deleted files).");
        return Task.CompletedTask;
    }

    private static long DirSize(string path) =>
        Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
            .Sum(f => { try { return new FileInfo(f).Length; } catch { return 0L; } });
}
