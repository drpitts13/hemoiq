using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace HemoIQ.Tests;

/// <summary>
/// When a report cannot compute the current analysis it prints the suspected and ruled-out lists saved at the last
/// Run Analysis. An issued report must say so, not present older lists as the current result.
/// </summary>
public class ReportSavedListsNoteTests
{
    [Fact]
    public void TheNote_NamesTheReasonAndTheSaveTime()
    {
        var note = ReportService.SavedListsNote("no such table: panel_cells", "2026-11-20 09:15");
        Assert.StartsWith("NOTE: The current analysis could not be computed (no such table: panel_cells).", note);
        Assert.Contains("saved at the last Run Analysis (2026-11-20 09:15) and may be out of date", note);
    }

    [Fact]
    public void TheNote_ReadsWithoutAReasonOrTime()
    {
        Assert.Equal(
            "NOTE: The current analysis could not be computed. The lists below were saved at the last Run Analysis " +
            "and may be out of date; run analysis again before relying on them.",
            ReportService.SavedListsNote(null, null));
    }

    [Fact]
    public void AnalysisResults_LabelsTheSavedLists_WhenTheAnalysisFails()
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("RSL", "L-RSL", "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen("RSL-1", "serum", null);
        iso.Db.LinkSpecimenPanel("RSL-1", panel);
        iso.Db.SaveReaction("RSL-1", panel, "1", "0", "0", "2+", "NT");
        iso.Analyzer.AnalyzeSpecimen("RSL-1", updateDb: true);

        var current = iso.Reports.GeneratePreviewText(ReportType.AnalysisResults, "RSL-1", null);
        Assert.DoesNotContain("could not be computed", current);

        // Make the analysis itself fail while the saved lists stay readable.
        using (var conn = new SqliteConnection($"Data Source={iso.DbPath};Pooling=False"))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "ALTER TABLE panel_cells RENAME TO panel_cells_hidden";
            cmd.ExecuteNonQuery();
        }

        var text = iso.Reports.GeneratePreviewText(ReportType.AnalysisResults, "RSL-1", null);
        Assert.Contains("NOTE: The current analysis could not be computed", text);
        var note = text.IndexOf("NOTE: The current analysis");
        Assert.True(note < text.IndexOf("SUSPECTED ANTIBODIES"), "the note comes before the lists");
    }

    [Fact]
    public void TheSpecimenSummaryCsv_CarriesTheNote()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "Services", "ReportService.cs"));
        Assert.Contains("csv.WriteField(\"AnalysisNote\");", src);
        Assert.DoesNotContain("AppendSavedSuspectedAndRuleouts(sb, specimenId);", src);
    }
}
