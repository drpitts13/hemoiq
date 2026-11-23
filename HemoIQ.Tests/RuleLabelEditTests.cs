using HemoIQ.Data;
using HemoIQ.Tests.Infrastructure;
using HemoIQ.ViewModels;

namespace HemoIQ.Tests;

/// <summary>
/// Renaming a rule or editing its description is audited but does not mark open analyses stale (analysis never
/// reads either); a change to what analysis reads does. The Edit status says which happened, and an unchanged save
/// says nothing was saved instead of claiming investigations are now stale.
/// </summary>
[Collection("PersistentDatabase")]
public class RuleLabelEditTests
{
    private static (IsolatedDatabase Iso, int Rule) Analyzed()
    {
        var iso = new IsolatedDatabase();
        var rule = iso.Db.AddRule("Kell three", "policy", "anti-K", null, false, 3);
        var panel = iso.Db.AddPanel("RL", "L-RL", "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen("RLE-1", "serum", null);
        iso.Db.LinkSpecimenPanel("RLE-1", panel);
        iso.Db.SaveReaction("RLE-1", panel, "1", "0", "0", "0", "2+");
        iso.Analyzer.AnalyzeSpecimen("RLE-1", updateDb: true);
        Assert.False(iso.Db.IsSpecimenAnalysisStale("RLE-1"));
        return (iso, rule);
    }

    [Fact]
    public void RenameOrDescription_IsAudited_ButDoesNotMarkAnalysesStale()
    {
        var (iso, rule) = Analyzed();
        using (iso)
        {
            var outcome = iso.Db.UpdateRule(rule, "Kell - three cells", "lab policy 2026", "anti-K", null, false, 3);
            Assert.Equal(DatabaseService.RuleEditResult.LabelOnly, outcome);
            Assert.Contains(iso.Db.GetAuditEventsForEntity("rule", rule.ToString()), e => e.Action == "update_rule_label");
            Assert.False(iso.Db.IsSpecimenAnalysisStale("RLE-1"));
        }
    }

    [Fact]
    public void ChangeToWhatAnalysisReads_MarksAnalysesStale()
    {
        var (iso, rule) = Analyzed();
        using (iso)
        {
            var outcome = iso.Db.UpdateRule(rule, "Kell three", "policy", "anti-K", null, false, 2);
            Assert.Equal(DatabaseService.RuleEditResult.AnalysisChanged, outcome);
            Assert.Contains(iso.Db.GetAuditEventsForEntity("rule", rule.ToString()), e => e.Action == "update_rule");
            Assert.True(iso.Db.IsSpecimenAnalysisStale("RLE-1"));
        }
    }

    [Fact]
    public void UnchangedSave_WritesNothing()
    {
        var (iso, rule) = Analyzed();
        using (iso)
        {
            var before = iso.Db.GetAuditEvents(1000).Count;
            Assert.Equal(DatabaseService.RuleEditResult.Unchanged, iso.Db.UpdateRule(rule, "Kell three", "policy", "anti-K", null, false, 3));
            Assert.Equal(before, iso.Db.GetAuditEvents(1000).Count);
            Assert.False(iso.Db.IsSpecimenAnalysisStale("RLE-1"));
        }
    }

    [Fact]
    public void EditStatus_SaysOnlyWhatHappened()
    {
        Assert.Equal("Rule 'K' unchanged; nothing was saved.", RulesViewModel.EditStatus("K", DatabaseService.RuleEditResult.Unchanged));
        Assert.Contains("analyses are not affected", RulesViewModel.EditStatus("K", DatabaseService.RuleEditResult.LabelOnly));
        Assert.DoesNotContain("Stale analysis", RulesViewModel.EditStatus("K", DatabaseService.RuleEditResult.LabelOnly));
        Assert.Contains("Stale analysis", RulesViewModel.EditStatus("K", DatabaseService.RuleEditResult.AnalysisChanged));
    }
}
