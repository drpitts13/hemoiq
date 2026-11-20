using System;
using System.Linq;
using System.Text.Json;

namespace HemoIQ.Models
{
    public class LabSettings
    {
        public string LabName { get; set; } = "Immunohematology Laboratory";
        public string Department { get; set; } = "";

        // Report letterhead. Each is optional; a blank value leaves its part of the letterhead out.
        public const int FacilityNameMaxLength = 120;
        public const int FacilityAddressMaxLength = 400;
        public const int FacilityContactMaxLength = 200;
        /// <summary>Facility name printed at the top of every report. Blank: not printed.</summary>
        public string FacilityName { get; set; } = "";
        /// <summary>Facility address, one line per address line. Blank: not printed.</summary>
        public string FacilityAddress { get; set; } = "";
        /// <summary>Contact information (phone, fax, email), one line per entry. Blank: not printed.</summary>
        public string FacilityContact { get; set; } = "";

        /// <summary>
        /// The report letterhead: facility name, address lines and contact lines, each trimmed, with blank lines
        /// left out. Empty when none of the three is set.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<string> FacilityHeaderLines()
        {
            var lines = new System.Collections.Generic.List<string>();
            foreach (var text in new[] { FacilityName, FacilityAddress, FacilityContact })
                lines.AddRange(SplitLines(text));
            return lines;
        }

        /// <summary>Trims each line and drops blank ones; joins them with newlines (empty when nothing remains).</summary>
        public static string NormalizeMultiline(string? text) => string.Join("\n", SplitLines(text));

        private static System.Collections.Generic.IEnumerable<string> SplitLines(string? text) =>
            (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length > 0);
        public double ProbabilityThreshold { get; set; } = 0.5;
        public int IdentificationCellCount { get; set; } = 3;
        public int AcsRuleoutCount { get; set; } = 3;
        /// <summary>
        /// Antigens added to the fixed ACS core (<see cref="AntigenConstants.AcsCoreAntigens"/>): an antibody
        /// to each must also be ruled out before All Clinically Significant Antibodies Ruled Out is suggested.
        /// </summary>
        public string AcsAdditionalAntigens { get; set; } = AntigenConstants.AcsDefaultAdditionalAntigens;

        /// <summary>Every antigen the ACS result covers under these settings.</summary>
        public System.Collections.Generic.IReadOnlyList<string> AcsAntigenList() =>
            AntigenConstants.AcsAntigens(AcsAdditionalAntigens);
        /// <summary>
        /// Suggested multiple-antibody criterion: reactive cells each called antibody needs that are
        /// negative for every other called antigen. 0 turns the check off.
        /// </summary>
        public int SeparationCellCount { get; set; } = 1;
        /// <summary>
        /// Default number of qualifying antigen-positive nonreactive cells
        /// required to rule out an antibody when no per-antibody rule exists.
        /// 1 preserves historical behavior.
        /// </summary>
        public int DefaultMinRuleoutCount { get; set; } = 1;
        public string DefaultSpecimenType { get; set; } = "serum";
        public bool ShowInactiveByDefault { get; set; }
        public bool HideRuledOutAntigenColumns { get; set; }
        /// <summary>
        /// When true, the reaction antigram groups columns by blood-group system
        /// and marks homozygous antigen-positive cells as ++. Display only.
        /// </summary>
        public bool ShowAntigramDosage { get; set; }
        public string DefaultIdentifiedBy { get; set; } = "";
        public int ExpirationWarningDays { get; set; } = 14;
        public int DefaultSpecimenDatingDays { get; set; } = 3;
        /// <summary>
        /// A transfusion this many days or fewer before the specimen makes
        /// serologic patient typing uninterpretable (possible donor cells).
        /// </summary>
        public int RecentTransfusionWindowDays { get; set; } = 90;
        /// <summary>
        /// When true, a confirmed identification counts as Completed only
        /// after a second person records a supervisor review.
        /// </summary>
        public bool RequireSupervisorReview { get; set; }
        /// <summary>Open investigations older than this many days are listed as overdue on Analytics.</summary>
        public int TurnaroundThresholdDays { get; set; } = 3;
        /// <summary>
        /// A transfusion this many days or fewer before collection limits specimen dating
        /// to collection + <see cref="DefaultSpecimenDatingDays"/>.
        /// </summary>
        public int DatingTransfusionLookbackDays { get; set; } = 90;
        /// <summary>Anti-D within this many days of RhIG is flagged as possibly passive.</summary>
        public int RhigPassiveWindowDays { get; set; } = 84;
        public int MaxDatabaseSizeMb { get; set; } = 500;
        public bool WorklistShowIncomplete { get; set; } = true;
        public bool WorklistShowStale { get; set; } = true;
        public bool WorklistShowExpiring { get; set; } = true;
        public bool WorklistShowExpired { get; set; } = true;
        /// <summary>Review items: overdue testing, supervisor review, rule problems, reagent and typing checks.</summary>
        public bool WorklistShowReview { get; set; } = true;
        public bool IntendedUseAcknowledged { get; set; }
        /// <summary>
        /// Comma-separated extra reaction phases (e.g. RT, PEG, Gel, Solid,
        /// Dil1–Dil128). Empty keeps the classic IS / 37°C / AHG / CC grid.
        /// Gel, Solid, and PEG are IAT-like evidence. Dilution columns are
        /// titer endpoints only, not a diagnosis.
        /// </summary>
        public string ExtraPhases { get; set; } = "";

