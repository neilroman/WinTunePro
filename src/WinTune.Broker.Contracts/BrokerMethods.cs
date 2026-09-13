namespace WinTune.Broker.Contracts;

public static class BrokerMethods
{
    public const string PipeName = "WinTuneBroker";

    public const string Ping = "ping";
    public const string CreateRestorePoint = "system.create_restore_point";
    public const string DeleteFiles = "fs.delete_files";
    public const string RunDism = "system.dism";
    public const string RegistryWrite = "registry.write";
    public const string RegistryDelete = "registry.delete";
    public const string ServiceSetState = "service.set_state";
    public const string GetSystemInfo = "system.info";
    public const string FlushDns = "network.flush_dns";
    public const string SetTcpTweaks = "network.tcp_tweaks";
    public const string SetTimerResolution = "gaming.timer_resolution";
    public const string SetHags = "gaming.hags";
    public const string RunTrim = "storage.trim";
    public const string AnalyzeDefrag = "storage.analyze_defrag";
    public const string RunDefrag = "storage.defrag";
}
