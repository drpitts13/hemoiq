using HemoIQ.Models;
using HemoIQ.Services;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]
public class AcsAntigenConfigurationTests
{
    private static Dictionary<string, int> AllRuledOut(IEnumerable<string> antigens, int count = 3) =>
        antigens.ToDictionary(ag => $"anti-{ag}", _ => count);

    private static T WithAdditions<T>(string additions, Func<T> body)
    {
        var previous = AppSettings.Current.AcsAdditionalAntigens;
        try
        {
            AppSettings.Current.AcsAdditionalAntigens = additions;
            return body();
        }
        finally
        {
            AppSettings.Current.AcsAdditionalAntigens = previous;
        }
    }

    [Fact]
    public void Default_IsTheCorePlusLewisAndMn_SameAsBefore()
    {
        Assert.Equal(AntigenConstants.ClinicallySignificantAntigens, new LabSettings().AcsAntigenList());
        Assert.Equal(new[] { "Lea", "Leb", "M", "N" }, new LabSettings().AcsAntigenList().Skip(AntigenConstants.AcsCoreAntigens.Count));
    }

    [Fact]
    public void Parse_KeepsKnownAntigensInAntigramOrder_DropsCoreAndRepeats_RefusesUnknownOrWrongCase()
    {
        Assert.True(AntigenConstants.TryParseAcsAdditions("P1, Lea Kpa; Lea, D", out var normalized, out var problem));
        Assert.Null(problem);
        Assert.Equal("Kpa, Lea, P1", normalized);

        Assert.False(AntigenConstants.TryParseAcsAdditions("lea, Xyz", out _, out problem));
        Assert.Equal("lea, Xyz are not known antigens (names are case-sensitive)", problem);

        Assert.True(AntigenConstants.TryParseAcsAdditions("  ", out normalized, out _));
        Assert.Equal("", normalized);
        Assert.Equal(AntigenConstants.AcsCoreAntigens, AntigenConstants.AcsAntigens(""));
    }

    [Fact]
    public void CoreOnly_AcsIsEligible_WhenLewisAndMnAreNotRuledOut()
    {
        var ruledOut = AllRuledOut(AntigenConstants.AcsCoreAntigens);

        var byDefault = AntibodyAnalyzer.EvaluateAcs(ruledOut, new Dictionary<string, double>());
        Assert.False(byDefault.IsEligible);
        Assert.Contains(byDefault.Shortfalls, s => s.Antibody == "anti-Lea");

        var coreOnly = WithAdditions("", () => AntibodyAnalyzer.EvaluateAcs(ruledOut, new Dictionary<string, double>()));
        Assert.True(coreOnly.IsEligible);
        Assert.Equal(AntigenConstants.AcsCoreAntigens, coreOnly.Antigens);
    }

    [Fact]
    public void AddedAntigen_MustAlsoBeRuledOut()
    {
        var ruledOut = AllRuledOut(AntigenConstants.ClinicallySignificantAntigens);
        var acs = WithAdditions("Lea, Leb, M, N, P1", () => AntibodyAnalyzer.EvaluateAcs(ruledOut, new Dictionary<string, double>()));

        Assert.False(acs.IsEligible);
        var p1 = Assert.Single(acs.Shortfalls);
        Assert.Equal("anti-P1", p1.Antibody);
        Assert.Equal(0, p1.Count);
        Assert.Contains("P1", acs.Antigens);

        ruledOut["anti-P1"] = 3;
        Assert.True(WithAdditions("Lea, Leb, M, N, P1", () => AntibodyAnalyzer.EvaluateAcs(ruledOut, new Dictionary<string, double>())).IsEligible);
    }

    [Fact]
    public void RuleTrace_RecordsAndExplainsTheConfiguredList()
    {
        var settings = new LabSettings { AcsAdditionalAntigens = "Kpa" };
        Assert.Contains("\"AcsAntigens\":[\"D\",", AnalysisRuleTrace.Serialize(Array.Empty<Rule>(), settings));
        Assert.Contains("\"Kpa\"]", AnalysisRuleTrace.Serialize(Array.Empty<Rule>(), settings));
        var text = AnalysisRuleTrace.Explain(Array.Empty<Rule>(), settings);
        Assert.Contains("for anti-D, anti-C,", text);
        Assert.Contains("anti-s, anti-Kpa;", text);
        Assert.DoesNotContain("anti-Lea", text);
    }

    [Fact]
    public void ChangingTheList_ChangesTheClinicalSnapshot_SoAnalysesBecomeStale()
    {
        var a = System.Text.Json.JsonSerializer.Serialize(new LabSettings().ClinicalSnapshot());
        var b = System.Text.Json.JsonSerializer.Serialize(new LabSettings { AcsAdditionalAntigens = "Lea, Leb" }.ClinicalSnapshot());
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Clamp_DropsUnknownNames_KeepsKnownOnes()
    {
        var s = new LabSettings { AcsAdditionalAntigens = "Lea, nonsense, P1" };
        s.Clamp();
        Assert.Equal("Lea, P1", s.AcsAdditionalAntigens);
    }

    [Fact]
    public void PreferenceTransfer_CarriesTheList_AndRefusesUnknownNames()
    {
        var source = new LabSettings { AcsAdditionalAntigens = "Lea, Leb, M, N, P1" };
        var json = PreferenceTransfer.Export(source, new DateTime(2026, 11, 9));
        var preview = PreferenceTransfer.Preview(json, new LabSettings());
        var change = Assert.Single(preview.Changes, c => c.Name == "acsAdditionalAntigens");
        Assert.Equal("Lea, Leb, M, N, P1", change.After);

        var bad = json.Replace("\"Lea, Leb, M, N, P1\"", "\"lea\"");
        Assert.Contains(PreferenceTransfer.Preview(bad, new LabSettings()).Refused,
            r => r.StartsWith("acsAdditionalAntigens: lea is not a known antigen"));
    }
}
