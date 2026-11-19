# Antibody identification capability matrix

Decision-support software for trained blood-bank professionals. The engine
computes evidence; a qualified user must confirm identification. Scores and
rule-outs are analytical evidence, not a diagnosis.

Status: **Present** (usable), **Partial** (exists but incomplete or silent),
**Missing**.

Last updated: 2026-11-19 (iteration 129 — special panels stay empty on an untreated-only workup).

## Capability matrix

| Area | Capability | Status | Current evidence | Next action |
|---|---|---|---|---|
| Gap file | Living capability matrix | Present | This file | Update every iteration |
| Import | Provider adapter framework | Partial | `IVendorPanelSource` + Bio-Rad/Ortho/Quotient public catalogs; Immucor/Grifols/Medion file-only | Authenticated portals only with vendor permission |
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
| Analysis | Configurable visible rule-out | Partial | `Rule.MinRuleoutCount` is now applied; lab default + explanation sentences. Heterozygous permission applies only to the antibody named on the rule, so an anti-D rule with exception C does not rule out anti-C on heterozygous cells. An analytics range with no confirmed ID shows the empty title without a negative count scale. Saving a new default rule-out count marks an unconfirmed analysis stale and leaves a confirmed ID unchanged. Audit Find keeps one accession such as E2E-WARM-AUTO. An archive row for the warm-auto specimen shows DAT Positive, and a confirmed anti-K row shows that final identification. The printed antigram omits an all-NT column and keeps typed E, K, Fya, and a typed minus. The warm-auto autocontrol prints NT for E, K, and Fya, and a typed minus stays. A restored specimen keeps the saved analysis snapshot, including its settings. A restored lot keeps a saved antigen order, and a lot still in the database keeps its current order. The reactions grid for the warm-auto lot shows E, K, and Fya and hides an all-NT column. A typed minus stays. Specimens leads with the untreated-run exclusion, and the ruled-out grid follows that analysis, so anti-E, anti-K, and anti-Fya are not listed when the partner antigen is NT. Analysis Explain for the warm-auto lot says D is NT on every cell, and does not call that column never imported. The Analysis Summary for that workup keeps the warm-auto pattern and does not say no antibodies are suspected. Analysis Results and Specimen Summary follow that same analysis, so the warm-auto report does not list anti-E, anti-K, or anti-Fya as ruled out, and Analysis Results includes the pattern. Analysis Results for E2E-WARM-AUTO-ALLOS names anti-E and anti-K and says one cell lost reactivity after autoadsorption while five remained reactive. Clinical Identification cites those supporting cells on the Autoadsorbed run. Specimens lists that residual comparison immediately after the untreated-run exclusion, ahead of the warm and pan sentences, with anti-E and anti-K suspected and nothing ruled out. Compare on the Autoadsorbed run of E2E-WARM-AUTO-ALLOS checks the cells that differ from Untreated, including cell 4 at AHG 0 versus 3+, and does not open a binding error. Those grade columns are named Autoadsorbed and Untreated, so cell 4 reads Autoadsorbed AHG 0 beside Untreated AHG 3+. The E2E warm auto plus allos panel hides every all-NT antigen column and keeps typed c, E, and K, including a typed minus. The panel list shows the full lot E2E-WARM-AUTO-ALLOS beside that name. An E+ search keeps allos cells 1, 3, and 5, cell 1 stays K−, and D on that cell is NT. A D− search leaves that lot out. Search results keep the column headings while scrolling, and the lot column shows E2E-WARM-AUTO-ALLOS. A homozygous E+ search leaves that lot out because e is NT, and the status line says positive cells are homozygous. Opening Search replaces a leftover worklist line, and returning to Search keeps the homozygous result. A rule-out cell in Explain names the run, so a ficin nonreactive cell is not read as untreated. A cell that blocks rule-out names the run as well. The Ruled Out cell grid shows the check-cell grade beside AHG. Supporting cells and conflicting cells on Suspected Antibodies show the check-cell grade beside AHG. Explain cites a supporting cell with its strongest phase and the check cell. Explain cites a rule-out cell with the run, the zygosity, the nonreactive phase grades, and the check cell. Explain cites a cell that blocks rule-out with the run, the strongest phase, and the check cell. A rule-out cell whose AHG is 0 and whose check cell did not react is pink on the Ruled Out grid, with the same warning Reactions uses. Explain says a rule-out cell at CC 0 did not react, and a rule-out cell at CC 2+ does not. The Ruled Out explanation names a counted cell whose check cell did not react, and a counted cell at CC 2+ is listed without that phrase. The Specimens ruled-out list names a counted cell whose check cell did not react, and a counted cell at CC 2+ leaves that column blank. Specimen Summary and Analysis Results name that same cell beside the count, and a counted cell at CC 2+ keeps the count only. Clinical Identification already names the cell in the ruled-out sentence. Specimen Summary CSV names a counted cell whose check cell did not react in the CheckCell column, and a counted cell at CC 2+ leaves that column blank. Changing the report on the Reports tab updates the status line to name that report. Changing the Analytics date range updates the status line to name that range and the confirmed-specimen count. Selecting another specimen updates the Specimens status line to name that accession. Selecting another panel updates the Panels status line to name that lot. Changing the run on Reactions updates the status line to name that run. Selecting another specimen on Analysis updates the status line to name that accession. Selecting another rule updates the Rules status line to name that rule. | Selecting a search result updates the Search status line to name that panel, lot, and cell. Opening a search result shows that panel and selects that cell. A finished search brings the antigen that matched into view on the result row, and the panel, lot, and cell stay on screen. When several antigens are set, the last of those is brought into view on the result row, and the panel, lot, and cell stay on screen. Opening that result shows the search antigens first on the panel, so the earlier one stays beside the later one, and the cell stays on screen. A finished search shows those antigens first on the result row, so the earlier one stays beside the later one. A finished search leaves off a warehouse antigen column, such as Kna, that is blank on every matching cell. Dia stays when a matching cell types it, and an untyped cell in that column shows NT. Scrolling to that column keeps the search antigens on the row with the panel, lot, and cell. Opening that result keeps those antigens on the panel with the cell when the panel is scrolled. Opening that result also brings the lot into view in the panel list. The last lot stays fully on screen, including its lot, cell count, and expiration. The panel list shows that name in full. The search result row shows that name in full as well. The Search help says the panel name and the lot are shown in full. Close dismisses that help. The All Panels report keeps a long name, its lot, the cell count, and the expiration in their own columns. The intended-use statement stays on the report preview. The specimen list shows the full accession and expiration. Reactions for the invalid check cell show pink CC 0 on the nonreactive cell and name the ruled-out antibody. Analysis names cell 1 check cell did not react and shows that cell at CC 0 beside cell 3 at CC 2+. Combinations for that specimen stay empty, which matches zero suspected antibodies. Pattern Matches stays empty because every panel cell is nonreactive. Special Panels stays empty because this workup has only an untreated run. |
| Analysis | Single / multiple / dosage scoring | Partial | Fisher + pattern; pairwise combinations; dosage averages | Label as evidence, not diagnosis |
| Analysis | Panreactive / cold / warm / HTLA / auto / HFA / LFA models | Partial | DAT scores; named LFA/pan cells; an adsorbed aliquot is left out of the cold/warm/panreactive pattern count. Enzyme and DTT comparisons name unpaired runs, short comparisons, no-fit comparisons, one-sided cells, cells missing from the panel list, and panel cells that were nonreactive on both runs. A DEMO scenario that has never been analyzed is analyzed on open; grades stay as seeded. The clinical identification report lists panreactive and autoantibody pattern evidence and does not call a warm-auto workup unsuspected. Search for a typed minus skips NT, and a result row shows NT for an untyped antigen. An anti-D rule with exception C does not grant a heterozygous anti-C rule-out. Empty analytics charts do not draw a negative count scale. A Preferences change to the default rule-out count marks an unconfirmed analysis stale. The audit log can be narrowed to one seeded accession. An archive of the warm-auto workup shows the stored DAT and does not invent an identification. The printed antigram omits an all-NT column and keeps typed E, K, Fya, and a typed minus. The warm-auto autocontrol prints NT for E, K, and Fya, and a typed minus stays. A restored specimen keeps the saved analysis snapshot, including its settings. A restored lot keeps a saved antigen order, and a lot still in the database keeps its current order. The reactions grid for the warm-auto lot shows E, K, and Fya and hides an all-NT column. A typed minus stays. Specimens leads with the untreated-run exclusion, and the ruled-out grid follows that analysis, so anti-E, anti-K, and anti-Fya are not listed when the partner antigen is NT. Analysis Explain for the warm-auto lot says D is NT on every cell, and does not call that column never imported. The Analysis Summary for that workup keeps the warm-auto pattern and does not say no antibodies are suspected. Analysis Results and Specimen Summary follow that same analysis, so the warm-auto report does not list anti-E, anti-K, or anti-Fya as ruled out, and Analysis Results includes the pattern. Analysis Results for E2E-WARM-AUTO-ALLOS names anti-E and anti-K and says one cell lost reactivity after autoadsorption while five remained reactive. Clinical Identification cites those supporting cells on the Autoadsorbed run. Specimens lists that residual comparison immediately after the untreated-run exclusion, ahead of the warm and pan sentences, with anti-E and anti-K suspected and nothing ruled out. Compare on the Autoadsorbed run of E2E-WARM-AUTO-ALLOS checks the cells that differ from Untreated, including cell 4 at AHG 0 versus 3+, and does not open a binding error. Those grade columns are named Autoadsorbed and Untreated, so cell 4 reads Autoadsorbed AHG 0 beside Untreated AHG 3+. The E2E warm auto plus allos panel hides every all-NT antigen column and keeps typed c, E, and K, including a typed minus. The panel list shows the full lot E2E-WARM-AUTO-ALLOS beside that name. An E+ search keeps allos cells 1, 3, and 5, cell 1 stays K−, and D on that cell is NT. A D− search leaves that lot out. Search results keep the column headings while scrolling, and the lot column shows E2E-WARM-AUTO-ALLOS. A homozygous E+ search leaves that lot out because e is NT, and the status line says positive cells are homozygous. Opening Search replaces a leftover worklist line, and returning to Search keeps the homozygous result. A rule-out cell in Explain names the run, so a ficin nonreactive cell is not read as untreated. A cell that blocks rule-out names the run as well. The Ruled Out cell grid shows the check-cell grade beside AHG. Supporting cells and conflicting cells on Suspected Antibodies show the check-cell grade beside AHG. Explain cites a supporting cell with its strongest phase and the check cell. Explain cites a rule-out cell with the run, the zygosity, the nonreactive phase grades, and the check cell. Explain cites a cell that blocks rule-out with the run, the strongest phase, and the check cell. A rule-out cell whose AHG is 0 and whose check cell did not react is pink on the Ruled Out grid, with the same warning Reactions uses. Explain says a rule-out cell at CC 0 did not react, and a rule-out cell at CC 2+ does not. The Ruled Out explanation names a counted cell whose check cell did not react, and a counted cell at CC 2+ is listed without that phrase. The Specimens ruled-out list names a counted cell whose check cell did not react, and a counted cell at CC 2+ leaves that column blank. Specimen Summary and Analysis Results name that same cell beside the count, and a counted cell at CC 2+ keeps the count only. Clinical Identification already names the cell in the ruled-out sentence. Specimen Summary CSV names a counted cell whose check cell did not react in the CheckCell column, and a counted cell at CC 2+ leaves that column blank. Changing the report on the Reports tab updates the status line to name that report. Changing the Analytics date range updates the status line to name that range and the confirmed-specimen count. Selecting another specimen updates the Specimens status line to name that accession. Selecting another panel updates the Panels status line to name that lot. Changing the run on Reactions updates the status line to name that run. Selecting another specimen on Analysis updates the status line to name that accession. Selecting another rule updates the Rules status line to name that rule. | Selecting a search result updates the Search status line to name that panel, lot, and cell. Opening a search result shows that panel and selects that cell. A finished search brings the antigen that matched into view on the result row, and the panel, lot, and cell stay on screen. When several antigens are set, the last of those is brought into view on the result row, and the panel, lot, and cell stay on screen. Opening that result shows the search antigens first on the panel, so the earlier one stays beside the later one, and the cell stays on screen. A finished search shows those antigens first on the result row, so the earlier one stays beside the later one. A finished search leaves off a warehouse antigen column, such as Kna, that is blank on every matching cell. Dia stays when a matching cell types it, and an untyped cell in that column shows NT. Scrolling to that column keeps the search antigens on the row with the panel, lot, and cell. Opening that result keeps those antigens on the panel with the cell when the panel is scrolled. Opening that result also brings the lot into view in the panel list. The last lot stays fully on screen, including its lot, cell count, and expiration. The panel list shows that name in full. The search result row shows that name in full as well. The Search help says the panel name and the lot are shown in full. Close dismisses that help. The All Panels report keeps a long name, its lot, the cell count, and the expiration in their own columns. The intended-use statement stays on the report preview. The specimen list shows the full accession and expiration. Reactions for the invalid check cell show pink CC 0 on the nonreactive cell and name the ruled-out antibody. Analysis names cell 1 check cell did not react and shows that cell at CC 0 beside cell 3 at CC 2+. Combinations for that specimen stay empty, which matches zero suspected antibodies. Pattern Matches stays empty because every panel cell is nonreactive. Special Panels stays empty because this workup has only an untreated run. |
| Analysis | Selected-cell recommendations with “why” | Partial | Unused in-date vials preferred; P1−/Ch−/Sd(a−) selected cells ranked when neutralization favors soluble substance | More ISBT alleles later |
| Analysis | Patient phenotype / genotype / transfusion limits | Partial | GYPA null/Mk En(a); RHCE*Ce vs ce case-sensitive; CeRN variant-C; RHD DAR/DVI/DNB partial D does not rule out anti-D; GATA still does not support anti-Fya | More ISBT alleles later |
| UI | Antigram freeze / slashes / run compare | Partial | Cell plus IS, 37°C, AHG, and CC stay frozen ahead of the antigen columns. An explicit ++ stays visible when Show dosage is off. Specimens shows panreactive and autoantibody pattern sentences when the suspected grid is empty. A new panel cell starts as NT, and an E2E column that is negative on every cell is corrected to NT. A panel column that is NT on every cell is hidden while browsing. The Worklist "Never analyzed" row is for a paneled specimen that still has no analysis. Clinical Identification prints the same pattern sentences. Search results show NT for an untyped antigen. Heterozygous rule-out permission follows the antibody named on the rule. Empty analytics charts stay at zero. A Preferences change that affects analysis marks an unconfirmed specimen stale. Audit Find shows the rows for one accession, and Export CSV writes those rows. Archive rows show stored antibodies, DAT, and notes. The printed antigram omits an all-NT column and keeps typed E, K, Fya, and a typed minus. The warm-auto autocontrol prints NT for E, K, and Fya, and a typed minus stays. A restored specimen keeps the saved analysis snapshot, including its settings. A restored lot keeps a saved antigen order, and a lot still in the database keeps its current order. The reactions grid for the warm-auto lot shows E, K, and Fya and hides an all-NT column. A typed minus stays. Specimens leads with the untreated-run exclusion, and the ruled-out grid follows that analysis, so anti-E, anti-K, and anti-Fya are not listed when the partner antigen is NT. Analysis Explain for the warm-auto lot says D is NT on every cell, and does not call that column never imported. The Analysis Summary for that workup keeps the warm-auto pattern and does not say no antibodies are suspected. Analysis Results and Specimen Summary follow that same analysis, so the warm-auto report does not list anti-E, anti-K, or anti-Fya as ruled out, and Analysis Results includes the pattern. Analysis Results for E2E-WARM-AUTO-ALLOS names anti-E and anti-K and says one cell lost reactivity after autoadsorption while five remained reactive. Clinical Identification cites those supporting cells on the Autoadsorbed run. Specimens lists that residual comparison immediately after the untreated-run exclusion, ahead of the warm and pan sentences, with anti-E and anti-K suspected and nothing ruled out. Compare on the Autoadsorbed run of E2E-WARM-AUTO-ALLOS checks the cells that differ from Untreated, including cell 4 at AHG 0 versus 3+, and does not open a binding error. Those grade columns are named Autoadsorbed and Untreated, so cell 4 reads Autoadsorbed AHG 0 beside Untreated AHG 3+. The E2E warm auto plus allos panel hides every all-NT antigen column and keeps typed c, E, and K, including a typed minus. The panel list shows the full lot E2E-WARM-AUTO-ALLOS beside that name. An E+ search keeps allos cells 1, 3, and 5, cell 1 stays K−, and D on that cell is NT. A D− search leaves that lot out. Search results keep the column headings while scrolling, and the lot column shows E2E-WARM-AUTO-ALLOS. A homozygous E+ search leaves that lot out because e is NT, and the status line says positive cells are homozygous. Opening Search replaces a leftover worklist line, and returning to Search keeps the homozygous result. A rule-out cell in Explain names the run, so a ficin nonreactive cell is not read as untreated. A cell that blocks rule-out names the run as well. The Ruled Out cell grid shows the check-cell grade beside AHG. Supporting cells and conflicting cells on Suspected Antibodies show the check-cell grade beside AHG. Explain cites a supporting cell with its strongest phase and the check cell. Explain cites a rule-out cell with the run, the zygosity, the nonreactive phase grades, and the check cell. Explain cites a cell that blocks rule-out with the run, the strongest phase, and the check cell. A rule-out cell whose AHG is 0 and whose check cell did not react is pink on the Ruled Out grid, with the same warning Reactions uses. Explain says a rule-out cell at CC 0 did not react, and a rule-out cell at CC 2+ does not. The Ruled Out explanation names a counted cell whose check cell did not react, and a counted cell at CC 2+ is listed without that phrase. The Specimens ruled-out list names a counted cell whose check cell did not react, and a counted cell at CC 2+ leaves that column blank. Specimen Summary and Analysis Results name that same cell beside the count, and a counted cell at CC 2+ keeps the count only. Clinical Identification already names the cell in the ruled-out sentence. Specimen Summary CSV names a counted cell whose check cell did not react in the CheckCell column, and a counted cell at CC 2+ leaves that column blank. Changing the report on the Reports tab updates the status line to name that report. Changing the Analytics date range updates the status line to name that range and the confirmed-specimen count. Selecting another specimen updates the Specimens status line to name that accession. Selecting another panel updates the Panels status line to name that lot. Changing the run on Reactions updates the status line to name that run. Selecting another specimen on Analysis updates the status line to name that accession. Selecting another rule updates the Rules status line to name that rule. | Selecting a search result updates the Search status line to name that panel, lot, and cell. Opening a search result shows that panel and selects that cell. A finished search brings the antigen that matched into view on the result row, and the panel, lot, and cell stay on screen. When several antigens are set, the last of those is brought into view on the result row, and the panel, lot, and cell stay on screen. Opening that result shows the search antigens first on the panel, so the earlier one stays beside the later one, and the cell stays on screen. A finished search shows those antigens first on the result row, so the earlier one stays beside the later one. A finished search leaves off a warehouse antigen column, such as Kna, that is blank on every matching cell. Dia stays when a matching cell types it, and an untyped cell in that column shows NT. Scrolling to that column keeps the search antigens on the row with the panel, lot, and cell. Opening that result keeps those antigens on the panel with the cell when the panel is scrolled. Opening that result also brings the lot into view in the panel list. The last lot stays fully on screen, including its lot, cell count, and expiration. The panel list shows that name in full. The search result row shows that name in full as well. The Search help says the panel name and the lot are shown in full. Close dismisses that help. The All Panels report keeps a long name, its lot, the cell count, and the expiration in their own columns. The intended-use statement stays on the report preview. The specimen list shows the full accession and expiration. Reactions for the invalid check cell show pink CC 0 on the nonreactive cell and name the ruled-out antibody. Analysis names cell 1 check cell did not react and shows that cell at CC 0 beside cell 3 at CC 2+. Combinations for that specimen stay empty, which matches zero suspected antibodies. Pattern Matches stays empty because every panel cell is nonreactive. Special Panels stays empty because this workup has only an untreated run. |
| UI | Explain Analysis panel | Present | Explain tab + Summary + clinical report assemble rule-out, support, conflict, phenotype, additional testing | Keep wording as evidence, not diagnosis |
| Audit | Panel, reactions, rules, settings, final ID | Partial | Snapshots store rule-engine version, rules JSON, operator, and the adsorption comparison (conclusions plus ordinary-score exclusions) | Cross-panel enzyme/DTT pairing later |
| Tests | Synthetic cases with intermediate reasoning | Partial | Enzyme and DTT cases cover unpaired runs, short comparisons, no-fit comparisons, one-sided cells, cells missing from the panel list, both-nonreactive panel cells, and a partial destroyed pattern. E2E warm-auto cells keep typed E, K, and Fya and store untyped D as NT. Explicit ++ stays visible with dosage off. DEMO scenarios drop the Never analyzed worklist row after one analysis and keep the seeded AHG grade. The warm-auto clinical report includes the warm and autoantibody sentences; anti-K does not. Search for D− skips the warm-auto lot, and an E+ result row shows D as NT and K− as minus. Two heterozygous C+ nonreactive cells plus an anti-D exception for C do not rule out anti-C. Empty analytics charts are titled and limited to a non-negative range. A default rule-out change marks an unconfirmed analysis stale and does not list a confirmed ID. Audit Find matches E2E-WARM-AUTO and skips E2E-NEGATIVE. An archive inspection shows DAT Positive for the warm-auto specimen and anti-K for the confirmed row. The printed antigram omits an all-NT column and keeps typed E, K, Fya, and a typed minus. The warm-auto autocontrol prints NT for E, K, and Fya, and a typed minus stays. A restored specimen keeps the saved analysis snapshot, including its settings. A restored lot keeps a saved antigen order, and a lot still in the database keeps its current order. The reactions grid for the warm-auto lot shows E, K, and Fya and hides an all-NT column. A typed minus stays. Specimens leads with the untreated-run exclusion, and the ruled-out grid follows that analysis, so anti-E, anti-K, and anti-Fya are not listed when the partner antigen is NT. Analysis Explain for the warm-auto lot says D is NT on every cell, and does not call that column never imported. The Analysis Summary for that workup keeps the warm-auto pattern and does not say no antibodies are suspected. Analysis Results and Specimen Summary follow that same analysis, so the warm-auto report does not list anti-E, anti-K, or anti-Fya as ruled out, and Analysis Results includes the pattern. Analysis Results for E2E-WARM-AUTO-ALLOS names anti-E and anti-K and says one cell lost reactivity after autoadsorption while five remained reactive. Clinical Identification cites those supporting cells on the Autoadsorbed run. Specimens lists that residual comparison immediately after the untreated-run exclusion, ahead of the warm and pan sentences, with anti-E and anti-K suspected and nothing ruled out. Compare on the Autoadsorbed run of E2E-WARM-AUTO-ALLOS checks the cells that differ from Untreated, including cell 4 at AHG 0 versus 3+, and does not open a binding error. Those grade columns are named Autoadsorbed and Untreated, so cell 4 reads Autoadsorbed AHG 0 beside Untreated AHG 3+. The E2E warm auto plus allos panel hides every all-NT antigen column and keeps typed c, E, and K, including a typed minus. The panel list shows the full lot E2E-WARM-AUTO-ALLOS beside that name. An E+ search keeps allos cells 1, 3, and 5, cell 1 stays K−, and D on that cell is NT. A D− search leaves that lot out. Search results keep the column headings while scrolling, and the lot column shows E2E-WARM-AUTO-ALLOS. A homozygous E+ search leaves that lot out because e is NT, and the status line says positive cells are homozygous. Opening Search replaces a leftover worklist line, and returning to Search keeps the homozygous result. A rule-out cell in Explain names the run, so a ficin nonreactive cell is not read as untreated. A cell that blocks rule-out names the run as well. The Ruled Out cell grid shows the check-cell grade beside AHG. Supporting cells and conflicting cells on Suspected Antibodies show the check-cell grade beside AHG. Explain cites a supporting cell with its strongest phase and the check cell. Explain cites a rule-out cell with the run, the zygosity, the nonreactive phase grades, and the check cell. Explain cites a cell that blocks rule-out with the run, the strongest phase, and the check cell. A rule-out cell whose AHG is 0 and whose check cell did not react is pink on the Ruled Out grid, with the same warning Reactions uses. Explain says a rule-out cell at CC 0 did not react, and a rule-out cell at CC 2+ does not. The Ruled Out explanation names a counted cell whose check cell did not react, and a counted cell at CC 2+ is listed without that phrase. The Specimens ruled-out list names a counted cell whose check cell did not react, and a counted cell at CC 2+ leaves that column blank. Specimen Summary and Analysis Results name that same cell beside the count, and a counted cell at CC 2+ keeps the count only. Clinical Identification already names the cell in the ruled-out sentence. Specimen Summary CSV names a counted cell whose check cell did not react in the CheckCell column, and a counted cell at CC 2+ leaves that column blank. Changing the report on the Reports tab updates the status line to name that report. Changing the Analytics date range updates the status line to name that range and the confirmed-specimen count. Selecting another specimen updates the Specimens status line to name that accession. Selecting another panel updates the Panels status line to name that lot. Changing the run on Reactions updates the status line to name that run. Selecting another specimen on Analysis updates the status line to name that accession. Selecting another rule updates the Rules status line to name that rule. | Selecting a search result updates the Search status line to name that panel, lot, and cell. Opening a search result shows that panel and selects that cell. A finished search brings the antigen that matched into view on the result row, and the panel, lot, and cell stay on screen. When several antigens are set, the last of those is brought into view on the result row, and the panel, lot, and cell stay on screen. Opening that result shows the search antigens first on the panel, so the earlier one stays beside the later one, and the cell stays on screen. A finished search shows those antigens first on the result row, so the earlier one stays beside the later one. A finished search leaves off a warehouse antigen column, such as Kna, that is blank on every matching cell. Dia stays when a matching cell types it, and an untyped cell in that column shows NT. Scrolling to that column keeps the search antigens on the row with the panel, lot, and cell. Opening that result keeps those antigens on the panel with the cell when the panel is scrolled. Opening that result also brings the lot into view in the panel list. The last lot stays fully on screen, including its lot, cell count, and expiration. The panel list shows that name in full. The search result row shows that name in full as well. The Search help says the panel name and the lot are shown in full. Close dismisses that help. The All Panels report keeps a long name, its lot, the cell count, and the expiration in their own columns. The intended-use statement stays on the report preview. The specimen list shows the full accession and expiration. Reactions for the invalid check cell show pink CC 0 on the nonreactive cell and name the ruled-out antibody. Analysis names cell 1 check cell did not react and shows that cell at CC 0 beside cell 3 at CC 2+. Combinations for that specimen stay empty, which matches zero suspected antibodies. Pattern Matches stays empty because every panel cell is nonreactive. Special Panels stays empty because this workup has only an untreated run. |

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

