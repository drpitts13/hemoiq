using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]

public class CandidateExplanationTests
{
    [Fact]
    public void RuledOutAntiE_NarrativeNamesCellAndCriteria()
    {
        using var iso = new IsolatedDatabase();
        SeedHomozygousNegative(iso, "EX-E-RO", "E", "e");

        var result = iso.Analyzer.AnalyzeSpecimen("EX-E-RO", updateDb: false);
        var exp = RequireExplanation(result, "anti-E");

        Assert.Equal("Ruled out", exp.Status);
        Assert.Contains("currently meets configured rule-out criteria", exp.Narrative);
        Assert.Contains("cell 1", exp.Narrative);
        Assert.NotEmpty(exp.RuleoutCells);
        Assert.Contains(result.Suggestions, s => s.Contains("Explain Analysis"));
    }

    [Fact]
    public void IncompleteAntiK_IsInProgress_AndStillACandidate()
    {
        using var iso = new IsolatedDatabase();
        SeedHomozygousNegative(iso, "EX-K-3", "K", "k");
        iso.Db.AddRule("K needs 3", "Require three homozygous K+ nonreactive cells",
            "anti-K", null, heterozygousOk: false, minRuleoutCount: 3);

        var result = iso.Analyzer.AnalyzeSpecimen("EX-K-3", updateDb: false);
        var exp = RequireExplanation(result, "anti-K");

        Assert.Equal("Rule-out in progress", exp.Status);
        Assert.Contains("1 of 3", exp.Narrative);
        Assert.DoesNotContain("anti-K", result.RuledOut.Keys);
        Assert.False(result.Suspected.ContainsKey("anti-K") && result.RuledOut.ContainsKey("anti-K"));
        Assert.Equal(AnalysisRuleTrace.EngineVersion, result.RuleEngineVersion);
        Assert.Contains("anti-K requires 3", result.RuleConfigurationNote);
        Assert.Contains("Lab default rule-out count is", result.RuleConfigurationNote);
        Assert.Contains("not a diagnosis", result.RuleConfigurationNote, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("anti-K requires 3", AnalysisExplainer.FormatDocument(result));
    }

    [Fact]
    public void RuleoutCell_NamesTheRunThatSuppliedIt()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("EX-RUN-RO", "serum", null);
        var panelId = iso.Db.AddPanel("Run panel", "L", "V", 2, null, false);
        iso.Db.LinkSpecimenPanel("EX-RUN-RO", panelId);
        var cells = iso.Db.GetPanelCells(panelId);
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "E", "+");
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "e", "-");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "E", "-");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "e", "+");

        iso.Db.SaveReaction("EX-RUN-RO", panelId, "1", "0", "0", "3+", "NT");
        iso.Db.SaveReaction("EX-RUN-RO", panelId, "2", "0", "0", "0", "2+");

        var ficin = iso.Db.AddPanelRun(
            "EX-RUN-RO", panelId, CellTreatment.Ficin, SerumTreatment.None, "Ficin");
        iso.Db.SaveReaction(ficin, "1", "0", "0", "0", "2+");
        iso.Db.SaveReaction(ficin, "2", "0", "0", "0", "2+");

        var result = iso.Analyzer.AnalyzeSpecimen("EX-RUN-RO", updateDb: false);
        var exp = RequireExplanation(result, "anti-E");

        Assert.Contains(exp.RuleoutCells, c =>
            c.Contains("cell 1", StringComparison.Ordinal) &&
            c.Contains("Ficin, homozygous, IS 0, 37°C 0, AHG 0, CC 2+", StringComparison.Ordinal));
        Assert.DoesNotContain(exp.RuleoutCells, c =>
            c.Contains("Untreated", StringComparison.Ordinal));
        Assert.Contains("Ficin, homozygous, IS 0, 37°C 0, AHG 0, CC 2+", exp.Narrative);
        Assert.DoesNotContain("check cell did not react", exp.Narrative);
        Assert.Contains(exp.ConflictingCells, c =>
            c.Contains("cell 1", StringComparison.Ordinal) &&
            c.Contains("Untreated, AHG 3+, CC NT, antigen-positive and reactive", StringComparison.Ordinal));
        Assert.DoesNotContain(exp.ConflictingCells, c =>
            c.Contains("Ficin, antigen-positive and reactive", StringComparison.Ordinal));

        var details = result.DetailedRuleouts["anti-E"];
        Assert.Contains(details, d =>
            d.CellNumber == "1" && d.RunLabel == "Ficin" && d.CC == "2+");
        Assert.DoesNotContain(details, d => d.RunLabel == "Untreated");
    }

    [Fact]
    public void RuleoutCell_FailedCheckCell_SaysTheCheckCellDidNotReact()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("EX-CC-FAIL", "serum", null);
        var panelId = iso.Db.AddPanel("P", "L", "V", 2, null, false);
        iso.Db.LinkSpecimenPanel("EX-CC-FAIL", panelId);
        var cells = iso.Db.GetPanelCells(panelId);
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "K", "+");
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "k", "-");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "K", "+");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "k", "-");
        iso.Db.SaveReaction("EX-CC-FAIL", panelId, "1", "0", "0", "0", "0");
        iso.Db.SaveReaction("EX-CC-FAIL", panelId, "2", "0", "0", "0", "2+");

        var result = iso.Analyzer.AnalyzeSpecimen("EX-CC-FAIL", updateDb: false);
        var exp = RequireExplanation(result, "anti-K");
        var failed = Assert.Single(exp.RuleoutCells, c => c.Contains("cell 1", StringComparison.Ordinal));
        var controlled = Assert.Single(exp.RuleoutCells, c => c.Contains("cell 2", StringComparison.Ordinal));

        Assert.Contains("IS 0, 37°C 0, AHG 0, CC 0, check cell did not react", failed);
        Assert.Contains("IS 0, 37°C 0, AHG 0, CC 2+", controlled);
        Assert.DoesNotContain("check cell did not react", controlled);
        Assert.Contains("check cell did not react", exp.Narrative);
    }

    [Fact]
    public void ReactiveEPositive_ListedAsConflict_NotAsRuleoutCell()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("EX-E-CONFLICT", "serum", null);
        var panelId = iso.Db.AddPanel("P", "L", "V", 2, null, false);
        iso.Db.LinkSpecimenPanel("EX-E-CONFLICT", panelId);
        var cells = iso.Db.GetPanelCells(panelId);
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "E", "+");
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "e", "-");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "E", "+");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "e", "-");
        iso.Db.SaveReaction("EX-E-CONFLICT", panelId, "1", "0", "0", "3+", "NT");
        iso.Db.SaveReaction("EX-E-CONFLICT", panelId, "2", "0", "0", "0", "2+");

        var result = iso.Analyzer.AnalyzeSpecimen("EX-E-CONFLICT", updateDb: false);
        var exp = RequireExplanation(result, "anti-E");

        Assert.Contains(exp.ConflictingCells, c => c.Contains("cell 1"));
        Assert.DoesNotContain(exp.RuleoutCells, c => c.Contains("cell 1"));
        Assert.Contains(exp.RuleoutCells, c => c.Contains("cell 2"));
        Assert.Contains("Cells that block rule-out", exp.Narrative);
    }

    [Fact]
    public void SuspectedAntiE_ListsSupportingCells()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("EX-E-SUP", "serum", null);
        var panelId = iso.Db.AddPanel("P", "L", "V", 4, null, false);
        iso.Db.LinkSpecimenPanel("EX-E-SUP", panelId);
        var cells = iso.Db.GetPanelCells(panelId);
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "E", "+");
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "e", "-");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "E", "+");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "e", "-");
        iso.Db.UpdatePanelCellAntigen(cells[2].Id, "E", "-");
        iso.Db.UpdatePanelCellAntigen(cells[2].Id, "e", "+");
        iso.Db.UpdatePanelCellAntigen(cells[3].Id, "E", "-");
        iso.Db.UpdatePanelCellAntigen(cells[3].Id, "e", "+");
        iso.Db.SaveReaction("EX-E-SUP", panelId, "1", "0", "0", "3+", "NT");
        iso.Db.SaveReaction("EX-E-SUP", panelId, "2", "0", "0", "3+", "NT");
        iso.Db.SaveReaction("EX-E-SUP", panelId, "3", "0", "0", "0", "2+");
        iso.Db.SaveReaction("EX-E-SUP", panelId, "4", "0", "0", "0", "2+");

        var previous = AppSettings.Current.IdentificationCellCount;
        try
        {
            AppSettings.Current.IdentificationCellCount = 2;
            var result = iso.Analyzer.AnalyzeSpecimen("EX-E-SUP", updateDb: false);
            Assert.True(result.Suspected.ContainsKey("anti-E"),
                $"Expected anti-E suspected. Found: {string.Join(", ", result.Suspected.Keys)}");
            var exp = RequireExplanation(result, "anti-E");
            Assert.Equal("Suspected", exp.Status);
            Assert.True(exp.SupportingCells.Count >= 2);
            Assert.Contains("Supporting evidence", exp.Narrative);
            Assert.Contains("cell 1", exp.Narrative);
            Assert.Contains(exp.SupportingCells, c =>
                c.Contains("cell 1", StringComparison.Ordinal) &&
                c.Contains("AHG 3+", StringComparison.Ordinal) &&
                c.Contains("CC NT", StringComparison.Ordinal));
            Assert.Contains("CC NT", exp.Narrative);
            var evidence = result.SuspectedEvidence["anti-E"];
            Assert.Contains(evidence.SupportingCells, c =>
                c.CellNumber == "1" && c.AHG == "3+" && c.CC == "NT");
            Assert.DoesNotContain(evidence.SupportingCells, c => c.CellNumber == "3");
            Assert.Empty(evidence.ConflictingCells);
        }
        finally
        {
            AppSettings.Current.IdentificationCellCount = previous;
        }
    }

    [Fact]
    public void AgNegativeReactiveCell_KeepsTheCheckCellGrade()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("EX-E-CC", "serum", null);
        var panelId = iso.Db.AddPanel("P", "L", "V", 5, null, false);
        iso.Db.LinkSpecimenPanel("EX-E-CC", panelId);
        var cells = iso.Db.GetPanelCells(panelId);
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "E", "+");
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "e", "-");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "E", "+");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "e", "-");
        iso.Db.UpdatePanelCellAntigen(cells[2].Id, "E", "+");
        iso.Db.UpdatePanelCellAntigen(cells[2].Id, "e", "-");
        iso.Db.UpdatePanelCellAntigen(cells[3].Id, "E", "-");
        iso.Db.UpdatePanelCellAntigen(cells[3].Id, "e", "+");
        iso.Db.UpdatePanelCellAntigen(cells[4].Id, "E", "-");
        iso.Db.UpdatePanelCellAntigen(cells[4].Id, "e", "+");
        iso.Db.SaveReaction("EX-E-CC", panelId, "1", "0", "0", "3+", "NT");
        iso.Db.SaveReaction("EX-E-CC", panelId, "2", "0", "0", "2+", "NT");
        iso.Db.SaveReaction("EX-E-CC", panelId, "3", "0", "0", "1+", "NT");
        iso.Db.SaveReaction("EX-E-CC", panelId, "4", "2+", "0", "0", "2+");
        iso.Db.SaveReaction("EX-E-CC", panelId, "5", "0", "0", "0", "2+");

        var result = iso.Analyzer.AnalyzeSpecimen("EX-E-CC", updateDb: false);
        Assert.True(result.Suspected.ContainsKey("anti-E"),
            $"Expected anti-E suspected. Found: {string.Join(", ", result.Suspected.Keys)}");
        var evidence = result.SuspectedEvidence["anti-E"];
        Assert.Contains(evidence.ConflictingCells, c =>
            c.CellNumber == "4" && c.AHG == "0" && c.CC == "2+" && c.RunLabel == "Untreated");
        Assert.DoesNotContain(evidence.SupportingCells, c => c.CellNumber == "4");
        Assert.Contains(evidence.SupportingCells, c =>
            c.CellNumber == "1" && c.CC == "NT");
        var exp = RequireExplanation(result, "anti-E");
        Assert.Contains(exp.SupportingCells, c =>
            c.Contains("cell 1", StringComparison.Ordinal) &&
            c.Contains("Untreated, AHG 3+, CC NT", StringComparison.Ordinal));
        Assert.Contains(exp.ConflictingCells, c =>
            c.Contains("cell 4", StringComparison.Ordinal) &&
            c.Contains("Untreated, IS 2+, CC 2+", StringComparison.Ordinal));
        Assert.DoesNotContain(exp.SupportingCells, c =>
            c.Contains("cell 4", StringComparison.Ordinal));
    }

    [Fact]
    public void HistoricalAntiK_AppearsEvenWhenNotSuspected()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("EX-HIST-K", "serum", null, notes: null, phenotype: null,
            previousAntibodies: "anti-K");
        var panelId = iso.Db.AddPanel("P", "L", "V", 2, null, false);
        iso.Db.LinkSpecimenPanel("EX-HIST-K", panelId);
        iso.Db.SaveReaction("EX-HIST-K", panelId, "1", "0", "0", "0", "2+");
        iso.Db.SaveReaction("EX-HIST-K", panelId, "2", "0", "0", "0", "2+");

        var result = iso.Analyzer.AnalyzeSpecimen("EX-HIST-K", updateDb: false);
        var exp = RequireExplanation(result, "anti-K");
        Assert.Equal("Historical", exp.Status);
        Assert.Contains("previously identified", exp.Narrative, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.Suspected.ContainsKey("anti-K"));
    }

    [Fact]
    public void PhenotypeAgainst_IsIncludedInSuspectedNarrative()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("EX-PHENO-E", "serum", null, notes: null, phenotype: "R2R2",
            previousAntibodies: null);
        var panelId = iso.Db.AddPanel("P", "L", "V", 4, null, false);
        iso.Db.LinkSpecimenPanel("EX-PHENO-E", panelId);
        var cells = iso.Db.GetPanelCells(panelId);
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "E", "+");
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "e", "-");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "E", "+");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "e", "-");
        iso.Db.UpdatePanelCellAntigen(cells[2].Id, "E", "-");
        iso.Db.UpdatePanelCellAntigen(cells[2].Id, "e", "+");
        iso.Db.UpdatePanelCellAntigen(cells[3].Id, "E", "-");
        iso.Db.UpdatePanelCellAntigen(cells[3].Id, "e", "+");
        iso.Db.SaveReaction("EX-PHENO-E", panelId, "1", "0", "0", "3+", "NT");
        iso.Db.SaveReaction("EX-PHENO-E", panelId, "2", "0", "0", "3+", "NT");
        iso.Db.SaveReaction("EX-PHENO-E", panelId, "3", "0", "0", "0", "2+");
        iso.Db.SaveReaction("EX-PHENO-E", panelId, "4", "0", "0", "0", "2+");

        var previous = AppSettings.Current.IdentificationCellCount;
        try
        {
            AppSettings.Current.IdentificationCellCount = 2;
            var result = iso.Analyzer.AnalyzeSpecimen("EX-PHENO-E", updateDb: false);
            var exp = RequireExplanation(result, "anti-E");
            Assert.Contains("argues against", exp.Narrative, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            AppSettings.Current.IdentificationCellCount = previous;
        }
    }

    [Fact]
    public void FormatDocument_IncludesDisclaimerAndCandidates()
    {
        using var iso = new IsolatedDatabase();
        SeedHomozygousNegative(iso, "EX-DOC", "E", "e");
        var result = iso.Analyzer.AnalyzeSpecimen("EX-DOC", updateDb: false);
        var doc = AnalysisExplainer.FormatDocument(result);
        Assert.Contains("Decision support", doc);
        Assert.Contains("Not a diagnosis", doc);
        Assert.Contains("anti-E", doc);
    }

    [Fact]
    public void ClinicalReport_IncludesExplainNarrative()
    {
        using var iso = new IsolatedDatabase();
        SeedHomozygousNegative(iso, "EX-RPT", "E", "e");
        var text = iso.Reports.GeneratePreviewText(ReportType.ClinicalIdentification, "EX-RPT");
        Assert.Contains("Explain Analysis", text);
        Assert.Contains("decision support", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("anti-E", text);
    }

    private static CandidateExplanation RequireExplanation(AnalysisResult result, string antibody)
    {
        var exp = result.CandidateExplanations
            .SingleOrDefault(e => string.Equals(e.Antibody, antibody, StringComparison.Ordinal));
        Assert.NotNull(exp);
        return exp!;
    }

    private static void SeedHomozygousNegative(IsolatedDatabase iso, string specimenId,
        string antigen, string antithetical)
    {
        iso.Db.AddSpecimen(specimenId, "serum", null);
        var panelId = iso.Db.AddPanel("P", "L", "V", 2, null, false);
        iso.Db.LinkSpecimenPanel(specimenId, panelId);
        var cells = iso.Db.GetPanelCells(panelId);
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, antigen, "+");
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, antithetical, "-");
        iso.Db.SaveReaction(specimenId, panelId, cells[0].CellNumber, "0", "0", "0", "2+");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, antigen, "-");
        iso.Db.SaveReaction(specimenId, panelId, cells[1].CellNumber, "0", "0", "0", "2+");
    }
}
