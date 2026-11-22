using HemoIQ.Services.Vendors;

namespace HemoIQ.Tests;

/// <summary>
/// The vendor dialog runs one download or import at a time, and a download that finishes after the dialog was
/// closed is discarded rather than stored as a lot the user walked away from.
/// </summary>
public class VendorDialogGateTests
{
    [Fact]
    public void SecondStart_IsRefusedWhileTheFirstRuns_AndAllowedAfter()
    {
        using var gate = new VendorDialogGate();
        Assert.True(gate.TryBegin());
        Assert.True(gate.IsBusy);
        Assert.False(gate.TryBegin());
        gate.End();
        Assert.True(gate.TryBegin());
    }

    [Fact]
    public void Closing_CancelsTheDownload_AndForbidsStoringItsResult()
    {
        using var gate = new VendorDialogGate();
        Assert.True(gate.TryBegin());
        Assert.True(gate.MayApply);
        gate.Close();
        Assert.True(gate.Token.IsCancellationRequested);
        Assert.False(gate.MayApply);
        Assert.False(gate.TryBegin());
    }

    [Fact]
    public async Task DownloadCancelledByClosing_IsNotApplied()
    {
        using var gate = new VendorDialogGate();
        gate.TryBegin();
        var download = Task.Delay(Timeout.Infinite, gate.Token);
        gate.Close();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => download);
        Assert.False(gate.MayApply);
    }

    [Fact]
    public void Dialog_PassesTheTokenAndChecksTheGateBeforeStoring()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "Views", "Dialogs", "DownloadFromVendorDialog.xaml.cs"));

        Assert.Contains("DownloadAndParseAsync(lot, _gate.Token)", src);
        Assert.Contains("ListLotsAsync(_gate.Token)", src);
        var download = src.IndexOf("DownloadAndParseAsync(lot, _gate.Token)", StringComparison.Ordinal);
        var persist = src.IndexOf("PersistParsed(parsed);", download, StringComparison.Ordinal);
        Assert.Contains("if (!_gate.MayApply) return;", src[download..persist]);
        Assert.Contains("Closing += (_, _) => _gate.Close();", src);
    }
}