**Deficiency:** A ficin, papain, or DTT comparison named paired cells
and skipped cells that were tested on only one of the two runs. A
reviewer could not tell that those cells were left out of the pair
count. The persistent case library also stopped at DEMO- and 2026-
patterns, so warm, cold, auto, exotic, and combination workups were
not all available to reopen later.

**Bounded improvement:** Same-panel enzyme and DTT notes now name
cells tested only on the untreated run and cells tested only on the
treated run, and say they were left out of the pair count. The
sentence is evidence, not a diagnosis. `E2EWorkflowSeeder` writes
idempotent E2E- accessions for warm allo (including Jka dosage), cold
allo, warm auto, cold auto, warm auto plus two allos, mixed cold plus
warm with prewarm, combinations, Vel, k, U, Dia, Wra, Kpb, an HTLA-like
Chido case with a titer and neutralization grade, a negative panel,
and panreactivity with a negative autocontrol. Existing rows are not
overwritten. Load Demo Data and `--seed-clinical` call the seeder.

**Not in this tick:** Naming cells that were recorded on both runs but
omitted because they are not on the panel cell list. Authenticated
vendor portals stay later and must not bypass auth, CAPTCHA, licensing,
or terms.

## Iteration 51 (this change)

**Deficiency:** A cell recorded on both the untreated run and the
ficin, papain, or DTT run was skipped with no sentence when that cell
number was not on the panel cell list. A DTT cell in that position
could also be counted as newly reactive even though the panel had no
antigen typing for it.

