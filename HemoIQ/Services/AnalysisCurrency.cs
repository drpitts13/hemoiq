using System;
using System.Collections.Generic;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    /// <summary>
    /// The latest rule or Preferences change. <see cref="Seq"/> is its audit id (-1 when unknown);
    /// <see cref="Local"/> is its local time for display and for analyses saved without an audit id.
    /// </summary>
    public sealed record AnalysisConfigurationChange(long Seq, string Local)
    {
        /// <summary>A bare local time, as older callers and tests pass it: compared by time only.</summary>
        public static implicit operator AnalysisConfigurationChange?(string? local) =>
            local == null ? null : new AnalysisConfigurationChange(-1, local);
    }

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
        public static string? StaleReason(Specimen s, AnalysisConfigurationChange? configChangedLocal, bool settingsDrifted = false)
        {
            if (s.LastAnalyzedAt == null) return null;
            var reasons = new List<string>();
            if (s.IsAnalysisStale)
                reasons.Add("Reactions or panel typing changed since last analysis.");
            if (!s.HasFinalCall && ConfigChangedAfter(s, configChangedLocal))
                reasons.Add($"Analysis rules or Preferences changed {configChangedLocal!.Local.Replace('T', ' ')}, after the last analysis.");
            else if (!s.HasFinalCall && settingsDrifted)
                reasons.Add("Preferences changed since last analysis.");
            return reasons.Count == 0 ? null : string.Join(" ", reasons);
        }

        /// <summary>
        /// True when the rule or Preferences change came after the specimen's last analysis. Ordered by audit
        /// id when the analysis recorded one, so the repeated hour at the end of daylight saving time or a
        /// clock correction cannot hide a change; analyses saved without an id compare local times.
        /// </summary>
        public static bool ConfigChangedAfter(Specimen s, AnalysisConfigurationChange? change)
        {
            if (change == null || s.LastAnalyzedAt == null) return false;
            if (change.Seq >= 0 && s.AnalysisConfigSeq is long seq) return change.Seq > seq;
            return string.Compare(change.Local, s.LastAnalyzedAt, StringComparison.Ordinal) > 0;
        }
    }
}
