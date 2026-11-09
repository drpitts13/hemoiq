using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// Regression tests from the Iteration 114 review: places that compared antigen or antibody
/// names ignoring case, so K/k, C/c, E/e or S/s were treated as the same thing.
/// </summary>
[Collection("PersistentDatabase")]
public class AntigenCaseRegressionTests
{
    [Fact]
    public void FinalId_AppendingAntik_BesideAntiK_AddsIt_ButADuplicateIsStillIgnored()
    {
        Assert.Equal("anti-K; anti-k", FinalAntibodyParser.Append("anti-K", "anti-k"));
        Assert.Equal("anti-K", FinalAntibodyParser.Append("anti-K", "Anti-K"));
        Assert.Equal("anti-E; anti-e", FinalAntibodyParser.Append("anti-E", "anti-e"));
        Assert.Equal("warm auto", FinalAntibodyParser.Append("warm auto", "Warm Auto")); // non-antibody text
    }

    [Fact]
    public void LotComparison_KeepsKandkAsSeparateColumns()
    {
        static PanelCell Cell(string n, params (string Ag, string V)[] ags)
        {
            var c = new PanelCell { CellNumber = n };
            foreach (var (ag, v) in ags) c.SetAntigen(ag, v);
            return c;
        }
        var left = new[] { Cell("1", ("K", "+"), ("k", "+")) };
        var right = new[] { Cell("1", ("K", "-"), ("k", "+")) };
        var result = PanelComparer.Compare("A", left, new[] { "K", "k" }, "B", right, new[] { "K", "k" });
        var diff = Assert.Single(result.TypingDiffs);
        Assert.Equal("K", diff.Antigen);
        Assert.False(result.Schema.Changed);

        var schema = PanelComparer.Compare("A", left, new[] { "K" }, "B", right, new[] { "K", "k" });
        Assert.Contains("k", schema.Schema.Added);

        // The import review's schema-vs-prior-lot check uses the same comparison.
        var diff2 = PanelImportReviewer.CompareSchema(new[] { "K" }, new[] { "K", "k" });
        Assert.Equal(new[] { "k" }, diff2.Removed);
    }

    [Fact]
    public void SelectedCells_PatientTyping_ForK_IsNotReadAsk()
    {
        var result = new AnalysisResult();
        result.PatientTypingConsiderations.Add(new PatientTypingConsideration
        {
            Antibody = "anti-K", Antigen = "K", Kind = PatientTypingKind.Predicted, PatientValue = "-",
        });
        Assert.Equal("-", SelectedCellRecommender.PatientAntigen(result, "K"));
        Assert.Null(SelectedCellRecommender.PatientAntigen(result, "k"));
    }

    [Fact]
    public void Analysis_ConflictingReactiveCellsForAntiK_AreNotReportedForAntik()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("AC-1", "serum", null);
        var panel = iso.Db.AddPanel("AC P", "ACL", "V", 3, null, false, startTypedNegative: true);
        var cells = iso.Db.GetPanelCells(panel);
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "K", "+");          // reactive, K+k-
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "k", "+");          // nonreactive, K-k+
        iso.Db.UpdatePanelCellAntigen(cells[2].Id, "k", "+");          // nonreactive, K-k+
        iso.Db.LinkSpecimenPanel("AC-1", panel);
        iso.Db.SaveReaction("AC-1", panel, "1", "0", "0", "0", "3+");
        iso.Db.SaveReaction("AC-1", panel, "2", "0", "0", "0", "0");
        iso.Db.SaveReaction("AC-1", panel, "3", "0", "0", "0", "0");

        var result = iso.Analyzer.AnalyzeSpecimen("AC-1", updateDb: false);
        var antik = Assert.Single(result.RuleoutEvaluations, e => e.Antibody == "anti-k");
        Assert.DoesNotContain("1", antik.ConflictingReactiveCells);
    }

    [Fact]
    public void ClinicalSignificance_UsesExactAntigenCase()
    {
        Assert.True(HemoIQ.ViewModels.RuleoutRow.IsClinicallySignificantAntibody("Anti-K"));
        Assert.False(HemoIQ.ViewModels.RuleoutRow.IsClinicallySignificantAntibody("anti-KELL"));
    }
}