        public string IdentificationRuleLabel =>
            $"{IdentificationCellCount} + {IdentificationCellCount}";

        public static LabSettings CreateDefault() => new();

        public void Clamp()
        {
            if (ProbabilityThreshold < 0.3) ProbabilityThreshold = 0.3;
            if (ProbabilityThreshold > 0.95) ProbabilityThreshold = 0.95;
            if (IdentificationCellCount < 1 || IdentificationCellCount > 3)
                IdentificationCellCount = 3;
            if (AcsRuleoutCount < 1 || AcsRuleoutCount > 5)
                AcsRuleoutCount = 3;
            if (SeparationCellCount < 0 || SeparationCellCount > 5)
                SeparationCellCount = 1;
            if (DefaultMinRuleoutCount < 1 || DefaultMinRuleoutCount > 5)
                DefaultMinRuleoutCount = 1;
            if (ExpirationWarningDays < 1) ExpirationWarningDays = 1;
            if (ExpirationWarningDays > 90) ExpirationWarningDays = 90;
            if (DefaultSpecimenDatingDays < 0) DefaultSpecimenDatingDays = 0;
            if (DefaultSpecimenDatingDays > 14) DefaultSpecimenDatingDays = 14;
            if (RhigPassiveWindowDays < 1) RhigPassiveWindowDays = 1;
            if (RhigPassiveWindowDays > 365) RhigPassiveWindowDays = 365;
            if (DatingTransfusionLookbackDays < 1) DatingTransfusionLookbackDays = 1;
            if (DatingTransfusionLookbackDays > 365) DatingTransfusionLookbackDays = 365;
            if (TurnaroundThresholdDays < 1) TurnaroundThresholdDays = 1;
            if (TurnaroundThresholdDays > 60) TurnaroundThresholdDays = 60;
            if (RecentTransfusionWindowDays < 1) RecentTransfusionWindowDays = 1;
            if (RecentTransfusionWindowDays > 365) RecentTransfusionWindowDays = 365;
            if (MaxDatabaseSizeMb < 50) MaxDatabaseSizeMb = 50;
            if (MaxDatabaseSizeMb > 10240) MaxDatabaseSizeMb = 10240;
            if (string.IsNullOrWhiteSpace(LabName))
                LabName = "Immunohematology Laboratory";
            if (string.IsNullOrWhiteSpace(DefaultSpecimenType) ||
                Array.IndexOf(new[] { "serum", "plasma", "eluate" }, DefaultSpecimenType) < 0)
                DefaultSpecimenType = "serum";
            DefaultIdentifiedBy = NormalizeInitials(DefaultIdentifiedBy);
            ExtraPhases = ExtraPhaseParser.NormalizeList(ExtraPhases);
            AntigenConstants.TryParseAcsAdditions(AcsAdditionalAntigens, out var acsAdditions, out _);
            AcsAdditionalAntigens = acsAdditions;
        }

