namespace WinTune.Sdk;

public sealed record Finding(
    string Id,
    string ModuleId,
    string Title,
    string Description,
    FindingSeverity Severity,
    long EstimatedBytesSaved,
    IReadOnlyDictionary<string, object> Metadata
)
{
    public static Finding Create(
        string moduleId,
        string title,
        string description,
        FindingSeverity severity = FindingSeverity.Info,
        long estimatedBytesSaved = 0,
        Dictionary<string, object>? metadata = null)
        => new(
            Guid.NewGuid().ToString(),
            moduleId,
            title,
            description,
            severity,
            estimatedBytesSaved,
            metadata ?? new Dictionary<string, object>()
        );
}

public enum FindingSeverity { Info, Low, Medium, High, Critical }
