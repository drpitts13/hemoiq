namespace HemoIQ.ViewModels
{
    /// <summary>
    /// Standing status for the screen the user is looking at. A line from
    /// another screen is not left in place.
    /// </summary>
    public static class ScreenStatus
    {
        public const string SearchIdle = "Search by antigen profile.";

        public static string Worklist(int total, int shown)
        {
            if (total == 0)
                return "Worklist is clear.";
            if (shown == 0)
                return "No worklist items match the selected filters.";
            if (shown == total)
                return $"Worklist: {shown} item(s) need attention.";
            return $"Worklist: {shown} of {total} item(s) shown.";
        }

        public static string Specimens(int shown, string? accession) =>
            shown == 0
                ? "No specimens to show."
                : string.IsNullOrEmpty(accession)
                    ? $"Specimens: {shown} shown."
                    : $"Specimens: {shown} shown. Selected {accession}.";

        public static string Panels(int shown, string? name, string? lot, string? cell = null)
        {
            if (shown == 0) return "No panels to show.";
            if (string.IsNullOrEmpty(name)) return $"Panels: {shown} shown.";
            var lotPart = string.IsNullOrEmpty(lot) ? "" : $" ({lot})";
            var cellPart = string.IsNullOrEmpty(cell) ? "" : $" cell {cell}";
            return $"Panels: {shown} shown. Selected {name}{lotPart}{cellPart}.";
        }

        public static string Reactions(string? accession, string? panel, string? runLabel, int cells)
        {
            if (string.IsNullOrEmpty(accession) || string.IsNullOrEmpty(panel))
                return "Select a specimen and panel to enter reactions.";
            var run = string.IsNullOrEmpty(runLabel) ? "" : $" ({runLabel})";
            return $"Reactions for {accession} / {panel}{run}: {cells} cells.";
        }

        public static string Analysis(string? accession, bool analyzed, bool stale, int suspected, int ruledOut)
        {
            if (string.IsNullOrEmpty(accession))
                return "Select a specimen to review analysis.";
            if (!analyzed)
                return $"{accession} has not been analyzed.";
            if (stale)
                return $"{accession} analysis is stale.";
            return $"{accession}: {suspected} suspected, {ruledOut} ruled out.";
        }

        public static string Reports(string? reportType) =>
            string.IsNullOrEmpty(reportType) ? "Reports." : $"Report: {reportType}.";

        public static string Analytics(string? rangeLabel, int confirmedSpecimens)
        {
            var range = string.IsNullOrEmpty(rangeLabel) ? "All time" : rangeLabel;
            return $"Analytics ({range}): {confirmedSpecimens} confirmed specimen(s).";
        }

        public static string Search(string? completedStatus, string? selected = null)
        {
            var line = string.IsNullOrEmpty(completedStatus) ? SearchIdle : completedStatus;
            if (string.IsNullOrEmpty(selected)) return line;
            return $"{line} Selected {selected}.";
        }

        public static string Rules(int count, string? name = null)
        {
            if (count == 0) return "No rules.";
            if (string.IsNullOrEmpty(name)) return $"Rules: {count}.";
            return $"Rules: {count}. Selected {name}.";
        }
    }
}
