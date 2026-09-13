namespace WinTune.Ai;

public sealed record ScanRecommendation(
    string ModuleId,
    string DisplayName,
    float Score,
    string Reason
);
