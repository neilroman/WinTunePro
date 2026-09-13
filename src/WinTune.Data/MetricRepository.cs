using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace WinTune.Data;

public sealed class MetricRepository : IDisposable
{
    private readonly SqliteConnection _db;
    private readonly ILogger<MetricRepository> _log;

    public MetricRepository(string dbPath, ILogger<MetricRepository> log)
    {
        _log = log;
        _db = new SqliteConnection($"Data Source={dbPath}");
        _db.Open();
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS metrics (
                ts       INTEGER NOT NULL,
                cpu      REAL NOT NULL,
                ram_used REAL NOT NULL,
                ram_tot  REAL NOT NULL,
                disk_r   REAL NOT NULL,
                disk_w   REAL NOT NULL,
                net_d    REAL NOT NULL,
                net_u    REAL NOT NULL,
                gpu      REAL NOT NULL,
                cpu_temp REAL NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_metrics_ts ON metrics(ts DESC);
            """;
        cmd.ExecuteNonQuery();
    }

    public void Insert(long tsTicks, float cpu, float ramUsed, float ramTot,
        float diskR, float diskW, float netD, float netU, float gpu, float cpuTemp)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            INSERT INTO metrics(ts,cpu,ram_used,ram_tot,disk_r,disk_w,net_d,net_u,gpu,cpu_temp)
            VALUES($ts,$cpu,$ru,$rt,$dr,$dw,$nd,$nu,$gpu,$ct)
            """;
        cmd.Parameters.AddWithValue("$ts", tsTicks);
        cmd.Parameters.AddWithValue("$cpu", cpu);
        cmd.Parameters.AddWithValue("$ru", ramUsed);
        cmd.Parameters.AddWithValue("$rt", ramTot);
        cmd.Parameters.AddWithValue("$dr", diskR);
        cmd.Parameters.AddWithValue("$dw", diskW);
        cmd.Parameters.AddWithValue("$nd", netD);
        cmd.Parameters.AddWithValue("$nu", netU);
        cmd.Parameters.AddWithValue("$gpu", gpu);
        cmd.Parameters.AddWithValue("$ct", cpuTemp);
        cmd.ExecuteNonQuery();
    }

    public List<MetricRow> QueryLast(int minutes)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-minutes).UtcTicks;
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "SELECT * FROM metrics WHERE ts >= $c ORDER BY ts DESC LIMIT 3600";
        cmd.Parameters.AddWithValue("$c", cutoff);
        using var reader = cmd.ExecuteReader();
        var rows = new List<MetricRow>();
        while (reader.Read())
        {
            rows.Add(new MetricRow(
                reader.GetInt64(0),
                (float)reader.GetDouble(1),
                (float)reader.GetDouble(2),
                (float)reader.GetDouble(3),
                (float)reader.GetDouble(4),
                (float)reader.GetDouble(5),
                (float)reader.GetDouble(6),
                (float)reader.GetDouble(7),
                (float)reader.GetDouble(8),
                (float)reader.GetDouble(9)));
        }
        return rows;
    }

    public void Prune(int keepDays = 30)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-keepDays).UtcTicks;
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "DELETE FROM metrics WHERE ts < $c";
        cmd.Parameters.AddWithValue("$c", cutoff);
        var deleted = cmd.ExecuteNonQuery();
        if (deleted > 0) _log.LogDebug("Pruned {Count} old metric rows", deleted);
    }

    public void Dispose() => _db.Dispose();
}

public sealed record MetricRow(
    long TsTicks, float Cpu, float RamUsed, float RamTot,
    float DiskR, float DiskW, float NetD, float NetU,
    float Gpu, float CpuTemp);
