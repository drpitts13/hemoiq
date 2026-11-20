using System.Linq;
using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

public class E2EWorkflowSeederTests
{
    [Fact]
    public void Seed_PersistsWarmColdAutoCombinationAndExoticCases()
    {
        using var iso = new IsolatedDatabase();
        E2EWorkflowSeeder.SeedIfNeeded(iso.Db, iso.Analyzer);

        foreach (var id in E2EWorkflowSeeder.SpecimenIds)
            Assert.NotNull(iso.Db.GetSpecimen(id));

        var warm = iso.Analyzer.AnalyzeSpecimen(E2EWorkflowSeeder.WarmK, updateDb: false);
        Assert.Contains("anti-K", warm.Suspected.Keys);

        var cold = iso.Analyzer.AnalyzeSpecimen(E2EWorkflowSeeder.ColdM, updateDb: false);
        Assert.Contains("anti-M", cold.Suspected.Keys);

        var combo = iso.Analyzer.AnalyzeSpecimen(E2EWorkflowSeeder.ComboEc, updateDb: false);
        Assert.Contains("anti-E", combo.Suspected.Keys);
        Assert.Contains("anti-c", combo.Suspected.Keys);

        var neg = iso.Analyzer.AnalyzeSpecimen(E2EWorkflowSeeder.Negative, updateDb: false);
        Assert.DoesNotContain(neg.ReactionPatterns, n => n.Kind == ReactionPatternClassifier.Panreactive);

        var jka = iso.Db.GetAllSpecimenRuns(E2EWorkflowSeeder.WarmJka);
        var jkaRx = iso.Db.GetReactions(jka[0].RunId);
        Assert.Contains(jkaRx, r => r.AHG == "3+");
        Assert.Contains(jkaRx, r => r.AHG == "1+");

        var htla = iso.Db.GetSpecimen(E2EWorkflowSeeder.HtlaCh);
        Assert.Contains("titer 64", htla!.Notes);
        var htlaRun = iso.Db.GetAllSpecimenRuns(E2EWorkflowSeeder.HtlaCh)[0];
        var htlaRx = iso.Db.GetReactions(htlaRun.RunId);
        Assert.Contains(htlaRx, r => r.ExtraPhases.ContainsKey("Neut"));

        var warmAutoPanel = iso.Db.GetAllPanels().Single(p => p.LotNumber == E2EWorkflowSeeder.WarmAuto);
        var warmAutoCells = iso.Db.GetPanelCells(warmAutoPanel.PanelId);
        var warmAutoCell1 = warmAutoCells.Single(c => c.CellNumber == "1");
        Assert.Equal("+", warmAutoCell1.GetTypedValue("E"));
        Assert.Equal("-", warmAutoCell1.GetTypedValue("K"));
        Assert.Equal("+", warmAutoCell1.GetTypedValue("Fya"));
        Assert.Equal(AntigenConstants.AntigenNotTested, warmAutoCell1.Antigens["D"]);
        Assert.False(warmAutoCell1.HasTypedAntigen("D"));

        foreach (var cell in warmAutoCells)
            iso.Db.UpdatePanelCellAntigen(cell.Id, "D", "-");
        E2EWorkflowSeeder.RepairBlanketNegatives(iso.Db);
        var repaired = iso.Db.GetPanelCells(warmAutoPanel.PanelId).Single(c => c.CellNumber == "1");
        Assert.Equal(AntigenConstants.AntigenNotTested, repaired.Antigens["D"]);
        Assert.Equal("-", repaired.GetTypedValue("K"));

        var ac = iso.Db.GetPanelCells(warmAutoPanel.PanelId).Single(c => c.CellNumber == "AC");
        iso.Db.UpdatePanelCellAntigen(ac.Id, "E", "-");
        iso.Db.UpdatePanelCellAntigen(ac.Id, "K", "-");
        iso.Db.UpdatePanelCellAntigen(ac.Id, "Fya", "-");
        E2EWorkflowSeeder.RepairBlanketNegatives(iso.Db);
        var repairedAc = iso.Db.GetPanelCells(warmAutoPanel.PanelId).Single(c => c.CellNumber == "AC");
        Assert.Equal(AntigenConstants.AntigenNotTested, repairedAc.Antigens["E"]);
        Assert.Equal(AntigenConstants.AntigenNotTested, repairedAc.Antigens["K"]);
        Assert.Equal(AntigenConstants.AntigenNotTested, repairedAc.Antigens["Fya"]);
        Assert.Equal("-", iso.Db.GetPanelCells(warmAutoPanel.PanelId).Single(c => c.CellNumber == "1").GetTypedValue("K"));

        var warmAuto = iso.Analyzer.AnalyzeSpecimen(E2EWorkflowSeeder.WarmAuto, updateDb: false);
        var warmAutoText = SpecimenPatternEvidence.Format(warmAuto);
        Assert.Contains("resembles a warm (IAT) pattern", warmAutoText);
        Assert.Contains("favors autoantibody", warmAutoText);
        Assert.Contains("removal of panreactive", warmAutoText);
        Assert.Contains("excluded from ordinary probability and pattern scores", warmAutoText);
        Assert.Contains("Use the autoadsorbed aliquot for underlying alloantibody evidence.", warmAutoText);
        Assert.StartsWith("Untreated is panreactive", warmAutoText);
        Assert.True(
            warmAutoText.IndexOf("removal of panreactive", StringComparison.Ordinal)
            < warmAutoText.IndexOf("resembles a warm", StringComparison.Ordinal));
        Assert.Empty(warmAuto.Suspected);
        Assert.Empty(SpecimenPatternEvidence.RuleoutRows(warmAuto));
        Assert.Contains(SpecimenPatternEvidence.SuspectedRows(warm), row => row.Antibody == "anti-K");

        var untypedD = warmAuto.UntypeableSpecificities.Single(u => u.Antigen == "D");
        Assert.True(untypedD.NotTestedOnEveryCell);
        Assert.Contains("NT on every cell", untypedD.Explanation);
        Assert.DoesNotContain("never imported", untypedD.Explanation);
        Assert.DoesNotContain(warmAuto.UntypeableSpecificities, u => u.Antigen == "E");
        var explainD = AnalysisExplainer.Build(warmAuto).Single(e => e.Antibody == "anti-D");
        Assert.Contains("NT on every cell", explainD.Narrative);
        Assert.DoesNotContain("never imported", explainD.Narrative);
        Assert.Contains(warmAuto.Suggestions, s => s.Contains("D") && s.Contains("NT on every cell"));
        Assert.DoesNotContain(warmAuto.Suggestions, s => s.Contains("never imported"));
        Assert.False(AnalysisViewModel.IncludeNoSuspectedSummary(warmAuto));
        Assert.False(AnalysisViewModel.IncludeNoSuspectedSummary(warm));
        Assert.True(AnalysisViewModel.IncludeNoSuspectedSummary(neg));

        var analysisReport = iso.Reports.GeneratePreviewText(
            ReportType.AnalysisResults, E2EWorkflowSeeder.WarmAuto);
        Assert.Contains("RULED-OUT ANTIBODIES (0)", analysisReport);
        Assert.Contains("PATTERN EVIDENCE (not a diagnosis):", analysisReport);
        Assert.Contains("favors autoantibody", analysisReport);
        Assert.DoesNotContain("anti-E", analysisReport);
        Assert.DoesNotContain("anti-K", analysisReport);
        var antiKReport = iso.Reports.GeneratePreviewText(
            ReportType.AnalysisResults, E2EWorkflowSeeder.WarmK);
        Assert.Contains("anti-K", antiKReport);
        Assert.DoesNotContain("PATTERN EVIDENCE", antiKReport);
        var face = iso.Reports.GeneratePreviewText(
            ReportType.SpecimenSummary, E2EWorkflowSeeder.WarmAuto);
        Assert.Contains("RULED-OUT ANTIBODIES (0)", face);
        Assert.Contains("DAT:", face);
        Assert.DoesNotContain("anti-E", face);
        Assert.Contains("lost reactivity after", analysisReport);
        Assert.DoesNotContain("remained reactive", analysisReport);

        var allosReport = iso.Reports.GeneratePreviewText(
            ReportType.AnalysisResults, E2EWorkflowSeeder.WarmAutoAllos);
        Assert.Contains("SUSPECTED ANTIBODIES (2)", allosReport);
        Assert.Contains("anti-E", allosReport);
        Assert.Contains("anti-K", allosReport);
        Assert.Contains("1 of 6 paired cells lost reactivity after Autoadsorbed", allosReport);
        Assert.Contains("5 remained reactive", allosReport);
        Assert.Contains("underlying alloantibody", allosReport);
        Assert.Contains("RULED-OUT ANTIBODIES (0)", allosReport);
        var allos = iso.Analyzer.AnalyzeSpecimen(E2EWorkflowSeeder.WarmAutoAllos, updateDb: false);
        var allosText = SpecimenPatternEvidence.Format(allos);
        Assert.StartsWith("Untreated is panreactive", allosText);
        Assert.True(
            allosText.IndexOf("5 remained reactive", StringComparison.Ordinal)
            < allosText.IndexOf("resembles a warm", StringComparison.Ordinal));
        Assert.Contains(SpecimenPatternEvidence.SuspectedRows(allos), row => row.Antibody == "anti-E");
        Assert.Contains(SpecimenPatternEvidence.SuspectedRows(allos), row => row.Antibody == "anti-K");
        Assert.Empty(SpecimenPatternEvidence.RuleoutRows(allos));

        var clinical = iso.Reports.GeneratePreviewText(
            ReportType.ClinicalIdentification, E2EWorkflowSeeder.WarmAutoAllos);
        Assert.Contains("cell 1 (Autoadsorbed, AHG 2+, CC NT)", clinical);
        Assert.Contains("cell 2 (Autoadsorbed, AHG 2+, CC NT)", clinical);
        Assert.DoesNotContain("cell 1 (AHG 2+)", clinical);
        Assert.Contains("anti-E", clinical);
        Assert.Contains("anti-K", clinical);

        var coldAutoResult = iso.Analyzer.AnalyzeSpecimen(E2EWorkflowSeeder.ColdAuto, updateDb: false);
        var coldAutoText = SpecimenPatternEvidence.Format(coldAutoResult);
        Assert.Contains("resembles a cold-reactive pattern", coldAutoText);
        Assert.Contains("favors autoantibody", coldAutoText);
        Assert.DoesNotContain("resembles a warm", coldAutoText);
        Assert.Empty(coldAutoResult.Suspected);

        var negText = SpecimenPatternEvidence.Format(neg);
        Assert.Contains("no panreactive or autoantibody pattern note", negText);
        Assert.Equal("", SpecimenPatternEvidence.Format(warm));

        var autoRuns = iso.Db.GetAllSpecimenRuns(E2EWorkflowSeeder.WarmAuto);
        Assert.Contains(autoRuns, r => r.SerumTreatment == SerumTreatment.AutoAdsorption);
        var coldAuto = iso.Db.GetReactions(iso.Db.GetAllSpecimenRuns(E2EWorkflowSeeder.ColdAuto)[0].RunId);
        Assert.Contains(coldAuto, r => r.IS == "3+" && r.CellNumber == "AC");

        var before = iso.Db.GetAllSpecimens().Count;
        var panels = iso.Db.GetAllPanels().Count;
        E2EWorkflowSeeder.SeedIfNeeded(iso.Db, iso.Analyzer);
        Assert.Equal(before, iso.Db.GetAllSpecimens().Count);
        Assert.Equal(panels, iso.Db.GetAllPanels().Count);
        Assert.Null(iso.Db.GetSpecimen("2026-001"));
        Assert.Null(iso.Db.GetSpecimen(DemoDataSeeder.Scenario1Id));
    }

