using HemoIQ.Data;
using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// Iteration 119 review: state changes that were written without an audit entry.
/// </summary>
public class WriteAuditCoverageTests
{
    [Fact]
    public void AddingARun_IsAudited_AndTreatedRunsAppearOnTheTimeline()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("WA-1", "serum", null);
        var panel = iso.Db.AddPanel("WA Panel", "WAL", "V", 2, null, false);
        iso.Db.LinkSpecimenPanel("WA-1", panel);
        iso.Db.GetOrCreateDefaultRun("WA-1", panel);
        iso.Db.AddPanelRun("WA-1", panel, CellTreatment.Ficin, SerumTreatment.None, "Ficin");
        iso.Db.AddPanelRun("WA-1", panel, CellTreatment.DTT, SerumTreatment.None);

        var adds = iso.Db.GetAuditEventsForEntity("specimen", "WA-1").Where(e => e.Action == "add_run").ToList();
        Assert.True(adds.Count >= 3);
        Assert.Contains(adds, e => e.AfterJson!.Contains("\"cellTreatment\":\"Ficin\"") && e.AfterJson.Contains("\"panel\":\"WA Panel\""));

        var timeline = InvestigationTimeline.Build(iso.Db, iso.Db.GetSpecimen("WA-1")!).Select(e => e.What).ToList();
        Assert.Contains("Run added: WA Panel, Ficin (cells Ficin)", timeline);
        Assert.Contains("Run added: WA Panel, cells DTT", timeline);
        Assert.DoesNotContain(timeline, w => w.Contains("untreated", StringComparison.OrdinalIgnoreCase) && w.StartsWith("Run added"));
    }

    [Fact]
    public void StartupDeactivationOfExpiredRecords_IsAudited_AndOnTheTimeline()
    {
        var path = Path.Combine(Path.GetTempPath(), $"abpanels_test_{Guid.NewGuid():N}.db");
        try
        {
            int panelId;
            using (var db = new DatabaseService(path))
            {
                db.AddSpecimen("WA-2", "serum", null);
                db.AddSpecimen("WA-3", "serum", null);
                panelId = db.AddPanel("Old lot", "OLD-1", "V", 2, null, false);
            }
            using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False"))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE specimens SET expiration_date = '2020-01-01', is_active = 1 WHERE accession_number = 'WA-2';" +
                                  $"UPDATE panels SET expiration_date = '2020-02-02', is_active = 1 WHERE panel_id = {panelId};";
                cmd.ExecuteNonQuery();
            }
            using (var db = new DatabaseService(path))
            {
                Assert.False(db.GetSpecimen("WA-2")!.IsActive);
                Assert.True(db.GetSpecimen("WA-3")!.IsActive);
                var s = Assert.Single(db.GetAuditEventsForEntity("specimen", "WA-2"), e => e.Action == "auto_deactivate_expired");
                Assert.Contains("Expired 2020-01-01", s.Reason);
                Assert.Contains("\"isActive\":true", s.BeforeJson);
                Assert.Single(db.GetAuditEventsForEntity("panel", panelId.ToString()), e => e.Action == "auto_deactivate_expired");
                Assert.Contains(InvestigationTimeline.Build(db, db.GetSpecimen("WA-2")!),
                    e => e.What.StartsWith("Made inactive automatically: Expired 2020-01-01"));
            }
            using (var db = new DatabaseService(path))
            {
                // Already inactive: no second entry on the next start.
                Assert.Single(db.GetAuditEventsForEntity("specimen", "WA-2"), e => e.Action == "auto_deactivate_expired");
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { File.Delete(path); } catch { }
        }
    }
}
