using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]
public class RuleTransferTests
{
    [Fact]
    public void ExportThenImportIntoEmptyDatabase_RecreatesTheRules_AndIsAudited()
    {
        string json;
        using (var source = new IsolatedDatabase())
        {
            source.Db.AddRule("Kell three", "lab policy", "anti-K", null, false, 3);
            source.Db.AddRule("f exempt", null, "anti-f", null, true, 1, separationExempt: true, separationExemptFrom: "e");
            json = RuleTransfer.Export(source.Db.GetAllRules(), new DateTime(2026, 11, 9, 10, 0, 0));
        }
        Assert.Contains("\"format\": \"hemoiq-rules\"", json);

        using var target = new IsolatedDatabase();
        var preview = RuleTransfer.Preview(json, target.Db.GetAllRules(), target.Db.GetKnownAntigens());
        Assert.All(preview, l => Assert.Equal(RuleImportAction.Add, l.Action));
        Assert.Empty(target.Db.GetAllRules()); // preview writes nothing

        Assert.Equal((2, 0), RuleTransfer.Apply(target.Db, preview));
        var k = Assert.Single(target.Db.GetAllRules(), r => r.Antibody == "anti-K");
        Assert.Equal(3, k.MinRuleoutCount);
        Assert.Equal("lab policy", k.Description);
        var f = Assert.Single(target.Db.GetAllRules(), r => r.Antibody == "anti-f");
        Assert.True(f.SeparationExempt);
        Assert.Equal("e", f.SeparationExemptFrom);
        Assert.Equal(2, target.Db.GetAuditEvents().Count(e => e.Action == "add_rule"));
    }

    [Fact]
    public void Preview_MatchesByAntibody_ListsUpdatesAndUnchanged_AndKeepsRulesMissingFromTheFile()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddRule("K rule", null, "anti-K", null, false, 2);
        iso.Db.AddRule("Fya rule", null, "anti-Fya", null, false, 1);
        iso.Db.AddRule("Keep me", null, "anti-Jka", null, false, 2);
        var json = """
        { "format": "hemoiq-rules", "version": 1, "rules": [
            { "name": "K rule", "antibody": "Anti-K", "minRuleoutCount": 3 },
            { "name": "Fya rule", "antibody": "anti-Fya", "minRuleoutCount": 1 }
        ] }
        """;
        var preview = RuleTransfer.Preview(json, iso.Db.GetAllRules(), iso.Db.GetKnownAntigens());
        var k = Assert.Single(preview, l => l.Antibody == "anti-K");
        Assert.Equal(RuleImportAction.Update, k.Action);
        Assert.Contains("rule-out cells 2 → 3", k.Detail);
        Assert.Equal(RuleImportAction.Unchanged, Assert.Single(preview, l => l.Antibody == "anti-Fya").Action);

