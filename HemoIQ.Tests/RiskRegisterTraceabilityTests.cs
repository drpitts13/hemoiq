using System.Text.RegularExpressions;

namespace HemoIQ.Tests;

/// <summary>
/// The FDA risk register and traceability matrix cite tests as verification of each control. A cited test that was
/// renamed or removed leaves a control with no real verification, so every cited test class (and method) must exist.
/// </summary>
public class RiskRegisterTraceabilityTests
{
    private static DirectoryInfo Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        return dir!;
    }

    [Theory]
    [InlineData("risk-management.md")]
    [InlineData("traceability.md")]
    public void EveryCitedTest_Exists(string document)
    {
        var root = Root();
        var doc = File.ReadAllText(Path.Combine(root.FullName, "docs", "fda-510k", document));
        var tests = string.Join("\n", Directory.EnumerateFiles(Path.Combine(root.FullName, "HemoIQ.Tests"), "*.cs")
            .Select(File.ReadAllText));

        var missing = new List<string>();
        foreach (Match m in Regex.Matches(doc, @"`(?<cls>[A-Z][A-Za-z0-9]*Tests)(?:\.(?<method>[A-Za-z0-9_]+))?`"))
        {
            var cls = m.Groups["cls"].Value;
            if (!Regex.IsMatch(tests, $@"\bclass {cls}\b"))
            {
                missing.Add(cls);
                continue;
            }
            var method = m.Groups["method"].Value;
            if (method.Length > 0 && !Regex.IsMatch(tests, $@"\bvoid {method}\(")) missing.Add($"{cls}.{method}");
        }
        Assert.True(missing.Count == 0, $"{document} cites tests that do not exist: " + string.Join(", ", missing.Distinct()));
    }

    [Fact]
    public void EveryRequirement_HasATraceabilityRow()
    {
        var dir = Path.Combine(Root().FullName, "docs", "fda-510k");
        var srs = File.ReadAllText(Path.Combine(dir, "srs.md"));
        var trace = File.ReadAllText(Path.Combine(dir, "traceability.md"));
        var ids = Regex.Matches(srs, @"^\| (SRS-[A-Z0-9]+-\d+) \|", RegexOptions.Multiline).Select(m => m.Groups[1].Value).ToList();
        Assert.NotEmpty(ids);
        // A traceability row may cover several requirements ("SRS-AUD-01 / 02").
        var untraced = ids.Where(id =>
        {
            var prefix = id[..id.LastIndexOf('-')];
            var number = id[(id.LastIndexOf('-') + 1)..];
            return !Regex.IsMatch(trace, $@"^\| {Regex.Escape(prefix)}-[0-9 /]*\b{number}\b[^|]*\|", RegexOptions.Multiline);
        }).ToList();
        Assert.True(untraced.Count == 0, "Requirements without a traceability row: " + string.Join(", ", untraced));
    }

    [Fact]
    public void RiskRegister_CoversTheRecentSafetyControls()
    {
        var doc = File.ReadAllText(Path.Combine(Root().FullName, "docs", "fda-510k", "risk-management.md"));
        foreach (var cited in new[] { "ExpiredSpecimenEntryTests", "EluateEvidenceTests", "PanelEditAuditTests",
                     "RunTreatmentTextTests", "DestructiveConfirmTests", "StartupFailureTests", "DemoDataGuardTests" })
            Assert.Contains($"`{cited}`", doc);
    }
}
