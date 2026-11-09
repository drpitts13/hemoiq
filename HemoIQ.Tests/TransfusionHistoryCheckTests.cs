using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]
public class TransfusionHistoryCheckTests
{
    private static string Day(int daysAgo) => DateTime.Today.AddDays(-daysAgo).ToString("yyyy-MM-dd");

    /// <summary>Earlier specimen for MRN-T that records a transfusion <paramref name="transfusedDaysAgo"/> days ago.</summary>
    private static void Earlier(IsolatedDatabase iso, string acc, int transfusedDaysAgo, params string[] products)
    {
        iso.Db.AddSpecimen(acc, "serum", null, createdDate: Day(transfusedDaysAgo));
        iso.Db.UpdateSpecimenMrn(acc, "MRN-T");
        iso.Db.UpdateSpecimenTransfusion(acc, TransfusionStatus.Transfused, Day(transfusedDaysAgo));
        if (products.Length > 0) iso.Db.UpdateSpecimenTransfusionProducts(acc, products, null);
    }

    private static Specimen Current(IsolatedDatabase iso, string acc, string status, string? lastTransfusion = null, string? mrn = "MRN-T")
    {
        iso.Db.AddSpecimen(acc, "serum", null, phenotype: "E- K-", createdDate: Day(0));
        if (mrn != null) iso.Db.UpdateSpecimenMrn(acc, mrn);
        iso.Db.UpdateSpecimenTransfusion(acc, status, lastTransfusion);
        return iso.Db.GetSpecimen(acc)!;
    }

    [Fact]
    public void NotTransfused_ContradictedByARecentTransfusion_MakesTypingUnreliable_InAnalysis()
    {
        using var iso = new IsolatedDatabase();
        Earlier(iso, "TH-OLD", 10);
        var now = Current(iso, "TH-NOW", TransfusionStatus.NotTransfused);

        var f = TransfusionHistoryCheck.Find(iso.Db, now, 90)!;
        Assert.True(f.MakesPhenotypeUnreliable);
        Assert.Equal("TH-OLD", f.SourceAccession);
        Assert.Equal(10, f.DaysBeforeCollection);
        Assert.StartsWith("REVIEW: this specimen is recorded as Not transfused, but TH-OLD records a red-cell transfusion on " + Day(10), f.Note);

        var panel = iso.Db.AddPanel("TH P", "THL", "V", 1, null, false, startTypedNegative: true);
        iso.Db.UpdatePanelCellAntigen(iso.Db.GetPanelCells(panel)[0].Id, "k", "+");
        iso.Db.LinkSpecimenPanel("TH-NOW", panel);
        iso.Db.SaveReaction("TH-NOW", panel, "1", "0", "0", "0", "0");
        var result = iso.Analyzer.AnalyzeSpecimen("TH-NOW", updateDb: false);
        Assert.True(result.PatientPhenotypeUnreliable);
        Assert.Contains("TH-OLD records a red-cell transfusion", result.TransfusionContextNote);
        Assert.Contains(result.TransfusionRequirementNotes, n => n == f.Note);
    }

    [Fact]
    public void UnknownStatus_IsFilledFromHistory_AsUnreliable_WithTheDateToRecord()
    {
        using var iso = new IsolatedDatabase();
        Earlier(iso, "TH-OLD2", 30);
        var f = TransfusionHistoryCheck.Find(iso.Db, Current(iso, "TH-NOW2", TransfusionStatus.Unknown), 90)!;
        Assert.True(f.MakesPhenotypeUnreliable);
        Assert.Contains($"record Transfused with last transfusion {Day(30)} (or later)", f.Note);

        var applied = TransfusionHistoryCheck.Apply(new TransfusionAssessment { Explanation = "Status Unknown." }, f);
        Assert.True(applied.PhenotypeUnreliable);
        Assert.Equal("history", applied.Source);
        Assert.StartsWith("Status Unknown. Transfusion status is Unknown, and TH-OLD2", applied.Explanation);
    }

    [Fact]
    public void Transfused_WithAnOlderDate_GetsAnUpdateNote_WithoutChangingReliability()
    {
        using var iso = new IsolatedDatabase();
        Earlier(iso, "TH-OLD3", 5);
        var f = TransfusionHistoryCheck.Find(iso.Db, Current(iso, "TH-NOW3", TransfusionStatus.Transfused, Day(60)), 90)!;
        Assert.False(f.MakesPhenotypeUnreliable);
        Assert.Contains($"last transfusion date ({Day(60)}) is earlier than the transfusion on {Day(5)}", f.Note);
        var before = new TransfusionAssessment { Explanation = "x" };
        Assert.Same(before, TransfusionHistoryCheck.Apply(before, f));
    }

    [Fact]
    public void NoFinding_OutsideTheWindow_ForNonRedCellProducts_FutureDates_OrWithoutMrn()
    {
        using var iso = new IsolatedDatabase();
        Earlier(iso, "TH-FAR", 200);
        Assert.Null(TransfusionHistoryCheck.Find(iso.Db, Current(iso, "TH-A", TransfusionStatus.NotTransfused), 90));

        Earlier(iso, "TH-PLT", 3, TransfusionProducts.Platelets, TransfusionProducts.Plasma);
        Assert.Null(TransfusionHistoryCheck.Find(iso.Db, iso.Db.GetSpecimen("TH-A")!, 90));

        var noMrn = Current(iso, "TH-B", TransfusionStatus.NotTransfused, mrn: null);
        Assert.Null(TransfusionHistoryCheck.Find(iso.Db, noMrn, 90));

        // A transfusion recorded after this specimen's collection says nothing about it.
        using var iso2 = new IsolatedDatabase();
        iso2.Db.AddSpecimen("TH-C", "serum", null, createdDate: Day(20));
        iso2.Db.UpdateSpecimenMrn("TH-C", "MRN-T");
        iso2.Db.UpdateSpecimenTransfusion("TH-C", TransfusionStatus.NotTransfused, null);
        Earlier(iso2, "TH-LATER", 5);
        Assert.Null(TransfusionHistoryCheck.Find(iso2.Db, iso2.Db.GetSpecimen("TH-C")!, 90));
    }
}