**Bounded improvement:** Those cells are omitted from the pair count
and named: "not on the panel cell list." The sentence is evidence, not
a diagnosis. Explain Analysis and the clinical report show it. A cell
that would have looked newly reactive on DTT is not counted as a gain.

**Not in this tick:** Naming panel cells that were recorded on both
runs but left out of the pair count because both grades were
nonreactive. Authenticated vendor portals stay later and must not
bypass auth, CAPTCHA, licensing, or terms.

## Iteration 52 (this change)

**Deficiency:** A panel cell recorded on both the untreated run and the
ficin, papain, or DTT run disappeared from the comparison when both
grades were nonreactive. A destroyed pattern could name the cells that
lost reactivity and never say which other cells had been tested and
stayed negative on both runs.

**Bounded improvement:** Those cells stay out of the pair count and are
named: "both grades were nonreactive." The sentence is evidence, not a
diagnosis. Explain Analysis and the clinical report show it. An
all-nonreactive comparison still stays incomplete and now lists the
cells.

**Not in this tick:** The Reactions grid for E2E-WARM-JKA opens on the
antigen columns. Stored grades are AHG 3+ on homozygous Jka cells and
1+ on heterozygous cells, and E2E-COLD-M is IS 2+ only, but those
phase columns were off the first screen. Authenticated vendor portals
stay later and must not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 53 (this change)

**Deficiency:** Reactions inserted the antigram at column 1, so IS,
37°C, AHG, and CC sat past a full antigen row. A reviewer opening
E2E-WARM-JKA saw Jka typing and had to scroll to the grades. An
explicit ++ was also drawn as + until Show dosage was checked.

**Bounded improvement:** Antigen columns now follow the check-cell
column, and those phase columns stay frozen. A stored ++ remains ++
when Show dosage is off. Inferred zygosity (E+ e− shown as + until
the checkbox) is unchanged. Nothing is identified. The Release
screen for E2E-WARM-JKA shows AHG 3+ with Jka ++ on cells 1 and 4
and AHG 1+ with Jka + on cells 2 and 5, with Show dosage unchecked.
E2E-COLD-M shows IS 2+ on cells 1, 3, and 5 and 0 at 37°C and AHG.

**Not in this tick:** The Specimens screen for warm auto and cold
auto. Authenticated vendor portals stay later and must not bypass
auth, CAPTCHA, licensing, or terms.

## Iteration 54 (this change)

**Deficiency:** E2E-WARM-AUTO and E2E-COLD-AUTO stored no suspected
specificity, so the Specimens screen looked like a negative workup.
The warm-auto pattern count also mixed the all-negative autoadsorbed
aliquot into the untreated cells, so the panreactive and warm notes
never formed.

**Bounded improvement:** Cold, warm, and panreactive pattern counts
skip adsorbed aliquots. Absorption notes still describe that aliquot.
Specimens shows those pattern sentences, including the autoadsorption
removal note, and says when a specimen has no suspected specificity
and no panreactive or autoantibody note. An ordinary anti-K score
does not add the block. Nothing is identified.

**Not in this tick:** The Panels screen for the warm-auto lot.
Authenticated vendor portals stay later and must not bypass auth,
CAPTCHA, licensing, or terms.

## Iteration 55 (this change)

**Deficiency:** The E2E warm-auto lot stored every antigen the seeder
did not type as "-". On Panels, cells 1 through 6 and the autocontrol
looked antigen-negative for D, C, c, and the rest of the standard
columns. A typed minus on K or Fya was indistinguishable from an
antigen that had never been tested.

**Bounded improvement:** A new panel cell starts as NT. Opening the
app rewrites an E2E column that is still "-" on every cell, including
the autocontrol, to NT. A column with any + or ++ is unchanged, so
the warm-auto K- and Fya- cells stay typed negatives. Nothing is
identified.