    [Fact]
    public void ClinicalReport_WarmAuto_IncludesPatternEvidence_AndAlloDoesNot()
    {
        using var iso = new IsolatedDatabase();
        E2EWorkflowSeeder.SeedIfNeeded(iso.Db, iso.Analyzer);

        var warmAuto = iso.Reports.GeneratePreviewText(
            ReportType.ClinicalIdentification, E2EWorkflowSeeder.WarmAuto);
        Assert.Contains("Pattern evidence (not a diagnosis):", warmAuto);
        Assert.Contains("resembles a warm (IAT) pattern", warmAuto);
        Assert.Contains("favors autoantibody", warmAuto);
        Assert.DoesNotContain("No antibodies suspected based on current reactions.", warmAuto);

        var antiK = iso.Reports.GeneratePreviewText(
            ReportType.ClinicalIdentification, E2EWorkflowSeeder.WarmK);
        Assert.Contains("anti-K", antiK);
        Assert.DoesNotContain("Pattern evidence (not a diagnosis):", antiK);
        Assert.DoesNotContain("favors autoantibody", antiK);
    }

    [Fact]
    public void PrintedAntigram_OmitsAllNtColumns_AndKeepsTypedMinus()
    {
        using var iso = new IsolatedDatabase();
        E2EWorkflowSeeder.SeedIfNeeded(iso.Db, iso.Analyzer);

        var warmAutoPanel = iso.Db.GetAllPanels().Single(p => p.LotNumber == E2EWorkflowSeeder.WarmAuto);
        Assert.Contains("D", iso.Db.GetPanelDisplayAntigens(warmAutoPanel.PanelId));

        var clinical = iso.Reports.GeneratePreviewText(
            ReportType.ClinicalIdentification, E2EWorkflowSeeder.WarmAuto);
        var clinicalHeaders = AntigenHeaders(clinical).ToList();
        Assert.NotEmpty(clinicalHeaders);
        foreach (var header in clinicalHeaders)
            Assert.Equal(new[] { "E", "K", "Fya" }, header);
        Assert.Contains("Antigens that are NT on every cell are omitted.", clinical);

        var antigram = iso.Reports.GeneratePreviewText(
            ReportType.PanelAntigram, panelId: warmAutoPanel.PanelId);
        Assert.Equal(new[] { "E", "K", "Fya" }, AntigenHeaders(antigram).Single());
        Assert.Equal(new[] { "+", "-", "+" }, AntigenValues(antigram, "1", 3).Single());
        Assert.Equal(new[] { "NT", "NT", "NT" }, AntigenValues(antigram, "AC", 3).Single());
        Assert.Contains("Antigens that are NT on every cell are omitted.", antigram);
        var clinicalAc = AntigenValues(clinical, "AC", 3).ToList();
        Assert.NotEmpty(clinicalAc);
        Assert.All(clinicalAc, row => Assert.Equal(new[] { "NT", "NT", "NT" }, row));
        Assert.All(AntigenValues(clinical, "1", 3), row => Assert.Equal(new[] { "+", "-", "+" }, row));
        Assert.Contains("D", iso.Db.GetPanelDisplayAntigens(warmAutoPanel.PanelId));

        var typedMinus = iso.Db.AddPanel("NT print", "NT-PRINT", "E2E", 2, "2030-01-01", false);
        var cells = iso.Db.GetPanelCells(typedMinus);
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "D", "-");
        var printed = iso.Reports.GeneratePreviewText(ReportType.PanelAntigram, panelId: typedMinus);
        Assert.Equal(new[] { "D" }, AntigenHeaders(printed).Single());
        Assert.Equal(new[] { "-" }, AntigenValues(printed, "1", 1).Single());
        Assert.Equal(new[] { "NT" }, AntigenValues(printed, "2", 1).Single());
        Assert.Equal(
            iso.Db.GetPanelDisplayAntigens(typedMinus).Count,
            AntigenConstants.Antigens.Count);
    }

    [Fact]
    public void ReactionsGrid_HidesAllNtColumns_AndKeepsTypedMinus()
    {
        using var iso = new IsolatedDatabase();
        E2EWorkflowSeeder.SeedIfNeeded(iso.Db, iso.Analyzer);
        var panel = iso.Db.GetAllPanels().Single(p => p.LotNumber == E2EWorkflowSeeder.WarmAuto);
        var order = iso.Db.GetPanelDisplayAntigens(panel.PanelId);
        var cells = iso.Db.GetPanelCells(panel.PanelId);

        Assert.Contains("D", order);
        Assert.Equal(new[] { "E", "K", "Fya" },
            ReactionsViewModel.AntigensForReactions(order, cells, groupBySystem: false));
        Assert.Equal(new[] { "E", "K", "Fya" },
            ReactionsViewModel.AntigensForReactions(order, cells, groupBySystem: true));

        var typedMinus = new PanelCell { CellNumber = "1" };
        typedMinus.SetAntigen("D", "-");
        typedMinus.SetAntigen("C", AntigenConstants.AntigenNotTested);
        var untyped = new PanelCell { CellNumber = "2" };
        untyped.SetAntigen("D", AntigenConstants.AntigenNotTested);
        untyped.SetAntigen("C", AntigenConstants.AntigenNotTested);
        Assert.Equal(new[] { "D" }, ReactionsViewModel.AntigensForReactions(
            new[] { "D", "C" }, new[] { typedMinus, untyped }, groupBySystem: false));
    }

    [Fact]
    public void InvalidCheckCell_IsNotARuleout_TheControlledCellIs_AndTheGradeIsFlagged()
    {
        using var iso = new IsolatedDatabase();
        E2EWorkflowSeeder.SeedIfNeeded(iso.Db, iso.Analyzer);

        var result = iso.Analyzer.AnalyzeSpecimen(E2EWorkflowSeeder.InvalidCc, updateDb: false);
        Assert.True(result.DetailedRuleouts.TryGetValue("anti-K", out var cells));
        Assert.DoesNotContain(cells, c => c.CellNumber == "1");
        Assert.Contains(result.GatedRuleouts, g => g.Antibody == "anti-K" && g.Reason.StartsWith("Cell 1 is AHG 0"));
        var controlled = Assert.Single(cells, c => c.CellNumber == "3");
        Assert.Equal("0", controlled.AHG);
        Assert.Equal("2+", controlled.CC);
        Assert.DoesNotContain(cells, c => c.CellNumber == "AC");
        Assert.True(new RuleoutDetailRow { AHG = "0", CC = "0" }.IsCcInvalid);
        Assert.False(new RuleoutDetailRow { AHG = controlled.AHG, CC = controlled.CC }.IsCcInvalid);
        Assert.True(new RuleoutDetailRow { AHG = "0", CC = "NT" }.IsCcInvalid);
        Assert.False(new RuleoutDetailRow { AHG = "2+", CC = "0" }.IsCcInvalid);
    }

    private static IEnumerable<string[]> AntigenValues(string report, string cellNumber, int count) =>
        report.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Select(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(t => t.Length > 0 && t[0] == cellNumber)
            .Select(t => t.Skip(1).Take(count).ToArray());

    private static IEnumerable<string[]> AntigenHeaders(string report) =>
        report.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Where(l => l.StartsWith("Cell ", StringComparison.Ordinal))
            .Select(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Skip(1)
                .TakeWhile(t => t != "IS")
                .ToArray());
}
