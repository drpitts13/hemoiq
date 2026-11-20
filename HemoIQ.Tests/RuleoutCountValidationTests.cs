using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace HemoIQ.Tests;

/// <summary>
/// A rule's rule-out cell count is stored only within the range analysis uses (1 to 5), so the Rules tab never
/// shows a count that analysis silently replaces. Counts already stored outside it are reported.
/// </summary>
public class RuleoutCountValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-3)]
    public void AddRule_OutOfRangeCount_IsRefused(int count)
    {
        using var iso = new IsolatedDatabase();
        Assert.Throws<ArgumentException>(() => iso.Db.AddRule("Bad", null, "anti-K", null, false, count));
        Assert.Empty(iso.Db.GetAllRules().Where(r => r.Name == "Bad"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void AddRule_BoundaryCounts_AreStored(int count)
    {
        using var iso = new IsolatedDatabase();
        var id = iso.Db.AddRule("Edge", null, "anti-K", null, false, count);
        Assert.Equal(count, iso.Db.GetAllRules().Single(r => r.RuleId == id).MinRuleoutCount);
    }

    [Fact]
    public void UpdateRule_OutOfRangeCount_IsRefused_AndLeavesTheRuleUnchanged()
    {
        using var iso = new IsolatedDatabase();
        var id = iso.Db.AddRule("K rule", null, "anti-K", null, false, 2);
        Assert.Throws<ArgumentException>(() => iso.Db.UpdateRule(id, "K rule", null, "anti-K", null, false, 9));
        Assert.Equal(2, iso.Db.GetAllRules().Single(r => r.RuleId == id).MinRuleoutCount);
    }

    [Fact]
    public void StoredOutOfRangeCount_IsReportedAsAProblem_WithTheCountAnalysisUses()
    {
        using var iso = new IsolatedDatabase();
        var id = iso.Db.AddRule("Legacy", null, "anti-K", null, false, 2);
        using (var conn = new SqliteConnection($"Data Source={iso.DbPath};Pooling=False"))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"UPDATE rules SET min_ruleout_count = 0 WHERE rule_id = {id}";
            cmd.ExecuteNonQuery();
        }

        var problems = RuleValidation.Problems(iso.Db.GetAllRules(), iso.Db.GetKnownAntigens());

        var p = Assert.Single(problems, x => x.Title == "Rule 'Legacy'");
        Assert.Equal("Rule-out cells is stored as 0, outside 1 to 5, so analysis uses 1. Edit the rule and enter the count you intend.",
            p.Detail);
    }
}