**Not in this tick:** The Worklist rows for demo scenarios that still
say Never analyzed. Authenticated vendor portals stay later and must
not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 56 (this change)

**Deficiency:** The 11 DEMO scenarios stored finished grades and a
linked panel, then never recorded an analysis. The Worklist listed
each one as stale with the detail "Never analyzed."

**Bounded improvement:** Opening the application, loading demo data,
or running the clinical seed analyzes a DEMO scenario only when it
has a panel and no prior analysis. A second pass does nothing. Grades
stay as seeded: DEMO-S1 cell 1 remains AHG 2+, and anti-E is stored
as evidence. A specimen outside that list, including 2026- accessions,
is not analyzed by this pass. Nothing is identified.

**Not in this tick:** Reports for a seeded workup. Authenticated
vendor portals stay later and must not bypass auth, CAPTCHA,
licensing, or terms.

## Iteration 57 (this change)

**Deficiency:** The Clinical Identification report for E2E-WARM-AUTO
said "No antibodies suspected based on current reactions." Specimens
already showed the warm IAT pattern, panagglutination, and the
autoantibody note. The printed worksheet did not.

**Bounded improvement:** When a result has a panreactive, autoantibody,
or high-prevalence pattern, the clinical report lists those pattern
sentences under "Pattern evidence (not a diagnosis)." The unsuspected
sentence is left off in that case. An ordinary anti-K report does not
get the block. Absorption notes stay in the absorption section.
Nothing is identified.

**Not in this tick:** Search for a seeded antigen profile. The panel
antigram still prints an all-NT antigen column. Authenticated vendor
portals stay later and must not bypass auth, CAPTCHA, licensing, or
terms.

## Iteration 58 (this change)

**Deficiency:** Search results drew every untyped antigen as −. On the
E2E warm-auto lot, a search for E+ returned cell 1 with D shown as
minus even though D is stored NT. A typed K− and an untyped D looked
the same.

**Bounded improvement:** A result row shows the stored typing. NT
stays NT, a typed minus stays −, and an explicit ++ stays ++. A −
criterion already matches only a stored minus, so the warm-auto lot
is left out of a D− search and cell 1 still matches K−. Nothing is
identified.

**Not in this tick:** Rules for one seeded specificity. The panel
antigram still prints an all-NT antigen column. Authenticated vendor
portals stay later and must not bypass auth, CAPTCHA, licensing, or
terms.

## Iteration 59 (this change)

**Deficiency:** The anti-D rule that allows heterozygous cells for
exception antigen C was applied to anti-C. Two nonreactive
heterozygous C+ cells then met rule-out for anti-C even when the
workup was not an anti-D case. The rule's own antibody was ignored.

**Bounded improvement:** Heterozygous permission applies only when
the rule names that antibody. An exception antigen must be blank or
the same antigen. The anti-D rule still requires 3 cells for anti-D.
A rule that names anti-C and exception C still allows heterozygous
C cells. Anti-C and anti-c stay separate. Nothing is identified.

**Not in this tick:** Analytics for one seeded workup. The panel
antigram still prints an all-NT antigen column. Authenticated vendor
portals stay later and must not bypass auth, CAPTCHA, licensing, or
terms.

## Iteration 60 (this change)

**Deficiency:** Analytics for the seeded workups, which have no
confirmed identification, drew both charts on a scale from −10 to 10.
A count of zero looked like a chart that could go negative.

**Bounded improvement:** An empty specificity chart and an empty
month chart keep their titles and use a 0 to 1 range with no tick
labels. A range that has confirmed identifications is unchanged.
Suspected results stay out of the counts. Nothing is identified.

**Not in this tick:** Preferences for one seeded workup. The panel
antigram still prints an all-NT antigen column. Authenticated vendor
portals stay later and must not bypass auth, CAPTCHA, licensing, or
terms.

## Iteration 61 (this change)

**Deficiency:** Saving a new score threshold, ID cell rule, ACS
rule-out count, default rule-out count, or extra-phase list left
every unconfirmed analysis looking current. The worklist stayed
clear, and the Analysis tab did not say the saved result was from
the previous Preferences.

**Bounded improvement:** An unconfirmed specimen whose latest
analysis snapshot does not match those Preferences is listed as
stale, with the detail "Preferences changed since last analysis."
The Analysis tab uses the same reason. A lab-name change does not.
A confirmed identification is not rewritten and is not listed.
Nothing is identified.

**Not in this tick:** The audit log for one seeded workup. The panel
antigram still prints an all-NT antigen column. Authenticated vendor
portals stay later and must not bypass auth, CAPTCHA, licensing, or
terms.

## Iteration 62 (this change)

**Deficiency:** The audit log listed the newest 500 of 671 events and
had no way to find one accession. Export CSV wrote a second, larger
query instead of the rows on screen, so a seeded workup such as
E2E-WARM-AUTO could not be reviewed as one record.

**Bounded improvement:** The window loads the newest 5,000 events.
Find narrows them by accession, action, Windows login, record type,
or reason. Export CSV writes the rows that Find is showing. The
Operator column remains the Windows login. Nothing is identified.

**Not in this tick:** The archive for one seeded workup. The panel
antigram still prints an all-NT antigen column. Authenticated vendor
portals stay later and must not bypass auth, CAPTCHA, licensing, or
terms.

## Iteration 63 (this change)

**Deficiency:** Open Archive listed each specimen by accession, type,
created date, and whether it was already in the live database. A
seeded warm-auto specimen and a seeded anti-K specimen looked the
same until someone restored them.

**Bounded improvement:** Each archive row shows the recorded
evidence. A confirmed identification is shown as stored. Otherwise
the row shows the antibodies stored with the specimen, the DAT, and
the notes. E2E-WARM-AUTO shows DAT Positive. A confirmed anti-K row
shows anti-K and does not pick up another antibody. Nothing is
identified.

**Not in this tick:** The panel antigram still prints an all-NT
antigen column. Authenticated vendor portals stay later and must not
bypass auth, CAPTCHA, licensing, or terms.

## Iteration 64 (this change)

**Deficiency:** Panel Summary, Panel Antigram, and the Clinical
Identification worksheet printed every antigen. GetAntigen maps NT
to a minus, so an E2E warm-auto column that is NT on every cell,
including D, printed as negative.

**Bounded improvement:** Those printed grids omit an antigen that is
NT or blank on every cell. Typed E, K, and Fya stay on the warm-auto
lot, and a typed minus stays. The stored panel order is unchanged.
Nothing is identified.

**Not in this tick:** An NT cell in a column that is typed on another
cell still prints as a minus. The autocontrol on the warm-auto lot
is that case for E, K, and Fya. Analysis snapshots are still not
copied into an archive. Authenticated vendor portals stay later and
must not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 65 (this change)

**Deficiency:** After an all-NT column was omitted, an NT cell in a
column that is typed on another cell still printed as a minus.
The warm-auto autocontrol is NT for E, K, and Fya, so the worksheet
showed that autocontrol as antigen-negative.

**Bounded improvement:** Panel Summary, Panel Antigram, and the
Clinical Identification grid print NT for an untyped cell. A typed
minus stays a minus. An E2E autocontrol left as minus by the old
default is stored as NT, because those lots do not type the
autocontrol. A donor cell keeps a typed minus. The warm-auto
autocontrol prints NT for E, K, and Fya, and cell 1 keeps K as a
typed minus. Nothing is identified.

**Not in this tick:** Analysis snapshots are still not copied into
an archive. Authenticated vendor portals stay later and must not
bypass auth, CAPTCHA, licensing, or terms.

## Iteration 66 (this change)

**Deficiency:** Purge removes the analysis snapshot with the
specimen. The archive did not copy that snapshot, so a restore
brought the specimen back without the settings used to judge
whether the analysis is stale.

**Bounded improvement:** The archive copies each saved analysis
snapshot for the purged specimens. Restore writes those rows back
for specimens that were missing. A specimen already in the live
database is skipped, and a second restore does not add another
snapshot. The settings, fingerprint, ruled-out list, suspected
list, rule version, operator, and absorption note come back as
stored. Nothing is identified.

**Not in this tick:** The saved panel antigen order is still not
copied into an archive. Authenticated vendor portals stay later
and must not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 67 (this change)

**Deficiency:** An archive copied the panel lot and its cells, but
not the saved antigen order. After the lot was deleted, restore put
the cells back in the default order.

**Bounded improvement:** The archive copies the saved order for
each purged lot. Restore writes that order back only for a lot that
was missing. A lot still in the database keeps the order it has
now. A second restore does not change either order. Nothing is
identified.

**Not in this tick:** Authenticated vendor portals stay later and
must not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 68 (this change)

**Deficiency:** The Reactions grid for a seeded warm-auto workup
showed every antigen column. Columns that are NT on every cell,
including D, sat beside the typed E, K, and Fya columns.

**Bounded improvement:** Those columns are hidden. A typed minus
keeps its column. Dosage grouping still applies to the columns
that remain. The warm-auto grid shows E, K, and Fya. Nothing is
identified.

**Not in this tick:** The Specimens screen for the same workup.
Authenticated vendor portals stay later and must not bypass auth,
CAPTCHA, licensing, or terms.

## Iteration 69 (this change)

**Deficiency:** Specimens for E2E-WARM-AUTO listed saved rule-outs
for anti-E, anti-K, and anti-Fya. The current analysis does not
rule those out: the partner antigens are NT, so the cells are not
homozygous rule-outs. The pattern block also omitted the sentence
that the panreactive untreated run was left out of ordinary scores.

**Bounded improvement:** Suspected and ruled-out rows follow the
same current analysis as the pattern block. An older saved list is
not shown in their place. The warm-auto ruled-out pane says no
specificity meets the criteria. The exclusion of the untreated run
is listed first and points to the autoadsorbed aliquot. An ordinary
anti-K workup still has no pattern block and still shows anti-K.
Nothing is identified.

**Not in this tick:** The Analysis screen for the same workup.
Authenticated vendor portals stay later and must not bypass auth,
CAPTCHA, licensing, or terms.

## Iteration 70 (this change)

**Deficiency:** Analysis Explain for E2E-WARM-AUTO said anti-D
could not be typed because D was never imported. D is on that
panel. Every cell stores D as NT. A typed antigen such as E stays
available for scoring.

**Bounded improvement:** An antigen that is NT on every linked
cell is described that way. The Explain detail and the suggestion
say NT on every cell, and they do not say the antigen was never
imported. An antigen that is not on the panel still says never
imported. Nothing is identified.

**Not in this tick:** The Analysis Summary still says no
antibodies are suspected when the warm-auto pattern is present.
Authenticated vendor portals stay later and must not bypass auth,
CAPTCHA, licensing, or terms.

## Iteration 71 (this change)

