using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]
public class RuleValidationTests
{
    private static readonly string[] Known = { "K", "k", "Fya", "Doa" };

    [Fact]
    public void Normalize_KeepsAntigenCase_AndStoresLowerCaseAnti()
    {
        Assert.Equal("anti-K", RuleValidation.NormalizeAntibody(" Anti-K ", Known, Array.Empty<Rule>(), null));
        Assert.Equal("anti-k", RuleValidation.NormalizeAntibody("anti-k", Known, Array.Empty<Rule>(), null));
    }

    [Fact]
    public void Normalize_RefusesNonAntibody_UnknownAntigen_AndSuggestsTheRightCase()
    {
        Assert.Contains("is not an antibody", Assert.Throws<ArgumentException>(() =>
            RuleValidation.NormalizeAntibody("Kell", Known, Array.Empty<Rule>(), null)).Message);
        Assert.Contains("not a known antigen", Assert.Throws<ArgumentException>(() =>
            RuleValidation.NormalizeAntibody("anti-Xyz", Known, Array.Empty<Rule>(), null)).Message);
        var ex = Assert.Throws<ArgumentException>(() => RuleValidation.NormalizeAntibody("anti-fya", Known, Array.Empty<Rule>(), null));
        Assert.Contains("Did you mean anti-Fya?", ex.Message);
    }

    [Fact]
    public void Normalize_RefusesASecondRuleForTheSameAntibody_ButNotForItsCaseVariant()
    {
        var existing = new[] { new Rule { RuleId = 7, Name = "K three", Antibody = "anti-K" } };
        Assert.Contains("Rule 'K three' already applies to anti-K", Assert.Throws<ArgumentException>(() =>
            RuleValidation.NormalizeAntibody("Anti-K", Known, existing, null)).Message);
        Assert.Equal("anti-K", RuleValidation.NormalizeAntibody("anti-K", Known, existing, editingRuleId: 7));
        Assert.Equal("anti-k", RuleValidation.NormalizeAntibody("anti-k", Known, existing, null));
    }

    [Fact]
    public void Problems_ListUnknownAntibodies_AndDuplicates_NamingTheRuleThatWins()
    {
        var rules = new[]
        {
            new Rule { RuleId = 1, Name = "b rule", Antibody = "anti-K" },
            new Rule { RuleId = 2, Name = "A rule", Antibody = "Anti-K" },
            new Rule { RuleId = 3, Name = "typo", Antibody = "anti-kell" },
            new Rule { RuleId = 4, Name = "small k", Antibody = "anti-k" },
        };
        var problems = RuleValidation.Problems(rules, Known);
        Assert.Contains(problems, p => p.Title == "Rule 'typo'" && p.Detail.Contains("never applies"));
        var dup = Assert.Single(problems, p => p.Title == "Rules for anti-K");
        Assert.Contains("Only 'A rule' is used", dup.Detail);
        Assert.DoesNotContain(problems, p => p.Title.Contains("anti-k"));
    }

    [Fact]
    public void Database_AddAndUpdate_Validate_AndWorklistListsStoredProblems()
    {
        using var iso = new IsolatedDatabase();
        var id = iso.Db.AddRule("K rule", null, "Anti-K", null, false, 2);
        Assert.Equal("anti-K", iso.Db.GetRule(id)!.Antibody);
        Assert.Throws<ArgumentException>(() => iso.Db.AddRule("K again", null, "anti-K", null, false, 3));
        Assert.Throws<ArgumentException>(() => iso.Db.AddRule("bad", null, "anti-Zxa", null, false, 1));

        // Warehouse antigens (for example Doa) are known, so a rule may name them.
        var doa = iso.Db.AddRule("Doa rule", null, "anti-Doa", null, true, 1);
        Assert.Throws<ArgumentException>(() => iso.Db.UpdateRule(doa, "Doa rule", null, "anti-K", null, true, 1));
        iso.Db.UpdateRule(doa, "Doa rule renamed", null, "anti-Doa", null, true, 2);
        Assert.Single(iso.Db.GetAllRules(), r => r.Antibody == "anti-K");
        Assert.DoesNotContain(iso.Db.GetWorklistItems(), i => i.Kind == WorklistKind.RuleConflict);

        // A rule stored before validation existed, with a typo, is listed for correction.
        using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={iso.DbPath};Pooling=False"))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO rules (name, antibody, heterozygous_ok, min_ruleout_count) VALUES ('old', 'anti-fya', 0, 1)";
            cmd.ExecuteNonQuery();
        }
        var item = Assert.Single(iso.Db.GetWorklistItems(), i => i.Kind == WorklistKind.RuleConflict);
        Assert.Equal("Rules", item.TargetTab);
        Assert.Contains("anti-fya", item.Detail);
    }
}
