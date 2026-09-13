using Microsoft.Extensions.Logging;
using WinTune.Data;

namespace WinTune.Ai;

public sealed class UsagePatternAnalyzer
{
    private readonly ChangeJournal _journal;
    private readonly ILogger<UsagePatternAnalyzer> _log;

    public UsagePatternAnalyzer(ChangeJournal journal, ILogger<UsagePatternAnalyzer> log)
    {
        _journal = journal;
        _log = log;
    }

    public IReadOnlyList<ScanRecommendation> GetRecommendations(
        IReadOnlyList<string> availableModuleIds)
    {
        List<JournalEntry> history;
        try
        {
            history = _journal.GetRecent(500);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "No se pudo leer el historial del journal");
            history = [];
        }

        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
        var byModule = history
            .Where(e => e.AppliedAt >= cutoff)
            .GroupBy(e => e.ModuleId)
            .ToDictionary(
                g => g.Key,
                g => new ModuleStats(
                    LastRun: g.Max(e => e.AppliedAt),
                    TotalFreed: g.Sum(e => e.BytesFreed),
                    RunCount: g.Count()));

        var now = DateTimeOffset.UtcNow;
        var recommendations = new List<ScanRecommendation>();

        foreach (var moduleId in availableModuleIds)
        {
            float score;
            string reason;

            if (!byModule.TryGetValue(moduleId, out var stats))
            {
                score = 0.9f;
                reason = "Nunca ejecutado — recomendado para análisis inicial.";
            }
            else
            {
                var daysSinceLast = (now - stats.LastRun).TotalDays;

                score = daysSinceLast switch
                {
                    > 30 => 0.8f,
                    > 14 => 0.5f,
                    > 7 => 0.3f,
                    _ => 0.1f,
                };

                if (stats.TotalFreed > 500L * 1024 * 1024) score += 0.2f;
                score = Math.Clamp(score, 0f, 1f);

                reason = $"Último análisis hace {(int)daysSinceLast}d · liberó {FormatBytes(stats.TotalFreed)} en 30d.";
            }

            recommendations.Add(new ScanRecommendation(moduleId, moduleId, score, reason));
        }

        return recommendations.OrderByDescending(r => r.Score).ToList();
    }

    private static string FormatBytes(long b) => b switch
    {
        < 1024 * 1024 => $"{b / 1024.0:F0} KB",
        < 1024L * 1024 * 1024 => $"{b / (1024.0 * 1024):F1} MB",
        _ => $"{b / (1024.0 * 1024 * 1024):F2} GB"
    };

    private sealed record ModuleStats(DateTimeOffset LastRun, long TotalFreed, int RunCount);
}
