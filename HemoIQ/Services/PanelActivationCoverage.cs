using System;
using System.Collections.Generic;
using System.Linq;
using HemoIQ.Data;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    public sealed record ActivationCoverage(Panel Prior, IReadOnlyList<string> Gaps, IReadOnlyList<string> AcsAntigensAffected)
    {
        public string PriorLabel => string.IsNullOrWhiteSpace(Prior.LotNumber) ? Prior.Name : $"lot {Prior.LotNumber!.Trim()}";

        /// <summary>For the activation prompt.</summary>
        public string Message =>
            $"Compared with the previous {Prior.Vendor} lot ({PriorLabel}), this lot does not type every cell for: " +
            string.Join(", ", Gaps) + "." +
            (AcsAntigensAffected.Count == 0 ? "" :
                $" {string.Join(", ", AcsAntigensAffected.Select(a => "anti-" + a))} " +
                (AcsAntigensAffected.Count == 1 ? "is" : "are") +
                " on the ACS list, so fewer cells will be available to rule " +
                (AcsAntigensAffected.Count == 1 ? "it" : "them") + " out.") +
            " Untyped cells stay NT and are never used as antigen-negative.";
    }

    /// <summary>
    /// When a panel is activated, compares it with the previous lot from the same vendor and product
    /// line (<see cref="DatabaseService.FindPriorVendorPanel"/>): antigens the previous lot typed on
    /// every cell but this lot types on fewer cells, or none. The import review shows the same
    /// comparison once; this repeats it at activation, which can be days later, and covers panels
    /// that were entered by hand or copied. The user decides; activating anyway is audited with the gaps.
    /// </summary>
    public static class PanelActivationCoverage
    {
        public static ActivationCoverage? Check(DatabaseService db, Panel panel, IReadOnlyCollection<string>? acsAntigens = null)
        {
            var prior = db.FindPriorVendorPanel(panel.Vendor, panel.ProductLine, panel.LotNumber);
            if (prior == null || prior.PanelId == panel.PanelId) return null;
            var compare = PanelComparer.Compare("previous", db.GetPanelCells(prior.PanelId), Array.Empty<string>(),
                "this", db.GetPanelCells(panel.PanelId), Array.Empty<string>());
            if (compare.CoverageGaps.Count == 0) return null;
            var acs = acsAntigens ?? AppSettings.Current.AcsAntigenList();
            var affected = compare.CoverageGaps
                .Select(g => g.Split(' ')[0])
                .Where(ag => acs.Contains(ag, StringComparer.Ordinal))
                .ToList();
            return new ActivationCoverage(prior, compare.CoverageGaps, affected);
        }
    }
}
