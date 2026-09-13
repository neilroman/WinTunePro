namespace WinTune.Sdk;

public interface IOptimizerModule
{
    ModuleMetadata Metadata { get; }
    IReadOnlyList<Capability> RequestedCapabilities { get; }

    Task<IReadOnlyList<Finding>> ScanAsync(ScanContext ctx, CancellationToken ct);
    Task<ChangeSet> PreviewAsync(IReadOnlyList<Finding> findings, CancellationToken ct);
    Task<ApplyResult> ApplyAsync(ChangeSet changeSet, CancellationToken ct);
    Task RevertAsync(ApplyResult result, CancellationToken ct);
}
