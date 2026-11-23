using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// Attaching a panel used to treat every database error as "already linked", so a specimen or lot deleted on another
/// workstation was reported as attached. Each is now refused in words; a repeated link is still a quiet no-op.
/// </summary>
public class LinkPanelMissingRecordTests
{
    [Fact]
    public void LinkingToADeletedSpecimen_IsRefused()
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("LP", "L-LP1", "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen("LP-1", "serum", null);
        iso.Db.DeleteSpecimen("LP-1");
        var ex = Assert.Throws<InvalidOperationException>(() => iso.Db.LinkSpecimenPanel("LP-1", panel));
        Assert.Contains("LP-1 no longer exists", ex.Message);
        Assert.DoesNotContain(iso.Db.GetAuditEventsForEntity("specimen", "LP-1"), e => e.Action == "link_panel");
    }

    [Fact]
    public void LinkingADeletedPanel_IsRefused()
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("LP", "L-LP2", "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen("LP-2", "serum", null);
        iso.Db.DeletePanel(panel);
        var ex = Assert.Throws<InvalidOperationException>(() => iso.Db.LinkSpecimenPanel("LP-2", panel));
        Assert.Contains($"Panel #{panel} no longer exists", ex.Message);
        Assert.Empty(iso.Db.GetSpecimenPanels("LP-2"));
    }

    [Fact]
    public void LinkingTwice_IsAQuietNoOp_AuditedOnce()
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("LP", "L-LP3", "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen("LP-3", "serum", null);
        iso.Db.LinkSpecimenPanel("LP-3", panel);
        iso.Db.LinkSpecimenPanel("LP-3", panel);
        Assert.Single(iso.Db.GetSpecimenPanels("LP-3"));
        Assert.Single(iso.Db.GetAuditEventsForEntity("specimen", "LP-3"), e => e.Action == "link_panel");
    }

    [Fact]
    public void AttachPanel_ShowsTheRefusal()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "ViewModels", "SpecimensViewModel.cs"));
        var body = src[src.IndexOf("private void AttachPanel()")..];
        body = body[..body.IndexOf("private void DetachPanel()")];
        Assert.Contains("\"Panel not attached\"", body);
        Assert.True(body.IndexOf("catch") < body.IndexOf("Panel '"), "the refusal is caught before the success status");
    }
}
