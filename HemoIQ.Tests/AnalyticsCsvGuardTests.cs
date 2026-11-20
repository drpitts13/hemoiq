using System.Text.RegularExpressions;
using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// The Analytics CSV is built line by line rather than through <see cref="CsvText.Writer"/>, so the source guard in
/// <see cref="CsvInjectionTests"/> cannot see it (Iteration 186 found an unprotected line). This feeds a spreadsheet
/// formula into every text field of every Analytics line builder and fails if any output field could run as one.
/// </summary>
public class AnalyticsCsvGuardTests
{
    private const string Formula = "=HYPERLINK(\"http://x\")";

    // A field that starts a cell (line start or after a comma), optionally inside quotes, with a formula character.
    private static readonly Regex LiveFormula = new(@"(^|,)""?[=+@\t]", RegexOptions.Compiled);

    private static void AssertNoLiveFormula(IEnumerable<string> lines, string builder)
    {
        foreach (var line in lines)
            Assert.False(LiveFormula.IsMatch(line), $"{builder} wrote a live formula: {line}");
    }

    [Fact]
    public void EveryAnalyticsCsvBuilder_NeutralizesFormulasInTextFields()
    {
        AssertNoLiveFormula(AnalyticsViewModel.TurnaroundCsvLines(new TurnaroundResult
        {
            ThresholdDays = 3,
            Overdue = { new OverdueInvestigation { Accession = Formula, Received = Formula, AgeDays = 4, Status = Formula } },
        }), nameof(AnalyticsViewModel.TurnaroundCsvLines));

        AssertNoLiveFormula(AnalyticsViewModel.WorkloadCsvLines(new[]
        {
            new TechnologistWorkload { Initials = Formula, Name = Formula, Confirmed = 1 },
        }), nameof(AnalyticsViewModel.WorkloadCsvLines));

        AssertNoLiveFormula(AnalyticsViewModel.LocationCsvLines(new[]
        {
            new LocationWorkload { Location = Formula, Received = 1 },
        }), nameof(AnalyticsViewModel.LocationCsvLines));

        AssertNoLiveFormula(AnalyticsViewModel.UserActivityCsvLines(new[]
        {
            new UserActivity { Login = Formula, Name = Formula },
        }), nameof(AnalyticsViewModel.UserActivityCsvLines));

        AssertNoLiveFormula(AnalyticsViewModel.OverrideCsvLines(new[]
        {
            new AnalyticsCase { Accession = Formula, Date = Formula, FinalAntibodies = Formula, Detail = Formula },
        }), nameof(AnalyticsViewModel.OverrideCsvLines));
    }

    [Fact]
    public void Guard_DetectsAnUnprotectedLine()
    {
        // The guard itself must catch the kind of line Iteration 186 fixed.
        Assert.True(LiveFormula.IsMatch($"{Formula},2026-11-01,9,Open"));
        Assert.True(LiveFormula.IsMatch($"2026-001,\"{Formula}\""));
        Assert.False(LiveFormula.IsMatch("2026-001,'=HYPERLINK(\"http://x\"),9,Open"));
        Assert.False(LiveFormula.IsMatch("anti-K,-5,2+"));
    }
}
