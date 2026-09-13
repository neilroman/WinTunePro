using Microsoft.Extensions.Logging;
using WinTune.Sdk;

namespace WinTune.Core.Orchestrator;

public sealed class ModuleOrchestrator
{
    private readonly ILogger<ModuleOrchestrator> _log;
    private readonly IReadOnlyList<IOptimizerModule> _modules;

    public ModuleOrchestrator(IEnumerable<IOptimizerModule> modules, ILogger<ModuleOrchestrator> log)
    {
        _modules = modules.ToList();
        _log = log;
    }

    public IReadOnlyList<IOptimizerModule> Modules => _modules;

    public async Task<OrchestratorScanResult> ScanAllAsync(
        ScanContext ctx,
        IProgress<ModuleScanProgress>? progress = null,
        CancellationToken ct = default)
    {
        var results = new List<ModuleScanResult>();
        int done = 0;

        foreach (var module in _modules)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                _log.LogInformation("Scanning {Module}", module.Metadata.Id);
                var findings = await module.ScanAsync(ctx, ct);
                results.Add(new ModuleScanResult(module.Metadata.Id, findings, null));
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Scan failed for {Module}", module.Metadata.Id);
                results.Add(new ModuleScanResult(module.Metadata.Id, [], ex.Message));
            }
            done++;
            progress?.Report(new ModuleScanProgress(done, _modules.Count, module.Metadata.DisplayName));
        }

        return new OrchestratorScanResult(results);
    }

    public async Task<ApplyResult> ApplyModuleAsync(
        string moduleId,
        ChangeSet changeSet,
        CancellationToken ct = default)
    {
        var module = _modules.FirstOrDefault(m => m.Metadata.Id == moduleId)
            ?? throw new InvalidOperationException($"Module '{moduleId}' not found.");

        return await module.ApplyAsync(changeSet, ct);
    }
}

public sealed record ModuleScanResult(
    string ModuleId,
    IReadOnlyList<Finding> Findings,
    string? Error);

public sealed record OrchestratorScanResult(IReadOnlyList<ModuleScanResult> Results)
{
    public long TotalBytesSaved => Results.Sum(r => r.Findings.Sum(f => f.EstimatedBytesSaved));
    public int TotalFindings => Results.Sum(r => r.Findings.Count);
}

public sealed record ModuleScanProgress(int CompletedModules, int TotalModules, string CurrentModule);
