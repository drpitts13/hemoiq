using System.Text.RegularExpressions;

namespace HemoIQ.Tests;

/// <summary>
/// Guards against a defect class found in Iterations 107 and 114: comparing or keying antigen or
/// antibody names ignoring case, which treats K/k, C/c, E/e and S/s as the same antigen. The
/// test scans the application source for case-insensitive comparisons on lines that mention
/// antigens or antibodies. Every such line must be on the reviewed list below with a reason;
/// a new one fails the test. To compare antibody names, use RuleoutPolicy.SameAntibody; to key
/// or compare antigen names, use StringComparer.Ordinal.
/// </summary>
public class AntigenCaseGuardTests
{
    private static readonly Regex Insensitive = new(@"IgnoreCase", RegexOptions.Compiled);
    private static readonly Regex AntigenMention = new(@"antigen|antibod|""anti-|\bag\b|\bab\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    /// <summary>Sorting and the "anti-" prefix check never merge two antigens.</summary>
    private static readonly Regex AlwaysSafe = new(@"OrderBy|ThenBy|StartsWith\(""anti-""", RegexOptions.Compiled);

    /// <summary>(file, text on the line, reason). Reviewed in Iteration 115.</summary>
    private static readonly (string File, string Snippet, string Reason)[] Reviewed =
    {
        ("Models/DatProfile.cs", "DatResults.Any", "DAT result words, not antigen names"),
        ("Views/Dialogs/SpecimenDialog.xaml.cs", "DatResults[i]", "DAT result words, not antigen names"),
        ("Services/AntibodyAnalyzer.cs", "Contains(\"anti-D\"", "there is no antigen d, so anti-D cannot be confused"),
        ("Services/AntigenNegativeRequirement.cs", "Regex.Replace", "expands Fy(a-) style text; the antigen is resolved with case afterwards"),
        ("Services/AntigramRowFilter.cs", "AllCells", "the 'all cells' filter word"),
        ("Services/AntigramRowFilter.cs", "Zygosity", "zygosity filter words"),
        ("Services/AntigramRowFilter.cs", "SortAntigenFirst", "sort option word"),
        ("Services/FinalAntibodyParser.cs", "AntibodyKey(p) == null", "only for Final ID text that is not an antibody name"),
        ("Services/PatientTypingParser.cs", "FirstOrDefault(a => string.Equals(a, antigen", "fallback after an exact-case match failed (fya -> Fya); every case pair is matched exactly first"),
        ("Services/RareAntigenReader.cs", "string.Equals(a, antigen", "resolves against high-prevalence antigen names; none of them form a case pair"),
        ("Services/RuleValidation.cs", "var near", "builds the 'Did you mean' hint only"),
        ("Services/SelectedCellRecommender.cs", "string.Equals(name, antigen", "rare antigen names from SpecialTypes; none of them form a case pair"),
        ("Services/SelectedCellRecommender.cs", "\"Sda\"", "Sd(a) spelling check"),
        ("Services/StructuredPanelParser.cs", "LocalName.Equals", "XML element names"),
        ("Services/Vendors/VendorAntigramParser.cs", "ALBAcyte", "vendor product name"),
    };

    private static string SourceRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "HemoIQ");
    }

    private static IEnumerable<(string File, int Line, string Text)> Hits()
    {
        var root = SourceRoot();
        foreach (var path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(root, path).Replace('\\', '/');
            if (rel.StartsWith("obj/") || rel.StartsWith("bin/")) continue;
            var lines = File.ReadAllLines(path);
            for (var i = 0; i < lines.Length; i++)
            {
                var text = lines[i];
                if (!Insensitive.IsMatch(text) || !AntigenMention.IsMatch(text) || AlwaysSafe.IsMatch(text)) continue;
                yield return (rel, i + 1, text.Trim());
            }
        }
    }

    [Fact]
    public void NoNewCaseInsensitiveAntigenComparisons()
    {
        var unreviewed = Hits()
            .Where(h => !Reviewed.Any(r => r.File == h.File && h.Text.Contains(r.Snippet, StringComparison.Ordinal)))
            .Select(h => $"{h.File}:{h.Line}: {h.Text}")
            .ToList();
        Assert.True(unreviewed.Count == 0,
            "Case-insensitive comparison near antigen or antibody names. K/k, C/c, E/e and S/s are different antigens. " +
            "Use RuleoutPolicy.SameAntibody or StringComparer.Ordinal, or, if it is truly safe, add it to " +
            "AntigenCaseGuardTests.Reviewed with the reason:" + Environment.NewLine + string.Join(Environment.NewLine, unreviewed));
    }

    [Fact]
    public void ReviewedList_HasNoStaleEntries()
    {
        var hits = Hits().ToList();
        var stale = Reviewed
            .Where(r => !hits.Any(h => h.File == r.File && h.Text.Contains(r.Snippet, StringComparison.Ordinal)))
            .Select(r => $"{r.File}: {r.Snippet}")
            .ToList();
        Assert.True(stale.Count == 0, "Remove reviewed entries that no longer match any line:" + Environment.NewLine + string.Join(Environment.NewLine, stale));
    }

    [Fact]
    public void Guard_FlagsTheDefectItWasWrittenFor()
    {
        const string sample = "if (string.Equals(rule.Antibody, antibody, StringComparison.OrdinalIgnoreCase))";
        Assert.True(Insensitive.IsMatch(sample) && AntigenMention.IsMatch(sample) && !AlwaysSafe.IsMatch(sample));
        Assert.Matches(AlwaysSafe, ".OrderBy(a => a, StringComparer.OrdinalIgnoreCase)");
    }
}
