using System;
using System.Collections.Generic;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    /// <summary>
    /// The one rule for whether a specimen's last analysis is out of date, shared by the
    /// Analysis banner, Confirm ID, the investigation status strip, the Worklist and the
    /// Pending Work report. An analysis is stale when grades were saved after it, or, for an
    /// investigation without a confirmed identification, when analysis rules or clinical
    /// Preferences changed after it, or when this workstation's clinical Preferences differ from
    /// those saved with the analysis. A confirmed identification is not reopened by a later
    /// configuration change.
    /// </summary>
    public static class AnalysisCurrency
    {
        /// <param name="configChangedLocal">
        /// Latest rule or Preferences change, local "yyyy-MM-ddTHH:mm:ss", as returned by
        /// <c>DatabaseService.LastAnalysisConfigurationChange</c>; null when none.
        /// </param>
        /// <returns>Why the analysis is out of date, or null when it is current or never ran.</returns>
        /// <param name="settingsDrifted">
        /// True when the clinical Preferences saved with the analysis differ from the current ones
        /// (<c>DatabaseService.SettingsDrifted</c>).
        /// </param>
        public static string? StaleReason(Specimen s, string? configChangedLocal, bool settingsDrifted = false)
        {
            if (s.LastAnalyzedAt == null) return null;
            var reasons = new List<string>();
            if (s.IsAnalysisStale)
                reasons.Add("Reactions changed since last analysis.");
            if (!s.HasFinalCall && configChangedLocal != null &&
                string.Compare(configChangedLocal, s.LastAnalyzedAt, StringComparison.Ordinal) > 0)
                reasons.Add($"Analysis rules or Preferences changed {configChangedLocal.Replace('T', ' ')}, after the last analysis.");
            else if (!s.HasFinalCall && settingsDrifted)
                reasons.Add("Preferences changed since last analysis.");
            return reasons.Count == 0 ? null : string.Join(" ", reasons);
        }
    }
}
