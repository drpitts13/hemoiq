using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// The Clinical Identification report prints the first Explain Analysis narratives in full. Every further candidate must
/// still be named with its status, so the printed record never silently ends where the on-screen list goes on.
/// </summary>
public class ExplainReportCompletenessTests
{
    private static List<CandidateExplanation> Candidates(int n) =>
        Enumerable.Range(1, n).Select(i => new CandidateExplanation
        {
            Antibody = $"anti-X{i}",
            Status = i <= 2 ? "Suspected" : "Ruled out",
            Narrative = $"narrative {i}",
        }).ToList();

    [Fact]
    public void UpToTheLimit_AllNarrativesArePrinted_WithNoRemainderLine()
    {
        var lines = ReportService.ExplainLines(Candidates(ReportService.ExplainNarrativesPrinted));
        Assert.Equal(ReportService.ExplainNarrativesPrinted, lines.Count);
        Assert.DoesNotContain(lines, l => l.Contains("more candidate"));
    }

    [Fact]
    public void BeyondTheLimit_EveryFurtherCandidateIsNamedWithItsStatus()
    {
        var lines = ReportService.ExplainLines(Candidates(11));
        Assert.Equal(ReportService.ExplainNarrativesPrinted + 1, lines.Count);
        var rest = lines[^1];
        Assert.StartsWith("3 more candidate(s) not described here:", rest);
        Assert.Contains("anti-X9 (Ruled out)", rest);
        Assert.Contains("anti-X10 (Ruled out)", rest);
        Assert.Contains("anti-X11 (Ruled out)", rest);
        Assert.Contains("Explain Analysis on the Analysis tab", rest);
    }

    [Fact]
    public void NoCandidates_NoLines() => Assert.Empty(ReportService.ExplainLines(new List<CandidateExplanation>()));

    [Fact]
    public void PrintedReport_NamesEveryCandidateTheAnalysisExplains()
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("Explain", "L-EXR", "V", 4, null, false, startTypedNegative: true);
        var cells = iso.Db.GetPanelCells(panel);
        foreach (var ag in new[] { "E", "K", "Fya", "Jka", "S", "c", "M", "Lea", "C", "Jkb" })
            cells[0].SetAntigen(ag, "+");
        cells[1].SetAntigen("E", "+");
        // Nonreactive cells carrying many antigens give rule-out evaluations for many antibodies.
        foreach (var ag in new[] { "D", "C", "c", "e", "k", "Fyb", "Jkb", "s", "N", "P1", "Leb", "Kpb" })
        {
            cells[2].SetAntigen(ag, "+");
            cells[3].SetAntigen(ag, "+");
        }
        foreach (var c in cells) iso.Db.UpdatePanelCell(c);
        iso.Db.AddSpecimen("EXR-1", "serum", null);
        iso.Db.LinkSpecimenPanel("EXR-1", panel);
        iso.Db.SaveReaction("EXR-1", panel, "1", "0", "0", "2+", "NT");
        iso.Db.SaveReaction("EXR-1", panel, "2", "0", "0", "2+", "NT");
        iso.Db.SaveReaction("EXR-1", panel, "3", "0", "0", "0", "2+");
        iso.Db.SaveReaction("EXR-1", panel, "4", "0", "0", "0", "2+");
        var result = iso.Analyzer.AnalyzeSpecimen("EXR-1", updateDb: true);

        Assert.True(result.CandidateExplanations.Count > ReportService.ExplainNarrativesPrinted,
            $"the case should evaluate more than {ReportService.ExplainNarrativesPrinted} candidates; it has {result.CandidateExplanations.Count}");

        var text = iso.Reports.GeneratePreviewText(ReportType.ClinicalIdentification, "EXR-1");
        Assert.Contains("Explain Analysis", text);
        foreach (var exp in result.CandidateExplanations)
            Assert.Contains(exp.Antibody, text);
        Assert.Equal(result.CandidateExplanations.Count > ReportService.ExplainNarrativesPrinted,
            text.Contains("more candidate(s) not described here"));
    }
}
