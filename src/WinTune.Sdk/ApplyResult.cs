namespace WinTune.Sdk;

public sealed record ApplyResult(
    string ChangeSetId,
    string ModuleId,
    bool Success,
    IReadOnlyList<AppliedChange> Applied,
    IReadOnlyList<FailedChange> Failed,
    string? ReversalToken,
    DateTimeOffset AppliedAt
);

public sealed record AppliedChange(string ChangeId, string Description, long BytesFreed);
public sealed record FailedChange(string ChangeId, string Description, string ErrorMessage);