**Deficiency:** The Analysis Summary for E2E-WARM-AUTO said no
antibodies were suspected. The same summary already listed the
warm pattern, the panagglutinin, and the autoantibody note. That
sentence contradicted the pattern.

**Bounded improvement:** Summary omits that sentence when a
panreactive, autoantibody, or high-prevalence pattern is present.
The pattern sentences stay. An ordinary anti-K workup still lists
anti-K. A negative workup with no such pattern still says no
antibodies are suspected. Nothing is identified.

**Not in this tick:** The Reports screen for the same workup.
Authenticated vendor portals stay later and must not bypass auth,
CAPTCHA, licensing, or terms.

## Iteration 72 (this change)

**Deficiency:** Analysis Results and Specimen Summary for
E2E-WARM-AUTO printed the saved rule-out list. That list still
named anti-E, anti-K, and anti-Fya. The current analysis does
not rule those out, because the partner antigens are NT. The
analysis report also omitted the warm-auto pattern.

**Bounded improvement:** Both reports use the current analysis.
The warm-auto ruled-out count is zero. Analysis Results includes
the pattern evidence, including the untreated-run exclusion. An
ordinary anti-K report still names anti-K and does not add the
pattern block. Nothing is identified.

**Not in this tick:** Analysis Results for E2E-WARM-AUTO-ALLOS,
which has residual reactivity after autoadsorption. Authenticated
vendor portals stay later and must not bypass auth, CAPTCHA,
licensing, or terms.

## Iteration 73 (this change)

**Deficiency:** Analysis Results for E2E-WARM-AUTO-ALLOS listed
anti-E and anti-K, but the autoadsorption comparison was empty.
Five cells stayed reactive after autoadsorption and only cell 4
became negative. The comparison required two cells to lose
reactivity before it would call that residual.

**Bounded improvement:** One paired cell that loses reactivity
while other paired cells remain reactive is residual reactivity.
The report says 1 of 6 paired cells lost reactivity after
Autoadsorbed and 5 remained reactive, and that this may be an
underlying alloantibody or incomplete adsorption. anti-E and
anti-K stay on the suspected list. A warm-auto aliquot that
clears every cell still reads as removal, not residual. Nothing
is identified.

**Not in this tick:** Clinical Identification for the same
workup. Authenticated vendor portals stay later and must not
bypass auth, CAPTCHA, licensing, or terms.

## Iteration 74 (this change)

**Deficiency:** Clinical Identification for E2E-WARM-AUTO-ALLOS
listed supporting cells for anti-E and anti-K as panel cells at
AHG 2+ and did not name the run. The untreated run of those same
cells is AHG 3+ on every cell, so the citation could be read as
the panreactive serum.

**Bounded improvement:** Explain citations include the run label.
The worksheet says cell 1 (Autoadsorbed, AHG 2+) and the same
for the other supporting cells. anti-E and anti-K stay
unconfirmed. Nothing is identified.

**Not in this tick:** The Specimens screen for the same workup.
Authenticated vendor portals stay later and must not bypass auth,
CAPTCHA, licensing, or terms.

## Iteration 75 (this change)

**Deficiency:** Specimens for E2E-WARM-AUTO-ALLOS led the pattern
box with the untreated panreactive note. The comparison that one
cell lost reactivity and five remained reactive came after the
warm, pan, and autocontrol sentences, below the visible fold.
anti-E and anti-K were listed beside the wrong run.

**Bounded improvement:** The adsorption comparison follows the
untreated-run exclusion and precedes the warm and pan sentences.
The warm-auto workup that clears every cell still leads with the
exclusion, then the removal sentence. anti-E and anti-K stay
suspected. Nothing is ruled out, and nothing is identified.

**Not in this tick:** Reactions for the same workup. Authenticated
vendor portals stay later and must not bypass auth, CAPTCHA,
licensing, or terms.

## Iteration 76 (this change)

**Deficiency:** Compare on the Autoadsorbed run of E2E-WARM-AUTO-ALLOS
opened an error that a two-way binding cannot use the read-only
AnyChanged property. The Changed boxes stayed clear even where
AHG differed, including cell 4 at 0 against the untreated 3+.

**Bounded improvement:** The Changed column reads that flag one way
and does not accept edits. Cells that differ are checked. Grades
are unchanged. Nothing is identified.

**Not in this tick:** The compare headers still say This and Other.
Authenticated vendor portals stay later and must not bypass auth,
CAPTCHA, licensing, or terms.

## Iteration 77 (this change)

**Deficiency:** Compare on E2E-WARM-AUTO-ALLOS labeled every grade
column This or Other. Cell 4 showed AHG 0 beside AHG 3+ without
saying which grade belonged to Autoadsorbed and which belonged to
Untreated. The row note said this and other as well.

**Bounded improvement:** Grade columns use the run names, so the
pair reads Autoadsorbed AHG and Untreated AHG. A difference note
uses those same names. A comparison with no run name still says
this and other. Grades are unchanged. Nothing is identified.

**Not in this tick:** The Panels screen for this lot. Authenticated
vendor portals stay later and must not bypass auth, CAPTCHA,
licensing, or terms.

## Iteration 78 (this change)

**Deficiency:** The Panels list gave the name the width of its text.
For E2E warm auto plus allos the lot was cut off at
E2E-WARM-AUTO-AL, so the list did not show which lot was selected.
The antigen grid itself was already right: only c, E, and K were
shown, a typed minus stayed, and the autocontrol was NT.

**Bounded improvement:** The name and the lot stay on screen. The
lot column fits E2E-WARM-AUTO-ALLOS. A longer name is shortened,
and pointing at the name or the lot shows the full text. Antigen
columns are unchanged. Nothing is identified.

**Not in this tick:** Search for this lot. Authenticated vendor
portals stay later and must not bypass auth, CAPTCHA, licensing,
or terms.

## Iteration 79 (this change)

**Deficiency:** An E+ search returned 141 cells, but scrolling the
results scrolled the column headings away with the rows. The lot
column was too narrow for E2E-WARM-AUTO-ALLOS, so the matching
allos cells could not be read as cell, lot, E, and K together.
The match itself was already right: cells 1, 3, and 5 are E+,
cell 1 is typed K−, D on those cells is NT, and a D− search does
not include the lot.

**Bounded improvement:** The results grid scrolls on its own, so
the headings stay visible. Panel, lot, and cell stay on screen
while the antigen columns scroll. The lot column fits
E2E-WARM-AUTO-ALLOS. A longer panel name is shortened, and
pointing at the name or the lot shows the full text. Grades and
the stored typing are unchanged. Nothing is identified.

**Not in this tick:** A homozygous E+ search. Authenticated vendor
portals stay later and must not bypass auth, CAPTCHA, licensing,
or terms.

## Iteration 80 (this change)

**Deficiency:** A homozygous E+ search already left out
E2E-WARM-AUTO-ALLOS, because those E+ cells have e stored as NT
and an untyped partner is not a typed minus. The status line
still said "Found N matching cells for 1 criteria," the same
sentence as an ordinary E+ search, so the smaller count did not
say that positive cells had to be homozygous.

**Bounded improvement:** The status line says "Positive cells are
homozygous" or "Positive cells are heterozygous" when that choice
is in effect, and one criterion is named as a criterion. The
match is unchanged: cells 1, 3, and 5 stay in an ordinary E+
search, and a homozygous E+ search leaves the lot out. Grades
and the stored typing are unchanged. Nothing is identified.

**Not in this tick:** The status line left behind by another
screen. Authenticated vendor portals stay later and must not
bypass auth, CAPTCHA, licensing, or terms.

## Iteration 81 (this change)

**Deficiency:** The status bar is one line for the whole window.
Worklist writes "Worklist is clear." when it loads, and Search
does not write a line until Search or Clear is pressed. After
opening Search, the bar still said the worklist was clear. A
worklist refresh from another screen could also replace that
screen's line.

**Bounded improvement:** Opening a screen writes a line for that
screen. Search with no completed search says "Search by antigen
profile." A finished search, including "Positive cells are
homozygous," is shown again when Search is opened. Worklist
refreshes while another screen is open no longer replace that
screen's line. Grades and the stored typing are unchanged.
Nothing is identified.

**Not in this tick:** Rule-out Explain lines name the panel and
cell, and omit the run. Authenticated vendor portals stay later
and must not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 82 (this change)

**Deficiency:** Explain listed a rule-out cell as the panel, the
cell number, and homozygous or heterozygous. The run was already
stored, and the Ruled Out grid already showed it, but the
Explain sentence did not. On a specimen with an untreated run and
a ficin run, a nonreactive cell could be read as coming from
either run.

**Bounded improvement:** The Explain sentence names the run with
the zygosity, in the same shape as a supporting cell. A
nonreactive E+ cell on the ficin run is cited as that run, and
the untreated run is not named on that rule-out cell. The
rule-out count is unchanged. Grades and the stored typing are
unchanged. Nothing is identified.

**Not in this tick:** A cell that blocks rule-out is still cited
as "cell N (antigen-positive and reactive)" without the run.
Authenticated vendor portals stay later and must not bypass auth,
CAPTCHA, licensing, or terms.

## Iteration 83 (this change)

**Deficiency:** Explain cited a cell that blocks rule-out as
"cell N (antigen-positive and reactive)." The run was known when
the cell was collected, and a rule-out cell already named its
run, but this sentence did not. On a specimen with an untreated
run and a ficin run, a reactive cell could not be told from a
cell on the other run.

**Bounded improvement:** The sentence names the panel, the cell,
and the run, in the same shape as a rule-out cell. A reactive E+
cell on the untreated run is cited on that run, and the ficin
run is not named on that blocking cell. The rule-out count is
unchanged. Grades and the stored typing are unchanged. Nothing
is identified.

**Not in this tick:** The Ruled Out cell grid shows IS, 37°C, and
AHG, and omits CC. Authenticated vendor portals stay later and
must not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 84 (this change)

**Deficiency:** The Ruled Out cell grid showed IS, 37°C, and AHG
and left out the check cell. The grade was already stored on the
rule-out cell, and the row already carried it, but the column
was not on the grid. A nonreactive cell could not be checked for
a valid antiglobulin control from that list.

**Bounded improvement:** The lower grid shows CC beside AHG. A
ficin nonreactive E+ cell keeps the check-cell grade from that
run, and the untreated reactive cell is not listed as a rule-out
cell. The rule-out count is unchanged. Grades and the stored
typing are unchanged. Nothing is identified.

**Not in this tick:** Supporting cells and conflicting cells on
Suspected Antibodies show IS, 37°C, and AHG, and omit CC.
Authenticated vendor portals stay later and must not bypass auth,
CAPTCHA, licensing, or terms.

## Iteration 85 (this change)

