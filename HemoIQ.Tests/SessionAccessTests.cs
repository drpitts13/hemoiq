using System.Text.RegularExpressions;
using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]
public class SessionAccessTests
{
    private static T As<T>(string login, Func<T> body)
    {
        var previous = AccessControl.CurrentLogin;
        try { AccessControl.CurrentLogin = login; return body(); }
        finally { AccessControl.CurrentLogin = previous; }
    }

    private static IsolatedDatabase WithRegistry()
    {
        var iso = new IsolatedDatabase();
        As("adm", () =>
        {
            iso.Db.SaveUser(new AppUser { Login = "adm", Initials = "AD", Name = "Ada Admin", Role = UserRole.Administrator });
            iso.Db.SaveUser(new AppUser { Login = "tech", Initials = "TE", Name = "Tom Tech", Role = UserRole.Technologist });
            iso.Db.SaveUser(new AppUser { Login = "view", Initials = "VW", Name = "Vic View", Role = UserRole.ReadOnly });
            return 0;
        });
        return iso;
    }

    [Fact]
    public void EmptyRegistry_AllowsChanges_AndSaysSo()
    {
        using var iso = new IsolatedDatabase();
        var s = SessionAccess.Describe(iso.Db);
        Assert.True(s.CanRecord);
        Assert.False(s.ViewOnly);
        Assert.Equal("Users not registered — all actions allowed", s.Text);
    }

    [Fact]
    public void RegisteredTechnologist_IsNamed_AndMayRecord()
    {
        using var iso = WithRegistry();
        var s = As("TECH", () => SessionAccess.Describe(iso.Db));
        Assert.Equal("Signed in: Tom Tech (TE) (Technologist)", s.Text);
        Assert.True(s.CanRecord);
        Assert.Contains("Changes are recorded under this login", s.Detail);
    }

    [Fact]
    public void ReadOnlyUser_AndUnregisteredLogin_AreViewOnly()
    {
        using var iso = WithRegistry();
        var view = As("view", () => SessionAccess.Describe(iso.Db));
        Assert.Equal("Signed in: Vic View (VW) (Read-only) — view only", view.Text);
        Assert.True(view.ViewOnly);
        Assert.False(view.CanRecord);

        var stranger = As("stranger", () => SessionAccess.Describe(iso.Db));
        Assert.Equal("Login 'stranger' not registered — view only", stranger.Text);
        Assert.False(stranger.CanRecord);
        Assert.Contains("Ask an Administrator", stranger.Detail);
    }

    [Fact]
    public void Refresh_UpdatesTheCachedState_UsedByScreens()
    {
        using var iso = WithRegistry();
        try
        {
            As("view", () => SessionAccess.Refresh(iso.Db));
            Assert.False(SessionAccess.CanRecord);
            As("tech", () => SessionAccess.Refresh(iso.Db));
            Assert.True(SessionAccess.CanRecord);
        }
        finally
        {
            using var empty = new IsolatedDatabase();
            SessionAccess.Refresh(empty.Db);
        }
        Assert.True(SessionAccess.CanRecord);
    }

    /// <summary>The commands that change laboratory data are disabled while the session is view-only.</summary>
    [Theory]
    [InlineData("SpecimensViewModel", "AddCommand,CancelInvestigationCommand,EditPatientCommand,EditCommand,DeleteCommand,AttachPanelCommand,DetachPanelCommand")]
    [InlineData("PanelsViewModel", "AddCommand,EditCommand,EditDetailsCommand,DeleteCommand,CopyCommand,ImportCsvCommand,DownloadVendorCommand,AddExtraAntigenCommand,RemoveExtraAntigenCommand,ActivateCommand")]
    [InlineData("ReactionsViewModel", "SaveCommand,SaveAnalyzeCommand,ClearCommand,FillNegativesCommand,AddRunCommand,DeleteRunCommand")]
    [InlineData("AnalysisViewModel", "AnalyzeCommand,ConfirmIdCommand,ClearConfirmationCommand")]
    [InlineData("RulesViewModel", "AddCommand,EditCommand,DeleteCommand,ImportCommand")]
    public void ChangeCommands_AreDisabledWhenViewOnly(string viewModel, string commands)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "ViewModels", viewModel + ".cs"));
        foreach (var command in commands.Split(','))
        {
            var m = Regex.Match(src, @"\b" + command + @" = new RelayCommand\((?<args>.*?)\);", RegexOptions.Singleline);
            Assert.True(m.Success, $"{viewModel}.{command} not found");
            Assert.Contains("SessionAccess.CanRecord", m.Groups["args"].Value);
        }
    }

    private static string Source(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        return File.ReadAllText(Path.Combine(new[] { dir!.FullName, "HemoIQ" }.Concat(parts).ToArray()));
    }

    [Fact]
    public void ReactionGrid_IsReadOnly_KeysAndPasteIgnored_AndNoDraft_WhenViewOnly()
    {
        Assert.Contains("IsReadOnly=\"{Binding GradesReadOnly}\"", Source("Views", "ReactionsView.xaml"));
        var keys = Source("Views", "ReactionsView.xaml.cs");
        var handler = keys.Substring(keys.IndexOf("private void ReactionsGrid_PreviewKeyDown", StringComparison.Ordinal));
        Assert.StartsWith("private void ReactionsGrid_PreviewKeyDown(object sender, KeyEventArgs e)", handler);
        Assert.Contains("if (_vm?.GradesReadOnly == true) return;", handler.Substring(0, 200));
        var vm = Source("ViewModels", "ReactionsViewModel.cs");
        Assert.Contains("public bool GradesReadOnly => !SessionAccess.CanRecord || LockMessage != null;", vm); // also a locked investigation (Iteration 283)
        Assert.Contains("|| GradesReadOnly) return;", vm.Substring(vm.IndexOf("private void WriteDraft()", StringComparison.Ordinal), 200));
        Assert.Contains("ReactionsVM.NotifySessionChanged();", Source("ViewModels", "MainViewModel.cs"));
    }

    [Fact]
    public void ViewOnlyNote_PrefixesTheEntryProgressWording()
    {
        Assert.Equal("View only: grades are shown but cannot be changed in this session. ", HemoIQ.ViewModels.ReactionsViewModel.ViewOnlyNote);
        Assert.Equal("Grades entered: 3 of 11 cells.", HemoIQ.ViewModels.ReactionsViewModel.FormatEntryProgress(3, 11));
    }
}
