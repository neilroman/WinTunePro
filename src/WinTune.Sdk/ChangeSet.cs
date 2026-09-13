namespace WinTune.Sdk;

public sealed record ChangeSet(
    string Id,
    string ModuleId,
    IReadOnlyList<PlannedChange> Changes,
    long TotalBytesSaved,
    DateTimeOffset CreatedAt
)
{
    public static ChangeSet Empty(string moduleId) =>
        new(Guid.NewGuid().ToString(), moduleId, [], 0, DateTimeOffset.UtcNow);
}

public sealed record PlannedChange(
    string Id,
    ChangeKind Kind,
    string Description,
    string? TargetPath,
    IReadOnlyDictionary<string, object> Payload
);

public enum ChangeKind
{
    DeleteFile,
    DeleteDirectory,
    TruncateFile,
    RegistryDelete,
    RegistrySet,
    ServiceDisable,
    ServiceEnable,
    KillProcess,
    RunCommand,
    ConfigChange
}
