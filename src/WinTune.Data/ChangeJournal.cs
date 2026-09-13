using Microsoft.Data.Sqlite;

namespace WinTune.Data;

/// <summary>
/// Append-only audit log de todos los cambios aplicados por plugins.
/// Permite undo y auditoría.
/// </summary>
public sealed class ChangeJournal : IDisposable
{
    private readonly SqliteConnection _db;

    public ChangeJournal(string dbPath)
    {
        _db = new SqliteConnection($"Data Source={dbPath}");
        _db.Open();
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS journal (
                id           TEXT PRIMARY KEY,
                module_id    TEXT NOT NULL,
                changeset_id TEXT NOT NULL,
                applied_at   INTEGER NOT NULL,
                kind         TEXT NOT NULL,
                target       TEXT,
                description  TEXT NOT NULL,
                bytes_freed  INTEGER NOT NULL DEFAULT 0,
                reversal_tok TEXT
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public void RecordApply(string moduleId, string changesetId,
        string changeId, string kind, string? target, string description,
        long bytesFreed, string? reversalToken)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            INSERT OR IGNORE INTO journal(id,module_id,changeset_id,applied_at,kind,target,description,bytes_freed,reversal_tok)
            VALUES($id,$mod,$cs,$at,$kind,$tgt,$desc,$bf,$tok)
            """;
        cmd.Parameters.AddWithValue("$id", changeId);
        cmd.Parameters.AddWithValue("$mod", moduleId);
        cmd.Parameters.AddWithValue("$cs", changesetId);
        cmd.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.UtcTicks);
        cmd.Parameters.AddWithValue("$kind", kind);
        cmd.Parameters.AddWithValue("$tgt", target ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$desc", description);
        cmd.Parameters.AddWithValue("$bf", bytesFreed);
        cmd.Parameters.AddWithValue("$tok", reversalToken ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public List<JournalEntry> GetRecent(int limit = 100)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "SELECT * FROM journal ORDER BY applied_at DESC LIMIT $n";
        cmd.Parameters.AddWithValue("$n", limit);
        using var r = cmd.ExecuteReader();
        var rows = new List<JournalEntry>();
        while (r.Read())
        {
            rows.Add(new JournalEntry(
                r.GetString(0), r.GetString(1), r.GetString(2),
                new DateTimeOffset(r.GetInt64(3), TimeSpan.Zero),
                r.GetString(4),
                r.IsDBNull(5) ? null : r.GetString(5),
                r.GetString(6), r.GetInt64(7),
                r.IsDBNull(8) ? null : r.GetString(8)));
        }
        return rows;
    }

    public void Dispose() => _db.Dispose();
}

public sealed record JournalEntry(
    string Id, string ModuleId, string ChangesetId,
    DateTimeOffset AppliedAt, string Kind, string? Target,
    string Description, long BytesFreed, string? ReversalToken);
