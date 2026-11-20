using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]
public class DosageRuleoutExplanationTests
{
    /// <summary>
    /// One specimen on a panel of nonreactive cells. Each entry is (Jka, Jkb) typing for one cell;
    /// null leaves Jkb untyped (NT).
    /// </summary>
    private static AnalysisResult Analyze(IsolatedDatabase iso, string acc, params (string Jka, string? Jkb)[] cells)
    {
        iso.Db.AddSpecimen(acc, "serum", null);
        var panel = iso.Db.AddPanel("Dosage", "DS-" + acc, "V", cells.Length, null, false);
        iso.Db.LinkSpecimenPanel(acc, panel);
        var stored = iso.Db.GetPanelCells(panel);
        for (var i = 0; i < cells.Length; i++)
        {
            iso.Db.UpdatePanelCellAntigen(stored[i].Id, "Jka", cells[i].Jka);
            iso.Db.UpdatePanelCellAntigen(stored[i].Id, "Jkb", cells[i].Jkb ?? AntigenConstants.AntigenNotTested);
            iso.Db.SaveReaction(acc, panel, stored[i].CellNumber, "0", "0", "0", "2+");
        }
        return iso.Analyzer.AnalyzeSpecimen(acc, updateDb: false);
    }

    [Fact]
    public void OnlyHeterozygousCells_ExplainsWhyAntiJkaIsNotRuledOut()
    {
        using var iso = new IsolatedDatabase();
        var result = Analyze(iso, "DS-1", ("+", "+"), ("+", "+"));

        var ev = Assert.Single(result.RuleoutEvaluations, e => e.Antibody == "anti-Jka");
        Assert.False(ev.MeetsCriteria);
        Assert.Equal(0, ev.ObservedCount);
        Assert.Equal(2, ev.DosageExcludedCells.Count);
        Assert.Equal("Anti-Jka cannot currently be ruled out because the only nonreactive Jka-positive cells are heterozygous (cells 1, 2), " +
                     "and heterozygous cells are not allowed for this antibody (dosage). " +
                     "Test 1 homozygous Jka-positive cell(s), or allow heterozygous cells with a rule on the Rules tab.", ev.Explanation);
        Assert.Contains(ev.Explanation, result.Suggestions);
        Assert.False(result.RuledOut.ContainsKey("anti-Jka"));
    }

    [Fact]
    public void UntypedAntithetical_IsReportedAsUnknownZygosity_NotAsHeterozygous()
    {
        using var iso = new IsolatedDatabase();
        var result = Analyze(iso, "DS-2", ("+", "+"), ("+", null));

        var ev = Assert.Single(result.RuleoutEvaluations, e => e.Antibody == "anti-Jka");
        Assert.Contains("are heterozygous (cell 1) or are of unknown zygosity because Jkb is not typed (cell 2)", ev.Explanation);
    }

    [Fact]
    public void ShortEvaluation_ListsTheCellsNotCountedForDosage()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddRule("Jka two", null, "anti-Jka", null, heterozygousOk: false, minRuleoutCount: 2);
        var result = Analyze(iso, "DS-3", ("+", "-"), ("+", "+"));

