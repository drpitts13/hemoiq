using HemoIQ.Services;
using Microsoft.Data.Sqlite;

namespace HemoIQ.Tests;

/// <summary>
/// The log is a plain-text file outside the database's access controls. HemoIQ's own exception messages name records
/// (accessions, MRNs, lots), so they must not reach it; framework storage and file errors keep their text for diagnosis.
/// </summary>
public class AppLogPrivacyTests
{
    [Fact]
    public void ApplicationException_MessageIsWithheld_TypeAndSiteKept()
    {
        Exception thrown;
        try { throw new ArgumentException("Eluate ACC-77 and source ACC-12 belong to different patients (MRN MRN-555 and MRN-999)."); }
        catch (Exception ex) { thrown = ex; }

        var text = AppLog.Describe(thrown);

        Assert.StartsWith("ArgumentException", text);
        Assert.Contains("message withheld", text);
        Assert.DoesNotContain("ACC-77", text);
        Assert.DoesNotContain("MRN-555", text);
        Assert.Contains($"at {nameof(AppLogPrivacyTests)}.", text);
    }

    [Fact]
    public void WrappedApplicationException_IsWithheldToo()
    {
        var wrapped = new InvalidOperationException("outer for ACC-1", new InvalidOperationException("Specimen ACC-2 not found."));
        var text = AppLog.Describe(wrapped);
        Assert.Contains("(inner InvalidOperationException)", text);
        Assert.DoesNotContain("ACC-1", text);
        Assert.DoesNotContain("ACC-2", text);
    }

    [Fact]
    public void FrameworkStorageAndFileErrors_KeepTheirMessage()
    {
        Assert.Contains("attempt to write a readonly database",
            AppLog.Describe(new SqliteException("attempt to write a readonly database", 8)));
        Assert.Contains("disk is full", AppLog.Describe(new IOException("disk is full")));
    }

    [Fact]
    public void ErrorLine_InTheLogFile_HasNoRecordIdentifiers()
    {
        AppLog.Error("privacy-check", new ArgumentException("Specimen PRIV-ACC-314 not found."));
        var today = Path.Combine(AppLog.LogDirectory, $"app-{DateTime.Now:yyyy-MM-dd}.log");
        var line = ReadShared(today).Last(l => l.Contains("privacy-check"));
        Assert.DoesNotContain("PRIV-ACC-314", line);
        Assert.Contains("ArgumentException", line);
    }

    [Fact]
    public void ConcurrentWrites_AreNotLost()
    {
        var tag = Guid.NewGuid().ToString("N");
        Parallel.For(0, 40, i => AppLog.Info($"concurrency-{tag}-{i}"));
        var today = Path.Combine(AppLog.LogDirectory, $"app-{DateTime.Now:yyyy-MM-dd}.log");
        var lines = ReadShared(today).Where(l => l.Contains($"concurrency-{tag}-")).ToList();
        Assert.Equal(40, lines.Count);
    }

    [Fact]
    public void AReaderHoldingTheLogOpen_DoesNotBlockWriting()
    {
        var today = Path.Combine(AppLog.LogDirectory, $"app-{DateTime.Now:yyyy-MM-dd}.log");
        AppLog.Info("ensure-file");
        var tag = Guid.NewGuid().ToString("N");
        using (new FileStream(today, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            AppLog.Info($"while-open-{tag}");
        Assert.Contains(ReadShared(today), l => l.Contains($"while-open-{tag}"));
    }

    /// <summary>Reads the log the way a viewer would, sharing it with the writer.</summary>
    private static string[] ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Split(Environment.NewLine);
    }

    [Fact]
    public void LoggedMessages_InTheApp_DoNotInterpolateRecordText()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var app = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "App.xaml.cs"));
        Assert.DoesNotContain("AppLog.Info(\"--seed-clinical refused: \" + refusal", app);
    }
}
