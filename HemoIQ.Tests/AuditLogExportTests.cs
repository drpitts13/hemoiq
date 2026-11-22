using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// The audit-log viewer loads only the newest events. Its CSV export must still be the whole log (filtered by Find),
/// and the viewer must say when it lists only part of it, so a partial export is never handed on as the full log.
/// </summary>
public class AuditLogExportTests
{
    private static IsolatedDatabase WithEvents(int count)
    {
        var iso = new IsolatedDatabase();
        for (var i = 0; i < count; i++)
            iso.Db.AppendAudit(i % 2 == 0 ? "probe_even" : "probe_odd", "specimen", $"AX-{i:000}", null, null, null);
        return iso;
    }

    private static string[] Rows(string csv) =>
        csv.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');

    [Fact]
    public void Export_WritesEveryEvent_NotOnlyTheLoadedOnes()
    {
        using var iso = WithEvents(40);
        var total = iso.Db.CountAuditEvents();
        Assert.True(iso.Db.GetAuditEvents(10).Count < total); // what a 10-event viewer would hold

        var sw = new StringWriter();
        var written = AuditLogExport.Write(iso.Db, sw, null);

        Assert.Equal(total, written);
        var rows = Rows(sw.ToString());
        Assert.Equal(AuditLogExport.Header, rows[0]);
        Assert.Equal(total + 1, rows.Length);
        Assert.Contains("AX-039", rows[1]); // newest first
        Assert.Contains(rows, r => r.Contains("AX-000"));
    }

    [Fact]
    public void Export_AppliesTheFindFilter_AcrossTheWholeLog()
    {
        using var iso = WithEvents(40);
        var sw = new StringWriter();
        var written = AuditLogExport.Write(iso.Db, sw, "probe_odd");
        Assert.Equal(20, written);
        Assert.All(Rows(sw.ToString()).Skip(1), r => Assert.Contains("probe_odd", r));
    }

    [Fact]
    public void Export_NeutralizesSpreadsheetFormulas()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AppendAudit("probe", "specimen", "F-1", "=HYPERLINK(\"x\")", null, null);
        var sw = new StringWriter();
        AuditLogExport.Write(iso.Db, sw, "F-1");
        Assert.DoesNotContain(",\"=HYPERLINK", sw.ToString());
    }

    [Theory]
    [InlineData(12, 12, 12, "12 event(s)")]
    [InlineData(3, 12, 12, "Showing 3 of 12")]
    [InlineData(5000, 5000, 12000, "Newest 5000 of 12000")]
    [InlineData(7, 5000, 12000, "Showing 7 of the newest 5000 (log: 12000)")]
    public void CountLabel_NamesTheWholeLogWhenOnlyPartIsLoaded(int shown, int loaded, int total, string expected) =>
        Assert.Equal(expected, AuditLogExport.CountLabel(shown, loaded, total));

    [Fact]
    public void TruncationNote_AppearsOnlyWhenTheLogIsLongerThanWhatWasLoaded()
    {
        Assert.Null(AuditLogExport.TruncationNote(12, 12));
        var note = AuditLogExport.TruncationNote(5000, 12000);
        Assert.Contains("12000 events", note);
        Assert.Contains("Export CSV writes every event that matches Find", note);
    }
}
