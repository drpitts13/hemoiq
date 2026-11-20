using System.Globalization;
using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// Numbers in files HemoIQ writes (CSV exports, the Preferences export) use a decimal point
/// whatever the regional settings, and typed decimals are accepted with a point or a comma.
/// </summary>
[Collection("PersistentDatabase")]
public class CommaDecimalCultureTests
{
    private static T OnGerman<T>(Func<T> body)
    {
        T result = default!;
        Exception? error = null;
        var t = new Thread(() =>
        {
            try { result = body(); }
            catch (Exception ex) { error = ex; }
        }) { CurrentCulture = AppCulture.Normalize(CultureInfo.GetCultureInfo("de-DE")) };
        t.Start();
        t.Join();
        if (error != null) throw new Xunit.Sdk.XunitException(error.ToString());
        return result;
    }

    [Theory]
    [InlineData("0.70", 0.70)]
    [InlineData("0,70", 0.70)]
    [InlineData(" 0,5 ", 0.5)]
    public void TryParseDecimal_AcceptsPointOrSingleComma(string text, double expected)
    {
        Assert.True(LabSettings.TryParseDecimal(text, out var v));
        Assert.Equal(expected, v, 6);
        Assert.True(OnGerman(() => LabSettings.TryParseDecimal(text, out var g) && Math.Abs(g - expected) < 1e-9));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1,000.5")]
    [InlineData("0,5,1")]
    public void TryParseDecimal_RefusesAmbiguousOrInvalidText(string text) =>
        Assert.False(LabSettings.TryParseDecimal(text, out _));

    [Fact]
    public void TurnaroundCsv_UsesDecimalPoint_UnderGermanSettings()
    {
        var lines = OnGerman(() => AnalyticsViewModel.TurnaroundCsvLines(new TurnaroundResult
        {
            ReceivedToConfirmed = new DurationStats { Count = 2, Median = 1.5, Percentile90 = 2.25 },
        }).ToList());
        Assert.Contains("Received to confirmed,2,1.50,2.25,days", lines);
    }

    [Fact]
    public void AntibodyCsv_PercentUsesDecimalPoint_UnderGermanSettings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"hemoiq_csv_{Guid.NewGuid():N}.csv");
        try
        {
            OnGerman(() =>
            {
                AntibodyAnalyticsService.ExportToCsv(new AntibodyAnalyticsResult
                {
                    Rows = new List<AntibodyCountRow> { new() { Antibody = "anti-E", Count = 1, PercentOfIds = 12.5, Specimens = 1 } },
                }, new DateTime(2026, 11, 1), new DateTime(2026, 11, 30), path);
                return 0;
            });
            var text = File.ReadAllText(path);
            Assert.Contains("anti-E,1,12.5%,1,2026-11-01,2026-11-30", text);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void PreferencesExport_UsesDecimalPoint_UnderGermanSettings()
    {
        var settings = LabSettings.CreateDefault();
        settings.ProbabilityThreshold = 0.75;
        var json = OnGerman(() => PreferenceTransfer.Export(settings, new DateTime(2026, 11, 9)));
        Assert.Contains("\"probabilityThreshold\": 0.75", json);
        var preview = OnGerman(() => PreferenceTransfer.Preview(json, LabSettings.CreateDefault()));
        Assert.Contains(preview.Changes, c => c.Name == "probabilityThreshold" && c.After == "0.75");
    }
}
