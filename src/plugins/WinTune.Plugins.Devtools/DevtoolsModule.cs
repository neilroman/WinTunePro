using Microsoft.Extensions.Logging;
using WinTune.Sdk;

namespace WinTune.Plugins.Devtools;

public sealed class DevtoolsModule : IOptimizerModule
{
    private readonly ILogger<DevtoolsModule> _log;
    private readonly IScanTask[] _tasks;

    public ModuleMetadata Metadata { get; } = new(
        Id: "wintune.devtools",
        DisplayName: "Herramientas de desarrollo",
        Description: "Detecta cachés de npm/yarn/pnpm, Docker y artefactos de Visual Studio.",
        Category: ModuleCategory.Developer,
        Version: new Version(1, 0, 0),
        MinHostVersion: new Version(0, 1, 0));

    public IReadOnlyList<Capability> RequestedCapabilities { get; } =
    [
        new(WellKnownCapabilities.ReadTempFiles, CapabilityLevel.ReadOnly),
        new(WellKnownCapabilities.DeleteUserFiles, CapabilityLevel.UserScope),
    ];

    public DevtoolsModule(ILogger<DevtoolsModule> log)
    {
        _log = log;
        _tasks =
        [
            new NpmCacheScanner(),
            new DockerScanner(),
            new VsArtifactsScanner(),
        ];
    }

    public async Task<IReadOnlyList<Finding>> ScanAsync(ScanContext ctx, CancellationToken ct)
    {
        var findings = new List<Finding>();
        int i = 0;
        foreach (var task in _tasks)
        {
            ct.ThrowIfCancellationRequested();
            findings.AddRange(await task.ScanAsync(ct));
            ctx.Progress?.Report(new ScanProgress(++i * 100 / _tasks.Length, task.Name));
        }
        return findings;
    }

    public Task<ChangeSet> PreviewAsync(IReadOnlyList<Finding> findings, CancellationToken ct)
    {
        var changes = findings
            .Where(f => f.Metadata.TryGetValue("paths", out var p) && p is IEnumerable<string>)
            .SelectMany(f =>
            {
                var paths = (IEnumerable<string>)f.Metadata["paths"];
                return paths.Select(p => new PlannedChange(
                    Guid.NewGuid().ToString(),
                    ChangeKind.DeleteFile,
                    $"Eliminar cache {f.Metadata.GetValueOrDefault("tool", "dev")} — {Path.GetFileName(p)}",
                    p,
                    new Dictionary<string, object> { ["findingId"] = f.Id }));
            })
            .ToList();

        long totalSaved = findings.Sum(f => f.EstimatedBytesSaved);
        return Task.FromResult(new ChangeSet(
            Guid.NewGuid().ToString(), Metadata.Id, changes, totalSaved, DateTimeOffset.UtcNow));
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
                long freed = 0;
                if (File.Exists(change.TargetPath))
                {
                    freed = new FileInfo(change.TargetPath).Length;
                    File.Delete(change.TargetPath);
                }
                applied.Add(new AppliedChange(change.Id, change.Description, freed));
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "No se pudo eliminar {Path}", change.TargetPath);
                failed.Add(new FailedChange(change.Id, change.Description, ex.Message));
            }
        }

        return await Task.FromResult(new ApplyResult(
            changeSet.Id, Metadata.Id, failed.Count == 0, applied, failed, null, DateTimeOffset.UtcNow));
    }

    public Task RevertAsync(ApplyResult result, CancellationToken ct) => Task.CompletedTask;
}
