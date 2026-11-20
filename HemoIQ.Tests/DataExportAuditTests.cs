using System.Security.Cryptography;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// The Analytics and Audit Log CSV exports carry accession numbers: every value is formula-safe, and each export
/// is recorded in the audit log with a fingerprint of the file.
/// </summary>
public class DataExportAuditTests
{
    [Fact]
    public void OverdueLines_NeutralizeAFormulaInTheAccessionAndStatus()
    {
        var t = new TurnaroundResult
        {
            ThresholdDays = 3,
            Overdue = { new OverdueInvestigation { Accession = "=HYPERLINK(\"http://x\")", Received = "2026-11-01", AgeDays = 9, Status = "@SUM(A1)" } },
        };

        var line = AnalyticsViewModel.TurnaroundCsvLines(t).Last();

        Assert.StartsWith("\"'=HYPERLINK(", line);
        Assert.EndsWith(",9,'@SUM(A1)", line);
    }

    [Fact]
    public void OverdueLines_PlainValues_AreUnchanged()
    {
        var t = new TurnaroundResult
        {
            ThresholdDays = 3,
            Overdue = { new OverdueInvestigation { Accession = "2026-001", Received = "2026-11-01", AgeDays = 9, Status = "Awaiting identification" } },
        };

        Assert.Equal("2026-001,2026-11-01,9,Awaiting identification", AnalyticsViewModel.TurnaroundCsvLines(t).Last());
    }

    [Fact]
    public void Record_WritesAnAuditEntry_WithTheFileNameAndItsFingerprint()
    {
        using var iso = new IsolatedDatabase();
        var path = Path.Combine(Path.GetTempPath(), $"hemoiq_export_{Guid.NewGuid():N}.csv");
        try
        {
            File.WriteAllText(path, "Accession\n2026-001\n");

            ExportAudit.Record(iso.Db, "analytics", path);

            var e = iso.Db.GetAuditEvents().Last(a => a.Action == ExportAudit.Action);
            Assert.Equal("export", e.EntityType);
            Assert.Equal("analytics", e.EntityId);
            Assert.Contains(Path.GetFileName(path), e.AfterJson);
            Assert.Contains(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))), e.AfterJson);
        }
        finally { File.Delete(path); }
    }
}