**Deficiency:** Supporting cells and conflicting cells on
Suspected Antibodies showed IS, 37°C, and AHG and left out the
check cell. The grade was already stored on the evidence cell,
and the row already carried it, but neither grid had the column.
A reactive cell could not be checked for a check-cell grade from
that list.

**Bounded improvement:** Both grids show CC beside AHG. An
antigen-positive cell that reacted at AHG keeps the stored check
cell, including NT when no check cell was recorded. An
antigen-negative cell that reacted at immediate spin keeps the
check-cell grade from that run, and it is not listed as a
supporting cell. The score is unchanged. Grades and the stored
typing are unchanged. Nothing is identified.

**Not in this tick:** Explain cites a supporting cell by its
strongest phase and leaves out the check cell. Authenticated
vendor portals stay later and must not bypass auth, CAPTCHA,
licensing, or terms.

## Iteration 86 (this change)

**Deficiency:** Explain cited a supporting cell by the panel, the
cell, the run, and the strongest phase. The check-cell grade was
already stored on that cell, and the Suspected Antibodies grid
already showed it, but the sentence did not. A reactive cell
recorded as CC NT could be read as though a check cell had been
performed.

**Bounded improvement:** The sentence names the check cell with
the strongest phase. An antigen-positive cell that reacted at
AHG is cited with CC NT when that is the stored grade. An
antigen-negative cell that reacted at immediate spin is cited
with the check-cell grade from that run, and it is not listed as
a supporting cell. The score is unchanged. Grades and the stored
typing are unchanged. Nothing is identified.

**Not in this tick:** Explain cites a rule-out cell with the run
and zygosity and leaves out the check cell. Authenticated vendor
portals stay later and must not bypass auth, CAPTCHA, licensing,
or terms.

## Iteration 87 (this change)

**Deficiency:** Explain cited a rule-out cell by the panel, the
cell, the run, and homozygous or heterozygous. The check-cell
grade was already stored on that cell, and the Ruled Out grid
already showed it, but the sentence did not. A nonreactive AHG
could be read as an unchecked negative.

**Bounded improvement:** The sentence names the check cell with
the run and the zygosity. A ficin nonreactive E+ cell is cited
as that run with CC 2+, and the untreated reactive cell is not
listed as a rule-out cell. The rule-out count is unchanged.
Grades and the stored typing are unchanged. Nothing is
identified.

**Not in this tick:** Explain cites a cell that blocks rule-out
by the run and leaves out the strongest phase and the check
cell. Authenticated vendor portals stay later and must not
bypass auth, CAPTCHA, licensing, or terms.

## Iteration 88 (this change)

**Deficiency:** Explain cited a cell that blocks rule-out by the
panel, the cell, and the run. The strongest phase and the check
cell were known when the cell was collected, and a supporting
cell already named both, but this sentence did not. A reactive
cell recorded as CC NT could not be told from one with a
performed check cell.

**Bounded improvement:** The sentence names the strongest phase
and the check cell with the run. An untreated E+ cell that
reacted at AHG is cited on that run as AHG 3+ with CC NT, and
the ficin run is not named on that blocking cell. The rule-out
count is unchanged. Grades and the stored typing are unchanged.
Nothing is identified.

**Not in this tick:** Explain cites a rule-out cell with the run,
zygosity, and check cell and leaves out the nonreactive phase
grades. Authenticated vendor portals stay later and must not
bypass auth, CAPTCHA, licensing, or terms.

## Iteration 89 (this change)

**Deficiency:** Explain cited a rule-out cell by the panel, the
cell, the run, homozygous or heterozygous, and the check cell.
The nonreactive phase grades were already stored on that cell,
and the Ruled Out grid already showed them, but the sentence did
not. A reader could not see whether the cell was negative at IS,
37°C, and AHG without leaving Explain.

**Bounded improvement:** The sentence names IS, 37°C, and AHG
with the run, the zygosity, and the check cell. A ficin
nonreactive E+ cell is cited on that run as IS 0, 37°C 0, AHG 0,
CC 2+, and the untreated reactive cell is not listed as a
rule-out cell. The rule-out count is unchanged. Grades and the
stored typing are unchanged. Nothing is identified.

**Not in this tick:** An invalid check cell is pink on Reactions
and plain text on the Ruled Out cell grid. Authenticated vendor
portals stay later and must not bypass auth, CAPTCHA, licensing,
or terms.

## Iteration 90 (this change)

**Deficiency:** A check cell that did not react when AHG is 0 is
pink on Reactions, with the tooltip that check cells should react
when AHG is 0. The same cell, when it counted toward a rule-out,
was ordinary text on the Ruled Out cell grid. A controlled
negative (CC 2+) and a failed check cell (CC 0) looked the same
there.

**Bounded improvement:** The Ruled Out CC cell uses that same pink
and tooltip when AHG is 0 and the check cell did not react. A
reacting check cell stays ordinary text. E2E-CC-INVALID keeps a
homozygous K+ cell at AHG 0, CC 0 beside a homozygous K+ cell at
AHG 0, CC 2+. The rule-out count is unchanged. Grades and the
stored typing are unchanged. Nothing is identified.

**Not in this tick:** Explain cites CC 0 on a rule-out cell and
does not say the check cell failed to react. Authenticated vendor
portals stay later and must not bypass auth, CAPTCHA, licensing,
or terms.

## Iteration 91 (this change)

**Deficiency:** Explain cited a rule-out cell at AHG 0, CC 0 with
the same shape as a controlled negative. The grade was visible,
and the Ruled Out grid already paints that CC cell pink, but the
sentence did not say the check cell failed to react.

**Bounded improvement:** A rule-out cell at AHG 0 and CC 0 is
cited with the phases and the words "check cell did not react."
A rule-out cell at AHG 0 and CC 2+ keeps the grade and does not
add that phrase. The rule-out count is unchanged. Grades and the
stored typing are unchanged. Nothing is identified.

**Not in this tick:** The Ruled Out explanation lists the failed
check-cell cell with the controlled cell and does not say the
check cell failed. Authenticated vendor portals stay later and
must not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 92 (this change)

**Deficiency:** The Ruled Out explanation listed every counted
cell together. On a workup with one failed check cell and one
controlled cell, both cell numbers appeared and the sentence did
not say which check cell failed. Explain already said that for
the cell citation.

**Bounded improvement:** When a counted cell is AHG 0 and CC 0,
the explanation names that cell and says the check cell did not
react. A counted cell at CC 2+ stays in the cell list without
that phrase. The rule-out count is unchanged. Grades and the
stored typing are unchanged. Nothing is identified.

**Not in this tick:** The Specimens ruled-out list shows anti-K
with a count of 2 and does not say a counted cell's check cell
failed. Authenticated vendor portals stay later and must not
bypass auth, CAPTCHA, licensing, or terms.

## Iteration 93 (this change)

**Deficiency:** The Specimens ruled-out list showed anti-K with a
count of 2. The Ruled Out explanation already named cell 1 as the
cell whose check cell did not react, but this list did not.

**Bounded improvement:** The Check cell column names that cell.
A ruled-out row whose counted cells are CC 2+ leaves the column
blank. The rule-out count is unchanged. Grades and the stored
typing are unchanged. Nothing is identified.

**Not in this tick:** The clinical identification report lists
anti-K (x2) and does not say a counted cell's check cell failed.
Authenticated vendor portals stay later and must not bypass auth,
CAPTCHA, licensing, or terms.

## Iteration 94 (this change)

**Deficiency:** Specimen Summary and Analysis Results listed
anti-K as (x2). Specimens and the Ruled Out explanation already
named cell 1 as the cell whose check cell did not react. Clinical
Identification already includes that sentence. These two reports
did not.

**Bounded improvement:** A counted cell at AHG 0 and CC 0 is named
on the ruled-out count line. A counted cell at CC 2+ keeps the
count only. The rule-out count is unchanged. Grades and the
stored typing are unchanged. Nothing is identified.

**Not in this tick:** Specimen Summary CSV exports the stored
rule-out count and does not include the check-cell note.
Authenticated vendor portals stay later and must not bypass auth,
CAPTCHA, licensing, or terms.

## Iteration 95 (this change)

**Deficiency:** Specimen Summary CSV wrote the stored rule-out
count. The preview, Specimens, and the Ruled Out explanation
already named cell 1 as the cell whose check cell did not react.
The spreadsheet did not, and a stored row has no place for that
note.

**Bounded improvement:** The spreadsheet uses the same current
ruled-out list as the preview. A counted cell at AHG 0 and CC 0
is named in the CheckCell column. A counted cell at CC 2+ leaves
that column blank. The rule-out count is unchanged. Grades and
the stored typing are unchanged. Nothing is identified.

**Not in this tick:** The Reports status line stays on Report:
All Specimens after the preview changes to Clinical Identification.
Authenticated vendor portals stay later and must not bypass auth,
CAPTCHA, licensing, or terms.

## Iteration 96 (this change)

**Deficiency:** Opening Reports set the status line to Report:
All Specimens. Changing the preview to Specimen Summary or
Clinical Identification left that line in place.

**Bounded improvement:** Changing the report while the Reports
tab is open replaces the status line with the report now on
screen. A report change while another tab is open does not
overwrite that tab's status. Grades and the stored typing are
unchanged. Nothing is identified.

**Not in this tick:** Changing the Analytics date range leaves
the status line on the range that was current when the tab
opened. Authenticated vendor portals stay later and must not
bypass auth, CAPTCHA, licensing, or terms.

## Iteration 97 (this change)

**Deficiency:** Opening Analytics set the status line to the
default 12-month range. Choosing All time or another preset
changed the charts and left that status line in place.

**Bounded improvement:** Changing the range while the Analytics
tab is open replaces the status line with that range and the
confirmed-specimen count. A range change while another tab is
open does not overwrite that tab's status. Confirmed
identifications are unchanged. Nothing is identified.

**Not in this tick:** Selecting another specimen leaves the
Specimens status line on the accession that was selected when
the tab opened. Authenticated vendor portals stay later and
must not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 98 (this change)

**Deficiency:** Opening Specimens set the status line to the
accession that was selected at that moment. Choosing another
specimen changed the detail pane and left that status line in
place.

**Bounded improvement:** Selecting a different accession while
the Specimens tab is open replaces the status line with that
accession. A selection change while another tab is open does not
overwrite that tab's status. An analysis that cannot be read
keeps its error line. Grades and the stored typing are
unchanged. Nothing is identified.

**Not in this tick:** Selecting another panel leaves the Panels
status line on the lot that was selected when the tab opened.
Authenticated vendor portals stay later and must not bypass
auth, CAPTCHA, licensing, or terms.

## Iteration 99 (this change)

**Deficiency:** Opening Panels set the status line to the lot
that was selected at that moment. Choosing another lot changed
the antigram and left that status line in place.

