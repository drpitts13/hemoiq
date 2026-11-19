namespace AntibodyPanels.Models
{
    public enum AnalysisStaleReason
    {
        None,
        Reactions,
        Preferences,
        ReactionsAndPreferences
    }

    public static class AnalysisFreshness
    {
        public static AnalysisStaleReason Combine(bool reactionsChanged, bool preferencesChanged) =>
            reactionsChanged && preferencesChanged ? AnalysisStaleReason.ReactionsAndPreferences
            : reactionsChanged ? AnalysisStaleReason.Reactions
            : preferencesChanged ? AnalysisStaleReason.Preferences
            : AnalysisStaleReason.None;

        public static string WorklistDetail(AnalysisStaleReason reason) => reason switch
        {
            AnalysisStaleReason.ReactionsAndPreferences =>
                "Reactions and Preferences changed since last analysis.",
            AnalysisStaleReason.Preferences =>
                "Preferences changed since last analysis.",
            _ => "Reactions changed since last analysis."
        };

        public static string Banner(AnalysisStaleReason reason) => reason switch
        {
            AnalysisStaleReason.Reactions =>
                "⚠ Analysis is stale — reactions have been updated since last analysis.",
            AnalysisStaleReason.Preferences =>
                "⚠ Analysis is stale — Preferences changed since the last saved analysis.",
            AnalysisStaleReason.ReactionsAndPreferences =>
                "⚠ Analysis is stale — reactions and Preferences changed since the last saved analysis.",
            _ => ""
        };
    }
}
