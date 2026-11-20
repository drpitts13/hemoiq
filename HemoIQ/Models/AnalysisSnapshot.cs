namespace HemoIQ.Models
{
    public class AnalysisSnapshot
    {
        public long Id { get; set; }
        public string SpecimenId { get; set; } = "";
        public string AnalyzedAtUtc { get; set; } = "";
        public string SoftwareVersion { get; set; } = "";
        public string SettingsJson { get; set; } = "";
        public string InputFingerprint { get; set; } = "";
        public string? RuledOutJson { get; set; }
        public string? SuspectedJson { get; set; }
        public string? AcsJson { get; set; }
        public string? RuleEngineVersion { get; set; }
        public string? RulesJson { get; set; }
        public string? AnalyzedBy { get; set; }
        /// <summary>
        /// Serialized adsorption comparison and ordinary-score exclusions.
        /// </summary>
        public string? AbsorptionJson { get; set; }
        /// <summary>JSON list of testing needs the analysis left open; null for analyses saved before this field.</summary>
        public string? AdditionalTestingJson { get; set; }

        public List<string> AdditionalTesting =>
            string.IsNullOrEmpty(AdditionalTestingJson)
                ? new List<string>()
                : System.Text.Json.JsonSerializer.Deserialize<List<string>>(AdditionalTestingJson) ?? new List<string>();
    }

    /// <summary>
    /// Audit copy of the adsorption comparison stored on an analysis snapshot.
    /// </summary>
    public class AdsorptionAudit
    {
        public List<AbsorptionConclusion> Conclusions { get; set; } = new();
        public List<string> OrdinaryScoreExclusions { get; set; } = new();
    }
}
