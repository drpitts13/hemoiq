# Antibody identification capability matrix

Decision-support software for trained blood-bank professionals. The engine
computes evidence; a qualified user must confirm identification. Scores and
rule-outs are analytical evidence, not a diagnosis.

Status: **Present** (usable), **Partial** (exists but incomplete or silent),
**Missing**.

Last updated: 2026-11-03 (iteration 66 — local users and roles enforcing review, clear, reinstate, Preferences).

## Capability matrix

| Area | Capability | Status | Current evidence | Next action |
|---|---|---|---|---|
| Gap file | Living capability matrix | Present | This file | Update every iteration |
| Import | Provider adapter framework | Partial | `IVendorPanelSource` + Bio-Rad/Quotient public catalogs; Ortho/Immucor/Grifols/Medion file-only (Ortho's public Quick Antigram PDFs are barcode/QR sheets with no readable antigen table) | Authenticated portals only with vendor permission |
| Import | CSV | Present | Lab CSV + vendor CSV parser; lab import now reviews before activation | — |
| Import | PDF | Partial | PdfPig positional grid; layout-fragile | Harden after schema work |
| Import | XLSX / XML / JSON panels | Present | JSON/XML structured schema; first-sheet XLSX via zip/shared strings; same inactive+artifact review | Multi-sheet XLSX later |
| Import | Vendor APIs / authenticated portals | Missing | Public HTML + file import only. Do not bypass auth, CAPTCHA, licensing, or terms | Secrets storage only if a vendor authorizes access |
| Common model | Manufacturer, lot, expiration, cell, ABO/Rh, standard antigens | Partial | `Panel` / `PanelCell`; ++ vs partner+ and rare-null (K0, Jk(a−b−), Rhnull-like) on import review | More ISBT alleles later |
| Common model | Homozygous / heterozygous / negative / unknown / NT | Present | Import and panel editor persist `++` / NT; rule-out and search use explicit ++ or typed partner − | Combo/list editors later |
| Import validation | Required fields, duplicate lot | Partial | Parse fails without cells; `FindPanelByVendorLot` | — |
| Import validation | Impossible values, schema-vs-prior-lot, unknown antigens | Partial | Schema vs prior lot; ++ vs partner+; unknown columns; rare-null notes; Weiner label vs typed D/C/c/E/e | More ISBT alleles later |
| Import traceability | Original artifact retained | Present | Vendor and lab CSV store SHA-256 + file; Artifact viewer verifies and opens | — |
| Reaction entry | IS / 37°C / AHG / CC | Present | Keyboard 0–4, W/M/H, N, Enter | — |
| Reaction entry | w+, MF, hemolysis | Present | Combo + keys; treated as reactive evidence | — |
| Reaction entry | RT, PEG, gel, solid phase as phases | Partial | Extra JSON plus Preferences RT/PEG/Gel/Solid and titer-grid checkboxes; empty stays tube-only; Gel/Solid/PEG are IAT-like; Dil1–Dil128 are titer only | Custom method names later |
| Analysis | Transparent candidate evidence | Partial | Explain Analysis lists suspected, in-progress, historical, untypeable, pattern, enzyme, DTT, and cell-level adsorption comparisons. Adsorbed aliquots do not rule out or enter ordinary scores for specificities they are expected to remove. A panreactive untreated run paired with autoadsorption or rr/R1R1/R2R2 adsorption is omitted from ordinary scores. Unpaired ficin, papain, and DTT runs are named and are not compared with another panel | Cross-panel enzyme and DTT pairing stays out of scope |
| Analysis | Configurable visible rule-out | Partial | `Rule.MinRuleoutCount` is now applied; lab default + explanation sentences | Watch seeded anti-D/anti-k rules; ACS remains separate; dosage-excluded cells (heterozygous / unknown zygosity) are named in the explanation, including antibodies with no qualifying cell (Iteration 141) |
| Analysis | Single / multiple / dosage scoring | Partial | Fisher + pattern; pairwise combinations; dosage averages | Label as evidence, not diagnosis |
| Audit | Investigation timeline | Present | `InvestigationTimeline` merges receipt, panel link/unlink (now audited), runs, grouped reaction saves, analysis snapshots, confirm/clear/review, MRN/transfusion/DAT/patient edits; Specimens Timeline expander and worksheet section | Report print/PDF/CSV audited with SHA-256 and shown on the timeline |
| Reporting | Turnaround metrics | Present | `TurnaroundService`: received→confirmed (days) and confirmed→reviewed (hours) median and nearest-rank 90th percentile by received date, cancelled excluded; overdue open list with status and Open; configurable threshold; CSV | Uses recorded received time when present |
| Reporting | Workload by technologist | Present | `WorkloadService`: per initials, confirmations (by confirmed date), multi-antibody confirmations, supervisor reviews (by review date), median received→confirmed; names from the user registry; cancelled excluded; on Analytics and in CSV | Reaction entry and amendments per user are on the Activity by user tab (by login, from the audit log) |
| Specimen | Collector, collection and ordering location, ordering provider | Present | Four optional specimen columns with DB length CHECK (128); trimmed and whitespace-collapsed; audited `update_origin`; locked after final call; on the timeline, detail pane, worksheet, and Search; `WorkloadService.ComputeByLocation` groups received investigations by ordering location on Analytics and CSV | Free text, not a configured location list |
| Specimen | Collection/receipt time and dating check | Present | Audited `collected_at`/`received_at`; expiration defaults from collection; `SpecimenDating` REVIEW when transfused within the look-back and expiration exceeds collection + dating days, or cannot be checked | Pregnancy (current or within look-back) is a dating trigger; RhIG date flags possible passive anti-D |
| Tests | Isolation from user data | Present | `AppDataPaths.RootOverride` set by a test-module initializer to a temp folder; live vendor test persists only to its isolated database; full run verified to leave %AppData%\HemoIQ untouched | One intermittent failure seen once and not reproduced in two reruns |
| Search | Investigation search | Present | Search tab › Investigations: accession, MRN, patient name/DOB, final/previous antibodies, technologist; every word must match; inclusive date range; status column; Open jumps to Specimens | Panel lot/manufacturer used in an investigation not yet searchable |
| Workflow | Investigation status and next step | Partial | `InvestigationStatusService` derives five steps and a status (awaiting panel → testing → analysis needed → awaiting ID → completed / amended / reopened / inactive) from panels, grades, analysis time, final call, and audit; shown on Specimens and the worksheet; Worklist shares the completeness check | Optional supervisor review step (two-person, audited, invalidated by clear/re-confirm, Worklist queue); audited Cancel/Reinstate with reason, locked and off work queues; local users/roles enforce review, clear, reinstate, Preferences, user management |
| Patient | Permanent record and transfusion requirements | Present | `patients` table (MRN, name, DOB, comments, free-text requirements, structured `antigen_negative`); `AntigenNegativeRequirement` parses codes (case-sensitive C/c, E/e, K/k, S/s; Fy(a-) notation) and refuses non-antigens; audited add/update; Edit Patient on Specimens; reports and detail pane print E−, K−, Fy(a−); analysis raises REVIEW when a known antibody's antigen is not in the structured list, and asks to add it when only free text mentions it | Expiry or source of each requirement; product-level requirements (irradiated, CMV-safe) remain free text |
| Patient | MRN and cross-specimen history | Partial | Indexed `specimens.mrn`; audited; confirmed final calls on the same MRN become Historical evidence citing the source accession; detail pane and reports list other specimens | Structured historical phenotype as analysis evidence (history lines now show encounter, earlier phenotype and genotype; sex is on the patient record) |
| Specimen | DAT polyspecific / IgG / C3 | Present | `DatProfile` on specimen; MF grade; audited IgG/C3; positive if any reactive, negative only if all recorded negative; C3-only caveat on autoantibody notes; MF DAT donor-cell note | Eluate specimens link to their source; `EluateEvidence` reads specificity, panreactivity, and nonreactivity against DAT and transfusion |
| Analysis | Panreactive / cold / warm / HTLA / auto / HFA / LFA models | Partial | DAT scores; named LFA/pan cells; enzyme lost/gained/resistant; DTT lost/resistant vs untreated. Unpaired runs, comparisons with fewer than 3 paired cells, and comparisons that meet no threshold are each named | Cells tested on only one of the paired runs stay unnamed |
| Analysis | Selected-cell recommendations with “why” | Partial | Unused in-date vials preferred; P1−/Ch−/Sd(a−) selected cells ranked when neutralization favors soluble substance | More ISBT alleles later |
| Specimen | Structured transfusion context | Partial | Status (Unknown / Not transfused / Transfused) + last-transfusion date on the specimen; DB CHECK constraint; audited; locked after final call; Preferences window (default 90 days) in analysis snapshot; MF autocontrol named against the record | Product counts/types, pregnancy, HSCT/BMT, mixed-field concern |
| Analysis | Patient phenotype / genotype / transfusion limits | Partial | Structured transfusion date vs configurable window decides phenotype reliability and is explained; note keywords only when status is Unknown; GYPA null/Mk En(a); RHCE*Ce vs ce case-sensitive; CeRN variant-C; RHD DAR/DVI/DNB partial D does not rule out anti-D; GATA still does not support anti-Fya | More ISBT alleles later |
| UI | Antigram freeze / slashes / run compare | Partial | Frozen cell; slashes; compare extras; Show dosage; filter/sort; panel lot compare | — |
| UI | Explain Analysis panel | Present | Explain tab + Summary + clinical report assemble rule-out, support, conflict, phenotype, additional testing | Keep wording as evidence, not diagnosis |
| Audit | Panel, reactions, rules, settings, final ID | Partial | Snapshots store rule-engine version, rules JSON, operator, and the adsorption comparison (conclusions plus ordinary-score exclusions) | Cross-panel enzyme/DTT pairing later |
| Panels | Antigen-coverage check at activation | Present | `PanelActivationCoverage` compares the lot being activated with the previous vendor/product-line lot; gaps (fewer or no typed cells) and affected ACS antigens shown in the activation prompt; acknowledged gaps recorded on the `activate_panel` audit from every activation path | Previous lot chosen by vendor/product line only; no comparison for panels without a vendor |
| Analysis | Cross-specimen transfusion check | Present | `TransfusionHistoryCheck`: a red-cell transfusion within the window on another same-MRN specimen contradicts Not transfused (REVIEW note) and fills Unknown, making typing unreliable with the source named; a stale Transfused date gets an update note | Uses only transfusions recorded in HemoIQ, not an external transfusion service |
| Workflow | Specimen-receipt checklist | Present | `ReceiptChecklist`: required MRN, collection time, transfusion status (+ last transfusion date when transfused); advisory receipt time, hemolysis, pregnancy, DAT. Drives the Received step and the Awaiting-panel next action; shaded line with reasons on the Specimens detail pane; printed on the worksheet | Required items are fixed, not configurable per lab |
| Reporting | Amended report marking | Present | Clinical Identification header shows AMENDED REPORT or PREVIOUS IDENTIFICATION WITHDRAWN when an identification was ever cleared; Amendment history lists each earlier identification, its confirmer, who cleared it, when and why, from `clear_final_call` audit entries | Text and PDF only; superseded issued copies are listed (Iteration 140), but recipients are not recorded |
| Data integrity | Reaction/specimen/panel consistency | Present | Detaching a panel with grades is refused; `DataConsistencyCheck` lists legacy reactions on detached panels, grades for cells the panel lacks, and unrecognized grade values as Worklist "Data inconsistency" items with an Open target | Check runs on Worklist refresh only; no scheduled report. New invalid grades and grades for missing cells are refused at save (Iteration 144) |
| Security | Roles and data-change permission | Present | Read-only, Technologist, Supervisor, Administrator; once users are registered every public data writer requires `RecordResults` (Technologist and above), so unregistered logins and Read-only users view only; guard test fails on any unguarded public writer; users table migrated to accept the new role | Role grants are fixed per role, not a configurable table; no Senior technologist or Medical director role. Status bar names the session; view-only sessions have change commands disabled (Iteration 132) |
| Configuration | ACS antigen list | Present | Fixed core (Rh, Kell, Duffy, Kidd, Ss) plus lab-configured additions (default Lea, Leb, M, N), validated case-sensitively; used by ACS evaluation, open-testing list, untypeable classifier and clinical-significance flags; listed on the report, in the rule trace and in the clinical snapshot; transferred with Preferences | The core itself is fixed by design |
| Configuration | Rules + Preferences transfer | Present | Separate rule and Preferences JSON exports; `ConfigurationBundle` exports both together; import previews both sections and the stored rules the file lacks, refuses a file with any refused entry, applies atomically with optional audited removal of local-only rules, one `import_configuration` audit | No signed or checksummed bundle; reaction grades and phases beyond extra phases are not yet configurable data |
| Tests | Synthetic cases with intermediate reasoning | Partial | Enzyme and DTT cases cover unpaired runs, short comparisons, mixed loss/gain/persistence that fits no pattern, and a one-loss two-persistence case that stays a partial destroyed pattern | Expand case library |

## Iteration 1 (this change)

**Deficiency:** Labs can configure `MinRuleoutCount` and heterozygous exceptions,
but the analyzer treated any single qualifying homozygous cell as ruled out and
never explained the policy.

**Bounded improvement:** Apply `DefaultMinRuleoutCount` (lab default 1) and
per-antibody `Rule.MinRuleoutCount`. Separate observed qualifying cells from
“meets configured rule-out.” Show an explainable sentence. ACS continues to use
`AcsRuleoutCount` against **observed** qualifying counts.

**Not in this tick:** import NT semantics, selected cells, phenotype engine,
XLSX/portals.

## Iteration 2 (this change)

**Deficiency:** Vendor sheets that omit an antigen column, or mark a cell NT,
were persisted as typed `-`. That made E+ look homozygous when `e` was never
typed, and let untested antigens enter Ag− scoring.

**Bounded improvement:** Persist missing/NT as not-tested. Analysis only uses
antigens the panel actually types. Antithetical zygosity requires a typed
partner. Summary and suggestions list clinically significant antigens that
were not tested.

**Not in this tick:** selected cells, phenotype engine, XLSX/portals, import
review-before-activation.

## Iteration 3 (this change)

**Deficiency:** After remaining candidates were listed, the app only said
“add more Ag+ cells.” It did not rank unused inventory cells or explain why
a cell would discriminate competing antibodies.

**Bounded improvement:** Score unused active-panel cells for remaining
candidates. Prefer homozygous expression of the antigen of interest, cells
that lack competing antigens, and cells that resolve the most competing
pairs. Show explanations on the Selected Cells tab, Summary, and suggestions.

**Not in this tick:** phenotype/genotype limits, full per-candidate narrative
for non-suspected antibodies, XLSX/portals.

## Iteration 4 (this change)

**Deficiency:** Specimen phenotype and previous antibodies were display-only.
A patient who typed E+ could still be left with silent anti-E suspicion, and
recent transfusion was not treated as a limit on interpretation.

**Bounded improvement:** Parse Weiner and antigen text into support / against /
uninterpretable / historical considerations. Do not auto-remove candidates.
Notes mentioning transfusion make phenotype evidence uninterpretable.

**Not in this tick:** structured genotype columns, per-candidate narrative for
non-suspected antibodies, XLSX/portals.

## Iteration 5 (this change)

**Deficiency:** Supporting and conflicting cells existed only for antibodies
that already crossed the suspected threshold. Ruled-out, in-progress, and
historical specificities had no assembled narrative a reviewer could read
in one place.

**Bounded improvement:** `AnalysisExplainer` builds a per-candidate explanation
from existing rule-out evaluations, suspected evidence, dosage, phase scores,
patient typing, and selected-cell recommendations. The Analysis Explain tab,
Summary, suggestions, and clinical report show that text. Nothing is
auto-confirmed.

**Not in this tick:** import review-before-activation, genotype columns,
XLSX/portals, broader reaction grades/phases.

## Iteration 6 (this change)

**Deficiency:** Vendor lots became active as soon as parse succeeded. The
original file was not retained, and a new lot that dropped or added an
antigen column was not compared to the previous lot from that vendor.

**Bounded improvement:** Vendor imports stay inactive. SHA-256 of the source
bytes is stored and the file is copied beside the database. The review
sentence names added/removed antigens versus the prior lot. A qualified user
must activate the panel before it enters inventory.

**Not in this tick:** lab-CSV artifact retention, XLSX/portals, broader
reaction grades/phases, structured genotype fields.

## Iteration 7 (this change)

**Deficiency:** Lab CSV import typed missing antigen columns as `-`, kept
no source file, and activated from the details dialog without comparing the
sheet to the prior lot.

**Bounded improvement:** Missing CSV columns stay untyped. `LabPanelImportService`
stores SHA-256 + the original file, compares antigen headers to the prior
lot, and leaves the panel inactive until review. Same review dialog as vendor
import.

**Not in this tick:** artifact viewer, XLSX/portals, broader reaction
grades/phases, structured genotype fields.

## Iteration 8 (this change)

**Deficiency:** Reaction entry and scoring only knew 0–4+ and NT. A weak,
mixed-field, or hemolyzed result had no grade. `ReactionToNumeric` treated
those strings as 0, so they could disappear from “strongest phase” even when
`IsPositive` saw them as non-zero text.

**Bounded improvement:** Canonical grades `w+`, `MF`, and `H` are selectable
and typed (W/M/H). They count as reactive evidence, cannot support rule-out,
and produce a reviewable sentence. RT/PEG/gel remain later as extra phases.

**Not in this tick:** configurable extra phases, artifact viewer, XLSX/portals.

## Iteration 9 (this change)

**Deficiency:** Labs that record RT, PEG, or gel reactivity had no column.
Those results were either omitted or stuffed into AHG, so a room-temp-only
pattern could look like a valid AHG-negative rule-out.

**Bounded improvement:** Lab Preferences accepts extra phase names. Grades
are stored as JSON on the reaction. Extra-phase reactivity counts as
positive evidence and cannot support rule-out. Prewarm still suppresses RT
with IS. Classic IS / 37°C / AHG / CC is unchanged when the setting is blank.

**Not in this tick:** artifact viewer, XLSX/portals, compare-grid extra columns.

## Iteration 10 (this change)

**Deficiency:** Vendor and lab CSV imports stored a SHA-256 and a copy of
the source file, but the UI never showed whether that file still existed or
still matched.

**Bounded improvement:** The Panels **Artifact** button inspects the retained
file, reports present / missing / hash-mismatch, previews CSV text, and can
open the original. Panel details show the stored hash. Nothing is
auto-activated.

**Not in this tick:** XLSX/XML/JSON adapters, compare-grid extra columns.

## Iteration 11 (this change)

**Deficiency:** Labs and vendors could import PDF or CSV only. Spreadsheet
exports and structured panel files were rejected or misread as CSV.

**Bounded improvement:** Vendor and lab import accept JSON, XML, and first-sheet
XLSX. JSON/XML carry lot, catalog, and per-cell antigens without inventing
types for omitted antigens. XLSX uses the same grid rules as CSV (Cell column
plus recognized antigen headers). Imports stay inactive with a retained
artifact. No new packages; no authenticated vendor portals.

**Not in this tick:** multi-sheet workbooks, antigram grouping / dosage toggle,
compare-grid extra columns, authenticated APIs.

## Iteration 12 (this change)

**Deficiency:** Extra phases (RT, PEG, gel) were entered and scored, but the
run-vs-run compare grid only showed IS / 37°C / AHG / CC. A cell that was
AHG-negative on both runs and RT 2+ on one looked unchanged.

**Bounded improvement:** Compare rows include configured extra phases (and any
extras stored on either run). A difference at RT/PEG/gel marks the row changed
and produces a sentence such as `Cell 1 differs: RT (this 2+ vs other 0).`
Missing extra on the other run is NT. Classic phases still compare as before.

**Not in this tick:** antigram grouping / dosage toggle, solid-phase defaults,
authenticated APIs.

## Iteration 13 (this change)

**Deficiency:** The reaction antigram showed only +/−. Homozygous and
heterozygous cells looked the same, and columns followed sheet order with no
blood-group grouping. Dosage lived only on a later Analysis tab.

**Bounded improvement:** Reactions **Show dosage** groups visible columns by
system (Rh, Kell, Duffy, …) and marks homozygous antigen-positive cells as
`++`, heterozygous as `+`, and `+/?` when the antithetical partner was never
typed. Tooltips name the cell and zygosity. Off restores the panel column
order and plain +/−. Rule-out is unchanged.

**Not in this tick:** filter/sort, panel compare, solid-phase defaults,
authenticated APIs.

## Iteration 14 (this change)

**Deficiency:** The reaction antigram always showed every cell in vial order.
A reviewer looking for homozygous E+ or reactive cells had to scan the whole
sheet, and there was no way to hide Ag− rows without losing them from save.

**Bounded improvement:** Filter the visible grid by antigen and zygosity, and
sort by cell number, reactive first, or selected-antigen first. Autocontrol
stays visible. Hidden cells remain in the working set for save, compare, and
analysis. A sentence states how many cells match.

**Not in this tick:** panel compare, solid-phase defaults, authenticated APIs.

## Iteration 15 (this change)

**Deficiency:** Import review compared antigen *columns* to the prior lot, but
labs could not open two stored panels and see which *cells* changed typing.
A new lot that flipped cell 1 from E+ to E− looked the same if the header
list matched.

**Bounded improvement:** Panels **Compare…** diffs the selected lot against
another stored panel (defaults to the prior vendor/product lot). It reports
added/removed antigens, cells present on only one lot, and per-cell typing
changes. Missing types stay NT, not invented negatives.

**Not in this tick:** solid-phase defaults, authenticated APIs.

## Iteration 16 (this change)

**Deficiency:** Phase scores existed, but a sheet that was all IS 2+ / AHG 0
looked like ordinary negative AHG rule-outs. Gel and Solid reactivity had no
IAT-like meaning in the narrative.

**Bounded improvement:** Classify cold (IS/RT+ AHG/IAT−), warm (IAT+ IS/RT−),
mixed-phase, and panreactive patterns when at least three evaluated cells
match. Gel, Solid, and PEG count as IAT-like. Prewarm still ignores IS/RT.
Sentences appear on Explain, Summary, and suggestions. Nothing is
auto-confirmed.

**Not in this tick:** HTLA / HFA / LFA models, ExtraPhases lab default list,
authenticated APIs.

## Iteration 17 (this change)

**Deficiency:** Weak pan-AHG (w+/1+) looked like an ordinary warm pattern.
Panagglutination did not use the autocontrol to separate autoantibody from
high-prevalence alloantibody, and a single unexplained reactive cell had no
low-frequency note.

**Bounded improvement:** Weak IAT without IS/RT (4+ cells) is labeled HTLA-like.
Panreactive plus a nonreactive AC favors high-prevalence; a reactive AC favors
autoantibody. One or two reactive cells among six or more is labeled
low-frequency / extra antibody / mistype. Still evidence, not a diagnosis.

**Not in this tick:** ExtraPhases lab default list, titration workflow,
authenticated APIs.

## Iteration 18 (this change)

**Deficiency:** Genotype lived only as free-text phenotype. After
transfusion the entire type was uninterpretable, so a recorded *RHCE*ce/ce*
or *FY*02/FY*02* result could not argue for or against an alloantibody.

**Bounded improvement:** Specimen `genotype` is stored and parsed into
predicted antigens (ISBT-style *RHCE* / *RHD* / *FY* / *JK* / *KEL* pairs,
or the same Weiner/Fy(a−b+) marks). Serology still becomes
uninterpretable after transfusion; genotype remains Predicted evidence.
Conflicts are explained. Nothing is auto-ruled-out or auto-confirmed.

**Not in this tick:** ExtraPhases lab default list, variant-allele risk
catalog, authenticated APIs.

## Iteration 19 (this change)

**Deficiency:** Extra phases existed only as a comma-separated string. Labs
did not have a visible RT / PEG / Gel / Solid default, and empty settings
gave no hint that Solid is an IAT-like Capture column.

**Bounded improvement:** Preferences now toggles the suggested extras.
`FromSuggested` / `Toggle` keep custom names, refuse reserved IS/AHG
columns, and leave a blank list blank so tube-only labs stay on IS / 37°C /
AHG / CC. Solid-phase 0 with AHG 0 can still rule out; Solid+ remains
IAT-like evidence, not a diagnosis.

**Not in this tick:** Variant-allele catalog, titration workflow,
authenticated APIs.

## Iteration 20 (this change)

**Deficiency:** Selected-cell suggestions treated every unused inventory cell
the same. A leftover ID-panel cell could outrank an unused Selectogen /
0.8% vial, and patient E− (or a post-transfusion genotype prediction)
did not raise an E+ cell.

**Bounded improvement:** Unused vials whose name looks like selected cells
get a ranking boost and an explanation. Patient Ag− (including Predicted
genotype after transfusion) boosts matching Ag+ cells; patient Ag+ only
softens the score and never drops a distinguishing cell. Still
recommendations, not a diagnosis.

**Not in this tick:** HTLA titration workflow, expired-vial filtering,
authenticated APIs.

## Iteration 21 (this change)

**Deficiency:** HTLA notes only said “consider titration.” A recorded titer
in Notes or a Titer extra-phase was ignored, and unused Yt(a−) / Vel−
cells were never ranked when the sheet looked HTLA or high-prevalence.

**Bounded improvement:** Parse a recorded titer (notes or Titer/Dilution
column). Titer ≥16 strengthens the HTLA sentence; a low titer warns that
it may not be HTLA. When HTLA or high-prevalence is present, unused cells
typed or annotated as high-prevalence Ag− are recommended. Nothing is
auto-identified.

**Not in this tick:** Neutralization workflow, expired-vial filtering,
authenticated APIs.

## Iteration 22 (this change)

**Deficiency:** Selected-cell ranking could name an expired Selectogen vial
ahead of an in-date ID-panel cell. Expiration was not explained.

**Bounded improvement:** In-date unused cells are preferred. Expired vials
are omitted when any in-date recommendation exists. If only expired
inventory remains, at most two cells are listed with a “do not use”
warning. Expiring lots stay recommended and ask the user to confirm the
date. Still not a diagnosis.

**Not in this tick:** Allele variant catalog, neutralization workflow,
authenticated APIs.

## Iteration 23 (this change)

**Deficiency:** Any `N` in an *RHD* allele was treated as a deletion, so
*RHD*DNB* became D−. *FY*01N (GATA) predicted Fy(a−) and treated that as
support for alloanti-Fya, which is usually wrong.

**Bounded improvement:** Null *RHD* alleles are *01N* / deletion / DEL only.
Weak D, partial D, DEL, GATA *FY*01N, FyX, and *RHCE*ceAR-like haplotypes
add Variant notes. GATA suppresses “predicted Fya− supports anti-Fya.”
Nothing is auto-identified.

**Not in this tick:** Full ISBT catalog, neutralization workflow,
authenticated APIs.

## Iteration 24 (this change)

**Deficiency:** HTLA notes said “consider neutralization,” but a Neut/Inhib
column or “neutralized with plasma” note was never compared to IAT.

**Bounded improvement:** Pair IAT with Neut/Neutral/Inhib grades. Loss of
reactivity favors a soluble-substance / HTLA-like pattern (plasma, urine,
saliva, P1). Persistence argues against readily neutralized Ch/Rg, Sd(a),
or Lewis. Notes without paired grades stay qualitative. Nothing is
auto-identified.

**Not in this tick:** Full titer grid, broader ISBT catalog, authenticated
APIs.

## Iteration 25 (this change)

**Deficiency:** Vendor `++` / `+/+` / HOMO was flattened to `+`. Homozygous
rule-out then required a typed antithetical partner, so a marked homozygous
cell with `e` NT could not rule out anti-E. A lone `+` with an untyped
partner still had to stay unknown.

**Bounded improvement:** Persist explicit `++`. Rule-out, selected-cell
ranking, dosage homo/het, antigram dosage, and inventory search treat `++`
as homozygous without inventing a partner-negative. A typed partner `+`
overrides `++` (conflict, not homozygous). A single `+` with an untyped
partner is still not homozygous. Nothing is auto-identified.

**Not in this tick:** Panel-editor `++` toggle, broader ISBT catalog,
authenticated APIs.

## Iteration 26 (this change)

**Deficiency:** *JK**, *KEL* null, and *GYPB* tokens were parsed for
predicted Jk/K/S types, but there were no review notes. *JK*01N/JK*02N*
looked like ordinary Jk(a−b−) support and never mentioned anti-Jk3.
Weak *JK*01W was treated as a normal Jk(a+).

**Bounded improvement:** True JK nulls stay predicted antigen-negative
(unlike GATA-FY). Two JK null alleles add a Jk3 review note. Weak JK
warns that predicted Jk+ does not rule out alloanti-Jk. KEL-null / K0
and GYPB null / U-var add Variant notes. Nothing is auto-identified.

**Not in this tick:** Full ISBT catalog, panel-editor zygosity marks,
authenticated APIs.

## Iteration 27 (this change)

**Deficiency:** The panel antigen editor toggled only `+`/`−` and displayed
`++` as `+`. A lab could not mark homozygous or NT without inventing a
partner-negative, and an imported `++` was lost on the first click.

**Bounded improvement:** Click cycles NT → + → ++ → − (unpaired antigens
such as D skip ++). The editor shows and saves `++` and NT. Saved `++`
with an untyped partner still rules out; saved NT is not treated as
antigen-negative. Nothing is auto-identified.

**Not in this tick:** Full titer grid, more ISBT alleles, authenticated
APIs.

## Iteration 28 (this change)

**Deficiency:** A titer was only a notes word or a single Titer/Dilution
number. Serial tubes (Dil1–Dil128 or 1:2 / 1:4 …) were ignored, and a
grade on a dilution column could look like a panel phase.

**Bounded improvement:** Last reactive serial-dilution extra phase is the
endpoint (prozone uses the highest reactive tube). `1+` is still a grade,
not titer 1. Dilution columns are not IAT and do not change rule-out.
Nothing is auto-identified.

**Not in this tick:** Preferences titer-grid checkbox, more ISBT alleles,
authenticated APIs.

## Iteration 29 (this change)

**Deficiency:** Serial-dilution columns parsed as a titer only if the lab
typed Dil1–Dil128 (or 1:n) into Extra phases. Tube-only Preferences had
no control to add that grid, and the extra-phase list capped at 12 names
so a titer grid plus Solid/Neut could be truncated.

**Bounded improvement:** Preferences **Titer grid (Dil1–Dil128)** adds
those columns. Unchecking removes them and keeps RT/PEG/Gel/Solid and
custom names such as Neut. Dilution grades remain titer evidence, not
IAT, and cannot prevent AHG-negative rule-out. Extra-phase cap raised
to 24. Nothing is auto-identified.

**Not in this tick:** More ISBT alleles, impossible-value import catalog,
authenticated APIs.

## Iteration 30 (this change)

**Deficiency:** Import review compared antigen *headers* to the prior lot,
but a cell marked E++ with e+ still looked like a clean homozygous type,
and columns the catalog does not know (Fy3, vendor extras) disappeared
without a review sentence.

**Bounded improvement:** `PanelTypingInspector` flags homozygous ++ with
a positive antithetical partner, unrecognized type tokens, and unknown
antigen columns. Issues appear on the inactive-lot review. Heterozygous
+/+ and ++ with an untyped or negative partner stay silent. Unknown
columns are not imported. Nothing is auto-activated or identified.

**Not in this tick:** Null-phenotype catalogs (K0, Rhnull), more ISBT
alleles, authenticated APIs.

## Iteration 31 (this change)

**Deficiency:** Both-negative antithetical types on a reagent cell looked
like ordinary negatives. K−k−, Jk(a−b−), or C−c− plus E−e− could be a
true null cell or a mistype, and Lewis Lea−Leb− (common) would have been
noisy if every pair were flagged.

**Bounded improvement:** Import review notes rare-null patterns (K0-like,
Jk(a−b−), Fy(a−b−), S−s−, M−N−, Lu(a−b−), Rhnull/D−−-like). Lewis both
negative is not flagged. An untyped partner is not treated as negative.
Lots stay inactive. Nothing is auto-identified.

**Not in this tick:** More ISBT alleles, phenotype-string vs typed-antigen
conflicts, authenticated APIs.

## Iteration 32 (this change)

**Deficiency:** A stored Weiner label (R1R1) was never compared with typed
D/C/c/E/e. An R1R1 cell that was c+ looked like a normal heterozygous
C+c+ type and could be used for homozygous anti-c rule-out by mistake.

**Bounded improvement:** Import review flags Weiner/Rh labels that
disagree with typed Rh antigens. C++ still matches an expected C+.
Untyped antigens and matching rr / R1R1 cells stay silent. Lots stay
inactive. Nothing is auto-identified.

**Not in this tick:** More ISBT alleles, ABO vs reverse-cell notes,
authenticated APIs.

## Iteration 33 (this change)

**Deficiency:** Neutralization notes explained IAT vs Neut, but unused
inventory was never ranked for the matching antigen-negative cell
(P1− after P1 substance, Ch− after plasma). Ordinary ID-panel P1−
rows would have drowned those vials.

**Bounded improvement:** When neutralization favors a soluble
substance (lost IAT reactivity or a qualitative note), unused
selected-cell vials or Special Types annotations that lack P1, Ch/Rg,
Sd(a), or Lewis are recommended with a why sentence. Persistence after
neutralization does not boost those cells. Ordinary panel P1− rows are
not ranked. Nothing is auto-identified.

**Not in this tick:** More ISBT alleles, saliva Le(a−b−) panel-wide
ranking, authenticated APIs.

## Iteration 34 (this change)

**Deficiency:** Analysis snapshots stored software version and lab
settings, but not the rule-engine version or per-antibody MinRuleoutCount
/ heterozygous exceptions. A later rule edit left no trace of the policy
that produced a prior analysis.

**Bounded improvement:** Snapshots record `rule-engine-v1` plus the
active antibody rules. Explain Analysis, Summary, and the report trace
line state lab default vs overrides (for example anti-D requires 3,
heterozygous C allowed). Changing a rule writes a new snapshot. Nothing
is auto-identified.

**Not in this tick:** More ISBT alleles, operator identity on the
snapshot row, authenticated APIs.

## Iteration 35 (this change)

**Deficiency:** GYPA tokens were recognized but GYPA*01N was treated as
ordinary GYPA*01 (predicted M+). Mk / En(a−) genotypes never produced a
review note, so M−N− looked like a typing gap rather than a GYPA null.

**Bounded improvement:** GYPA nulls persist as antigen-negative (M0/N0),
not as M+ or N+. GYPA*01N/GYPA*02 stays M−N+ without an En(a) note.
Two GYPA nulls or Mk alleles add an anti-En(a) review note. Ordinary
GYPA*01/GYPA*02 stays silent. Nothing is auto-identified.

**Not in this tick:** RHCE*CeRN / more RHD alleles, operator on snapshots,
authenticated APIs.

## Iteration 36 (this change)

**Deficiency:** RHCE haplotype parsing used case-insensitive "ce", so
RHCE*Ce and RHCE*CeRN were treated as little-c haplotypes. Variant C
(CeRN / HAR / C^w) had no review note, so predicted C+ looked like it
could rule out anti-C.

**Bounded improvement:** C vs c in RHCE alleles is case-sensitive. CeRN
still predicts C+e+ (with c+ if the partner is ce) and adds a variant-C
note that predicted C+ does not rule out anti-C. Ordinary RHCE*Ce/ce
stays silent. ceAR remains a variant-e note. Nothing is auto-identified.

**Not in this tick:** More RHD DAR/DVI catalog rows, never-imported
specificity classifiers, authenticated APIs.

## Iteration 37 (this change)

**Deficiency:** Analysis snapshots recorded software version and rules
but not who ran the analysis. Audit events already stored Windows login;
Explain Analysis and the report trace line did not.

**Bounded improvement:** Snapshots store `analyzed_by` (lab initials when
set, otherwise Windows login). Explain, Summary, and the report name the
operator and still say this is not a diagnosis. Blank initials fall back
to Windows login; neither recorded is stated as such. Nothing is
auto-identified.

**Not in this tick:** Never-imported specificity classifiers,
authenticated APIs.

## Iteration 38 (this change)

**Deficiency:** Antigens missing from imported sheets were listed as a
flat “not tested” name list. Antibodies such as anti-Kpa or anti-Cw
never appeared in Explain Analysis, so a reviewer could not see that
the current panels cannot type those specificities.

**Bounded improvement:** Classify standard-catalog antibodies whose
antigens were never imported. Explain Analysis, Summary, and
suggestions state that they cannot be typed (not a rule-out, not an
identification). Clinically significant antigens keep the existing
untreated list. Historical antibodies without a typed column are
labeled historical and untypeable. Warehouse antigens stay silent
unless a panel types them. Nothing is auto-identified.

**Not in this tick:** Pattern classifiers (HTLA/HFA/LFA beyond titer),
authenticated APIs.

## Iteration 39 (this change)

**Deficiency:** Pattern notes labeled cold, warm, panagglutination, and
HTLA but did not name which cells supported or conflicted, did not use
the recorded DAT, and did not attach an evidence score. Panagglutination
without an autocontrol could not lean auto vs high-prevalence.

**Bounded improvement:** Each pattern records supporting/conflicting
cells and an analytical evidence score (not a diagnosis). Low-frequency
notes name the reactive cell. Incomplete panagglutination names the
nonreactive cells. DAT positive with panagglutination and no autocontrol
favors autoantibody; DAT negative favors high-prevalence. A reactive
autocontrol with a negative DAT is shown as conflicting evidence.
Nothing is auto-identified.

**Not in this tick:** Enzyme-treated vs untreated pattern comparison,
authenticated APIs.

## Iteration 40 (this change)

**Deficiency:** Ficin/papain runs gated destroyed-antigen rule-outs, but
lost vs gained vs surviving reactivity was only inferred for antibodies
already suspected. Explain Analysis did not show a panel-wide enzyme
comparison, and ReactivityGainedOnEnzyme was unused.

**Bounded improvement:** Compare untreated vs ficin/papain runs on the
same panel. Lost reactivity names cells and enzyme-destroyed antigens
(Fya, M, S, …). Newly reactive cells favor enzyme-enhanced antigens
(Rh, Kidd). Survival of reactivity argues against destroyed antigens as
the sole explanation. Mixed lost/persisted is conflicting evidence.
Scores and named cells are not a diagnosis.

**Not in this tick:** Cross-panel enzyme pairing, more RHD DAR/DVI
catalog rows, authenticated APIs.

## Iteration 41 (this change)

**Deficiency:** RHD*DAR was treated as ordinary D+ with no review note,
so serologic D+ looked like it could rule out anti-D. DVI/DNB used a
generic weak/partial sentence that did not say D+ does not rule out
anti-D.

**Bounded improvement:** Catalog partial D alleles (DAR, DVI, DIII–DVII,
DNB, DAU, DFR, DOL, DBT, DHAR, and similar). They do not predict D+ and
add a note that serologic or predicted D+ does not rule out anti-D.
Weak D (RHD*01W) still predicts D+ with the existing weak-D note.
Ordinary RHD*01 is unchanged. A D+ tube type with RHD*DAR is conflicting
evidence, not a rule-out. Nothing is auto-identified.

**Not in this tick:** DTT-treated vs untreated pattern comparison,
authenticated APIs.

## Iteration 42 (this change)

**Deficiency:** DTT runs gated Kell/Lutheran rule-outs, but lost vs
surviving reactivity was only inferred for already-suspected antibodies.
Explain Analysis had no panel-wide DTT comparison parallel to ficin/papain.

**Bounded improvement:** Compare untreated vs DTT runs on the same panel.
Lost reactivity names cells and DTT-destroyed antigens (K/k, Lua/Lub, Yt,
Knops, Do, …). Survival argues against DTT-destroyed antigens as the sole
explanation. Mixed lost/persisted is conflicting evidence. Scores and named
cells are not a diagnosis. Enzyme kinds stay separate from DTT kinds.

**Not in this tick:** Cross-panel DTT pairing, absorption evidence in
Explain, authenticated APIs.

## Iteration 43 (this change)

**Deficiency:** Adsorption conclusions labeled a suspected antibody
“survived” when any cell on the aliquot was reactive. Explain Analysis
did not name the antigen-positive cells that lost or kept reactivity,
and a negative on an rr / R1R1 / R2R2 aliquot could count as a rule-out
of the antibody that aliquot is expected to remove.

**Bounded improvement:** Compare each adsorbed aliquot with the matching
untreated run on the same panel. For allogeneic adsorption, name
antigen-positive cells that lost reactivity (supporting removal), cells
that stayed reactive (conflicting), and typed-negative cells whose
remaining reactivity is not explained by the expected specificities.
Autoadsorption names cells that lost panreactivity and cells that
remained reactive. Homozygous cells are marked. Scores are analytical
evidence. Explain Analysis, Summary, suggestions, the adsorption grid,
and the clinical report show the sentences. Negatives on an aliquot do
not rule out antibodies that aliquot is expected to remove. Nothing is
auto-identified.

**Not in this tick:** Using the adsorbed aliquot as ordinary probability
evidence, cross-panel pairing, snapshot storage of the comparison,
authenticated APIs.

## Iteration 44 (this change)

**Deficiency:** An rr (or R1R1 / R2R2) aliquot no longer ruled out the
antibodies it is expected to remove, but those same reactions still
entered Fisher's exact test, pattern matches, phase scores, dosage, and
combination counts. Antigen-positive cells that became negative after
adsorption looked like evidence against anti-c. Cells that stayed
reactive were listed again as ordinary supporting cells.

**Bounded improvement:** For each specificity an aliquot is expected to
remove, skip that aliquot in ordinary probability and pattern scores.
Removal and survival stay on the absorption comparison. Explain Analysis,
Summary, suggestions, and the clinical report say the aliquot was
excluded and that this is not a diagnosis. The analysis snapshot stores
the comparison and the exclusion sentences. Antibodies the aliquot is
not expected to remove, including residual anti-K after rr adsorption,
still use the aliquot. Autoadsorption has no specific allo list, so its
aliquot remains ordinary evidence. Explain Analysis keeps anti-c and
anti-C as separate candidates.

**Not in this tick:** Once autoadsorption is present, stop using the
untreated panreactive run as ordinary alloantibody evidence. Authenticated
vendor portals stay later and must not bypass auth, CAPTCHA, licensing,
or terms.

## Iteration 45 (this change)

**Deficiency:** Autoadsorption has no specific alloantibody list, so the
aliquot stayed in ordinary scores. The paired untreated run, often
panreactive, stayed in those scores too. Antigen-negative cells that
reacted before adsorption looked like mismatches against an underlying
alloantibody.

**Bounded improvement:** When an untreated run is panreactive (at least 4
evaluated cells and at least 80 percent reactive) and an autoadsorbed
aliquot on the same panel and cell treatment has reactions, ordinary
probability and pattern scores use the aliquot and omit that untreated
run. A 3-of-4 reactive untreated run still counts. Explain Analysis,
Summary, suggestions, and the clinical report name the excluded run and
say this is not a diagnosis. The absorption comparison still uses both
runs. Allogeneic adsorption is unchanged.

**Not in this tick:** Applying the same omission to a panreactive
untreated run paired only with rr / R1R1 / R2R2 adsorption. Authenticated
vendor portals stay later and must not bypass auth, CAPTCHA, licensing,
or terms.

## Iteration 46 (this change)

**Deficiency:** A panreactive untreated run was omitted from ordinary
scores only when the paired aliquot was autoadsorbed. The same warm
panagglutinin beside rr, R1R1, or R2R2 adsorption still entered Fisher's
exact test and pattern matches. Antigen-negative cells that reacted
before adsorption looked like mismatches against an antibody that
remained on the aliquot.

**Bounded improvement:** The same panreactive rule (at least 4 evaluated
cells and at least 80 percent reactive) now applies when the paired
aliquot is allogeneic. Ordinary scores use that aliquot for specificities
it is not expected to remove. Specificities it is expected to remove stay
on the absorption comparison, including survival when antigen-positive
cells remain reactive. A 3-of-4 reactive untreated run still counts.
Explain Analysis names the excluded run and says this is not a diagnosis.

**Not in this tick:** Cross-panel pairing of enzyme or DTT runs.
Authenticated vendor portals stay later and must not bypass auth,
CAPTCHA, licensing, or terms.

## Iteration 47 (this change)

**Deficiency:** A ficin, papain, or DTT run with no untreated run on the
same panel was skipped without a sentence. An untreated run on a different
panel was also ignored silently, so a reviewer could not tell that no
enzyme or DTT comparison had been made.

**Bounded improvement:** Those runs now produce an evidence note naming
the treated run and panel, stating that no same-panel untreated run was
available, and stating that a different panel was not paired. A same-panel
comparison with fewer than three paired cells stays silent. Explain
Analysis, Summary, suggestions, the clinical report, and the Special
Panels tab show the note. Nothing is identified.

**Not in this tick:** Explaining a same-panel comparison that has too few
paired cells. Authenticated vendor portals stay later and must not bypass
auth, CAPTCHA, licensing, or terms.

## Iteration 48 (this change)

**Deficiency:** A ficin, papain, or DTT run that shared a panel with an
untreated run, but had fewer than three cells that could be paired, was
skipped. Two cells that lost reactivity looked like an enzyme-destroyed
or DTT-destroyed pattern and were not distinguished from a comparison
that never happened.

**Bounded improvement:** Those comparisons now produce an evidence note
naming the treated run, how many cells were paired, which cells, and how
many lost, gained, or kept reactivity. The note says three paired cells
are required and that no destroyed, enhanced, or resistant pattern was
assigned. Explain Analysis, Summary, suggestions, the clinical report,
and the Special Panels tab show the note. Nothing is identified.

**Not in this tick:** A same-panel comparison with three or more paired
cells that still fits no pattern. Authenticated vendor portals stay later
and must not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 49 (this change)

**Deficiency:** A ficin, papain, or DTT comparison with three or more
paired cells was silent when the counts missed every pattern threshold.
One cell that lost reactivity, one that gained it, and one that kept it
was indistinguishable from a comparison that was never reviewed.

**Bounded improvement:** Those comparisons now produce an evidence note
naming the treated run, the paired cells, and how many lost, gained, or
kept reactivity. The note states the destroyed, enhanced, and resistant
thresholds and says no pattern was assigned. A one-loss two-persistence
comparison still stays a partial destroyed pattern. Explain Analysis,
Summary, suggestions, the clinical report, and the Special Panels tab
show the note. Nothing is identified.

**Not in this tick:** Naming cells that were tested on only one of the
two runs. Authenticated vendor portals stay later and must not bypass
auth, CAPTCHA, licensing, or terms.

## Iteration 50 (this change)

Product review: iterations 44–49 all refined enzyme and DTT comparisons.
This tick moves to specimen and patient context, which the workflow
starts from.

**Deficiency:** Recent transfusion was detected only by keywords in the
specimen notes or phenotype. "Received 2 PRBC" was missed, so a likely
donor-cell E+ phenotype argued against anti-E. "No transfusion history"
was flagged as transfused. No last-transfusion date was recorded, and
the window was not configurable.

**Bounded improvement:** Specimens now store a transfusion status and a
last-transfusion date. The status column has a database CHECK
constraint, and changes are audited and blocked after a final call.
Preferences has a recent-transfusion window (1–365 days, default 90),
which is saved with each analysis snapshot. Analysis uses the structured
status first. Transfused within the window, with no date, or with a date
after the specimen makes the phenotype uninterpretable. A date outside
the window leaves the phenotype usable. The note keywords decide only
when the status is Unknown. Under Not transfused, transfusion wording in
the notes is named for review. Each explanation states the date, the day
count, and the window. Summary, suggestions, the specimen detail, and
both specimen reports show it. Nothing is identified.

**Not in this tick:** Product counts and types, pregnancy history,
HSCT/BMT, mixed-field concern, and connecting transfusion context to
mixed-field reaction notes. The legacy keyword check remains as the
fallback for Unknown.

## Iteration 51 (this change)

**Deficiency:** A mixed-field autocontrol produced no note, because the
special-grade notes skip the AC row. After a recent transfusion that
result is the classic sign of alloantibody coating surviving donor
cells (possible delayed serologic or hemolytic transfusion reaction).

**Bounded improvement:** Every autocontrol phase graded MF, including
extra phases, is named once per row and read against the transfusion
assessment from iteration 50. The note covers four cases: recent
transfusion (suggest a DAT and an eluate), Not transfused (review
history, transplant, and the specimen), Transfused outside the window
(donor cells less likely), and Unknown (record the status). Panel-cell
MF is unchanged. Suspected and ruled-out lists are unchanged. The note
reaches Summary, Explain Analysis, suggestions, and reports through the
existing special-reaction notes.

**Not in this tick:** DAT has no mixed-field option, and DAT IgG/C3 are
not recorded separately. Eluate results are not structured.

## Iteration 52 (this change)

**Deficiency:** The DAT was one polyspecific value with no IgG or C3
result and no mixed-field grade. Pattern notes could not tell a warm
IgG picture from C3-only reactivity. A mixed-field DAT after
transfusion could not be recorded.

**Bounded improvement:** Specimens store IgG and C3 DAT results beside
the polyspecific value. MF is now a DAT grade. A `DatProfile` makes the
DAT positive if any recorded result is reactive, and negative only when
every recorded result is negative. Autoantibody-favoring pattern notes
add a cold or complement caveat when only C3 reacts. A mixed-field DAT
gets the donor-cell note from iteration 51. IgG and C3 changes are
audited, validated against the grade list, and locked after a final
call. The dialog, specimen detail, and reports show the full DAT.
Polyspecific-only wording is unchanged.

**Not in this tick:** Eluate results are not structured, and DAT history
across specimens is not tracked by patient (there is no patient entity
yet).

## Iteration 53 — product review and patient link (this change)

**Product review.** Complete: panel import with review, reaction entry,
transparent analysis with Explain Analysis, final call with audit and
lock, structured transfusion and DAT context. Fragmented: everything is
keyed by accession, with no patient identity, so history does not carry
between specimens. No investigation object or status workflow exists
beyond the Worklist categories. Analysis explanations are strong, but
iterations 44–49 went deep on one niche (enzyme/DTT) while patient and
specimen basics lagged. The roadmap below re-weights toward patient and
investigation workflow.

**Deficiency:** A confirmed antibody (for example an evanescent anti-Jka)
did not reach the patient's next specimen unless someone retyped it into
Previous antibodies.

**Bounded improvement:** Specimens carry an indexed, audited MRN, locked
after a final call. Analysis adds each antibody confirmed on another
specimen with the same MRN as Historical evidence, citing the earliest
accession and date, without duplicating Previous antibodies. Unconfirmed
suspected lists are not carried. The detail pane, Specimen Summary, and
worksheet list the patient's other specimens. The filter matches MRN.

**Not in this tick:** A patient table (name, DOB, permanent blood-bank
comments, special transfusion requirements) and warning when two
specimens share an MRN but disagree on ABO/Rh or phenotype.

## Roadmap after review

1. Patient record: name, DOB, permanent comments, special transfusion
   requirements (antigen-negative, irradiated), shown on every specimen
   with that MRN.
2. Investigation status workflow (created → testing → analysis → review
   → completed/amended) with a visible step indicator per specimen.
3. Eluate results as structured data feeding analysis.
4. Product counts and types, pregnancy, HSCT/BMT on the transfusion record.
5. Enzyme/DTT: name cells tested on only one run.

## Iteration 54 (this change)

**Deficiency:** The MRN linked specimens, but there was nowhere to keep
permanent patient information. Nothing checked whether a known
antibody's antigen-negative requirement was recorded.

**Bounded improvement:** A `patients` table keyed by MRN holds name,
date of birth, permanent blood bank comments, and special transfusion
requirements. Add and update are audited with before and after values.
Future or invalid dates of birth are refused. The Specimens tab shows
the patient in its own box, separate from specimen context, with Edit
Patient. Specimen Summary and the worksheet print it. Analysis collects
antibodies known for the MRN (Previous antibodies, this specimen's
confirmed call, confirmed calls on other specimens). Each one whose
antigen-negative requirement is not written on the patient record
gives a REVIEW suggestion. Requirement matching is case-sensitive
(C vs c) and accepts `K-negative`, `K neg`, `K−`, and `Fy(a-)`.

**Not in this tick:** Requirements are free text, not a structured
list. Encounter, location, ordering provider, sex, and pregnancy history
are not recorded. There is no patient search screen beyond the
specimen filter.

## Iteration 55 (this change)

**Deficiency:** Where an investigation stood had to be pieced together
from panel links, the reaction grid, analysis time, and the final call.
A cleared and re-confirmed identification looked the same as one never
changed.

**Bounded improvement:** `InvestigationStatusService` derives five steps
and a status from recorded data. No status is stored, so it cannot
drift. Amended means a confirmed identification was cleared and
confirmed again (from the audit log). Reopened means cleared and not yet
re-confirmed. The next action names incomplete panels with
entered/total counts. The Specimens detail pane shows a status strip,
and the worksheet prints the status line. The Worklist now uses the same
`GetIncompleteReactionPanels` check.

**Not in this tick:** Separate technologist and supervisor review steps,
an explicit Cancel action with reason, and a status column in the
specimen list.

## Iteration 56 (this change)

**Deficiency:** The Search tab only searched reagent cells by antigen
profile. A patient's earlier workups could not be found by MRN or name,
and there was no way to list every anti-Jka identification or every
call by one technologist.

**Bounded improvement:** The Search tab now opens on **Investigations**.
`InvestigationSearch` matches accession, MRN, patient name and DOB,
final and previous antibodies, final comment, and technologist
initials. Every word must match. An optional inclusive date range can be
used alone. Rows show the derived status and sort newest first. Open
(or double-click) goes to the specimen. Empty searches and reversed date
ranges are refused with a message. The antigen-profile search moved
unchanged to its own section.

**Not in this tick:** Searching by panel lot or manufacturer used in an
investigation. The specimen-list filter still matches MRN but not
patient name.

## Iteration 57 (this change)

**Deficiency:** A confirmed identification was final the moment one
person entered it. There was no second-person review, no queue of calls
awaiting review, and nothing on the report showing sign-off.

**Bounded improvement:** A Preferences option, *Require supervisor
review*, adds a sixth status step. `SetSpecimenSupervisorReview` records
reviewer initials and time on the specimen. It refuses a review when
there is no confirmed call, when the call was already reviewed, when
initials are blank, or when the reviewer is the person who confirmed the
call. Each review is audited with the confirmed antibodies. Confirming
or clearing the call removes any earlier review. Status shows *Awaiting
supervisor review* until it is done. The Worklist adds a Supervisor
review queue. The worksheet prints the reviewer or *PENDING*.

**Not in this tick:** Role-based permissions (initials are not
authenticated users), a Cancel action with reason, and a turnaround
report for review time.

## Iteration 58 (this change)

**Deficiency:** Reconstructing an investigation meant reading the raw
audit log, run records, and analysis snapshots separately. Attaching or
removing a panel was not audited at all.

**Bounded improvement:** Panel link and unlink are now audited with the
panel name and lot, and an index on audit entity lookups was added.
`InvestigationTimeline` builds one ordered list from receipt, runs
(untreated or additional testing), reaction saves (grouped per run when
less than 15 minutes apart, with save and cell counts), each analysis
snapshot with operator and engine version, confirm, clear with reason,
supervisor review, MRN, transfusion, and DAT edits, and the patient
record's audit. Only this specimen's events appear. The Specimens tab
has a Timeline expander, and the worksheet prints the timeline.

**Not in this tick:** Report printing or export is not yet audited, so
it does not appear. Panels linked before this change show only their
run start date.

## Iteration 59 — product review (this change)

**Product review.** Iterations 50–58 moved the product from
accession-only records to a patient-linked investigation workflow:
structured transfusion and DAT data, MRN and patient record, derived
status with supervisor review, investigation search, and a timeline.
The app was renamed back to HemoIQ by merging the existing rename
branch. While doing that, the review found the most serious open
problem: **the test suite wrote to the user's real data.** The log
was written on every run, and a live vendor-download test imported
downloaded vendor lots into `%AppData%\HemoIQ\hemoiq.db` (and a
database beside the Release exe) with `replaceExisting: true`. It also
rewrote antigen order on other imported panels from the same vendor.
On this machine that added vendor panels 17, 19, and 21–24 and deleted
18 and 20, which were themselves earlier test imports. None were linked
to a specimen or had runs.

**Bounded improvement:** `AppDataPaths.RootOverride` lets the test
project point the data root (default database, settings, logs,
artifacts) at a per-run temp folder through a module initializer. The
live vendor test persists only to its isolated database. New tests
assert the root, log directory, settings path, and default database all
resolve under the test folder. A full run was checked against the real
%AppData% file timestamps and sizes, and nothing changed.

**Not in this tick:** The six test-imported vendor panels remain in the
user's database pending the user's decision. One intermittent test
failure appeared once and could not be reproduced.

## Roadmap after review

1. Audited report generation (print/PDF) so reports appear on the timeline.
2. Structured eluate results feeding analysis.
3. Cancel action with reason, and turnaround metrics from the timeline.
4. Role-based permissions (initials are not authenticated users).

## Iteration 60 (this change)

**Deficiency:** Printing and exporting a report left no record. Nobody
could tell which identification a report carried or whether a report
was issued before the identification was later amended.

**Bounded improvement:** `ReportService` writes a `report_output` audit
event after each successful PDF export, CSV export, or print. The event
holds the report type, output, file name, SHA-256 of the issued
content, software version, and, for specimen reports, the confirmed
identification and reviewer at issue. Specimen reports attach to the
specimen, panel reports to the panel, and list reports to `report`.
The timeline shows each issue. When the latest Clinical Identification
report predates the latest clear, an Amended investigation's next
action is to reissue the report, until a newer report is issued. A
failed export writes nothing.

**Not in this tick:** Printing has no confirmation that the printer
completed the job (the event is written when the job is handed to
Windows). There is no Worklist queue for reports needing reissue.

## Iteration 61 (this change)

**Deficiency:** An eluate could be entered as a specimen of type eluate,
but nothing tied it to the patient specimen it came from. Eluate
findings, which are central to recognizing delayed transfusion reactions
and warm autoantibodies, never reached the source investigation.

**Bounded improvement:** `specimens.source_accession` (indexed) links an
eluate to its source. `SetEluateSource` refuses non-eluates,
self-links, missing sources, and eluate sources, is audited, and is
locked after the eluate's final call. `EluateEvidence` counts graded and
reactive cells on each linked eluate's untreated runs and adds one note
per eluate to the source analysis. The note covers: not yet tested;
specificity (confirmed preferred over suspected), worded as possible
DHTR/DSTR after a recent transfusion and naming antibodies not suspected
in the source; panreactive (at least 80% of four or more cells); or
nonreactive with a positive DAT, explained for a C3-only DAT. The dialog
has *Eluate of*. The detail pane shows the link both ways, and the
timeline shows link changes.

**Not in this tick:** Elution method and last-wash control are not
recorded. Eluate antigen-negative rule-outs are not used for the source.

## Iteration 62 (this change)

**Deficiency:** An investigation could not be cancelled. A mislabeled
or unacceptable specimen could only be deleted, which loses the record,
or set inactive, which is meant for expiry and leaves the specimen
editable and on work queues.

**Bounded improvement:** `CancelInvestigation` and
`ReinstateInvestigation` each require a reason and are audited.
Cancelling is refused while an identification is confirmed or when
already cancelled. The lock (`EnsureSpecimenUnlocked`, analysis save,
and confirming a final call) now also covers cancelled records, with
its own message. Status shows Cancelled with date and reason. The
Worklist skips cancelled specimens. The timeline shows both events.
Confirm ID on a cancelled record shows a message instead of an
unhandled error.

**Not in this tick:** Cancel reasons are free text (no configurable
reason list). Turnaround metrics from the timeline remain.

## Iteration 63 (this change)

**Deficiency:** Analytics only counted confirmed antibodies. There was
no measure of investigation turnaround time and no view of open
investigations that were running late.

**Bounded improvement:** `TurnaroundService` is a reporting-only
service, separate from workflow code. For investigations received in
the Analytics date range, with cancelled ones excluded, it computes
received→confirmed in days and confirmed→reviewed in hours, each as a
median and nearest-rank 90th percentile with a count. It also lists
active open investigations older than the new Preferences turnaround
threshold (1–60 days, default 3), oldest first, with their derived
status. Analytics shows three cards and an overdue list with Open.
Export CSV appends the figures and the list.

**Not in this tick:** A received time (only the date is recorded),
workload by technologist, and a Worklist queue for overdue
investigations.

## Iteration 64 — product review and specimen timing (this change)

**Product review.** Iterations 60–63 completed audited report output,
eluate evidence, cancel/reinstate, and turnaround metrics. The weakest
remaining part of the specimen workflow was timing. Specimens had only
a received date set at record creation and no collection or receipt
time. Expiration defaulted from *today* instead of from collection.
Nothing checked dating against recent transfusion. That is the
highest-value gap and ties to the iteration-50 transfusion record.

**Bounded improvement:** Specimens carry collection and receipt
date-times. They are validated (not in the future, receipt not before
collection), audited, locked after a final call, and on the timeline.
On a new specimen, receipt defaults to now and choosing a collection
date sets expiration from collection. `SpecimenDating` raises a REVIEW
(Specimens pane, top of analysis suggestions, worksheet) when the
patient was transfused within a configurable look-back (default 90
days) and the expiration is later than collection + dating days, or
when it cannot be checked because the collection time or expiration is
missing. Turnaround uses the recorded receipt time.

**Not in this tick:** Pregnancy within 3 months as a dating trigger
(no structured pregnancy field yet), collector and location.

## Roadmap after review

1. Structured pregnancy history on the patient record (also a dating trigger).
2. Role-based permissions (initials are not authenticated users).
3. Workload by technologist on Analytics.

## Iteration 65 (this change)

**Deficiency:** Pregnancy could only be written in notes. It did not
trigger the specimen-dating check, although pregnancy within 3 months
triggers that rule just as transfusion does. RhIG was not recorded, so
anti-D in a recently treated patient looked the same as an immune
alloantibody.

**Bounded improvement:** Specimens carry pregnancy status (Unknown,
Not pregnant, Pregnant, Recently pregnant with a required end date) and
an RhIG date. The status column has a CHECK constraint, and changes are
validated (no future dates), audited, locked after a final call, and on
the timeline. `SpecimenDating` now applies when the patient is
pregnant, or the pregnancy ended within the look-back, and names every
reason. When anti-D is suspected or confirmed and RhIG was given within
the configurable window (default 84 days) before collection, analysis
raises a REVIEW for possible passive anti-D. The dialog, detail pane,
and worksheet show pregnancy and RhIG.

**Not in this tick:** Gravida/para counts, anti-D titer tracking across
pregnancies, and HSCT/BMT context.

## Iteration 66 (this change)

**Deficiency:** Anyone at the workstation could record a supervisor
review under any initials, change analysis Preferences, clear a
confirmed identification, or reinstate a cancelled investigation.

**Bounded improvement:** A `users` table keyed by Windows login holds
initials, name, and role (Technologist, Supervisor, Administrator), with
CHECK constraints. `AccessControl` gates actions in the service layer:
supervisor review, clearing an identification, and reinstating require
Supervisor or Administrator; Preferences and user management require
Administrator. While no user is registered, everything stays allowed,
so existing installs keep working. With users registered, review
initials must be the signed-in user's own, unregistered logins are
refused protected actions with a clear message, the first user must be
an Administrator, the last active Administrator is protected, and
initials are unique. User changes are audited, and every audit entry
records the signed-in login. Preferences has **Users & roles…**.

**Not in this tick:** A read-only role, per-lab configurable
permissions, and password or second-factor sign-in (identity is the
Windows login).

## Iteration 67 (this change)

**Deficiency:** Analytics showed turnaround for the lab as a whole but
not who did the work. A supervisor could not see how confirmations and
reviews were spread across technologists, or whose cases took longest.

**Bounded improvement:** `WorkloadService` is a reporting-only model
over specimens. For each set of initials it counts confirmed
identifications by confirmed date (Identified by), confirmations
naming more than one antibody, and supervisor reviews by review date
(Reviewed by), with the median received-to-confirmed time. Initials
match case-insensitively; names come from Users and roles when the
initials are registered. Cancelled investigations, unconfirmed cases
and blank initials are excluded, and a confirmation recorded before
receipt is counted but left out of the median. Analytics shows a
**Workload by technologist** table beside the overdue list, and
**Export CSV** appends it.

**Not in this tick:** Reaction-entry and amendment counts per user
(audit entries record the Windows login, so they can be joined only
when users are registered), and workload by location (no location
field yet).

## Product review (after iteration 67)

**Complete enough to rely on:** specimen receipt through confirmation
(status strip, worklist, timeline), transfusion/pregnancy/DAT context
feeding analysis as named REVIEW notes, supervisor review with roles,
cancel/reinstate, eluate linking, report-output audit, and
turnaround/workload analytics.

**Still fragmented:**
- Specimen origin (collector, locations, provider) was not recorded,
  so reports could not show where a specimen came from and workload by
  location was impossible. Addressed in iteration 68.
- Patient transfusion requirements are free text; the requirement
  check relies on keywords. A structured antigen-negative requirement
  list would make the check exact.
- Reaction entry and amendments per user cannot be counted by
  initials because the audit log stores the Windows login.
- PDF import remains layout-fragile; vendor portals stay file-only.

**Screens growing complex:** the Specimen dialog now has 21 rows. It
should be split into Specimen and Patient context sections (or tabs)
before more fields are added.

**Testing gaps:** no UI automation; dialog validation is covered only
through the service methods it calls.

## Roadmap after review

1. Specimen origin and workload by ordering location (iteration 68).
2. Structured antigen-negative transfusion requirements on the patient record.
3. Split the Specimen dialog into sections.
4. Per-user reaction-entry and amendment counts from the audit log (registered users).

## Iteration 68 (this change)

**Deficiency:** Specimens had no collector, collection location,
ordering location, or ordering provider. The worksheet could not say
where a specimen came from, Search could not find cases by unit or
provider, and Analytics could not show workload by location.

**Bounded improvement:** Four optional specimen columns, each with a
database CHECK of 128 characters. `UpdateSpecimenOrigin` trims and
collapses whitespace, writes an `update_origin` audit with before and
after values only when something changed, and is locked after a final
call. The Specimen dialog has **Collected by / at** and **Ordered by /
from**; the detail pane, timeline, and worksheet show the origin, and
Search matches it. Analytics has a **By ordering location** tab
(received, confirmed, open, median TAT; cancelled excluded; unrecorded
last), included in Export CSV.

**Not in this tick:** A configured location list (values are free
text, so spelling variants count separately), and location on the
patient record.

## Iteration 69 (this change)

**Deficiency:** Antigen-negative requirements lived only in free text,
and the requirement check found them with a keyword pattern. A typo,
an unusual phrasing, or C written for c could make the check pass or
fail silently.

**Bounded improvement:** Patients carry a structured **Antigen-negative
units** list (`patients.antigen_negative`), stored as canonical codes
in panel order. Entry accepts commas, semicolons or spaces, a trailing
-, neg or negative, and Fy(a-) notation; case decides C/c, E/e, K/k,
S/s, and other antigens match case-insensitively only when one fits.
Anything that is not a known antigen is refused by the dialog and by
`SavePatient`, so the list never holds an unreadable entry. Saves are
audited (re-entering the same set differently is not a change). The
requirement check uses the structured list exactly. A requirement
written only in the free text still raises a REVIEW asking the user to
add it to the list; existing records are never rewritten
automatically. The detail pane and worksheet print E−, K−, Fy(a−).

**Not in this tick:** A one-click "add to patient record" action from
the REVIEW note, requirement source and date, and product-level
requirements (irradiated, CMV-safe) as structured fields.

## Iteration 70 (this change)

**Deficiency:** The Specimen dialog had grown to 21 rows in one column.
Transfusion, pregnancy, DAT, and typing fields sat between identity and
notes, so users scrolled past fields they did not need and could miss
the ones that change analysis. Validation errors did not say where the
field was.

**Bounded improvement:** The dialog has four tabs: Specimen (identity,
collection, origin, expiration, Active), Clinical context (transfusion,
pregnancy, RhIG, with a note on how they affect analysis), Serology
(DAT, phenotype, genotype, previous antibodies), and Notes. A
`SpecimenContextSummary` line under the tabs lists every recorded
context and serology value as the user types, leaving out Unknown, Not
transfused, Not pregnant, and NT, so nothing on a closed tab is hidden.
Each validation error opens the tab holding the field and focuses it;
pregnancy and RhIG future dates now have separate messages. Saved data
and the save sequence are unchanged.

**Not in this tick:** Saving the specimen's fields in one transaction
(the view model still calls several audited update methods in turn, so
a later failure can leave earlier fields saved), and remembering the
last tab.

## Iteration 71 (this change)

**Deficiency:** Saving the Specimen dialog ran eight audited updates in
turn. If a later one was refused (an eluate source that does not exist,
received before collected), the earlier ones stayed saved. A new
specimen could be created half-filled, and the audit log recorded
changes the user was told had failed.

**Bounded improvement:** `DatabaseService.RunInTransaction` wraps work
in a SQL savepoint, so every existing update method takes part without
change and calls may nest. `SpecimenSaveService.Save` runs the whole
dialog save inside it, for both add and edit. A refused step rolls back
the specimen row, every field, and every audit entry from that save. The
message now says "Nothing was saved" / "No changes were saved". Each
step keeps its own validation and audit.

**Not in this tick:** Other multi-step writes (panel import with cells,
patient save plus MRN link) still run step by step and should move to
`RunInTransaction`.

## Iteration 72 (this change)

**Deficiency:** Replacing an already-stored lot during import deleted
the old panel first and then inserted the new one step by step. A
failure part-way left the lot missing. Worse, `DeletePanel` cascades to
specimen links and panel runs, and runs cascade to reactions, so
deleting or replacing a lot that had been used for testing silently
erased patients' recorded reactions. Panel copy and cell replacement
were also multi-step without a transaction.

**Bounded improvement:** `GetPanelUsage` reports linked specimens,
recorded reaction rows, and linked specimens with a confirmed
identification. `DeletePanel` refuses a panel with reactions or a
confirmed identification ("Deactivate it instead"), and the audit
entry of an allowed delete lists the links it removed. Vendor and lab
imports refuse to replace a stored lot that holds test history and
otherwise run delete-plus-import in one `RunInTransaction`, so a
failure keeps the stored lot. `ReplacePanelCells` and Copy Panel are
transactional. The Panels Delete button shows the refusal instead of
failing. A `RunInTransaction<T>` overload returns a value.

**Not in this tick:** A source artifact saved to disk before a failed
import is left in the artifact folder (content-addressed, harmless,
not referenced). Specimen deletion and archive purge were not reviewed
in this pass.

## Iteration 73 (this change)

**Deficiency:** Deleting a specimen wrote no audit entry and was allowed
even with a confirmed identification, erasing the reactions, analysis,
and final call by cascade. It could also leave eluates pointing at a
missing source. Purge Database could permanently delete confirmed
identifications with no archive, and any user could run it.

**Bounded improvement:** `DeleteSpecimen` refuses a specimen with a
confirmed identification (cancel the investigation or clear the
identification first) and one named as an eluate source. Allowed
deletions run in one transaction with a `delete_specimen` audit entry
listing the MRN, type, received date, linked panels, reaction rows, and
cancelled state. Purge needs the new `PurgeData` permission
(Administrator once users are registered), refuses to run without an
archive when any matching specimen has a confirmed identification, and
deletes and audits in one transaction before vacuuming. The Specimens
Delete button shows the refusal.

**Not in this tick:** A purge can still remove an eluate's source when
the eluate is newer than the cutoff (the eluate keeps the accession as
text). Per-specimen delete has no reason prompt.

## Iteration 74 (this change)

**Deficiency:** Every reaction save wrote an audit entry with only the
new value, even when nothing changed. A changed result could not be
told apart from first entry, the original value was lost from the
audit trail, and per-user counts would have been inflated by repeated
grid saves. Workload covered confirmations and reviews by initials only.

**Bounded improvement:** `SaveReaction` compares with the stored row:
unchanged saves write nothing; changes record the previous phases and
extra phases as the audit before-value. The timeline names how many
recorded results changed in each entry session. `UserActivityService`
counts, per Windows login and local date range, reaction cells entered,
recorded results changed, specimens touched, identifications confirmed,
identifications cleared, and supervisor reviews, with registered names.
Analytics shows it on an **Activity by user** tab and Export CSV
includes it.

**Not in this tick:** Reaction-change reasons (a change is recorded but
no reason is asked), and per-user counts for panel and rule edits.

## Iteration 75 (this change)

**Deficiency:** A recorded reaction result could be changed after
analysis with no reason, so the audit log showed what changed but not
why. The grid also saved cell by cell, so a failure part-way left a mix
of old and new grades. Iteration 74's change count also treated a
stored empty extra-phase object as a recorded result, overcounting
changes.

**Bounded improvement:** `DescribeReactionChanges` lists each recorded
(non-NT) phase a save would replace ("Cell 3 AHG: 2+ → 0").
`SaveReactions` saves a run's grid in one transaction; once the
specimen has been analyzed, it refuses changes to recorded results
without a reason (`ReasonRequiredException`, nothing saved). The grid
shows the list and asks for the reason; the reason is stored only on
the audit entries that replaced a result, and the timeline shows it.
First entries never need a reason. Change detection now reads extra
phases properly.

**Not in this tick:** Paste/fill and Add Run copy still use the
single-cell save (they write new runs or empty cells), and a reason is
not asked before analysis has run.

## Product review (after iteration 75)

**Method:** every public write method in `DatabaseService` was checked
for an audit entry and for protection of test history.

**Now complete:** specimen save, panel import, panel delete, specimen
delete, purge, and reaction-grid save are all-or-nothing and audited;
lots and specimens that hold test history cannot be deleted; recorded
reaction results changed after analysis need a reason; workload and
activity are reported by initials, location, and login.

**Writes still without an audit entry (ranked):**
1. Panel cell typing edits (`UpdatePanelCell`): editing a lot used for a
   confirmed identification silently changed the evidence behind it.
   Addressed in iteration 76.
2. Run and reaction deletion (`DeletePanelRun`, `DeleteReactions` from
   the Reactions tab): removes recorded results with no audit entry.
3. `UpdateSpecimen` (notes, phenotype, genotype, previous antibodies,
   expiration, type) and `AddSpecimen`: no before/after values.
4. `SetSpecimenActive`, `RemovePanelExtraAntigen`, `SetPanelAntigenOrder`.
5. Analysis outputs (`AddSpecimenAntibody`, rule-outs) are regenerated
   by analysis and covered by analysis snapshots; acceptable.

**Screens:** Analytics now has three workload tabs and stays readable;
the Specimen dialog is tabbed. Reactions tab Clear and Delete Run are
the remaining one-click destructive actions on clinical data.

## Roadmap after review

1. Panel typing edits audited and reason-gated (iteration 76).
2. Audit and protect run/reaction deletion (refuse after a confirmed
   identification; reason after analysis).
3. Audit `UpdateSpecimen` with before/after values and `AddSpecimen`.
4. Audit activation, extra-antigen removal, and antigen order changes.

## Iteration 76 (this change)

**Deficiency:** Editing a panel's antigen typing wrote every cell with
no audit entry, even for a lot used for confirmed identifications.
Analysis reads typing live, so a later re-analysis or reprint of those
investigations would silently use different evidence.

**Bounded improvement:** `DescribePanelCellChanges` lists each typing or
cell-metadata difference ("Cell 3 Jka: + → -"). `SavePanelCellEdits`
saves only real changes in one transaction and writes an
`edit_panel_typing` audit entry with the change list. When the lot holds
test history, a reason is required (`ReasonRequiredException`
otherwise) and the audit entry names the linked specimens. The Panels
Save button lists the changes and asks for the reason; every linked
specimen's timeline shows the edit and its reason.

**Not in this tick:** Extra-antigen removal and antigen display order
are still unaudited (roadmap item 4).

## Iteration 77 (this change)

**Deficiency:** Clear (all reactions on a run) and Delete Run removed
recorded results with no audit entry and only a Yes/No prompt, even
after analysis had used them. The deleted grades could not be
recovered or reviewed.

**Bounded improvement:** `DeleteReactions` and `DeletePanelRun` run in
one transaction and write `clear_reactions` / `delete_run` audit
entries holding every deleted grade, the panel and run name, and the
number of graded cells. After analysis, deleting graded cells requires
a reason (`ReasonRequiredException` otherwise); the Reactions tab asks
for it in place of Yes/No. Clearing an empty run writes nothing.
Deletion stays refused after a confirmed identification. The timeline
shows both actions with the reason.

**Not in this tick:** Restoring deleted grades from the audit snapshot
(they are kept and readable in the audit log, not one-click
restorable).

## Iteration 78 (this change)

**Deficiency:** Creating a specimen and editing its type, expiration,
Active flag, notes, phenotype, genotype, previous antibodies, or
polyspecific DAT wrote no audit entry, so a changed phenotype or a
removed "previous anti-K" left no trace. Every dialog save rewrote these
fields whether or not they changed.

**Bounded improvement:** `AddSpecimen` writes `add_specimen`.
`UpdateSpecimen` compares with the stored record, writes nothing when
nothing changed, and otherwise writes one `update_specimen` entry with
before and after values of only the changed fields; a locked specimen is
refused only when something would change. `SetSpecimenActive` is audited
when the value changes. The timeline shows "Specimen details changed:
notes, phenotype" and active/inactive changes.

**Not in this tick:** Roadmap item 4 (panel activation reasons,
extra-antigen removal, antigen order).

## Iteration 79 (this change)

**Deficiency:** Adding an extra (warehouse) antigen to a panel filled
every cell with "-", a typed negative, so a column nobody had typed was
analyzed as antigen-negative on every cell. Removing an extra antigen
deleted its typing with no audit entry, even on lots used for testing,
and antigen order changes were unaudited.

**Bounded improvement:** New extra-antigen cells start as NT, so the
antigen is unknown until typed (analysis already skips untyped cells).
`add_extra_antigen` is audited when the column is new.
`RemovePanelExtraAntigen` refuses to remove an antigen typed on any
cell of a lot that holds test history; otherwise it runs in one
transaction with a `remove_extra_antigen` entry holding the removed
values. `SetPanelAntigenOrder` audits a change to a previously saved
order. The Panels tab shows the refusal. Three tests that relied on the
negative default now type the values they need.

**Not in this tick:** Existing panels already carry "-" for extra
antigens added before this change; they are not rewritten
automatically, because some may be real typing. A data-quality check
listing extra-antigen columns that are "-" on every cell would let a
supervisor review them.

## Iteration 80 (this change)

**Deficiency:** Panels that gained an extra antigen before iteration 79
still hold "-" (typed negative) on every cell for that column. Some may
be real typing, some the old automatic default; analysis cannot tell,
and nothing pointed a person at them.

**Bounded improvement:** `FindUnverifiedAllNegativeExtraAntigens` lists
extra-antigen columns that are "-" on every non-AC cell (two or more)
with no `add_extra_antigen`, no audited typing edit for that antigen,
and no `verify_extra_antigen` entry. Each appears on the Worklist as
**Check typing** (opens Panels). **Mark typing verified** records a
`verify_extra_antigen` audit entry (supervisor permission once users are
registered) and clears the item without changing typing; an audited
Edit also clears it. Nothing is rewritten automatically.

**Not in this tick:** A count card for data-quality items on the
Worklist header, and other data-quality checks (duplicate lots, panels
with no typed antigens).

## Iteration 81 (this change)

**Deficiency:** Reactions could be recorded on a panel lot after its
expiration date with no warning, and analysis and reports used them
like any other results. Testing with expired reagent red cells is a
quality-system nonconformance that should be visible and reviewed.

**Bounded improvement:** `ExpiredReagentCheck` finds runs with graded
results whose last recorded save (from the audit log, or the run date)
is after the lot's expiration date. Analysis adds a REVIEW note per run
("recorded 2026-11-05, after the lot expired 2026-11-04 … Not an
exclusion"). The Worklist lists open or unreviewed investigations as
**Expired reagent** (checked only for specimens linked to a lot that
has expired, to keep the Worklist fast). The Reactions tab asks for
confirmation before saving graded results on an expired lot.

**Not in this tick:** The worksheet does not yet print the note outside
the analysis suggestions; a lab-configurable "block" mode is not
offered (warn-and-review only).

## Iteration 82 (this change)

**Deficiency:** When several antibodies were called together, HemoIQ
checked only each one's 3 + 3 rule. It did not check that the reactions
could tell them apart. A pair where every K+ reactive cell was also E+
could be pre-filled as "anti-E; anti-K" with no warning.

**Bounded improvement:** `MultipleAntibodySeparation` takes the antibodies
called together (suspected rows meeting the ID rule, or the Final ID text
at confirmation). For each, it counts reactive cells that carry its antigen
and are typed negative for every other called antigen. A cell whose partner
antigen is NT does not count. The required number is the lab setting
**Multiple-Ab separation**: default 1, range 0–5, 0 is off. Two kinds of
exemption apply:
- **Patient history.** Antibodies in the specimen's previous antibodies, or
  confirmed on another specimen with the same MRN, need no isolated cells
  of their own. Partners still need to be negative for them.
- **Rules tab.** A rule's new "Separation not required" option can list the
  partner antigens it covers, for example anti-f: e, c, or leave the list
  blank to cover every partner.

The result appears in a Suspected Antibodies **Separation** column and in
Suggestions, and as a Yes/No prompt at Confirm ID. The criterion only
warns. The rule engine version is now 2, and the setting and rule fields
are in the analysis snapshot's rule trace.

**Not in this tick:** The clinical report does not print the separation
evaluations. Selected-cell recommendations do not yet rank cells by their
ability to separate the called antibodies.

## Iteration 83 (this change)

**Deficiency:** Iteration 82 left three gaps:
- The Clinical Identification report did not show separation results.
- Selected-cell recommendations did not target a separation shortfall.
- Patient history compared antibody names without regard to case, so a
  prior anti-e could be treated as anti-E. This affected the history
  exemption, the prior-antibody notes, and parsing of the specimen's
  previous antibodies.

**Bounded improvement:**
- The report prints a "Multiple-antibody separation" section. It covers
  the confirmed call once there is one, and otherwise the suspected
  antibodies meeting the ID rule. Shortfalls are marked SHORT, and the
  Suggestions list no longer repeats these lines.
- Separation is now evaluated before selected cells are ranked. An unused
  cell that carries a short antibody's antigen and is typed negative for
  every non-exempt partner gets +6, and its explanation says it would count
  toward the criterion if reactive. Suggestions name those cells.
- `PatientHistory.AntibodyKey` keeps "Anti-E" and "anti-E" together but
  anti-E and anti-e apart. `ConfirmedAntibodies` and
  `PatientTypingParser` previous-antibody parsing dedupe on it with an
  ordinal comparison.

## Iteration 84 (this change)

**Deficiency:** Cells of a manually created panel start typed negative for
every antigen, not NT. If the antigram was never entered, analysis read
every cell as antigen-negative. That fed the ID rule's negative cells, the
separation counts, and the conflicts, and nothing flagged it. Duplicate
lot numbers were checked only on vendor import. Manual add, edit and copy
could store the same lot twice, and the Worklist did not show existing
duplicates.

**Bounded improvement:**
- `PanelIntegrityCheck.UntypedCells` finds non-AC cells with no antigen
  typed positive. Every real cell carries c or C, e or E, and k or K.
- **Untyped cells** Worklist items cover active panels, and analysis adds a
  REVIEW note for any linked panel with untyped cells.
- `DuplicateLotGroups` matches lots ignoring case and surrounding spaces.
  Vendors match ignoring case, and a blank vendor matches any vendor.
- **Duplicate lot** Worklist items cover groups with at least one active
  panel.
- Add, edit and copy on Panels ask Yes/No before saving a lot that is
  already stored.
- No data is changed automatically and there is no schema change.

**Not in this tick:** New panel cells still default to "-" rather than NT.
Changing that default touches every import path, so it is a separate
change.

## Iteration 85 (this change)

**Deficiency (data loss, found while tracing cell creation):**
- `UpdatePanel` deleted and recreated every cell on each details edit
  (name, lot or expiry). That erased all antigen typing, even when the cell
  count did not change and on lots with test history.
- The Edit dialog also passed no special notes, so each edit cleared the
  vendor's special notes.
- Separately, new cells started typed "-" for every standard antigen, so a
  missing antigram read as antigen-negative. Iteration 84 only flagged this.

**Bounded improvement:**
- `UpdatePanel` keeps cells in range with their ids and typing. It adds
  only new cells, as NT, and removes only cells outside the new range.
- It refuses to remove a cell with a real grade, so all-NT rows don't
  count. The check runs before anything changes, and the update runs in a
  savepoint. The audit records `cellsAdded` and `cellsRemoved`.
- Panels → Edit passes the stored special notes through.
- New cells start NT. `AddPanel(startTypedNegative: true)` exists only for
  synthetic data: the demo seeder, and test fixtures that type only the
  antigens under test.
- Imports already wrote NT for antigens they did not read, so they are
  unchanged. Existing stored cells are not altered.
- Entry stays quick: the + / − / N keys set a value directly, and Fill NT
  with − types the NT cells of a column as negative. Both stay unsaved
  until Save, which audits each change.

## Product review (after iteration 85)

I swept every destructive write in `DatabaseService` (DELETE and
INSERT OR REPLACE).

**Already well guarded:**
- Specimen, panel and run deletion, and clearing reactions: refused after a
  confirmed ID, a reason is required after analysis, and every deleted
  grade is audited.
- Purge: an archive is required when confirmed IDs would be lost.
- Unlinking a panel: refused when the specimen is locked.
- Panel edits: fixed in iteration 85.

**Weakest area — analysis rules:**
- No permission. Preferences need an Administrator, but anyone could add,
  edit or delete a rule.
- The audit stored only name and antibody, so an edit lost the old count and
  heterozygous setting, and a delete lost the whole rule.
- A rule change did not mark open analyses out of date.

**Other observations, for the roadmap:**
- Panel Summary does not report untyped cells or duplicate lots.
- The Panels list does not show how completely each lot is typed.
- The full test run takes about 2 minutes. One 3.5-minute run during
  iteration 85 did not recur, so there is no regression.
- Rule and Preference changes don't appear on specimen timelines.

## Iteration 86 (this change)

**Bounded improvement:**
- New `Permission.ConfigureRules` (Supervisor, Administrator), demanded by
  AddRule, UpdateRule and DeleteRule and checked by the Rules tab before a
  dialog opens.
- The rule audit now records every analysis-relevant field: before and after
  on update (an unchanged save writes nothing), before on delete. Deleting a
  missing rule writes nothing.
- `LastAnalysisConfigurationChange()` is the latest add_rule, update_rule,
  delete_rule or update_settings audit, in local time. A Worklist
  **Stale analysis** item now also covers open investigations analyzed
  before that time, and its detail says why. Confirmed investigations are
  not listed.
- No schema change.

## Iteration 87 (this change)

**Deficiency:**
- The Panels list gave no sign of how completely a lot was typed, so
  half-entered antigrams were visible only on the Worklist or in the grid.
- The Panel Summary and All Panels reports printed no data-quality
  findings, so a reviewer signing off a lot on paper could miss them.

**Bounded improvement:**
- `PanelIntegrityCheck.Summarize` counts, over non-AC cells:
  - antigens typed on every cell;
  - antigens typed on some cells but NT on others ("partial", a likely
    entry gap);
  - untyped cells.
- It is shown in a new amber-on-issue **Typing** column on the Panels list,
  which refreshes after a typing save.
- **Panel Summary** prints a Typing line and a "Data quality (review before
  use)" section: untyped cells, partial antigens, and the same lot stored
  on another panel. **All Panels** gains a Typing column.
- No schema change.

## Iteration 88 (this change)

**Deficiency:** The investigation timeline did not show lab configuration
changes. When a rule or Preference changed mid-investigation, the timeline
gave no sign that analyses before and after it were run under different
criteria. The worksheet printed the same incomplete timeline.

**Bounded improvement:**
- `InvestigationTimeline.OpenPeriods` builds the investigation's open
  periods from its own audit trail. They run from receipt until a confirm
  or cancel, and resume after a clear or reinstate.
- Rule add, change and delete, and Preferences changes that fall inside an
  open period now appear on the timeline (and so on the Clinical
  Identification worksheet), with a field-by-field diff from the audit
  JSON. Each entry notes that earlier analyses used the previous setting.
- `DatabaseService.GetAnalysisConfigurationEvents` shares one audit query
  helper with `GetAuditEventsForEntity`.
- No schema change.

## Iteration 89 (this change)

**Deficiency:** Panel Compare listed every per-cell typing difference. A new
lot always differs from the old one, because the donors are different, so an
unfinished antigram entry (Fya left NT on two cells) was lost among the
expected donor differences.

**Bounded improvement:**
- `PanelCompareResult` gains `LeftTyping` / `RightTyping` (the iteration 87
  typing summary) and `CoverageGaps`. These are antigens typed on every
  non-AC cell of the selected lot but not of the comparison lot, with
  counts.
- The explanation names the gaps and the comparison lot's untyped cells.
- The dialog shows both typing summaries above the explanation.
- Antigens the selected lot does not fully type are not flagged.
- No schema change.

## Iteration 90 (this change)

**Deficiency:**
- The import review compared only the antigen column list with the prior
  lot. A vendor sheet with blank cells in a column it normally types fully
  passed review with no mention, even though the blanks become NT, so that
  antigen could no longer be ruled out on those cells.
- The review showed no overall typing summary for the new lot.

**Bounded improvement:**
- `PanelImportReview` gains `TypingSummary` and `CoverageGaps`.
- `PanelImportReviewer.ApplyCoverage` reuses `PanelComparer` (iteration 89)
  on the stored cells of the prior lot and the new lot. Antigens missing
  from every new cell are left out, because they are already reported as
  removed columns.
- The vendor and lab import services both call it, and the explanation
  adds the Typing line and the partly-typed antigens.
- No schema change.

## Iteration 91 (this change) — reaction-entry review

**Review findings:**
- Within Reactions, changing the specimen, panel or run already prompts
  before discarding unsaved grades.
- Fill negatives exists, and NT handling is explicit.
- Two paths bypassed that protection:
  1. **Quitting:** the generic "Quit the application?" said nothing about
     unsaved grades or an open antigram edit, so a misclick lost the work.
  2. **Run Analysis on the Analysis tab:** this analyzed saved grades only,
     while unsaved grades for the same specimen sat on the Reactions tab.
     The result looked current but left out what was just entered, an
     analytical-safety gap.

**Bounded improvement:**
- New `UnsavedWorkGuard` holds the wording and decisions.
- `ReactionsViewModel` exposes `UnsavedAccession`,
  `UnsavedGradesDescription` and `SaveUnsavedGrades()`. The last one runs
  the same expired-lot and change-reason checks as Save.
- Run Analysis prompts Yes (save, then analyze), No (saved grades only) or
  Cancel. If the save is declined or fails, analysis does not run.
- Quit lists each kind of unsaved work, with No as the default.
- No schema change.

**Still open in reaction entry:**
- Pasting a column of grades from a spreadsheet.
- Draft preservation across a crash: grades are only in memory until
  saved.

## Iteration 92 (this change)

**Deficiency:**
- Grades could only be keyed one cell at a time. A lab that records gel or
  solid-phase reads in a spreadsheet or instrument export had to re-key
  every value.
- `ReactionGrade.Normalize` passes unrecognized text through unchanged and
  `IsPositive` treats it as reactive. So any bulk-entry path needs strict
  validation, or a typo becomes a positive reaction.

**Bounded improvement:**
- `GradePaste.Parse` reads one value per line.
  - It accepts only values that normalize to a canonical grade; a blank line
    is NT.
  - It refuses the whole paste on any unrecognized value (with line
    numbers), on more values than cells below the selection, on
    tab-separated multi-column clipboards, and on an empty clipboard.
- `GradePaste.Overwrites` lists entered grades that would change, and the
  user confirms before they are replaced.
- Ctrl+V on a phase column applies the paste from the selected cell down,
  through the same row setters as keyed entry. Grades are dirty-tracked and
  unsaved until Save, and the status bar names the cells filled.
- No schema change.

**Still open:**
- A multi-column paste spanning phases.
- Draft preservation across a crash.

## Iteration 93 (this change)

**Deficiency:** Unsaved grades lived only in memory. A crash, power loss or
killed process lost a whole run of entry. This is the last open item from
the iteration 91 reaction-entry review.

**Bounded improvement:**
- `ReactionDraftStore` keeps one recovery file in
  `%LocalAppData%\HemoIQ\drafts`.
  - It holds the specimen, run, panel, description, operator, time and
    every row's grades, including extra phases.
  - It is written on every grade edit, through an atomic write via a temp
    file, best effort.
  - It is deleted on save, on a deliberate discard, and on a normal quit,
    so a file present at startup means an unclean exit.
- At startup, `OfferDraftRecovery`:
  - discards the draft silently when the run or specimen no longer exists,
    or when nothing differs from the saved grades;
  - lists the grades without restoring them when the identification has
    since been confirmed;
  - otherwise asks, then reopens the run and refills only the changed
    cells. The values pass through `GradePaste.Strict`, so an unrecognized
    grade becomes NT and can never read as reactive. The grades stay
    unsaved and unaudited until Save.
- Drafts never write to the database. No schema change.

## Iteration 94 (this change) — dashboard review

**Review findings:**
- **Antibody frequency table:** it grouped specificities ignoring case, so
  anti-E and anti-e were counted as one. This is the same defect iteration
  83 fixed in patient history, and it is a reporting-correctness problem.
- **No drill-down:** only the overdue list opened the cases behind a
  number. The antibody, technologist and location counts did not, though
  the prompt asks for drill-down from metrics to cases.
- **Cancelled investigations** with a final call still count in antibody
  frequency, but not in workload or turnaround. The drill-down agrees with
  its count, but the two rules differ. Noted, not changed this tick.

**Bounded improvement:**
- `AntibodyAnalyticsService.SpecificityKey` keeps the antigen
  case-sensitive and ignores case only in the "anti-" prefix (via
  `PatientHistory.AntibodyKey`). Other text is grouped ignoring case.
- `ConfirmedInRange` is shared by `Compute` and the new `CasesForAntibody`,
  so a drill-down list always matches its count.
- `WorkloadService.CasesForTechnologist` (confirmations and reviews) and
  `CasesForLocation` apply the same filters as their counts.
- The Analytics tab gets a drill-down panel beside the antibody grid, filled
  by selecting an antibody, technologist or location row. Open or
  double-click goes to Specimens. A range change or Refresh clears the
  panel.
- No schema change.

## Iteration 95 (this change)

**Deficiency:** Antibody frequency counted cancelled investigations that
held a final ID; workload and turnaround did not.

**Finding:** the application already blocks this state in both directions.
`CancelInvestigation` requires the ID cleared first, and
`SetSpecimenFinalCall` refuses a cancelled specimen. So such a record can
only come from older or imported data, or direct database edits. Nothing
surfaced it, and analytics counted it silently.

**Bounded improvement:**
- `AntibodyAnalyticsService.ConfirmedInRange` skips cancelled
  investigations, which gives one rule across all analytics. The
  frequency table, cards, trend and drill-down all inherit it.
- **Inconsistent status** Worklist items list cancelled investigations
  holding a confirmed ID. The item tells the user to reinstate (keeping
  the ID) or clear the ID with a reason, and both paths were verified to
  work in that state.
- No schema change.

## Iteration 96 (this change)

**Deficiency:** Paste took one phase at a time, so gel or tube results
exported as IS / 37 / AHG columns needed three separate pastes.

**Bounded improvement:**
- `GradePaste.ParseBlock` reads tab-separated blocks.
  - Each line is one cell, and each column one phase. Short lines are
    padded with NT.
  - Every value must be a recognized grade, and errors are reported by line
    and column.
  - It refuses more rows than cells below the selection, or more columns
    than phases to the right.
- The Reactions grid maps the columns onto the phases as they appear on
  screen (reordered columns respected, hidden ones skipped), starting at
  the selected phase.
- Replaced grades are listed with their phase and confirmed, and the grades
  stay unsaved until Save.
- The old single-column `Parse` was removed, because a one-column block
  does the same job. Its tests now run against `ParseBlock`.
- No schema change.

## Iteration 97 (this change) — specimen and patient record review

**Review findings:**
Checked against the prompt's patient, specimen and transfusion-context
lists.

**Covered:**
- MRN, name, date of birth, permanent comments, transfusion requirements
  and antigen-negative units;
- history across specimens;
- collection, receipt and origin fields;
- type, expiry and status;
- transfusion status and date;
- pregnancy and RhIG;
- DAT IgG/C3;
- phenotype and genotype;
- eluate links.

**Missing, highest analytical impact:** stem-cell or marrow transplant.
After an allogeneic HPC transplant, the patient's serologic phenotype is
donor or mixed cells, and a genotype from blood or marrow is the donor's.
HemoIQ used both for antigen-negative inferences (Supporting, Against,
Predicted) with no warning. Only transfusion triggered "phenotype
unreliable".

**Still not modelled:**
- visual hemolysis on the specimen;
- testing methodology per specimen (it is recorded per run);
- products and units transfused;
- encounter or account identifiers.

These are candidates, lower in analytical effect.

**Bounded improvement:**
- The patient record gains `HpcTransplant` (none, autologous or
  allogeneic) and `HpcTransplantDate`.
  - Migration: `patients.hpc_transplant` (default none) and
    `hpc_transplant_date`.
  - The type is normalized and the date validated (not in the future). The
    audit records before and after values, and an unchanged save writes
    nothing.
- `TransplantContext.Apply` folds an allogeneic transplant into the typing
  assessment for specimens collected (or, failing that, received) on or
  after the transplant date, or always when the date is unknown. It marks
  the phenotype unreliable and the genotype as possibly the donor's.
  `PatientTypingParser.Evaluate` then reports predicted antigens as
  Uninterpretable, saying when a genotype would still apply.
- An autologous transplant has no effect.
- The Patient dialog gains a type and date. The patient box and the
  Clinical Identification report print it.

## Iteration 98 (this change)

**Deficiency:** The prompt lists visual hemolysis as a specimen property,
and HemoIQ had nowhere to record it. A grossly hemolyzed specimen makes an
H grade, or weak agglutination read through free hemoglobin, hard to
interpret, and analysis gave no warning.

**Bounded improvement:**
- `Specimen.Hemolysis` takes the values not recorded, none, slight,
  moderate or gross (migration: `specimens.hemolysis`).
- `UpdateSpecimenHemolysis` normalizes common wordings. It is audited as
  `update_hemolysis` and shown on the timeline, a no-op save writes
  nothing, and it is locked after a confirmed identification. It is saved
  in the specimen dialog's transaction.
- `SpecimenHemolysis.ReviewNotes`:
  - **Gross:** always adds a REVIEW note.
  - **Slight or moderate:** adds one only when an H grade is recorded,
    naming those cells.
  - Grades are never changed.
- The Clinical Identification worksheet prints the hemolysis level.

**Still not modelled:**
- testing methodology per specimen;
- products and units transfused;
- encounter or account identifiers.

## Iteration 99 (this change) — investigation workflow review

**Review findings:**
- Statuses map to the prompt's list as follows:
  - Created → Awaiting panel;
  - Testing in progress;
  - Analysis available → Awaiting identification (this is the
    technologist-review step);
  - Supervisor review;
  - Completed;
  - Amended / Reopened;
  - Cancelled.
- "Awaiting additional testing" is not derived. It would need the analysis
  result, not just recorded work. Candidate.
- The real defect is that four places each had their own staleness rule:
  the Analysis banner, the status strip, the Pending Work report and the
  Worklist. Only the Worklist (iteration 86) counted rule and Preferences
  changes. The strip could show "✓ Analysis current / Awaiting
  identification" while the Worklist said "Stale analysis", and the
  banner's text always said "reactions".
- Confirm ID did not check staleness at all. An identification could be
  confirmed from an analysis that predated the current grades or
  configuration, or that never ran.
- Seven test classes mutated `AppSettings.Current` outside the
  `PersistentDatabase` collection. That race showed up as an intermittent
  HTLA titration failure.

**Bounded improvement:**
- `AnalysisCurrency.StaleReason` is now the single rule: reactions saved
  after the analysis, or a configuration change for an unconfirmed
  investigation.
- All four places use it, plus `DatabaseService.AnalysisStaleReason`. The
  banner and Pending Work state the reason.
- Confirm ID prompts, with No as the default, when the analysis is stale or
  never ran.
- The seven settings-mutating test classes now run in the serialized
  collection.
- No schema change.

## Iteration 100 (this change)

**Deficiency:**
- The status went straight from "Analysis needed" to "Awaiting
  identification" even when the analysis had left clinically significant
  antibodies unresolved.
- The analysis snapshot did not record the open questions, so status could
  not tell "ready to identify" from "needs more cells".
- While building this I found that "not yet ruled out" cannot be read from
  `RuleoutEvaluations`. An antibody with no antigen-positive nonreactive
  cell has no evaluation at all, though it is the clearest case for more
  testing.

**Bounded improvement:**
- `AdditionalTestingNeeds.Compute` lists four kinds of open item:
  - **Not yet ruled out:** clinically significant antibodies the panels
    type that are not ruled out, identified or suspected. It skips any
    the patient's own reliable typing or genotype argues against.
  - **Identification rule:** suspected antibodies short of it.
  - **Separation:** shortfalls.
  - **Selected cells:** up to three in-date recommendations.
- The list is saved with each analysis (migration:
  `analysis_snapshots.additional_testing_json`). Older snapshots read as no
  needs.
- `InvestigationStatusService` shows **Awaiting additional testing** with
  the needs as the next action when the analysis is current and the list is
  not empty. It is a suggestion; identification is not blocked.
- Two status tests with two-cell fixtures now expect it, which is correct:
  those panels cannot rule out most antibodies.

**Not in this tick:** a Worklist "Awaiting testing" queue. It needs real-lab
judgement on noise first.

## Iteration 101 (this change) — product review of the new status on demo data

**Method:** analyzed the 10 clinical demo investigations and read the
"Awaiting additional testing" items for each.

**Findings:**
- **Signal is reasonable:**
  - 2 cases have no open items (single antibody, everything else ruled
    out).
  - The rest name specific open antibodies, separation shortfalls and
    cells.
  - A Worklist queue for these would hold most open investigations, so it
    stays deferred. The status strip and the existing Suggestions carry
    the information.
- **Wrong direction for patterns:** in 2026-006 (warm, panreactive,
  high-prevalence) the next action listed seven unmet identification
  rules and three selected cells. Single selected cells cannot separate
  specificities when every cell reacts.
- **Anti-f separation:** 2026-007 (anti-e, anti-f, anti-Leb) reports a
  separation shortfall for anti-e and anti-f. f+ cells are always c+ and
  e+, so that separation is impossible by biology. The user asked that
  exceptions come only from patient history or the Rules tab, so no
  built-in exemption was added. A lab should add a Rules-tab exemption
  (anti-f, from e, c). This is noted in help and here.

**Bounded improvement:** `AdditionalTestingNeeds` checks the reaction
patterns first:
- **Panreactive or autoantibody:** a single autocontrol, DAT and adsorption
  item.
- **High-prevalence or HTLA:** rare antigen-negative cells, titration or
  neutralization, with the recommended cells.

**No schema change.**

## Iteration 102 (this change)

**Deficiency:** The transfusion record had only a status and a date. Any
recent transfusion made the serologic phenotype unreliable, including
platelets-only or plasma-only transfusions, which carry few or no donor red
cells. Nothing noted that plasma-containing products can passively
transfer antibody. The prompt asks for "number/type of products where
known".

**Bounded improvement:**
- `TransfusionProducts` codes: red cells, whole blood, granulocytes,
  platelets, plasma, cryo. Red-cell-containing and plasma-containing sets
  are defined.
- `Specimen.TransfusionProducts` and `TransfusionUnits` (migration:
  `specimens.transfusion_products` and `transfusion_units`).
- `UpdateSpecimenTransfusionProducts` keeps products only while the status
  is Transfused and validates units (1–999). It is audited as
  `update_transfusion_products`, shown on the timeline, a no-op save writes
  nothing, and it is locked after confirmation.
- `TransfusionContext.Assess(..., products)`:
  - When the products are recorded and none carry red cells, the phenotype
    stays usable and the explanation says why.
  - Plasma-containing products add a passive-antibody note.
  - Unknown products leave the current behaviour unchanged.
- The specimen dialog has product checkboxes and a units field, with
  validation. `TransfusionDisplay` (detail pane, history, reports) lists
  them.

## Iteration 103 (this change)

**Deficiency:** A specimen had no encounter or account number. The prompt
lists "Encounter/account identifiers" under patient information. Without one,
a technologist cannot search by the visit number on the order, and nothing
catches an encounter number that also appears under a different MRN, which is
a sign of a mislabeled specimen or a mistyped number.

**Bounded improvement:**
- `specimens.encounter_number` (migration, CHECK length <= 64, NOCASE index).
- `UpdateSpecimenEncounter`:
  - removes whitespace, and blank clears the value;
  - is audited as `update_encounter` and appears on the timeline;
  - an unchanged save writes nothing;
  - it is locked after confirmation.
- `GetSpecimensByEncounter` and `EncounterCheck.Describe` warn when the
  encounter is already on another MRN's specimen. Saves from the specimen
  dialog ask for confirmation, and nothing is blocked.
- The specimen dialog has an Encounter box beside MRN, saved in the same
  transaction as the other fields.
- `OriginDisplay` begins with "Encounter X", so the detail pane and the
  worksheet Origin line show it. Search matches it.

## Iteration 104 (this change)

**Deficiency:** "Awaiting additional testing" appeared only on the status
strip of each investigation. The Worklist had no queue for workups waiting on
selected cells, typing or adsorptions, so a case stuck there for days was
visible only on the Analytics overdue table and gave no next step. Listing
every awaiting case had been deferred as too noisy.

**Bounded improvement:**
- `WorklistKind.OverdueAdditionalTesting` and `OverdueTesting.Item`.
- A case is listed only when all of these hold:
  - it has no confirmed identification;
  - it is active and not cancelled;
  - it is older than the Preferences `TurnaroundThresholdDays`, counted from
    `ReceivedAt`, or from the created date when no received time is recorded;
  - its computed status is Awaiting additional testing.
- The detail gives the received date, the age, the threshold and the open
  needs from the latest analysis snapshot. Open goes to Analysis.
- The status is computed only for open cases past the threshold, so the
  Worklist does not compute it for every specimen.

## Iteration 105 (this change)

**Deficiency:** The patient record had no sex. The prompt lists "Sex where
operationally relevant". The relevant case is pregnancy. A specimen could be
recorded as pregnant or recently pregnant for a male patient, which usually
means a wrong-patient specimen or a data-entry error, and nothing noticed.

**Bounded improvement:**
- `PatientSex`: codes unknown, female, male and other. It accepts F, M and X
  and labels them for display.
- `patients.sex` migration: NOT NULL DEFAULT 'unknown', with a CHECK on the
  code.
- `SavePatient` normalizes and compares the sex. It is part of the audit
  snapshot, so changes appear in update_patient with before and after values.
- `Patient.Display` adds the sex label when it is known, so the detail pane
  and reports show it.
- Patient dialog: a Sex combo beside the date of birth.
- `PatientSexCheck`:
  - a REVIEW suggestion in analysis when the specimen records pregnant or
    recently pregnant and the patient is recorded as male;
  - a non-blocking confirmation when saving such a specimen.

## Iteration 106 (this change)

**Correction:** the Iteration 105 "next tick" was wrong. A passive anti-D
review note already exists: `SpecimenDating.PassiveAntiD`, with Preferences
`RhigPassiveWindowDays`, applied in analysis and tested in
`PregnancyContextTests`. No change was needed.

**Deficiency:** Investigation search could not find investigations by the
panel manufacturer or lot they were tested on, both named in the prompt's
search list. After a reagent recall or a suspected lot problem, a
technologist could not list the affected investigations, and the results grid
did not show which panels or encounter each one used.

**Bounded improvement:**
- `DatabaseService.GetPanelsBySpecimen()` builds, in one query, the panels each
  specimen used, from linked panels and panel runs. Search therefore makes
  one query for panels rather than one per specimen.
- `InvestigationSearch` matches panel name, manufacturer and lot. Each row
  gains `Encounter` and `Panels`, for example "Resolve A (Ortho, lot 1234)".
- The Search grid has Encounter and Panels columns. The help describes
  searching for every investigation tested on a lot.

## Iteration 107 (this change): product review, and a rule-matching fix it found

### Review (since Iteration 101)

**Workflows now complete end to end:**
- Specimen receipt with origin, encounter, transfusion products, hemolysis,
  pregnancy and RhIG.
- Panel attach, reaction entry with drafts, analysis, Awaiting additional
  testing (now also an overdue Worklist queue), confirm, supervisor review,
  amend, and reports.
- Search by patient, specimen, encounter, panel manufacturer or lot.

**Still fragmented:**
- Patient history rows do not show the encounter number.
- The Overdue testing queue has no count card or Show toggle.
- The Worklist cards cover only four of its thirteen kinds.

**Screens getting dense:**
- The Specimen dialog (two tabs, 23 rows) and Preferences (22 rows).
- Neither has room for more fields without regrouping. Prefer placing new
  inputs beside related ones, as done for encounter and sex.

**Configuration:** no duplicates found. Turnaround threshold, dating
look-back and the RhIG window are distinct.

**Services with no tests of their own:**
- `RuleoutPolicy`, the rule-out policy core, was tested only through
  analysis. It now has direct tests, and they found the bug below.
- `StructuredPanelParser`, `XlsxGridReader` and `ImportArtifactStore` are
  exercised only through import tests. Direct parser tests are a future
  candidate.

**Large files:**
- `DatabaseService.cs` (4,371 lines), `AntibodyAnalyzer.cs` (1,573) and
  `AnalysisViewModel.cs` (1,170).
- No split now, because no change is blocked. Revisit if a feature needs a
  separate repository or engine seam.

### Fix: rule lookups ignored antigen case

**Deficiency:**
- `RuleoutPolicy.FindAntibodyRule` and `HeterozygousAllowed` compared antibody
  names case-insensitively. `ReactionsViewModel` had a copy of the latter.
- Antigens are case-sensitive, so a lab rule for anti-K (for example
  "3 cells") also governed anti-k. A heterozygous-OK rule for anti-C also
  applied to c. The first matching rule won.
- This changed rule-out requirements without anyone seeing it.

**Change:**
- New `RuleoutPolicy.SameAntibody`: "Anti-" and "anti-" match, and the
  antigen is ordinal (uses `PatientHistory.AntibodyKey`).
- Rule lookup, heterozygous permission and the separation-exemption lookup
  (through `FindAntibodyRule`) all use it.
- `ReactionsViewModel` now delegates to `RuleoutPolicy`.
- The duplicate checks for DTT and enzyme inferences are now ordinal.

**Tests:** `RuleoutPolicyTests` (12):
- `SameAntibody` cases for K/k, C/c, E/e and S/s;
- required count and its clamping;
- heterozygous permission by antibody and by exception antigen;
- explanation wording;
- an analysis regression where an anti-K rule of 3 leaves anti-k at the lab
  default.

## Iteration 108 (this change)

**Deficiency:** This follows from Iteration 107. Rule lookup takes the first
matching rule, ordered by name, and the Rule dialog's Antibody box accepted
free text. That allowed:
- a rule naming no real antigen ("anti-kell", "anti-fya", or a bare "K", as
  one test fixture had), which silently never applied;
- two rules for the same antibody, where which one applied depended on the
  rule names, so renaming a rule could change rule-out counts.

**Bounded improvement:**
- `RuleValidation.NormalizeAntibody`, enforced in `DatabaseService.AddRule`
  and `UpdateRule`:
  - requires the anti- form and a known antigen in its exact case, with a
    "Did you mean anti-Fya?" hint;
  - refuses a second rule for the same antibody and names the existing rule;
  - stores the normalized "anti-X". anti-K and anti-k stay separate.
- `GetKnownAntigens` returns the standard and warehouse antigens plus any
  stored extra antigen, for older data.
- `RuleValidation.Problems` and a new Worklist kind, `RuleConflict` ("Rule
  problem"), list stored rules that break these checks, naming the rule that
  actually wins. Open goes to the Rules tab.
- `GetAllRules` orders by name without regard to case, then by id, so the
  order is fixed.
- The Rules tab shows the refusal message when a save is refused.

## Iteration 109 (this change)

**Deficiency:** Patient history lines showed the accession, date, confirmed
identification, DAT and transfusion status for each earlier specimen. They did
not show the encounter number, or the phenotype or genotype recorded on that
specimen. The prompt asks for previous phenotype and previous genotype.
When the current phenotype is unreliable after a transfusion, an earlier
(often pre-transfusion) phenotype is what a technologist looks for. To find
it, they had to open each older specimen.

**Bounded improvement:**
- `PatientHistory.Describe` adds the encounter after the date, and the
  phenotype and genotype when recorded.
- These lines are shown in the detail pane, Specimen Summary and the
  identification worksheet.
- They are shown for review only. Analysis does not use an earlier phenotype
  as evidence, because its date and reliability are not structured.

## Iteration 110 (this change)

**Deficiency:** The Iteration 107 review found that `StructuredPanelParser`
(JSON and XML) and `XlsxGridReader` were tested only indirectly through
import flows. Direct tests found three defects that could put wrong or
missing antigen typing into an import preview:
1. **Rows dropped.** In XLSX, a cell's `r` reference is optional. Rows whose
   cells had none were dropped entirely, and a row mixing both kinds
   misplaced the unreferenced cells.
2. **Extra text in values.** Phonetic reading runs (`rPh`) in shared or
   inline strings were appended to the cell text, so "Fya" read as "Fyaふ"
   and the header was then unrecognized.
3. **Duplicates missed.** The duplicate-cell check compared untrimmed
   numbers, so " 1 " and "1" were both kept.

**Change:**
- `XlsxGridReader` places a cell without `r` after the previous cell and
  ignores phonetic runs. `ColumnIndex` is now public.
- `StructuredPanelParser` trims the cell number before the duplicate check.

**Tests:** `StructuredImportParserTests` (13):
- JSON: K and k kept apart; value mapping (++, 0, numeric 1, nt); aliases
  such as Fy(a); unknown antigens reported; no typing invented for antigens
  a cell did not list; errors for an array root, bad JSON, bad values, a
  missing cell number and duplicates; the UTF-8 BOM.
- XML: attributes or child elements, and `ag id=`.
- `ToLabResult`: NT values dropped.
- XLSX: the workbook relationship followed to a non-default sheet; shared,
  rich and inline strings; sparse and wide columns (AA); cells without
  references; phonetic runs ignored.

## Iteration 111 (this change)

**Deficiency:** The Worklist had count cards and Show toggles for four
categories (Incomplete, Stale, Expiring, Expired). Its other ten kinds had no
count and could not be isolated: overdue testing, supervisor review, expired
reagent use, untyped cells, check typing, duplicate lots, rule problems and
inconsistent status. A supervisor could not see how many review items were
waiting, or list only those. Isolating a card left them mixed into the list.

**Bounded improvement:**
- New `LabSettings.WorklistCategory(kind)` maps every kind to exactly one card.
  The kinds without their own card go to a fifth category, "Review", with a
  `WorklistShowReview` preference.
- The copied isolation logic in `LabSettings` and `WorklistViewModel` is
  replaced by generic `ShowsWorklistCategory`, `SetWorklistCategory`,
  `IsWorklistCategoryIsolated` and `IsolateOrRestoreWorklistCategory`. A new
  kind can no longer be left off every card.
- Worklist view: a fifth card ("Review", with its count), a Review Show
  checkbox, and the label "Showing only items to review".
- Behavior change: isolating another card now also hides Review items.

## Iteration 112 (this change)

**Deficiency:** Section 31 asks for controlled import and export of
configuration and rules. Rules could only be typed in by hand on each
installation. Copying a validated rule set to a second workstation, or
restoring it, meant re-entering every count, zygosity choice and separation
exemption, with room for transcription errors in settings that decide
rule-outs.

**Bounded improvement:**
- `RuleTransfer.Export` writes a versioned JSON file (`format:
  "hemoiq-rules"`, `version: 1`).
- `RuleTransfer.Preview` checks the file and plans each entry:
  - Add, Update (each changed field named), Unchanged, or Error (with the
    reason);
  - nothing is written;
  - rules are matched by antibody with case kept;
  - entries are validated with `RuleValidation` (known antigen in exact
    case, name required, count 1–5, known exception antigen, no duplicate
    antibody in the file);
  - a foreign or newer file format is refused.
- `RuleTransfer.Apply` writes adds and updates in one transaction through the
  normal audited, permission-checked `AddRule` and `UpdateRule`. It is all or
  nothing, and it never deletes rules missing from the file.
- The Rules tab has **Export…** and **Import…** buttons. Import shows the
  summary and asks for confirmation.

## Iteration 113 (this change)

**Deficiency:** Following Iteration 112, rules could move between
installations but the laboratory-wide Preferences could not. Those
Preferences decide analysis: probability threshold, ID rule, default
rule-out count, separation cells, transfusion, dating and RhIG windows, and
extra phases. Setting up a second workstation meant retyping them, and a
mismatch would make the same reactions analyze differently on two
workstations.

**Bounded improvement:**
- `PreferenceTransfer.Export` writes a versioned JSON file (`format:
  "hemoiq-preferences"`) of the laboratory-wide settings. Workstation and
  personal settings are excluded: initials, Worklist filters, display
  toggles and the database size.
- `PreferenceTransfer.Preview` lists each change as old → new without
  writing anything:
  - each setting is checked against its own range and type, and refused
    rather than clamped;
  - unknown names are listed as not transferred;
  - foreign files and newer versions are rejected.
- `PreferenceTransfer.Apply`:
  - requires the Preferences permission;
  - audits `import_preferences` with every changed value;
  - audits `update_settings` (the clinical snapshot) only when analysis
    settings changed, so staleness behaves exactly as after a manual edit.
- The Preferences dialog has **Export…** and **Import…** buttons. Import
  confirms, saves and closes the dialog.

## Iteration 114 (this change): product review and an antigen-case sweep

### Review (since Iteration 107)

**Completed since the last review:**
- Rule validation and the Rule problem queue (Iterations 108 and 111).
- Encounter, phenotype and genotype in patient history (109).
- Direct tests for the structured import parsers (110).
- The Review card on the Worklist (111).
- Rule and Preferences export/import with preview and confirm (112 and 113).

**Defect class reviewed.** Iteration 107 found that antigen case was ignored
in rule lookups. This review searched the whole codebase for the same
pattern: `OrdinalIgnoreCase` comparisons, dictionaries and sets keyed by an
antigen or antibody name. Places that compare cell numbers, phase names,
vendors, file names and SQL columns are correctly case-insensitive and were
left alone.

### Fixes (10 sites; K/k, C/c, E/e, S/s had been treated as one)

| Area | Effect before the fix |
|---|---|
| `AntibodyAnalyzer.SerumRemovesAntibody` | An adsorbed aliquot expected to remove anti-K was also treated as removing anti-k, which then left ordinary scoring |
| `AntibodyAnalyzer.CollectConflictingReactiveCells` | One dictionary for anti-K and anti-k, so anti-K's conflicting reactive cells appeared on anti-k's rule-out evaluation |
| `FinalAntibodyParser.Append` | Adding anti-k to a Final ID that already held anti-K did nothing |
| `PanelComparer` (antigen union and normalize) | Lot comparison merged the K and k columns, so a K-typing change could be hidden |
| `PanelImportReviewer.CompareSchema` | The import preview's schema-vs-prior-lot check missed an added or removed k (or c, e, s) column |
| `SelectedCellRecommender.PatientAntigen` | Patient typing for K could be read as k when ranking selected cells |
| `SelectedCellRecommender.ResolveCandidates` | anti-k was dropped from the candidates when anti-K was also suspected |
| `VendorPanelImportService.IncomingAntigens` | With no explicit antigen order, k was dropped from the imported column order |
| `ReactionsViewModel` ruled-out columns | K and k nonreactive-cell counts were added together, so a column could show as ruled out with too few cells |
| `RuleoutRow.IsClinicallySignificantAntibody` | Matched names ignoring antigen case |

**Tests:** `AntigenCaseRegressionTests`:
- Final ID: appending anti-k beside anti-K; duplicates still ignored.
- Lot comparison: the K and k columns stay separate, and the import schema
  check reports a removed k.
- Selected cells: patient K typing is not read as k.
- Analysis: anti-K's conflicting reactive cell is not on anti-k's evaluation.
- Clinical-significance matching.
- The three private or internal sites (candidate list, vendor import column
  order and Reactions ruled-out columns) are one-line comparer changes and
  are covered by the full suite.

### Roadmap

1. A guard against this class of bug: one shared `AntigenComparer.Name`
   (ordinal) used wherever an antigen or antibody name is compared or keyed,
   or a test that fails on `OrdinalIgnoreCase` near antigen code.
2. The Specimen dialog regrouping (the dialog is still dense).
3. A single configuration bundle combining rules and Preferences.

## Iteration 115 (this change)

**Deficiency:** Two reviews (Iterations 107 and 114) fixed eleven places that
ignored antigen case. Nothing stopped the next change from adding a twelfth,
and the convention was written down nowhere a developer would look.

**Bounded improvement:**
- `AntigenCaseGuardTests` scans the application source. Every
  case-insensitive comparison on a line that mentions antigens or antibodies
  must appear in a reviewed list with a reason, or the test fails and names
  the line.
  - Sorting and the "anti-" prefix check are always allowed.
  - A second test fails when a reviewed entry no longer matches anything, so
    the list cannot go stale.
  - A third test checks that the guard flags the original defect's shape.
- The 15 current sites were reviewed and listed. Each is a non-antigen word
  (DAT results, AC, XML element names, vendor or product names, filter
  words) or a reviewed fallback that cannot merge a case pair.
- New `docs/developer-conventions.md` covers case-sensitive antigen names,
  audited changes, preview-before-write imports, schema migrations, and test
  isolation for settings.

## Iteration 116 (this change)

**Deficiency:** Confirm ID warned when the analysis was missing or stale, and
when the separation criterion was not met. Confirming anyway was allowed but
left no record that a warning was overridden. The prompt asks for overrides to
be audited (section 27) and shown to supervisors (section 20). Confirm ID also
did not warn when the latest analysis still had testing open, such as
clinically significant antibodies not ruled out. Those items appeared only in
the status strip.

**Bounded improvement:**
- `ConfirmationOverrides` builds the override texts and the new
  open-testing prompt.
- Confirm ID now has three warnings: stale or missing analysis, testing still
  open, and separation shortfall. Each defaults to No, and each one
  confirmed past is collected.
- `SetSpecimenFinalCall(..., overrides)` stores them in the same
  `confirm_final_call` audit entry. The reason reads "Confirmed past N
  warning(s)", so they are written together. No schema change.
- The overrides are shown in three places:
  - the Timeline, as "WARNINGS OVERRIDDEN: …";
  - the identification worksheet, as one line per override;
  - the supervisor-review Worklist item's detail.
- They are read from the latest confirmation only, so a clean
  re-confirmation after a clear shows none. Audit history is kept.

## Iteration 117 (this change)

**Deficiency:** Iteration 116 recorded each warning confirmed past at Confirm
ID. The operational dashboard asks for the "number of manual overrides" with
drill-down to the cases, and Analytics had neither.

**Bounded improvement:**
- `OverrideAnalytics.Cases` reads `confirm_final_call` audit entries that
  carry overrides, by confirmation date in the selected range, newest first.
  - Each case has the accession, date, Final ID, confirmer and the overridden
    warnings.
  - Confirmations that were later cleared, or replaced by a newer
    confirmation, still count and are marked "No longer the current
    identification."
- The date-range test `UserActivityService.InRange` is now shared.
- Analytics has a fifth stat card, **Confirmed past warnings**, showing the
  count. **Show cases** fills the existing drill-down list, and Open goes to
  the specimen.
- The drill-down Detail column is wider, and Export CSV adds a "Confirmed past
  warnings" section.

## Iteration 118 (this change)

**Deficiency:** Iteration 116 recorded Confirm ID overrides. The two
specimen-save warnings could still be saved past without any record: an
encounter number already on another patient's specimen (Iteration 103), and
a pregnancy for a patient recorded as male (Iteration 105). Both are
data-quality and misidentification signals that a supervisor should be able
to review.

**Bounded improvement:**
- `SpecimenEdit.SavedPastWarnings`: the specimen dialog collects each warning
  the user saved past.
- `SpecimenSaveService.Save` records each as a `save_override` audit entry,
  with the warning as the reason, inside the same transaction. A failed save
  records nothing.
- The Timeline shows "Saved past a warning: …".
- `OverrideAnalytics` now lists save overrides alongside confirmation
  overrides, newest first. The Analytics card is renamed **Warnings
  overridden**, and the CSV section is renamed to match.

## Iteration 119 (this change): product review and an audit-coverage sweep

### Review (since Iteration 114)

**Completed since the last review:**
- The antigen-case guard and developer conventions (115).
- Confirm ID overrides: recorded (116), counted on Analytics (117), and
  extended to specimen-save warnings (118).
- A supervisor can now see every warning someone continued past, on the
  Timeline, the worksheet, the review queue and Analytics.

**Defect class reviewed.** The prompt says "never silently overwrite
clinically important historical information". Every `DatabaseService` method
that writes data was checked for an `AppendAudit` call. 22 had none. Most are
derived analysis output, timestamps, or helpers whose callers audit. Two were
real gaps.

### Fixes

| Gap | Before | Now |
|---|---|---|
| Startup auto-deactivation of expired specimens and panels | Switched to inactive with no record, so they left the Worklist with nothing on their history | One `auto_deactivate_expired` entry per record, with the expiration date and before/after; Timeline "Made inactive automatically: Expired …" |
| `AddPanelRun` | Treated or labelled runs (ficin, DTT, prewarm and so on) were not audited, while run deletion was, and additional testing never appeared on the Timeline | `add_run` audit with panel, treatments and label; Timeline "Run added: …" for treated or labelled runs, with the implied untreated default run omitted |

**Guard:** `AuditCoverageGuardTests` fails on any new unaudited writer that
is not on its reviewed list of 19 entries with reasons, and it fails when an
entry goes stale. `docs/developer-conventions.md` documents it.

**Tests:** `WriteAuditCoverageTests` covers the run audit and Timeline
wording, and startup deactivation (audited once, not repeated on the next
start).

### Roadmap

1. The Specimen dialog regrouping (dense, flagged since Iteration 107).
2. One configuration bundle for rules and Preferences.
3. Overrides by technologist on Analytics.

## Iteration 120 (this change)

**Roadmap item checked:** "Specimen dialog regrouping (dense)", carried since
Iteration 107. The dialog was rendered off-screen to images, one per tab, and
inspected. All four tabs (Specimen, Clinical context, Serology, Notes) fit,
with no clipped or crowded fields, so no regrouping was made. The roadmap
item is closed.

**Found while checking:**
- The Clinical context note still said a recent transfusion always makes the
  phenotype unusable. Since Iteration 102 a transfusion of only platelets,
  plasma or cryoprecipitate does not. The text is corrected.
- The Iteration 119 reviewed-list claim was verified: both callers of
  `UpdatePanelCell` (typing edits and import replacement) audit. The
  method stays public because tests use it to set up data.

**Guard:** `SpecimenDialogSectionsTests.EveryTab_FitsWithoutClipping` renders
the dialog with the most content (transfused with products, recent
pregnancy). It fails if any tab needs more height than it has. The Specimen
tab has about one row of spare room, which is documented in
`docs/developer-conventions.md`.

## Iteration 121 (this change)

**Deficiency:** Analytics counted warnings overridden overall (Iterations 117
and 118) but not by person. A supervisor could not see whether overrides
cluster with one technologist, which is a training and competency signal.

**Bounded improvement:**
- `UserActivity.Overrides`, per Windows login from the audit log, counts:
  - `confirm_final_call` entries that carry overrides;
  - `save_override` entries.
- It is not added to the activity total, which still measures work.
- The Analytics user-activity table has an **Overrides** column. The activity
  CSV gains "Warnings overridden".

## Iteration 122 (this change)

**Deficiency:** The Preferences dialog sized itself to its content, with 22
rows of 36 px plus buttons (about 860 px), and had no scrolling. On a common
1366×768 lab workstation, where the work area is about 728 px, the window ran
past the bottom of the screen. Save and Cancel were unreachable by mouse;
only Enter or Esc worked. Checked because Iteration 120 left the
Preferences dialog without a layout test.

**Bounded improvement:**
- The button row is docked at the bottom.
- The settings grid sits in a vertical `ScrollViewer`.
- The window's `MaxHeight` is the screen work area minus 40 px, with a
  minimum of 300.
- Checked by rendering at a 420 px limit: the settings scroll and the
  buttons stay visible.
- `SettingsDialogLayoutTests`:
  - the default cap is no taller than the work area;
  - at 420 px the dialog stays within the limit, the settings scroll, and
    Save is fully inside the window.

## Iteration 123 (this change)

**Deficiency:** Iteration 122 found Preferences taller than a 1366×768
screen. The other dialogs that size to their content had never been
measured, and nothing would catch a future field addition pushing one past
the screen.

**Result:** Each dialog was measured off-screen with no height cap:

| Dialog | Height (px) |
|---|---|
| Specimen | 549 |
| Patient | 459 |
| Panel | 491 |
| Rule | 419 |
| Purge database | 279 |
| Preferences, uncapped | 875 |

Preferences is already fixed. Every other dialog fits the roughly 728 px work
area, so no layout changes were needed.

**Guard:** `DialogHeightGuardTests` shows every dialog off-screen and fails
when its natural height exceeds 728 px, unless the dialog caps itself and
scrolls. The measurement was checked against uncapped Preferences, which it
flags. Developer conventions say to add new dialogs to the test.

## Iteration 124 (this change): product review and a date-culture sweep

### Review (since Iteration 119)

**Completed since the last review:**
- Audit coverage guard (119).
- Specimen dialog checked and fit-tested (120).
- Overrides per technologist (121).
- Preferences usable on short screens (122).
- Every dialog height-guarded (123).

**Defect class reviewed.** Dates are stored as Gregorian text and compared
as text. 44 `ToString("yyyy…")` calls and 15 interpolated `{x:yyyy…}`
formats had no culture, and about 10 `DateTime.TryParse` calls (mostly in
dialogs) had no culture. Under the workstation's regional settings this
corrupts stored data:
- Thai (Buddhist calendar) writes year 2569 for 2026.
- Saudi Arabic (Um al-Qura calendar) writes a Hijri year.
- Finnish writes "10.30" for 10:30, which breaks ordinal date comparisons
  for turnaround and specimen dating.

Each of these was confirmed in a test before the fix.

### Fix

`AppCulture.Apply()` runs first in `App.OnStartup` and covers every current
and future site in one place:
- The user's culture is kept for language and numbers.
- The calendar is forced to Gregorian; if the culture cannot use Gregorian,
  invariant date formatting is used instead.
- The time separator is forced to ":".

**Tests:** `AppCultureTests` (7):
- each case first shows the defect under the raw culture (th-TH, ar-SA,
  fi-FI), then shows the normalized culture fixes it;
- stored ISO dates parse as the same day;
- en-US is unchanged in effect, and month names stay localized;
- an end-to-end database save under normalized Thai stores "2026-…";
- a source check that startup calls `Apply()` before any data access.

### Roadmap

1. One configuration bundle for rules and Preferences.
2. Number formatting: verify that stored decimals, such as the probability
   threshold, use the invariant culture. The Preferences threshold already
   does.

## Iteration 125 (this change)

**Sweep:** Following the date fix in Iteration 124, numbers were checked
under comma-decimal regional settings (de-DE).
- No data corruption: Preferences are stored as JSON, database values are
  typed parameters, and the turnaround CSV already used the invariant
  culture.
- Two defects were fixed.

**Fixes:**
1. **Score threshold input.** Preferences accepted only "0.70". A user on
   comma-decimal settings typing "0,70" was told the value must be "between
   0.30 and 0.95". `LabSettings.TryParseDecimal` now accepts a decimal point
   or a single decimal comma, and refuses thousands separators and ambiguous
   text.
2. **Antibody-count CSV.** The percentage column was written with the local
   format ("12,5%", quoted), unlike the other CSV sections. The From and To
   dates were also culture-formatted. Both now use the invariant culture.

**Tests:** `CommaDecimalCultureTests` (10):
- the decimal parser cases, also run under German settings;
- the turnaround CSV, the antibody CSV (which fails without fix 2) and the
  Preferences export round trip, all run under German settings.

## Iteration 126 (this change)

**Deficiency:** Iteration 121 added an Overrides count per login, but a
supervisor could not see which cases those were without reading the audit
log. The operational dashboard asks for drill-down from metrics to cases.

**Bounded improvement:**
- `OverrideAnalytics.Cases(..., login)` filters to one Windows login. The
  match ignores case, as Windows logins do.
- "Current identification" is still decided across every user's
  confirmations before filtering. A clean re-confirmation by someone else
  therefore marks the earlier override "No longer the current
  identification".
- Analytics, Activity by user: selecting a row fills the investigations list
  with that user's override cases. Open goes to each case. The selection
  clears when the date range changes.

## Iteration 127 (this change)

**Deficiency:** The prompt asks for historical phenotype and asks that
analysis account for an unreliable serologic phenotype after transfusion.
When a recent transfusion made the current phenotype unusable, analysis lost
all patient-typing evidence. The patient's earlier phenotype, typed before
the transfusion, was ignored even when recorded on an earlier specimen with
the same MRN; since Iteration 109 it was only displayed in history lines.

**Bounded improvement:**
- `HistoricalPhenotype.Find` returns the most recent earlier specimen with a
  phenotype, under these rules:
  - it applies only when the current phenotype is unreliable or empty;
  - the earlier specimen was collected before the current last-transfusion
    date, or before the current specimen when it is not transfused;
  - the earlier specimen's own transfusion assessment was reliable;
  - a current specimen marked Transfused with no date is skipped, because the
    earlier typing cannot be shown to predate the transfusion;
  - allogeneic HPC transplant patients are skipped.
- Its Supporting and Against considerations are added to the analysis, each
  labelled with the source accession and date. The transfusion note states
  that the earlier phenotype is used instead.
- Existing consumers then apply it as reliable typing:
  - the suspected-but-patient-positive WARNING;
  - the open-testing list;
  - selected-cell ranking.

**Tests:** `HistoricalPhenotypeTests` (4):
- the earlier phenotype is used and labelled;
- in a full analysis, the consideration and the note appear;
- it is not used for an earlier specimen after the transfusion, or one that
  was itself recently transfused, and a valid older one is picked instead;
- it is not used when the transfusion date is missing, after an allogeneic
  transplant, or when the current typing is usable.

No existing test changed behavior (1101 passed).

## Iteration 128 (this change): product review of historical-record mutation

**Review scope:** every path that can change data a confirmed identification
relied on.
- Specimen fields lock after confirmation (since earlier iterations).
- Patient, rule and Preferences changes are audited and mark analyses stale.
- **Defect found:** panel typing. The Panels tab edit was already reasoned and
  audited (`SavePanelCellEdits`). The public direct writers did not check
  history, though: `UpdatePanelCell`, `UpdatePanelCellAntigen`,
  `UpdatePanelCellMetadata`, `DeletePanelCells`, `ReplacePanelCells` and
  `CopyPanelCells`. Any future caller could therefore silently rewrite the
  antigram behind a confirmed identification, against "never silently
  overwrite clinically important historical information".

**Bounded improvement:**
- Each direct writer now refuses a panel that was used for a confirmed
  identification. The message points the user to the reasoned Edit on the
  Panels tab.
- The SQL moved into private `…Core` methods, which the audited edit calls.
- App callers (imports, copy, seeders) only write to panels they just
  created, so they are unaffected.
- The guard covers confirmed identifications only, not panels with reactions
  alone. Those remain editable directly, because the analyzer re-reads typing
  on every run and an unconfirmed workup is not yet a record. The audited edit
  still asks for a reason on them.
- `AuditCoverageGuardTests` lists the `…Core` methods, with the reason that
  their public wrappers refuse confirmed panels.

**Tests:** `PanelTypingEditTests` (+2):
- every direct writer is refused on a confirmed panel and the typing is
  unchanged;
- the reasoned edit still saves.

No existing test changed behavior (1103 passed).

## Iteration 129 (this change)

**Deficiency:** Rule-outs depend on both the rules (Rules tab) and the
laboratory-wide Preferences (counts, windows, thresholds). These were
exported and imported separately, and with gaps:
- A workstation could take one without the other and reach different
  rule-outs from the same reactions.
- Rule import kept stored rules missing from the file, without listing them.
  A local extra rule could therefore silently change rule-outs after a
  "copy of the reference configuration".
- Each import skipped refused entries and applied the rest.

**Bounded improvement:**
- `ConfigurationBundle.Export` writes one file (`hemoiq-configuration`, v1)
  that nests the existing rule and Preferences formats.
- `Preview` reuses both existing previews and adds `LocalOnlyRules`, the
  stored rules with no counterpart in the file. It writes nothing.
- `Apply`:
  - refuses a file with any refused rule or Preference;
  - checks the needed roles before writing;
  - writes rule adds and updates, optional audited removals of local-only
    rules, Preference audits and one `import_configuration` audit in a single
    transaction;
  - changes the live settings only after the commit.
  Preference changes are audited as `update_settings`, so open analyses
  become stale as after a manual edit.
- UI: Tools > Export Lab Configuration… and Import Lab Configuration…. The
  import shows one combined preview, a Yes/No/Cancel choice to remove or keep
  local-only rules, and a final confirmation. On success, Preferences are
  saved and the Rules tab and Worklist refresh.

**Tests:** `ConfigurationBundleTests` (6):
- the round trip reproduces rules and Preferences, with the audits;
- local-only rules are listed, kept by default and removed on request;
- a file with a refused value changes nothing;
- wrong format, newer version and a missing section are refused;
- a Supervisor cannot apply Preference changes, and the rules roll back too;
- a mid-apply failure rolls everything back and leaves the settings untouched.

No existing test changed behavior (1109 passed).

## Iteration 130 (this change): product review of configurable laboratory policy

**Review:** which laboratory choices are data, and which are still in code.

Configured as data:
- per-antibody rules (count, heterozygous, exception, separation exemption);
- lab Preferences (score threshold, ID, ACS, separation and default counts,
  windows, review, turnaround, extra phases);
- users and roles;
- warehouse antigens on panels;
- since Iteration 129, all of the above transfer together.

Still in code:

| Item | Assessment |
|---|---|
| ACS antigen list | **Fixed in this iteration.** It decides when "All Clinically Significant Antibodies Ruled Out" is suggested, and labs differ on Lea/Leb/M/N/P1. |
| Reaction grades | Fixed. They are standard serology, and changing them would break stored reactions; not worth configuring. |
| Standard phases (IS/37/AHG/CC) | Fixed, with configurable extras. Adequate. |
| Ficin/DTT antigen effects | Fixed and published; low value to configure. |
| Dosage (antithetical pairs) | Fixed and genetic; per-antibody rules already govern heterozygous use. |
| Role-to-permission grants | Fixed by role. A configurable grant table is a candidate if labs need a senior-technologist role. |
| Antigram system grouping | Fixed. Display only. |

**Bounded improvement:**
- The ACS list is now a fixed core (D, C, c, E, e, K, k, Fya, Fyb, Jka, Jkb,
  S, s) plus `LabSettings.AcsAdditionalAntigens`, defaulting to Lea, Leb, M,
  N. With the default, behavior is unchanged.
- `AntigenConstants.TryParseAcsAdditions` accepts known antigens in exact
  case, drops core antigens and repeats, and orders the rest as on the
  antigram. Unknown names are refused in Preferences and in Preferences
  import. When settings load, unknown names are dropped.
- Every consumer uses the configured list:
  - ACS evaluation;
  - the open-testing list;
  - the untypeable-specificity classifier;
  - the clinical-significance flag on rule-out rows.
- `AcsEvaluation.Antigens` records the antibodies evaluated. The clinical
  report lists them when ACS is suggested.
- The rule trace (`Serialize` and `Explain`) and `ClinicalSnapshot` include
  the list, so changing it is audited and open analyses become stale.

**Tests:** `AcsAntigenConfigurationTests` (8):
- the default equals the previous list;
- parsing (order, core and repeats dropped, wrong case refused, blank means
  core only);
- core-only makes ACS eligible without Lewis/MN rule-outs;
- an added P1 must be ruled out;
- the rule trace serializes and explains the list;
- the snapshot changes when the list changes;
- `Clamp` drops unknown names;
- Preferences transfer carries the list and refuses unknown names.

No existing test changed behavior (1117 passed).

## Iteration 131 (this change)

**Deficiency:** Once a lab registered users, only supervisor and
administrator actions were checked. Specimens, panels, reactions,
identifications and patient records could be changed by any Windows login,
including one that was never registered. The audit log then names a login no
one is accountable for. There was also no role for staff who should see but
not change results, such as reporting staff, auditors or a medical director's
delegate.

**Bounded improvement:**
- New permission `RecordResults`, granted to Technologist, Supervisor and
  Administrator.
- New role `Read-only` (`readonly`), which holds no permissions.
- Every public `DatabaseService` method that changes laboratory data calls
  `EnsureCanRecord()` first (44 method bodies, including both `DeleteReactions`
  overloads). With no registered users, everything is still allowed, as
  before.
- Exempt: derived analysis output and snapshots, so a Read-only user can open
  an analysis; and the empty default run needed to open the reaction grid.
- Migration `MigrateUserRoles` rebuilds an existing `users` table in a
  transaction, keeping its rows, so its role CHECK accepts `readonly`.
- The Users dialog lists the new role automatically. A refused change shows
  who the user is, their role, and the roles that are allowed.

**Tests:** `RecordResultsPermissionTests` (6):
- a Read-only user can read and run an analysis, but adding a specimen,
  saving a reaction, confirming an ID, adding a panel or saving a patient is
  refused and nothing changes;
- an unregistered login is refused with a clear message;
- a Technologist records results as before;
- an empty registry still allows everything;
- an old database's users table is migrated and keeps its users;
- a source-scanning guard fails if any public writer lacks a permission check
  and is not on its reviewed list. On its first run it found the unguarded
  `DeleteReactions`.

`AccessControlTests` setup now adds its specimen before registering users.

All 1123 tests pass.

## Iteration 132 (this change)

**Deficiency:** After Iteration 131, a Read-only user or an unregistered
login could not change data, but nothing on screen showed this. Every change
button looked available, and the refusal came only after the user had filled
in a dialog or typed a reaction grid. Nor did any screen show who the
application thought was signed in.

**Bounded improvement:**
- `SessionAccess.Describe` reports the session as one of:
  - users not registered: all actions allowed;
  - a named user and role, who may record;
  - a named Read-only user: view only;
  - an unregistered login: view only.
  The description includes a detail tooltip. `Refresh` caches it for
  screens.
- Status bar: the session label sits at the right and turns amber when the
  session is view only. It refreshes at startup, after Preferences closes
  (users and roles are managed there) and on Refresh All. A view-only
  startup also says so in the status text.
- Change commands require `SessionAccess.CanRecord`, so their buttons are
  disabled for view-only sessions:
  - Specimens (7 commands);
  - Panels (10 commands);
  - Reactions (6 commands);
  - Analysis (Confirm ID, Clear);
  - Rules (4 commands).
- Navigation, Refresh, print, export, analysis and reports stay available.
- The database permission check from Iteration 131 remains the enforcement.
  A stale cached state can only hide a button, never allow a change.

**Tests:** `SessionAccessTests` (9):
- empty registry;
- named Technologist, with case-insensitive login matching;
- Read-only user and unregistered login are view only;
- `Refresh` updates the cache, which is reset afterwards;
- a source check, per view model, that each change command's can-execute
  includes `SessionAccess.CanRecord`.

All 1132 tests pass.

## Iteration 133 (this change): product review after the access-control series

**What is now complete:**
- **Access chain**, built over Iterations 128–132. Registered users and
  roles, with every data writer permission-checked. Confirmed panels cannot
  be silently rewritten. The session is visible on the status bar, and change
  commands are disabled for view-only sessions.
- **Configuration chain**, from Iterations 129–130. Rules, Preferences and
  the ACS list are data, transferred together, audited, and they mark
  analyses stale.

**Fragmented or inconsistent:**
- Reaction entry in view-only sessions. **Fixed here.** The grid still
  accepted typing, grade hot-keys and Ctrl+V paste, and it wrote local
  recovery drafts. Those drafts could never be saved, and the next
  technologist at that workstation would be offered them as "unsaved work".
- Specimen and patient dialogs open from Edit, which is now disabled for
  view-only sessions. Read-only users read the same details in the detail
  pane, so no separate read-only dialog is needed yet.
- Role grants are a fixed table in code. No lab has asked for Senior
  technologist or Medical director roles; revisit if one does.

**Lacking automated tests:** WPF behavior (enabled state, grid edit mode) is
covered by source checks, not UI automation. This is accepted, consistent
with earlier UI guards.

**Database structures limiting future work:** none found in this review.

**Bounded improvement:**
- `ReactionsViewModel.GradesReadOnly` is true when the session may not
  record. The grid binds `IsReadOnly` to it, and `PreviewKeyDown` ignores
  grade keys and paste.
- `WriteDraft` writes nothing, and the progress line starts with a
  "View only" note.
- `MainViewModel.RefreshSession` notifies the Reactions view, so a role
  change in Preferences applies immediately.

**Tests:** `SessionAccessTests` (+2):
- source checks for the grid binding, the key-handler guard, the draft guard
  and the session notification;
- the wording of the view-only note.

All 1134 tests pass.

### Roadmap (after the Iteration 133 review)

1. Workflow: a specimen-receipt checklist tying patient, specimen, screen
   and panels before testing starts.
2. Reporting: an amended-report marker and the reason on reprints after an
   identification is cleared and re-confirmed.
3. Data integrity: a periodic consistency check (orphaned runs, reactions on
   unlinked panels) shown on the Worklist Review category.
4. Configuration: a role-to-permission table, only if requested.

## Iteration 134 (this change)

**Deficiency:** Analysis reads every run recorded for a specimen
(`GetAllSpecimenRuns`). Detach, though, removed only the specimen–panel link,
even when that panel held grades. The detached panel's reactions kept
driving the analysis while the Specimens tab, reports and Reactions panel
list no longer showed it: hidden evidence. Foreign keys cannot catch this, or
grades left on cell numbers a panel no longer has, or stored grade values
outside the recognized set (from older or imported data). All three stayed
silent.

**Bounded improvement:**
- Prevention: `UnlinkSpecimenPanel` refuses when the panel has graded cells
  for the specimen. It says how many and tells the user to clear them, with
  a reason, first. The Specimens tab shows this as a warning instead of an
  error.
- Detection:
  - `DatabaseService.GetReactionRecords` returns each stored reaction with
    its raw grades (four phases plus extra phases), whether its panel is
    attached and whether the panel still has the cell. The autocontrol is
    exempt from the cell check.
  - `DataConsistencyCheck.Find` reports, per specimen and panel: grades on a
    detached panel, grades for missing cells, and unrecognized grade values.
    Each finding has a detail, an action and an Open target.
- Worklist: a new `DataInconsistency` kind in the Review category, with
  "Resolve" urgency. Detached-panel findings open the specimen, where the
  panel can be re-attached; the others open Reactions. Nothing is changed
  automatically.

**Tests:** `DataConsistencyCheckTests` (5):
- clean data yields no findings and no Worklist items;
- detaching a panel with grades is refused, the link stays, and after the
  reactions are cleared with a reason the panel detaches;
- a legacy detached panel is found and listed on the Worklist, with the
  Specimens target and the Review category;
- grades on a deleted cell and an unrecognized grade "pos" are found;
- ungraded rows on detached panels are not reported, the autocontrol needs
  no panel cell, and grade recognition is case-exact.

All 1139 tests pass.

## Iteration 135 (this change)

**Deficiency:** After a confirmed identification was cleared, with a reason,
and confirmed again, the status said "Amended" and asked for the report to be
reissued. The reissued Clinical Identification report, though, looked
exactly like an original. It did not say it was amended, which
identification it replaced, or why. A clinician comparing two printed
reports could not tell which was current, or what had changed.

**Bounded improvement:**
- `AmendmentHistory.For` reads each `clear_final_call` audit entry, oldest
  first. For each one it records:
  - the identification before the clear, with who confirmed it and when;
  - who cleared it and when;
  - the reason.
  Unreadable older snapshots say "not recorded" rather than failing.
- The report header carries a banner:
  - **AMENDED REPORT** when an identification is confirmed again;
  - **PREVIOUS IDENTIFICATION WITHDRAWN** when none is confirmed now.
- The interpretation lists a numbered **Amendment history**, under the final
  identification or, when none is confirmed, at the top of the
  interpretation.
- PDF export uses the same text, so it carries the banner too.

**Tests:** `AmendmentHistoryTests` (4):
- a never-cleared identification has no banner;
- cleared and re-confirmed: the banner is in the header, and the history
  names the replaced identification, its confirmer, the clearer and the
  reason;
- cleared and not re-confirmed: the withdrawn banner;
- several clears are numbered oldest first, and an unreadable snapshot says
  "not recorded".

All 1143 tests pass.

### Roadmap

1. Configurable role permissions, only if requested.

## Iteration 136 (this change)

**Deficiency:** The investigation's first step, "Received", was always
marked done. A specimen could go to testing and analysis with no MRN, no
collection time and an Unknown transfusion status. Without the MRN there is
no patient history or transfusion requirements. Without the collection time
the transfusion window and specimen dating cannot be counted. With an Unknown
transfusion status the phenotype cannot be judged. Nothing told the
technologist this at receipt.

**Bounded improvement:**
- `ReceiptChecklist.Evaluate`:
  - required: MRN, collection date/time, transfusion status, and the last
    transfusion date when the patient was transfused;
  - advisory: received date/time, visual hemolysis, pregnancy status, DAT.
  Each item states why it matters. `Summary` lists the missing required
  items first, then the advisory ones; `Detail` gives the reasons for the
  tooltip.
- Investigation status:
  - the **Received** step is done only when the required items are
    present;
  - the Awaiting-panel next action names the missing items and says to
    complete them with Edit.
- Specimens detail pane: a **Receipt** line, shaded amber when required
  items are missing, with a tooltip listing the reasons.
- The Clinical Identification worksheet prints the same line.
- It reports only and never blocks testing.

**Tests:** `ReceiptChecklistTests` (6):
- a complete specimen;
- a new specimen's ordered gaps and the reason text;
- a transfused specimen without a date is incomplete, and a not-transfused
  one needs no date;
- advisory gaps do not hold the step;
- the status step and next action follow the checklist;
- the worksheet line.

Three `InvestigationStatusTests` now record the required receipt items first.
That is the intended change: a bare new specimen is no longer "Received" in
full.

All 1149 tests pass.

## Iteration 137 (this change)

**Deficiency:** Each specimen's transfusion status was judged on its own. A
new specimen recorded as **Not transfused**, or left **Unknown**, had its
serologic phenotype used as evidence. That held even when another specimen
for the same MRN recorded a red-cell transfusion ten days earlier. Typing
that may reflect donor cells could argue against a real alloantibody, the
case the transfusion window exists to prevent, and the conflict was not
shown anywhere.

**Bounded improvement:**
- `TransfusionHistoryCheck.Find` looks at the patient's other specimens and
  takes the latest recorded red-cell transfusion on or before this
  collection, within the recent-transfusion window. Transfusions of
  platelets, plasma or cryo only are excluded. Then:
  - **Not transfused** gets a **REVIEW** conflict note, and typing is
    treated as unreliable;
  - **Unknown** has typing treated as unreliable, with the date to record;
  - **Transfused** with an older date gets an update note, and reliability
    is unchanged.
- `Apply` marks the assessment unreliable, appends the reason, and sets
  `Source = "history"`.
- The analyzer applies the check after the transplant check. The note leads
  the suggestions and appears in the [Transfusion] note, and the earlier
  phenotype rule (Iteration 127) still applies.

**Tests:** `TransfusionHistoryCheckTests` (4):
- the Not-transfused conflict, in full analysis (unreliable, note, suggestion);
- Unknown is filled, and `Apply` composes the explanation;
- an older Transfused date gets a note without changing reliability;
- no finding outside the window, for platelets or plasma only, without an
  MRN, or for a transfusion after this collection.

All 1153 tests pass.

## Iteration 138 (this change): product review of Iterations 134–137

**What is now complete:**
- Receipt to report: receipt checklist, consistency check on stored
  reactions, amended-report marking, and the cross-specimen transfusion
  check.
- Every new check follows the same pattern: it reports with a reason, never
  blocks, and has a route to fix the problem.

**Fragmented (fixed here):** the Iteration 136 receipt checklist and the
Iteration 137 history check did not know about each other. A specimen marked
Not transfused, contradicted by a recent transfusion on the patient's other
specimen, showed "Receipt: Complete" and a done Received step on the
Specimens tab. The conflict appeared only on the Analysis tab, after testing.

**Bounded improvement:**
- `ReceiptChecklist.Evaluate(db, specimen)` adds the history finding:
  - a contradiction or a filled-in Unknown becomes the required item
    **Transfusion status matches patient history**, with the source
    specimen, date and day count;
  - an older Transfused date becomes an advisory item.
- The status service, the Specimens detail pane and the worksheet use the
  database-aware form. The specimen-only form is unchanged, for callers
  without a database.

**Other review findings (no change needed):**
- Screens: the Specimens detail pane now carries Receipt, Collection,
  Transfusion and Pregnancy lines. Its layout at 1366×768 was not checked in
  this review; the dialog height guard does not cover panes.
- Duplicated configuration: none new. The ACS list, windows and counts live
  only in Preferences.
- Testing: every check from Iterations 134–137 has database-level tests.
  Specimens-pane bindings rely on the same view-model properties.
- Database structures: none blocking.

**Tests:** `ReceiptChecklistTests` (+2):
- a contradicted status is a required gap, holds the Received step and names
  the source in the next action, and the specimen-only form is unchanged;
- an older Transfused date is advisory only.

All 1155 tests pass.

### Roadmap

1. Configurable role permissions, only if requested.
2. Reports: an optional "Report to" distribution list per specimen, so an
   amended report can name who received the earlier one.
3. Panels: lot comparison already exists. Next would be a warning when a new
   lot drops an antigen the previous lot typed (antigen coverage
   regression).

## Iteration 139 (this change)

**Deficiency:** The import review compared a new lot's antigen coverage with
the previous lot, but only once, in the dialog at import. Activation often
comes later, from the Panels tab, and showed only the source hash. Lots
entered by hand or copied were never compared. Activating a lot that types
Fya on fewer cells, or not at all, silently reduced the cells available to
rule out anti-Fya, and the audit did not record that anyone accepted it.

**Bounded improvement:**
- `PanelActivationCoverage.Check` takes the previous lot
  (`FindPriorVendorPanel`; never the lot itself) and runs the existing
  `PanelComparer` coverage comparison. It returns the gaps (fewer typed
  cells, or none) and the affected ACS antigens.
- The activation prompt shows an **ANTIGEN COVERAGE** paragraph, with a
  warning icon, naming the ACS antibodies that will have fewer rule-out
  cells.
- `SetPanelActive(id, true, coverage)` records the gaps on `activate_panel`:
  - the reason "Activated with coverage gaps against lot X";
  - `priorPanelId` and `coverageGaps` in the after-JSON.
- All three activation paths pass the check: the Panels tab, Activate now
  after a file import, and Activate now after a vendor download. A plain
  activation keeps its original audit shape.

**Tests:** `PanelActivationCoverageTests` (4):
- partial and absent typing are reported, with the ACS antigen named in the
  message;
- activating anyway records the reason, the previous lot and the gaps;
- no report when coverage matches, the vendor differs or there is no vendor,
  and a plain activation's audit is unchanged;
- an antigen added by the new lot is not a gap.

All 1159 tests pass.

## Iteration 140 (this change)

**Deficiency:** An amended Clinical Identification report (Iteration 135)
said what was cleared and why. It did not say which earlier copies were out
there, although every print and export is audited. Recalling the earlier
report meant searching the audit log for the right entries.

**Choice:** The roadmap item proposed a per-specimen "Report to" list. A new
free-text recipient field would add data entry and a schema change, and it
would only be as complete as people kept it. The audit log already records
each issue with its time, operator, output, file name, SHA-256 and the
identification at issue. This iteration uses that record instead.
Recipients remain unrecorded.

**Bounded improvement:**
- `AmendmentHistory.SupersededReports` lists the Clinical Identification
  `report_output` entries issued before the last `clear_final_call`, oldest
  first. Each entry has when it was issued, the output and file, who issued
  it, the identification at issue, and an 8-character fingerprint. Other
  report types, and reports issued after the clear, are excluded.
  Unreadable entries are tolerated.
- The amended report adds **Superseded reports (N)**, under the amendment
  history, with the instruction to recall these copies or mark them
  superseded.

**Tests:** `AmendmentHistoryTests` (+2):
- only the clinical report issued before the clear is listed, with its
  output, identification, fingerprint and issuer, and the report text shows
  the section;
- with no clear there is nothing, and broken audit JSON and an issue
  without an identification are handled.

All 1161 tests pass.

### Roadmap

1. Configurable role permissions, only if requested.
2. Recipients for reports, if labs need them beyond the issue record.

## Iteration 141 (this change)

**Audit:** cells of unknown zygosity (antigen +, antithetical not typed).
`PanelCell.IsHomozygousFor` requires the partner to be typed negative, or
the cell marked ++, and a typed + partner overrides a ++ mark. An
unknown-zygosity cell is therefore never treated as homozygous and counts
only where heterozygous cells are allowed. That is safe; no change was
needed.

**Deficiency found:** The explanation was missing, the case the
requirements use as their example. When every nonreactive Jka-positive cell
was heterozygous, or of unknown zygosity, and heterozygous cells were not
allowed, anti-Jka got **no** rule-out evaluation at all, because only
antibodies with at least one qualifying cell were evaluated. Nothing said why
anti-Jka was not ruled out. A short evaluation (1 of 2) also did not say
which cells had been left out for dosage.

**Bounded improvement:**
- `GetDetailedRuleouts` now also returns the dosage-excluded cells: those
  that pass every other gate (serum removal, treatment destruction) and fail
  only the zygosity rule.
- `RuleoutEvaluation.DosageExcludedCells` holds them.
  `BuildRuleoutEvaluations` evaluates the union of antibodies with
  qualifying cells and antibodies with dosage-excluded cells.
- `RuleoutPolicy.FormatExplanation`:
  - with no qualifying cell: "cannot currently be ruled out because the only
    nonreactive Jka-positive cells are heterozygous (cells …) or are of
    unknown zygosity because Jkb is not typed (cell …), and heterozygous cells
    are not allowed for this antibody (dosage)", plus the next step;
  - when short: "Not counted for dosage: …".
- The rule-out results (`RuledOut` holds only evaluations that meet the
  criteria) are unchanged. The new rows add explanation only.

**Tests:** `DosageRuleoutExplanationTests` (5):
- heterozygous only gives the full sentence, which also appears in the
  suggestions;
- an untyped partner is reported as unknown zygosity;
- a short evaluation lists the dosage-excluded cell;
- a homozygous rule-out, or heterozygous cells allowed, gives no dosage note;
- antigens without a partner are never excluded.

All 1166 tests pass.

## Iteration 142 (this change)

**Deficiency:** The rows added in Iteration 141 for antibodies whose rule-out
is blocked by dosage showed badly in the UI:
- The Ruled Out grid called them "In progress" with Count 0.
- The cell list below was empty, because it showed only counted cells.
- Explain gave "Rule-out in progress" and listed no cells.
The explanation text was right, but the structured views gave no evidence.

**Bounded improvement:**
- `RuleoutRow.From`: status **Blocked by dosage** when no cell counted and
  some were left out for dosage. A short rule-out stays "In progress".
- `AnalysisViewModel.DetailRowsFor` lists the counted cells (Counted: Yes),
  then the dosage-excluded cells (Counted: No (dosage)). `ZygosityLabel`
  distinguishes Homozygous, Heterozygous, and "Unknown (Jkb not typed)".
  The cell grid has a Counted column and a header that says what it lists.
- `AnalysisExplainer`: the rule-out cells include the excluded ones ("not
  counted: heterozygous" or "not counted: zygosity unknown, Jkb not
  typed"), and the status is **Not ruled out (dosage)**.

**Tests:** `DosageRuleoutExplanationTests` (+3):
- grid status and excluded-cell rows with their zygosity;
- a short rule-out lists counted cells first, then excluded ones, and stays
  In progress;
- Explain status, cell lines and criteria.

All 1169 tests pass.

## Iteration 143 (this change): product review of Iterations 139–142

**What is now complete:**
- The dosage chain for ruling out is complete. Unknown zygosity is never
  homozygous (audited in Iteration 141). The explanation names the excluded
  cells, the grid and Explain show them, and the recommender points to the
  cell that would complete the rule-out (this iteration).
- Panel activation compares antigen coverage with the previous lot.
- Amended reports list the superseded copies.

**Fragmented (fixed here):** The Iteration 141 explanation tells the user to
"Test 1 homozygous Jka-positive cell(s)". Selected Cells never pointed to
one, for two reasons:
- its candidates are suspected antibodies (or pattern matches), and an
  antibody blocked by dosage is neither;
- with no candidates, `ScoreCell` returned before any other scoring, so a
  homozygous cell in inventory was never considered.

**Bounded improvement:**
- `SelectedCellRecommender.HasDosageBlockedRuleout` covers rule-outs that are
  short, have dosage-excluded cells and are not suspected. It keeps the
  recommender running and passes the `ScoreCell` early return.
- `ApplyDosageRuleoutCompletion` adds +5 per such rule-out for an untested
  cell homozygous for the antigen, with the note "is homozygous Jka+ Jkb- and,
  if nonreactive, would count toward ruling out anti-Jka (0 of 1 so far)".
  Cells carrying a suspected antibody's antigen are skipped, since they
  would react.
- `SelectedCellRecommendation.CompletesRuleout` is a new flag, separate from
  `FillsSeparation`, so the two reasons are not conflated.

**Other review findings (no change needed):**
- Configuration: no new duplicated settings.
- Database: no new structures needed.
- Testing: every iteration in 139–142 added tests at the analysis level. UI
  column widths at 1366×768 for the new Counted and Zygosity columns remain
  unchecked.

**Tests:** `DosageRuleoutExplanationTests` (+2):
- through a full analysis, an unlinked selected-cell panel's homozygous cell
  is recommended with the note, and its heterozygous cell is not;
- a recommender unit test: a cell carrying a suspected antigen is not
  offered, and a clean homozygous cell is.

All 1171 tests pass.

### Roadmap

1. Configurable role permissions, only if requested.
2. Report recipients, only if requested.
3. Check Analysis-tab column widths at 1366×768.

## Iteration 144 (this change)

**Deficiency:** `SaveReaction` stored grades exactly as given and never
checked the cell number. A caller could store "pos", "999" or "5+", or a
grade for cell 9 on a 2-cell panel. Analysis reads such grades as neither
reactive nor nonreactive, and cannot match a missing cell to typing.
Iteration 134 only *detected* these after the fact on the Worklist; the
`ValidationStressTests` even asserted that garbage was "stored as provided".

**Bounded improvement:**
- Every save path goes through `SaveReaction`: the grid's `SaveReactions`,
  add-run copy, and the seeders.
  - Each phase, including extra phases, is passed through
    `ReactionGrade.Normalize`. Recognized synonyms are stored in standard
    form ("W" becomes w+, "2" becomes 2+, "mixed field" becomes MF).
    Anything outside `ReactionGrade.All` is refused with the cell, phase and
    value named.
  - The cell must exist on the run's panel; the autocontrol (AC) is exempt.
  - `SaveReactions` runs in one transaction, so one bad row means nothing
    from the grid is saved, and the Reactions tab shows the message.
- Older data that already holds bad values is still detected by
  `DataConsistencyCheck`, and analysis still tolerates it.

**Tests:**
- `ReactionSaveValidationTests` (5): synonyms are normalized; a missing cell
  is refused; AC is accepted; a bad row rolls back the whole grid; extra
  phases are validated and normalized.
- `ValidationStressTests`, rewritten to the new contract (2): unrecognized
  values are refused and nothing is stored; garbage written directly to the
  database, as older data can hold, still analyzes without crashing.

All 1176 tests pass.

## Iteration 145 (this change)

**Deficiency:** The Panels editor cycles only through +, ++, - and NT, and
imports report invalid values in their review (`PanelTypingInspector`
InvalidValue). The database method `UpdatePanelCellAntigen`, used by seeders
and any future caller, stored whatever it was given, for example "0", "w" or
"+w". Analysis treats anything that is not +, - or ++ as untyped, so such a
value would silently drop the cell from that antigen's evidence. The
warehouse-antigen table behaved the same way.

**Bounded improvement:**
- `UpdatePanelCellAntigenCore`, shared by the direct writer and the audited
  edit, calls `RequireAntigenValue` first:
  - +, -, ++ and NT are stored, trimmed; blank becomes NT;
  - anything else is refused with the cell number, the antigen and the
    value.
- This applies to standard and warehouse antigens alike.
- Unchanged: `UpdatePanelCell` and imports still map unreadable values to NT
  before writing, and the import review lists them as InvalidValue, so the
  user sees them before activation.

**Tests:** `PanelTypingValueTests` (11 cases):
- six accepted inputs, with trimming and blank as NT;
- four refused inputs, with the exact message and the typing unchanged;
- a warehouse antigen is validated the same way.

All 1187 tests pass.

## Merge of main (commit 4cdb7a1) into HemoIQ

Main had one commit (4cdb7a1) that HemoIQ lacked, made on the
`AntibodyPanels/` layout before the HemoIQ rename. It was merged into
HemoIQ and adapted to the HemoIQ names. What it brought:
- **Check cell in rule-out evidence.** A counted rule-out cell whose check
  cell is 0 is named in the explanation. Conflicting reactive cells carry
  their run, strongest phase and CC (`ReactiveConflictCell`).
- **Per-screen status.** `ScreenStatus` and `AnnounceStatus` on each view
  model: the status line names what the open screen shows. This sits
  alongside HemoIQ's signed-in session label on the status bar.
- **User manual.** `UserManual` writes `docs/USER_MANUAL.md` and a PDF from
  the help topics when the shortcut is built.
- **Workflow seeder.** `E2EWorkflowSeeder`, with tests.
- **Search.** Panel search results gain Open and frozen columns.
- **Panels.** Antigen columns that are NT on every cell are hidden outside
  Edit, and printed antigrams leave them out.
- **Settings-drift staleness.** Each analysis snapshot keeps its clinical
  Preferences; a differing current setting marks an unconfirmed analysis
  stale.

How the overlaps were resolved:
- **Staleness: both rules kept.** An analysis is stale when grades changed,
  when audited rules or Preferences changed after it (HemoIQ), or when this
  workstation's clinical Preferences differ from those saved with it
  (main). `DatabaseService.AnalysisStaleText` gives the reason;
  `GetAnalysisStaleReason` gives the category. Unconfirmed analyses saved
  before Iteration 130 (which added the ACS list to the snapshot) show as
  stale once and need re-analysis.
- **Heterozygous rule permission: main's fix taken.** An exception antigen
  now widens heterozygous use only for the rule's own antibody. Before,
  HemoIQ applied any rule's exception antigen to every antibody.
- **Case-sensitive antibody keys: HemoIQ kept.** Main's conflicting-cell map
  was case-insensitive, which would merge anti-K with anti-k.
- **All Panels report.** Main's sized columns, with HemoIQ's Typing column.
- **Gap analysis.** HemoIQ's iteration log is kept. Main's separate log
  (its own iteration numbering) remains in main's git history.

## Iteration 146 (this change)

**Deficiency:** Restore from an archive copies grades and panel typing as
stored. Since Iterations 144–145 the save paths refuse unrecognized grades,
grades for cells a panel lacks and typing other than +, -, ++ or NT, but an
older archive can still hold them. Restore brought them back silently:
analysis then read the typing as untyped and the grade as missing, with no
prompt to review.

**Bounded improvement:**
- `RestoreArchive` lists review findings after the copy. Nothing is changed
  or refused, so archived history is kept as it was:
  - `DataConsistencyCheck` findings for the restored specimens only
    (unrecognized grades, cells missing from the panel, detached panels);
  - unreadable typing on panel lots that the restore added, read from the
    raw columns (standard and warehouse antigens), because the normal cell
    reader drops such values and they would look untyped.
- `RestoreResult.ReviewFindings` carries them. The restore audit entry's
  after-JSON stores the full list.
- The completion message lists up to eight under "Review before use" with a
  warning icon; the remainder is referred to the audit entry.
- Help: the Open Archive topic explains the review list.

**Tests:** `RestoreReviewTests` (3 cases):
- a clean archive restores with no findings;
- an archived AHG grade "pos" is restored unchanged, reported, and recorded
  in the audit entry;
- typing "0" on a restored panel lot is reported with the cell, antigen and
  value, and points to Edit on the Panels tab.

All 1230 tests pass.

## Iteration 147 (this change)

**Deficiency:** Found while rebuilding the shortcut after Iteration 146. The
merge of main added a `--write-manual` step to `CreateShortcut.ps1`. App.xaml
still opened the main window through `StartupUri`, so the command-line runs
(`--write-manual`, `--seed-clinical`, `--import-vendor-samples`) created it
even though `OnStartup` returned early. `Shutdown()` then closed it, its
Closing handler asked "Quit the application?", and the build waited for a
click that never came. The manual had been written, but the shortcut was
not refreshed and the installer could not be built unattended.

**Bounded improvement:**
- `StartupUri` is removed. `OnStartup` opens `MainWindow` itself, after every
  command-line branch has shut down and returned.
- Developer conventions gain "Command-line runs open no window".
- Checked by hand: `HemoIQ.exe --write-manual <path>` exits 0 at once with
  the manual written; a normal launch still opens the HemoIQ window.

**Tests:** `HeadlessStartupGuardTests` (2 cases):
- App.xaml has no `StartupUri`;
- `new MainWindow()` appears once in App.xaml.cs, after each command-line
  handler's `Shutdown(` and `return;`.

All 1232 tests pass.

## Iteration 148 (this change): product review of Iterations 144–147 and the merge

**Reviewed:**
- Iterations 144–145 (grade and typing save checks): no new gaps.
- Iterations 146–147 (restore review list, headless runs): no new gaps.
- The merged main features: check-cell evidence, per-screen status, manual
  PDF, Search and Panels changes, the E2E workflow seeder, and its startup
  hooks. The startup hooks had the defect below.

**Deficiency found (data integrity and access control):** the merge added two
writes to `MainWindow_Loaded`, which runs at every launch for every user.
- `E2EWorkflowSeeder.RepairBlanketNegatives` turned an antigen that was "-"
  on every cell into NT, through the unaudited direct writer, on any panel
  whose vendor field read "E2E". A laboratory panel with that vendor name and
  an all-negative low-frequency column (Kpa, Jsa, Lua are typical) would have
  its typing silently erased at each start.
- The direct writer demands Record Results and refuses a lot used for a
  confirmed identification. For a Read-only user, or once an E2E lot was
  confirmed, it threw partway through `Loaded`. That showed an error box and
  skipped the rest of startup, including `RefreshSession`, which disables
  change commands for view-only sessions.
- `DemoDataSeeder.AnalyzeIfNeverAnalyzed` also writes analyses at startup and
  had no permission check.

**Bounded improvement:**
- The blanket-negative repair is no longer called at startup. It runs only
  from the seeding commands (`--seed-clinical`, Load Demo Data). It touches
  only the seeder's own lots (vendor E2E and a lot number from `SpecimenIds`)
  and skips a lot used for a confirmed identification.
- `AnalyzeIfNeverAnalyzed` does nothing for a session that may not record
  results.
- Developer conventions gain "Startup does not change laboratory data".

**Tests:** `StartupDataSafetyTests` (5 cases):
- Read-only and unregistered sessions: startup demo analysis does nothing and
  does not throw;
- a technologist: it analyzes once, then nothing on the next launch;
- a laboratory panel with vendor E2E keeps an all-negative Kpa column;
- a seeded lot used for a confirmed identification keeps its typing;
- `MainWindow.xaml.cs` no longer calls the repair.

All 1237 tests pass.

## Iteration 149 (clean pass): merged main paths, writes outside the guarded methods

**Reviewed:** every line the merge added under ViewModels, Views,
`DemoDataSeeder`, `ReportService` and `Data`, searched for database writes
and file writes.
- Search (Open and frozen columns), Panels (all-NT columns hidden outside
  Edit and on printed antigrams), Reactions (frozen phase columns, compare
  headers) and Analysis: display only, no writes.
- Staleness (`SettingsDrifted`, `LastAnalysisConfigurationChange`): reads.
- Help, Save manual: a Save dialog with an overwrite prompt; writes only the
  file the user picked. An error such as a locked file reaches the
  application error handler, which logs it and shows the message.
- Load Demo Data: asks first, runs inside try/catch, and every seeder write
  goes through the permission-checked database methods. After Iteration 148
  the blanket-negative repair touches only unconfirmed seeder lots.

No new issue or gap worth fixing. All 1237 tests pass.

## Iteration 150: review of staleness, reports and Search; time-of-day test fix

**Reviewed, no gap:**
- **Settings-drift staleness.** The snapshot stores
  `JsonSerializer.Serialize(ClinicalSnapshot())`. Drift compares the same
  serialization of the current settings and applies only to unconfirmed
  analyses. Worklist, Pending Work report and `GetAnalysisStaleReason` agree.
- **Specimen reports** (Analysis Results, Clinical Identification).
  - They recompute the analysis from current data (`updateDb: false`), so
    they cannot print a stale stored result.
  - The clinical report prints the confirmed final call, any warnings
    overridden at confirmation, the amendment banner and the rule trace.
- **Search.** Cell results show the typed value or NT, and the antigen
  filter matches only typed values.

**Deficiency found (test reliability):** the full suite failed 3
`ReceiptChecklistTests` when run just after midnight. The tests recorded a
collection time of "today 07:00", which the app correctly refuses as a future
time between 00:00 and 07:00. The product behavior is right; the tests were
wrong for seven hours of every day.

**Bounded improvement:**
- The tests use 00:00 for today's collection time.
- The developer conventions' Tests section now says how to build times from
  today's date.
- Other tests were checked: they use fixed past dates or `DateTime.Now`
  minus an interval.

All 1237 tests pass.

## Iteration 151: staleness keeps the order of events when the clock does not

**Reviewed, no gap:**
- Tests that build dates from the clock use whole-day offsets on date-only
  values, so they do not depend on the time of day.
- Panel and specimen expiry (`ExpirationStatus`, the expired-deactivation
  query, the dialogs) use the local date consistently, and a lot is usable
  through its expiry date.

**Deficiency found (patient safety, data integrity):**
- Staleness compares `reactions_updated_at` with `last_analyzed_at`, both
  local time to the second, as strings.
- A grade saved after an analysis in the same second, or after the clock went
  back, got an equal or earlier time. Clocks go back at the end of daylight
  saving time (the repeated hour) or with a time correction.
- The analysis then looked current although the grades had changed.

**Bounded improvement:**
- `TouchSpecimenReactionsUpdated` stamps a reaction change one second after
  the last analysis when the clock reads that time or earlier.
- `SetSpecimenLastAnalyzed` never stamps an analysis before the reactions it
  read.
- Both are SQL CASE expressions in the single update, so no extra read is
  needed. Ordinary operation still records the current time.
- Help (Analysis) explains it.

**Tests:** `StalenessClockOrderTests` (4 cases):
- a grade after the clock went back makes the analysis stale, stamped one
  second after it;
- a grade in the same second makes it stale;
- an analysis after the clock went back covers the grades it read;
- ordinary order uses the current time.

The first three fail on the previous code.

All 1241 tests pass.

## Iteration 152: rule and Preferences changes ordered by audit id

**Deficiency (patient safety):** an unconfirmed analysis is stale when an
audited rule or Preferences change came after it.
- The check converted the latest change's UTC audit time to local time and
  compared it with `last_analyzed_at` as text.
- During the repeated hour at the end of daylight saving time, or after a
  clock correction, a change made after an analysis could show an earlier
  local time.
- Iteration 151 can move `last_analyzed_at` up to an hour forward to keep
  grade order, which made the same miss possible.
- A change in the same second as the analysis was also missed. The existing
  test slept 1.1 s to avoid it.
- In each case the Worklist, the Analysis banner and Confirm ID treated an
  analysis computed under the old rules as current.

**Bounded improvement:**
- `specimens.analysis_config_seq` (new, nullable INTEGER) records, at each
  analysis, the id of the latest rule or Preferences audit event (0 when
  none).
- `LastAnalysisConfigurationChange` returns `AnalysisConfigurationChange`:
  the audit id, plus the local time for the message.
- `AnalysisCurrency.ConfigChangedAfter` compares ids when the analysis has
  one. Audit ids only increase, so the clock cannot reorder them. Analyses
  saved before this change (NULL) compare times as before.
- One rule serves `StaleReason` (Worklist, banner, status strip, Pending
  Work, Confirm ID) and `GetAnalysisStaleReason`.
- Restore copies only shared columns, so an archive made before this change
  restores with NULL and uses the time comparison.

**Data/schema:** `specimens.analysis_config_seq INTEGER NULL`, added by
`MigrateSpecimenTimestamps`. Nothing is backfilled.

**Tests:** `ConfigurationChangeOrderTests` (6 cases):
- a rule change in the same second is stale;
- a change whose clock time looks earlier is still stale;
- an analysis after the change is current even when its time looks earlier;
- a pre-migration analysis compares times;
- a confirmed identification is not reopened;
- a bare local time from older callers compares by time.

Help (Rules) notes it.

All 1247 tests pass.

## Iteration 153 (clean pass): panel import, rule governance, Analytics

**Reviewed:**
- **Panel import (lab CSV and vendor).**
  - New lots are stored inactive with a review that covers schema changes,
    typing issues and coverage against the prior lot.
  - Re-importing an existing lot requires the replace choice and refuses a
    lot that holds test history. The stored panel is left unchanged, and the
    replacement runs in one transaction.
  - `DeletePanel` itself refuses panels with history.
- **Rules.** Add, update and delete demand Configure Rules, and each is
  audited with before and after values. Since Iteration 152 a change marks
  open analyses stale by audit order.
- **Analytics.** Specificities are grouped with ordinal comparers, so anti-K
  and anti-k stay separate. The one case-insensitive comparer only sorts the
  display. Override analytics also key ordinally.

No new issue or gap worth fixing. All 1247 tests pass.

## Iteration 154 (clean pass): reaction entry, specimen locks, users and roles

**Reviewed:**
- **Reactions.**
  - Every save goes through `SaveReaction(runId, …)`, which demands Record
    Results, refuses a locked specimen, normalizes and validates each grade
    (extra phases too) and refuses a cell the panel lacks.
  - Batch saves run in one transaction and require a reason for changes
    after analysis.
  - Unchanged saves write nothing. Quit warns about unsaved grades.
- **Specimens.** All eleven field-level writers (`UpdateSpecimen*`, link and
  unlink panel) demand the permission, refuse a locked record and audit
  before and after values.
  - `DeleteSpecimen` refuses a confirmed identification or an eluate
    source.
  - Activation, final call and supervisor review have their own audited
    flows.
- **Users.** `SaveUser` demands Manage Users, validates login, initials and
  role, and keeps initials unique among active users. It refuses any change
  that would leave no active Administrator, and audits add and update.

No new issue or gap worth fixing. All 1247 tests pass.

## Iteration 155 (clean pass): configuration transfer, eluates, screening cells

**Reviewed:**
- **Configuration export and import** (`ConfigurationBundle`).
  - The file carries rules and clinical Preferences only, so there are no
    secrets in it. Format and version are checked, and a preview is shown
    before anything changes.
  - A file with refused entries cannot be applied.
  - Rule changes demand Configure Rules and Preferences changes demand
    Configure Preferences. Everything applies in one transaction, and live
    settings change only after commit.
  - Rules go through the audited `AddRule`, `UpdateRule` and `DeleteRule`.
    Preferences log `update_settings`, so open analyses go stale (ordered
    by audit id since Iteration 152). The whole import is also audited as
    `import_configuration`.
- **Eluates.** `SetEluateSource` demands the permission, refuses a locked
  record and is audited. Deleting a specimen that is an eluate source is
  refused.
- **Screening cells.** These are stored as panels, so the panel import,
  typing validation and deletion guards reviewed in Iterations 145 and 153
  apply.

No new issue or gap worth fixing. All 1247 tests pass.

## Iteration 156: a confirmed identification is never replaced in place

**Reviewed, no gap:** the audit log is append-only; no code updates or
deletes `audit_events`.

**Deficiency found (patient safety, auditability):**
- On a confirmed specimen, Final ID stayed editable and Confirm ID stayed
  enabled. Typing a new identification and clicking Confirm ID again
  overwrote it.
- That overwrite asked for no reason and silently removed the supervisor
  review. It left no `clear_final_call` entry, so the amendment history on
  the clinical report did not show the change.
- At the database layer, `SetSpecimenFinalCall` also cleared a confirmed
  identification when given null antibodies. That path had no reason, no
  audit entry and no Clear Identification permission check.

**Bounded improvement:**
- `SetSpecimenFinalCall` refuses a specimen that already has a confirmed
  identification (`RecordLockedException`, naming the current call and
  what to do), and refuses null antibodies (`ArgumentException`).
- Clearing goes only through `ClearSpecimenFinalCall`, which demands Clear
  Identification and a reason and records the identification as it stood.
  Both share a private writer.
- Confirm ID on the Analysis tab checks the stored specimen first, explains
  the clear-then-confirm route, and restores the stored Final ID text.
- `RecordLockedException` gains a constructor with an action-specific
  message.
- Help (Suspected Antibodies) explains it.

**Tests:** `FinalCallAmendmentTests` (5 cases):
- re-confirming is refused, and the call, comment, confirmer and supervisor
  review are unchanged;
- clearing through the confirm method is refused;
- clear with a reason, then confirm, changes the call and records the
  earlier one in the amendment history;
- a clear without a reason is refused;
- a first confirmation still works.

The full suite showed no caller relied on re-confirming.

All 1252 tests pass.

## Iteration 157: CSV exports cannot run formulas

**Reviewed, no gap:**
- `SavePatient` demands the permission and validates the MRN, dates and
  antigen codes. It audits before and after values.
- Removing an antigen-negative requirement is caught at the next analysis:
  `TransfusionRequirementCheck` reports a clinically significant antibody
  with no matching requirement.

**Deficiency found (security, data integrity):** none of the CSV exports
neutralized spreadsheet formulas. A cell starting with =, +, -, @, tab or CR
runs as a formula when the file is opened in Excel, so a typed value could
fetch a URL or launch a DDE command on the reader's machine. The affected
exports were:
- audit log: reason and before/after JSON;
- Reports: panel names, lots, vendors, accessions, notes;
- Analytics: final calls, user names, override warnings;
- the panel antigram CSV.

**Bounded improvement:**
- `CsvText.Safe` adds a leading apostrophe to a value a spreadsheet would
  run, and `CsvText.Field` quotes it.
- Values made only of + and - (panel typing "+", "++", "-") and plain
  numbers are unchanged. An exported antigram re-imports with the same
  typing.
- `CsvText.Writer` (a CsvHelper writer) applies it to every field, both
  through `WriteRecords` and through `WriteField`. CsvHelper's own escaping
  is not used because it would alter "-" typing.
- Report, Analytics, panel and audit exports all use it. Files are UTF-8
  with a BOM, so °C reads correctly in spreadsheets.
- Help (Audit log, Reports) and developer conventions explain it.

**Tests:** `CsvInjectionTests` (19 cases):
- 14 Safe cases: formulas neutralized, typing and numbers kept;
- Field quoting;
- writer fields;
- a panel CSV round trip with +, ++ and - unchanged;
- a formula panel name in the All Panels report;
- a source guard that no `new CsvWriter(` exists outside `CsvText`.

Analytics keeps its quote-when-needed format through `CsvText.Minimal`. All 1271 tests pass.

## Iteration 158 (clean pass): cancel and reinstate, Timeline, PDF and print

**Reviewed:**
- **Cancel.** It demands Record Results and a reason. It refuses an
  investigation that is already cancelled, and one with a confirmed
  identification, which must be cleared with a reason first. It is
  audited, and the record locks.
- **Reinstate.** It demands its own Reinstate Investigation permission and
  a reason, is audited with the cancellation as it stood, and refuses an
  investigation that is not cancelled.
- **Timeline.** Built only by reading specimen data and the audit log; it
  writes nothing. Clears show the identification as it stood and the
  reason, so since Iteration 156 every change of a confirmed call appears.
- **PDF and print.** These render the report text as text: nothing in them
  is evaluated, unlike the CSV formula case fixed in Iteration 157. Each
  issue is audited with its SHA-256 fingerprint.

No new issue or gap worth fixing. All 1271 tests pass.

## Iteration 159 (clean pass): DAT, phenotype and genotype, selected cells

**Reviewed:**
- **DAT.** `UpdateSpecimenDatSplit` demands the permission and accepts
  only recognized grades for each reagent. It refuses a locked record and
  audits before and after values.
- **Phenotype and genotype.** Both are stored as typed, and
  `PatientTypingParser` reads them into evidence:
  - Weiner codes, system pairs such as Fy(a−b+), and single antigen tokens;
  - E/e, C/c, K/k and S/s kept case-sensitive;
  - allele tokens for predicted typing and variant notes.

  The evidence is advisory: it never adds or removes a suspected or
  ruled-out antibody. Each antigen it read is listed on the Analysis tab.
  Text it does not recognize adds no evidence, never wrong evidence.
  Recent transfusion and HPC transplant mark the typing unreliable.
- **Selected cells.** Recommendations come from the analysis; nothing is
  ordered or recorded automatically. Reviewed with the analysis engine in
  earlier iterations.

No new issue or gap worth fixing. All 1271 tests pass.

## Iteration 160: a purge never replaces an existing archive

**Deficiency found (data integrity):**
- `CreateSpecimenArchive` deleted any existing file at the chosen archive
  path before writing the new archive.
- The dialog suggested `hemoiq_archive_yyyyMMdd.db`, so a second purge on
  the same day offered that name and, after the Save dialog's replace
  prompt, deleted the first archive.
- An earlier archive may be the only copy of specimens already removed from
  the live database, so those records were lost permanently.

**Bounded improvement:**
- The purge refuses an archive path that already exists before anything is
  archived or deleted: "Choose a new file name … Nothing was purged."
  `CreateSpecimenArchive` checks again rather than deleting.
- The dialog checks straight after the Save dialog, before the purge
  confirmation. It suggests `hemoiq_archive_yyyyMMdd_HHmm.db` and no longer
  offers to replace a file.
- Help (Purge) explains it.

**Reviewed, no gap:**
- Purge demands Purge Data. It requires an archive when confirmed
  identifications would be lost, refuses the live database file as the
  archive, and deletes and audits in one transaction.
- `--write-manual` writes headlessly (Iteration 147).

**Tests:** `ArchiveOverwriteTests` (3 cases):
- a second purge to the same archive is refused, keeping both the archive
  and the live specimen;
- an unrelated existing file is not replaced;
- a new archive in a new folder still works.

All 1274 tests pass.

## Iteration 161: a rule's rule-out count is stored as analysis uses it

**Reviewed, no gap:**
- **Preferences.** Every numeric box is range-checked and the ACS antigen
  list is validated. The ID rule is a fixed choice. Saving demands
  Configure Preferences and audits `update_settings`.
- **Startup data migration.** It moves or merges legacy folders without
  overwriting and never deletes. It stops once the current folder has a
  database, and a failure leaves the old files in place.

**Deficiency found (configuration integrity):**
- `RuleoutPolicy.RequiredCount` clamps a rule's rule-out count to 1–5, but
  neither `AddRule`, `UpdateRule` nor the Rules dialog checked the value.
- A rule could be stored and shown as 0, 9 or −1 while analysis silently
  used 1 or 5. A stress test recorded that −1 persisted.
- The dialog read unparseable text as 3 without a word.
- Configuration import already refused out-of-range counts, so the paths
  disagreed.

**Bounded improvement:**
- `RuleValidation.RequireRuleoutCount` refuses counts outside 1–5 in
  `AddRule` and `UpdateRule`, with the entered value in the message.
- The Rules dialog validates the box as a whole number from 1 to 5 before
  saving.
- `RuleValidation.Problems`, shown on the Rules tab and Worklist, reports a
  stored rule outside the range with the count analysis uses.
- The stress test now expects the refusal. Help (Rules) explains it.

**Tests:** `RuleoutCountValidationTests` (7 cases):
- 0, 6 and −3 are refused;
- 1 and 5 are stored;
- an update to 9 is refused and leaves the rule unchanged;
- a stored 0 is reported with "analysis uses 1".

`ValidationStressTests` was updated.

All 1281 tests pass.

## Iteration 162 (clean pass): Worklist, Specimens search, F1 help

**Reviewed:**
- **Worklist.**
  - All 15 kinds are produced: the expiry kinds by `GetWorklistItems`'
    expiry pass, overdue testing by `OverdueTesting`, and rule problems
    (including Iteration 161's out-of-range count) by
    `RuleValidation.Problems`.
  - `LabSettings.WorklistCategory` maps every kind to exactly one of the
    five count cards. The card counts and the filter use the same mapping.
  - Stale items use the shared `AnalysisCurrency` rule, ordered by audit id
    since Iteration 152.
- **Specimens search.** It reads only; filters do not change data.
- **F1 help.** `HelpSystemTests` checks that every topic loads and that
  the required topics cover every section. The user manual includes every
  chapter and the intended use.

No new issue or gap worth fixing. All 1281 tests pass.

## Iteration 163 (clean pass): vendor sources, artifact store, panel edit audit

**Reviewed:**
- **Vendor sources.**
  - Only public product and antigram pages are read, with a named user
    agent and a 45-second timeout.
  - No credentials, tokens or sign-in exist anywhere in the vendor code.
  - A source whose antigrams are not publicly readable (Ortho Quick
    Antigram) says so and directs the user to import the e-Antigram file.
  - Downloaded lots are stored inactive for review (Iteration 153).
- **Import artifact store.**
  - Files are named vendor_lot_SHA-256-prefix. The same bytes always map to
    the same name, and different content does not collide in practice.
  - Names are sanitized to letters, digits, dot, dash and underscore, so a
    vendor or lot cannot escape the folder.
  - The panel records the full hash for verification.
- **Panels editor.** Typing edits on a panel with history go through the
  audited, reason-required path. Typing values are validated (Iteration
  145).

No new issue or gap worth fixing. All 1281 tests pass.

## Iteration 164 (clean pass): report fingerprints, specimen dialog, help window

**Reviewed:**
- **Report fingerprints.** `RecordReportOutput` hashes the exact bytes
  issued (CSV exports read the written file back). It records the type,
  output, file name, software version, and for specimen reports the
  confirmed call and supervisor review at that moment.
- **Specimen dialog.** The accession is trimmed and required, and can no
  longer be changed when editing. Collection and receipt times are
  validated and refuse future times (`SpecimenDating`). Saves go through
  `SpecimenSaveService` with the dialog's values. Only seeders and tests
  call `AddSpecimen` directly.
- **Help window.** Escape and Close dismiss it once. Saving the manual uses
  a Save dialog with an overwrite prompt, as reviewed in Iteration 149.

No new issue or gap worth fixing. All 1281 tests pass.

## Iteration 165 (clean pass): Explain tab, investigation status, reaction grid

**Reviewed:**
- **Investigation status.**
  - Derived on each call from panels, reactions, analysis time, final call
    and audit, so nothing is stored that could drift.
  - Uses the shared `AnalysisCurrency` stale rule (audit-id ordered since
    Iteration 152) and the receipt checklist.
  - "Report predates the last clear" compares audit ids, not clock times.
- **Explain tab.** Built from the same `AnalysisResult` as the Suspected
  and Ruled-out tabs, so its reasons cannot disagree with them. The rule
  trace line on reports names the snapshot's software, rules, settings and
  input fingerprint.
- **Reaction grid.** Frozen phase columns and compare headers are display
  only (Iteration 149). Every save goes through the validated, lock-checked
  `SaveReaction` (Iteration 154).

No new issue or gap worth fixing. All 1281 tests pass.

## Iteration 166: Lewis antigens are enzyme-enhanced, not destroyed

**Reviewed, no gap:**
- Rule-out, ID-rule, dosage, separation and pattern loops skip an antigen
  that the run's cell treatment destroys. A nonreactive ficin-treated cell
  cannot rule out anti-Fya, and a DTT-treated cell cannot rule out anti-K.
  Those cells are listed as gated rule-outs on Special Panels.
- The ACS suggestion uses the same per-run evidence.

**Deficiency found (analytical correctness):**
- `AntigenTreatmentEffects` listed Lea and Leb as destroyed by ficin and
  papain. The comment said "variable; treat as destroyed for rule-out
  gating".
- Standard references (AABB Technical Manual; Blood Group Antigen
  FactsBook) give Lewis antigens as resistant to proteolytic enzymes, with
  anti-Lea and anti-Leb reacting more strongly with treated cells.
- The same table drives `EnzymeTreatmentPatternClassifier`. A reactivity
  pattern that disappeared on ficin-treated cells was therefore offered as
  consistent with Lewis, the opposite of what enzyme testing indicates.
- A nonreactive enzyme-treated Le(a+) or Le(b+) cell was also refused as a
  rule-out, contrary to normal practice.

**Bounded improvement:**
- Lea and Leb are Enhanced under ficin and papain, with the source in the
  code comment.
- DTT is unchanged: Lewis is unaffected by DTT.
- The treatment test now expects ficin to destroy M, N, S, s, Fya, Fyb and
  Xga only.
- Help (Special Panels) states the Lewis behavior.
- This changes clinical logic; the laboratory should confirm it against its
  own procedure manual before relying on enzyme-panel Lewis rule-outs.

**Tests:**
- `ScenarioTests.TreatmentModel_FicinAndPapainEnhanceLewis` (new);
- `TreatmentModel_FicinDestroysExpectedAntigens` (updated).

All 1282 tests pass.

## Iteration 167: Indian Inb is destroyed by enzymes (a false rule-out removed)

**Reviewed, no change:**
- Ficin and papain: Mia, Vw, Mur, Ch, Rg and Ge2 stay Destroyed.
  - Ch, Rg and Ge2 are protease-sensitive in the references.
  - For the variable MNS hybrid antigens (Mia, Vw, Mur), Destroyed is the
    conservative reading: enzyme-treated cells do not rule them out, so the
    table cannot produce a false rule-out.
- DTT: the Kell, Lutheran, Dombrock, Yt, LW, Knops, Indian and Cromer
  entries match the references.
  - Ch, and the Lutheran and Cromer antigens, are classically weakened;
    Destroyed is the conservative reading.

**Deficiency found (patient safety):**
- Inb (Indian system, on CD44) was listed as Enhanced by ficin and papain,
  and its warehouse note read "Ficin enhanced".
- Indian antigens are destroyed by proteolytic enzymes, as they are by DTT,
  which the table already had right (AABB Technical Manual; FactsBook).
- So a nonreactive ficin- or papain-treated Inb+ cell counted as a rule-out
  for anti-Inb, although the antigen was gone: a false rule-out of a
  clinically significant antibody.

**Bounded improvement:**
- Inb is Destroyed under ficin and papain (and DTT), with the source in the
  code. The warehouse note reads "Ficin/DTT destroyed".
- Enzyme-treated cells are now gated for anti-Inb on Special Panels, like
  the other destroyed antigens.
- Help (Special Panels) states it.
- This changes clinical logic; the laboratory should confirm it against its
  procedure manual.

**Tests:**
- `WarehouseAntigenTests.TreatmentEffects_IndianInb_IsDestroyedByEnzymesAndDtt`
  (new);
- the ficin-enhanced list no longer includes Inb.

All 1283 tests pass.

## Iteration 168: rr absorption removes anti-f (a false rule-out removed)

**Reviewed, no gap:**
- Only ficin, papain and DTT are modelled as cell treatments, and
  Iterations 166–167 reviewed them.
- The R1R1 (anti-D, -C, -e) and R2R2 (anti-D, -c, -E) lists match the
  absorbing cells. Those cells lack f, and HemoIQ does not model G.

**Deficiency found (patient safety):**
- rr (ce/ce) absorbing cells carry the compound antigen f (c and e in cis),
  so rr absorption removes anti-f.
- `RrAbsorbedAntibodies` listed only anti-c and anti-e, and
  `SerumRemovesAntibody` excludes only the listed specificities from the
  absorbed aliquot's ordinary evidence.
- So nonreactive f+ cells on an rr-absorbed aliquot ruled anti-f out,
  although the absorption had removed it. The new test reproduced that on
  the previous code.

**Bounded improvement:**
- The rr list includes anti-f, and the serum-treatment note says so.
- `AbsorptionPatternClassifier`'s residual check treats f as negative on a
  cell typed c− or e−, since f cannot be present without both. A cell
  reactive after rr absorption that lacks c and e is still reported as
  residual, unexplained reactivity when f was not typed.
- Help (Panel run) lists what each absorbing cell is expected to remove.

**Tests:** `AbsorptionCompoundAntigenTests` (3 cases):
- only rr removes anti-f;
- nonreactive f+ cells on the rr aliquot do not rule out anti-f;
- an untreated run still rules anti-f out.

The existing residual-cell test passes through the implied-f rule. All 1286
tests pass.

## Iteration 169: no rule-outs from autoadsorption after a recent transfusion

**Reviewed, no gap:** prewarm excludes only the IS phase, which the prewarm
technique does not read; 37°C and AHG are interpreted.

**Deficiency found (patient safety):**
- An autoadsorbed aliquot is used as ordinary evidence for alloantibodies,
  by design (`Autoadsorption_DoesNotExcludeAlloSpecificitiesFromOrdinaryScores`).
- Autoadsorption is not reliable within about three months of a red-cell
  transfusion: transfused donor cells in the autologous sample can adsorb
  alloantibodies along with the autoantibody.
- HemoIQ never connected the two. A nonreactive cell on an autoadsorbed
  aliquot ruled out, for example, anti-K for a patient transfused ten days
  earlier, although donor K+ cells could have removed it. Nothing warned
  the user.

**Bounded improvement:**
- When a specimen has an autoadsorbed run and `TransfusionContext` marks
  the patient's typing unreliable, `BuildContexts` sets
  `RunContext.AutoAdsorptionCaution` on that run.
  - Unreliable means a red-cell transfusion within the recent-transfusion
    window, or transfusion noted while the status is Unknown.
  - A platelet-only transfusion does not set it.
- `SerumRemovesAntibody` then treats that aliquot as unable to rule out any
  specificity. Its negatives become gated rule-outs on Special Panels, with
  a reason naming the transfusion and suggesting allogeneic (differential)
  adsorption. Ordinary scores carry the same exclusion note.
- The guidance on a panreactive untreated run paired with that
  autoadsorption no longer says to use the autoadsorbed aliquot.
- Outside the window, or without transfusion, nothing changes.
- Help (Panel run) explains it.

**Tests:** `AutoadsorptionTransfusionTests` (3 cases):
- transfused 10 days ago: anti-K is not ruled out and is gated with the
  reason;
- not transfused: ruled out;
- transfused 200 days ago: ruled out.

All 1289 tests pass.

## Iteration 170 (clean pass): eluate and DAT after transfusion

**Reviewed (`EluateEvidence`):**
- An eluate with a specificity after a recent transfusion notes antibody
  coating transfused donor cells (possible delayed hemolytic or serologic
  transfusion reaction). Recent transfusion uses the same assessment as
  Iteration 169.
- A panreactive eluate supports IgG autoantibody and warns that a recently
  transfused patient may also have an alloantibody; adsorb before ruling one
  out.
- A nonreactive eluate with a positive DAT lists drug-related antibody,
  antibody to an antigen not on the panel (such as ABO from incompatible
  plasma), and complement-only coating. C3-only coating is called out as
  expected to give a nonreactive eluate.
- A specificity in the eluate but not suspected in the serum is flagged
  before serum rule-outs are relied on.
- DAT-positive pattern guidance comes from `ReactionPatternClassifier`, and
  every statement ends "Not a diagnosis".

No new issue or gap worth fixing. All 1289 tests pass.

## Iteration 171: one typing-reliability assessment, including HPC transplant

**Reviewed, no gap:**
- Allogeneic HPC transplant marks serologic typing unreliable and a
  genotype possibly the donor's, from the transplant date
  (`TransplantContext`). An autologous transplant changes neither.
- Passive anti-D from RhIG within the configured window is flagged
  (`SpecimenDating.PassiveAntiD`).

**Deficiency found (patient safety, consistency):**
- `ApplyPatientTyping` combined three sources into one assessment:
  - the specimen's transfusion record;
  - an allogeneic HPC transplant (`TransplantContext.Apply`);
  - transfusions recorded on the patient's other specimens
    (`TransfusionHistoryCheck.Apply`).
- Iteration 169's autoadsorption check, and the separation step's
  historical-antibody lookup, called only the bare `TransfusionContext.Assess`.
- A patient with an allogeneic transplant, or a transfusion recorded only on
  another specimen, could have alloantibodies ruled out from an autoadsorbed
  aliquot. After an allogeneic transplant the autologous red cells may be
  donor-derived, so they can adsorb alloantibodies just as transfused cells
  can.

**Bounded improvement:**
- `AntibodyAnalyzer.AssessTypingReliability` is the one assessment, used by
  patient typing, the autoadsorption caution and the separation step.
- The caution names both causes: recently transfused or an allogeneic HPC
  transplant.
- Help (Panel run) lists every condition.

**Tests:** `AutoadsorptionTransfusionTests` gains 2 cases (5 in all):
- an allogeneic transplant 60 days before, not transfused: anti-K is not
  ruled out and is gated with the transplant named;
- an autologous transplant: anti-K is still ruled out.

All 1291 tests pass.

## Iteration 172 (clean pass): analyzer entry points use the same run contexts

**Reviewed:**
- **`EvaluateFinalCall`** (the separation check at Confirm ID).
  - It builds its contexts with `BuildContexts(runs, specimenId)`, so the
    autoadsorption caution from Iterations 169 and 171 applies.
  - It re-marks panreactive untreated runs. The instance set is cleared
    first, so nothing carries over from an earlier specimen.
  - It uses the same `ExcludedFromOrdinaryScores` gate as the analysis.
- **`SelectedCellRecommender`.** It reads only the finished
  `AnalysisResult`, whose `RuledOut` already reflects every gate: treatment
  destruction, absorption removal, the autoadsorption caution and dosage.

No new issue or gap worth fixing. All 1291 tests pass.

## Iteration 173 (clean pass): neutralization, HTLA titration, cold patterns

**Reviewed:**
- **Neutralization** (`NeutralizationParser`, `NeutralizationNotes`). The
  substances map to the expected specificities:
  - plasma / C4 → Ch, Rg;
  - urine (including Cad) → Sd(a);
  - saliva → Lea, Leb;
  - hydatid or pigeon P1 substance → P1.

  The notes say whether neutralization favors or argues against a readily
  neutralized HTLA-like specificity. Each ends "Not a diagnosis".
- **HTLA titration** (`HtlaTitrationNotes`). It reads the highest recorded
  titer from the notes and extra phases, and states the pattern as evidence
  only.
- **Cold patterns.** IS-only reactivity and the prewarm suggestion are
  covered by existing tests. Prewarm excludes only IS (Iteration 169).

No new issue or gap worth fixing. All 1291 tests pass.

## Iteration 174 (clean pass): antithetical pairs, dosage and heterozygous rules

**Reviewed:**
- **`AntigenConstants.AntitheticalPairs`** matches the genetic pairs:
  - Rh: C/c, E/e;
  - Kell: K/k, Kpa/Kpb, Jsa/Jsb;
  - Kidd, Duffy, MNS (M/N, S/s) and Lutheran.

  D, Cw, P1 and Xga have no pair, so the Panels editor offers no ++ for
  them.
- **Lewis.** Lea/Leb is also listed as a pair, although Lewis phenotype
  follows secretor status rather than zygosity. The effect is conservative
  and matches practice:
  - anti-Lea is ruled out on Le(a+b−) cells and anti-Leb on Le(a−b+) cells;
  - the rare Le(a+b+) cell counts as single-dose and does not rule out
    where heterozygous cells are not allowed.

  No false rule-out results, so this is left as is.
- **Heterozygous exceptions.** `RuleoutPolicy.HeterozygousAllowed` widens
  only the rule's own antibody (taken from main in the merge), and the
  matching is case-sensitive. Dosage-excluded cells are listed separately
  (`DosageExcludedCells`).

No new issue or gap worth fixing. All 1291 tests pass.

## Iteration 175: an AHG-negative cell with a failed check cell is not a rule-out

**Reviewed, no gap:**
- **ACS eligibility.** It uses the same gated rule-out counts as the rest
  of the analysis, and an exception is named in the suggested result.
- **Panel cell counts.** The Panel dialog limits panels to 1–100 cells,
  starting at cell 1 or later. Imports take the count from the file.

**Deficiency found (patient safety):**
- Merged from main, a nonreactive AHG cell whose check cell was recorded as
  0 still counted toward rule-outs. It was only named in the explanation
  and shown pink.
- `E2EWorkflowSeederTests.InvalidCheckCell_RulesOutOnTheCellAndFlagsTheGrade`
  asserted that.
- A negative antiglobulin result whose check cell (IgG-coated cells) does
  not react is invalid: it does not show that the AHG phase worked, and the
  test must be repeated. Counting it relied on the user noticing a colour
  and discounting the cell by hand, so an antibody could be ruled out on an
  invalid test.

**Bounded improvement:**
- `RunContext.IsNegative` is false when `CheckCellFailed` (AHG 0 and CC 0),
  so the cell is no evidence in any rule-out, score, pattern, dosage,
  separation or ACS loop.
- `CalculateRuleouts` lists a gated rule-out for each antigen on that cell:
  "Cell N is AHG 0 but its check cell did not react, so the result is
  invalid and cannot rule this antibody out. Repeat the test."
- A check cell not recorded (NT) still counts, as before; the Reactions grid
  keeps flagging it.
- Test data that recorded CC 0 on negatives as filler now records 2+.
- `TestDataSeeder.SeedVersion` makes the kept fixture database rebuild when
  seed data changes. Before this, the change would never have reached the
  fixture.
- Help (Ruled out) and developer conventions explain it.
- This changes clinical logic; the laboratory should confirm it against its
  procedure manual.

**Tests:** three rewritten for the new rule:
- `RuleoutEvaluationTests.FailedCheckCell_IsNotCountedAndIsGated_TheControlledCellStillRulesOut`;
- `CandidateExplanationTests.RuleoutCell_FailedCheckCell_IsNotListedAsARuleoutCell`;
- `E2EWorkflowSeederTests.InvalidCheckCell_IsNotARuleout_TheControlledCellIs_AndTheGradeIsFlagged`.

Two tests' filler data was corrected.

All 1291 tests pass.

## Iteration 176 (clean pass): reaction validity on rule-out cells

**Reviewed (`RunContext`, `ReactionGrade`):**
- A cell counts as negative evidence only when:
  - AHG is explicitly 0 (NT is not negative);
  - every other interpretable phase is 0 or NT;
  - since Iteration 175, the check cell is not recorded as 0.
- w+, mixed field (MF) and hemolysis (H) are reactive (`IsPositive` is
  anything other than 0 or NT), so they never support a rule-out and are
  read as reactive evidence.
- Phases the serum treatment makes uninterpretable (IS after prewarm) are
  left out of both tests.
- A positive autocontrol does not invalidate nonreactive panel cells. The
  warm-auto, absorption and DAT classifiers handle its interpretation, and
  a panreactive untreated run beside an adsorption is excluded from ordinary
  scores (`MarkPanreactiveUntreatedBesideAdsorption`).

No new issue or gap worth fixing. All 1291 tests pass.

## Iteration 177: reports list rule-outs not counted

**Deficiency found (reporting, reviewability):**
- `AnalysisResult.GatedRuleouts` holds every nonreactive cell that did not
  count toward a rule-out, with the reason:
  - an enzyme- or DTT-destroyed antigen;
  - an absorbed aliquot;
  - autoadsorption after transfusion or transplant;
  - a failed check cell.
- The Analysis tab shows them on Special Panels and in its copied text, but
  neither the Analysis Results nor the Clinical Identification report
  included them.
- A reader of the printed record could not tell why an antibody was left
  out of the rule-outs, or that a test had to be repeated. Iterations
  166–175 added several of these reasons.

**Bounded improvement:**
- `ReportService.AppendGatedRuleouts` writes "Rule-outs not counted (n):"
  after the rule-outs in both reports. It gives one line per distinct
  antibody and reason, sorted, and adds no section when there are none.
- Print and PDF use the same report text. The CSV exports are structured
  separately and do not include this section yet.
- Help (Reports) describes the section.

**Tests:** `GatedRuleoutReportTests` (4 cases):
- a failed check cell is listed with its full reason in both reports;
- a specimen with no gated cells has no section in either.

All 1295 tests pass.

## Iteration 178 (user request): facility letterhead on reports

**Request:** reports should carry the facility name, address and contact
information, set in Preferences. Anything not set is left off.

**Implemented:**
- **Preferences fields.** `LabSettings` gains `FacilityName`,
  `FacilityAddress` and `FacilityContact`, all blank by default.
  - The address and contact are multi-line, one entry per line.
  - `NormalizeMultiline` trims each line and drops blank ones.
  - Limits: 120, 400 and 200 characters.
- **Letterhead.** `FacilityHeaderLines` gives the printed lines;
  `ReportService.FacilityHeader` writes them, wrapped to the report width,
  plus a blank line.
- **Reports.** `GeneratePreviewText` puts the letterhead above the product
  and intended-use preamble, so preview, print and PDF of all eight report
  types carry it. With nothing set, it is empty and reports start with the
  product line as before.
- **Preferences dialog.** Facility name, Facility address (multi-line) and
  Contact information (multi-line) sit after Department, with length
  checks.
  - The dialog still scrolls with Save docked;
    `SettingsDialogLayoutTests` and `DialogHeightGuardTests` pass.
- **Preferences export and import.** Both carry the three fields, and
  overlong values are refused.
- **Not clinical settings.** They are not in `ClinicalSnapshot`, so a change
  does not mark analyses stale. Each issued report's SHA-256 fingerprint
  still covers the letterhead as printed.
- **Help.** The Preferences and Reports topics describe it.

**Tests:** `FacilityLetterheadTests` (12 cases):
- every report type starts with the trimmed name, address and contact lines
  in order, then a blank line and the product line;
- nothing set: no letterhead;
- only contact set: only contact printed;
- normalization;
- the Preferences file carries the fields and refuses an overlong name.

## Iteration 179: Specimen Summary carries rule-outs not counted

**Deficiency found (reporting consistency):**
- Iteration 177 added "Rule-outs not counted" to the Analysis Results and
  Clinical Identification reports. The Specimen Summary text and its CSV
  still listed only the counted rule-outs.
- Since Iteration 175, a cell whose check cell did not react is never
  counted, so the CSV's CheckCell column is always blank. The failed check
  cell then appeared nowhere in the Specimen Summary.

**Bounded improvement:**
- The Specimen Summary text lists "Rule-outs not counted (n):" after its
  rule-outs, using the shared `AppendGatedRuleouts`.
- The Specimen Summary CSV adds a "RuleoutsNotCounted" section
  (Antibody, Reason), sorted and de-duplicated, and omitted when empty.
- The CheckCell column is kept so existing readers of the file do not
  break.
- Help (Reports) describes both.

**Tests:** `GatedRuleoutReportTests` now covers the Specimen Summary (8
cases in all):
- the failed check cell is listed in all three text reports and the CSV;
- there is no section in any of them when nothing is gated.

All 1311 tests pass.

## Iteration 180: panel reports state the lot's status

**Reviewed, no gap:**
- The Panel Antigram omits antigen columns that are NT on every cell
  (`AntigensTypedOnAnyCell`) and says how many were omitted.
- The Panel Summary shows the typing summary.
- Both carry the facility letterhead (Iteration 178).

**Deficiency found (data integrity, imports):**
- Neither panel report said whether the lot may be used. A lot imported
  from a vendor or lab file is stored inactive until someone reviews and
  activates it. An expired lot is deactivated at start-up.
- A printed antigram of either looked like one for an approved lot in use,
  so an unreviewed import could be taken as approved on paper.

**Bounded improvement:**
- `ReportService.PanelStatusLine`, printed on both panel reports:
  - "Active";
  - "INACTIVE - imported <date> from <format>; not offered for testing
    until reviewed and activated on the Panels tab";
  - "INACTIVE; not offered for new testing";
  - "EXPIRED <date>; not offered for new testing" (expired takes
    precedence).
- Help (Reports) describes it.

**Tests:** `PanelReportStatusTests` (7 cases): active, imported awaiting
review and expired, each on both reports, and deactivated without an import.

All 1318 tests pass.

## Iteration 181: list reports show record status

**Reviewed, no gap:** Pending Work lists active specimens by stage, uses the
shared stale rule, and carries the letterhead.

**Deficiency found (reporting, data integrity):**
- **All Panels.** The report and its CSV had no status. Expired lots,
  deactivated lots and imported lots awaiting review were listed exactly
  like lots in use, the same gap Iteration 180 closed on the single-panel
  reports.
- **All Specimens.** `GetSpecimenStatus` did not check cancellation, so a
  cancelled investigation was listed as "Not analyzed" or "Analyzed". Its
  CSV had no status column at all.

**Bounded improvement:**
- `ReportService.PanelStatusShort` gives Active, "Imported, inactive",
  Inactive or Expired. All Panels has a sized Status column, and the CSV a
  Status field.
- `GetSpecimenStatus` returns Cancelled first. The All Specimens CSV adds
  the Status field.
- Help (Reports) lists the values.

**Tests:** `ListReportStatusTests` (3 cases):
- four lots with each status, in the text and the CSV;
- a cancelled investigation shown as Cancelled in the text and the CSV,
  beside a not-analyzed one.

All 1322 tests pass.

## Iteration 182 (clean pass): Analysis Summary tab against the reports

**Reviewed:**
- The Analysis tab's Summary text (`AnalysisViewModel.SummaryText`) is an
  on-screen working view, not an issued report. It lists the same sections
  the printed reports carry, including the gated rule-outs with the same
  reasons.
- It lists each gated entry, where the reports merge identical antibody and
  reason pairs. That suits a working screen and changes no conclusion.
- Issued output (print, PDF, CSV) all goes through `ReportService`, which
  Iterations 177–181 aligned: letterhead, lot and record status, and
  rule-outs not counted.

No new issue or gap worth fixing. All 1322 tests pass.

## Iteration 183 (clean pass): Timeline coverage and Audit Log filters

**Reviewed:**
- **Timeline coverage.** Every specimen audit action written by
  `DatabaseService` has an entry in `InvestigationTimeline`:
  - confirm and clear (with the identification as it stood and the reason);
  - supervisor review, cancel and reinstate;
  - overrides, eluate links, runs and every field update;
  - report output (print, PDF, CSV with fingerprint);
  - reaction saves, grouped into summary entries;
  - receipt, which comes from the specimen record.

  An action without an entry is left off rather than shown raw, and a
  deleted specimen has no timeline.
- **Configuration import.** It is a laboratory-level event and is not on a
  specimen timeline. It is in the Audit Log as `import_configuration`, and
  its rule and Preferences changes are audited individually.
- **Audit Log.** Find narrows by accession, action, Windows login, record
  type and reason over the newest 5,000 events. Export uses the
  formula-safe CSV (Iteration 157).

No new issue or gap worth fixing. All 1322 tests pass.

## Iteration 184: overdue age runs from receipt in Analytics too

**Reviewed, no gap:**
- The Worklist's `OverdueTesting` item skips confirmed, cancelled and
  inactive investigations. It measures from receipt, lists only those
  awaiting additional testing, and names what the last analysis left open.
- Analytics turnaround leaves cancelled investigations out of every figure.

**Deficiency found (consistency):**
- `TurnaroundService.Compute` measured an open investigation's age from
  `CreatedDate`, the date it was entered.
- The Worklist's overdue item, and Analytics' own time-to-confirm, measure
  from `ReceivedTime`, the recorded receipt time.
- When receipt and entry differ, the same investigation could be overdue on
  the Worklist and not in Analytics, or the reverse. The overdue list also
  showed the entry date labelled as "Received".

**Bounded improvement:**
- Overdue age and the Received column come from `ReceivedTime`, which
  falls back to the entry date only when no receipt time is recorded.
- Help (Analytics) says so.

**Tests:** `TurnaroundTests.OverdueAge_RunsFromReceipt_NotFromTheDateTheSpecimenWasEntered`.
A specimen received 12 days before and entered 5 days before is overdue at
7 days; one entered 12 days before but received 5 days before is not. The
previous code reported the second instead.

All 1323 tests pass.

## Iteration 185: "received in the range" means received

**Reviewed, no gap:**
- Workload by technologist counts confirmations by confirmed date and
  reviews by review date, as its help states.
- Warnings overridden count by the date of the override.

**Deficiency found (consistency):** Help (Analytics) describes turnaround
and By ordering location as covering investigations "received" in the date
range. The code filtered three of them on `CreatedDate`, the entry date:
- `TurnaroundService.Compute`;
- `WorkloadService.ComputeByLocation`;
- `CasesForLocation`, the location drill-down.

Their medians already measured from `ReceivedTime`. A specimen received in
October but entered in November was counted in November, and the reverse.

**Bounded improvement:**
- `WorkloadService.ReceivedInRange` uses the recorded receipt time, or the
  entry date when none is recorded. All three filters use it, so turnaround,
  the location summary and its drill-down count the same investigations.

**Tests:** `ReceivedRangeTests` (2 cases):
- location workload and its drill-down count by receipt date, and the entry
  date when no receipt time is recorded;
- turnaround counts only the investigation received inside the range.

All 1325 tests pass.

## Iteration 186: Analytics overdue CSV is formula-safe; data exports are audited

**Reviewed, no gap:**
- Antibody counts use the confirmed date (`IdentifiedAt`).
- The Analytics CSV carries every section shown on screen: antibody counts,
  trend, turnaround, overdue list, workload, location, user activity and
  overrides.

**Deficiencies found (security, auditability):**
- **Formula safety.** `AnalyticsViewModel.TurnaroundCsvLines` built the
  overdue lines (accession, received, age, status) without `CsvText`. An
  accession starting with = or @ exported as a live spreadsheet formula.
  Iteration 157's guard covers `new CsvWriter(` only, so it missed this
  hand-built line.
- **Auditing.** The Analytics and Audit Log CSV exports were not recorded in
  the audit log, although both contain accession numbers. Report exports
  have been audited with a fingerprint since earlier iterations.

**Bounded improvement:**
- The overdue lines use `CsvText.Minimal` for every text value.
- `ExportAudit.Record` writes an `export_data` audit entry (entity
  "export", id "analytics" or "audit_log") with the file name, software
  version and the SHA-256 of the file written. Both exports call it after
  writing.
- Help (Analytics, Audit Log) says so.

**Tests:** `DataExportAuditTests` (3 cases):
- formula values in the accession and status are neutralized;
- plain values are unchanged;
- `Record` writes the entry with the file name and its fingerprint.

All 1328 tests pass.

## Iteration 187: a guard for hand-built CSV lines

**Reviewed, no further defect:**
- Every file HemoIQ writes was checked:
  - Reports and panel CSV use `CsvText.Writer`;
  - the Audit Log uses `CsvText.Field`;
  - all five Analytics line builders use `CsvText` since Iteration 186;
  - configuration, rules and Preferences are JSON, the manual is Markdown,
    and the reaction draft is JSON.

**Deficiency found (automated testing):** the CSV guard in
`CsvInjectionTests` checks only that `new CsvWriter(` appears nowhere but
`CsvText`. Hand-built lines are invisible to it, which is how the Analytics
overdue line escaped Iteration 157. Nothing would stop the next one.

**Bounded improvement:**
- `AnalyticsCsvGuardTests` puts a formula in every text field of every
  Analytics line builder and fails if any output field starts a cell with
  =, +, @ or tab, quoted or not.
- A second case proves the check catches the Iteration 186 line and passes
  neutralized values and numbers.
- Developer conventions say to add new line builders to it.

**Tests:** `AnalyticsCsvGuardTests` (2 cases).

All 1330 tests pass.

## Iteration 188 (clean pass): Search output and the panel compare view

**Reviewed:**
- **Search.** It has no export or clipboard path of its own; results are
  read-only grid rows. Nothing writes data outside HemoIQ from this tab.
- **Panel compare** (`PanelComparer`).
  - Antigen names from both lots are joined with an ordinal comparer, so K
    and k stay separate columns.
  - Case-insensitive comparison is used only for cell numbers and typing
    values (+, -, ++, NT), where case carries no meaning.
  - The autocontrol is left out of the cell comparison.
  - Cells only on one lot are listed separately.

No new issue or gap worth fixing. All 1330 tests pass.

## Iteration 189 (clean pass): patient history on screen and on the report

**Reviewed:**
- `SpecimensViewModel.FormatPatient` and `ReportService.AppendPatientRecord`
  print the same patient fields with the same display helpers:
  - antigen-negative units (`AntigenNegativeRequirement.Display`);
  - transfusion requirements;
  - HPC transplant (`TransplantDisplay.Text`);
  - blood bank comments.
- Both list the patient's other specimens through
  `PatientHistory.Describe(PatientHistory.OtherSpecimens(...))`, so earlier
  antibodies read the same on screen and in print.

No new issue or gap worth fixing. All 1330 tests pass.

## Iteration 190: a custom run label never hides the treatment

**Deficiency found (reporting, interpretation):**
- `PanelRun.DisplayLabel` returned a custom label on its own. A
  ficin-treated, DTT-treated, absorbed or prewarmed run labelled, say,
  "Repeat" appeared that way on the Reactions run selector, the Analysis
  explanations and the printed worksheet ("Run: Repeat").
- The treatment decides how a run's negatives are read: ficin-treated cells
  cannot rule out anti-Fya, and an absorbed aliquot has lost the antibodies
  it removes. A reader could misread the evidence.

**Bounded improvement:**
- A custom label that does not name each of the run's treatments gets the
  missing ones appended, for example "Repeat (Ficin-treated)" or "Second
  look (DTT-treated + Absorbed: rr)".
- A label counts as naming a treatment when it contains the treatment's
  keyword as a whole word (ficin, papain, dtt, prewarm, r1r1, r2r2, rr,
  auto), ignoring case. "Ficin repeat" and "Absorbed: rr (ce/ce)" are
  unchanged; "rr" inside "Error" does not count.
- The stored label is unchanged; only the display adds the treatment, so
  every screen and report that uses `DisplayLabel` carries it.
- Help (Panel run) explains it.

**Tests:** `RunLabelTreatmentTests` (10 cases): appended, already named
(5 labels), one of two named, untreated, generated, and the whole-word rule.

All 1340 tests pass.

## Iteration 191 (clean pass): compare view headers and treatment notes

**Reviewed:**
- **Reactions compare view.** Headers and difference notes take each run's
  name from `PanelRun.DisplayLabel` (`CompareReactionRow.GradeHeader`,
  `RunName`), so since Iteration 190 they carry the treatment. Column width
  is sized to the header text.
- **Status line and "Added run" message.** These use `DisplayLabel` too.
- **Treatment inference notes.** They name treatments through
  `AntigenTreatmentEffects.GetDisplayName`, the same names the label
  appends.

No new issue or gap worth fixing. All 1340 tests pass.

## Iteration 192: correcting a panel's typing makes open analyses stale

**Reviewed, no gap:**
- The editor cycles NT → + → ++ → − → NT, and offers ++ only for antigens
  with a pair (`NextEditorAntigenValue`).
- `SavePanelCellEdits` demands Record Results and requires a reason when
  the lot holds test history. It saves in one transaction and audits
  `edit_panel_typing` with the linked specimens.

**Deficiency found (patient safety):**
- Correcting a lot's typing changes the evidence behind every analysis that
  used it, for example from a vendor's corrected antigram. Staleness tracked
  only grade saves (`reactions_updated_at`) and rule or Preferences changes,
  and the snapshot fingerprint covers reactions only.
- After a typing correction, an open investigation kept showing rule-outs
  and suspected antibodies from the old typing as current. Worklist, the
  Analysis banner and Confirm ID gave no warning.

**Bounded improvement:**
- After a typing edit, `SavePanelCellEdits` marks each open investigation
  linked to the lot (no confirmed call, not cancelled) as changed since its
  last analysis. `OpenInvestigationsOnPanel` finds them, and
  `TouchSpecimenReactionsUpdated` stamps them, keeping event order
  (Iteration 151).
- Confirmed and cancelled investigations are not reopened; the audit entry
  names every linked specimen.
- The stale reason now reads "Reactions or panel typing changed since last
  analysis."
- The Panels status line says how many open investigations need
  re-analysis and names up to five.
- Help (Panels, Analysis, Worklist) explains it.

**Tests:** `PanelTypingStalenessTests` (3 cases):
- an open investigation becomes stale with the new reason;
- confirmed and cancelled ones are untouched;
- a save with no changes marks nothing.

Three tests were updated for the new wording.

All 1343 tests pass.

## Iteration 193: specimen and patient details that feed analysis mark it stale

**Reviewed, no gap:**
- Adding an extra antigen column to a lot creates NT cells, which change no
  analysis until typed. Typing goes through `SavePanelCellEdits`, which
  marks analyses stale (Iteration 192).
- Antigen display order is cosmetic.
- A run's treatments are set when the run is added; no edit path changes
  them later.

**Deficiency found (patient safety):** several specimen and patient details
are analysis inputs, but changing them after an analysis left it current:
- **Transfusion** status, date and products, and the patient's **HPC
  transplant**. These decide whether autoadsorbed negatives count as
  rule-outs (Iterations 169, 171) and whether patient typing is used.
- **Previous antibodies.** These exempt antibodies from the separation
  criterion.
- **Phenotype and genotype.** These drive the typing evidence.
- **DAT, pregnancy and RhIG, notes and specimen type.** These drive notes
  and suggestions.
- The **antigen-negative requirement**, which drives transfusion-requirement
  notes.

Recording, for example, a recent transfusion after the analysis left rule-outs
from an autoadsorbed aliquot shown as current.

**Bounded improvement:**
- `MarkAnalysisInputChanged` marks an analysed, open investigation out of
  date, as a grade change does. It leaves never-analysed, confirmed and
  cancelled investigations alone.
- It is called by the transfusion, products, DAT and pregnancy writers, and
  by `UpdateSpecimen` when type, notes, phenotype, genotype, previous
  antibodies or DAT changed (not for expiration or active).
- `SavePatient` calls it for every specimen on the MRN when the transplant
  or antigen-negative requirement changed.
- The stale reason reads "Reactions, panel typing or specimen details
  changed since last analysis."
- Help (Analysis, Worklist) lists the triggers.

**Tests:** `SpecimenInputStalenessTests` (7 cases):
- transfusion, phenotype, DAT, pregnancy and patient transplant changes
  mark the analysis stale;
- an expiration-only change does not;
- a confirmed investigation is not reopened by a patient change;
- a never-analysed one is not marked.

Five tests were updated for the wording.

All 1350 tests pass.

## Iteration 194: eluate changes mark the source's analysis stale

**Reviewed, no gap:**
- `DeletePanelRun` and `DeleteReactions` stamp the specimen as changed, and
  need a reason after analysis.
- Unlinking a panel is refused while it has reactions.
- Adding a run or linking a panel adds no evidence.

**Deficiency found (consistency, patient safety):**
- The source specimen's analysis lists each linked eluate's findings
  (`EluateEvidence`), for example "anti-K is in the eluate but not suspected
  in serum S-1 ... Review before relying on serum rule-outs".
- Linking or unlinking an eluate, analysing it, or confirming its
  identification changed those notes without marking the source's analysis
  out of date.

**Bounded improvement:**
- `SetEluateSource` marks both the former and the new source.
- `SetSpecimenLastAnalyzed` marks an eluate's linked source when the eluate
  is analysed.
- The final-call writer marks it when an eluate is confirmed or cleared
  (`MarkEluateSourceChanged`).
- All go through `MarkAnalysisInputChanged`, so a never-analysed, confirmed
  or cancelled source is untouched.
- Help (Analysis) lists the trigger.

**Tests:** `EluateSourceStalenessTests` (4 cases):
- linking makes the source stale;
- analysing a linked eluate makes the source stale;
- unlinking makes the former source stale;
- a confirmed source is not reopened by the eluate's analysis or
  confirmation.

All 1354 tests pass.

## Iteration 195: product review of Iterations 178–194

**Reviewed, no gap:**
- **Letterhead (178).** One `FacilityHeader` above the preamble serves every
  report. It is not a clinical setting, so it does not affect staleness.
- **Report sections (177, 179–181).** "Rule-outs not counted" and the lot
  and record status share their helpers (`AppendGatedRuleouts`,
  `PanelStatusLine`, `PanelStatusShort`, `GetSpecimenStatus`) across the
  text, print, PDF and CSV paths.
- **CSV (186–187).** Every writer goes through `CsvText`. Two guards cover
  it: the `new CsvWriter(` source check, and `AnalyticsCsvGuardTests` for
  hand-built lines.
- **Staleness (192–194).** Grade, panel typing, specimen detail, patient
  record and eluate changes all reach `MarkAnalysisInputChanged` or the
  order-safe timestamp writer.

**Deficiency found (patient safety):** checking the triggers against every
input `ApplyPatientTyping` and `EvaluateSeparation` read found two paths
Iteration 193 missed:
- `UpdateSpecimenMrn`. A different MRN brings another patient's transplant,
  antigen-negative requirement and prior confirmed antibodies.
- Confirming or clearing an identification on another specimen of the same
  patient. That changes the prior antibodies used for history and
  separation exemptions on the patient's open specimens.

Neither marked the open specimen's analysis out of date.

**Bounded improvement:**
- `UpdateSpecimenMrn` calls `MarkAnalysisInputChanged`.
- The final-call writer calls `MarkSamePatientSpecimensChanged`, which
  marks the patient's other analysed, open specimens.
- Help (Analysis) lists both.

**Tests:** `PatientContextStalenessTests` (4 cases):
- an MRN change makes the specimen stale;
- confirming another specimen of the patient makes the open one stale;
- clearing one makes the open one stale;
- a different patient's confirmation does not.

All 1358 tests pass.

## Iteration 196 (clean pass): one stale item per specimen

**Reviewed:**
- Staleness is a state of the specimen (`reactions_updated_at` after
  `last_analyzed_at`, a later configuration change, or settings drift), not
  a queue of events. However many edits mark a specimen, it is stale once.
- `GetWorklistItems` makes one pass over active, uncancelled specimens and
  adds at most one Stale analysis item each, with the combined reason
  (`AnalysisCurrency.StaleReason`).
- Pending Work filters the same specimen list once.
- Running analysis clears the state.

No new issue or gap worth fixing. All 1358 tests pass.

## Iteration 197 (clean pass): stale wording on screen

**Reviewed:**
- The Analysis banner, the Confirm ID out-of-date prompt, the investigation
  status strip and the Worklist detail take their text from one place:
  `AnalysisCurrency.StaleReason`, and `AnalysisFreshness` for the category.
  The broadened reason ("Reactions, panel typing or specimen details
  changed since last analysis.") therefore reads the same everywhere, and
  no screen hard-codes older wording.
- Help (Analysis, Worklist, Panels) lists the triggers.

**Housekeeping:** the `AnalysisCurrency` class comment still described only
grade saves. It now names every trigger and points to
`MarkAnalysisInputChanged`. Comment only; behavior is unchanged, so this
does not count as a finding.

No new issue or gap worth fixing. All 1358 tests pass.

## Iteration 198: indexes for per-specimen and per-panel lookups

**Reviewed, no gap:**
- `GetWorklistItems` reads the configuration change once, and every
  snapshot's settings in one query (`GetLatestSnapshotSettingsBySpecimen`).
- Reactions, runs and specimen-panel links are indexed through their
  composite UNIQUE keys.

**Deficiency found (performance, scaling):** four child-table foreign keys
had no index:
- `analysis_snapshots.specimen_id`;
- `panel_cells.panel_id`;
- `specimen_antibodies.specimen_id`;
- `specimen_ruleouts.specimen_id`.

`analysis_snapshots` gains a row with every analysis and is never trimmed.
`GetLatestAnalysisSnapshot` is called once per specimen by `SettingsDrifted`,
`GetAnalysisStaleReason`, the All Specimens report and the status strip, so
each call scanned every snapshot. A report over thousands of specimens did
quadratic work, and panel loads scanned all cells. Cascading deletes scanned
these tables too.

**Bounded improvement:** `CreateForeignKeyIndexes` adds the four indexes:
- `ix_snapshots_specimen` on (specimen_id, id), which also serves "latest";
- `ix_panel_cells_panel`;
- `ix_specimen_antibodies_specimen`;
- `ix_specimen_ruleouts_specimen`.

They are created with IF NOT EXISTS at start-up, so existing databases and
archives gain them on the next open.

**Data/schema:** four new indexes; no table or column changes.

**Tests:** `ForeignKeyIndexTests` (4 cases): SQLite's `EXPLAIN QUERY PLAN`
for each lookup uses its index and does not scan the table.

All 1362 tests pass.

## Iteration 199: audit log indexes for action lookups

**Reviewed, no gap:**
- Specimens by MRN use `ix_specimens_mrn`.
- Search reads every panel cell by design: it is a search across all lots.

**Deficiency found (performance, scaling):** the audit log is the largest
table (every reaction save, edit and report). Its only index,
`ix_audit_entity`, leads with entity_type, so action-only and
action-plus-entity queries scanned the whole log:
- `LastAnalysisConfigurationChange`, called once per specimen by the All
  Specimens report, the status strip and staleness;
- the analysis-time subquery recording `analysis_config_seq`;
- `CountAuditEvents(action, entity_id)`, per specimen in investigation
  status;
- the override analytics query.

**Bounded improvement:**
- `ix_audit_action` on (action, id), which also serves the latest event.
- `ix_audit_action_entity` on (action, entity_id).
- Both are created with the other indexes at start-up, so existing
  databases and archives gain them on the next open.

**Data/schema:** two new indexes; no table or column changes.

**Tests:** `ForeignKeyIndexTests` gains 2 cases (6 in all): the latest
configuration change and the per-specimen action count use their indexes
and do not scan the audit log.

All 1364 tests pass.

## Iteration 200 (clean pass): start-up work and the reaction save path

**Reviewed:**
- **Start-up.**
  - Every migration checks its column or table first and returns
    immediately once applied (for example `MigrateReactionsToRuns` stops
    when `reactions.run_id` exists).
  - Index creation is IF NOT EXISTS.
  - Expiry deactivation is two UPDATEs over active rows.

  An existing database therefore opens with a handful of cheap statements.
- **Reaction saves.** Each cell does a few indexed lookups: the run, the
  specimen lock, the cell on the panel (`ix_panel_cells_panel` since
  Iteration 198), and the before value via the reactions UNIQUE key. Batch
  saves run in one transaction, and unchanged saves write nothing.
- **Performance tests.** `PerformanceTests` time specimen and panel listing,
  single and bulk analysis, every report type and Search on the seeded
  database. Reaction saving has no dedicated timing test; its cost is the
  indexed per-cell work above.

No new issue or gap worth fixing. All 1364 tests pass.

## Iteration 201 (clean pass): resources over a long session

**Reviewed:**
- Every `CreateCommand()` and `ExecuteReader()` in HemoIQ is wrapped in
  `using`.
- Short-lived `DatabaseService` instances (headless runs, archive creation)
  are in `using` blocks.
- The one long-lived connection, `MainViewModel.Database`, is disposed in
  `MainViewModel.Dispose`, which also unsubscribes the static
  `AppSettings.Changed` handler. That runs when the main window closes,
  which also closes the help window (`HelpService.Close`).
- Dialogs are modal and released on close.

No new issue or gap worth fixing. All 1364 tests pass (this run took
11 minutes against the usual 2–3, from machine load; no code had changed).

## Iteration 202 (clean pass): export file errors

**Reviewed:**
- The report CSV and PDF exports, the Panels CSV export, configuration and
  preference exports, and the Analytics export catch write failures and
  show "Export failed" with the reason.
- The Audit Log CSV, help manual and Rules exports have no local catch. A
  locked file or read-only folder there reaches the application's
  unhandled-exception handler, which logs the error and shows its message;
  the app keeps running and nothing is lost.
- The Audit Log export records its `export_data` audit entry only after the
  file is written, so a failed write leaves no false export record.
- Save dialogs ask before overwriting an existing file, and archives always
  use a new file (Iteration 160).

No new issue or gap worth fixing. All 1364 tests pass.

## Iteration 203: rule import checks separation exemption antigens

**Reviewed, no gap:**
- Rule, preference and configuration-bundle imports parse the whole file
  and show a preview before anything is written.
- Malformed JSON, a wrong format or a newer version is refused with a
  reason. Applying is one transaction, so a failed write changes nothing.

**Deficiency found (data integrity, configuration):** the Rule dialog
refuses unknown antigens in "Separation not required from". Rule import
did not check them, so a file listing for example `Le(a)` stored an
exemption that matched no partner and never applied, with no warning.
The configuration bundle import shares this path.

**Bounded improvement:** `RuleTransfer.Preview` refuses such an entry with
"Separation exemption lists unknown antigen(s) …; antigen names are
case-sensitive". The rest of the file still imports. The list is ignored
when the entry is not exempt, as before.

**Data/schema:** none.

**Tests:** `RuleTransferTests` gains one case. An unknown partner is
refused, while known partners (including the `anti-` prefix) import. A
list on a non-exempt rule is not stored.

**Docs:** the Rules help topic lists the new refusal.

All 1365 tests pass.

## Iteration 204: cut-off rows in a lab panel CSV are refused

**Reviewed, no gap:**
- Lab and vendor lot imports store the new lot inactive, inside one
  transaction.
- A file with errors is refused before anything is written.
- A stored lot that holds test history cannot be replaced.

**Deficiency found (patient safety, data integrity):**
- The CSV reader tolerates missing fields, so a row shorter than the
  header (a truncated file, or a lost line end) read its missing fields as
  blank.
- In a lab CSV a blank reads as negative, so every antigen after the cut
  was stored as typed negative.
- Missing typing turned into false negative typing, which the importer
  otherwise avoids (an antigen with no column is NT).

**Bounded improvement:**
- `PanelCsvService.Import` refuses a row with fewer fields than the header,
  with the message "Row N cell X: has A of B columns; the file may be cut
  off. Missing values are not read as negative. Re-export the file."
- The whole import is then refused, so no lot is stored.
- A blank value inside a complete row still reads as negative. That is the
  long-standing lab-sheet convention, now stated in the help.

**Data/schema:** none.

**Tests:** `LabCsvImportTests` gains 2 cases:
- a cut-off row is refused with the column counts, and no panel is stored;
- a blank field in a complete row still reads as negative.

**Docs:** the Panels help topic explains NT for a missing column, the
blank-means-negative convention and the refusal of short rows.

All 1367 tests pass.

## Iteration 205: JSON panel files with the wrong shape are refused, not thrown

**Reviewed, no gap:**
- Malformed JSON, XML and XLSX files are caught by their parsers and
  refused with "Invalid JSON", "Invalid XML" or "Could not read XLSX".
- Vendor CSV and XLSX grids read a blank or missing field as NT, so a
  short row cannot type negative there. The import review then flags the
  partly typed antigens.

**Deficiency found (reliability, import):**
- A JSON file that parses but has the wrong shape got past the parser's
  error handling: a root that is a string, number or null, or a cell that
  is a number or string.
- `JsonElement.TryGetProperty` throws `InvalidOperationException` on a
  non-object, and the parser caught only `JsonException`.
- Such a file reached the application's unhandled-exception handler
  instead of the import's "Could not import panel" list. Nothing was
  stored, but the user saw a raw .NET message.

**Bounded improvement:**
- `StructuredPanelParser.ParseJson` refuses a non-object root with "JSON
  root must be a panel object, not a string/number/null".
- The property lookup treats a non-object as having no properties.
- A malformed cell is then reported by the existing validation ("A cell
  is missing cellNumber."), and the file is refused as a whole.

**Data/schema:** none.

**Tests:** `StructuredImportParserTests` gains 4 cases:
- three non-object roots;
- non-object cells and an array where the antigens object belongs,
  through both the vendor parser and the lab import path.

All 1371 tests pass.

## Iteration 206 (clean pass): vendor download failures

**Reviewed:**
- **Outbound requests.** `VendorHttpClient` sends GET requests only, over
  HTTPS, to an allow-list of vendor document hosts. It uses a 45-second
  timeout and sends no specimen or patient fields. It never signs in,
  stores credentials or works around a vendor page: a listing without a
  public download URL is refused with "Import a vendor file instead".
- **Failures.** A non-success status throws. Listing, downloading and file
  import in `DownloadFromVendorDialog` each catch the failure and show its
  message. A cut-off or partial download fails parsing and is refused with
  the parser's reasons.
- **Storage.** Downloaded lots go through `VendorPanelImportService`: stored
  inactive in one transaction, with no replacement of a lot that holds test
  history.
- **Headless test.** `VendorLiveImport`, used only by the headless
  live-import test switch, uses the same import.

**Noted, not a gap:**
- Redirects are followed without re-checking the final host. An HTTPS
  vendor site redirecting to its own CDN would otherwise break.
- Requests carry no patient data, and whatever comes back is parsed,
  reviewed and stored inactive before anyone can use it.

No new issue or gap worth fixing. All 1371 tests pass.

## Iteration 207 (clean pass): roles and permissions on write paths

**Reviewed:**
- **Database writers.** Every SQL write is in `DatabaseService`.
  `RecordResultsPermissionTests.EveryPublicDatabaseWriter_ChecksAPermission_OrIsListed`
  finds every public writer, directly or through another writer, and
  requires `EnsureCanRecord()` or a stricter `AccessControl.Demand` unless
  the method is listed with a reason. The listed ones are:
  - the audit log, schema creation and migrations;
  - derived analysis output and its timestamps;
  - helpers called only by guarded panel creation or import;
  - the empty default run a viewer needs to open the grid.

  An independent scan this pass found the same set, and the test rejects
  stale entries in the list.
- **Commands.** Commands created without a permission gate are read-only:
  Refresh, Search, Clear, report and analytics export, and Worklist
  filters. Write commands are gated through `SessionAccess`, so their
  buttons are disabled for read-only roles, and the writer checks again.

No new issue or gap worth fixing. All 1371 tests pass.

## Iteration 208: panel detail edits keep old values and cannot skip activation review

**Reviewed, no gap:** these writers already record before and after
values:
- specimen details, transfusion, products, DAT, hemolysis, origin,
  encounter, collection, pregnancy and MRN;
- final call confirm and clear;
- panel typing edits.

**Deficiency found (auditability, patient safety):**
- **Audit.** `update_panel` recorded only the new name, lot and vendor and
  the cells added or removed. It kept no old values and left out
  expiration, cell range, enzyme treatment, catalog number, product line
  and the active flag. Correcting the lot number or expiry of a lot that
  holds test history overwrote it with no trace of what it had been.
- **Activation.** The Details dialog's Active box could switch an inactive
  (for example imported) lot on directly. That skipped the antigram review
  prompt, the antigen-coverage check against the prior lot and the
  `activate_panel` audit entry, which the Activate button and the import
  review always go through.

**Bounded improvement:**
- `UpdatePanel` reads the stored panel first and refuses a missing one.
  The audit entry lists each changed field in BeforeJson and AfterJson,
  with `cellsAdded` and `cellsRemoved` kept. Unchanged fields are left out,
  and blank equals null.
- `PanelEditActivation` decides the active flag the edit may save: never
  inactive to active.
- `PanelsViewModel.EditPanel` saves the details with the lot still inactive.
  When Active was ticked, it then runs the normal Activate prompt. Answering
  No leaves the lot inactive and says so on the status line.

**Data/schema:** none. Older `update_panel` entries keep their after-only
form.

**Tests:** `PanelEditAuditTests` (8 cases):
- old and new values for lot, expiry and enzyme treatment, with unchanged
  fields left out;
- an edit that changes nothing records an empty before, with blank treated
  as null;
- a missing panel is refused with no audit entry;
- the four stored/requested combinations of the active-flag decision;
- a source guard that the Details edit saves through the guard and calls
  `ActivatePanel`.

**Docs:** the Panels help topic describes the audit and the activation
prompt on Details.

All 1379 tests pass.

## Iteration 209: copied lots start inactive and record where their antigram came from

**Reviewed, no gap:**
- Rules have no enable/disable switch. A rule applies once saved, and
  every save is audited.
- Specimen deactivation (`set_specimen_active`) records its before value.

**Deficiency found (patient safety, auditability):** Copy panel had three
problems:
- **Active by default.** Copy creates a new lot carrying another lot's
  antigram, and the Copy dialog started with Active ticked. A new lot
  could be offered for testing with typing nobody had checked against its
  own sheet, skipping the activation review.
- **No provenance.** `CopyPanelCells` wrote no audit entry, so the log
  showed only `add_panel`, not where the typing came from.
- **Overwrite of tested lots.** As a public writer it would overwrite the
  antigram of a lot with recorded reactions. Only lots behind a confirmed
  identification were refused, although its one caller copies into a
  brand-new lot.

**Bounded improvement:**
- The Copy dialog starts with Active unticked, and the copy is saved
  inactive whatever the box says.
- Ticking Active runs the normal Activate review after the copy. The
  status line asks the user to check the copied antigram against the new
  lot's sheet.
- `CopyPanelCells` writes `copy_panel_typing`: source panel, name, lot,
  vendor and cell count. It is removed from the unaudited list in
  `AuditCoverageGuardTests`.
- `CopyPanelCells` refuses a target that holds test history (reactions or
  a confirmed identification), naming the usage.

**Data/schema:** none; a new audit action `copy_panel_typing`.

**Tests:** `PanelEditAuditTests` gains 3 cases (11 in all):
- the copy audit names the source lot, and the target stays inactive;
- copying over a lot with recorded reactions is refused and writes no copy
  audit;
- a source guard that Copy starts inactive and activates only through
  `ActivatePanel`.

**Docs:** the Panels help topic describes Copy's inactive start, the
review and the audit.

All 1382 tests pass.

## Iteration 210: activating an expired lot warns first and is recorded

**Reviewed, no gap:**
- Screening cells have no separate model. They are panel lots, so the
  panel rules (inactive until reviewed, audited edits) cover them.
- Every way a lot is created or switched on was listed: manual Add (cells
  start NT), Copy and Details (through Activate since Iterations
  208–209), the lab import review, the vendor download review, and
  Activate.

**Deficiency found (patient safety, auditability):**
- Expired lots are deactivated automatically at start-up, but every
  activation route switched an expired lot back on without mentioning the
  expiry: Activate, the Details and Copy routes into it, and both import
  reviews.
- Reactions recorded afterwards were flagged on the Worklist only later,
  as expired-reagent use.
- The `activate_panel` audit entry did not say the lot was past its date.

**Bounded improvement:**
- `PanelEditActivation.ExpiryWarning` returns "EXPIRED: this lot expired
  on …; reactions recorded on it are flagged as expired-reagent use on the
  Worklist", or null.
- The Activate prompt shows the warning with a warning icon.
- Both import reviews ask "Activate it anyway? No keeps it inactive"
  (`PanelsViewModel.ConfirmExpiredActivation`).
- `SetPanelActive` adds "Activated after expiry (date)" to the audit
  reason, together with any coverage gaps.
- Activation stays the user's decision, for example for a documented
  extension.

**Data/schema:** none.

**Tests:** `PanelEditAuditTests` gains 3 cases (14 in all):
- the warning appears only for a date that has passed;
- the audit reason on activating an expired lot, and none on a current
  lot;
- a source guard that every activation route warns.

**Docs:** the Panels help topic describes the warning and the audit.

All 1385 tests pass.

## Iteration 211 (clean pass): results recorded on a lot after it expired

**Reviewed:**
- **Reaction entry.** Saving grades on an expired lot asks "Panel lot …
  expired …; results from expired reagent red cells will be flagged for
  review. Record these reactions anyway?". No keeps them unsaved and says
  so in the save status.
- **Analysis.** `ExpiredReagentCheck` finds runs recorded after the lot's
  expiry. Each finding becomes a critical "REVIEW:" suggestion on the
  Analysis tab, naming the run, recorded date and expiry, and asking for
  documented review before interpretation.
- **Reports.** Report suggestions print the same line.
- **Worklist.** The "Expired reagent" item lists open investigations with
  such runs until a reviewed final call clears them.
- **Tests.** `ExpiredReagentTests` covers the finding, in-date and ungraded
  cases, the analysis note and the Worklist item.

No new issue or gap worth fixing. All 1385 tests pass.

## Iteration 212 (clean pass): the Worklist as a whole

**Reviewed:**
- **Item kinds.** All 15 `WorklistKind` values have a producer in
  `GetWorklistItems` (expiring and expired come from one shared
  expiration helper) and at least one test file.
- **Open.** `NavigateToWorklistItem` routes the Reactions, Analysis,
  Panels, Rules and Specimens targets to tab indices that match
  MainWindow's tab order (Worklist 0 through Rules 8).
- **Hidden targets.** Opening an inactive or filtered-out lot turns on
  Show inactive and clears the list filter, so the item's target is
  always selected. Specimens are made visible the same way.
- **Counts.** The tab and the status strip both read one
  `GetWorklistItems` result, so their counts cannot differ.
- **Clearing.** Items are recomputed from current data on every refresh,
  so a resolved condition (reviewed, reanalysed, retyped, deactivated)
  drops off with nothing stored to go stale.

No new issue or gap worth fixing. All 1385 tests pass.

## Iteration 213: Search marks cells on inactive or expired lots

**Reviewed, no gap:**
- Search clauses compare the stored value exactly. SQLite `=` and
  warehouse `antigen_name` are case-sensitive, so E and e differ.
- "+" matches + or ++ only, and "−" matches only a cell typed negative,
  never NT.
- Zygosity filtering requires typed partners.

**Deficiency found (patient safety, workflow):**
- Search spans every stored lot and is how selected cells are found
  (for example K− E+ cells). Each result's panel was built with only its
  id, name, lot and vendor.
- A cell on a deactivated lot, or one past its expiration date (expired
  lots are deactivated at start-up), looked exactly like a cell in
  current use. Nothing on the row or in the count said otherwise.

**Bounded improvement:**
- `SearchCellsByProfile` reads each result lot's active flag and
  expiration date.
- `SearchResultRow.LotStatus` returns "expired (date)" (which wins, being
  the reason), "inactive", or null.
- The Lot column shows `LotDisplay` ("LOT-7 · inactive") in red italics
  for a lot not in use. The raw `LotNumber` is kept for keys.
- The result count adds "N are on an inactive or expired lot (marked in
  the Lot column)", and the selected-row status names the lot status.
- No column was added, so the frozen panel/lot/cell columns and the
  antigen column ordering are unchanged.

**Data/schema:** none.

**Tests:** `SearchLotStatusTests` (8 cases):
- results carry active and expiry for a current, an inactive and an
  expired lot;
- the status rules, with expired winning;
- the count wording for one lot and for several;
- the selected-row label.

**Docs:** the Search help topic describes the marking.

All 1393 tests pass.

## Iteration 214: investigation search dates by received time

**Reviewed, no gap:**
- Investigation search covers active and inactive specimens: `GetAllSpecimens`
  reads every row.
- Open goes to the Specimens tab and makes the row visible by turning on
  Show inactive and clearing the list filter.
- Every word must match some field, ignoring case. A From date after the
  To date, or an empty search, is refused with a message.

**Deficiency found (workflow, reporting consistency):**
- The help says From and To limit results to specimens *received* in the
  range, but `InvestigationSearch` compared the entry date
  (`created_date`).
- A specimen received in May but entered in November (from a downtime log,
  or a correction) appeared in a November search and not in May.
- That disagreed with Analytics, which counts by the received time since
  Iteration 184. The Date column showed the entry date too.

**Bounded improvement:** the range uses `WorkloadService.ReceivedInRange`:
the received time when recorded, otherwise the entry date. The Date
column and the newest-first order use the same date.

**Data/schema:** none.

**Tests:** `InvestigationSearchTests` gains one case. A specimen entered
in November but received in May is found by a May range, shows
2026-05-02, and is not found by a November range. The existing inclusive
range test still passes.

**Docs:** the Search help topic defines the received date and the Date
column.

All 1394 tests pass.

## Iteration 215 (clean pass): other date filters, received versus entered

**Reviewed:**
- **Received time.** Analytics (workload, location, turnaround) and the
  Worklist's overdue-testing age use the received time, with the entry
  date as fallback (Iteration 184). Investigation search now matches
  (Iteration 214).
- **Purge and archive.** These cut by entry date (`created_date <
  cutoff`). That is deliberate for record retention, and the Purge help
  says "Delete specimens created before".
- **No other ranges.** The Reports tab has no specimen date range. The
  Audit Log has a text filter only, and its CSV writes the UTC event time.
- **Order only.** Other `created_date` uses only order lists: MRN history,
  same-encounter specimens and eluate links.

No new issue or gap worth fixing. All 1394 tests pass.

## Iteration 216 (clean pass): patient history across specimens

**Reviewed:**
- **MRN matching.** `GetSpecimensByMrn` compares exactly, so every MRN
  write must be normalized (trimmed, upper-case). Every write is: the
  specimen MRN, the patient record and its lookup all call
  `PatientHistory.NormalizeMrn`, and have done since the MRN column was
  added. Specimens are created without an MRN, and archive restore copies
  rows this app wrote. No stored MRN can miss the lookup on case.
- **Carried forward.** Only confirmed final calls on other specimens are
  carried forward, never suspected lists. Each antibody cites its earliest
  confirmation, and the explanation says it is historical evidence, not a
  current identification. Keys keep antigen case (anti-E and anti-e
  differ). A cancelled investigation cannot carry one: cancelling is
  refused once a final call exists.
- **MRN correction.** Since Iteration 195, an MRN change marks both the
  old and new patients' open investigations for re-analysis. The same-MRN
  list follows the stored value.
- **Reports.** `PatientHistory.Describe` gives one line per other specimen
  with its accession, date, encounter, confirmed identification, DAT,
  transfusion and any recorded phenotype or genotype.

No new issue or gap worth fixing. All 1394 tests pass.

## Iteration 217: one antigen-negative review note per antibody

**Reviewed, no gap:**
- **Sources.** `TransfusionRequirementCheck` checks every known antibody:
  historical antibodies from the patient's previous-antibody text,
  antibodies confirmed on the same MRN's other specimens, and the current
  final call. Suspected antibodies are not checked.
- **Structured list.** The structured Antigen-negative list satisfies the
  check case-sensitively, so anti-C is not covered by "c".
- **Free text.** A requirement written only in the free text prompts the
  user to add it to the structured list.
- **No patient record.** Without a patient record the note says so.
- **Wording.** Every note ends "Not an order" and reaches the Analysis tab
  and report suggestions as a critical "REVIEW:" line.

**Deficiency found (UX, alert noise):**
- Historical and earlier-specimen antibodies are normalized to "anti-" +
  antigen, but the typed final call is not. The check removed duplicates
  by exact text.
- A final call typed "Anti-E", for a patient whose history also lists
  anti-E, produced the same critical REVIEW note twice. Repeated critical
  lines train readers to skim them.

**Bounded improvement:**
- The check keys each antibody with `PatientHistory.AntibodyKey`. The
  prefix is case-insensitive, and the antigen keeps its case and is
  trimmed.
- There is one note per antibody, anti-E and anti-e still give two, and
  text that is not an antibody (for example "warm autoantibody") is
  skipped as before.

**Data/schema:** none.

**Tests:** `AntigenNegativeRequirementTests` gains one case. "anti-E",
"Anti-E", "ANTI- E", "anti-e" and "warm autoantibody" give exactly two
notes, for E and for e.

All 1395 tests pass.

## Iteration 218 (clean pass): clinical-significance list and antibody guidance

**Reviewed:**
- **No guidance catalog.** HemoIQ has no per-antibody guidance catalog, so
  no clinical significance, phase or dosage text is generated for an
  antibody it does not know. Guidance comes from the evidence: dosage
  notes, phase-specific reactivity notes, untypeable-specificity
  classification and additional-testing needs.
- **Core list.** The "All clinically significant antibodies ruled out"
  (ACS) statement uses the lab's configured list. Its fixed core of Rh
  (D, C, c, E, e), Kell (K, k), Duffy, Kidd and Ss cannot be removed.
- **Lab additions.** Lea, Leb, M and N are a lab-editable default
  addition, matching the common practice of ruling out the usual
  antibodies while leaving their significance to the lab.
- **Gaps in the list.** An ACS antigen that is NT on the panel is listed
  as not evaluated rather than counted as ruled out. The report states
  which list was used ("lab-configured").

No new issue or gap worth fixing. All 1395 tests pass.

## Iteration 219 (clean pass): Analytics figures, drill-downs and CSV

**Reviewed:**
- **Cancelled investigations.** Every figure leaves them out: antibody
  frequency, technologist workload, location workload and turnaround. The
  drill-down lists (`CasesForAntibody`, `CasesForTechnologist`,
  `CasesForLocation`) use the same filters as the counts beside them.
- **Confirmations and reviews.** Both are counted by their own timestamp
  in range, and the case list shows the same rows.
- **Antibody frequency.** It shares `ConfirmedInRange` between the summary
  and its cases. Each specimen counts once per specificity, with prefix
  case ignored and antigen case kept.
- **Location and turnaround.** These use the received time (Iteration
  184).
- **CSV export.** It writes the on-screen collections (the antibody rows,
  turnaround, technologist and location workload) instead of recomputing,
  so it matches the screen. It records an `export_data` audit entry and
  guards against spreadsheet formulas (Iterations 186–187).

No new issue or gap worth fixing. All 1395 tests pass.

## Iteration 220: an eluate cannot be linked to another patient's specimen

**Reviewed, no gap:**
- **Existing rules.** The link refuses non-eluates, the eluate itself, a
  missing source and an eluate source. It is audited, and it marks both
  the old and the new source stale (Iteration 194).
- **Analysis and final call.** Analysing or confirming the eluate marks the
  source stale.
- **Evidence.** Eluate evidence is a reviewable note and never changes the
  source's candidates.
- **Locking.** A confirmed eluate is locked like any specimen.

**Deficiency found (patient safety):**
- `SetEluateSource` never compared the two specimens' MRNs. An eluate could
  be linked to another patient's serum through a mistyped accession, and
  its findings ("Eluate … contains anti-K", "panreactive…") then appeared
  as evidence in the wrong patient's analysis and reports.
- Changing either specimen's MRN under an existing link could separate
  them the same way.

**Bounded improvement:**
- `SetEluateSource` refuses a link when both MRNs are recorded and differ,
  with the message "belong to different patients (MRN a and b)".
- The check runs even when the link is unchanged, so a save that changes
  only the eluate's MRN is caught. The save sets the MRN first, then calls
  this.
- `UpdateSpecimenMrn` refuses a source MRN that would differ from a linked
  eluate's.
- The specimen save is one transaction, so a refusal keeps nothing.
- Moving an eluate to the other patient's own source in the same save is
  allowed, and a blank MRN on either side is not refused.

**Data/schema:** none.

**Tests:** `EluateEvidenceTests` gains 2 cases:
- a link to another patient is refused with no audit entry; the same
  patient with MRN case differing on entry is linked, and a missing MRN is
  allowed;
- a source MRN change under a link is refused; an eluate save with a new
  MRN but the old link is rolled back; relinking to the new patient's
  source in the same save succeeds.

**Docs:** the Specimen dialog help topic states the same-patient rule.

All 1397 tests pass.

## Iteration 221 (clean pass): other cross-specimen links and the same-patient rule

**Reviewed:**
- **Transfusion history.** `TransfusionHistoryCheck` reads only
  `PatientHistory.OtherSpecimens`, the same MRN, so another patient's
  transfusion can never make this patient's typing unreliable. The receipt
  checklist uses that finding.
- **Prior antibodies and history lines.** These are MRN-keyed too
  (Iteration 216).
- **Encounter number.** It is used only by `EncounterCheck`, which warns
  at save when the encounter is already recorded on a specimen with a
  different MRN. It never pulls evidence across. A warning rather than a
  refusal is right, because either label may be the wrong one and the
  user must check it.
- **Eluate link.** This was the one link that fed another specimen's
  evidence into an analysis without a patient check. Fixed in Iteration
  220.

No new issue or gap worth fixing. All 1397 tests pass.

## Iteration 222 (clean pass): reaction grid entry and paste

**Reviewed:**
- **Paste parsing.** `GradePaste.ParseBlock` checks every value strictly.
  One unrecognized value refuses the whole paste, naming the line and
  column. A blank inside the block is NT, and trailing blank lines are
  dropped.
- **Paste size.** A block with more rows than the cells from the selected
  one down, or more columns than the phases from the selected one right,
  is refused ("only N cell(s) remain…"), so a run with fewer cells cannot
  be overfilled.
- **Overwrites.** Pasting over a different entered grade lists the changes
  and asks first. A pasted grid is unsaved until Save, which runs the
  usual checks (expired lot, reason after analysis).
- **Read-only sessions.** A view-only session gets no grade keys and no
  paste.
- **Tests.** `GradePasteTests` covers these cases.
- **Check cell.** It counts only where AHG is negative
  (`RunContext.CheckCellFailed`). A CC grade beside a positive AHG is
  ignored by analysis, so it cannot change a rule-out.

No new issue or gap worth fixing. All 1397 tests pass.

## Iteration 223: a new run is never pre-recorded with another run's grades

**Reviewed, no gap:**
- **Delete run.** Refused after a confirmed identification, needs a reason
  for graded cells after analysis, and audited with every deleted grade.
- **Labels.** Run labels always name the treatment (Iteration 190).
- **Untreated run.** The untreated run cannot be deleted.

**Deficiency found (patient safety, data integrity):**
- **Copied grades saved as results.** The Add run dialog's "Copy grades
  from the current run" was ticked by default. It saved the viewed run's
  grades at once as recorded results on the new run (`SaveReaction` /
  `CopyReactions`), for any treatment.
- **Clinical effect.** A ficin, DTT or adsorbed run created that way
  claimed the treatment changed nothing. For example, Fya+ cells still
  reacting after ficin argues against anti-Fya, from results nobody
  observed. Any cell the user forgot to re-grade fed analysis as a
  treated result.
- **Stale treatment text.** The dialog's hand-written lines contradicted
  the engine. They said ficin destroys Lea and Leb (the engine treats them
  as enhanced since Iteration 166), left out Inb and others, and left
  anti-f off the rr adsorption (Iteration 168).

**Bounded improvement:**
- `RunTreatmentText.CanCopyGrades` allows copying only into an untreated
  repeat. The dialog disables and clears the option for any cell or serum
  treatment, with a tooltip explaining why, and it now starts unticked.
- Copied grades are never saved. `PrefillUnsaved` places them in the new
  run's grid as unsaved edits, which mark the grid dirty and go to the
  recovery draft. The status line says "pre-filled …, NOT saved: check
  each against this test, then Save."
- `DatabaseService.CopyReactions` is removed.
- `RunTreatmentText.Describe` builds the dialog text from
  `AntigenTreatmentEffects` (the new `AntigensWithEffect`,
  `GetAbsorbedAntibodies`, `GetNonInterpretablePhases`), so it cannot drift
  from analysis again.

**Data/schema:** none.

**Tests:** `RunTreatmentTextTests` (9 cases) replace the removed
`CopyReactions` test:
- ficin text equals the engine's destroyed list (Fya and Inb destroyed;
  Lea and Leb enhanced, not destroyed), and papain matches ficin;
- the DTT, rr (with anti-f), R1R1 and prewarmed lines;
- copying is allowed only with no treatment (5 combinations);
- a source guard that AddRun never calls `SaveReaction` and the option
  starts off.

**Docs:** the Add run help explains the untreated-only, unsaved pre-fill
and that the treatment note comes from the analysis tables.

All 1404 tests pass.

## Iteration 224: enzyme pattern notes take their antigen examples from the engine

**Reviewed, no gap:**
- **Help topics.** Special Panels, Ruled-out, Reactions and Add run agree
  with `AntigenTreatmentEffects`: Lewis is enhanced by enzymes, Inb is
  destroyed by ficin, papain and DTT, Kell is destroyed by DTT, and rr
  adsorption removes anti-f.
- **Adsorption explanations.** These read
  `AntigenTreatmentEffects.GetAbsorbedAntibodies`.
- **Dosage.** Dosage uses the single `AntigenConstants.AntitheticalPairs`.
- **DTT classifier.** It names antigens only from the run's cells.

**Deficiency found (explainability, clinical accuracy):**
- When an enzyme-destroyed pattern could not name a specific lost
  antigen, `EnzymeTreatmentPatternClassifier` fell back to hand-written
  text: "consistent with antibodies to enzyme-destroyed antigens (Fya/Fyb,
  M/N, S/s, **Lewis**, Xg)".
- Lewis antigens are enhanced by ficin and papain (Iteration 166), and the
  help says a pattern that disappears on enzyme cells does not point to
  Lewis, so the note pointed the reader the wrong way.
- The enhanced fallback ("Rh, Kidd, P1") left Lewis out.

**Bounded improvement:** both fallbacks now come from
`EnzymeTreatmentPatternClassifier.PanelExamples`: the standard panel
antigens in the ficin table with that effect. The destroyed list is M, N,
S, s, Fya, Fyb and so on; the enhanced list adds Rh, Kidd, P1 and Lewis.

**Data/schema:** none.

**Tests:** `EnzymePatternExampleTests` (2 cases):
- the destroyed examples include Fya and S and not Lea or Leb, the
  enhanced examples include Lea and Jka, and all are standard antigens;
- a source guard against the old hand-written lists.

All 1406 tests pass.

## Iteration 225: the ACS exception is worded as a score, not a probability

**Reviewed, no gap:**
- **ID cell rule.** The help describes the 3 + 3 rule as the count of
  antigen-positive reactive and antigen-negative nonreactive cells, and
  the engine checks exactly that (`MeetsIdentificationRule`).
- **Score grid.** The Suspected grid shows Score, Fisher p-value and
  Pattern Score as separate columns.

**Deficiency found (explainability, report accuracy):**
- An antibody becomes an ACS exception when its combined score is at
  least `AcsProbabilityCutoff` (0.95). The combined score is the mean of
  (1 − Fisher p) and the share of reactive cells carrying the antigen, a
  ranking heuristic and not a probability.
- Four places said otherwise:
  - the Analysis suggestion: "anti-X ≥95% probability";
  - the ACS-with-exception prompt: "has ≥95% probability";
  - the status line: "≥95%";
  - the printed report: "ruled out, with ≥95% exception".
- A reader of the record could take it as a 95% statistical certainty.

**Bounded improvement:**
- `AntigenConstants.AcsExceptionCriterion` ("combined score ≥ 0.95", built
  from the cutoff) is the one wording used by all four.
- The suggestion and the prompt add "from the Fisher test and the reaction
  pattern; a score, not a probability".
- The report reads "ruled out, except antibodies with a combined score ≥
  0.95 (named in the result below)".

**Data/schema:** none. The cutoff and the logic are unchanged.

**Tests:** `AcsEvaluationTests` gains one case: the criterion text, and a
source guard that "≥95%" and "95% probability" no longer appear in the
analyzer, report service or Analysis view model.

**Docs:** the Preferences help explains the exception and the combined
score.

All 1407 tests pass.

## Iteration 226: reports label suspected-antibody percentages as scores

**Reviewed, no gap:**
- **Before a final call.** Reports never say "confirmed" or
  "identification" before one. Unconfirmed lists are headed "Suspected
  antibodies (unconfirmed)".
- **After a final call.** The analyzer's list is "Analyzer suspected (for
  reference)".
- **Evidence sections.** Pattern, absorption and Explain Analysis sections
  each say "not a diagnosis".
- **Dosage and high-support notes.** These already say "support score".

**Deficiency found (report accuracy, explainability):**
- Every suspected-antibody line on the Specimen Summary, Analysis Results
  and Clinical Identification reports printed the combined score as a
  bare percentage ("anti-E  85.0%"), with no label.
- The All Specimens list printed "anti-E (85%)", and the adsorption note
  said "Ordinary probability and pattern scores".
- On screen the column is headed Score, but on paper the bare percentage
  reads as a probability of the antibody being present, which the engine
  does not compute. This is the same issue as the ACS wording in
  Iteration 225.

**Bounded improvement:**
- `ReportService.ScoreLabel` prints "score 85.0%" on every suspected line.
- The All Specimens list reads "(score 85%)".
- The adsorption note reads "Ordinary Fisher and pattern scores".
- The CSV column was already headed Score.

**Data/schema:** none.

**Tests:** `AcsEvaluationTests` gains one case. It checks the label, and
builds a specimen with a real suspected anti-E: the three report types
print "anti-E  score N.N%", never a bare percentage, and no anti-E line
says "probability".

**Docs:** the Reports help topic explains the score.

All 1408 tests pass.

## Iteration 227: the Analysis Summary labels suspected percentages as scores

**Reviewed, no gap:**
- **Grids.** The Suspected, Explain and Combinations grids head their
  percentages "Score".
- **Pattern Evidence.** It heads its ratio "Confidence", which the help
  defines as matches divided by matches plus mismatches. Both are labelled
  on screen, so the percentage convention stays.

**Deficiency found (explainability):**
- The Analysis tab's Summary is a read-only text digest that users select
  and copy into notes and messages.
- It printed suspected antibodies as "anti-E  (85.0%)" with no label: the
  same unlabelled percentage Iteration 226 removed from the printed
  reports.

**Bounded improvement:** the Summary uses `ReportService.ScoreLabel`
("anti-E  (score 85.0%)").

**Data/schema:** none.

**Tests:**
- The `AcsEvaluationTests` source guard now also checks that the
  Analysis view model uses the score label and no bare `({prob *
  100:F1}%)`.
- `DashboardAccuracyTests`' mirror of the summary is updated.

**Docs:** the Summary help topic explains the score.

All 1408 tests pass (this run took 11 minutes from machine load).

## Iteration 228: the Pending Work report counts cancelled and unreviewed work correctly

**Reviewed, no gap:**
- **"Analysis complete".** This status refers to the analysis run, not the
  investigation.
- **Worklist.** It excludes cancelled investigations and lists unreviewed
  confirmations as Awaiting supervisor review when review is required.

**Deficiency found (reporting accuracy, workflow):**
- `PendingWorkText` treated every active specimen without a final call as
  pending. A cancelled investigation, which is closed without a call, was
  counted as Pending and listed under "Not yet analyzed" or "Analyzed —
  awaiting confirmation".
- The report printed "All active specimens have a confirmed antibody
  identification. No pending work." even when required supervisor reviews
  were outstanding, which the Worklist shows as work.

**Bounded improvement:**
- Cancelled investigations are excluded from Pending and counted on their
  own line ("Cancelled: N (closed without a call; not pending)").
- With supervisor review required, unreviewed confirmations get an
  "Awaiting review" count and a "CONFIRMED — AWAITING SUPERVISOR REVIEW"
  section (accession, final ID, confirmer).
- "No pending work" appears only when nothing is pending or awaiting
  review. When cancelled investigations remain it says "Every active
  specimen has a confirmed identification or a cancelled investigation".

**Data/schema:** none.

**Tests:**
- `PendingWorkReportTests` (4 cases):
  - a cancelled specimen is counted separately and not listed;
  - only-cancelled wording;
  - an outstanding required review is listed and blocks "No pending
    work", then clears after review;
  - with review not required, a confirmed call is finished.
- `LabUxFeatureTests.PendingWorkReport_SeparatesSections` now allows the
  confirmed specimen only in the review section.

**Docs:** the Reports help topic describes the counts and the review
section.

All 1412 tests pass.

## Iteration 229 (clean pass): status in the list reports

**Reviewed:**
- **All Specimens.** The report and its CSV share `GetSpecimenStatus`:
  Cancelled first, then Confirmed, Not analyzed, Stale and Analyzed, so a
  cancelled investigation is never shown as pending or analyzed.
  "Confirmed" stays accurate for a call still awaiting supervisor review.
  Review-pending work is listed where work is tracked: the Worklist and,
  since Iteration 228, Pending Work.
- **All Panels.** The report and its CSV use `PanelStatusShort` (Active,
  Inactive or Expired, Iteration 180).
- **No summary counts.** Neither list report prints counts that could
  include cancelled work.

No new issue or gap worth fixing. All 1412 tests pass.

## Iteration 230 (clean pass): investigation status in the Specimens detail pane

**Reviewed:**
- **Single source.** The detail pane's status line, checklist and next
  step come from `InvestigationStatusService.Compute`, which investigation
  search also uses.
- **Cancelled.** Checked before anything else. It shows "Cancelled", the
  date and reason, that the record is locked, and that it can be
  reinstated with a reason.
- **Review pending.** With review required, an unreviewed call is "Awaiting
  supervisor review". The next step names a supervisor other than the
  confirming technologist.
- **Later states.** Amended (with "reissue the report" when the last report
  predates the change), Completed, and the open states (Awaiting panel,
  Testing in progress, Analysis needed, Awaiting additional testing,
  Awaiting identification) follow in order.
- **Tests.** `InvestigationStatusTests` covers them.

No new issue or gap worth fixing. All 1412 tests pass.

## Iteration 231: the timeline dates receipt from the recorded received time

**Reviewed, no gap:**
- **Coverage.** Every specimen audit action that can appear on a live
  specimen has a timeline line, including cancellation, reinstatement,
  clear and reconfirm with reasons, supervisor review, reports, runs and
  eluate links.
- **Exceptions.** Only add_specimen (represented by the received line) and
  delete_specimen (no timeline remains) have none.
- **Order.** Events sort by date, then date-only events first, then time,
  then insertion order.

**Deficiency found (timeline accuracy, consistency):**
- The first event, "Specimen received", was always dated by
  `created_date`, the day the specimen was entered.
- When a received time was recorded, for example received in May but
  entered in November from a downtime log, the timeline showed receipt in
  November. That contradicted the collection/receipt line further down,
  Analytics and investigation search (Iterations 184 and 214).
- The "open period" used to show configuration changes during the
  investigation also started at entry.

**Bounded improvement:**
- "Specimen received" uses the recorded received time, with its time of
  day.
- If entry was on another day, a separate date-only "Specimen entered in
  HemoIQ" line is added.
- Without a received time the line keeps the entry date.
- The open period starts at `TurnaroundService.ReceivedTime`.

**Data/schema:** none.

**Tests:** `InvestigationTimelineTests` gains 2 cases:
- received in May and entered in November shows both lines, in order;
- received the same day it was entered shows one timed received line.

The existing received-only test is unchanged.

**Docs:** the Specimens help topic's timeline section describes both
lines.

All 1414 tests pass.

## Iteration 232 (clean pass): specimen date labels

**Reviewed:**
- **Entry date.** Every place that shows the entry date labels it
  "Created": the Specimens list column, the Specimen Summary report, the
  All Specimens report, and the CSV (`CreatedDate`).
- **Received date.** Every line labelled received uses the recorded
  received time, falling back to the entry date: the specimen detail pane
  ("Collected …; received …"), the Clinical Identification header, and
  since Iteration 231 the timeline.
- **No mislabeling.** No screen or report calls the entry date "received".

No new issue or gap worth fixing. All 1414 tests pass.

## Iteration 233: saving grades on an expired specimen asks first

**Reviewed, no gap:**
- **Dating.** Default specimen dating comes from Preferences.
  `SpecimenDating.Check` warns when a recent transfusion or pregnancy
  shortens the allowed dating.
- **Start-up.** Expired specimens are deactivated at start-up (audited as
  `auto_deactivate_expired`).
- **Inactive specimens.** These cannot take reactions.
- **Worklist.** It lists expiring and expired specimens.

**Deficiency found (patient safety, compliance):**
- `TrySaveGrades` checked expiry for the panel lot only. A specimen that
  expired while the application was open stayed active until the next
  start-up, for example across midnight on a long shift.
- Grades could then be saved on it with no warning. Unlike an expired lot,
  nothing asked the technologist to collect a new specimen.

**Bounded improvement:**
- `ReactionsViewModel.ExpiredSpecimenPrompt` builds the warning: "Specimen
  … expired …; results may not represent the patient's current antibodies
  …; collect a new specimen …; record anyway?".
- It is shown before the panel-lot check. No leaves the grades unsaved,
  with the save status "Not saved: the specimen has expired".
- A specimen that expires today is still in date.

**Data/schema:** none.

**Tests:** `ExpiredSpecimenEntryTests` (6 cases):
- the prompt names the specimen and its date;
- an in-date, same-day or undated specimen, or no specimen, gets no
  prompt;
- a source guard that the save checks the specimen before the panel.

**Docs:** the Reactions help topic describes the prompt.

**Test isolation (found while verifying):**
- `PatientContextStalenessTests` failed once in a full run and passed
  alone. The cause is in the tests, not the product: staleness compares
  each analysis snapshot with the global `AppSettings.Current`.
- Six staleness test classes were outside the `PersistentDatabase`
  collection, so a parallel test changing a clinical setting between
  analysis and check made the specimen read stale: `EluateSourceStaleness`,
  `LabUxFeature`, `PanelTypingStaleness`, `PatientContextStaleness`,
  `SpecimenInputStaleness` and `StalenessClockOrder`.
- They now join the collection and no longer run alongside tests that
  change settings.

All 1420 tests pass.

## Iteration 234: grades recorded after the specimen expired stay on the record

**Reviewed, no gap:**
- **Analysis.** Running analysis reads saved grades only, and creates no
  new results, so it needs no expiry prompt of its own.
- **Confirmation.** Confirmation happens on the Analysis tab, where the
  REVIEW note below is shown among the critical suggestions.

**Deficiency found (auditability, patient safety):**
- Iteration 233 made saving grades on an expired specimen ask first, but
  answering Yes left no lasting trace.
- An expired panel lot is recorded by `ExpiredReagentCheck`: a REVIEW note
  on the Analysis tab and reports, and an "Expired reagent" Worklist item.
- Grades saved after the specimen's own expiry left nothing for a reviewer
  or a later reader to see.

**Bounded improvement:**
- `ExpiredReagentCheck.CheckSpecimen` finds each graded run recorded after
  the specimen's expiration date. It uses the same recording-date rule as
  the lot check: the last `save_reaction` for the run, else the run's
  created date. The rule is now shared in `GradedRunsWithRecordedDate`.
- Analysis adds a critical "REVIEW: Reactions on … were recorded …, after
  the specimen expired …" note, printed on the reports.
- While the specimen is still active, the Worklist lists it as "Graded
  after specimen expiry", until a reviewed final call clears it. This is
  separate from the existing "Expired specimen" expiration item.

**Data/schema:** none.

**Tests:** `ExpiredReagentTests` gains 2 cases:
- grades recorded after the specimen expired give a finding (but no lot
  finding), an analysis REVIEW note and the Worklist item;
- an in-date specimen, one expiring today, and an undated one give none.

**Docs:** the Reactions help topic describes the note and the Worklist
item.

All 1422 tests pass.

## Iteration 235 (clean pass): specimen state on report headers

**Reviewed:**
- **Specimen Summary.** The header prints Expiration.
- **Clinical Identification.** The worksheet header prints Collected,
  Received and Expires, plus the investigation status from
  `InvestigationStatusService`: Cancelled, Inactive – not completed,
  Awaiting supervisor review, Amended and so on.
- **Clinical evidence.** Grades recorded after the specimen or the lot
  expired print as REVIEW notes (Iterations 211 and 234).
- **Conclusion.** A printed report from an expired, inactive or cancelled
  specimen states that on the page and cannot be mistaken for a current
  one. A report printed after the specimen expired is still a valid record
  of testing done while it was in date.

No new issue or gap worth fixing. All 1422 tests pass.

## Iteration 236 (clean pass): Help topics, index and manual

**Reviewed:**
- **Catalog.** All 37 topic files are in `HelpCatalog`.
- **Screen links.** The 34 distinct `Help.Topic` values set in XAML each
  resolve to an existing topic file. Every dialog except the Help window
  itself sets one. `HelpLocator` picks the deepest selected tab's topic,
  then the nearest local one, so each screen's Help opens its own chapter.
- **Tests.** `HelpSystemTests` checks that every topic loads, that the
  required topics cover every section, and that the generated user manual
  includes every chapter and the intended use and prints as a multi-page
  PDF. `CreateShortcut.ps1` regenerates `docs/USER_MANUAL.md` from the
  topics on each build, and recent iterations committed those
  regenerations.

No new issue or gap worth fixing. All 1422 tests pass.

## Iteration 237: confirmations that delete data default to No

**Reviewed, no gap:**
- **Enter and Escape.** All 18 dialogs have an Enter (IsDefault) and an
  Escape (IsCancel) button, except the Help window, which has only Escape.
- **Tab order.** No field is taken out of Tab order.
- **Existing No defaults.** Confirming an identification against open
  testing, and importing a configuration, already default to No.

**Deficiency found (patient safety, data integrity):**
- **Purge.** Purge was the Purge dialog's Enter button, and its "Permanently
  delete N specimen(s)…" confirmation was a Yes/No box with Yes under the
  keyboard. Pressing Enter twice deleted specimens. Without an archive the
  message itself says "This cannot be undone".
- **Other deletes.** These used the same Yes-default box: delete specimen
  (with all reactions and results), delete panel, delete rule, remove
  extra antigens, detach panel, and delete run or clear reactions.

**Bounded improvement:**
- `DestructiveConfirm.Ask` shows a Yes/No warning with No as the default
  answer. Every deleting confirmation above now uses it.
- The Purge button is no longer the dialog's Enter button.

**Data/schema:** none.

**Tests:** `DestructiveConfirmTests` (8 cases):
- the default answer is No;
- a source scan finds no Yes/No MessageBox whose message deletes, removes,
  detaches, purges or clears outside the helper (verified to flag all six
  old calls);
- each deleting action asks through the helper;
- the Purge button is not IsDefault.

**Docs:** the Purge help topic explains Enter and the No default for every
deleting confirmation.

All 1430 tests pass.

## Iteration 238: the Rule dialog puts the cursor in the refused field

**Reviewed, no gap:** these dialogs already focus the field named by
every refusal on Save:
- Preferences (13 checks);
- Panel (3);
- Patient (3);
- Specimen (1 Validation message; its other refusals come from the
  transactional save and name the field).

**Deficiency found (UX):** two of the Rule dialog's three refusals left
the cursor where it was:
- "Name and Antibody are required";
- "Unknown antigen(s) in Exempt from …".

The user had to find the box named in the message.

**Bounded improvement:**
- The required-fields refusal focuses Name when it is empty, otherwise
  Antibody.
- The unknown-antigen refusal focuses the Exempt from box.

**Data/schema:** none.

**Tests:** `ValidationFocusTests` (5 cases, one per dialog) is a source
guard: every "Validation" message box in a dialog's code is followed by a
`.Focus()` before its `return`. It was verified to flag the two old Rule
dialog refusals and to find refusals in every dialog.

All 1435 tests pass.

## Iteration 239 (clean pass): refusals and Cancel in the smaller dialogs

**Reviewed:**
- **Reason.** Focuses its box when it opens and again on "A reason is
  required".
- **Select panel and Select warehouse antigen.** Each has a single list,
  so "Please select…" can only mean that list.
- **Panel run.** Has nothing to refuse (Iteration 223 made its copy
  option safe).
- **Users.** Writes only through its audited Save user button. It shows
  the access-control service's message, which names the problem
  (duplicate initials, last administrator and so on).
- **Cancel.** Every one of these dialogs closes without writing on
  Cancel or Escape.

No new issue or gap worth fixing. All 1435 tests pass.

## Iteration 240: a read-only or unusable database is refused at start-up with a clear message

**Reviewed, no gap:**
- **Locking.** SQLite allows a second copy of HemoIQ on the same file.
  Microsoft.Data.Sqlite waits on a busy lock before failing.
- **Archives.** They are read through their own `Mode=ReadOnly` connection
  and attached for restore, so a read-only archive still opens.

**Deficiency found (reliability, data integrity):**
- **Read-only opens quietly.** SQLite opens a read-only database file, or
  one on a read-only share, in read-only mode without any error. An
  up-to-date database needs no write at start-up, so HemoIQ started
  normally.
- **Every save then failed.** Each save failed with SQLite's raw "attempt
  to write a readonly database" through the app-wide error handler, so the
  user could do a full workup before learning nothing could be recorded.
- **Invisible process.** If opening did throw (locked, damaged, cannot
  open), the app-wide handler swallowed it after `MainWindow` had failed
  to build. HemoIQ then stayed running invisibly, with no window, after
  the error box.
- **File left open.** A constructor failure left the connection open.

**Bounded improvement:**
- `DatabaseService` refuses a read-only database at open, using
  `sqlite3_db_readonly` and throwing SQLite's own read-only error (code 8).
- Any open or migration failure disposes the connection before rethrowing,
  so the file is released.
- `App.OnStartup` catches a main-window failure, logs it, and shows
  `StartupFailure.Describe`: the database path and a plain reason for
  read-only, in use, cannot open, damaged, or the underlying error. It
  says "Nothing was changed", then `Shutdown(1)`.

**Data/schema:** none.

**Tests:** `StartupFailureTests` (7 cases):
- each SQLite failure code is explained with the path, even when wrapped
  in another exception;
- other errors show the innermost message;
- a real read-only, up-to-date database file is refused at open with the
  read-only explanation, and the file is released (deleted afterwards).

**Docs:** the Getting started help topic gains "If HemoIQ cannot start".

All 1442 tests pass.

## Iteration 241 (clean pass): the database becoming unwritable mid-session

**Reviewed:**
- **Savepoints.** Writes run inside `RunInTransaction`. A failure rolls
  back to the savepoint, including the audit entries written in it, so a
  disk-full or dropped-share error cannot leave a half-saved record.
- **Reaction saves.** The batch save (`SaveReactions`) is one transaction.
  On failure `TrySaveGrades` shows "Save failed: …" in the save status and
  status line. It leaves the grid marked unsaved and keeps the recovery
  draft, so no entered grade is lost and the save can be retried.
- **Specimen saves.** `SpecimenSaveService` is one transaction, and the
  Specimens view shows the refusal.
- **Other errors.** Any other write that fails reaches the app-wide
  handler, which logs it and shows the message. HemoIQ keeps running and
  the rolled-back data is unchanged.

No new issue or gap worth fixing. All 1442 tests pass.

## Iteration 242 (clean pass): the reaction recovery draft

**Reviewed:**
- **Writing.** The draft is written on every grade edit (including
  Iteration 223's unsaved pre-fill), but not while a draft is being
  restored and not in a read-only session.
- **Offering back.** At start-up the draft is offered only when its run
  and specimen still exist and its grades differ from the saved ones
  (`ReactionDraftStore.ChangedRows`).
- **Confirmed since.** If the identification was confirmed after the
  draft was written, recovery is refused, and the prompt lists the grades
  and says to clear the identification first.
- **Restoring.** Yes puts the grades back as unsaved edits for review and
  Save. No discards them.
- **Run unavailable.** If the run cannot be opened, the recovery file is
  kept and its path is shown.
- **Deleting.** The draft is deleted on a successful save, on an explicit
  discard, and when the user agrees to discard unsaved grades while
  switching specimens or runs.
- **Tests.** `ReactionDraftStoreTests` covers the store.

No new issue or gap worth fixing. All 1442 tests pass (this run took 7
minutes from machine load).

## Iteration 243 (clean pass): Search at scale and with partial warehouse antigens

**Reviewed:**
- **Indexing.** Warehouse-antigen criteria filter with `EXISTS` on
  `panel_cell_extra_antigens`, whose primary key (cell_id, antigen_name)
  serves each probe. Standard-antigen criteria compare `panel_cells`
  columns directly.
- **Extra antigens.** Each search attaches the matched cells' extra
  antigens in a single query. The cell IDs are a literal integer list, so
  there is no parameter-count limit.
- **Lots without the antigen.** A lot that never typed the antigen has
  no row for it. A "−" criterion needs a stored "−", and a "+" criterion
  needs + or ++, so those cells are left out rather than treated as
  negative. The Lot column marks inactive and expired lots (Iteration
  213).
- **Performance.** `PerformanceTests.SearchCellsByProfile_CompletesUnder1Second`
  times 50 searches on the seeded database.

No new issue or gap worth fixing. All 1442 tests pass.

## Iteration 244 (clean pass): version identity and intended use

**Reviewed:**
- **Version.** `SoftwareIdentity.Version` reads the assembly's
  informational version (2.0.0 from the project file). The About box and
  every report preamble (`ReportPreamble`, prefixed to every report by
  `ReportService`) both use it. The installer's 2.0 is the same release,
  and its file name `HemoIQ-Setup-2.0.exe` is the delivery name the user
  asked for.
- **Intended use.** One constant, `SoftwareIdentity.IntendedUse`, feeds
  the first-run acknowledgement, the report preamble and the user manual.
  It states decision support for licensed professionals, that a
  qualified professional must confirm the identification, and that the
  software does not issue units.
- **Author.** The About box credits the author, as requested by the user (commit a299f55).

No new issue or gap worth fixing. All 1442 tests pass.

## Iteration 245: demo data cannot be mixed into a laboratory database

**Reviewed, no gap:**
- **Confirmation.** Load Demo Data asks before writing.
- **Auditing and permissions.** Every seeded write goes through the
  audited, permission-checked writers, so a read-only session cannot load
  it and the audit log records who did.

**Deficiency found (patient safety, data integrity):**
- **Any database.** Load Demo Data seeded whatever database was open,
  including a live laboratory database.
- **Look-alike records.** The clinical seed set uses realistic accession
  numbers (2026-001 to 2026-010) with analysed, interpretable results. Its
  five lots carry real vendor names and plausible lot numbers, and are
  created active, so they are offered for real testing. Once loaded,
  nothing distinguished them from laboratory records.
- **Partial seeding.** The seeders were not transactional. A laboratory
  specimen that happened to use a demo accession (for example 2026-005)
  made seeding fail part-way, leaving the earlier demo specimens and the
  active demo lots behind.

**Bounded improvement:**
- `DemoDataGuard.RefusalReason` allows demo data only into an empty
  database, or one whose every specimen is a demo accession (to fill in
  missing scenarios).
- Otherwise it refuses, naming the non-demo record or the lab lots, and
  explains how to use a separate database (`HEMOIQ_DB`).
- `DemoDataGuard.Load` runs all four seeders in one transaction, so a
  failure leaves nothing.
- The Tools menu command uses both.

**Data/schema:** none.

**Tests:** `DemoDataGuardTests` (5 cases):
- an empty database loads, and a demo-only one may be reloaded;
- a database with a laboratory specimen is refused with its accession,
  and nothing is added;
- a database with only lab lots is refused;
- a load that fails part-way on a duplicate accession leaves exactly the
  pre-existing record and no demo lots;
- a source guard that the command goes through the guard.

**Docs:** the Demo data help topic explains the rule and the separate
database.

All 1447 tests pass.

## Iteration 246: the headless seed switch follows the demo-data rule; risk file updated

**Reviewed, no gap:** `--import-vendor-samples` imports vendor lots through
`VendorPanelImportService`, which stores them inactive and never replaces a
lot with test history. It follows the rule that imported data is never
activated without review.

**Deficiency found (patient safety, traceability):**
- `--seed-clinical` still called the four seeders directly. It bypassed
  Iteration 245's `DemoDataGuard`, so run against the default database it
  could seed demo data into a laboratory database, non-transactionally.
- The FDA documentation named this switch as a controlled entry point:
  risk R-03 "Seed / demo data treated as patient work", requirement
  SRS-SEED-02, and the traceability matrix. The control and verification
  they described no longer matched the code.

**Bounded improvement:**
- `--seed-clinical` uses `DemoDataGuard`: refused, with the reason on
  standard error and in the log, and exit code 3, unless the database is
  empty or demo-only; otherwise loaded in one transaction.
- `docs/fda-510k/risk-management.md` (R-03 sequence, control and
  verification), `srs.md` (SRS-SEED-02) and `traceability.md` now
  describe the guard and `DemoDataGuardTests`.
- `docs/developer-conventions.md` notes the exit code.

**Data/schema:** none.

**Tests:** `DemoDataGuardTests` gains a source guard. The switch's branch
uses the guard and exits with 3. No code outside the guard and the
seeders calls `ClinicalDataSeeder.SeedIfNeeded`.

All 1448 tests pass.

## Iteration 247: the FDA risk register covers the recent safety controls, with cited tests guarded

**Reviewed, no gap:** R-01 to R-10 still describe controls that exist,
and the tests they cite exist.

**Deficiency found (regulatory documentation, traceability):**
`docs/fda-510k/risk-management.md` listed ten hazards. None covered the
hazards whose controls were built in the last iterations, so the risk file
understated what the software controls and how each control is verified:
- expired reagent or specimen use (Iterations 210, 211, 233 and 234);
- another patient's evidence in an identification (220);
- unreviewed antigrams in use (208–210);
- unobserved results recorded in a treated run (223);
- truncated lot files typed negative (204 and 205);
- accidental deletion (237);
- work recorded on an unwritable database (240);
- scores read as probabilities (225–227).

**Bounded improvement:**
- Risk rows R-11 to R-18 give each hazard's sequence, harm, control and
  verification test classes.
- `RiskRegisterTraceabilityTests` checks that every test class or method
  cited in the risk register and the traceability matrix exists. A
  renamed or removed test can no longer leave a control with no real
  verification.
- It also checks that the register cites the recent safety-control tests.

**Data/schema:** none.

**Tests:** `RiskRegisterTraceabilityTests` (3 cases): cited tests exist in
both documents, and the register covers the recent controls.

All 1451 tests pass.

## Iteration 248: the SRS and anomaly list match the software; every requirement is traced

**Reviewed, no gap:**
- **UA-01.** MathNet is still referenced but unused, as stated.
- **UA-03.** Confirmation by initials is still a residual of the
  510(k)-only scope, as stated.

**Deficiency found (regulatory documentation):**
- **UA-02.** "Duplicate `* 2.cs` files exist in the tree": none remain.
- **UA-04.** "No in-app user accounts or roles": HemoIQ has registered
  users with Technologist, Supervisor, Administrator and Read-only roles,
  every database writer checks a permission, and user management is
  audited.
- **SRS-CYB-01.** Said access control relies only on the Windows login and
  NTFS ACLs.
- **Missing requirements.** No requirement stated the safety behaviour
  behind risks R-11 to R-18.

**Bounded improvement:**
- UA-02 and UA-04 are marked resolved, with UA-04's residual stated: sign-in
  is the Windows logon.
- SRS-CYB-01 states the role permissions.
- SRS-SAFE-01 to SRS-SAFE-08 state the requirements for expiry, the
  same-patient rule, unreviewed lots, copied run grades, truncated files,
  deletion defaults, unwritable databases and score wording.
- `traceability.md` gains matching rows naming the code and tests.
- `RiskRegisterTraceabilityTests` now also checks that every SRS
  requirement has a traceability row (a row may cover several, such as
  "SRS-AUD-01 / 02").

**Data/schema:** none.

**Tests:** `RiskRegisterTraceabilityTests` gains one case (4 in all), and
the cited-test check now covers the new rows.

All 1452 tests pass.

## Iteration 249 (clean pass): V&V summary, version history and gap register

**Reviewed:**
- **Gap register.** `gap-register.md` has no OPEN items; all 16 entries
  are CLOSED or RESIDUAL.
- **Version history.** `version-history.md` describes release 2.0.0, which
  is still the product version (Iteration 244).
- **V&V summary.** `vv-summary.md` is labelled a starter document. It lists
  categories of automated verification rather than counts, so it states
  nothing that has gone stale. The newer safety-control tests are mapped
  in the risk register and traceability matrix (Iterations 247–248),
  guarded by `RiskRegisterTraceabilityTests`.

No new issue or gap worth fixing. All 1452 tests pass.

## Iteration 250: the log holds no record identifiers and does not drop lines

**Reviewed, no gap:**
- **Exports.** They are audited as `export_data`, with what was exported,
  and CSV output is safe against spreadsheet formulas.
- **Database file.** The ACL tests in `HipaaSecurityTests` still apply.
- **Reaction grades.** No code logs grades.

**Deficiency found (privacy, reliability):**
- **Identifiers in the log.** `AppLog` states "Do not write … PHI", but
  `AppLog.Error` wrote `ex.Message` for every exception the app-wide
  handler caught. HemoIQ's own messages name records: "Specimen X not
  found", "X is already cancelled", and since Iteration 220 "belong to
  different patients (MRN a and b)". Accessions and MRNs could therefore
  reach the plain-text log, which sits outside the database's access
  controls. Iteration 246's `--seed-clinical` refusal also logged an
  accession.
- **Dropped lines.** Writes used `File.AppendAllText` and swallowed every
  error. A second writer, or any program holding the file open with
  read-only sharing (a viewer, a backup agent), made lines vanish
  silently. A test reading the log while another wrote showed it.

**Bounded improvement:**
- `AppLog.Describe` keeps the message only for framework storage, file and
  network errors (SQLite, I/O, HTTP, timeout), whose text concerns the
  system.
- For HemoIQ's own exceptions, including wrapped ones, it logs the type,
  "message withheld because it may name a patient record", and the
  throwing type and method.
- The seed refusal is logged without the record.
- Writes are serialized and open the file with shared read, write and
  delete access, with a short retry.
- SRS-LOG-01, the traceability row and risk R-08 describe the control and
  cite `AppLogPrivacyTests`.

**Data/schema:** none.

**Tests:** `AppLogPrivacyTests` (7 cases):
- an application exception's accession and MRN are withheld, with its
  type and site kept, including when wrapped;
- SQLite and I/O messages are kept;
- the log file line holds no identifier;
- 40 concurrent writes all land;
- a reader holding the file open does not block writing;
- a source guard on the seed refusal.

`Fda510kControlTests` now reads the log with shared access.

All 1459 tests pass.

## Iteration 251 (clean pass): plain-text files outside the database

**Reviewed:**
- **Recovery draft.** The draft (`reaction-draft.json`) holds an accession
  and unsaved grades. It is stored in the Windows user's own profile
  (`%LOCALAPPDATA%\HemoIQ\drafts`), which Windows restricts to that
  user, SYSTEM and administrators. Each Windows user on a shared
  workstation has their own. It is deleted on save or discard (Iteration
  242), and its tests use a temporary directory.
- **Import artifacts.** These are vendor antigram files stored beside the
  database, with no patient data.
- **Exported preferences and configuration.** These are settings only.
- **Exports.** Report and CSV exports go to a location the user picks.
  They are audited as `export_data` with what was exported, and their
  default names say what they contain.
- **Log.** Since Iteration 250 the log withholds record identifiers.

No new issue or gap worth fixing. All 1459 tests pass.

## Iteration 252 (clean pass): the purge archive file

**Reviewed:**
- **Saving.** The Purge dialog's save box suggests a timestamped name
  (`hemoiq_archive_yyyyMMdd_HHmm.db`) in a folder the user chooses, and
  refuses an existing file (Iteration 160), so an earlier archive is
  never overwritten.
- **Warning.** The purge confirmation says "Archives contain specimen
  records and should be stored securely", and the Purge help says the
  same.
- **Reading.** Open Archive inspects the file through `OpenArchiveReadOnly`
  (`Mode=ReadOnly`). Restore attaches it and only reads from it. Every
  `INSERT INTO archive.*` is in the purge path that creates the archive,
  so inspecting or restoring never changes it.
- **Archive refused.** A purge that includes confirmed identifications
  needs an archive or is refused.

No new issue or gap worth fixing. All 1459 tests pass.

## Iteration 253: every Preferences value analysis reads is audited and recorded

**Reviewed, no gap:**
- **Dialog and import.** The Preferences dialog and Preferences import
  write `update_settings` with the before and after clinical snapshot,
  and import also writes `import_preferences`.
- **Staleness.** Open analyses go stale by the snapshot comparison and by
  the configuration-change sequence (Iteration 152).

**Deficiency found (auditability, traceability, patient safety):**
- **Missing settings.** Analysis reads `DefaultSpecimenDatingDays`,
  `DatingTransfusionLookbackDays` and `RhigPassiveWindowDays` for its
  critical specimen-dating and passive anti-D REVIEW notes. The RhIG
  window decides whether anti-D is flagged as possibly passive.
- **Consequences.** None of the three was in `ClinicalSnapshot`, so a
  change was not audited (the audit entry is written only when the
  snapshot differs). It did not mark open analyses stale, and an
  analysis did not record which value produced its notes.
- **Upgrade risk.** Simply adding them would have made every analysis
  saved before the change read stale, because the snapshots were compared
  as whole strings.

**Bounded improvement:**
- The three windows are added to `ClinicalSnapshot`.
- `LabSettings.ClinicalSettingsDiffer` now compares setting by setting,
  over the settings the saved analysis recorded. A setting added later
  does not mark older analyses stale, and later changes to it are caught
  through the audit sequence. Unparseable JSON falls back to the old
  whole-string comparison.

**Data/schema:** none (the snapshot JSON gains three keys).

**Tests:** `AnalysisSettingsSnapshotTests` (3 cases):
- the snapshot holds the three windows;
- an older snapshot without them is not stale, while a changed shared
  setting is, and missing or unparseable JSON behaves as before;
- changing the RhIG window through Preferences import after an analysis
  marks it stale and audits the old and new values.

**Docs:** the Preferences help topic lists every setting that marks
analyses stale and says each change is audited.

All 1462 tests pass.

## Iteration 254 (clean pass): rules and engine tables in the audit and the snapshot

**Reviewed:**
- **Rule audit.** Rule add, update and delete each write an audit entry
  (`add_rule`, `update_rule`, `delete_rule`) with before and after values.
  These actions are in the configuration-change sequence, so open analyses
  go stale (Iteration 152).
- **Snapshot.** Each analysis snapshot stores the software version, the
  rule engine version, the clinical settings JSON (Iteration 253) and
  `AnalysisRuleTrace.Serialize`. That holds every rule with all its fields
  (antibody, minimum rule-out count, heterozygous permission, exception
  antigen, separation exemption and partners), the ACS antigen list, and
  the rule-out, ID and separation counts.
- **Engine tables.** The fixed ACS core list and the treatment tables are
  code, so the software version identifies them.

No new issue or gap worth fixing. All 1462 tests pass.

**Release note (process, not code):** the treatment-table corrections in
Iterations 166–168 were made without a version change (still 2.0.0). Bump
the version at the next release, so snapshots taken before and after the
corrections can be told apart.

## Iteration 255: the analysis input fingerprint covers every input analysis reads

**Reviewed, no gap:** staleness does not depend on the fingerprint. It uses
the update timestamps and the configuration sequence (Iterations 151–152).
The fingerprint is a traceability record, printed on the Analysis and
Clinical reports as "Input fingerprint".

**Deficiency found (traceability):**
- `ComputeInputFingerprint` hashed only run treatments and the IS, 37°C,
  AHG and CC grades.
- It left out lab-configured extra-phase grades, the antigen typing of the
  panel cells (which every rule-out depends on), and the specimen fields
  analysis reads: DAT, transfusion and pregnancy records, phenotype and
  genotype, previous antibodies, eluate source and notes. It also left out
  the patient record's transplant and antigen-negative fields.
- Two reports with the same fingerprint could therefore come from
  different typing or grades. That defeats R-06 ("reconstruct why a
  result was produced") and SRS-SNAP-01.

**Bounded improvement:**
- The fingerprint now covers all of the above, with antigen case kept and
  every part in a fixed order.
- An "fp2" marker keeps the new fingerprints from coinciding with the old
  form.
- `PersistSnapshot` passes the panels' cells, the specimen and the patient
  record.
- Stored fingerprints keep the value recorded at the time.
- SRS-SNAP-01 states what the fingerprint covers, and the traceability row
  cites `InputFingerprintTests`.

**Data/schema:** none (same 64-character SHA-256 column).

**Tests:** `InputFingerprintTests` (4 cases):
- identical inputs give the same fingerprint;
- each of extra-phase grade, cell typing, antigen key, DAT, transfusion
  status, patient antigen-negative and a reaction grade changes it;
- the reactions-only form still works and differs from the full one;
- editing panel typing changes the next analysis's stored fingerprint.

All 1466 tests pass.

## Iteration 256: every analysis input marks open analyses stale when it changes

**Reviewed, no gap:** these already mark staleness:
- reactions, panel typing (Iteration 192) and run add or delete;
- specimen type, notes, phenotype, genotype, previous antibodies, DAT,
  transfusion status and products, and pregnancy (193);
- the patient's transplant and antigen-negative fields;
- MRN changes and same-patient final calls (195);
- eluate links (194).

**Deficiency found (data integrity, patient safety):** checked against the
inputs listed in Iteration 255, four writes changed what analysis computes
without marking anything stale:
- **Hemolysis.** `UpdateSpecimenHemolysis`, although analysis prints
  hemolysis REVIEW notes. Hemolysis was also missing from the fingerprint.
- **Collection time.** `UpdateSpecimenCollection`, although collection is
  the reference date for specimen dating and the transfusion-history
  window.
- **Free-text requirements.** `SavePatient` marked transplant and
  antigen-negative changes, but not the free-text transfusion
  requirements that feed the antigen-negative review note.
- **Other specimens.** Transfusion record, collection time and phenotype
  changes marked only their own specimen, although the patient's other
  specimens read them (transfusion history, historical phenotype).

**Bounded improvement:**
- Hemolysis changes mark the specimen stale.
- Collection changes mark the specimen and the patient's other specimens.
- Transfusion status and products, and phenotype changes, also mark the
  patient's other specimens.
- Free-text requirement changes mark the MRN's specimens.
- Hemolysis is added to the input fingerprint.
- Every writer still returns early when nothing changed, so an unchanged
  save marks nothing.

**Data/schema:** none.

**Tests:** `AnalysisInputStalenessTests` (6 cases):
- hemolysis;
- collection on one specimen marks it and the patient's other open
  analysis stale, but not another patient's;
- transfusion and phenotype on another specimen;
- free-text requirements;
- hemolysis in the fingerprint.

**Docs:** the Analysis help topic's stale-warning list adds hemolysis,
collection time, transfusion requirements and the same-patient changes.

All 1472 tests pass.

## Iteration 257: label-only panel cell edits no longer mark analyses stale

**Reviewed, no gap:** the specimen writers that mark staleness all return
early when nothing changed, and they mark only on fields analysis reads:
- type, notes, phenotype, genotype, previous antibodies and DAT;
- transfusion, products, hemolysis and pregnancy;
- collection time, but not receipt time.

Reaction saves that change nothing write nothing.

**Deficiency found (UX, alert fatigue):**
- `SavePanelCellEdits` marked every open investigation on the lot stale
  for any cell change, including the donor ID and the Rh phenotype label.
- Analysis reads neither label: the Rh phenotype is used only by the
  import review, while special types do feed rare-antigen reading and
  selected-cell recommendations.
- Correcting a donor number made every open workup on that lot show a
  "stale" warning that rerunning analysis could not resolve differently.
  Needless warnings teach users to ignore real ones.

**Bounded improvement:**
- `DatabaseService.ChangesAnalysisInput` treats donor and Rh phenotype
  label changes as non-inputs.
- The save marks open investigations stale only when at least one change
  is to antigen typing or special types.
- The Panels status line count uses the same rule.
- The edit is still saved, audited (`edit_panel_typing`) and needs a
  reason on a lot with test history.

**Data/schema:** none.

**Tests:** `PanelMetadataStalenessTests` (8 cases):
- donor-only and Rh-label-only edits are saved and audited but leave the
  analysis current;
- special-types and antigen edits still mark it stale;
- the change classifier separates labels from typing for each kind.

**Docs:** the Panels help topic explains which cell edits mark analyses
stale.

All 1480 tests pass.

## Iteration 258 (clean pass): every Worklist item can be cleared

**Reviewed:**
- **Recomputed items.** Incomplete reactions, stale analysis, expiring
  and expired items, untyped cells, rule problems, data inconsistencies
  and inconsistent status clear when the data they describe is fixed.
  They are recomputed on every refresh (Iteration 212).
- **Expired reagent and graded-after-expiry items.** These stay until a
  confirmed call is supervisor-reviewed, as the Worklist help states. The
  REVIEW note asks for documented review before relying on such results.
  The Supervisor Review action is available on any confirmed, unreviewed
  specimen whether or not Preferences require review, so the item can
  always be cleared by a documented review.
- **Duplicate lots.** These are listed only while at least one copy is
  active, so deactivating the extra copies clears them even when every
  copy holds test history and cannot be deleted.
- **Overdue testing.** This clears when the investigation is confirmed,
  cancelled or deactivated, or once a re-analysis no longer asks for
  additional testing.

No new issue or gap worth fixing. All 1480 tests pass.

## Iteration 259: safety-warning overrides default to No

**Reviewed:** prompts that repeat after acknowledgement. The
intended-use acknowledgement shows once and is saved; draft recovery
asks once and deletes the draft on either answer (it keeps the file only
when the run cannot be opened, so the grades are not lost); the ACS
exception offer is asked once per explicit Run Analysis. None repeats.

**Found:** the prompts that let the user go ahead past a safety warning
did not agree on their keyboard default. The stale-analysis and
open-testing prompts at Confirm ID default to No, but these defaulted
to Yes, so pressing Enter recorded the override:
- duplicate lot on save, and activating an expired lot (Panels);
- grading an expired specimen or an expired panel lot (Reactions);
- an encounter recorded on another patient, and a pregnancy recorded
  for a male patient (Specimens);
- confirming multiple antibodies short of the separation criterion
  (Analysis);
- replacing a stored lot from a vendor download.

**Implemented:**
- `OverrideConfirm.Ask` shows a Yes/No warning with No as the default,
  like `DestructiveConfirm`. All eight prompts above use it.
- The database-nearly-full purge offer at start-up also defaults to No.

**Tests:** `OverrideConfirmTests` (11 cases): the default is No; a
source scan refuses any Yes/No warning without a default answer and any
literal "anyway?" prompt that bypasses the helper; each prompt above
asks through it.

**Docs:** risk R-19, SRS-SAFE-09 and its traceability row; developer
conventions.

## Iteration 260: next steps that follow the user's role

**Reviewed:** messages that name a next step. View-only sessions have
every change command disabled, so they are not sent anywhere. But a
Technologist was told to do things only a Supervisor or Administrator
may do, and following the message ended in a refusal:
- "click Clear and give a reason" when confirming over a confirmed
  identification (Analysis and the database refusal);
- "Clear the confirmation" or "Reinstate it" in every locked-record
  refusal;
- "Clear the identification first" in the draft-recovery message;
- "Reinstate … or clear it" on the Inconsistent status Worklist item.

**Also found:** running analysis on a cancelled investigation said it
"has a confirmed identification". The analyzer now uses the same lock
check as every other write, which names the actual lock.

**Implemented:**
- `AccessControl.WhoCan(permission)` gives "a Supervisor or
  Administrator" or "an Administrator".
- `AccessControl.NextStep(db, permission, step, askFor)` returns the
  step when the signed-in user may take it, otherwise "Ask a Supervisor
  or Administrator to …". With no users registered the step is
  unchanged.
- `RecordLockedException` takes who to ask. `DatabaseService.LockedError`
  fills it from the permission that lifts the lock (reinstate for a
  cancellation, clear for a confirmation).

**Tests:** `RoleAwareNextStepTests` (7 cases): role names and articles,
the unchanged step with no registry, a Technologist and a Supervisor
confirming over a confirmed call and editing a confirmed record, and
analysis of a cancelled investigation naming the cancellation and who
can reinstate it.

**Docs:** the Preferences help topic (users and roles).

## Iteration 261: printed reports are never an older preview

**Reviewed:** text that goes stale after a later action. The status
line is replaced on every screen change and by every action. The
Worklist is rebuilt when its tab opens and after every write. The
Analysis tab re-reads the stale state when it opens, and Confirm ID
re-reads the specimen from the database.

**Found:** the Reports preview is built only when the report type,
specimen or panel selection changes. Running analysis, saving grades or
changing rules on another tab did not rebuild it, and opening the
Reports tab did not either. **Copy** and **Print** sent that cached
text, while Export PDF and CSV rebuild from the database. So a report
built from the earlier analysis could be printed and recorded in the
audit log (with its fingerprint) as the issued report.

**Implemented:**
- `ReportsViewModel.OnTabSelected` rebuilds the lists and the preview
  when the Reports tab opens, keeping the selection.
- `ReportsViewModel.CurrentShareText` rebuilds the report at the moment
  of Copy or Print. The printed text, the preview and the audited
  fingerprint are the same current report.

**Tests:** `ReportPreviewFreshnessTests` (3 cases): a confirmation on
another tab leaves the preview old until the tab opens, then shows it
with the selection kept; the Copy/Print text is rebuilt and matches the
preview; a deleted specimen leaves the list when the tab opens.

**Docs:** the Reports help topic.

## Iteration 262: Search and Analytics recompute when opened

**Reviewed:** screens that keep data computed earlier. The Panels grid
is edited only on its own tab, and Reactions refuses a save on a record
locked elsewhere with the reason. Two tabs kept older data:
- **Search.** Antigen-search results show each lot's status so selected
  cells are taken from a lot in use (Iteration 213). A lot deactivated
  or expired on the Panels tab still showed as in use until the search
  was run again. Investigation results kept the status and final ID
  they had when searched.
- **Analytics.** It was recomputed only after Confirm ID or Clear on
  the Analysis tab. Cancelling, reinstating, reviewing, editing or
  deleting an investigation left its counts and turnaround old until
  Refresh.

**Implemented:**
- `SearchViewModel.OnTabSelected` re-runs the antigen and investigation
  searches behind the results on screen, with the criteria, zygosity,
  text and dates they were run with, and keeps the selected row when it
  still matches. A refused or cleared search is not re-run. The status
  line is left to the screen announcement.
- `AnalyticsViewModel.OnTabSelected` recomputes the tab.
- `SearchViewModel` can be built without the main window, for tests.

**Tests:** `TabFreshnessTests` (6 cases): a lot deactivated elsewhere
shows as not in use with the selection kept; re-running uses the
original criteria, not ones edited since; a cleared search and a
refused investigation search are not re-run; investigation results show
a confirmation made elsewhere; Analytics counts it.

**Docs:** the Search and Analytics help topics.

## Iteration 263: one vendor download at a time, none after closing

**Reviewed:** actions that can be started twice. Every command handler
runs on the UI thread, and its prompts are modal, so a second click
waits until the first finishes and then sees the saved state. The only
work that yields while running is the vendor dialog's catalog listing
and lot download.

**Found:** during a download only **Import selected** was disabled.
- **Import vendor file…** could start a second import, and the vendor
  could be changed.
- Closing the dialog did not cancel the download. When it finished, the
  lot was imported (inactive) into a dialog the user had closed, and the
  attempt to close an already closed window failed with a message.

**Implemented:**
- `VendorDialogGate` allows one download or import at a time. Its
  cancellation token is passed to the catalog listing and the download,
  and it is cancelled when the dialog closes.
- After either finishes, the dialog stores and shows the result only
  while the gate is still open (`MayApply`). A download cut off by
  closing is discarded silently: nothing was stored.
- While busy, the vendor list and all three buttons are disabled.

**Tests:** `VendorDialogGateTests` (4 cases): a second start is refused
while the first runs; closing cancels the token and forbids storing; a
download cancelled by closing is not applied; the dialog passes the
token and checks the gate before storing.

**Docs:** the Download from vendor help topic.

## Iteration 264: Preferences survive a failed save

**Reviewed:** files the application writes when the target is
read-only, locked or on a full disk.
- Report exports, archives and the audit-log CSV report the error and
  write their audit record only after the file is written.
- The recovery draft is written beside its target and then moved.
- Lab configuration export now reports a failed write instead of
  falling through to the generic error box.

**Found:** `SettingsService.Save` wrote settings.json in place, and its
callers change the settings in effect first.
- **Read-only or locked file:** the write failed, but the session kept
  running on the changed clinical settings. They were not audited (the
  audit is written after the save) and reverted at the next start.
- **Preferences import:** the audit entries were written before the
  save, so they recorded a change the file did not hold.
- **Full disk:** a write cut short left a truncated file. `Load` treats
  an unreadable file as missing and replaces every Preference with its
  default at the next start, silently.

**Implemented:**
- `SettingsService.Persist` writes a temporary file and moves it over
  the target, so the old file stays whole until the new one is
  complete. A failure raises `SettingsSaveException`, which names the
  path and says nothing was changed.
- `SettingsService.Save` restores the settings in effect to the ones
  last loaded or saved when the write fails.
- Preferences import writes its audit entries and the file in one
  database transaction.
- Lab configuration import writes the file inside its transaction
  (`ConfigurationBundle.Apply(..., persist)`), so its rules,
  Preferences and audit entries roll back together.
- The Preferences dialog reports a failed save and stays open with the
  edits.

**Tests:** `SettingsSaveFailureTests` (5 cases):
- a failed save restores the settings, leaves the file unchanged, leaves
  no temporary file and says so;
- a save writes the whole file and reloads;
- `Persist` leaves the settings in effect alone;
- a configuration import whose Preferences cannot be saved changes no
  rules, writes no audit entries and leaves the settings alone;
- a successful import persists the new Preferences.

**Docs:** the Preferences help topic.

## Iteration 265: an unreadable Preferences file is reported and kept

**Found:** `SettingsService.Load` caught every error and switched to
the default Preferences without a word. A damaged, truncated or locked
settings.json meant the lab tested on the default ID rule, rule-out
counts, separation cells and dating windows without knowing. The next
save of any Preference (even a Worklist filter) then overwrote the
unreadable file, so the lab's settings were lost with no trace.

**Implemented:**
- When the file exists but cannot be read (I/O error, access denied,
  JSON that does not parse or has the wrong types, or `null`), `Load`
  copies it to `settings.unreadable-<date-time>.json` beside it before
  anything else can write. It then records `LastLoadProblem` and starts
  on defaults. A missing file is still a quiet first run.
- At start-up the main window shows the problem first ("Preferences
  not loaded"). The message names the file and the kept copy, and asks
  for Preferences to be reviewed or imported before testing. If no copy
  could be kept, it says not to save Preferences until the file has
  been copied elsewhere.
- The application log records that defaults are in effect, without the
  path.

**Tests:** `UnreadableSettingsTests` (7 cases): a file cut short, one
with a wrong-type value and one that is not JSON fall back to defaults,
are reported and are kept, and the copy survives a later save; `null`
is reported; a missing file is not a problem; a good file loads
cleanly; the message when no copy could be kept.

**Docs:** the Preferences help topic.

## Iteration 266: analysis settings replaced at load are reported

**Reviewed:** values brought into range. Preferences and configuration
imports refuse out-of-range values with the reason, and never replace
them.

**Found:** `SettingsService.Load` ran `LabSettings.Clamp` silently. An
analysis setting outside its range in settings.json (a hand-edited
file, or one from another version) was replaced without a word, and
some are replaced by their default rather than the nearest limit. For
example, a separation count of 9 became 1 and an ID cell rule of 4
became 3. The next save then wrote the replacement, losing the stored
value.

**Implemented:**
- `LabSettings.DescribeClinicalChanges(before, after)` lists the
  analysis settings that differ between two snapshots, as "Name:
  before → after".
- `Load` compares the analysis settings as stored with the values after
  `Clamp`. If any changed, it keeps the file as
  `settings.adjusted-<date-time>.json` and reports each replaced
  setting through `LastLoadProblem`.
- The start-up message (now titled "Review Preferences") lists them and
  asks for review before testing.
- Display settings, such as a blank lab name, are still normalised
  without a notice.

**Tests:** `UnreadableSettingsTests` (+3): an out-of-range separation
count is replaced, reported alone and the file kept; an in-range file
is not reported; `DescribeClinicalChanges` lists only the changed
setting.

**Docs:** the Preferences help topic.

## Iteration 267: the audit-log export is the whole log

**Found:** the Audit Log window loads the newest 5,000 events. With a
longer log:
- its count still read "5000 event(s)", as if that were everything;
- **Export CSV** wrote only those loaded rows, to a file named
  audit-log.csv, so an inspection copy could silently omit every older
  event;
- the export gave no confirmation of what it wrote, and a failed write
  fell through to the generic error box.

**Implemented:**
- `DatabaseService.CountAuditEvents` and `ReadAllAuditEvents` (newest
  first, read one row at a time, so a long log is never held in memory
  whole).
- `AuditLogExport.Write` writes the header and every event in the whole
  log that matches the Find text, through the formula-neutralising CSV
  field, and returns the count. The dialog streams it to the file,
  records the export as before, and reports "Exported N event(s) … (the
  whole audit log)" or "… matching …". A failed write says the log was
  not exported, and nothing is recorded.
- `AuditLogExport.CountLabel` reads "Newest 5000 of 12000" when only
  part of the log is listed. `TruncationNote` adds a line under Find
  saying what is listed and that the export covers the whole log.

**Tests:** `AuditLogExportTests` (8 cases): the export writes every
event, newest first, beyond what a viewer loads; Find filters across the
whole log; formulas are neutralised; the count labels; the note appears
only when the log is longer than what was loaded.

**Docs:** the Audit Log help topic.

## Iteration 268: the printed Explain Analysis names every candidate

**Reviewed:** exports and reports that read a limited list. The All
Specimens and All Panels reports, the Analytics CSV (its date range is
stated) and the Timeline read their whole lists. The Audit Log viewer
was the only database read with a cap (Iteration 267). On-screen
prompts that list the first 10 or 12 changes say "… and N more".

**Found:** the Clinical Identification report printed only the first 8
Explain Analysis narratives (`Take(8)`), with nothing to say more
existed. Candidates include every antibody evaluated for rule-out, so
a routine panel often has more than 8. Ruled-out candidates sort last,
so the printed record silently dropped them while the Analysis tab
listed them all.

**Implemented:** `ReportService.ExplainLines` prints the first
`ExplainNarrativesPrinted` (8) narratives, then one line naming every
further candidate with its status, pointing to Explain Analysis on the
Analysis tab for the full descriptions.

**Tests:** `ExplainReportCompletenessTests` (4 cases): up to the limit
there is no remainder line; beyond it each further candidate is named
with its status; no candidates gives no lines; a real analysis with
more than 8 candidates prints a report naming every one.

**Docs:** the Reports help topic.

## Iteration 269: the Analysis Results report carries the warnings

**Reviewed:** printed reports against the Analysis tab.
- The Clinical Identification report prints suspected antibodies,
  rule-outs, absorption conclusions, score exclusions, separation,
  Explain Analysis and every suggestion.
- Pattern Matches, Combinations and Dosage are extra views of the same
  run (analysis help). Their dosage, gated rule-out and treatment REVIEW
  notes reach the reports through Suggestions.

**Found:** the Analysis Results report printed suspected antibodies,
rule-outs and pattern evidence, but none of the Suggestions. Its
printout therefore left out every warning and REVIEW note the screen
shows first, for example:
- an antibody both ruled out and suspected;
- patient typing or genotype arguing against a suspected antibody;
- hemolysis, expired reagent and specimen dating;
- transfusion requirements;
- a separation shortfall.

A printed analysis could read cleaner than the screen it came from.

**Implemented:** the Analysis Results report adds a "SUGGESTIONS
(warnings and review items first)" section, with the same list in the
same order as the Analysis tab.

**Tests:** `AnalysisResultsReportTests` (2 cases): every suggestion
the analysis gives is printed, in the analysis order; a grossly
hemolyzed specimen's hemolysis note is printed.

**Docs:** the Reports help topic.

## Iteration 270: the Specimen Summary says which investigation and where it stands

**Found:** against the specimen record:
- **Specimen Summary report:** it printed MRN, type, dates, typing,
  transfusion, DAT and notes, but not:
  - the investigation status, or a cancellation and its reason;
  - the confirmed identification, though the Reports help said it did,
    with who confirmed it, the supervisor review and the comment;
  - collection and receipt, and encounter or ordering details;
  - pregnancy and visual hemolysis.
- **Specimen Summary CSV:** it began directly with the antibody rows,
  with no accession, MRN or status, so a saved file could not be tied
  back to an investigation.
- **Eluates:** neither the Specimen Summary nor the Clinical
  Identification worksheet named the red-cell specimen an eluate was
  made from.

**Implemented:**
- The Specimen Summary adds the status line used on the worksheet,
  collection, receipt, origin, final ID (or "Not confirmed") with who
  confirmed it, review (or PENDING when required) and comment, the
  cancellation with its reason, pregnancy and hemolysis.
- `ReportService.EluateSourceLine` gives the source accession, its MRN
  when it differs from the eluate's, or "not found in this database".
  Both reports print it as **Eluate of**.
- The Specimen Summary CSV starts with a **Specimen** section of
  Field/Value rows: Accession, MRN, Type, EluateOf, Status, Collected,
  Received, FinalId, IdentifiedBy, IdentifiedAt, ReviewedBy, ReviewedAt,
  CancelledAt and CancelReason. The existing sections follow unchanged.

**Tests:** `SpecimenSummaryCompletenessTests` (4 cases):
- a confirmed specimen prints its status, final ID, comment, hemolysis,
  pregnancy and receipt;
- a cancelled one prints "Not confirmed" and the cancellation reason;
- an eluate names its source on both reports, and its source does not;
- the CSV starts with the specimen it describes.

**Docs:** the Reports help topic.

## Iteration 271: printed lots carry treatment and cell details

**Reviewed:** the panel reports against the panel record. The status
line already gives Active, Imported-inactive with import date and
format, Inactive or Expired. Extra antigens are printed in the grid
when any cell types them. Activation history is in the panel's audit
history.

**Found:** no report printed the lot details from the vendor sheet that
the antigen grid has no column for:
- the enzyme-treated flag, so the antigram of a ficin or papain lot read
  as untreated cells;
- the product line, catalog number and lot notes;
- each cell's donor ID, Rh phenotype and special types (for example
  Yt(a-) or Bg). Analysis reads the special types.

This held for the Panel Summary, the Panel Antigram and each run on the
Clinical Identification worksheet.

**Implemented:**
- `ReportService.PanelProductLines` gives ENZYME-TREATED for a treated
  lot, the product and catalog, and the lot notes.
- `ReportService.CellNoteLines` gives a "Cell details" list, one line
  per cell that has a donor, Rh phenotype or special types.
- Both are printed under the Panel Summary and Panel Antigram headers
  and grids, and under each run's grid on the worksheet. A lot with
  none of these prints nothing extra.

**Tests:** `PanelReportCompletenessTests` (4 cases): the Panel Summary
and the Panel Antigram each print the treatment, product, catalog,
notes and cell details, and only for the cell that has them; the
worksheet prints them under the run; a plain lot adds nothing.

**Docs:** the Reports help topic.

## Iteration 272: the Pending Work report carries the whole Worklist

**Found:** the Pending Work report, described in help as "specimens
and panels that still need attention", listed only analysis state: not
analyzed, stale, awaiting confirmation and awaiting review. It omitted
every other Worklist item, and no panel item appeared at all:
- overdue additional testing and expired reagent use;
- incomplete reactions and data inconsistencies;
- expiring or expired specimens and lots, and untyped cells;
- duplicate lots and rule conflicts.

It could print "No pending work" while the Worklist still held items
that need review, so a printed hand-off could hide them.

**Implemented:**
- `ReportService.OtherWorklistItems` gives the Worklist items not
  already listed (everything except stale analysis and awaiting
  review), in Worklist order.
- The report adds an "OTHER WORKLIST ITEMS (n)" section with each
  item's kind, title, urgency and detail, built from the same
  `GetWorklistItems` call and expiration window as the Worklist.
- "No pending work" is printed only when that list is empty too;
  otherwise the report says how many Worklist items still need
  attention.

**Tests:** `PendingWorkReportTests` (+3): a lot's "Untyped cells"
item is listed and "No pending work" is withheld; the items already covered
above are not repeated; with an empty Worklist the report still says
"No pending work".

**Docs:** the Reports help topic.

## Iteration 273 (clean pass): expired lots and the All Specimens report

**Reviewed:**
- **The expired lot in the Iteration 272 test.** `AddPanel` stores a
  lot whose expiration date is already past as inactive (unless told
  otherwise), as start-up does for lots that expire while stored. The
  Worklist raises Expiring and Expired items only for lots in use, so
  an inactive expired lot correctly raises none. Searching marks it as
  not in use (Iteration 213).
- **All Specimens report and CSV against the Specimens tab.** The report
  has the tab's accession, type, final ID, expiration and created
  columns, plus the investigation status (Cancelled, Confirmed, Not
  analyzed, Stale or Analyzed), which the tab does not show. The tab's
  Active check box is the only column not printed. Pending Work counts
  active specimens only, and status already says whether work remains.

No new issue or gap worth fixing. All tests pass.

## Iteration 274: export file names from panel data are valid and name the lot

**Reviewed:** default file names in Save dialogs. None includes a
patient's name or MRN. Fixed names (audit-log.csv, the archive, rules,
Preferences and configuration exports with the date) are safe. The
report exports suggest no name. Vendor source files are stored under
sanitized vendor and lot names.

**Found:** Panels **Export** and **Antigram** suggested the panel name
as typed:
- a name with / : ? " < > | or * gave Windows an invalid default name;
- the lot was left out, so two lots of one panel were offered the same
  file, and the second export could replace the first;
- a failed CSV write fell through to the generic error box.

**Implemented:**
- `ExportFileName.ForPanel(panel, suffix, extension)` gives "<name>
  lot <lot> <suffix>.<ext>". Characters Windows refuses (and control
  characters) are replaced with _, trailing dots and spaces are
  trimmed, and the length is capped. Both Panels exports use it.
- The panel CSV export reports a failed write and saves nothing.

**Tests:** `ExportFileNameTests` (6 cases): the lot is included and
distinguishes two lots; refused characters are replaced; the result is
a valid file name; no lot and very long names are handled.

**Docs:** the Panels help topic.

## Iteration 275 (clean pass): keyboard access

**Reviewed:**
- **Escape.** Every dialog has a Cancel or Close button, so Escape
  closes it without changing anything.
- **Enter.** Every dialog has a default button except two, both by
  design: Help, which has nothing to accept, and Purge, which must never
  run on Enter (Iteration 237).
- **First focus.** The dialogs whose first focus is not set
  (`ArtifactViewerDialog`, `AuditLogDialog`, `ImportReviewDialog`,
  `PanelCompareDialog`, `PanelRunDialog`, `RestoreArchiveDialog`,
  `SelectPanelDialog`, `UsersDialog`, `PurgeDatabaseDialog`,
  `HelpWindow`) have read-only, filter or optional text boxes. Enter,
  Escape and Tab work there without one.
- **Main window.** Ctrl+S, Ctrl+N and Ctrl+R work on the main tabs,
  Ctrl+P prints on Reports, F1 opens context help, and every menu
  command has an access key.

No new issue or gap worth fixing. All tests pass.

## Iteration 276 (clean pass): numbers and dates under other locales

**Reviewed:**
- **Stored dates read back.** The dialogs read stored `yyyy-MM-dd` dates
  into date pickers with `DateTime.TryParse` under the current culture,
  and the Worklist reads expiration days the same way.
  `AppCulture.Apply()` runs first at start-up and forces the Gregorian
  calendar and ":" time separator while keeping the user's language and
  number format, so a Thai Buddhist or Um al-Qura workstation reads and
  writes Gregorian years (`AppCultureTests`).
- **Dates typed in pickers.** Date pickers show and accept dates in the
  user's own order, for example dd/MM/yyyy, and are stored as
  `yyyy-MM-dd`.
- **Typed times.** Collection and receipt times are read with the fixed
  formats H:mm and HH:mm.
- **Typed decimals.** These go through `LabSettings.TryParseDecimal`,
  which accepts "0.70" or "0,70" and refuses thousands separators.
- **Exports.** These write numbers with the invariant culture
  (`CommaDecimalCultureTests`).

No new issue or gap worth fixing. All tests pass.

## Likely next tick

Continue clean-pass reviews: help topics against the screens they
describe (each button or column a help topic names still exists on that
screen, and each screen's buttons are described).

Loop clean passes: 2/5
