using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// Edit is disabled on a confirmed or cancelled investigation and Delete on a confirmed one, each with the reason as
/// its tooltip. The Delete refusal gives a next step that works: clear the identification, then cancel (cancelling a
/// confirmed investigation is itself refused).
/// </summary>
[Collection("PersistentDatabase")]
public class EditDeleteLockTests
{
    private static void As(string login, Action body)
    {
        var previous = AccessControl.CurrentLogin;
        try { AccessControl.CurrentLogin = login; body(); }
        finally { AccessControl.CurrentLogin = previous; }
    }

    private static string Source(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        return File.ReadAllText(Path.Combine(new[] { dir!.FullName, "HemoIQ" }.Concat(parts).ToArray()));
    }

    [Fact]
    public void DeleteAdvice_Works_ClearThenCancel()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("ED-1", "serum", null);
        iso.Db.SetSpecimenFinalCall("ED-1", "anti-K", null, "TE");
        var refusal = iso.Db.DeleteRefusal(iso.Db.GetSpecimen("ED-1")!);
        Assert.Contains("clear the identification with a reason", refusal);
        Assert.Throws<InvalidOperationException>(() => iso.Db.CancelInvestigation("ED-1", "withdrawn")); // what the old advice said

        iso.Db.ClearSpecimenFinalCall("ED-1", "entered on the wrong accession");
        iso.Db.CancelInvestigation("ED-1", "withdrawn");
        Assert.True(iso.Db.GetSpecimen("ED-1")!.IsCancelled);
        Assert.Null(iso.Db.DeleteRefusal(iso.Db.GetSpecimen("ED-1")!)); // no longer blocked by a confirmed identification
    }

    [Fact]
    public void DeleteAdvice_NamesWhoCanClear_ForATechnologist()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("ED-2", "serum", null);
        As("adm", () =>
        {
            iso.Db.SaveUser(new AppUser { Login = "adm", Initials = "AD", Name = "Admin", Role = UserRole.Administrator });
            iso.Db.SaveUser(new AppUser { Login = "tech", Initials = "TE", Name = "Tech", Role = UserRole.Technologist });
        });
        As("tech", () =>
        {
            iso.Db.SetSpecimenFinalCall("ED-2", "anti-E", null, "TE");
            var refusal = iso.Db.DeleteRefusal(iso.Db.GetSpecimen("ED-2")!);
            Assert.Contains("Ask a Supervisor or Administrator to clear the identification", refusal);
            var ex = Assert.Throws<InvalidOperationException>(() => iso.Db.DeleteSpecimen("ED-2"));
            Assert.Equal(refusal, ex.Message);
        });
    }

    [Fact]
    public void OpenSpecimen_HasNoDeleteRefusal()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("ED-3", "serum", null);
        Assert.Null(iso.Db.DeleteRefusal(iso.Db.GetSpecimen("ED-3")!));
    }

    [Fact]
    public void EditAndDelete_AreDisabledWithTheReasonAsTheirTooltip()
    {
        var vm = Source("ViewModels", "SpecimensViewModel.cs");
        string Args(string command)
        {
            var at = vm.IndexOf($"{command} = new RelayCommand(", StringComparison.Ordinal);
            Assert.True(at >= 0, command);
            return vm.Substring(at, vm.IndexOf(");", at, StringComparison.Ordinal) - at);
        }
        Assert.Contains("!(SelectedSpecimen.HasFinalCall || SelectedSpecimen.IsCancelled)", Args("EditCommand"));
        Assert.Contains("!SelectedSpecimen.HasFinalCall", Args("DeleteCommand"));
        var xaml = Source("Views", "SpecimensView.xaml");
        Assert.Contains("ToolTip=\"{Binding InvestigationLock}\"", xaml.Substring(xaml.IndexOf("Command=\"{Binding EditCommand}\"", StringComparison.Ordinal), 200));
        Assert.Contains("ToolTip=\"{Binding DeleteLock}\"", xaml.Substring(xaml.IndexOf("Command=\"{Binding DeleteCommand}\"", StringComparison.Ordinal), 200));
    }
}
