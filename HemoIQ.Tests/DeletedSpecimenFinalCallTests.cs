using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// The Analysis tab can still list a specimen that another workstation deleted. Confirming or clearing its
/// identification must be refused, not recorded in the audit log and reported as done for a specimen that is gone.
/// </summary>
[Collection("PersistentDatabase")]
public class DeletedSpecimenFinalCallTests
{
    [Fact]
    public void ConfirmingOnADeletedSpecimen_IsRefused_AndNotAudited()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("DFC-1", "serum", null);
        iso.Db.DeleteSpecimen("DFC-1");
        var ex = Assert.Throws<InvalidOperationException>(() =>
            iso.Db.SetSpecimenFinalCall("DFC-1", "anti-K", null, "AB"));
        Assert.Contains("DFC-1 no longer exists", ex.Message);
        Assert.Contains("not confirmed", ex.Message);
        Assert.DoesNotContain(iso.Db.GetAuditEventsForEntity("specimen", "DFC-1"), e => e.Action == "confirm_final_call");
    }

    [Fact]
    public void ClearingOnADeletedSpecimen_IsRefused_AndNotAudited()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("DFC-2", "serum", null);
        iso.Db.DeleteSpecimen("DFC-2");
        var ex = Assert.Throws<InvalidOperationException>(() => iso.Db.ClearSpecimenFinalCall("DFC-2", "wrong lot"));
        Assert.Contains("not cleared", ex.Message);
        Assert.DoesNotContain(iso.Db.GetAuditEventsForEntity("specimen", "DFC-2"), e => e.Action == "clear_final_call");
    }

    [Fact]
    public void ConfirmAndClear_StillWorkOnAnExistingSpecimen()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("DFC-3", "serum", null);
        iso.Db.SetSpecimenFinalCall("DFC-3", "anti-K", null, "AB");
        Assert.True(iso.Db.GetSpecimen("DFC-3")!.HasFinalCall);
        iso.Db.ClearSpecimenFinalCall("DFC-3", "re-test");
        Assert.False(iso.Db.GetSpecimen("DFC-3")!.HasFinalCall);
    }

    [Fact]
    public void TheAnalysisTab_ShowsTheRefusal_ForConfirmAndClear()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "ViewModels", "AnalysisViewModel.cs"));
        var confirm = src[src.IndexOf("_db.SetSpecimenFinalCall(")..];
        Assert.Contains("catch (InvalidOperationException ex)", confirm[..confirm.IndexOf("RememberInitials(")]);
        var clear = src[src.IndexOf("_db.ClearSpecimenFinalCall(")..];
        Assert.Contains("or InvalidOperationException", clear[..clear.IndexOf("LoadFinalCall(")]);
    }
}
