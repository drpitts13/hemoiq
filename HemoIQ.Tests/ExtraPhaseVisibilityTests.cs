using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// Analysis reads every stored extra phase. So the Reactions grid and the printed worksheet show the configured
/// phases and also any phase graded on the run that is no longer in Preferences, rather than hiding grades that
/// still count.
/// </summary>
[Collection("PersistentDatabase")]
public class ExtraPhaseVisibilityTests
{
    [Fact]
    public void Columns_AreTheConfiguredPhases_ThenOtherStoredOnes()
    {
        var columns = ExtraPhaseParser.ColumnsFor("PEG, Enz",
            new[] { new[] { "PEG", "4C" }, new[] { "enz", "IS" } });
        Assert.Equal(new[] { "PEG", "Enz", "4C" }, columns); // case-insensitive; core phases are never extra
    }

    [Fact]
    public void NothingConfiguredOrStored_GivesNoColumns() =>
        Assert.Empty(ExtraPhaseParser.ColumnsFor("", Array.Empty<IEnumerable<string>>()));

    [Fact]
    public void Worksheet_PrintsConfiguredAndStoredExtraPhaseGrades()
    {
        var previous = AppSettings.Current.ExtraPhases;
        try
        {
            AppSettings.Current.ExtraPhases = "PEG";
            using var iso = new IsolatedDatabase();
            var panel = iso.Db.AddPanel("XP", "L-XP", "V", 2, null, false, startTypedNegative: true);
            iso.Db.AddSpecimen("XP-1", "serum", null);
            iso.Db.LinkSpecimenPanel("XP-1", panel);
            iso.Db.SaveReaction("XP-1", panel, "1", "0", "0", "0", "2+");
            var run = iso.Db.GetPanelRuns("XP-1", panel).Single();
            iso.Db.SaveReaction(run.RunId, "1", "0", "0", "0", "2+",
                new Dictionary<string, string> { ["PEG"] = "2+", ["Enz"] = "3+" });

            // Enz was graded, then removed from Preferences: it still counts in analysis, so it must still print.
            var text = iso.Reports.GeneratePreviewText(ReportType.ClinicalIdentification, "XP-1");
            var header = text.Split('\n').First(l => l.Contains("IS   37   AHG  CC"));
            Assert.Contains("PEG", header);
            Assert.Contains("Enz", header);
            var cell1 = text.Split('\n').First(l => l.StartsWith("1 ") && l.Contains("2+"));
            Assert.Contains("3+", cell1);
        }
        finally { AppSettings.Current.ExtraPhases = previous; }
    }
}