        /// <summary>
        /// Reads a decimal typed with either a decimal point or, as on comma-decimal regional
        /// settings, a single decimal comma ("0.70" or "0,70"). Thousands separators are refused.
        /// </summary>
        public static bool TryParseDecimal(string? text, out double value)
        {
            value = 0;
            var t = (text ?? string.Empty).Trim();
            if (t.Length == 0) return false;
            if (t.Contains(',') && !t.Contains('.') && t.Count(ch => ch == ',') == 1) t = t.Replace(',', '.');
            if (t.Contains(',')) return false;
            return double.TryParse(t, System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        public static string NormalizeInitials(string? initials)
        {
            var t = (initials ?? string.Empty).Trim();
            return t.Length <= 12 ? t : t[..12];
        }

        public static DateTime? DefaultExpirationDate(DateTime today, int datingDays)
        {
            if (datingDays <= 0) return null;
            return today.Date.AddDays(datingDays);
        }

        /// <summary>The Worklist count cards, in display order.</summary>
        public static readonly string[] WorklistCategories = { "Incomplete", "Stale", "Expiring", "Expired", "Review" };

        /// <summary>The count card a Worklist kind belongs to. Every kind belongs to exactly one.</summary>
        public static string WorklistCategory(WorklistKind kind) => kind switch
        {
            WorklistKind.IncompleteReactions => "Incomplete",
            WorklistKind.StaleAnalysis => "Stale",
            WorklistKind.ExpiringSpecimen or WorklistKind.ExpiringPanel => "Expiring",
            WorklistKind.ExpiredSpecimen or WorklistKind.ExpiredPanel => "Expired",
            _ => "Review"
        };

        public bool ShowsWorklistCategory(string category) => category switch
        {
            "Incomplete" => WorklistShowIncomplete,
            "Stale" => WorklistShowStale,
            "Expiring" => WorklistShowExpiring,
            "Expired" => WorklistShowExpired,
            "Review" => WorklistShowReview,
            _ => true
        };

        public void SetWorklistCategory(string category, bool show)
        {
            switch (category)
            {
                case "Incomplete": WorklistShowIncomplete = show; break;
                case "Stale": WorklistShowStale = show; break;
                case "Expiring": WorklistShowExpiring = show; break;
                case "Expired": WorklistShowExpired = show; break;
                case "Review": WorklistShowReview = show; break;
            }
        }

        public bool ShowsWorklistKind(WorklistKind kind) => ShowsWorklistCategory(WorklistCategory(kind));

        /// <summary>True when this category is the only one shown.</summary>
        public bool IsWorklistCategoryIsolated(string category) =>
            ShowsWorklistCategory(category) &&
            System.Linq.Enumerable.All(WorklistCategories, c => c == category || !ShowsWorklistCategory(c));

        public bool ShowsAllWorklistCategories => System.Linq.Enumerable.All(WorklistCategories, ShowsWorklistCategory);

        /// <summary>Shows only <paramref name="category"/>, or every category when it was already the only one.</summary>
        public void IsolateOrRestoreWorklistCategory(string category)
        {
            if (System.Array.IndexOf(WorklistCategories, category) < 0) return;
            var restore = IsWorklistCategoryIsolated(category);
            foreach (var c in WorklistCategories) SetWorklistCategory(c, restore || c == category);
        }

        public string ClinicalSettingsJson() => JsonSerializer.Serialize(ClinicalSnapshot());

        /// <summary>
        /// True when a saved analysis used a different score threshold, ID cell
        /// rule, ACS count, default rule-out count, or extra-phase list.
        /// Lab name and other display settings are ignored. A blank snapshot
        /// is not treated as a change.
        /// </summary>
        public static bool ClinicalSettingsDiffer(string? savedJson, string currentJson) =>
            !string.IsNullOrWhiteSpace(savedJson) &&
            !string.Equals(savedJson.Trim(), currentJson, StringComparison.Ordinal);

        public object ClinicalSnapshot() => new
        {
            ProbabilityThreshold,
            IdentificationCellCount,
            AcsRuleoutCount,
            AcsAdditionalAntigens,
            DefaultMinRuleoutCount,
            SeparationCellCount,
            ExtraPhases,
            RecentTransfusionWindowDays
        };

        public string IsolatedWorklistLabel =>
            IsWorklistCategoryIsolated("Incomplete") ? "Showing only incomplete work." :
            IsWorklistCategoryIsolated("Stale") ? "Showing only stale analyses." :
            IsWorklistCategoryIsolated("Expiring") ? "Showing only items nearing expiration." :
            IsWorklistCategoryIsolated("Expired") ? "Showing only expired specimens and panels." :
            IsWorklistCategoryIsolated("Review") ? "Showing only items to review." :
            "";
    }
}
