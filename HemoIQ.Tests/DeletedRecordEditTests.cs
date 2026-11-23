using HemoIQ.Data;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// A rule or panel can be deleted on another workstation while it is being edited here. Saving the edit must say the
/// record no longer exists, not report "unchanged" or "no typing changes" and lose the edit without a word.
/// </summary>
[Collection("PersistentDatabase")]
public class DeletedRecordEditTests
{
    [Fact]
    public void EditingADeletedRule_IsRefused_AndNothingIsAudited()
    {
        using var iso = new IsolatedDatabase();
        var rule = iso.Db.AddRule("Gone", null, "anti-K", null, false, 3);
        iso.Db.DeleteRule(rule);
        var ex = Assert.Throws<InvalidOperationException>(() =>
            iso.Db.UpdateRule(rule, "Gone", null, "anti-K", null, false, 2));
        Assert.Contains("no longer exists", ex.Message);
        Assert.Null(iso.Db.GetRule(rule));
        Assert.DoesNotContain(iso.Db.GetAuditEventsForEntity("rule", rule.ToString()),
            e => e.Action is "update_rule" or "update_rule_label");
    }

    [Fact]
    public void EditingAnExistingRule_StillSaves()
    {
        using var iso = new IsolatedDatabase();
        var rule = iso.Db.AddRule("Here", null, "anti-K", null, false, 3);
        Assert.Equal(DatabaseService.RuleEditResult.AnalysisChanged,
            iso.Db.UpdateRule(rule, "Here", null, "anti-K", null, false, 2));
        Assert.Equal(2, iso.Db.GetRule(rule)!.MinRuleoutCount);
    }

    [Fact]
    public void EditingTypingOnADeletedPanel_IsRefused()
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("Gone", "L-GONE", "V", 2, null, false, startTypedNegative: true);
        var cells = iso.Db.GetPanelCells(panel);
        iso.Db.DeletePanel(panel);
        var ex = Assert.Throws<InvalidOperationException>(() => iso.Db.SavePanelCellEdits(panel, cells));
        Assert.Contains("no longer exists", ex.Message);
        Assert.DoesNotContain(iso.Db.GetAuditEventsForEntity("panel", panel.ToString()), e => e.Action == "edit_panel_typing");
    }

    [Fact]
    public void EditingTypingOnAnExistingPanel_StillSaves()
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("Here", "L-HERE", "V", 2, null, false, startTypedNegative: true);
        var cells = iso.Db.GetPanelCells(panel);
        var first = cells.First(c => c.CellNumber == "1");
        first.Antigens["K"] = "+";
        var changes = iso.Db.SavePanelCellEdits(panel, cells);
        Assert.NotEmpty(changes);
        Assert.Equal("+", iso.Db.GetPanelCells(panel).First(c => c.CellNumber == "1").Antigens["K"]);
    }
}
