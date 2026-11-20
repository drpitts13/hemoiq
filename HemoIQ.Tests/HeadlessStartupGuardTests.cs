namespace HemoIQ.Tests;

/// <summary>
/// The command-line runs (--write-manual, --seed-clinical, --import-vendor-samples) are used by the shortcut and
/// installer builds with nobody at the screen. If the main window opened for them, shutting down would raise the
/// quit confirmation and the build would wait forever. These checks keep the window off those paths.
/// </summary>
public class HeadlessStartupGuardTests
{
    private static string SourceRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "HemoIQ");
    }

    [Fact]
    public void AppXaml_DoesNotOpenTheMainWindowThroughStartupUri()
    {
        var xaml = File.ReadAllText(Path.Combine(SourceRoot(), "App.xaml"));
        Assert.DoesNotContain("StartupUri", xaml);
    }

    [Fact]
    public void MainWindow_IsOpenedOnlyAfterEveryHeadlessRunHasReturned()
    {
        var code = File.ReadAllText(Path.Combine(SourceRoot(), "App.xaml.cs"));
        var open = code.IndexOf("new MainWindow()", StringComparison.Ordinal);
        Assert.True(open > 0, "App.OnStartup must open the main window itself.");
        Assert.Equal(open, code.LastIndexOf("new MainWindow()", StringComparison.Ordinal));
        foreach (var flag in new[] { "--write-manual", "--seed-clinical", "--import-vendor-samples" })
        {
            var at = code.IndexOf(flag, StringComparison.Ordinal);
            Assert.True(at > 0, $"{flag} handler not found");
            var shutdown = code.IndexOf("Shutdown(", at, StringComparison.Ordinal);
            var ret = code.IndexOf("return;", at, StringComparison.Ordinal);
            Assert.True(shutdown > at && shutdown < open, $"{flag} must shut down before the main window opens.");
            Assert.True(ret > shutdown && ret < open, $"{flag} must return before the main window opens.");
        }
    }
}
