using HemoIQ.Tests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace HemoIQ.Tests;

/// <summary>
/// Per-specimen and per-panel lookups (latest snapshot, panel cells, stored antibodies and rule-outs) must use an
/// index; a full scan of a table that grows with every analysis makes staleness checks and reports quadratic.
/// </summary>
public class ForeignKeyIndexTests
{
    private static string Plan(string dbPath, string sql)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath};Pooling=False");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "EXPLAIN QUERY PLAN " + sql;
        using var r = cmd.ExecuteReader();
        var detail = new List<string>();
        while (r.Read()) detail.Add(r.GetString(r.GetOrdinal("detail")));
        return string.Join(" | ", detail);
    }

    [Theory]
    [InlineData("SELECT * FROM analysis_snapshots WHERE specimen_id = 'X' ORDER BY id DESC LIMIT 1", "ix_snapshots_specimen")]
    [InlineData("SELECT * FROM panel_cells WHERE panel_id = 1", "ix_panel_cells_panel")]
    [InlineData("SELECT * FROM specimen_antibodies WHERE specimen_id = 'X'", "ix_specimen_antibodies_specimen")]
    [InlineData("SELECT * FROM specimen_ruleouts WHERE specimen_id = 'X'", "ix_specimen_ruleouts_specimen")]
    [InlineData("SELECT id, occurred_at_utc FROM audit_events WHERE action IN ('add_rule', 'update_rule', 'delete_rule', 'update_settings') ORDER BY id DESC LIMIT 1", "ix_audit_action")]
    [InlineData("SELECT COUNT(*) FROM audit_events WHERE action = 'clear_final_call' AND entity_id = 'X'", "ix_audit_action_entity")]
    public void PerRecordLookup_UsesAnIndex(string sql, string index)
    {
        using var iso = new IsolatedDatabase();

        var plan = Plan(iso.DbPath, sql);

        Assert.Contains(index, plan);
        Assert.DoesNotContain("SCAN analysis_snapshots", plan);
        Assert.DoesNotContain("SCAN panel_cells", plan);
        Assert.DoesNotContain("SCAN audit_events", plan);
    }
}