**Bounded improvement:** Selecting a different lot while the
Panels tab is open replaces the status line with that panel and
its lot. A selection change while another tab is open does not
overwrite that tab's status. Trying to change lots while antigen
edits are open keeps the save-or-cancel line. Grades and the
stored typing are unchanged. Nothing is identified.

**Not in this tick:** Choosing another run on Reactions leaves
the status line on the run that was current when the tab opened.
Authenticated vendor portals stay later and must not bypass
auth, CAPTCHA, licensing, or terms.

## Iteration 100 (this change)

**Deficiency:** Opening Reactions set the status line to
Reactions for the untreated run. Loading that run again, or
another run, replaced the line with "Loaded N cells" instead of
keeping the same sentence for the run now on screen.

**Bounded improvement:** Loading grades while the Reactions tab
is open sets the status line to the accession, panel, and run
on screen, with the cell count. A load while another tab is
open does not overwrite that tab's status. Grades and the
stored typing are unchanged. Nothing is identified.

**Not in this tick:** Selecting another specimen on Analysis
leaves the status line on the accession that was selected when
the tab opened. Authenticated vendor portals stay later and
must not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 101 (this change)

**Deficiency:** Opening Analysis set the status line to the
accession that was selected when the tab opened. Choosing
another specimen reloaded that accession and left the status
line on the first one.

**Bounded improvement:** Selecting a specimen while the Analysis
tab is open sets the status line to that accession, including
whether it is unanalyzed, stale, or the suspected and ruled-out
counts. A selection while another tab is open does not overwrite
that tab's status. A reload that fails does not replace the
status with an empty count. Run Analysis still writes its own
completion line. Grades and the stored typing are unchanged.
Nothing is identified.

**Not in this tick:** Selecting another rule leaves the status
line on the rule count from when the tab opened. Authenticated
vendor portals stay later and must not bypass auth, CAPTCHA,
licensing, or terms.

## Iteration 102 (this change)

**Deficiency:** Opening Rules set the status line to the rule
count. Choosing another rule left that count in place and did
not name the rule on screen.

**Bounded improvement:** Selecting a rule while the Rules tab is
open sets the status line to the rule count and that rule's name.
Opening Rules names the rule that is already selected. A selection
while another tab is open does not overwrite that tab's status.
Adding, editing, or deleting a rule still leaves its own
confirmation on the status line. Rule settings are unchanged.
Nothing is identified.

**Not in this tick:** Selecting a search result leaves the status
line on the search count. Authenticated vendor portals stay later
and must not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 103 (this change)

**Deficiency:** A finished search set the status line to the
matching-cell count. Choosing a result row left that count in
place and did not name the panel, lot, and cell on screen.

**Bounded improvement:** Selecting a result while the Search tab
is open adds that panel, lot, and cell to the status line after
the count. Opening Search names a result that is already
selected. A selection while another tab is open does not
overwrite that tab's status. Running Search or Clear still
leaves its own line, without a selected cell. Grades and the
stored typing are unchanged. Nothing is identified.

**Not in this tick:** A search result does not open that panel.
Authenticated vendor portals stay later and must not bypass
auth, CAPTCHA, licensing, or terms.

## Iteration 104 (this change)

**Deficiency:** A search result named its panel, lot, and cell
and left the user on Search. Nothing opened that panel.

**Bounded improvement:** Double-clicking a result, or selecting
it and pressing Enter or Open, switches to Panels and selects
that lot. The Panels status line names the panel and lot. Grades
and the stored typing are unchanged. Nothing is identified.

**Not in this tick:** Opening a search result does not select
that cell on the panel. Authenticated vendor portals stay later
and must not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 105 (this change)

**Deficiency:** Opening a search result showed the panel and
left every donor cell unmarked. The status line named the lot
and did not name the cell from the search.

**Bounded improvement:** Opening a result highlights that cell
on the antigen grid and adds the cell number to the Panels
status line. Choosing another lot clears the highlight and
returns the status line to the lot alone. Antigen edits that
are still open keep the save-or-cancel line. Grades and the
stored typing are unchanged. Nothing is identified.

**Not in this tick:** The opened cell does not bring the antigen
that matched the search into view. Authenticated vendor portals
stay later and must not bypass auth, CAPTCHA, licensing, or
terms.

## Iteration 106 (this change)

**Deficiency:** Opening a search result highlighted the cell and
left the antigen grid at the left. A match on a later antigen,
such as Lub, stayed off the screen.

**Bounded improvement:** Opening a result scrolls the antigen
used in the search into view on that cell. The cell number stays
on screen. Choosing another lot clears the highlight. When
several antigens are set, the first of those is the one brought
into view. Grades and the stored typing are unchanged. Nothing
is identified.

**Not in this tick:** The search results grid still starts at D,
so that antigen can sit off the result row. Authenticated vendor
portals stay later and must not bypass auth, CAPTCHA, licensing,
or terms.

## Iteration 107 (this change)

**Deficiency:** A finished search left the results grid at D.
The antigen used in the search, such as Lub, sat off the result
row. Panel, lot, and cell were already fixed on the left.

**Bounded improvement:** A finished search scrolls that antigen
into view on the result row. Panel, lot, and cell stay on
screen. Clearing the search returns the grid to D. When several
antigens are set, the first of those is the one brought into
view. Grades and the stored typing are unchanged. Nothing is
identified.

**Not in this tick:** When several antigens are set, only the
first is brought into view, so a later one can still sit off
the result row. Authenticated vendor portals stay later and
must not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 108 (this change)

**Deficiency:** A finished search with several antigens scrolled
the results grid to the first of those. A later antigen, such as
P1 after D, stayed off the result row.

**Bounded improvement:** A finished search scrolls the last
antigen used in the search into view on the result row. Panel,
lot, and cell stay on screen. A search with one antigen still
brings that antigen into view. Opening the result still brings
the first antigen into view on the panel. Grades and the stored
typing are unchanged. Nothing is identified.

**Not in this tick:** Opening a result still brings only the
first antigen into view on the panel, so a later one can sit
off that grid. Authenticated vendor portals stay later and
must not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 109 (this change)

**Deficiency:** Opening a search result scrolled the panel to
the first antigen in the search. A later antigen, such as P1
after D, stayed off that grid.

**Bounded improvement:** Opening a result scrolls the last
antigen used in the search into view on that cell. The cell
stays highlighted, and the status line names the cell. A
search with one antigen still brings that antigen into view.
The columns between the first and last antigen still fill the
grid. Grades and the stored typing are unchanged. Nothing is
identified.

**Not in this tick:** The columns between the first and last
antigen still fill the panel, so the earlier antigen can leave
the screen. Authenticated vendor portals stay later and must
not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 110 (this change)

**Deficiency:** Opening a search result brought the last
antigen into view and left the columns between it and the
first antigen in place. On Bio-Rad ID-DiaPanel, D left the
screen when P1 came into view.

**Bounded improvement:** Opening a result with several antigens
shows those columns first, in search order, so the earlier one
stays beside the later one. The cell stays highlighted, and the
status line names the cell. A search with one antigen keeps the
panel order and still brings that antigen into view. Choosing
another lot restores the panel's own column order. Edit still
shows the panel order, including all-NT columns. Grades and the
stored typing are unchanged. Nothing is identified.

**Not in this tick:** The search results row still fills with
the columns between the first and last antigen, so the earlier
antigen can leave that row. Authenticated vendor portals stay
later and must not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 111 (this change)

**Deficiency:** A finished search brought the last antigen into
view on the result row and left the columns between it and the
first antigen in place. On a D+ and P1+ search, D left the row
when P1 came into view. Panel, lot, and cell stayed on screen.

**Bounded improvement:** A finished search with several antigens
shows those columns first, in search order, so the earlier one
stays beside the later one. Panel, lot, and cell stay on screen.
A search with one antigen keeps the usual column order and still
brings that antigen into view. Clearing the search restores the
usual column order. Grades and the stored typing are unchanged.
Nothing is identified.

**Not in this tick:** A result row still shows a warehouse
antigen column, such as Kna, that is blank on every matching
cell. Authenticated vendor portals stay later and must not
bypass auth, CAPTCHA, licensing, or terms.

## Iteration 112 (this change)

**Deficiency:** A finished search kept every warehouse antigen
column. On a D+ and P1+ search, Kna stayed on the result row
even though it was blank on all 109 matching cells. Dia was the
only warehouse antigen those cells typed.

**Bounded improvement:** A finished search leaves off a warehouse
column that is blank on every matching cell. Dia stays when a
matching cell types it. Panel, lot, and cell stay on screen, and
several search antigens still lead the row. A search with no
matching cells, or Clear, shows the warehouse columns again.
Grades and the stored typing are unchanged. Nothing is identified.

**Not in this tick:** An untyped cell in a warehouse column that
stays, such as Dia, is blank rather than NT. Authenticated vendor
portals stay later and must not bypass auth, CAPTCHA, licensing,
or terms.

## Iteration 113 (this change)

**Deficiency:** A warehouse column that stayed, because some
matching cell typed it, showed a blank on a cell that was not
typed. On a D+ and P1+ search, Dia stayed and the ALBAcyte
cells were blank in that column, while an untyped standard
antigen such as Xga showed NT.

**Bounded improvement:** An untyped cell in a warehouse column
that stays shows NT, the same as an untyped standard antigen.
A typed +, ++, or minus stays. A warehouse column that is NT
on every matching cell, such as Kna, is still left off. Grades
and the stored typing are unchanged. Nothing is identified.

**Not in this tick:** Scrolling the result row to a later
column, such as Dia, still pushes the search antigens off that
row. Panel, lot, and cell stay. Authenticated vendor portals
stay later and must not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 114 (this change)

**Deficiency:** Scrolling the result row to a later column, such
as Dia, pushed the search antigens off that row. On a D+ and P1+
search the visible antigens started at E. Panel, lot, and cell
stayed.

**Bounded improvement:** When several antigens are set, those
columns stay on screen with the panel, lot, and cell when the
row is scrolled. One antigen, or Clear, leaves only the panel,
lot, and cell frozen and restores the usual column order. Grades
and the stored typing are unchanged. Nothing is identified.

**Not in this tick:** Opening that result shows the search
antigens first on the panel, and scrolling that panel still
pushes them off while the cell stays. Authenticated vendor
portals stay later and must not bypass auth, CAPTCHA, licensing,
or terms.

## Iteration 115 (this change)

**Deficiency:** Opening a D+ and P1+ result showed those antigens
first on the panel, and scrolling that panel pushed them off.
The cell column stayed.

**Bounded improvement:** When several antigens are set, those
columns stay on screen with the cell when the panel is scrolled.
One antigen, choosing another lot, or Edit leaves only the cell
frozen and restores that screen's own column order. Grades and
the stored typing are unchanged. Nothing is identified.

