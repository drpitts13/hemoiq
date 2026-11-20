using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]
public class AmendmentHistoryTests
{
    private static string Report(IsolatedDatabase iso, string acc) =>
        iso.Reports.GeneratePreviewText(ReportType.ClinicalIdentification, acc);

    [Fact]
    public void NeverCleared_HasNoBannerOrHistory()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("AM-1", "serum", null);
        iso.Db.SetSpecimenFinalCall("AM-1", "anti-E", null, "TE");

        Assert.Empty(AmendmentHistory.For(iso.Db, "AM-1"));
        var text = Report(iso, "AM-1");
        Assert.DoesNotContain("AMENDED REPORT", text);
        Assert.DoesNotContain("Amendment history", text);
    }

    [Fact]
    public void ClearedAndReconfirmed_ReportIsMarkedAmended_AndNamesWhatItReplaced()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("AM-2", "serum", null);
        iso.Db.SetSpecimenFinalCall("AM-2", "anti-E", null, "TE");
        iso.Db.ClearSpecimenFinalCall("AM-2", "anti-K missed: cell 4 reactive was K+");
        iso.Db.SetSpecimenFinalCall("AM-2", "anti-E, anti-K", null, "TE");

        var history = AmendmentHistory.For(iso.Db, "AM-2");
        var entry = Assert.Single(history);
        Assert.Equal("anti-E", entry.PreviousIdentification);
        Assert.Equal("TE", entry.PreviouslyConfirmedBy);
        Assert.Equal("anti-K missed: cell 4 reactive was K+", entry.Reason);
        Assert.Equal(AccessControl.CurrentLogin, entry.ClearedBy);

        var text = Report(iso, "AM-2");
        Assert.Contains(AmendmentHistory.AmendedBanner, text);
        Assert.True(text.IndexOf(AmendmentHistory.AmendedBanner, StringComparison.Ordinal) <
                    text.IndexOf("Accession:", StringComparison.Ordinal), "banner is in the header");
        Assert.Contains("FINAL IDENTIFICATION (confirmed)", text);
        Assert.Contains("  Amendment history (earlier identifications, cleared with a reason):", text);
        Assert.Contains($"\"anti-E\" (confirmed by TE on {entry.PreviouslyConfirmedAt}) cleared by {entry.ClearedBy}.", text);
        Assert.Contains("     Reason: anti-K missed: cell 4 reactive was K+", text);
    }

    [Fact]
    public void ClearedAndNotReconfirmed_ReportSaysTheIdentificationWasWithdrawn()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("AM-3", "serum", null);
        iso.Db.SetSpecimenFinalCall("AM-3", "anti-Fya", null, "TE");
        iso.Db.ClearSpecimenFinalCall("AM-3", "wrong patient sample");

        var text = Report(iso, "AM-3");
        Assert.Contains(AmendmentHistory.WithdrawnBanner, text);
        Assert.Contains("\"anti-Fya\"", text);
        Assert.Contains("Reason: wrong patient sample", text);
    }

    [Fact]
    public void SeveralClears_AreNumberedOldestFirst_AndUnreadableSnapshotsSayNotRecorded()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("AM-4", "serum", null);
        iso.Db.AppendAudit("clear_final_call", "specimen", "AM-4", null, "{not json", null);
        iso.Db.SetSpecimenFinalCall("AM-4", "anti-c", null, "TE");
        iso.Db.ClearSpecimenFinalCall("AM-4", "second look");
        iso.Db.SetSpecimenFinalCall("AM-4", "anti-c, anti-E", null, "TE");

        var history = AmendmentHistory.For(iso.Db, "AM-4");
        Assert.Equal(2, history.Count);
        Assert.Equal("not recorded", history[0].PreviousIdentification);
        Assert.Equal("not recorded", history[0].Reason);
        Assert.Equal("anti-c", history[1].PreviousIdentification);

        var lines = AmendmentHistory.Lines(history).ToList();
        Assert.StartsWith("  1. ", lines[0]);
        Assert.StartsWith("  2. ", lines[2]);
        Assert.Contains("\"not recorded\" cleared by", lines[0]);
        Assert.Equal(AmendmentHistory.AmendedBanner, AmendmentHistory.Banner(true, history));
        Assert.Null(AmendmentHistory.Banner(true, Array.Empty<AmendmentEntry>()));
    }

    [Fact]
    public void ReportsIssuedBeforeTheClear_AreListedAsSuperseded_LaterOnesAreNot()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("AM-5", "serum", null);
        iso.Db.SetSpecimenFinalCall("AM-5", "anti-E", null, "TE");
        iso.Reports.RecordPrint(ReportType.ClinicalIdentification, "AM-5", null, "first issue");
        iso.Reports.RecordPrint(ReportType.SpecimenSummary, "AM-5", null, "not a clinical report");
        iso.Db.ClearSpecimenFinalCall("AM-5", "anti-K missed");
        iso.Db.SetSpecimenFinalCall("AM-5", "anti-E, anti-K", null, "TE");
        iso.Reports.RecordPrint(ReportType.ClinicalIdentification, "AM-5", null, "corrected issue");

        var superseded = AmendmentHistory.SupersededReports(iso.Db, "AM-5");
        var r = Assert.Single(superseded);
        Assert.Equal("print", r.Output);
        Assert.Equal("anti-E", r.IdentificationAtIssue);
        Assert.Equal(8, r.Fingerprint!.Length);
        Assert.Equal(AccessControl.CurrentLogin, r.IssuedBy);

        var text = Report(iso, "AM-5");
        Assert.Contains("  Superseded reports (1): issued before the identification was cleared. Recall these copies or mark them superseded:", text);
        Assert.Contains($"print by {r.IssuedBy}; identification at issue: anti-E; fingerprint {r.Fingerprint}…", text);
    }

    [Fact]
    public void NoClear_NoSupersededReports_AndUnreadableEntriesAreTolerated()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("AM-6", "serum", null);
        iso.Reports.RecordPrint(ReportType.ClinicalIdentification, "AM-6", null, "issue");
        Assert.Empty(AmendmentHistory.SupersededReports(iso.Db, "AM-6"));

        iso.Db.AppendAudit("report_output", "specimen", "AM-6", null, null, "{broken");
        iso.Db.AppendAudit("clear_final_call", "specimen", "AM-6", "legacy", null, null);
        var list = AmendmentHistory.SupersededReports(iso.Db, "AM-6");
        var issued = Assert.Single(list);
        Assert.Null(issued.IdentificationAtIssue);
        Assert.Contains("identification at issue: none confirmed", AmendmentHistory.SupersededLines(list).Single());
    }
}