        var ev = Assert.Single(result.RuleoutEvaluations, e => e.Antibody == "anti-Jka");
        Assert.Equal(1, ev.ObservedCount);
        Assert.Equal("Anti-Jka does not yet meet configured rule-out criteria (1 of 2 homozygous antigen-positive nonreactive cells). " +
                     "Not counted for dosage: nonreactive Jka-positive cells that are heterozygous (cell 2).", ev.Explanation);
    }

    [Fact]
    public void HomozygousRuleOut_AndHeterozygousAllowed_HaveNoDosageNote()
    {
        using var iso = new IsolatedDatabase();
        var met = Analyze(iso, "DS-4", ("+", "-"), ("+", "+"));
        var ev = Assert.Single(met.RuleoutEvaluations, e => e.Antibody == "anti-Jka");
        Assert.True(ev.MeetsCriteria);
        Assert.DoesNotContain("dosage", ev.Explanation);

        using var iso2 = new IsolatedDatabase();
        iso2.Db.AddRule("Jka het", null, "anti-Jka", null, heterozygousOk: true, minRuleoutCount: 1);
        var allowed = Analyze(iso2, "DS-5", ("+", "+"));
        var ev2 = Assert.Single(allowed.RuleoutEvaluations, e => e.Antibody == "anti-Jka");
        Assert.True(ev2.MeetsCriteria);
        Assert.Empty(ev2.DosageExcludedCells);
    }

    [Fact]
    public void AntigensWithoutAPartner_AreNeverExcludedForDosage()
    {
        using var iso = new IsolatedDatabase();
        var result = Analyze(iso, "DS-6", ("+", "+"));
        Assert.All(result.RuleoutEvaluations, e =>
            Assert.True(AntigenConstants.AntitheticalPairs.ContainsKey(e.Antigen) || e.DosageExcludedCells.Count == 0));
    }

    [Fact]
    public void RuledOutGrid_ShowsBlockedByDosage_AndTheExcludedCellsWithTheirZygosity()
    {
        using var iso = new IsolatedDatabase();
        var result = Analyze(iso, "DS-7", ("+", "+"), ("+", null));
        var ev = Assert.Single(result.RuleoutEvaluations, e => e.Antibody == "anti-Jka");

        Assert.Equal(RuleoutRow.BlockedByDosage, RuleoutRow.From(ev).Status);
        var rows = AnalysisViewModel.DetailRowsFor(result, "anti-Jka");
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Equal("No (dosage)", r.Counted));
        Assert.Equal(new[] { "Heterozygous", "Unknown (Jkb not typed)" }, rows.Select(r => r.Zygosity));
    }

    [Fact]
    public void ShortRuleOut_ListsCountedCellsFirst_ThenExcluded_AndStaysInProgress()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddRule("Jka two", null, "anti-Jka", null, heterozygousOk: false, minRuleoutCount: 2);
        var result = Analyze(iso, "DS-8", ("+", "-"), ("+", "+"));
        var ev = Assert.Single(result.RuleoutEvaluations, e => e.Antibody == "anti-Jka");

        Assert.Equal("In progress", RuleoutRow.From(ev).Status);
        var rows = AnalysisViewModel.DetailRowsFor(result, "anti-Jka");
        Assert.Equal(new[] { ("1", "Yes", "Homozygous"), ("2", "No (dosage)", "Heterozygous") },
            rows.Select(r => (r.CellNumber, r.Counted, r.Zygosity)));
    }

    [Fact]
    public void ExplainView_NamesTheExcludedCells_AndADosageStatus()
    {
        using var iso = new IsolatedDatabase();
        var result = Analyze(iso, "DS-9", ("+", "+"), ("+", null));
        var exp = AnalysisExplainer.Explain("anti-Jka", result);

        Assert.Equal("Not ruled out (dosage)", exp.Status);
        Assert.Contains(exp.RuleoutCells, c => c.EndsWith("cell 1 (not counted: heterozygous)"));
        Assert.Contains(exp.RuleoutCells, c => c.EndsWith("cell 2 (not counted: zygosity unknown, Jkb not typed)"));
        Assert.StartsWith("Anti-Jka cannot currently be ruled out", exp.RuleoutCriteria);
    }

    /// <summary>An unlinked selected-cell panel: cell 1 Jk(a+b-) homozygous, cell 2 Jk(a+b+), cell 3 Jk(a+b-) but E+.</summary>
    private static void SelectedCellPanel(IsolatedDatabase iso)
    {
        var sel = iso.Db.AddPanel("Selected cells", "SEL-1", "V", 3, null, false, startTypedNegative: true);
        var cells = iso.Db.GetPanelCells(sel);
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "Jka", "+");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "Jka", "+");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "Jkb", "+");
        iso.Db.UpdatePanelCellAntigen(cells[2].Id, "Jka", "+");
        iso.Db.UpdatePanelCellAntigen(cells[2].Id, "E", "+");
    }

    [Fact]
    public void SelectedCells_RecommendAHomozygousCell_ToCompleteADosageBlockedRuleOut()
    {
        using var iso = new IsolatedDatabase();
        SelectedCellPanel(iso);
        var result = Analyze(iso, "DS-10", ("+", "+"), ("+", "+"));

        var rec = Assert.Single(result.SelectedCellRecommendations, r => r.CompletesRuleout && r.PanelName == "Selected cells" && r.CellNumber == "1");
        Assert.Contains("It is homozygous Jka+ Jkb- and, if nonreactive, would count toward ruling out anti-Jka (0 of 1 so far; heterozygous cells do not count).",
            rec.Explanation);
        Assert.False(rec.FillsSeparation);
        Assert.DoesNotContain(result.SelectedCellRecommendations, r => r.CompletesRuleout && r.CellNumber == "2" && r.PanelName == "Selected cells");
    }

    [Fact]
    public void CellsCarryingASuspectedAntigen_AreNotOfferedToCompleteARuleOut()
    {
        var result = new AnalysisResult();
        result.Suspected["anti-E"] = 0.9;
        result.RuleoutEvaluations.Add(new RuleoutEvaluation
        {
            Antibody = "anti-Jka", Antigen = "Jka", RequiredCount = 1,
            DosageExcludedCells = { new RuleoutDetail { CellNumber = "4", Antigen = "Jka", Antithetical = "Jkb", AntitheticalValue = "+" } },
        });
        var panel = new Panel { PanelId = 9, Name = "Inventory" };
        PanelCell Cell(string n, string e) => new()
        {
            CellNumber = n,
            Antigens = new Dictionary<string, string> { ["Jka"] = "+", ["Jkb"] = "-", ["E"] = e },
        };
        var recs = SelectedCellRecommender.Recommend(result, Array.Empty<(int, string)>(),
            new[] { (panel, Cell("1", "+")), (panel, Cell("2", "-")) });

        Assert.DoesNotContain(recs, r => r.CellNumber == "1" && r.CompletesRuleout);
        Assert.Contains(recs, r => r.CellNumber == "2" && r.CompletesRuleout);
    }
}