        Assert.Equal((0, 1), RuleTransfer.Apply(iso.Db, preview));
        Assert.Equal(3, iso.Db.GetAllRules().Single(r => r.Antibody == "anti-K").MinRuleoutCount);
        Assert.Single(iso.Db.GetAllRules(), r => r.Name == "Keep me");
        Assert.Contains("1 to update, 1 unchanged", RuleTransfer.Summarize(preview));
    }

    [Fact]
    public void Preview_RefusesBadEntries_WithReasons_AndSkipsThemOnApply()
    {
        using var iso = new IsolatedDatabase();
        var json = """
        { "format": "hemoiq-rules", "version": 1, "rules": [
            { "name": "typo", "antibody": "anti-fya" },
            { "name": "", "antibody": "anti-K" },
            { "name": "count", "antibody": "anti-E", "minRuleoutCount": 9 },
            { "name": "exc", "antibody": "anti-C", "exceptionAntigen": "Zz" },
            { "name": "S one", "antibody": "anti-S" },
            { "name": "S two", "antibody": "anti-S" }
        ] }
        """;
        var preview = RuleTransfer.Preview(json, iso.Db.GetAllRules(), iso.Db.GetKnownAntigens());
        Assert.Contains("Did you mean anti-Fya?", preview.Single(l => l.Name == "typo").Detail);
        Assert.Contains("no name", preview.Single(l => l.Antibody == "anti-K").Detail);
        Assert.Contains("outside 1–5", preview.Single(l => l.Name == "count").Detail);
        Assert.Contains("Zz is not a known antigen", preview.Single(l => l.Name == "exc").Detail);
        Assert.Equal(RuleImportAction.Add, preview.Single(l => l.Name == "S one").Action);
        Assert.Contains("more than one rule for anti-S", preview.Single(l => l.Name == "S two").Detail);

        Assert.Equal((1, 0), RuleTransfer.Apply(iso.Db, preview));
        Assert.Single(iso.Db.GetAllRules());
    }

    [Fact]
    public void Preview_RefusesUnknownSeparationPartners_ButAcceptsKnownOnes()
    {
        using var iso = new IsolatedDatabase();
        var json = """
        { "format": "hemoiq-rules", "version": 1, "rules": [
            { "name": "bad", "antibody": "anti-f", "separationExempt": true, "separationExemptFrom": "e, Le(a)" },
            { "name": "good", "antibody": "anti-K", "separationExempt": true, "separationExemptFrom": "anti-E, c" },
            { "name": "off", "antibody": "anti-S", "separationExempt": false, "separationExemptFrom": "Zz" }
        ] }
        """;
        var preview = RuleTransfer.Preview(json, iso.Db.GetAllRules(), iso.Db.GetKnownAntigens());

        var bad = preview.Single(l => l.Name == "bad");
        Assert.Equal(RuleImportAction.Error, bad.Action);
        Assert.Contains("unknown antigen(s) Le(a)", bad.Detail);
        Assert.DoesNotContain(" e,", bad.Detail);
        Assert.Equal(RuleImportAction.Add, preview.Single(l => l.Name == "good").Action);
        // Not exempt: the list is ignored and not stored.
        Assert.Equal(RuleImportAction.Add, preview.Single(l => l.Name == "off").Action);

        Assert.Equal((2, 0), RuleTransfer.Apply(iso.Db, preview));
        var rules = iso.Db.GetAllRules();
        Assert.DoesNotContain(rules, r => r.Name == "bad");
        Assert.Equal("E, c", rules.Single(r => r.Name == "good").SeparationExemptFrom);
        Assert.Null(rules.Single(r => r.Name == "off").SeparationExemptFrom);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{ "format": "something-else", "rules": [] }""")]
    [InlineData("""{ "format": "hemoiq-rules", "version": 99, "rules": [] }""")]
    public void Preview_RejectsFilesThatAreNotRuleExports(string json)
    {
        using var iso = new IsolatedDatabase();
        Assert.Throws<ArgumentException>(() => RuleTransfer.Preview(json, iso.Db.GetAllRules(), iso.Db.GetKnownAntigens()));
    }

    [Fact]
    public void Apply_IsAllOrNothing_WhenAWriteFails()
    {
        using var iso = new IsolatedDatabase();
        var json = """
        { "format": "hemoiq-rules", "version": 1, "rules": [
            { "name": "K", "antibody": "anti-K" }, { "name": "E", "antibody": "anti-E" } ] }
        """;
        var preview = RuleTransfer.Preview(json, iso.Db.GetAllRules(), iso.Db.GetKnownAntigens());
        // Another user adds a rule for anti-E after the preview: the second add is refused and the first is rolled back.
        iso.Db.AddRule("E meanwhile", null, "anti-E", null, false, 1);
        Assert.Throws<ArgumentException>(() => RuleTransfer.Apply(iso.Db, preview));
        Assert.DoesNotContain(iso.Db.GetAllRules(), r => r.Antibody == "anti-K");
    }
}
