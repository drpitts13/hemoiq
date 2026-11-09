using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

public class AdditionalTestingStatusTests
{
    /// <summary>One reactive K+ cell and one nonreactive k+ cell: anti-K is short of the 3 + 3 rule.</summary>
    private static void SeedShortOfRule(IsolatedDatabase iso, string acc)
    {
        iso.Db.AddSpecimen(acc, "serum", null);
        var panel = iso.Db.AddPanel("AT " + acc, "AT-" + acc, "V", 2, null, false, startTypedNegative: true);
        var cells = iso.Db.GetPanelCells(panel);
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "K", "+");
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "k", "+");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "k", "+");
        iso.Db.LinkSpecimenPanel(acc, panel);
        iso.Db.SaveReaction(acc, panel, "1", "0", "0", "2+", "NT");
        iso.Db.SaveReaction(acc, panel, "2", "0", "0", "0", "2+");
    }

    [Fact]
    public void Needs_NameShortIdentificationRuleAndUnruledOutAntibodies()
    {
        using var iso = new IsolatedDatabase();
        SeedShortOfRule(iso, "AT-1");
        var result = iso.Analyzer.AnalyzeSpecimen("AT-1", updateDb: false);
        var needs = AdditionalTestingNeeds.Compute(result);
        // anti-K has no K+ nonreactive cell, so it has no rule-out evaluation, but it is still open.
        Assert.Contains(needs, n => n.StartsWith("Not yet ruled out:") && n.Contains("anti-K"));
        Assert.DoesNotContain(needs, n => n.Contains("anti-k,") || n.EndsWith("anti-k."));
    }

    [Fact]
    public void Needs_ListSuspectShortOfRule_AndSeparationShortfall()
    {
        var result = new AnalysisResult();
        result.Suspected["anti-Jka"] = 0.8;
        result.SuspectedStatistics["anti-Jka"] = new SuspectedStatistics { MeetsIdentificationRule = false };
        result.SeparationEvaluations.Add(new SeparationEvaluation { Antibody = "anti-K", IsolatedCount = 0, Required = 1 });
        foreach (var ag in AntigenConstants.ClinicallySignificantAntigens)
            if (ag != "Jka") result.RuledOut["anti-" + ag] = 3;
        result.SelectedCellRecommendations.Add(new SelectedCellRecommendation { PanelName = "Sel", CellNumber = "4" });

        var needs = AdditionalTestingNeeds.Compute(result);
        Assert.Equal(new[]
        {
            "Identification rule not met for anti-Jka.",
            "Multiple-antibody separation not met for anti-K.",
            "Selected cells: Sel cell 4.",
        }, needs);
    }

    [Fact]
    public void Status_AwaitingAdditionalTesting_FromLatestSnapshot_ClearsWhenIdConfirmed()
    {
        using var iso = new IsolatedDatabase();
        SeedShortOfRule(iso, "AT-2");
        iso.Analyzer.AnalyzeSpecimen("AT-2", updateDb: true);

        var snap = iso.Db.GetLatestAnalysisSnapshot("AT-2")!;
        Assert.NotEmpty(snap.AdditionalTesting);
        var status = InvestigationStatusService.Compute(iso.Db, iso.Db.GetSpecimen("AT-2")!, requireReview: false);
        Assert.Equal(InvestigationStatusService.AwaitingAdditionalTesting, status.Label);
        Assert.Contains("Not yet ruled out:", status.NextAction);
        Assert.True(status.Steps.Single(s => s.Name == "Analysis current").Done);

        iso.Db.SetSpecimenFinalCall("AT-2", "anti-K", null, "TE");
        Assert.Equal(InvestigationStatusService.Completed,
            InvestigationStatusService.Compute(iso.Db, iso.Db.GetSpecimen("AT-2")!, requireReview: false).Label);
    }

    [Fact]
    public void Needs_EmptyWhenNothingIsOutstanding()
    {
        var result = new AnalysisResult();
        Assert.Contains(AdditionalTestingNeeds.Compute(result), n => n.StartsWith("Not yet ruled out: anti-D"));

        foreach (var ag in AntigenConstants.ClinicallySignificantAntigens.Where(a => a is not "E" and not "Fya"))
            result.RuledOut["anti-" + ag] = 3;
        result.UntypedClinicallySignificant.Add("Fya");
        result.PatientTypingConsiderations.Add(new PatientTypingConsideration
            { Antibody = "anti-E", Antigen = "E", Kind = PatientTypingKind.Against, PatientValue = "+" });
        result.SelectedCellRecommendations.Add(new SelectedCellRecommendation { PanelName = "P", CellNumber = "3" });
        Assert.Empty(AdditionalTestingNeeds.Compute(result));
    }

    [Fact]
    public void ExpiredSelectedCellsOnly_AreNotListed()
    {
        var result = new AnalysisResult();
        result.SelectedCellRecommendations.Add(new SelectedCellRecommendation { PanelName = "Old", CellNumber = "2", IsExpired = true });
        var needs = AdditionalTestingNeeds.Compute(result);
        Assert.NotEmpty(needs);
        Assert.DoesNotContain(needs, n => n.StartsWith("Selected cells"));
    }

    [Fact]
    public void SnapshotsWithoutTheField_ReadAsNoNeeds()
    {
        Assert.Empty(new AnalysisSnapshot { AdditionalTestingJson = null }.AdditionalTesting);
        Assert.Equal(new[] { "x" }, new AnalysisSnapshot { AdditionalTestingJson = "[\"x\"]" }.AdditionalTesting);
    }
}
