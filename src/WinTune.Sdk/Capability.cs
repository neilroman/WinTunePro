namespace WinTune.Sdk;

public sealed record Capability(string Name, CapabilityLevel Level);

public enum CapabilityLevel
{
    ReadOnly,
    UserScope,
    SystemScope,
    ElevatedRequired
}

public static class WellKnownCapabilities
{
    public const string ReadTempFiles = "fs.temp.read";
    public const string DeleteUserFiles = "fs.user.delete";
    public const string DeleteSystemFiles = "fs.system.delete";
    public const string ReadRegistry = "registry.read";
    public const string WriteRegistry = "registry.write";
    public const string ManageServices = "services.manage";
    public const string ManageProcesses = "processes.manage";
    public const string ReadNetworkConfig = "network.read";
    public const string WriteNetworkConfig = "network.write";
    public const string CreateRestorePoint = "system.restore_point";
    public const string RunDism = "system.dism";
}
