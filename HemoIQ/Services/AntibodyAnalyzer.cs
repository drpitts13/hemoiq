using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HemoIQ.Data;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    /// <summary>
    /// Antibody identification engine.
    /// Operates across all panel runs for a specimen; each run's treatment context
    /// gates which antigens and phases contribute to rule-outs and probability scoring.
    /// </summary>
    public class AntibodyAnalyzer
    {
        private readonly DatabaseService _db;
        private readonly HashSet<int> _panreactiveUntreatedRunIds = new();

        public AntibodyAnalyzer(DatabaseService db) => _db = db;

        // ── Public entry point ────────────────────────────────────────────────

        public AnalysisResult AnalyzeSpecimen(string specimenId, bool updateDb = true)
        {
            var reactions = _db.GetAllSpecimenReactions(specimenId);
            var empty = new AnalysisResult { SpecimenId = specimenId };
            if (reactions.Count == 0) return empty;

            var runs = _db.GetAllSpecimenRuns(specimenId);
            var contexts = BuildContexts(runs, specimenId);
            var antigens = RelevantAntigens(contexts);
            var byRun = GroupByRun(reactions);

            var rules = _db.GetAllRules();
            var scoreExclusions = new HashSet<string>(StringComparer.Ordinal);
            MarkPanreactiveUntreatedBesideAdsorption(byRun, contexts, scoreExclusions);
            var observedRuleouts = CalculateRuleouts(byRun, contexts, antigens, rules, out var gatedRuleouts);
            var detailedRuleouts = GetDetailedRuleouts(byRun, contexts, antigens, rules, out var dosageExcluded);
            var evaluations = BuildRuleoutEvaluations(
                observedRuleouts, detailedRuleouts, dosageExcluded, rules, byRun, contexts, antigens, scoreExclusions);
            var ruledOut = evaluations
                .Where(e => e.MeetsCriteria)
                .ToDictionary(e => e.Antibody, e => e.ObservedCount);
            var (suspected, suspectedStats, allScores) = CalculateProbabilities(
                byRun, contexts, antigens, ruledOut, scoreExclusions);
            var patterns = PatternMatching(byRun, contexts, antigens, ruledOut, scoreExclusions);
            var suspectedEvidence = GetSuspectedAntibodyEvidence(
                byRun, contexts, suspected, scoreExclusions);
            var combinations = DetectAntibodyCombinations(
                byRun, contexts, suspected, scoreExclusions);
            var phaseProbabilities = CalculatePhaseSpecificProbabilities(
                byRun, contexts, antigens, ruledOut, scoreExclusions);
            var dosageEffects = DetectDosageEffects(byRun, contexts, suspected, scoreExclusions);
            var inferences = BuildTreatmentInferences(byRun, contexts, suspected);

            // Names the actual lock (cancelled or confirmed) and who can lift it.
            if (updateDb)
            {
                // Saving an analysis replaces the stored suspected and ruled-out lists and writes a snapshot, so it
                // needs Record results like every other change: a view-only session may view, not store, an analysis.
                AccessControl.Demand(_db, Permission.RecordResults);
                _db.EnsureSpecimenUnlocked(specimenId);
            }

            var untypedCs = AppSettings.Current.AcsAntigenList()
                .Where(ag => !antigens.Contains(ag))
                .ToList();
            var untypeable = UntypeableSpecificityClassifier.Classify(
                antigens, AntigensNtOnEveryCell(contexts));

            var result = new AnalysisResult
            {
                SpecimenId = specimenId,
                RuledOut = ruledOut,
                RuleoutEvaluations = evaluations,
                UntypedClinicallySignificant = untypedCs,
                UntypeableSpecificities = untypeable,
                Suspected = suspected,
                SuspectedStatistics = suspectedStats,
                PatternMatches = patterns,
                DetailedRuleouts = detailedRuleouts,
                SuspectedEvidence = suspectedEvidence,
                Combinations = combinations,
                PhraseProbabilities = phaseProbabilities,
                DosageEffects = dosageEffects,
                GatedRuleouts = gatedRuleouts,
                TreatmentInferences = inferences,
                Acs = EvaluateAcs(observedRuleouts, allScores),
                RuleEngineVersion = AnalysisRuleTrace.EngineVersion,
                RuleConfigurationNote = AnalysisRuleTrace.Explain(rules, AppSettings.Current),
                AnalyzedBy = AnalysisRuleTrace.ResolveOperator(AppSettings.Current),
                OperatorNote = AnalysisRuleTrace.FormatOperatorNote(AppSettings.Current),
            };
            var transfusion = ApplyPatientTyping(specimenId, result);
            if (_db.GetSpecimen(specimenId) is Specimen hemolysisSource)
            {
                result.TransfusionRequirementNotes.InsertRange(0,
                    SpecimenHemolysis.ReviewNotes(hemolysisSource.Hemolysis, reactions));
                result.TransfusionRequirementNotes.InsertRange(0,
                    PatientSexCheck.ReviewNotes(_db.GetPatient(hemolysisSource.Mrn), hemolysisSource));
            }
            result.SpecialReactionNotes = DescribeSpecialGrades(reactions);
            result.SpecialReactionNotes.AddRange(
                TransfusionContext.DescribeMixedFieldAutocontrol(reactions, transfusion));
            if (_db.GetSpecimen(specimenId) is Specimen eluateSource)
                result.SpecialReactionNotes.AddRange(
                    EluateEvidence.Describe(_db, eluateSource, result.Suspected.Keys, transfusion));
            var datProfile = _db.GetSpecimen(specimenId)?.Dat;
            if (datProfile != null)
                result.SpecialReactionNotes.AddRange(
                    TransfusionContext.DescribeMixedFieldDat(datProfile, transfusion));
            result.ReactionPatterns = ClassifyReactionPatterns(specimenId, byRun, contexts);
            ApplyHtlaTitration(specimenId, result, reactions);
            ApplyNeutralization(specimenId, result, reactions, runs);
            var cellsByPanel = contexts.Values
                .Select(c => c.Run.PanelId)
                .Distinct()
                .ToDictionary(id => id, id => _db.GetPanelCells(id));
            result.SeparationEvaluations = EvaluateSeparation(
                suspectedStats.Where(s => s.Value.MeetsIdentificationRule).Select(s => s.Key),
                byRun, contexts, cellsByPanel, rules, specimenId, scoreExclusions);
            result.SelectedCellRecommendations = RecommendSelectedCells(reactions, result);
            EnzymeTreatmentPatternClassifier.Apply(result, byRun, contexts, cellsByPanel);
            DttTreatmentPatternClassifier.Apply(result, byRun, contexts, cellsByPanel);
            AbsorptionPatternClassifier.Apply(result, byRun, contexts, cellsByPanel);
            result.AdsorptionScoreExclusions = scoreExclusions
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();
            result.CandidateExplanations = AnalysisExplainer.Build(result);
            result.Suggestions = GenerateSuggestions(result);

            if (updateDb)
            {
                UpdateSpecimenAnalysis(specimenId, ruledOut, suspected);
                PersistSnapshot(specimenId, reactions, runs, result);
            }

            return result;
        }

        /// <summary>
        /// Evaluates the suggested multiple-antibody separation criterion for the antibodies in a
        /// final identification text (anti-E; anti-K). Empty when fewer than two known antibodies
        /// are listed, the check is off, or there are no reactions.
        /// </summary>
        public List<SeparationEvaluation> EvaluateFinalCall(string specimenId, string? finalText)
        {
            var reactions = _db.GetAllSpecimenReactions(specimenId);
            if (reactions.Count == 0) return new List<SeparationEvaluation>();
            var runs = _db.GetAllSpecimenRuns(specimenId);
            var contexts = BuildContexts(runs, specimenId);
            var byRun = GroupByRun(reactions);
            var scoreExclusions = new HashSet<string>(StringComparer.Ordinal);
            MarkPanreactiveUntreatedBesideAdsorption(byRun, contexts, scoreExclusions);
            var cellsByPanel = contexts.Values
                .Select(c => c.Run.PanelId)
                .Distinct()
                .ToDictionary(id => id, id => _db.GetPanelCells(id));
            return EvaluateSeparation(FinalAntibodyParser.Split(finalText), byRun, contexts, cellsByPanel,
                _db.GetAllRules(), specimenId, scoreExclusions);
        }

        private List<SeparationEvaluation> EvaluateSeparation(
            IEnumerable<string> called,
            Dictionary<int, List<Reaction>> byRun,
            Dictionary<int, RunContext> contexts,
            IReadOnlyDictionary<int, List<PanelCell>> cellsByPanel,
            List<Rule> rules,
            string specimenId,
            ISet<string> scoreExclusions)
        {
            var historical = new HashSet<string>(StringComparer.Ordinal);
            if (_db.GetSpecimen(specimenId) is Specimen specimen)
            {
                var transfusion = AssessTypingReliability(specimen);
                var typing = PatientTypingParser.Parse(
                    specimen.Phenotype, specimen.PreviousAntibodies, specimen.Notes, specimen.Genotype, transfusion);
                var prior = PatientHistory.ConfirmedAntibodies(PatientHistory.OtherSpecimens(_db, specimen))
                    .Select(p => p.Antibody);
                foreach (var ab in typing.HistoricalAntibodies.Concat(prior))
                    if (MultipleAntibodySeparation.AntigenOf(ab) is string ag)
                        historical.Add("anti-" + ag);
            }
            return MultipleAntibodySeparation.Evaluate(
                called, byRun, contexts, cellsByPanel, AppSettings.Current.SeparationCellCount, rules, historical,
                (ctx, antibody) => ExcludedFromOrdinaryScores(ctx, antibody, scoreExclusions));
        }

        private void PersistSnapshot(string specimenId, List<Reaction> reactions,
            List<PanelRun> runs, AnalysisResult result)
        {
            var settings = AppSettings.Current;
            var settingsJson = JsonSerializer.Serialize(settings.ClinicalSnapshot());
            var specimen = _db.GetSpecimen(specimenId);
            var cellsByPanel = runs.Select(r => r.PanelId).Distinct().ToDictionary(id => id, id => _db.GetPanelCells(id));
            var fingerprint = ComputeInputFingerprint(reactions, runs, cellsByPanel, specimen,
                specimen?.Mrn == null ? null : _db.GetPatient(specimen.Mrn));
            _db.SaveAnalysisSnapshot(
                specimenId,
                SoftwareIdentity.Version,
                settingsJson,
                fingerprint,
                JsonSerializer.Serialize(result.RuledOut),
                JsonSerializer.Serialize(result.Suspected),
                JsonSerializer.Serialize(new
                {
                    result.Acs.IsEligible,
                    result.Acs.IsEligibleWithException,
                    result.Acs.RequiredRuleoutCount,
                    result.Acs.SuggestedCombinedResult
                }),
                AnalysisRuleTrace.EngineVersion,
                AnalysisRuleTrace.Serialize(_db.GetAllRules(), settings),
                result.AnalyzedBy,
                JsonSerializer.Serialize(new AdsorptionAudit
                {
                    Conclusions = result.AbsorptionConclusions,
                    OrdinaryScoreExclusions = result.AdsorptionScoreExclusions
                }),
                JsonSerializer.Serialize(AdditionalTestingNeeds.Compute(result)));
        }

        /// <summary>
        /// SHA-256 over every input the analysis reads, so two analyses with the same fingerprint were computed from
        /// the same data: run treatments; every reaction phase, including lab-configured extra phases; the typing of
        /// every cell on each run's panel (standard and extra antigens, case kept); the specimen fields analysis uses;
        /// and the patient record's transplant and antigen-negative fields. The "fp2" marker keeps these fingerprints
        /// from ever matching the earlier, reactions-only form.
        /// </summary>
        public static string ComputeInputFingerprint(IEnumerable<Reaction> reactions, IEnumerable<PanelRun> runs,
            IReadOnlyDictionary<int, List<PanelCell>>? cellsByPanel = null, Specimen? specimen = null, Patient? patient = null)
        {
            var sb = new StringBuilder("fp2;");
            var runList = runs.OrderBy(r => r.RunId).ToList();
            foreach (var run in runList)
            {
                sb.Append(run.RunId).Append('|').Append(run.PanelId).Append('|')
                    .Append(run.CellTreatment).Append('|')
                    .Append(run.SerumTreatment).Append(';');
            }
            foreach (var rx in reactions.OrderBy(r => r.RunId).ThenBy(r => r.CellNumber, StringComparer.Ordinal))
            {
                sb.Append(rx.RunId).Append('|').Append(rx.CellNumber).Append('|')
                    .Append(rx.IS).Append('|').Append(rx.C37).Append('|')
                    .Append(rx.AHG).Append('|').Append(rx.CC);
                foreach (var (phase, grade) in rx.ExtraPhases.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
                    sb.Append('|').Append(phase).Append('=').Append(grade);
                sb.Append(';');
            }
            if (cellsByPanel != null)
            {
                foreach (var panelId in runList.Select(r => r.PanelId).Distinct().OrderBy(id => id))
                {
                    if (!cellsByPanel.TryGetValue(panelId, out var cells)) continue;
                    sb.Append("panel ").Append(panelId).Append(';');
                    foreach (var cell in cells.OrderBy(c => c.CellNumber, StringComparer.Ordinal))
                    {
                        sb.Append(cell.CellNumber).Append(':');
                        foreach (var (antigen, value) in cell.Antigens.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                            sb.Append(antigen).Append('=').Append(value).Append(',');
                        sb.Append(';');
                    }
                }
            }
            if (specimen != null)
            {
                foreach (var field in new[]
                         {
                             specimen.Type, specimen.Mrn, specimen.SourceAccession, specimen.CollectedAt,
                             specimen.DatResult, specimen.DatIgg, specimen.DatC3,
                             specimen.TransfusionStatus, specimen.LastTransfusionDate, specimen.TransfusionProducts,
                             specimen.PregnancyStatus, specimen.PregnancyEndDate, specimen.RhigDate,
                             specimen.Phenotype, specimen.Genotype, specimen.PreviousAntibodies, specimen.Notes,
                             specimen.Hemolysis,
                         })
                    sb.Append(field ?? "").Append('|');
                sb.Append(';');
            }
            if (patient != null)
                sb.Append(patient.HpcTransplant).Append('|').Append(patient.HpcTransplantDate ?? "").Append('|')
                    .Append(patient.AntigenNegative ?? "").Append('|').Append(patient.TransfusionRequirements ?? "").Append(';');

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
        }

        // ── Context helpers ───────────────────────────────────────────────────

        private Dictionary<int, RunContext> BuildContexts(List<PanelRun> runs, string specimenId)
        {
            var extrasByPanel = new Dictionary<int, List<string>>();
            var typedByPanel = new Dictionary<int, List<string>>();
            foreach (var panelId in runs.Select(r => r.PanelId).Distinct())
            {
                extrasByPanel[panelId] = _db.GetPanelExtraAntigens(panelId);
                typedByPanel[panelId] = _db.GetPanelTypedAntigens(panelId);
            }

            var dict = new Dictionary<int, RunContext>();
            foreach (var run in runs)
                dict[run.RunId] = new RunContext(run, extrasByPanel[run.PanelId], typedByPanel[run.PanelId]);
            MarkAutoAdsorptionAfterTransfusion(dict.Values, specimenId);
            return dict;
        }

        /// <summary>
        /// Autoadsorption is not reliable within the recent-transfusion window: transfused donor red cells in the
        /// patient's sample can adsorb alloantibodies along with the autoantibody, so a nonreactive autoadsorbed
        /// aliquot cannot rule any alloantibody out. Uses the same assessment that marks patient typing unreliable
        /// (red-cell transfusion within the window, or transfusion noted when the status is unknown).
        /// </summary>
        private void MarkAutoAdsorptionAfterTransfusion(IEnumerable<RunContext> contexts, string specimenId)
        {
            var auto = contexts.Where(c => c.IsAutoAdsorbed).ToList();
            if (auto.Count == 0 || _db.GetSpecimen(specimenId) is not Specimen s) return;
            if (!AssessTypingReliability(s).PhenotypeUnreliable) return;
            foreach (var ctx in auto)
                ctx.AutoAdsorptionCaution =
                    $"{RunLabel(ctx)} was autoadsorbed, but the patient was recently transfused or has an allogeneic HPC " +
                    "transplant, so donor red cells in the autologous sample may have adsorbed alloantibodies. Negative " +
                    "reactions on this aliquot cannot rule out any alloantibody; consider allogeneic (differential) " +
                    "adsorption. Not a diagnosis.";
        }

        /// <summary>
        /// Whether the patient's red cells may not be their own: the specimen's transfusion record, an allogeneic
        /// HPC transplant on or before collection, and transfusions recorded on the patient's other specimens.
        /// The one assessment used for patient typing, autoadsorption and historical antibodies.
        /// </summary>
        private TransfusionAssessment AssessTypingReliability(Specimen specimen, TransfusionHistoryFinding? historyFinding = null)
        {
            historyFinding ??= TransfusionHistoryCheck.Find(_db, specimen, AppSettings.Current.RecentTransfusionWindowDays);
            return TransfusionHistoryCheck.Apply(TransplantContext.Apply(TransfusionContext.Assess(
                specimen.TransfusionStatus, specimen.LastTransfusionDate, specimen.CreatedDate,
                AppSettings.Current.RecentTransfusionWindowDays, specimen.Phenotype, specimen.Notes, specimen.TransfusionProducts),
                _db.GetPatient(specimen.Mrn), specimen.CollectedAt ?? specimen.CreatedDate), historyFinding);
        }

        private static string RunLabel(RunContext ctx) =>
            string.IsNullOrWhiteSpace(ctx.Run.DisplayLabel) ? ctx.Run.SerumTreatment.ToString() : ctx.Run.DisplayLabel;

        private static IReadOnlyList<string> RelevantAntigens(Dictionary<int, RunContext> contexts)
        {
            var typed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var ctx in contexts.Values)
            {
                foreach (var ag in ctx.TypedAntigens)
                    typed.Add(ag);
            }
            if (typed.Count == 0)
            {
                var extras = contexts.Values.SelectMany(c => c.ExtraAntigens);
                return AntigenConstants.GetAnalyzedAntigens(extras);
            }
            var extrasOnPanels = contexts.Values.SelectMany(c => c.ExtraAntigens);
            return AntigenConstants.GetAnalyzedAntigens(extrasOnPanels)
                .Where(typed.Contains)
                .ToList();
        }

        /// <summary>
        /// Antigens on a linked panel that are NT on every cell.
        /// A saved import order is the sheet. A panel with no saved order uses
        /// the antigens stored on its cells. An antigen outside that set was
        /// not on the panel.
        /// </summary>
        private List<string> AntigensNtOnEveryCell(Dictionary<int, RunContext> contexts)
        {
            var nt = new HashSet<string>(StringComparer.Ordinal);
            var seenPanels = new HashSet<int>();
            foreach (var ctx in contexts.Values)
            {
                if (!seenPanels.Add(ctx.Run.PanelId)) continue;
                var cells = _db.GetPanelCells(ctx.Run.PanelId);
                var typed = new HashSet<string>(StringComparer.Ordinal);
                foreach (var cell in cells)
                {
                    foreach (var ag in cell.Antigens.Keys)
                    {
                        if (cell.HasTypedAntigen(ag))
                            typed.Add(ag);
                    }
                }

                var order = _db.GetPanelAntigenOrder(ctx.Run.PanelId);
                IEnumerable<string> onPanel = order.Count > 0
                    ? order.Concat(_db.GetPanelExtraAntigens(ctx.Run.PanelId))
                    : cells.SelectMany(c => c.Antigens.Keys);
                foreach (var ag in onPanel)
                {
                    if (!AntigenConstants.IsKnown(ag) || typed.Contains(ag)) continue;
                    nt.Add(ag);
                }
            }
            return nt.ToList();
        }

        private static Dictionary<int, List<Reaction>> GroupByRun(List<Reaction> reactions)
        {
            var dict = new Dictionary<int, List<Reaction>>();
            foreach (var r in reactions)
            {
                if (!dict.ContainsKey(r.RunId)) dict[r.RunId] = new();
                dict[r.RunId].Add(r);
            }
            return dict;
        }

        // ── Rule-outs ─────────────────────────────────────────────────────────

        private Dictionary<string, int> CalculateRuleouts(
            Dictionary<int, List<Reaction>> byRun,
            Dictionary<int, RunContext> contexts,
            IReadOnlyList<string> antigens,
            List<Rule> rules,
            out List<GatedRuleout> gatedRuleouts)
        {
            var ruledOut = new Dictionary<string, int>();
            gatedRuleouts = new List<GatedRuleout>();
            var gatedSeen = new HashSet<string>(); // prevent duplicate gate messages

            foreach (var (runId, runReactions) in byRun)
            {
                if (!contexts.TryGetValue(runId, out var ctx)) continue;
                var cellDict = _db.GetPanelCells(ctx.Run.PanelId).ToDictionary(c => c.CellNumber);

                foreach (var rxn in runReactions)
                {
                    if (rxn.CellNumber == "AC") continue;
                    if (!cellDict.TryGetValue(rxn.CellNumber, out var failedCcCell)) continue;
                    if (ctx.CheckCellFailed(rxn))
                    {
                        foreach (var ag in antigens)
                        {
                            if (!ctx.TypesAntigen(ag) || failedCcCell.GetAntigen(ag) != "+") continue;
                            var gated = $"anti-{ag}";
                            if (gatedSeen.Add($"{gated}|cc|{runId}|{rxn.CellNumber}"))
                                gatedRuleouts.Add(new GatedRuleout
                                {
                                    Antibody = gated,
                                    Antigen = ag,
                                    CellTreatmentLabel = ctx.Run.DisplayLabel,
                                    Reason = $"Cell {rxn.CellNumber} is AHG 0 but its check cell did not react, so the " +
                                             "result is invalid and cannot rule this antibody out. Repeat the test.",
                                });
                        }
                        continue;
                    }
                    if (!ctx.IsNegative(rxn)) continue;
                    var cell = failedCcCell;

                    foreach (var ag in antigens)
                    {
                        if (!ctx.TypesAntigen(ag)) continue;
                        if (cell.GetAntigen(ag) != "+") continue;
                        var antibody = $"anti-{ag}";

                        if (SerumRemovesAntibody(ctx, antibody))
                        {
                            var gateKey = $"{antibody}|serum|{ctx.Run.SerumTreatment}";
                            if (gatedSeen.Add(gateKey))
                                gatedRuleouts.Add(new GatedRuleout
                                {
                                    Antibody = antibody,
                                    Antigen = ag,
                                    CellTreatmentLabel =
                                        AntigenTreatmentEffects.GetDisplayName(ctx.Run.SerumTreatment),
                                    Reason = ctx.AutoAdsorptionCaution ??
                                        $"{ctx.Run.DisplayLabel} is expected to remove {antibody}; " +
                                        "a negative reaction on this aliquot cannot rule it out.",
                                });
                            continue;
                        }

                        // Check whether the treatment destroys this antigen on the cell.
                        // If so, a negative reaction is uninformative for this antibody.
                        if (!ctx.CanContributeRuleout(ag, cell))
                        {
                            var gateKey = $"{antibody}|{ctx.Run.CellTreatment}";
                            if (gatedSeen.Add(gateKey))
                                gatedRuleouts.Add(new GatedRuleout
                                {
                                    Antibody = antibody,
                                    Antigen = ag,
                                    CellTreatmentLabel =
                                        AntigenTreatmentEffects.GetDisplayName(ctx.Run.CellTreatment),
                                    Reason = $"{ag} is destroyed by {ctx.Run.CellTreatment}; " +
                                             "negative reactions cannot rule out this antibody.",
                                });
                            continue;
                        }

                        if (CanRuleOut(ag, cell, rules))
                        {
                            ruledOut.TryGetValue(antibody, out var cnt);
                            ruledOut[antibody] = cnt + 1;
                        }
                    }
                }
            }
            return ruledOut;
        }

        private Dictionary<string, List<RuleoutDetail>> GetDetailedRuleouts(
            Dictionary<int, List<Reaction>> byRun,
            Dictionary<int, RunContext> contexts,
            IReadOnlyList<string> antigens,
            List<Rule> rules,
            out Dictionary<string, List<RuleoutDetail>> dosageExcluded)
        {
            var result = new Dictionary<string, List<RuleoutDetail>>();
            // Nonreactive antigen-positive cells left out only because of dosage: heterozygous, or
            // zygosity unknown (antithetical antigen not typed), where heterozygous cells are not allowed.
            dosageExcluded = new Dictionary<string, List<RuleoutDetail>>();

            foreach (var (runId, runReactions) in byRun)
            {
                if (!contexts.TryGetValue(runId, out var ctx)) continue;
                var panel = _db.GetPanel(ctx.Run.PanelId);
                var panelName = panel?.Name ?? $"Panel {ctx.Run.PanelId}";
                var cellDict = _db.GetPanelCells(ctx.Run.PanelId).ToDictionary(c => c.CellNumber);

                foreach (var rxn in runReactions)
                {
                    if (rxn.CellNumber == "AC") continue;
                    if (!ctx.IsNegative(rxn)) continue;
                    if (!cellDict.TryGetValue(rxn.CellNumber, out var cell)) continue;

                    foreach (var ag in antigens)
                    {
                        if (!ctx.TypesAntigen(ag)) continue;
                        if (cell.GetAntigen(ag) != "+") continue;
                        var antibodyForSerum = $"anti-{ag}";
                        if (SerumRemovesAntibody(ctx, antibodyForSerum)) continue;
                        if (!ctx.CanContributeRuleout(ag, cell)) continue;
                        var target = CanRuleOut(ag, cell, rules) ? result : dosageExcluded;

                        var antibody = $"anti-{ag}";
                        var antithetical = AntigenConstants.AntitheticalPairs
                            .TryGetValue(ag, out var at) ? at : null;
                        var antitheticalTyped = antithetical != null && cell.HasTypedAntigen(antithetical);
                        var antitheticalVal = antitheticalTyped ? cell.GetAntigen(antithetical!) : null;
                        var isHomo = cell.IsHomozygousFor(ag);

                        if (!target.ContainsKey(antibody)) target[antibody] = new();
                        target[antibody].Add(new RuleoutDetail
                        {
                            RunId = runId,
                            RunLabel = ctx.Run.DisplayLabel,
                            PanelId = ctx.Run.PanelId,
                            PanelName = panelName,
                            CellNumber = rxn.CellNumber,
                            Antigen = ag,
                            AntigenValue = "+",
                            Antithetical = antithetical,
                            AntitheticalValue = antitheticalVal,
                            IsHomozygous = isHomo,
                            IS = rxn.IS,
                            C37 = rxn.C37,
                            AHG = rxn.AHG,
                            CC = rxn.CC,
                        });
                    }
                }
            }
            return result;
        }

        private static bool SerumRemovesAntibody(RunContext ctx, string antibody) =>
            ctx.AutoAdsorptionCaution != null ||
            ctx.AbsorbedAntibodies.Any(a => RuleoutPolicy.SameAntibody(a, antibody));

        /// <summary>
        /// An adsorbed aliquot is not ordinary panel evidence for specificities
        /// that aliquot is expected to remove. Removal and survival stay on the
        /// absorption comparison.
        /// </summary>
        private bool ExcludedFromOrdinaryScores(
            RunContext ctx, string antibody, ISet<string> exclusions)
        {
            if (_panreactiveUntreatedRunIds.Contains(ctx.Run.RunId))
                return true;
            if (!SerumRemovesAntibody(ctx, antibody)) return false;
            if (ctx.AutoAdsorptionCaution != null)
            {
                exclusions.Add(ctx.AutoAdsorptionCaution);
                return true;
            }
            var label = RunLabel(ctx);
            exclusions.Add(
                $"{label} is expected to remove {antibody}; reactions on this aliquot were " +
                "excluded from ordinary probability and pattern scores. Review the absorption " +
                "comparison for removal or survival. Not a diagnosis.");
            return true;
        }

        /// <summary>
        /// A panreactive untreated run paired with autoadsorption or allogeneic
        /// adsorption is not ordinary alloantibody evidence. The aliquot stays
        /// in the scores except for specificities that aliquot is expected to remove.
        /// </summary>
        private void MarkPanreactiveUntreatedBesideAdsorption(
            Dictionary<int, List<Reaction>> byRun,
            Dictionary<int, RunContext> contexts,
            ISet<string> exclusions)
        {
            _panreactiveUntreatedRunIds.Clear();
            var adsorbed = contexts.Values.Where(IsAdsorptionRun).ToList();
            if (adsorbed.Count == 0) return;

            foreach (var untreated in contexts.Values.Where(c => c.Run.SerumTreatment == SerumTreatment.None))
            {
                var paired = adsorbed.Where(a =>
                    a.Run.PanelId == untreated.Run.PanelId
                    && a.Run.CellTreatment == untreated.Run.CellTreatment
                    && a.Run.RunId != untreated.Run.RunId
                    && byRun.ContainsKey(a.Run.RunId)).ToList();
                if (paired.Count == 0) continue;
                if (!byRun.TryGetValue(untreated.Run.RunId, out var reactions)) continue;
                if (!IsPanreactive(untreated, reactions, out var reactive, out var evaluated)) continue;

                _panreactiveUntreatedRunIds.Add(untreated.Run.RunId);
                AddPanreactiveExclusion(exclusions, untreated, paired.Where(a => a.IsAutoAdsorbed).ToList(), reactive, evaluated, auto: true);
                AddPanreactiveExclusion(exclusions, untreated, paired.Where(a => !a.IsAutoAdsorbed).ToList(), reactive, evaluated, auto: false);
            }
        }

        private static bool IsAdsorptionRun(RunContext ctx) =>
            ctx.IsAutoAdsorbed || ctx.AbsorbedAntibodies.Count > 0;

        private static void AddPanreactiveExclusion(
            ISet<string> exclusions,
            RunContext untreated,
            List<RunContext> paired,
            int reactive,
            int evaluated,
            bool auto)
        {
            if (paired.Count == 0) return;
            var labels = string.Join(", ", paired.Select(a => a.Run.DisplayLabel).Distinct(StringComparer.Ordinal));
            var guidance = auto
                ? paired.Any(a => a.AutoAdsorptionCaution != null)
                    ? "The patient was recently transfused, so the autoadsorbed aliquot is not used for alloantibody " +
                      "evidence either; consider allogeneic (differential) adsorption."
                    : "Use the autoadsorbed aliquot for underlying alloantibody evidence."
                : "Use the adsorbed aliquot for specificities it is not expected to remove. " +
                  "Removal or survival of the expected specificities stays on the absorption comparison.";
            exclusions.Add(
                $"{untreated.Run.DisplayLabel} is panreactive ({reactive} of {evaluated} evaluated cells) " +
                $"and is paired with {labels}. Reactions on this untreated run were excluded from " +
                $"ordinary probability and pattern scores. {guidance} Not a diagnosis.");
        }

        private static bool IsPanreactive(
            RunContext ctx, List<Reaction> reactions, out int reactive, out int evaluated)
        {
            reactive = 0;
            evaluated = 0;
            foreach (var rxn in reactions)
            {
                if (string.Equals(rxn.CellNumber, "AC", StringComparison.OrdinalIgnoreCase)) continue;
                var positive = ctx.IsPositive(rxn);
                if (!positive && !ctx.IsNegative(rxn)) continue;
                evaluated++;
                if (positive) reactive++;
            }
            return evaluated >= 4 && reactive * 5 >= evaluated * 4;
        }

        private bool CanRuleOut(string antigen, PanelCell cell, List<Rule> rules)
        {
            if (cell.GetAntigen(antigen) != "+") return false;
            if (!AntigenConstants.AntitheticalPairs.ContainsKey(antigen))
                return true;
            if (cell.IsHomozygousFor(antigen)) return true;
            return RuleAllowsHeterozygous(antigen, rules);
        }

        private static bool RuleAllowsHeterozygous(string antigen, List<Rule> rules) =>
            RuleoutPolicy.HeterozygousAllowed(antigen, rules);

        private List<RuleoutEvaluation> BuildRuleoutEvaluations(
            Dictionary<string, int> observed,
            Dictionary<string, List<RuleoutDetail>> detailed,
            Dictionary<string, List<RuleoutDetail>> dosageExcluded,
            List<Rule> rules,
            Dictionary<int, List<Reaction>> byRun,
            Dictionary<int, RunContext> contexts,
            IReadOnlyList<string> antigens,
            ISet<string> scoreExclusions)
        {
            var labDefault = AppSettings.Current.DefaultMinRuleoutCount;
            var conflicts = CollectConflictingReactiveCells(byRun, contexts, antigens, scoreExclusions);
            var list = new List<RuleoutEvaluation>();

            // Antibodies with no qualifying cell but dosage-excluded ones still get an evaluation, so the
            // reason they are not ruled out is stated rather than silent.
            var all = observed.Keys.Union(dosageExcluded.Keys, StringComparer.Ordinal)
                .Select(ab => (Antibody: ab, Count: observed.TryGetValue(ab, out var n) ? n : 0));
            foreach (var (antibody, count) in all.OrderBy(x => x.Antibody))
            {
                var antigen = antibody.StartsWith("anti-", StringComparison.OrdinalIgnoreCase)
                    ? antibody[5..]
                    : antibody;
                detailed.TryGetValue(antibody, out var cells);
                cells ??= new List<RuleoutDetail>();
                var rule = RuleoutPolicy.FindAntibodyRule(antibody, rules);
                var required = RuleoutPolicy.ResolveRequiredCount(antibody, rules, labDefault);
                conflicts.TryGetValue(antibody, out var conflicting);

                var evaluation = new RuleoutEvaluation
                {
                    Antibody = antibody,
                    Antigen = antigen,
                    ObservedCount = count,
                    RequiredCount = required,
                    HomozygousCount = cells.Count(c => c.IsHomozygous),
                    HeterozygousCount = cells.Count(c => !c.IsHomozygous),
                    HeterozygousAllowed = RuleoutPolicy.HeterozygousAllowed(antigen, rules),
                    MeetsCriteria = count >= required,
                    PolicySource = rule != null ? "antibody rule" : "lab default",
                    Cells = cells,
                    DosageExcludedCells = dosageExcluded.TryGetValue(antibody, out var excluded) ? excluded : new List<RuleoutDetail>(),
                    ConflictingReactiveCells = conflicting ?? new List<ReactiveConflictCell>(),
                };
                evaluation.Explanation = RuleoutPolicy.FormatExplanation(evaluation);
                list.Add(evaluation);
            }

            return list;
        }

        private Dictionary<string, List<ReactiveConflictCell>> CollectConflictingReactiveCells(
            Dictionary<int, List<Reaction>> byRun,
            Dictionary<int, RunContext> contexts,
            IReadOnlyList<string> antigens,
            ISet<string> scoreExclusions)
        {
            // Ordinal: anti-K and anti-k are different antibodies with different conflicting cells.
            var result = new Dictionary<string, List<ReactiveConflictCell>>(StringComparer.Ordinal);
            var seen = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var panelNames = new Dictionary<int, string>();
            foreach (var (runId, runReactions) in byRun)
            {
                if (!contexts.TryGetValue(runId, out var ctx)) continue;
                if (!panelNames.TryGetValue(ctx.Run.PanelId, out var panelName))
                {
                    panelName = _db.GetPanel(ctx.Run.PanelId)?.Name ?? $"Panel {ctx.Run.PanelId}";
                    panelNames[ctx.Run.PanelId] = panelName;
                }
                var cellDict = _db.GetPanelCells(ctx.Run.PanelId).ToDictionary(c => c.CellNumber);
                foreach (var rxn in runReactions)
                {
                    if (rxn.CellNumber == "AC") continue;
                    if (!ctx.IsPositive(rxn)) continue;
                    if (!cellDict.TryGetValue(rxn.CellNumber, out var cell)) continue;
                    foreach (var ag in antigens)
                    {
                        if (!ctx.TypesAntigen(ag)) continue;
                        if (cell.GetAntigen(ag) != "+") continue;
                        var antibody = $"anti-{ag}";
                        if (ExcludedFromOrdinaryScores(ctx, antibody, scoreExclusions)) continue;
                        if (!seen.TryGetValue(antibody, out var keys))
                        {
                            keys = new HashSet<string>(StringComparer.Ordinal);
                            seen[antibody] = keys;
                        }
                        if (!keys.Add($"{runId}:{rxn.CellNumber}")) continue;
                        if (!result.TryGetValue(antibody, out var cells))
                        {
                            cells = new List<ReactiveConflictCell>();
                            result[antibody] = cells;
                        }
                        var (phase, value) = ctx.GetStrongestPhase(rxn);
                        cells.Add(new ReactiveConflictCell
                        {
                            PanelName = panelName,
                            CellNumber = rxn.CellNumber,
                            RunLabel = ctx.Run.DisplayLabel,
                            StrongestPhase = phase,
                            StrongestValue = value,
                            CC = rxn.CC,
                        });
                    }
                }
            }
            return result;
        }

        // ── Probabilities (Fisher's Exact Test) ───────────────────────────────

        private (Dictionary<string, double>, Dictionary<string, SuspectedStatistics>, Dictionary<string, double>)
            CalculateProbabilities(
                Dictionary<int, List<Reaction>> byRun,
                Dictionary<int, RunContext> contexts,
                IReadOnlyList<string> antigens,
                Dictionary<string, int> ruledOut,
                ISet<string> scoreExclusions)
        {
            var suspected = new Dictionary<string, double>();
            var stats = new Dictionary<string, SuspectedStatistics>();
            var allScores = new Dictionary<string, double>();

            foreach (var ag in antigens)
            {
                var antibody = $"anti-{ag}";
                double posWithAg = 0, posWithoutAg = 0, negWithAg = 0, negWithoutAg = 0;
                var posAgPosCells = new HashSet<(int PanelId, string CellNumber)>();
                var negAgNegCells = new HashSet<(int PanelId, string CellNumber)>();

                foreach (var (runId, runReactions) in byRun)
                {
                    if (!contexts.TryGetValue(runId, out var ctx)) continue;
                    if (!ctx.TypesAntigen(ag)) continue;
                    // Skip this run for this antigen if the treatment destroys it
                    if (AntigenTreatmentEffects.GetCellEffect(ctx.Run.CellTreatment, ag)
                            == AntigenEffect.Destroyed)
                        continue;
                    if (ExcludedFromOrdinaryScores(ctx, antibody, scoreExclusions))
                        continue;

                    var weight = ctx.EvidenceWeight;
                    var cellDict = _db.GetPanelCells(ctx.Run.PanelId).ToDictionary(c => c.CellNumber);

                    foreach (var rxn in runReactions)
                    {
                        if (rxn.CellNumber == "AC") continue;
                        if (!cellDict.TryGetValue(rxn.CellNumber, out var cell)) continue;
                        if (!cell.HasTypedAntigen(ag)) continue;
                        bool agPresent = ctx.IsAntigenPresent(cell, ag);
                        bool isPos = ctx.IsPositive(rxn);
                        if (isPos && agPresent)
                        {
                            posWithAg += weight;
                            posAgPosCells.Add((ctx.Run.PanelId, rxn.CellNumber));
                        }
                        else if (isPos && !agPresent) posWithoutAg += weight;
                        else if (!isPos && agPresent) negWithAg += weight;
                        else negWithoutAg += weight;

                        if (!isPos && !agPresent && ctx.IsNegative(rxn))
                            negAgNegCells.Add((ctx.Run.PanelId, rxn.CellNumber));
                    }
                }

                double total = posWithAg + posWithoutAg + negWithAg + negWithoutAg;
                if (total == 0 || posWithAg == 0) continue;

                try
                {
                    var pvalue = FisherExactOneSided(
                        (int)Math.Round(posWithAg), (int)Math.Round(posWithoutAg),
                        (int)Math.Round(negWithAg), (int)Math.Round(negWithoutAg));
                    double fisherComp = pvalue < 0.5 ? 1 - pvalue : 0.0;
                    double patternScore = (posWithAg + posWithoutAg) > 0
                        ? posWithAg / (posWithAg + posWithoutAg) : 0;
                    double combined = posWithAg > 0
                        ? (fisherComp + patternScore) / 2 : fisherComp;
                    allScores[antibody] = Math.Round(combined, 3);

                    if (combined <= AppSettings.Current.ProbabilityThreshold) continue;

                    bool include;
                    if (ruledOut.TryGetValue(antibody, out var ruleoutCnt))
                        include = combined > 0.95 && posWithoutAg == 0 && ruleoutCnt <= 2 && posWithAg >= 3;
                    else
                        include = true;

                    if (!include) continue;
                    double rounded = Math.Round(combined, 3);
                    int required = AppSettings.Current.IdentificationCellCount;
                    if (required < 1 || required > 3) required = 3;
                    int posCount = posAgPosCells.Count;
                    int negCount = negAgNegCells.Count;
                    suspected[antibody] = rounded;
                    stats[antibody] = new SuspectedStatistics
                    {
                        FisherPValue = Math.Round(pvalue, 4),
                        PatternScore = Math.Round(patternScore, 4),
                        FisherComponent = Math.Round(fisherComp, 4),
                        CombinedScore = rounded,
                        PositiveAgPositiveCount = posCount,
                        NegativeAgNegativeCount = negCount,
                        IdentificationRequired = required,
                        MeetsIdentificationRule = posCount >= required && negCount >= required
                    };
                }
                catch { /* skip */ }
            }
            return (suspected, stats, allScores);
        }

        // ── Pattern matching ──────────────────────────────────────────────────

        private List<PatternMatch> PatternMatching(
            Dictionary<int, List<Reaction>> byRun,
            Dictionary<int, RunContext> contexts,
            IReadOnlyList<string> antigens,
            Dictionary<string, int> ruledOut,
            ISet<string> scoreExclusions)
        {
            var patterns = new List<PatternMatch>();

            foreach (var ag in antigens)
            {
                var antibody = $"anti-{ag}";
                if (ruledOut.ContainsKey(antibody)) continue;

                int matches = 0, mismatches = 0;
                foreach (var (runId, runReactions) in byRun)
                {
                    if (!contexts.TryGetValue(runId, out var ctx)) continue;
                    if (!ctx.TypesAntigen(ag)) continue;
                    if (AntigenTreatmentEffects.GetCellEffect(ctx.Run.CellTreatment, ag)
                            == AntigenEffect.Destroyed)
                        continue;
                    if (ExcludedFromOrdinaryScores(ctx, antibody, scoreExclusions))
                        continue;

                    var cellDict = _db.GetPanelCells(ctx.Run.PanelId).ToDictionary(c => c.CellNumber);
                    foreach (var rxn in runReactions)
                    {
                        if (rxn.CellNumber == "AC") continue;
                        if (!cellDict.TryGetValue(rxn.CellNumber, out var cell)) continue;
                        if (!cell.HasTypedAntigen(ag)) continue;
                        if (!ctx.IsPositive(rxn)) continue;
                        if (ctx.IsAntigenPresent(cell, ag)) matches++; else mismatches++;
                    }
                }
                if (matches <= 0) continue;
                double confidence = (matches + mismatches) > 0
                    ? Math.Round((double)matches / (matches + mismatches), 3) : 0;
                patterns.Add(new PatternMatch
                {
                    Antibody = antibody,
                    Matches = matches,
                    Mismatches = mismatches,
                    Confidence = confidence
                });
            }
            patterns.Sort((a, b) => b.Confidence.CompareTo(a.Confidence));
            return patterns;
        }

        // ── Suspected evidence ────────────────────────────────────────────────

        private Dictionary<string, SuspectedEvidence> GetSuspectedAntibodyEvidence(
            Dictionary<int, List<Reaction>> byRun,
            Dictionary<int, RunContext> contexts,
            Dictionary<string, double> suspected,
            ISet<string> scoreExclusions)
        {
            var evidence = new Dictionary<string, SuspectedEvidence>();

            foreach (var (antibody, probability) in suspected)
            {
                var ag = antibody.Replace("anti-", "");
                if (!AntigenConstants.IsKnown(ag)) continue;

                var supporting = new List<EvidenceCell>();
                var conflicting = new List<EvidenceCell>();

                foreach (var (runId, runReactions) in byRun)
                {
                    if (!contexts.TryGetValue(runId, out var ctx)) continue;
                    if (!ctx.TypesAntigen(ag)) continue;
                    if (AntigenTreatmentEffects.GetCellEffect(ctx.Run.CellTreatment, ag)
                            == AntigenEffect.Destroyed)
                        continue;
                    if (ExcludedFromOrdinaryScores(ctx, antibody, scoreExclusions))
                        continue;

                    var panel = _db.GetPanel(ctx.Run.PanelId);
                    var panelName = panel?.Name ?? $"Panel {ctx.Run.PanelId}";
                    var cellDict = _db.GetPanelCells(ctx.Run.PanelId).ToDictionary(c => c.CellNumber);

                    foreach (var rxn in runReactions)
                    {
                        if (rxn.CellNumber == "AC") continue;
                        if (!cellDict.TryGetValue(rxn.CellNumber, out var cell)) continue;
                        if (!cell.HasTypedAntigen(ag)) continue;
                        bool agPresent = ctx.IsAntigenPresent(cell, ag);
                        bool isPos = ctx.IsPositive(rxn);
                        if (!isPos) continue;

                        var (sp, sv) = ctx.GetStrongestPhase(rxn);
                        var ec = new EvidenceCell
                        {
                            RunId = runId,
                            RunLabel = ctx.Run.DisplayLabel,
                            PanelId = ctx.Run.PanelId,
                            PanelName = panelName,
                            CellNumber = rxn.CellNumber,
                            IS = rxn.IS,
                            C37 = rxn.C37,
                            AHG = rxn.AHG,
                            CC = rxn.CC,
                            StrongestPhase = sp,
                            StrongestValue = sv,
                        };
                        if (agPresent) supporting.Add(ec); else conflicting.Add(ec);
                    }
                }
                int totalPos = supporting.Count + conflicting.Count;
                evidence[antibody] = new SuspectedEvidence
                {
                    Probability = probability,
                    SupportingCells = supporting,
                    ConflictingCells = conflicting,
                    PatternQuality = totalPos > 0
                        ? Math.Round((double)supporting.Count / totalPos, 3) : 0,
                    TotalSupporting = supporting.Count,
                    TotalConflicting = conflicting.Count
                };
            }
            return evidence;
        }

        // ── Antibody combinations ─────────────────────────────────────────────

        private List<AntibodyCombination> DetectAntibodyCombinations(
            Dictionary<int, List<Reaction>> byRun,
            Dictionary<int, RunContext> contexts,
            Dictionary<string, double> suspected,
            ISet<string> scoreExclusions)
        {
            var combos = new List<AntibodyCombination>();
            if (suspected.Count < 2) return combos;

            var top = suspected.OrderByDescending(x => x.Value).Take(5).ToList();

            for (int i = 0; i < top.Count; i++)
            {
                for (int j = i + 1; j < top.Count; j++)
                {
                    var (ab1, p1) = top[i];
                    var (ab2, p2) = top[j];
                    var ag1 = ab1.Replace("anti-", "");
                    var ag2 = ab2.Replace("anti-", "");
                    if (!AntigenConstants.IsKnown(ag1) ||
                        !AntigenConstants.IsKnown(ag2)) continue;

                    int both = 0, ab1only = 0, ab2only = 0, neither = 0;
                    foreach (var (runId, runReactions) in byRun)
                    {
                        if (!contexts.TryGetValue(runId, out var ctx)) continue;
                        if (!ctx.TypesAntigen(ag1) || !ctx.TypesAntigen(ag2)) continue;
                        if (ExcludedFromOrdinaryScores(ctx, ab1, scoreExclusions)
                            || ExcludedFromOrdinaryScores(ctx, ab2, scoreExclusions))
                            continue;
                        var cellDict = _db.GetPanelCells(ctx.Run.PanelId).ToDictionary(c => c.CellNumber);
                        foreach (var rxn in runReactions)
                        {
                            if (rxn.CellNumber == "AC") continue;
                            if (!cellDict.TryGetValue(rxn.CellNumber, out var cell)) continue;
                            if (!ctx.IsPositive(rxn)) continue;
                            bool h1 = ctx.IsAntigenPresent(cell, ag1);
                            bool h2 = ctx.IsAntigenPresent(cell, ag2);
                            if (h1 && h2) both++;
                            else if (h1) ab1only++;
                            else if (h2) ab2only++;
                            else neither++;
                        }
                    }
                    int totalPos = both + ab1only + ab2only + neither;
                    if (totalPos <= 0 || both == 0) continue;
                    double score = Math.Round((double)(both + ab1only + ab2only) / totalPos, 3);
                    if (score > 0.5)
                        combos.Add(new AntibodyCombination
                        {
                            Antibodies = new() { ab1, ab2 },
                            Probabilities = new() { p1, p2 },
                            BothSupport = both,
                            Ab1Only = ab1only,
                            Ab2Only = ab2only,
                            Neither = neither,
                            CombinationScore = score
                        });
                }
            }
            combos.Sort((a, b) => b.CombinationScore.CompareTo(a.CombinationScore));
            return combos;
        }

        // ── Phase-specific probabilities ──────────────────────────────────────

        private Dictionary<string, Dictionary<string, double>> CalculatePhaseSpecificProbabilities(
            Dictionary<int, List<Reaction>> byRun,
            Dictionary<int, RunContext> contexts,
            IReadOnlyList<string> antigens,
            Dictionary<string, int> ruledOut,
            ISet<string> scoreExclusions)
        {
            var result = new Dictionary<string, Dictionary<string, double>>();
            // CC is a check-cell control, not an antibody reactivity phase
            var phases = ExtraPhaseParser.AllInterpretableNames(AppSettings.Current.ExtraPhases);

            foreach (var phase in phases)
            {
                result[phase] = new();
                foreach (var ag in antigens)
                {
                    var antibody = $"anti-{ag}";
                    double posWithAg = 0, posWithoutAg = 0, negWithAg = 0, negWithoutAg = 0;

                    foreach (var (runId, runReactions) in byRun)
                    {
                        if (!contexts.TryGetValue(runId, out var ctx)) continue;
                        if (!ctx.IsPhaseInterpretable(phase)) continue;
                        if (!ctx.TypesAntigen(ag)) continue;
                        if (AntigenTreatmentEffects.GetCellEffect(ctx.Run.CellTreatment, ag)
                                == AntigenEffect.Destroyed)
                            continue;
                        if (ExcludedFromOrdinaryScores(ctx, antibody, scoreExclusions))
                            continue;

                        var cellDict = _db.GetPanelCells(ctx.Run.PanelId).ToDictionary(c => c.CellNumber);
                        foreach (var rxn in runReactions)
                        {
                            if (rxn.CellNumber == "AC") continue;
                            if (!cellDict.TryGetValue(rxn.CellNumber, out var cell)) continue;
                            if (!cell.HasTypedAntigen(ag)) continue;
                            bool agPresent = ctx.IsAntigenPresent(cell, ag);
                            var phaseVal = ctx.GetInterpretedPhaseValue(rxn, phase);
                            bool phasePos = ReactionGrade.IsPositive(phaseVal);
                            if (phasePos && agPresent) posWithAg++;
                            else if (phasePos && !agPresent) posWithoutAg++;
                            else if (!phasePos && agPresent) negWithAg++;
                            else negWithoutAg++;
                        }
                    }

                    double total = posWithAg + posWithoutAg + negWithAg + negWithoutAg;
                    if (total == 0 || posWithAg == 0) continue;

                    try
                    {
                        var pvalue = FisherExactOneSided(
                            (int)posWithAg, (int)posWithoutAg,
                            (int)negWithAg, (int)negWithoutAg);
                        double prob = pvalue < 0.5 ? 1 - pvalue : 0.0;
                        if (posWithAg > 0 && (posWithAg + posWithoutAg) > 0)
                        {
                            double ps = posWithAg / (posWithAg + posWithoutAg);
                            prob = (prob + ps) / 2;
                        }
                        if (prob > 0.3 && posWithAg > 0)
                            result[phase][antibody] = Math.Round(prob, 3);
                    }
                    catch { }
                }
            }
            return result;
        }

        // ── Dosage effects ────────────────────────────────────────────────────

        private List<DosageEffect> DetectDosageEffects(
            Dictionary<int, List<Reaction>> byRun,
            Dictionary<int, RunContext> contexts,
            Dictionary<string, double> suspected,
            ISet<string> scoreExclusions)
        {
            var warnings = new List<DosageEffect>();

            foreach (var (antibody, prob) in suspected)
            {
                if (prob < 0.6) continue;
                var ag = antibody.Replace("anti-", "");
                if (!AntigenConstants.AntitheticalPairs.TryGetValue(ag, out var antithetical)) continue;

                var homoRxns = new List<double>();
                var hetRxns = new List<double>();

                foreach (var (runId, runReactions) in byRun)
                {
                    if (!contexts.TryGetValue(runId, out var ctx)) continue;
                    if (!ctx.TypesAntigen(ag)) continue;
                    // Use only untreated (or enhanced) runs for dosage analysis
                    if (AntigenTreatmentEffects.GetCellEffect(ctx.Run.CellTreatment, ag)
                            == AntigenEffect.Destroyed)
                        continue;
                    if (ExcludedFromOrdinaryScores(ctx, antibody, scoreExclusions))
                        continue;

                    var cellDict = _db.GetPanelCells(ctx.Run.PanelId).ToDictionary(c => c.CellNumber);
                    foreach (var rxn in runReactions)
                    {
                        if (rxn.CellNumber == "AC") continue;
                        if (!cellDict.TryGetValue(rxn.CellNumber, out var cell)) continue;
                        bool isHomo = cell.IsHomozygousFor(ag);
                        bool isHet = cell.GetAntigen(ag) == "+" &&
                                     cell.HasTypedAntigen(antithetical) &&
                                     cell.GetAntigen(antithetical) == "+";
                        if (!isHomo && !isHet) continue;
                        var (_, sv) = ctx.GetStrongestPhase(rxn);
                        double str = RunContext.ReactionToNumeric(sv);
                        if (isHomo) homoRxns.Add(str); else hetRxns.Add(str);
                    }
                }

                if (homoRxns.Count > 0 && hetRxns.Count > 0)
                {
                    double avgH = homoRxns.Average(), avgT = hetRxns.Average();
                    if (avgH > avgT + 0.5)
                        warnings.Add(new DosageEffect
                        {
                            Antibody = antibody,
                            Antigen = ag,
                            AvgHomozygous = Math.Round(avgH, 2),
                            AvgHeterozygous = Math.Round(avgT, 2),
                            HomozygousCount = homoRxns.Count,
                            HeterozygousCount = hetRxns.Count,
                            Severity = (avgH - avgT) > 1.0 ? "high" : "medium"
                        });
                }
            }
            return warnings;
        }

        // ── Treatment inferences ──────────────────────────────────────────────

        private List<TreatmentInference> BuildTreatmentInferences(
            Dictionary<int, List<Reaction>> byRun,
            Dictionary<int, RunContext> contexts,
            Dictionary<string, double> suspected)
        {
            var inferences = new List<TreatmentInference>();

            // Find untreated runs for comparison
            var untreatedRuns = contexts.Values
                .Where(c => c.Run.IsUntreated)
                .ToList();
            var treatedRuns = contexts.Values
                .Where(c => !c.Run.IsUntreated)
                .ToList();

            if (!untreatedRuns.Any() || !treatedRuns.Any()) return inferences;

            foreach (var treatedCtx in treatedRuns)
            {
                if (!byRun.TryGetValue(treatedCtx.Run.RunId, out var treatedRxns)) continue;

                // Find the untreated run for the same panel
                var untreatedCtx = untreatedRuns
                    .FirstOrDefault(c => c.Run.PanelId == treatedCtx.Run.PanelId);
                if (untreatedCtx == null) continue;
                if (!byRun.TryGetValue(untreatedCtx.Run.RunId, out var untreatedRxns)) continue;

                var treatedByCell = treatedRxns.ToDictionary(r => r.CellNumber);
                var untreatedByCell = untreatedRxns.ToDictionary(r => r.CellNumber);

                foreach (var (cellNum, treatedRxn) in treatedByCell)
                {
                    if (cellNum == "AC") continue;
                    if (!untreatedByCell.TryGetValue(cellNum, out var untreatedRxn)) continue;

                    bool treatedPos = treatedCtx.IsPositive(treatedRxn);
                    bool untreatedPos = untreatedCtx.IsPositive(untreatedRxn);

                    if (untreatedPos && !treatedPos)
                    {
                        // Reactivity lost on treated cells
                        var type = treatedCtx.Run.CellTreatment switch
                        {
                            CellTreatment.DTT    => TreatmentInferenceType.ReactivityLostOnDTT,
                            CellTreatment.Ficin  => TreatmentInferenceType.ReactivityLostOnEnzyme,
                            CellTreatment.Papain => TreatmentInferenceType.ReactivityLostOnEnzyme,
                            _                    => TreatmentInferenceType.ReactivityLostOnEnzyme,
                        };
                        // Which suspected antibodies does this cell support?
                        var cellDict = _db.GetPanelCells(treatedCtx.Run.PanelId)
                            .ToDictionary(c => c.CellNumber);
                        if (!cellDict.TryGetValue(cellNum, out var cell)) continue;

                        foreach (var (ab, _) in suspected)
                        {
                            var ag = ab.Replace("anti-", "");
                            var effect = AntigenTreatmentEffects.GetCellEffect(
                                treatedCtx.Run.CellTreatment, ag);
                            if (effect == AntigenEffect.Destroyed && cell.GetAntigen(ag) == "+")
                            {
                                inferences.Add(new TreatmentInference
                                {
                                    RunLabel = treatedCtx.Run.DisplayLabel,
                                    Antibody = ab,
                                    InferenceType = type,
                                    Observation =
                                        $"Cell {cellNum}: reactive untreated, non-reactive on " +
                                        $"{treatedCtx.Run.DisplayLabel} — consistent with {ab} " +
                                        $"(antigen {ag} destroyed by {treatedCtx.Run.CellTreatment}).",
                                });
                            }
                        }
                    }
                }
            }

            return inferences.DistinctBy(i => i.Observation).ToList();
        }

        // ── Suggestions ───────────────────────────────────────────────────────

        public static AcsEvaluation EvaluateAcs(
            Dictionary<string, int> ruledOut,
            Dictionary<string, double> allCombinedScores)
        {
            var required = AppSettings.Current.AcsRuleoutCount;
            if (required < 1 || required > 5) required = 3;

            var acsAntigens = AppSettings.Current.AcsAntigenList();
            var acs = new AcsEvaluation { RequiredRuleoutCount = required, Antigens = acsAntigens.ToList() };
            foreach (var ag in acsAntigens)
            {
                var antibody = $"anti-{ag}";
                ruledOut.TryGetValue(antibody, out var count);
                if (count < required)
                    acs.Shortfalls.Add(new AcsShortfall
                    {
                        Antibody = antibody,
                        Count = count,
                        Required = required
                    });
            }

            foreach (var (antibody, score) in allCombinedScores.OrderByDescending(x => x.Value))
            {
                if (score < AntigenConstants.AcsProbabilityCutoff) continue;
                ruledOut.TryGetValue(antibody, out var count);
                acs.Exceptions.Add(new AcsExceptionAntibody
                {
                    Antibody = antibody,
                    CombinedScore = score,
                    RuleoutCount = count
                });
            }

            var allCsRuledOut = acs.Shortfalls.Count == 0;
            acs.IsEligible = allCsRuledOut && acs.Exceptions.Count == 0;
            acs.IsEligibleWithException = allCsRuledOut && acs.Exceptions.Count > 0;

            if (allCsRuledOut)
            {
                var parts = new List<string> { AntigenConstants.AcsResultText };
                foreach (var ex in acs.Exceptions)
                    parts.Add(ex.Antibody);
                acs.SuggestedCombinedResult = string.Join("; ", parts);
                acs.SuggestedComment = string.Join("; ",
                    acs.Exceptions.Select(ex =>
                        $"{ex.Antibody} ruled out {ex.RuleoutCount} time{(ex.RuleoutCount == 1 ? "" : "s")}"));
            }

            return acs;
        }

        private static void AddUntypeableSuggestion(
            List<string> informational,
            AnalysisResult result,
            bool notTestedOnEveryCell,
            string reason)
        {
            var names = result.UntypeableSpecificities
                .Where(u => !u.IsClinicallySignificant && u.NotTestedOnEveryCell == notTestedOnEveryCell)
                .Select(u => u.Antibody)
                .ToList();
            if (names.Count == 0) return;
            var shown = string.Join(", ", names.Take(8));
            var more = names.Count > 8 ? $" (+{names.Count - 8} more)" : "";
            informational.Add($"Current panels also cannot type {shown}{more} {reason}");
        }

        private List<string> GenerateSuggestions(AnalysisResult result)
        {
            var critical = new List<string>();
            var important = new List<string>();
            var informational = new List<string>();

            if (result.Acs.IsEligible)
            {
                informational.Add(
                    $"All clinically significant antibodies are ruled out the required " +
                    $"{result.Acs.RequiredRuleoutCount} times. Suggested result: {AntigenConstants.AcsResultText}.");
            }
            else if (result.Acs.IsEligibleWithException)
            {
                var names = string.Join(", ", result.Acs.Exceptions.Select(e => e.Antibody));
                important.Add(
                    $"{names}: {AntigenConstants.AcsExceptionCriterion} (from the Fisher test and the reaction pattern; " +
                    $"a score, not a probability). You may result ACS and {names} " +
                    $"({result.Acs.SuggestedComment}).");
            }
            else if (result.Acs.Shortfalls.Count > 0)
            {
                var list = string.Join(", ",
                    result.Acs.Shortfalls.Select(s => $"{s.Antibody} ({s.Count}/{s.Required})"));
                informational.Add($"ACS not met: {list}.");
            }

            foreach (var ab in result.RuledOut.Keys)
                if (result.Suspected.ContainsKey(ab))
                    critical.Add($"WARNING: {ab} is both ruled out and suspected. " +
                        "This may indicate weak antigen expression or testing issues.");

            foreach (var ev in result.RuleoutEvaluations.Where(e => !e.MeetsCriteria))
                informational.Add(ev.Explanation);

            if (result.UntypedClinicallySignificant.Count > 0)
            {
                var shown = string.Join(", ", result.UntypedClinicallySignificant.Take(8));
                var more = result.UntypedClinicallySignificant.Count > 8
                    ? $" (+{result.UntypedClinicallySignificant.Count - 8} more)" : "";
                var ntOnPanel = new HashSet<string>(
                    result.UntypeableSpecificities
                        .Where(u => u.NotTestedOnEveryCell)
                        .Select(u => u.Antigen),
                    StringComparer.Ordinal);
                var reason = result.UntypedClinicallySignificant.All(ntOnPanel.Contains)
                    ? "because that antigen is NT on every cell."
                    : "because the antigen was not tested.";
                informational.Add(
                    $"This panel does not type {shown}{more}. Those antibodies cannot be " +
                    $"ruled out or supported from these cells {reason}");
            }

            AddUntypeableSuggestion(informational, result, notTestedOnEveryCell: true,
                "because that antigen is NT on every cell. This is not a rule-out.");
            AddUntypeableSuggestion(informational, result, notTestedOnEveryCell: false,
                "because those antigens were never imported. This is not a rule-out.");

            foreach (var (ab, ev) in result.SuspectedEvidence)
                if (ev.TotalConflicting > 0)
                {
                    var ag = ab.Replace("anti-", "");
                    important.Add($"{ab} has {ev.TotalConflicting} positive reaction(s) on " +
                        $"{ag}-negative cells. This suggests either multiple antibodies or dosage effect.");
                }

            foreach (var (ab, ev) in result.SuspectedEvidence)
                if (ev.Probability > 0.7 && ev.PatternQuality < 0.8)
                    important.Add($"{ab} has high support score ({ev.Probability * 100:F1}%) but " +
                        "imperfect pattern fit. Consider additional testing to confirm.");

            foreach (var (ab, stats) in result.SuspectedStatistics)
            {
                var ag = ab.Replace("anti-", "");
                var n = stats.IdentificationRequired;
                if (stats.MeetsIdentificationRule)
                {
                    informational.Add($"{ab} meets the {stats.IdentificationRuleLabel} identification rule " +
                        $"({stats.PositiveAgPositiveCount} Ag+ reactive, {stats.NegativeAgNegativeCount} Ag- nonreactive).");
                }
                else
                {
                    if (stats.PositiveAgPositiveCount < n)
                    {
                        var need = n - stats.PositiveAgPositiveCount;
                        important.Add($"{ab} has {stats.PositiveAgPositiveCount} of {n} required {ag}+ reactive cells. " +
                            $"Add {need} more {ag}+ cell(s) that react.");
                    }
                    if (stats.NegativeAgNegativeCount < n)
                    {
                        var need = n - stats.NegativeAgNegativeCount;
                        important.Add($"{ab} has {stats.NegativeAgNegativeCount} of {n} required {ag}- nonreactive cells. " +
                            $"Add {need} more {ag}- cell(s) that do not react.");
                    }
                }
            }

            foreach (var sep in result.SeparationEvaluations)
            {
                if (sep.MeetsCriterion) informational.Add(sep.Explanation);
                else important.Add(sep.Explanation);
            }
            var separationCells = result.SelectedCellRecommendations
                .Where(c => c.FillsSeparation && !c.IsExpired)
                .Select(c => $"{c.PanelName} cell {c.CellNumber}")
                .ToList();
            if (separationCells.Count > 0)
                important.Add($"Unused inventory cell(s) {string.Join(", ", separationCells)} have the phenotype needed " +
                              "to meet the multiple-antibody separation criterion if the serum reacts with them.");

            if (result.Suspected.Count > 1)
            {
                var top2 = result.Suspected.OrderByDescending(x => x.Value).Take(2).Select(x => x.Key);
                important.Add($"Pattern suggests multiple antibodies ({string.Join(", ", top2)}). " +
                    "Consider adsorption/elution studies.");
            }

            if (result.CandidateExplanations.Count > 0)
                informational.Add(
                    $"Explain Analysis lists {result.CandidateExplanations.Count} candidate(s) with rule-out, " +
                    "supporting, conflicting, phenotype, and additional-testing evidence.");

            foreach (var note in result.SpecialReactionNotes)
                important.Add(note);

            foreach (var note in result.TransfusionRequirementNotes)
                critical.Add($"REVIEW: {note}");

            foreach (var pattern in result.ReactionPatterns)
                important.Add(pattern.Explanation);

            if (result.PatientPhenotypeUnreliable)
                important.Add("Patient phenotype cannot be interpreted for alloantibody exclusion. " +
                    (string.IsNullOrEmpty(result.TransfusionContextNote)
                        ? "Recent transfusion or another configured limitation was noted."
                        : result.TransfusionContextNote));

            foreach (var c in result.PatientTypingConsiderations.Where(x => x.Kind == PatientTypingKind.Against))
            {
                if (result.Suspected.ContainsKey(c.Antibody))
                    critical.Add($"WARNING: {c.Antibody} is suspected, but patient typing argues against it. {c.Explanation}");
            }

            foreach (var c in result.PatientTypingConsiderations.Where(x =>
                         x.Kind == PatientTypingKind.Predicted && x.PatientValue == "+"))
            {
                if (result.Suspected.ContainsKey(c.Antibody))
                    critical.Add($"WARNING: {c.Antibody} is suspected, but the recorded genotype predicts a positive type. {c.Explanation}");
            }

            foreach (var c in result.PatientTypingConsiderations.Where(x => x.Kind == PatientTypingKind.Historical))
            {
                if (!result.Suspected.ContainsKey(c.Antibody))
                    informational.Add($"{c.Explanation} Look for this specificity if the current pattern is incomplete.");
            }

            if (result.SelectedCellRecommendations.Count > 0)
            {
                var names = result.SelectedCellRecommendations
                    .Take(3)
                    .Select(c => $"{c.PanelName} cell {c.CellNumber}");
                important.Add(
                    $"Selected cells {string.Join(", ", names)} may help resolve remaining candidates. " +
                    result.SelectedCellRecommendations[0].Explanation);
            }

            foreach (var de in result.DosageEffects)
                important.Add($"{de.Antibody} shows dosage effect (homozygous avg: {de.AvgHomozygous:F2}, " +
                    $"heterozygous avg: {de.AvgHeterozygous:F2}). " +
                    $"Consider additional homozygous {de.Antigen}+ cells.");

            // Special-panel suggestions
            foreach (var gate in result.GatedRuleouts)
                informational.Add($"NOTE: {gate.Antibody} cannot be ruled out from " +
                    $"{gate.CellTreatmentLabel} reactions — {gate.Reason}");

            foreach (var inf in result.TreatmentInferences)
                important.Add(inf.Observation);

            foreach (var abs in result.AbsorptionConclusions)
            {
                if (!string.IsNullOrWhiteSpace(abs.Explanation))
                    important.Add(abs.Explanation);
            }

            foreach (var exclusion in result.AdsorptionScoreExclusions)
                important.Add(exclusion);

            if (result.PatternMatches.Count > 0)
            {
                var best = result.PatternMatches[0];
                if (best.Confidence >= 1.0 && best.Matches >= 3)
                    informational.Add($"Perfect pattern match for {best.Antibody} " +
                        $"({best.Matches} matches, 0 mismatches). This is a strong identification.");
            }

            var common = new[] { "anti-D", "anti-E", "anti-K", "anti-c", "anti-C" };
            foreach (var ab in common)
            {
                if (result.RuledOut.ContainsKey(ab) || result.Suspected.ContainsKey(ab)) continue;
                informational.Add($"{ab} is not ruled out or suspected. " +
                    "Consider adding appropriate cells if ruling out this antibody is required.");
                break;
            }

            return critical.Concat(important).Concat(informational).Distinct().ToList();
        }

        private TransfusionAssessment? ApplyPatientTyping(string specimenId, AnalysisResult result)
        {
            var specimen = _db.GetSpecimen(specimenId);
            if (specimen == null) return null;
            var historyFinding = TransfusionHistoryCheck.Find(_db, specimen, AppSettings.Current.RecentTransfusionWindowDays);
            var transfusion = AssessTypingReliability(specimen, historyFinding);
            var typing = PatientTypingParser.Parse(
                specimen.Phenotype, specimen.PreviousAntibodies, specimen.Notes, specimen.Genotype, transfusion);
            result.PatientPhenotypeUnreliable = typing.PhenotypeUnreliable;
            result.TransfusionContextNote = transfusion.Explanation;
            result.PatientTypingConsiderations = PatientTypingParser.Evaluate(typing);
            if (HistoricalPhenotype.Find(_db, specimen, typing, AppSettings.Current.RecentTransfusionWindowDays) is { } earlier)
            {
                result.PatientTypingConsiderations.AddRange(earlier.Considerations);
                result.TransfusionContextNote = (string.IsNullOrEmpty(result.TransfusionContextNote) ? "" : result.TransfusionContextNote + " ") +
                    $"The patient's earlier phenotype from {earlier.Specimen.AccessionNumber} ({earlier.CollectedOn}, \"{earlier.Specimen.Phenotype!.Trim()}\"), " +
                    "typed before this transfusion context, is used as patient-typing evidence instead.";
            }
            var priorConfirmed = PatientHistory.ConfirmedAntibodies(PatientHistory.OtherSpecimens(_db, specimen));
            var known = typing.HistoricalAntibodies
                .Concat(priorConfirmed.Select(p => p.Antibody))
                .Concat(FinalAntibodyParser.Split(specimen.FinalAntibodies));
            result.TransfusionRequirementNotes = TransfusionRequirementCheck.MissingAntigenNegative(
                known, _db.GetPatient(specimen.Mrn), specimen.Mrn);
            if (SpecimenDating.Check(specimen, AppSettings.Current.DefaultSpecimenDatingDays,
                    AppSettings.Current.DatingTransfusionLookbackDays) is string dating)
                result.TransfusionRequirementNotes.Insert(0, dating);
            var antiD = result.Suspected.ContainsKey("anti-D") ||
                        FinalAntibodyParser.Split(specimen.FinalAntibodies).Contains("anti-D", StringComparer.OrdinalIgnoreCase);
            if (SpecimenDating.PassiveAntiD(specimen, antiD, AppSettings.Current.RhigPassiveWindowDays) is string passive)
                result.TransfusionRequirementNotes.Insert(0, passive);
            if (historyFinding != null)
                result.TransfusionRequirementNotes.Insert(0, historyFinding.Note);
            foreach (var expired in ExpiredReagentCheck.Check(_db, specimen.AccessionNumber))
                result.TransfusionRequirementNotes.Insert(0, expired.Note);
            foreach (var expired in ExpiredReagentCheck.CheckSpecimen(_db, specimen))
                result.TransfusionRequirementNotes.Insert(0, expired.Note);
            foreach (var panel in _db.GetSpecimenPanels(specimen.AccessionNumber))
            {
                var cells = _db.GetPanelCells(panel.PanelId);
                var untyped = PanelIntegrityCheck.UntypedCells(cells);
                if (untyped.Count == 0) continue;
                var total = cells.Count(c => !string.Equals(c.CellNumber, "AC", StringComparison.OrdinalIgnoreCase));
                result.TransfusionRequirementNotes.Insert(0,
                    $"{panel.Name}: {PanelIntegrityCheck.DescribeUntyped(panel, untyped, total)} " +
                    "Enter the typing from the vendor antigram on Panels, then re-analyze.");
            }
            foreach (var prior in priorConfirmed)
            {
                if (typing.HistoricalAntibodies.Any(h => PatientHistory.AntibodyKey(h) == prior.Antibody))
                    continue;
                result.PatientTypingConsiderations.Add(new PatientTypingConsideration
                {
                    Antibody = prior.Antibody,
                    Antigen = prior.Antibody[5..],
                    Kind = PatientTypingKind.Historical,
                    Explanation = prior.Explanation,
                });
            }
            return transfusion;
        }

        private void ApplyHtlaTitration(string specimenId, AnalysisResult result, List<Reaction> reactions)
        {
            var specimen = _db.GetSpecimen(specimenId);
            var extras = reactions.SelectMany(r => r.ExtraPhases);
            HtlaTitrationNotes.Apply(result, TiterParser.Highest(specimen?.Notes, extras));
        }

        private void ApplyNeutralization(
            string specimenId, AnalysisResult result, List<Reaction> reactions, List<PanelRun> runs)
        {
            var specimen = _db.GetSpecimen(specimenId);
            NeutralizationNotes.Apply(result, specimen?.Notes, reactions, runs);
        }

        private List<SelectedCellRecommendation> RecommendSelectedCells(
            List<Reaction> reactions, AnalysisResult result)
        {
            var tested = reactions
                .Where(r => r.CellNumber != "AC")
                .Select(r => (r.PanelId, r.CellNumber))
                .Distinct()
                .ToList();
            var inventory = new List<(Panel Panel, PanelCell Cell)>();
            foreach (var panel in _db.GetActivePanels())
            {
                foreach (var cell in _db.GetPanelCells(panel.PanelId))
                    inventory.Add((panel, cell));
            }
            if (inventory.Count == 0)
            {
                foreach (var panel in _db.GetAllPanels())
                {
                    foreach (var cell in _db.GetPanelCells(panel.PanelId))
                        inventory.Add((panel, cell));
                }
            }
            return SelectedCellRecommender.Recommend(result, tested, inventory);
        }

        private List<ReactionPatternNote> ClassifyReactionPatterns(
            string specimenId,
            Dictionary<int, List<Reaction>> byRun,
            Dictionary<int, RunContext> contexts)
        {
            // An all-negative adsorbed aliquot must not cancel a panreactive
            // untreated run. Adsorption evidence stays in the absorption notes.
            var observations = new List<PatternCellObservation>();
            foreach (var (runId, rxns) in byRun)
            {
                contexts.TryGetValue(runId, out var ctx);
                if (ctx != null && IsAdsorptionRun(ctx))
                    continue;
                foreach (var rxn in rxns)
                    observations.Add(ReactionPatternClassifier.Observe(rxn, ctx));
            }
            var dat = _db.GetSpecimen(specimenId)?.Dat ?? new DatProfile();
            return ReactionPatternClassifier.Classify(observations, dat);
        }

        private static List<string> DescribeSpecialGrades(IEnumerable<Reaction> reactions)
        {
            var notes = new List<string>();
            foreach (var rxn in reactions)
            {
                if (string.Equals(rxn.CellNumber, "AC", StringComparison.OrdinalIgnoreCase))
                    continue;
                var phases = new List<(string Phase, string Value)>
                {
                    ("IS", rxn.IS), ("37°C", rxn.C37), ("AHG", rxn.AHG)
                };
                foreach (var (phase, val) in rxn.ExtraPhases)
                    phases.Add((phase, val));
                foreach (var (phase, val) in phases)
                {
                    var canon = ReactionGrade.Normalize(val);
                    if (canon is ReactionGrade.Weak or ReactionGrade.MixedField or ReactionGrade.Hemolysis)
                    {
                        notes.Add(
                            $"Cell {rxn.CellNumber} {phase} is {ReactionGrade.Describe(canon)}; " +
                            "treat as reactive evidence, not a rule-out.");
                    }
                    else if (phase is not ("IS" or "37°C" or "AHG")
                             && ReactionGrade.IsPositive(val)
                             && !ReactionGrade.IsPositive(rxn.AHG))
                    {
                        notes.Add(
                            $"Cell {rxn.CellNumber} {phase} is {ReactionGrade.Describe(val)} while AHG is {ReactionGrade.Describe(rxn.AHG)}; " +
                            "treat as phase-specific reactivity, not a rule-out.");
                    }
                }
            }
            return notes;
        }

        // ── Update DB after analysis ───────────────────────────────────────────

        private void UpdateSpecimenAnalysis(string specimenId,
            Dictionary<string, int> ruledOut, Dictionary<string, double> suspected)
        {
            _db.ClearSpecimenAntibodies(specimenId);
            _db.ClearSpecimenRuleouts(specimenId);
            foreach (var (ab, cnt) in ruledOut)
                _db.AddSpecimenRuleout(specimenId, ab, cnt);
            foreach (var (ab, prob) in suspected)
                _db.AddSpecimenAntibody(specimenId, ab, prob);
            _db.SetSpecimenLastAnalyzed(specimenId);
        }

        // ── Fisher's Exact Test ───────────────────────────────────────────────

        private static double FisherExactOneSided(int a, int b, int c, int d)
        {
            int n = a + b + c + d;
            int r1 = a + b;
            int c1 = a + c;
            double pValue = 0;
            int kMax = Math.Min(r1, c1);
            for (int k = a; k <= kMax; k++)
                pValue += HypergeometricPmf(n, c1, r1, k);
            return Math.Min(1.0, Math.Max(0.0, pValue));
        }

        private static double HypergeometricPmf(int n, int K, int draws, int k)
        {
            if (k < Math.Max(0, draws + K - n) || k > Math.Min(draws, K)) return 0;
            return Math.Exp(
                LogBinom(K, k) + LogBinom(n - K, draws - k) - LogBinom(n, draws));
        }

        private static double LogBinom(int n, int k)
        {
            if (k < 0 || k > n) return double.NegativeInfinity;
            return LogFactorial(n) - LogFactorial(k) - LogFactorial(n - k);
        }

        private static double LogFactorial(int n)
        {
            double r = 0;
            for (int i = 2; i <= n; i++) r += Math.Log(i);
            return r;
        }
    }
}
