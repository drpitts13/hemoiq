using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using HemoIQ.Data;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    public enum ReportType
    {
        SpecimenSummary,
        PanelSummary,
        AnalysisResults,
        AllSpecimens,
        AllPanels,
        ClinicalIdentification,
        PanelAntigram,
        PendingWork
    }

    public class ReportService
    {
        private readonly DatabaseService _db;

        public ReportService(DatabaseService db) => _db = db;

        // ── Text preview ──────────────────────────────────────────────────────

        public string GeneratePreviewText(ReportType type, string? specimenId = null, int? panelId = null)
        {
            var body = type switch
            {
                ReportType.SpecimenSummary => SpecimenSummaryText(specimenId),
                ReportType.PanelSummary => PanelSummaryText(panelId),
                ReportType.AnalysisResults => AnalysisResultsText(specimenId),
                ReportType.AllSpecimens => AllSpecimensText(),
                ReportType.AllPanels => AllPanelsText(),
                ReportType.ClinicalIdentification => ClinicalIdentificationText(specimenId),
                ReportType.PanelAntigram => PanelAntigramText(panelId),
                ReportType.PendingWork => PendingWorkText(),
                _ => string.Empty
            };
            return FacilityHeader(AppSettings.Current) + SoftwareIdentity.ReportPreamble() + body;
        }

        /// <summary>
        /// The facility letterhead from Preferences (name, address, contact), one item per line, followed by a
        /// blank line. Empty when none is set, so the report starts with the product line as before.
        /// </summary>
        public static string FacilityHeader(LabSettings settings)
        {
            var lines = settings.FacilityHeaderLines();
            if (lines.Count == 0) return string.Empty;
            var sb = new StringBuilder();
            foreach (var line in lines)
                sb.AppendLine(SoftwareIdentity.Wrap(line, SoftwareIdentity.ReportLineWidth));
            sb.AppendLine();
            return sb.ToString();
        }

        private void AppendPatientRecord(StringBuilder sb, Specimen s)
        {
            if (string.IsNullOrWhiteSpace(s.Mrn)) return;
            var p = _db.GetPatient(s.Mrn);
            if (p == null)
            {
                sb.AppendLine($"Patient: no patient record for MRN {s.Mrn}");
                return;
            }
            sb.AppendLine($"Patient: {p.Display}");
            sb.AppendLine($"Antigen-negative units: {AntigenNegativeRequirement.Display(p.AntigenNegative)}");
            sb.AppendLine($"Transfusion requirements: {p.TransfusionRequirements ?? "None recorded"}");
            sb.AppendLine($"HPC transplant: {TransplantDisplay.Text(p)}");
            if (!string.IsNullOrWhiteSpace(p.BloodBankComments))
                sb.AppendLine($"Blood bank comments: {p.BloodBankComments}");
        }

        private void AppendPatientHistory(StringBuilder sb, Specimen s)
        {
            AppendPatientRecord(sb, s);
            if (string.IsNullOrWhiteSpace(s.Mrn)) return;
            var lines = PatientHistory.Describe(PatientHistory.OtherSpecimens(_db, s));
            sb.AppendLine($"Patient history (other specimens, MRN {s.Mrn}):");
            if (lines.Count == 0) sb.AppendLine("  None recorded.");
            foreach (var line in lines) sb.AppendLine($"  {line}");
        }

        private string SpecimenSummaryText(string? specimenId)
        {
            if (specimenId == null) return "No specimen selected.";
            var s = _db.GetSpecimen(specimenId);
            if (s == null) return $"Specimen {specimenId} not found.";
            var sb = new StringBuilder();
            sb.AppendLine($"SPECIMEN SUMMARY — {s.AccessionNumber}");
            sb.AppendLine(new string('=', 50));
            sb.AppendLine($"MRN:             {s.Mrn ?? "Not recorded"}");
            sb.AppendLine($"Type:            {s.Type}");
            sb.AppendLine($"Created:         {s.CreatedDate}");
            sb.AppendLine($"Expiration:      {s.ExpirationDate ?? "N/A"}");
            sb.AppendLine($"Last Analyzed:   {s.LastAnalyzedAt ?? "Never"}");
            if (!string.IsNullOrWhiteSpace(s.Phenotype))
                sb.AppendLine($"Phenotype:       {s.Phenotype}");
            if (!string.IsNullOrWhiteSpace(s.Genotype))
                sb.AppendLine($"Genotype:        {s.Genotype}");
            if (!string.IsNullOrWhiteSpace(s.PreviousAntibodies))
                sb.AppendLine($"Previous Abs:    {s.PreviousAntibodies}");
            sb.AppendLine($"Transfusion:     {s.TransfusionDisplay}");
            if (s.Dat.AnyRecorded)
                sb.AppendLine($"DAT:             {s.DatDisplay}");
            if (!string.IsNullOrWhiteSpace(s.Notes))
                sb.AppendLine($"Notes:           {s.Notes}");
            AppendPatientHistory(sb, s);
            sb.AppendLine();

            var panels = _db.GetSpecimenPanels(specimenId);
            sb.AppendLine($"LINKED PANELS ({panels.Count}):");
            foreach (var p in panels) sb.AppendLine($"  {p.Name} (Lot: {p.LotNumber ?? "N/A"})");
            sb.AppendLine();

            AppendCurrentSuspectedAndRuleouts(sb, specimenId);
            return sb.ToString();
        }

        private string PanelSummaryText(int? panelId)
        {
            if (panelId == null) return "No panel selected.";
            var p = _db.GetPanel(panelId.Value);
            if (p == null) return "Panel not found.";
            var sb = new StringBuilder();
            sb.AppendLine($"PANEL SUMMARY — {p.Name}");
            sb.AppendLine(new string('=', 50));
            sb.AppendLine($"Lot:        {p.LotNumber ?? "N/A"}");
            sb.AppendLine($"Vendor:     {p.Vendor ?? "N/A"}");
            sb.AppendLine($"Cells:      {p.NumCells}{(p.IncludeAc ? " + AC" : "")}");
            sb.AppendLine($"Expiration: {p.ExpirationDate ?? "N/A"}");
            var cells = _db.GetPanelCells(panelId.Value);
            AppendPanelDataQuality(sb, p, cells);
            sb.AppendLine();
            AppendAntigenGrid(sb, panelId.Value, cells);
            return sb.ToString();
        }

        /// <summary>Typing completeness and lot findings, the same checks the Worklist uses.</summary>
        private void AppendPanelDataQuality(StringBuilder sb, Panel p, List<PanelCell> cells)
        {
            var summary = PanelIntegrityCheck.Summarize(cells);
            sb.AppendLine($"Typing:     {summary.Display}");
            var findings = new List<string>();
            if (summary.Untyped.Count > 0)
                findings.Add(PanelIntegrityCheck.DescribeUntyped(p, summary.Untyped, summary.Cells));
            if (summary.Partial.Count > 0)
                findings.Add($"Typed on some cells but NT on others: {string.Join(", ", summary.Partial)}. " +
                             "Check these columns against the vendor antigram.");
            var dupes = PanelIntegrityCheck.DuplicatesOf(_db.GetAllPanels(), p.Vendor, p.LotNumber, p.PanelId);
            if (dupes.Count > 0)
                findings.Add($"Lot also stored as {PanelIntegrityCheck.DescribeDuplicates(dupes)}.");
            if (findings.Count == 0) return;
            sb.AppendLine("Data quality (review before use):");
            foreach (var f in findings) sb.AppendLine($"  • {f}");
        }

        private string PanelAntigramText(int? panelId)
        {
            if (panelId == null) return "No panel selected.";
            var p = _db.GetPanel(panelId.Value);
            if (p == null) return "Panel not found.";
            var sb = new StringBuilder();
            var lab = AppSettings.Current.LabName;
            sb.AppendLine(lab.ToUpperInvariant());
            sb.AppendLine("PANEL ANTIGRAM");
            sb.AppendLine($"{p.Name}   Lot: {p.LotNumber ?? "N/A"}   Vendor: {p.Vendor ?? "N/A"}   Exp: {p.ExpirationDate ?? "N/A"}");
            sb.AppendLine(new string('=', 78));
            AppendAntigenGrid(sb, panelId.Value, _db.GetPanelCells(panelId.Value));
            return sb.ToString();
        }

        private void AppendAntigenGrid(StringBuilder sb, int panelId, IReadOnlyList<PanelCell> cells)
        {
            var fullOrder = _db.GetPanelDisplayAntigens(panelId);
            var headerAntigens = PanelCell.AntigensTypedOnAnyCell(fullOrder, cells);
            sb.Append($"{"Cell",-6}");
            foreach (var ag in headerAntigens) sb.Append($" {ag,4}");
            sb.AppendLine();
            sb.AppendLine(new string('-', 6 + headerAntigens.Count * 5));
            foreach (var cell in cells)
            {
                sb.Append($"{cell.CellNumber,-6}");
                foreach (var ag in headerAntigens) sb.Append($" {cell.PrintedAntigen(ag),4}");
                sb.AppendLine();
            }
            AppendOmittedAntigenNote(sb, fullOrder.Count, headerAntigens.Count);
        }

        private static void AppendOmittedAntigenNote(StringBuilder sb, int fullCount, int printedCount)
        {
            if (printedCount < fullCount)
                sb.AppendLine("Antigens that are NT on every cell are omitted.");
        }

        /// <summary>
        /// Nonreactive cells that did not count toward a rule-out, with the reason (treatment destroys the antigen,
        /// the aliquot was absorbed, autoadsorption after transfusion, a failed check cell). The analysis tab shows
        /// these on Special Panels; the printed record carries them too, so a reader can see why an antibody is not
        /// ruled out and what to repeat.
        /// </summary>
        private static void AppendGatedRuleouts(StringBuilder sb, AnalysisResult result)
        {
            var lines = result.GatedRuleouts
                .Select(g => $"{g.Antibody}: {g.Reason}")
                .Distinct(StringComparer.Ordinal)
                .OrderBy(l => l, StringComparer.Ordinal)
                .ToList();
            if (lines.Count == 0) return;
            sb.AppendLine($"Rule-outs not counted ({lines.Count}):");
            foreach (var line in lines)
                sb.AppendLine("  " + line);
        }

        private string AnalysisResultsText(string? specimenId)
        {
            if (specimenId == null) return "No specimen selected.";
            var runs = _db.GetAllSpecimenRuns(specimenId);
            var sb = new StringBuilder();
            sb.AppendLine($"ANALYSIS RESULTS — {specimenId}");
            sb.AppendLine(new string('=', 50));
            sb.AppendLine(AnalysisTraceLine(specimenId));
            var result = TryAnalyze(specimenId);
            if (result == null)
            {
                AppendSavedSuspectedAndRuleouts(sb, specimenId);
            }
            else
            {
                AppendSuspectedAndRuleouts(sb, result);
                AppendGatedRuleouts(sb, result);
                var pattern = SpecimenPatternEvidence.Format(result);
                if (!string.IsNullOrWhiteSpace(pattern))
                {
                    sb.AppendLine();
                    sb.AppendLine("PATTERN EVIDENCE (not a diagnosis):");
                    sb.AppendLine(pattern);
                }
            }

            if (runs.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"PANEL RUNS ({runs.Count}):");
                foreach (var run in runs)
                    sb.AppendLine($"  [{run.PanelName}] {run.DisplayLabel}");
            }
            return sb.ToString();
        }

        private void AppendAmendmentHistory(StringBuilder sb, IReadOnlyList<AmendmentEntry> amendments, string accession)
        {
            if (amendments.Count == 0) return;
            sb.AppendLine("  Amendment history (earlier identifications, cleared with a reason):");
            foreach (var line in AmendmentHistory.Lines(amendments))
                sb.AppendLine(line);
            var superseded = AmendmentHistory.SupersededReports(_db, accession);
            if (superseded.Count == 0) return;
            sb.AppendLine($"  Superseded reports ({superseded.Count}): issued before the identification was cleared. " +
                          "Recall these copies or mark them superseded:");
            foreach (var line in AmendmentHistory.SupersededLines(superseded))
                sb.AppendLine(line);
        }

        private string ClinicalIdentificationText(string? specimenId)
        {
            if (specimenId == null) return "No specimen selected.";
            var s = _db.GetSpecimen(specimenId);
            if (s == null) return $"Specimen {specimenId} not found.";

            var settings = AppSettings.Current;
            var analyzer = new AntibodyAnalyzer(_db);
            var analysis = analyzer.AnalyzeSpecimen(specimenId, updateDb: false);
            var runs = _db.GetAllSpecimenRuns(specimenId);
            var sb = new StringBuilder();

            sb.AppendLine(settings.LabName.ToUpperInvariant());
            if (!string.IsNullOrWhiteSpace(settings.Department))
                sb.AppendLine(settings.Department);
            sb.AppendLine("ANTIBODY IDENTIFICATION WORKSHEET");
            sb.AppendLine(new string('=', 78));
            var amendments = AmendmentHistory.For(_db, specimenId);
            if (AmendmentHistory.Banner(s.HasFinalCall, amendments) is string banner)
                sb.AppendLine(banner);
            sb.AppendLine(AnalysisTraceLine(specimenId));
            sb.AppendLine($"Accession: {s.AccessionNumber,-16} Type: {s.Type,-10} Date: {DateTime.Now:yyyy-MM-dd}");
            sb.AppendLine($"MRN: {s.Mrn ?? "Not recorded"}");
            sb.AppendLine($"Investigation status: {InvestigationStatusService.Compute(_db, s).Summary}");
            sb.AppendLine($"Collected: {s.CollectedAt ?? "not recorded"}   Received: {s.ReceivedAt ?? s.CreatedDate}   Expires: {s.ExpirationDate ?? "N/A"}");
            sb.AppendLine($"Origin: {(s.OriginDisplay.Length == 0 ? "not recorded" : s.OriginDisplay)}");
            sb.AppendLine($"Receipt checklist: {ReceiptChecklist.Summary(ReceiptChecklist.Evaluate(_db, s))}");
            sb.AppendLine($"Phenotype: {s.Phenotype ?? "N/A"}");
            sb.AppendLine($"Genotype: {s.Genotype ?? "N/A"}");
            sb.AppendLine($"Previous antibodies: {s.PreviousAntibodies ?? "N/A"}");
            sb.AppendLine($"Transfusion: {s.TransfusionDisplay}");
            sb.AppendLine($"Pregnancy: {s.PregnancyDisplay}");
            sb.AppendLine($"DAT: {s.DatDisplay}");
            sb.AppendLine($"Visual hemolysis: {SpecimenHemolysis.Label(s.Hemolysis)}");
            if (!string.IsNullOrWhiteSpace(s.Notes))
                sb.AppendLine($"Notes: {s.Notes}");
            AppendPatientHistory(sb, s);
            sb.AppendLine();

            foreach (var run in runs)
            {
                var panel = _db.GetPanel(run.PanelId);
                var cells = _db.GetPanelCells(run.PanelId);
                var rxns = _db.GetReactions(run.RunId).ToDictionary(r => r.CellNumber);
                sb.AppendLine($"Panel: {panel?.Name ?? run.PanelName}   Lot: {panel?.LotNumber ?? "N/A"}   Vendor: {panel?.Vendor ?? "N/A"}");
                sb.AppendLine($"Run: {run.DisplayLabel}");

                var fullOrder = _db.GetPanelDisplayAntigens(run.PanelId);
                var antigens = PanelCell.AntigensTypedOnAnyCell(fullOrder, cells);
                sb.Append($"{"Cell",-5}");
                foreach (var ag in antigens)
                    sb.Append(ag.PadLeft(Math.Max(3, ag.Length) + 1));
                sb.Append("  IS   37   AHG  CC");
                sb.AppendLine();
                sb.AppendLine(new string('-', 5 + antigens.Sum(a => Math.Max(3, a.Length) + 1) + 22));

                foreach (var cell in cells)
                {
                    rxns.TryGetValue(cell.CellNumber, out var rxn);
                    sb.Append($"{cell.CellNumber,-5}");
                    foreach (var ag in antigens)
                    {
                        var w = Math.Max(3, ag.Length) + 1;
                        var v = cell.PrintedAntigen(ag);
                        sb.Append(v.PadLeft(w));
                    }
                    sb.Append($"  {(rxn?.IS ?? "NT"),-4} {(rxn?.C37 ?? "NT"),-4} {(rxn?.AHG ?? "NT"),-4} {rxn?.CC ?? "NT"}");
                    sb.AppendLine();
                }
                AppendOmittedAntigenNote(sb, fullOrder.Count, antigens.Count);
                sb.AppendLine();
            }

            sb.AppendLine("INTERPRETATION");
            sb.AppendLine(new string('-', 40));
            if (s.HasFinalCall)
            {
                sb.AppendLine("FINAL IDENTIFICATION (confirmed)");
                sb.AppendLine($"  {s.FinalAntibodies}");
                sb.AppendLine($"  Confirmed by {s.IdentifiedBy ?? "—"} on {s.IdentifiedAt ?? "—"}");
                foreach (var o in ConfirmationOverrides.ForCurrentConfirmation(_db, s.AccessionNumber))
                    sb.AppendLine($"  Warning overridden at confirmation: {o}");
                if (s.IsReviewed)
                    sb.AppendLine($"  Supervisor reviewed by {s.ReviewedBy} on {s.ReviewedAt ?? "—"}");
                else if (AppSettings.Current.RequireSupervisorReview)
                    sb.AppendLine("  Supervisor review: PENDING");
                if (!string.IsNullOrWhiteSpace(s.FinalComment))
                    sb.AppendLine($"  Comment: {s.FinalComment}");
                AppendAmendmentHistory(sb, amendments, s.AccessionNumber);
                sb.AppendLine();
                sb.AppendLine("Analyzer suspected (for reference):");
                if (analysis.Suspected.Count > 0)
                {
                    foreach (var (ab, prob) in analysis.Suspected.OrderByDescending(x => x.Value))
                        sb.AppendLine(FormatSuspectedAntibodyLine(ab, prob, analysis));
                }
                else
                {
                    sb.AppendLine("  None.");
                }
            }
            else
            {
                if (amendments.Count > 0)
                {
                    AppendAmendmentHistory(sb, amendments, s.AccessionNumber);
                    sb.AppendLine();
                }
                if (analysis.Acs.Antigens.Count > 0 && (analysis.Acs.IsEligible || analysis.Acs.IsEligibleWithException))
                    sb.AppendLine("Clinically significant antibodies evaluated (lab-configured): " +
                                  string.Join(", ", analysis.Acs.Antigens.Select(ag => "anti-" + ag)) + ".");
                if (analysis.Acs.IsEligible)
                {
                    sb.AppendLine("All clinically significant antibodies are ruled out.");
                    sb.AppendLine($"Suggested result: {AntigenConstants.AcsResultText}");
                }
                else if (analysis.Acs.IsEligibleWithException)
                {
                    sb.AppendLine("All clinically significant antibodies are ruled out, with ≥95% exception.");
                    sb.AppendLine($"Suggested result: {analysis.Acs.SuggestedCombinedResult}");
                    if (!string.IsNullOrWhiteSpace(analysis.Acs.SuggestedComment))
                        sb.AppendLine($"Comment: {analysis.Acs.SuggestedComment}");
                }

                if (analysis.Suspected.Count > 0)
                {
                    sb.AppendLine("Suspected antibodies (unconfirmed):");
                    foreach (var (ab, prob) in analysis.Suspected.OrderByDescending(x => x.Value))
                        sb.AppendLine(FormatSuspectedAntibodyLine(ab, prob, analysis));
                }
                else if (!analysis.Acs.IsEligible && !analysis.Acs.IsEligibleWithException
                         && SpecimenPatternEvidence.PatternLines(analysis).Count == 0)
                {
                    sb.AppendLine("No antibodies suspected based on current reactions.");
                }
            }
            AppendPatternEvidence(sb, analysis);
            sb.AppendLine();
            if (analysis.RuleoutEvaluations.Count > 0)
            {
                var met = analysis.RuleoutEvaluations.Where(e => e.MeetsCriteria).ToList();
                var pending = analysis.RuleoutEvaluations.Where(e => !e.MeetsCriteria).ToList();
                if (met.Count > 0)
                {
                    sb.AppendLine("Ruled out (configured criteria met):");
                    foreach (var ev in met.OrderBy(e => e.Antibody))
                        sb.AppendLine("  " + ev.Explanation);
                }
                if (pending.Count > 0)
                {
                    sb.AppendLine("Rule-out in progress:");
                    foreach (var ev in pending.OrderBy(e => e.Antibody))
                        sb.AppendLine("  " + ev.Explanation);
                }
            }
            else if (analysis.RuledOut.Count > 0)
            {
                sb.AppendLine("Ruled out:");
                sb.AppendLine("  " + string.Join(", ", analysis.RuledOut.Keys.OrderBy(x => x)));
            }
            AppendGatedRuleouts(sb, analysis);
            // A confirmed call is checked as confirmed; otherwise the antibodies meeting the ID rule.
            var separation = s.HasFinalCall
                ? analyzer.EvaluateFinalCall(specimenId, s.FinalAntibodies)
                : analysis.SeparationEvaluations;
            if (separation.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(s.HasFinalCall
                    ? "Multiple-antibody separation for the confirmed call (suggested criterion, not a rule-out):"
                    : "Multiple-antibody separation (suggested criterion, not a rule-out):");
                foreach (var sep in separation)
                    sb.AppendLine($"  {RuleoutPolicy.DisplayAntibody(sep.Antibody)}: {sep.Display}" +
                                  (sep.MeetsCriterion ? "" : " — SHORT") + $". {sep.Explanation}");
            }
            if (analysis.AbsorptionConclusions.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Absorption comparison (evidence only, not a diagnosis):");
                foreach (var abs in analysis.AbsorptionConclusions)
                    sb.AppendLine("  " + abs.Explanation);
            }
            if (analysis.AdsorptionScoreExclusions.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Ordinary probability and pattern scores excluded these reactions:");
                foreach (var exclusion in analysis.AdsorptionScoreExclusions)
                    sb.AppendLine("  " + exclusion);
            }
            if (analysis.CandidateExplanations.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Explain Analysis (decision support, not a diagnosis):");
                foreach (var exp in analysis.CandidateExplanations.Take(8))
                    sb.AppendLine("  " + exp.Narrative);
            }
            // Separation lines already printed above are not repeated.
            var printed = separation.Select(e => e.Explanation).ToHashSet(StringComparer.Ordinal);
            var suggestions = analysis.Suggestions.Where(x => !printed.Contains(x)).ToList();
            if (suggestions.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Suggestions:");
                foreach (var sug in suggestions)
                    sb.AppendLine($"  • {sug}");
            }

            sb.AppendLine();
            sb.AppendLine("INVESTIGATION TIMELINE");
            sb.AppendLine(new string('-', 40));
            foreach (var e in InvestigationTimeline.Build(_db, s))
                sb.AppendLine($"  {e.Display}");

            sb.AppendLine();
            sb.AppendLine(new string('_', 78));
            sb.AppendLine("Technologist: ___________________________  Date: ______________");
            sb.AppendLine();
            sb.AppendLine("Supervisor:   ___________________________  Date: ______________");
            return sb.ToString();
        }

        private static void AppendPatternEvidence(StringBuilder sb, AnalysisResult analysis)
        {
            var lines = SpecimenPatternEvidence.PatternLines(analysis);
            if (lines.Count == 0) return;
            sb.AppendLine();
            sb.AppendLine("Pattern evidence (not a diagnosis):");
            foreach (var line in lines)
                sb.AppendLine("  " + line);
        }

        private void AppendCurrentSuspectedAndRuleouts(StringBuilder sb, string specimenId)
        {
            var result = TryAnalyze(specimenId);
            if (result == null)
            {
                AppendSavedSuspectedAndRuleouts(sb, specimenId);
                return;
            }
            AppendSuspectedAndRuleouts(sb, result);
        }

        private static void AppendSuspectedAndRuleouts(StringBuilder sb, AnalysisResult result)
        {
            var suspected = SpecimenPatternEvidence.SuspectedRows(result);
            sb.AppendLine($"SUSPECTED ANTIBODIES ({suspected.Count}):");
            foreach (var a in suspected)
                sb.AppendLine($"  {a.Antibody}  {a.Probability * 100:F1}%");
            sb.AppendLine();
            var ruleouts = SpecimenPatternEvidence.RuleoutRows(result);
            sb.AppendLine($"RULED-OUT ANTIBODIES ({ruleouts.Count}):");
            foreach (var r in ruleouts)
                sb.AppendLine(FormatRuleoutCountLine(r));
        }

        private static string FormatRuleoutCountLine(SpecimenRuleout r)
        {
            var line = $"  {r.Antibody}  (x{r.RuleoutCount})";
            return string.IsNullOrWhiteSpace(r.Note) ? line : $"{line}  {r.Note}";
        }

        private void AppendSavedSuspectedAndRuleouts(StringBuilder sb, string specimenId)
        {
            var antibodies = _db.GetSpecimenAntibodies(specimenId);
            sb.AppendLine($"SUSPECTED ANTIBODIES ({antibodies.Count}):");
            foreach (var a in antibodies)
                sb.AppendLine($"  {a.Antibody}  {a.Probability * 100:F1}%");
            sb.AppendLine();
            var ruleouts = _db.GetSpecimenRuleouts(specimenId);
            sb.AppendLine($"RULED-OUT ANTIBODIES ({ruleouts.Count}):");
            foreach (var r in ruleouts)
                sb.AppendLine(FormatRuleoutCountLine(r));
        }

        private void WriteSpecimenSummaryCsv(CsvWriter csv, string specimenId)
        {
            var result = TryAnalyze(specimenId);
            var antibodies = result == null
                ? _db.GetSpecimenAntibodies(specimenId)
                : SpecimenPatternEvidence.SuspectedRows(result);
            var ruleouts = result == null
                ? _db.GetSpecimenRuleouts(specimenId)
                : SpecimenPatternEvidence.RuleoutRows(result);

            csv.WriteField("Antibodies");
            csv.NextRecord();
            csv.WriteRecords(antibodies.Select(a => new { a.Antibody, Score = $"{a.Probability * 100:F1}%" }));
            csv.WriteField("Ruleouts");
            csv.NextRecord();
            csv.WriteField("Antibody");
            csv.WriteField("RuleoutCount");
            csv.WriteField("CheckCell");
            csv.NextRecord();
            foreach (var r in ruleouts)
            {
                csv.WriteField(r.Antibody);
                csv.WriteField(r.RuleoutCount);
                csv.WriteField(r.Note);
                csv.NextRecord();
            }
        }

        private AnalysisResult? TryAnalyze(string specimenId)
        {
            try
            {
                return new AntibodyAnalyzer(_db).AnalyzeSpecimen(specimenId, updateDb: false);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string AnalysisTraceLine(string specimenId)
        {
            var settings = AppSettings.Current;
            var snap = _db.GetLatestAnalysisSnapshot(specimenId);
            var version = snap?.SoftwareVersion ?? SoftwareIdentity.Version;
            var settingsText = snap?.SettingsJson
                ?? $"threshold={settings.ProbabilityThreshold:0.00}; idRule={settings.IdentificationRuleLabel}; acs={settings.AcsRuleoutCount}";
            var fingerprint = snap == null ? "n/a" : snap.InputFingerprint;
            var rules = snap?.RuleEngineVersion == null
                ? ""
                : $"  Rule engine v{snap.RuleEngineVersion}";
            var who = string.IsNullOrWhiteSpace(snap?.AnalyzedBy)
                ? ""
                : $"  Operator {snap!.AnalyzedBy}";
            var absorption = string.IsNullOrWhiteSpace(snap?.AbsorptionJson)
                ? ""
                : "  Absorption comparison stored";
            return $"Software {version}{rules}{who}{absorption}  Settings: {settingsText}  Input fingerprint: {fingerprint}";
        }

        private static string FormatSuspectedAntibodyLine(string antibody, double probability,
            AnalysisResult analysis)
        {
            analysis.SuspectedStatistics.TryGetValue(antibody, out var stats);
            var id = stats != null ? $"  {stats.IdentificationDetail}" : "";
            return $"  {antibody}  {probability * 100:F1}%{id}";
        }

        private string AllSpecimensText()
        {
            var specimens = _db.GetAllSpecimens();
            var sb = new StringBuilder();
            sb.AppendLine($"ALL SPECIMENS ({specimens.Count})");
            sb.AppendLine(new string('=', 100));
            sb.AppendLine($"{"Accession",-20} {"Type",-8} {"Status",-14} {"Final ID",-30} {"Created",-12} {"Expiration",-12}");
            sb.AppendLine(new string('-', 100));
            foreach (var s in specimens)
            {
                var status = GetSpecimenStatus(s);
                var finalId = s.FinalAntibodies ?? "";
                if (finalId.Length > 28) finalId = finalId[..28] + "…";
                sb.AppendLine($"{s.AccessionNumber,-20} {s.Type,-8} {status,-14} {finalId,-30} {s.CreatedDate,-12} {s.ExpirationDate ?? "N/A",-12}");
            }
            return sb.ToString();
        }

        private string GetSpecimenStatus(Specimen s)
        {
            if (s.HasFinalCall) return "Confirmed";
            if (s.LastAnalyzedAt == null) return "Not analyzed";
            if (AnalysisCurrency.StaleReason(s, _db.LastAnalysisConfigurationChange(), _db.SettingsDrifted(s)) != null) return "Stale";
            return "Analyzed";
        }

        private string PendingWorkText()
        {
            var allSpecimens = _db.GetAllSpecimens().Where(s => s.IsActive).ToList();
            var pending = allSpecimens.Where(s => !s.HasFinalCall).ToList();
            var sb = new StringBuilder();
            var settings = AppSettings.Current;
            sb.AppendLine(settings.LabName.ToUpperInvariant());
            sb.AppendLine("PENDING WORK SUMMARY");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine(new string('=', 78));
            sb.AppendLine($"Active specimens:  {allSpecimens.Count}");
            sb.AppendLine($"Confirmed:         {allSpecimens.Count(s => s.HasFinalCall)}");
            sb.AppendLine($"Pending:           {pending.Count}");
            sb.AppendLine();

            var notAnalyzed = pending.Where(s => s.LastAnalyzedAt == null).ToList();
            var configChanged = _db.LastAnalysisConfigurationChange();
            var stale = pending.Where(s => AnalysisCurrency.StaleReason(s, configChanged, _db.SettingsDrifted(s)) != null).ToList();
            var analyzed = pending.Where(s => s.LastAnalyzedAt != null && AnalysisCurrency.StaleReason(s, configChanged, _db.SettingsDrifted(s)) == null).ToList();

            if (notAnalyzed.Count > 0)
            {
                sb.AppendLine($"NOT YET ANALYZED ({notAnalyzed.Count}):");
                sb.AppendLine($"  {"Accession",-20} {"Type",-8} {"Expiration",-12} {"Previous Abs"}");
                sb.AppendLine("  " + new string('-', 64));
                foreach (var s in notAnalyzed)
                    sb.AppendLine($"  {s.AccessionNumber,-20} {s.Type,-8} {s.ExpirationDate ?? "N/A",-12} {s.PreviousAntibodies ?? ""}");
                sb.AppendLine();
            }

            if (stale.Count > 0)
            {
                sb.AppendLine($"STALE ANALYSIS ({stale.Count}):");
                sb.AppendLine($"  {"Accession",-20} {"Type",-8} {"Expiration",-12} {"Last Analyzed"}");
                sb.AppendLine("  " + new string('-', 64));
                foreach (var s in stale)
                {
                    sb.AppendLine($"  {s.AccessionNumber,-20} {s.Type,-8} {s.ExpirationDate ?? "N/A",-12} {s.LastAnalyzedAt ?? "—"}");
                    sb.AppendLine($"    {AnalysisCurrency.StaleReason(s, configChanged, _db.SettingsDrifted(s))}");
                }
                sb.AppendLine();
            }

            if (analyzed.Count > 0)
            {
                sb.AppendLine($"ANALYZED — AWAITING CONFIRMATION ({analyzed.Count}):");
                sb.AppendLine($"  {"Accession",-20} {"Type",-8} {"Expiration",-12} {"Top Suspected Ab"}");
                sb.AppendLine("  " + new string('-', 64));
                foreach (var s in analyzed)
                {
                    var top = _db.GetSpecimenAntibodies(s.AccessionNumber)
                        .OrderByDescending(a => a.Probability)
                        .FirstOrDefault();
                    var topLabel = top != null ? $"{top.Antibody} ({top.Probability * 100:F0}%)" : "None suspected";
                    sb.AppendLine($"  {s.AccessionNumber,-20} {s.Type,-8} {s.ExpirationDate ?? "N/A",-12} {topLabel}");
                }
                sb.AppendLine();
            }

            if (pending.Count == 0)
                sb.AppendLine("All active specimens have a confirmed antibody identification. No pending work.");

            return sb.ToString();
        }

        private string AllPanelsText()
        {
            var panels = _db.GetAllPanels();
            var nameW = ColumnWidth("Name", panels.Select(p => p.Name));
            var lotW = ColumnWidth("Lot", panels.Select(p => p.LotNumber ?? "N/A"));
            var vendorW = ColumnWidth("Vendor", panels.Select(p => p.Vendor ?? "N/A"));
            var cellsW = ColumnWidth("Cells", panels.Select(p => p.NumCells.ToString()));
            var expW = ColumnWidth("Expires", panels.Select(p => p.ExpirationDate ?? "N/A"));
            var lineW = nameW + lotW + vendorW + cellsW + expW + 4;
            var sb = new StringBuilder();
            sb.AppendLine($"ALL PANELS ({panels.Count})");
            sb.AppendLine(new string('=', lineW));
            sb.AppendLine(string.Join(" ",
                "Name".PadRight(nameW),
                "Lot".PadRight(lotW),
                "Vendor".PadRight(vendorW),
                "Cells".PadRight(cellsW),
                "Expires".PadRight(expW),
                "Typing"));
            sb.AppendLine(new string('-', lineW));
            foreach (var p in panels)
                sb.AppendLine(string.Join(" ",
                    p.Name.PadRight(nameW),
                    (p.LotNumber ?? "N/A").PadRight(lotW),
                    (p.Vendor ?? "N/A").PadRight(vendorW),
                    p.NumCells.ToString().PadRight(cellsW),
                    (p.ExpirationDate ?? "N/A").PadRight(expW),
                    PanelIntegrityCheck.Summarize(_db.GetPanelCells(p.PanelId)).Display));
            return sb.ToString();
        }

        private static int ColumnWidth(string header, IEnumerable<string> values) =>
            Math.Max(header.Length, values.Select(v => v.Length).DefaultIfEmpty(0).Max());

        // ── Report output audit ───────────────────────────────────────────────

        /// <summary>Records a printed report. Call after the print job was sent.</summary>
        public void RecordPrint(ReportType type, string? specimenId, int? panelId, string printedText) =>
            RecordReportOutput(type, "print", specimenId, panelId, null, Encoding.UTF8.GetBytes(printedText));

        public static bool IsSpecimenReport(ReportType t) =>
            t is ReportType.SpecimenSummary or ReportType.AnalysisResults or ReportType.ClinicalIdentification;

        public static bool IsPanelReport(ReportType t) => t is ReportType.PanelSummary or ReportType.PanelAntigram;

        /// <summary>
        /// Writes a report_output audit event: report type, output kind, file name,
        /// SHA-256 of what was issued, software version, and (for specimen reports) the
        /// identification and review state at that moment. Specimen and panel reports are
        /// attached to their specimen or panel so they appear on that record's timeline.
        /// </summary>
        private void RecordReportOutput(ReportType type, string output, string? specimenId, int? panelId,
            string? filePath, byte[] content)
        {
            string entityType = "report", entityId = type.ToString();
            string? finalAntibodies = null, reviewedBy = null;
            if (IsSpecimenReport(type) && specimenId != null)
            {
                entityType = "specimen";
                entityId = specimenId;
                var s = _db.GetSpecimen(specimenId);
                finalAntibodies = s?.FinalAntibodies;
                reviewedBy = s?.ReviewedBy;
            }
            else if (IsPanelReport(type) && panelId.HasValue)
            {
                entityType = "panel";
                entityId = panelId.Value.ToString(CultureInfo.InvariantCulture);
            }
            _db.AppendAudit("report_output", entityType, entityId, null, afterJson: JsonSerializer.Serialize(new
            {
                reportType = type.ToString(),
                output,
                file = filePath == null ? null : Path.GetFileName(filePath),
                sha256 = Convert.ToHexString(SHA256.HashData(content)),
                softwareVersion = SoftwareIdentity.Version,
                finalAntibodies,
                reviewedBy,
            }));
        }

        public static string ReportLabel(string? reportType) => reportType switch
        {
            nameof(ReportType.SpecimenSummary) => "Specimen Summary",
            nameof(ReportType.PanelSummary) => "Panel Summary",
            nameof(ReportType.AnalysisResults) => "Analysis Results",
            nameof(ReportType.ClinicalIdentification) => "Clinical Identification",
            nameof(ReportType.PanelAntigram) => "Panel Antigram",
            nameof(ReportType.AllPanels) => "All Panels",
            nameof(ReportType.AllSpecimens) => "All Specimens",
            nameof(ReportType.PendingWork) => "Pending Work",
            _ => reportType ?? "Report",
        };

        // ── CSV Export ────────────────────────────────────────────────────────

        public void ExportToCsv(ReportType type, string filePath,
            string? specimenId = null, int? panelId = null)
        {
            WriteCsv(type, filePath, specimenId, panelId);
            RecordReportOutput(type, "csv", specimenId, panelId, filePath, File.ReadAllBytes(filePath));
        }

        private void WriteCsv(ReportType type, string filePath, string? specimenId, int? panelId)
        {
            using var writer = CsvText.File(filePath);
            using var csv = CsvText.Writer(writer);

            switch (type)
            {
                case ReportType.AllSpecimens:
                    csv.WriteRecords(_db.GetAllSpecimens().Select(s => new
                    {
                        s.AccessionNumber, s.Type, s.CreatedDate, s.ExpirationDate, s.LastAnalyzedAt
                    }));
                    break;
                case ReportType.AllPanels:
                    csv.WriteRecords(_db.GetAllPanels().Select(p => new
                    {
                        p.Name, p.LotNumber, p.Vendor, p.NumCells, p.ExpirationDate
                    }));
                    break;
                case ReportType.SpecimenSummary when specimenId != null:
                    WriteSpecimenSummaryCsv(csv, specimenId);
                    break;
                case ReportType.PanelSummary when panelId.HasValue:
                    var cells = _db.GetPanelCells(panelId.Value);
                    var antigens = PanelCell.AntigensTypedOnAnyCell(
                        _db.GetPanelDisplayAntigens(panelId.Value), cells);
                    csv.WriteField("Cell");
                    foreach (var ag in antigens) csv.WriteField(ag);
                    csv.NextRecord();
                    foreach (var cell in cells)
                    {
                        csv.WriteField(cell.CellNumber);
                        foreach (var ag in antigens) csv.WriteField(cell.PrintedAntigen(ag));
                        csv.NextRecord();
                    }
                    break;
                default:
                    writer.Write(GeneratePreviewText(type, specimenId, panelId));
                    break;
            }
        }

        // ── PDF Export ────────────────────────────────────────────────────────

        public void ExportToPdf(ReportType type, string filePath,
            string? specimenId = null, int? panelId = null)
        {
            var text = GeneratePreviewText(type, specimenId, panelId);
            var doc = new PdfDocument();
            doc.Info.Title = type.ToString();
            var page = doc.AddPage();
            page.Size = PdfSharp.PageSize.Letter;
            bool landscape = type is ReportType.ClinicalIdentification or ReportType.PanelAntigram;
            if (landscape)
                page.Orientation = PdfSharp.PageOrientation.Landscape;
            XGraphics? gfx = XGraphics.FromPdfPage(page);
            var fontSize = landscape ? 7 : 9;
            var font = new XFont("Courier New", fontSize);
            double x = 28, y = 28, lineH = landscape ? 10 : 13;
            double bottom = page.Height.Point - 36;
            foreach (var line in text.Split('\n'))
            {
                if (y > bottom)
                {
                    gfx?.Dispose();
                    page = doc.AddPage();
                    page.Size = PdfSharp.PageSize.Letter;
                    if (landscape)
                        page.Orientation = PdfSharp.PageOrientation.Landscape;
                    gfx = XGraphics.FromPdfPage(page);
                    y = 28;
                }
                gfx!.DrawString(line.TrimEnd(), font, XBrushes.Black,
                    new XRect(x, y, page.Width.Point - 56, lineH), XStringFormats.TopLeft);
                y += lineH;
            }
            gfx?.Dispose();
            doc.Save(filePath);
            RecordReportOutput(type, "pdf", specimenId, panelId, filePath, Encoding.UTF8.GetBytes(text));
            doc.Dispose();
        }
    }
}
