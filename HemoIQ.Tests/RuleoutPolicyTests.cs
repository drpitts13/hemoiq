using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// Direct tests of the rule-out policy. Antigen names are case-sensitive, so a rule written
/// for anti-K must not govern anti-k (or C/c, E/e, S/s), while "Anti-" and "anti-" match.
/// </summary>
[Collection("PersistentDatabase")]
public class RuleoutPolicyTests
{
    private static Rule R(string antibody, int count = 1, bool het = false, string? exception = null) => new()
    {
        Name = antibody, Antibody = antibody, MinRuleoutCount = count, HeterozygousOk = het, ExceptionAntigen = exception,
    };

    [Theory]
    [InlineData("anti-K", "Anti-K", true)]
    [InlineData(" anti-Fya ", "anti-Fya", true)]
    [InlineData("anti-K", "anti-k", false)]
    [InlineData("anti-C", "anti-c", false)]
    [InlineData("anti-E", "anti-e", false)]
    [InlineData("anti-S", "anti-s", false)]
    [InlineData("anti-K", "K", false)]
    [InlineData(null, null, false)]
    public void SameAntibody_IgnoresPrefixCase_ButNotAntigenCase(string? a, string? b, bool expected) =>
        Assert.Equal(expected, RuleoutPolicy.SameAntibody(a, b));

    [Fact]
    public void FindAntibodyRule_AndRequiredCount_DoNotCrossCase()
    {
        var rules = new[] { R("anti-K", count: 3), R("Anti-e", count: 2) };
        Assert.Equal(3, RuleoutPolicy.ResolveRequiredCount("anti-K", rules, 1));
        Assert.Equal(1, RuleoutPolicy.ResolveRequiredCount("anti-k", rules, 1));
        Assert.Equal(2, RuleoutPolicy.ResolveRequiredCount("anti-e", rules, 1));
        Assert.Equal(1, RuleoutPolicy.ResolveRequiredCount("anti-E", rules, 1));
        Assert.Null(RuleoutPolicy.FindAntibodyRule("anti-c", new[] { R("anti-C") }));
        Assert.Equal(5, RuleoutPolicy.ResolveRequiredCount("anti-K", new[] { R("anti-K", count: 9) }, 1));
        Assert.Equal(1, RuleoutPolicy.ResolveRequiredCount("anti-K", new[] { R("anti-K", count: 0) }, 1));
    }

    [Fact]
    public void HeterozygousAllowed_ByAntibodyOrExceptionAntigen_CaseSensitive()
    {
        var rules = new[] { R("anti-C", het: true), R("anti-Jkb", het: true, exception: "Jkb"), R("anti-D", het: true, exception: "E"),
                            R("anti-Fya", het: false) };
        Assert.True(RuleoutPolicy.HeterozygousAllowed("C", rules));
        Assert.False(RuleoutPolicy.HeterozygousAllowed("c", rules));
        Assert.True(RuleoutPolicy.HeterozygousAllowed("Jkb", rules));
        Assert.False(RuleoutPolicy.HeterozygousAllowed("jkb", rules));
        Assert.False(RuleoutPolicy.HeterozygousAllowed("Fya", rules));
        // An exception antigen on another antibody's rule does not widen heterozygous use.
        Assert.False(RuleoutPolicy.HeterozygousAllowed("E", rules));
    }

    [Fact]
    public void FormatExplanation_NamesCellsRequirementAndZygosity()
    {
        var met = new RuleoutEvaluation
        {
            Antibody = "anti-Fya", ObservedCount = 2, RequiredCount = 2, MeetsCriteria = true, HeterozygousAllowed = false,
            Cells = new List<RuleoutDetail> { new() { CellNumber = "2" }, new() { CellNumber = "6" } },
        };
        Assert.Equal("Anti-Fya currently meets configured rule-out criteria because 2 antigen-positive nonreactive cells were observed " +
                     "(cells 2, 6) (required 2; heterozygous cells not allowed).", RuleoutPolicy.FormatExplanation(met));

        var short1 = new RuleoutEvaluation { Antibody = "anti-Jka", ObservedCount = 0, RequiredCount = 1, HeterozygousAllowed = false };
        Assert.Equal("Anti-Jka does not yet meet configured rule-out criteria (0 of 1 homozygous antigen-positive nonreactive cells).",
            RuleoutPolicy.FormatExplanation(short1));
    }

    [Fact]
    public void Analysis_ARuleForAntiK_DoesNotRaiseTheRequirementForAntik()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddRule("K needs three", null, "anti-K", null, false, 3);
        iso.Db.AddSpecimen("RP-1", "serum", null);
        var panel = iso.Db.AddPanel("RP P", "RPL", "V", 2, null, false, startTypedNegative: true);
        var cells = iso.Db.GetPanelCells(panel);
        // Cell 1 is K-k+ and cell 2 is K+k-, both nonreactive: one homozygous rule-out cell each.
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "k", "+");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "K", "+");
        iso.Db.LinkSpecimenPanel("RP-1", panel);
        iso.Db.SaveReaction("RP-1", panel, "1", "0", "0", "0", "2+");
        iso.Db.SaveReaction("RP-1", panel, "2", "0", "0", "0", "2+");

        var result = iso.Analyzer.AnalyzeSpecimen("RP-1", updateDb: false);
        var antiK = Assert.Single(result.RuleoutEvaluations, e => e.Antibody == "anti-K");
        Assert.Equal(3, antiK.RequiredCount);
        Assert.False(antiK.MeetsCriteria);
        var antik = Assert.Single(result.RuleoutEvaluations, e => e.Antibody == "anti-k");
        Assert.Equal(AppSettings.Current.DefaultMinRuleoutCount, antik.RequiredCount);
        Assert.True(antik.MeetsCriteria);
    }
}
