using System.Text.RegularExpressions;

namespace HemoIQ.Tests;

/// <summary>
/// Guards "never silently overwrite clinically important information": every DatabaseService
/// method that runs UPDATE, DELETE or INSERT must call AppendAudit, or be on the reviewed list
/// below with the reason it needs no entry of its own (derived analysis output, timestamps,
/// helpers whose callers audit, restore and migration plumbing). A new unaudited writer fails.
/// </summary>
public class AuditCoverageGuardTests
{
    private static readonly (string Method, string Reason)[] Reviewed =
    {
        ("TouchSpecimenReactionsUpdated", "timestamp only; the grade save that calls it is audited"),
        ("SetSpecimenLastAnalyzed", "timestamp only; the analysis snapshot is the record"),
        ("AddSpecimenAntibody", "derived analysis output, rebuilt on each analysis"),
        ("ClearSpecimenAntibodies", "derived analysis output, rebuilt on each analysis"),
        ("AddSpecimenRuleout", "derived analysis output, rebuilt on each analysis"),
        ("ClearSpecimenRuleouts", "derived analysis output, rebuilt on each analysis"),
        ("SaveAnalysisSnapshot", "the snapshot is itself the stored record of the analysis"),
        ("SetPanelVendorMetadata", "called only while importing a panel; the import is audited"),
        ("AddPanelCell", "called only by AddPanel and panel replacement, which are audited"),
        ("UpdatePanelCellAntigen", "called by audited panel edits; direct calls are seed and test data only"),
        ("UpdatePanelCellMetadata", "called by audited panel edits"),
        ("DeletePanelCells", "called by audited panel replacement"),
        ("ReplacePanelCellsCore", "core of an audited replacement"),
        ("CopyPanelCells", "used when copying a panel; the new panel's add is audited"),
        ("SetPanelAntigenOrderCore", "core of the audited antigen-order change"),
        ("CopyPanelAntigenOrder", "used when copying a panel; the new panel's add is audited"),
        ("UpsertExtraCellAntigen", "called by audited extra-antigen changes"),
        ("FillRestoreSkipTable", "temporary table used during restore"),
        ("BumpSequence", "SQLite sequence plumbing during restore"),
    };

    private static string DatabaseSource()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "Data", "DatabaseService.cs"));
    }

    private static List<string> UnauditedWriters()
    {
        var src = DatabaseSource();
        var methods = Regex.Split(src, @"\n(?=        (?:public|private|internal)[^\n]*\()");
        var result = new List<string>();
        foreach (var body in methods)
        {
            var m = Regex.Match(body, @"^\s*(?:public|private|internal)[^\n(]*?(\w+)\s*\(");
            if (!m.Success) continue;
            var name = m.Groups[1].Value;
            if (name.StartsWith("Migrate") || name.Contains("Archive")) continue;
            if (!Regex.IsMatch(body, @"\b(UPDATE \w+\s+SET|DELETE FROM \w+|INSERT (?:OR \w+ )?INTO \w+)")) continue;
            if (body.Contains("AppendAudit(")) continue;
            result.Add(name);
        }
        return result;
    }

    [Fact]
    public void EveryDatabaseWriter_IsAudited_OrReviewed()
    {
        var unreviewed = UnauditedWriters().Where(n => Reviewed.All(r => r.Method != n)).ToList();
        Assert.True(unreviewed.Count == 0,
            "DatabaseService methods that change data without AppendAudit. Audit the change, or if it is truly derived or " +
            "plumbing, add it to AuditCoverageGuardTests.Reviewed with the reason: " + string.Join(", ", unreviewed));
    }

    [Fact]
    public void ReviewedList_HasNoStaleEntries()
    {
        var current = UnauditedWriters();
        var stale = Reviewed.Where(r => !current.Contains(r.Method)).Select(r => r.Method).ToList();
        Assert.True(stale.Count == 0, "Remove reviewed entries that are now audited or gone: " + string.Join(", ", stale));
    }
}