**Not in this tick:** Bio-Rad ID-DiaPanel was already in the
visible part of the panel list and was highlighted there, so
this screen does not show whether a lot that starts further
down the list is brought into view. Authenticated vendor
portals stay later and must not bypass auth, CAPTCHA, licensing,
or terms.

## Iteration 116 (this change)

**Deficiency:** Opening a D+ and P1+ result selected the lot, and
the panel list left it where it was. Grifols DG Gel ID-Panels
is row 37 of the active list, below the first screen, so the
opened lot stayed out of view.

**Bounded improvement:** Opening a search result brings that lot
into view in the panel list, including when the same lot is
opened again. The row shows the panel name, lot, cell count,
and expiration. Grades and the stored typing are unchanged.
Nothing is identified.

**Not in this tick:** Grifols landed on the last fully visible
row. The following lot, Immucor CAPTURE-R Ready-ID, is only a
sliver. This screen does not show whether the last lot in the
list, Quotient BioResearch MicroTyping, is fully visible.
Authenticated vendor portals stay later and must not bypass
auth, CAPTCHA, licensing, or terms.

## Iteration 117 (this change)

**Deficiency:** Opening a D+ and P1+ result for the last lot
left the panel list where it was. Quotient BioResearch
MicroTyping is the last active lot, below Grifols, so that row
stayed out of view. Scrolling the row into view at the first
layout pass did not move a list that was already scrolled.

**Bounded improvement:** After that layout, the list scrolls to
the opened lot, including when it is the last lot. The row
shows the panel name, lot, cell count, and expiration, and the
row is fully on screen. Grades and the stored typing are
unchanged. Nothing is identified.

**Not in this tick:** The name column shortens Quotient
BioResearch MicroTyping to Quotient BioResearch MicroT....
The lot QUO2026D, 8 cells, expiration 2027-04-16, and the
Active check are fully visible, and the status names the full
panel. Authenticated vendor portals stay later and must not
bypass auth, CAPTCHA, licensing, or terms.

## Iteration 118 (this change)

**Deficiency:** The panel list shortened a long name. Quotient
BioResearch MicroTyping showed as Quotient BioResearch MicroT...,
and the longest name, E2E panreactive negative autocontrol,
was cut the same way. The lot, cell count, and expiration were
already fully visible.

**Bounded improvement:** The list is wide enough to show the
full name beside the lot, cell count, and expiration. A narrowed
list still shortens the name, and pointing at it shows the full
text. Grades and the stored typing are unchanged. Nothing is
identified.

**Not in this tick:** This screen is the panel list. It does not
show the search result row, where a long panel name is still
shortened. Authenticated vendor portals stay later and must not
bypass auth, CAPTCHA, licensing, or terms.

## Iteration 119 (this check)

**Checked:** An E+ and K+ search returns 12 cells. The result
row shows the longest panel name, E2E panreactive negative
autocontrol, in full, with lot E2E-PAN-AC-NEG. Quotient
BioResearch MicroTyping and the longest lot, E2E-WARM-AUTO-ALLOS,
are also in full. The status is "Found 12 matching cells for 2
criteria." No code change.

**Not in this tick:** The search help still says a long name on
the result row is shortened. This screen shows the name in full.
Authenticated vendor portals stay later and must not bypass
auth, CAPTCHA, licensing, or terms.

## Iteration 120 (this change)

**Deficiency:** The Search help still said a long result name
is shortened, after the result row already showed E2E
panreactive negative autocontrol and Quotient BioResearch
MicroTyping in full.

**Bounded improvement:** User Manual — Search, under Run it,
now says the column headings stay visible and the panel name
and the lot are shown in full. Grades and the stored typing
are unchanged. Nothing is identified.

**Not in this tick:** The help window's Close button stayed
highlighted and the window stayed open after the click and
after Escape. The help window's title-bar control dismissed
it. Authenticated vendor portals stay later and must not
bypass auth, CAPTCHA, licensing, or terms.

## Iteration 121 (this change)

**Deficiency:** The Search help Close button left User Manual —
Search open. The button stayed highlighted after the click, and
Escape left the same window open. The help window's title-bar
control was what dismissed it.

**Bounded improvement:** Close dismisses the manual. Escape
dismisses it as well. The Search screen returns, and the status
stays "Search by antigen profile." Grades and the stored typing
are unchanged. Nothing is identified.

**Not in this tick:** This screen has no search criteria and
Results is 0. It does not show another shortened name or a
stuck help window. Authenticated vendor portals stay later and
must not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 122 (this change)

**Deficiency:** The All Panels report used a 25-character name
column. E2E panreactive negative autocontrol, E2E mixed cold
and warm, and E2E warm auto plus allos ran into the lot, so
the lot, vendor, cell count, and expiration no longer sat
under their headings.

**Bounded improvement:** Those columns follow the longest name,
lot, vendor, cell count, and expiration. On the All Panels
report, E2E panreactive negative autocontrol stays in the name
column with lot E2E-PAN-AC-NEG, vendor E2E, 6 cells, and
expiration 2027-11-17. E2E-MIXED-COLD-WARM and
E2E-WARM-AUTO-ALLOS stay in the lot column the same way. The
status is "Report: All Panels." Grades and the stored typing
are unchanged. Nothing is identified.

**Not in this tick:** The intended-use sentence still runs off
the right edge of the preview, ending at "confirm identifi".
Authenticated vendor portals stay later and must not bypass
auth, CAPTCHA, licensing, or terms.

## Iteration 123 (this change)

**Deficiency:** The intended-use sentence was one line, so the
All Panels preview cut it off at "confirm identifi". The rest,
including that the software does not issue or release blood
units, was off the right edge.

**Bounded improvement:** The statement wraps onto the preview.
All Specimens shows the product name and version, then the
full sentence through "licensed clinical judgment." The status
is "Report: All Specimens." Grades and the stored typing are
unchanged. Nothing is identified.

**Not in this tick:** The specimen list continues below the
window. Accession, type, status, created date, and expiration
stay in their columns on the rows that are on screen.
Authenticated vendor portals stay later and must not bypass
auth, CAPTCHA, licensing, or terms.

## Iteration 124 (this change)

**Deficiency:** The specimen list cut a long accession and the
expiration. E2E-WARM-AUTO-ALLOS showed as E2E-WARM-AUTO-AL,
E2E-MIXED-COLD-WARM showed as E2E-MIXED-COLD-W, and the
expiration column showed as Expir with the date cut at 2027-0.
Show Inactive was cut to Show In.

**Bounded improvement:** The list shows the full accession and
the full expiration. E2E-WARM-AUTO-ALLOS and
E2E-MIXED-COLD-WARM stay complete, each with expiration
2027-01-01, created 2026-11-17, and Active checked. Show
Inactive is fully visible. The status is "Specimens: 44 shown.
Selected E2E-CC-INVALID." The ruled-out row for anti-K still
says cell 1 check cell did not react. Grades and the stored
typing are unchanged. Nothing is identified.

**Not in this tick:** More specimens continue below
E2E-COMBO-JK. This screen does not show another cut-off
accession. Authenticated vendor portals stay later and must
not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 125 (this check)

**Checked:** Enter Reactions for E2E-CC-INVALID opens the
untreated run of E2E invalid check cell. Cell 1 is 0 at IS,
37°C, and AHG, with CC 0 in pink, K+, and anti-K ruled out.
Cells 2 and 3 are 0 at those phases with CC 2+. Cell 2 is K−
k+ and rules out anti-k. Cell 3 is K+ and rules out anti-K.
The autocontrol is 0 with CC 0 in pink. The status is
"Reactions for E2E-CC-INVALID / E2E invalid check cell
(Untreated): 4 cells." No code change.

**Not in this tick:** This screen is the grade grid. It does
not show the analysis explanation for these same cells.
Authenticated vendor portals stay later and must not bypass
auth, CAPTCHA, licensing, or terms.

## Iteration 126 (this check)

**Checked:** Analysis for E2E-CC-INVALID shows 0 suspected and
2 ruled out. Anti-k meets the rule-out on cell 2, homozygous,
untreated, 0 at IS, 37°C, and AHG, CC 2+. Anti-K meets it on
cells 1 and 3. Cell 1 is homozygous, untreated, 0 at those
phases, with CC 0 in pink, and the explanation says cell 1
check cell did not react. Cell 3 is the same phases with CC
2+. The status is "E2E-CC-INVALID: 0 suspected, 2 ruled out."
No code change.

**Not in this tick:** This is the second check in a row with
no in-scope issue. The 30-minute loop stops. Authenticated
vendor portals stay later and must not bypass auth, CAPTCHA,
licensing, or terms.

## Iteration 127 (this check)

**Checked:** Combinations for E2E-CC-INVALID is empty. The
caption says a pair is listed when the combination score
exceeds 50%. The headings Antibody Pair, Individual Scores,
Combination Score, Both +, Ab1 Only, Ab2 Only, and Neither
are on screen. This specimen has 0 suspected antibodies, and
a pair is scored only when at least two antibodies are
suspected. The status stays "E2E-CC-INVALID: 0 suspected, 2
ruled out." No code change.

**Not in this tick:** This screen does not show Pattern
Matches. The stop bar is now five consecutive checks with no
in-scope issue. Iterations 125, 126, and 127 are three of
those five. Authenticated vendor portals stay later and must
not bypass auth, CAPTCHA, licensing, or terms.

## Iteration 128 (this check)

**Checked:** Pattern Matches for E2E-CC-INVALID is empty. The
headings Antibody, Matches, Mismatches, and Confidence are on
screen. A row is added only for a positive reaction on a typed
cell, and ruled-out antibodies are left out of this list.
Every panel cell on this run is 0, so there is no pattern to
match. The status stays "E2E-CC-INVALID: 0 suspected, 2 ruled
out." No code change.

**Not in this tick:** This screen does not show Dosage.
Iterations 125 through 128 are four consecutive checks with
no in-scope issue. The loop stops only after five. Authenticated
vendor portals stay later and must not bypass auth, CAPTCHA,
licensing, or terms.

## Iteration 129 (this check)

**Checked:** Special Panels for E2E-CC-INVALID. Treatment
comparison, gated rule-outs, treatment inferences, and
absorption comparison are empty. This workup has one untreated
run and no enzyme, DTT, or adsorption run to compare. The
section sentences are on screen, including that scores are
evidence and not a diagnosis. The status stays
"E2E-CC-INVALID: 0 suspected, 2 ruled out." No code change.

**Not in this tick:** This is the fifth consecutive check with
no in-scope issue. The 30-minute loop stops. Authenticated
vendor portals stay later and must not bypass auth, CAPTCHA,
licensing, or terms.

## Loop stopped

Iterations 125 through 129 found no in-scope issue. The
invalid check-cell grades, the ruled-out explanation, and the
empty Combinations, Pattern Matches, and Special Panels
screens agree. The recurring check is stopped.

