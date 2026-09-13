namespace WinTune.Sdk;

public sealed record ModuleMetadata(
    string Id,
    string DisplayName,
    string Description,
    ModuleCategory Category,
    Version Version,
    Version MinHostVersion
);

public enum ModuleCategory
{
    Cleanup,
    Performance,
    Storage,
    Power,
    Privacy,
    Network,
    Gaming,
    Developer,
    Monitoring,
    Backup
}
