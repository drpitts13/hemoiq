namespace HemoIQ.Tests;

/// <summary>
/// Clear identification, Supervisor review and Reinstate check the role before their reason or initials dialog opens,
/// as Purge, Preferences, Users &amp; roles and the imports do (Iterations 278-281), so a Technologist is not asked to
/// type a reason only to be refused.
/// </summary>
public class RoleGateTests
{
    private static string Source(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        return File.ReadAllText(Path.Combine(new[] { dir!.FullName, "HemoIQ" }.Concat(parts).ToArray()));
    }

    [Theory]
    [InlineData("AnalysisViewModel.cs", "private void ClearConfirmation()", "Permission.ClearIdentification")]
    [InlineData("SpecimensViewModel.cs", "private void SupervisorReview()", "Permission.SupervisorReview")]
    [InlineData("SpecimensViewModel.cs", "private void ReinstateInvestigation()", "Permission.ReinstateInvestigation")]
    public void TheRoleIsChecked_BeforeTheDialogOpens(string file, string method, string permission)
    {
        var src = Source("ViewModels", file);
        var at = src.IndexOf(method, StringComparison.Ordinal);
        Assert.True(at >= 0, method);
        var body = src[at..];
        var gate = body.IndexOf($"RoleGate.AllowedOrExplained(_db, {permission}", StringComparison.Ordinal);
        var dialog = body.IndexOf("ReasonDialog(", StringComparison.Ordinal);
        Assert.True(gate >= 0, $"{method} does not check {permission}");
        Assert.True(gate < dialog, $"{method} opens its dialog before checking the role");
    }
}
