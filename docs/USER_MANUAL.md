# Antibody Panel Management System

# User Manual

Version 2.0.0

## Intended use

Decision-support software for licensed immunohematology professionals to document antibody-panel reactions and compute rule-out and probability statistics. A qualified professional must confirm identification. This software does not issue or release blood units and is not a substitute for licensed clinical judgment.

This file is the same text as **Help → User Manual**. Chapters follow the topic list. Press **F1** to open the chapter for the screen you are on. **Save manual** writes this manual as PDF, Markdown, or text.

## Contents

- Getting started
- Worklist
- Specimens
    - Specimen record
    - Attach a panel
- Panels
    - Panel details
    - Download from vendor
    - Review an import
    - Import artifact
    - Compare panels
    - Warehouse antigens
- Reactions
    - Add a run
- Analysis
    - Explain
    - Suspected Antibodies
    - Ruled Out
    - Pattern Matches
    - Combinations
    - Dosage
    - Special Panels
    - Patient Typing
    - Selected Cells
    - Summary
    - Clear a confirmed ID
- Reports
- Analytics
- Search
- Rules
    - Rule editor
- Preferences
- Purge database
- Open archive
- Audit log
- Demo data
- Keyboard shortcuts

---

# Getting started

Antibody Panel Management System helps licensed immunohematology professionals document antibody-panel reactions and compute rule-out and probability statistics. A qualified professional must confirm identification. This software does not issue or release blood units and is not a substitute for licensed clinical judgment.

## How to open help

- Press **F1** to open help for the section you are in. On Analysis, F1 follows the inner tab that is selected.
- Click **Help** on a tab or dialog to open that section.
- Choose **Help → User Manual** to open this manual at the beginning.
- Choose **Help → Topics** to jump to any chapter.
- **Help → Keyboard Shortcuts** opens the shortcut list. **Help → About** shows the product name, version, and intended use.

The manual stays open while you work. Use **Find** to search titles and chapter text. **Print chapter** prints the page you are reading. **Print manual** prints every chapter in order. **Save manual** writes the whole manual, including the intended-use statement and the contents, as a PDF, a Markdown file, or plain text. **Close**, or Escape, dismisses the manual and leaves the screen you were on.

## A typical workup

1. Set **Tools → Preferences** for the lab name, identification cell rule, rule-out counts, and extra phases.
2. On **Panels**, add a lot or import one, review the antigram, and activate it.
3. On **Specimens**, add the accession and clinical context, then attach the panel.
4. On **Reactions**, enter grades and choose **Save & Analyze**.
5. On **Analysis**, review the evidence and **Confirm ID** with your initials.
6. On **Reports**, print or export the clinical identification report.

**Worklist** shows incomplete reactions, stale analyses, and specimens or panels that are expiring or expired. **Analytics** summarizes confirmed identifications. **Search** finds panel cells by antigen profile. **Rules** stores per-antibody rule-out overrides.

## Menus

- **File → Exit** closes the application after you confirm.
- **Edit → Save** (Ctrl+S) saves the antigen grid on Panels or the grades on Reactions.
- **Edit → Refresh All** (Ctrl+R) reloads every tab.
- **Edit → New Item** (Ctrl+N) adds a specimen, panel, or rule, depending on the tab.
- **Tools** opens Preferences, Purge Database, Open Archive, Audit Log, and Load Demo Data.

The first time the application starts, it shows the intended-use statement. The status bar at the bottom reports the result of the last action. Opening a screen replaces that line with a line for the screen you are looking at, so a worklist message does not stay on Search. Returning to Search shows the last search result again, including the homozygous or heterozygous wording.

---

# Worklist

The Worklist is the operational list for specimens that still need work and for specimens or panels that are near or past expiration.

## Count cards

Four cards sit across the top:

- **Incomplete** — reaction grids that are not finished, for specimens that do not yet have a confirmed antibody ID.
- **Stale analysis** — grades were saved after the last analysis, or Preferences that change analysis (score threshold, ID cell rule, ACS rule-outs, default rule-outs, or extra phases) were saved after the last analysis. A specimen that has a panel and has never been analyzed is listed here with the detail "Never analyzed." A confirmed identification is not listed.
- **Expiring** — specimens or panel lots inside the expiration warning window from Preferences.
- **Expired** — specimens or panel lots past their expiration date.

Click a card to show only that category. Click the same card again to show every category that is still checked. A thicker border means that card is the active filter.

## Filters

- **Find** narrows the grid by accession, panel name, kind, urgency, or detail.
- **Show all** clears the card filter.
- The **Show** checkboxes turn Incomplete, Stale analysis, Expiring, and Expired on or off together.
- **Open** goes to the selected row. Double-click a row, or press **Enter**, to do the same.
- **Refresh** reloads the counts and the grid.
- **Help** opens this chapter.

## What a row opens

- Incomplete reactions open the Reactions tab for that specimen.
- A stale analysis opens the Analysis tab.
- Specimen rows open Specimens.
- Panel rows open Panels.

Row colors match the category: amber for incomplete, orange for stale, yellow for expiring, and red for expired.

The worklist does not include a confirmed identification as unfinished work. After you confirm an ID, incomplete and stale rows for that specimen drop off.

---

# Specimens

The Specimens tab is where you keep accessions and the clinical context that analysis uses as evidence.

## List

The left pane lists accession number, type, final ID, expiration, created date, and whether the specimen is active. The accession and the expiration stay on screen in full.

- **Find** filters by accession, type, phenotype, previous antibodies, notes, DAT, or final ID.
- **Add** opens a blank specimen record. **Edit** opens the selected record. Double-click a row to edit it.
- **Delete** removes the selected specimen after you confirm.
- **Refresh** reloads the list.
- **Show Inactive** includes specimens that were marked inactive.
- **Help** opens this chapter.

Row colors: green when a final ID is confirmed (the tooltip shows the confirmed antibodies), yellow when the specimen is nearing expiration, and red when it is expired.

## Details

The right pane follows the selected specimen. Selecting a specimen updates the status line to name that accession.

- **Enter Reactions** opens Reactions for this accession.
- **Analyze** opens Analysis.
- **Clinical Report** opens Reports on the clinical identification report.

**Clinical Context** shows phenotype, genotype, previous antibodies, DAT, notes, and the confirmed final ID. These fields are edited from the specimen record.

**Linked Panels** are the lots used for this workup. **Attach Panel** picks a lot. **Detach** removes the selected link. Linking a panel makes it available when you enter reactions.

**Pattern evidence** appears when the latest analysis has a panreactive, autoantibody, or high-prevalence note, including a cold or warm phase sentence and an adsorption comparison when one was run. When that analysis left a panreactive untreated run out of ordinary scores, that exclusion is listed first, and the adsorption comparison follows it. Residual reactivity after autoadsorption is visible with the suspected antibodies, ahead of the warm and pan sentences. A specimen with no suspected specificity and no such note says so. The wording is evidence, not a diagnosis. An ordinary alloantibody score stays in Suspected Antibodies and does not add this block.

**Suspected Antibodies** and **Ruled-out Antibodies** follow the same current analysis as Pattern evidence. An older saved list is not shown in their place. A cell whose partner antigen is NT does not rule an antibody out. When nothing meets the criteria, the ruled-out pane says so. When a counted cell's check cell is 0, the Check cell column names that cell and says the check cell did not react. A counted cell at CC 2+ leaves that column blank. Change them by entering grades and running analysis, not by typing in these grids.

---

# Specimen record

Use this dialog to add or edit an accession. Open it with **Add** or **Edit** on the Specimens tab, or with Ctrl+N while Specimens is selected.

## Fields

- **Accession Number** identifies the specimen. Use the accession your laboratory already uses.
- **Type** is serum, plasma, or eluate. The default comes from Preferences.
- **Expiration Date** is the specimen dating date. Preferences can pre-fill it from the specimen-dating setting. Clear the date if this specimen should not expire.
- **DAT** is the direct antiglobulin test result: NT, Negative, W+, 1+, 2+, 3+, or 4+.
- **Phenotype** is the serologic type, for example R1r, K−, Fy(a−b+). If the patient was recently transfused, say so in Notes. Analysis will not use a recently transfused phenotype to exclude alloantibodies.
- **Genotype** is the predicted type from molecular results, for example RHCE*ce/ce. It stays available for review after transfusion. It is evidence for the reader, not a diagnosis.
- **Previous antibodies** records antibodies already known for this patient, for example anti-E.
- **Active** hides the specimen from the default lists when it is unchecked. Turn on **Show Inactive** to see it again.
- **Notes** holds free text such as transfusion history, medications, or why the workup was ordered.

**Save** writes the record. **Cancel** discards the changes. **Help** opens this chapter.

---

# Attach a panel

This dialog links a panel lot to the specimen you have selected.

1. Open **Specimens** and select the accession.
2. Click **Attach Panel**.
3. Use **Find** to narrow the list by name, lot, vendor, or expiration.
4. Select the lot. Each row shows the panel name, lot number, and expiration date.
5. Click **Attach**, or double-click the row.

The panel then appears under **Linked Panels** and can be chosen on the Reactions tab. **Detach** on the Specimens tab removes the link. Detaching does not delete the panel lot or any grades already stored.

**Cancel** closes the dialog without linking. **Help** opens this chapter.

---

# Panels

The Panels tab stores panel lots and the antigen profile of each cell. Analysis uses this grid together with the grades you enter.

## List

- **Find** filters by name, lot, vendor, or expiration date. Find is unavailable while you are editing antigens. The name and lot stay on screen in full. If the list is narrowed, a long name is shortened and the full name shows when you point at it.
- **Add** creates a lot. **Details** edits the name, lot, vendor, cell count, and expiration. **Copy** duplicates a lot so you can start a new lot from an existing antigram.
- **Edit** turns on antigen editing for the selected panel. **Delete** removes the lot after you confirm.
- **Show Inactive** includes lots that are not active. Imported lots stay inactive until you review them.
- Yellow rows are nearing expiration. Red rows are expired.
- Selecting a lot updates the status line to name that panel and its lot. Opening a cell from Search brings that lot fully into view in the list, including when it is the last lot, names that cell, highlights it on the antigen grid, and brings the antigen from the search into view. When several antigens are set, those columns are shown first and stay on screen with the cell when that panel is scrolled, so the earlier one stays beside the later one. Choosing another lot restores the panel's own column order.

## Bring a lot in

- **Import** creates a panel from a CSV lot sheet. The file uses a header row such as Cell, D, C, c, and the rest of the antigens.
- **Download…** opens the vendor catalog, or lets you import a vendor PDF or CSV you already have.
- After an import, the review dialog lets you **Activate now** or **Keep inactive**. You can also select an inactive lot later and click **Activate**.
- **Artifact** opens the retained import file and its SHA-256 hash.
- **Export** saves the selected antigen grid as CSV. **Antigram** saves a bench-copy PDF that leaves out an antigen column that is NT on every cell. A cell that is NT in a shown column prints NT. A typed minus stays a minus.

## Antigen grid

The grid on the right is the antigram. Press **Edit** before changing it.

- Click a cell to cycle **NT → + → ++ → −**. Antigens without an antithetical pair skip ++.
- A new cell starts as NT. Green is + or ++. Light gray is a typed −. NT means the antigen was not typed, which is not the same as negative.
- An antigen column that is NT on every cell is hidden. Press **Edit** to show those columns and type them. Save or Cancel hides them again if they are still NT.
- Drag column headers to reorder while editing.
- **Save** writes the grid (Ctrl+S does the same on this tab). **Cancel** leaves edit mode without saving.

**Add antigen…** adds a non-standard antigen from the warehouse catalog to this panel only. **Remove antigen…** drops one of those extra columns. **Compare…** diffs this antigram against another stored lot.

Antigen typing on the grid is the profile analysis uses. A blank or NT antigen is not treated as negative.

---

# Panel details

This dialog creates a lot, edits its details, or copies an existing lot. Antigen reactions for each cell are edited on the Panels grid after you save the details.

## Fields

- **Panel Name** is the name shown in lists.
- **Lot Number** and **Vendor** identify the manufacturer lot.
- **Number of Cells** is how many donor cells the panel contains. The usual screen-and-panel size is 16, but enter the count printed on the lot sheet.
- **Starting Cell #** is the first cell number. Enter 12 if this panel begins at cell 12.
- **Expiration Date** drives the expiring and expired warnings on the Worklist and Panels list.
- **Include AC** adds an autocontrol row.
- **Active** makes the lot available for new work. Leave it unchecked until an imported antigram has been reviewed.
- **Catalog #** and **Product line** describe the vendor product.
- **Enzyme treated** marks a lot that was manufactured as an enzyme-treated panel.

Imported lots may show a short note about the retained source file. Open **Artifact** on the Panels tab to see the hash and preview.

**Save** stores the details. **Cancel** closes without saving. **Help** opens this chapter.

---

# Download from vendor

Open this dialog with **Download…** on the Panels tab. It can refresh a public vendor lot catalog or import a PDF or CSV you already downloaded.

## Catalog download

1. Choose a **Vendor**.
2. Click **Refresh lots**. The application contacts that vendor’s published catalog. It does not accept an address you type in.
3. Select a row. Columns are product, lot, expiration, catalog number, and whether the lot is enzyme-treated.
4. Click **Import selected**.

The status line under the vendor list tells you whether the catalog loaded. If a vendor has no live catalog, use **Import vendor file…** instead.

## File you already have

**Import vendor file…** asks for a vendor PDF or CSV. The same review step follows either path: the new lot stays inactive until you activate it, and the source file is kept so you can open it from **Artifact**.

**Close** leaves the dialog. **Help** opens this chapter.

A downloaded antigram is still source material. Compare it with the lot sheet before you activate the panel and use it for a workup.

---

# Review an import

Imported panels stay inactive until a qualified user reviews them. This dialog opens after **Import**, **Download…**, or **Activate** on a lot that still needs review.

The text box summarizes what was read: panel name, lot, vendor, cell count, and any parse notes. Read it against the manufacturer antigram.

- **Activate now** marks the lot active so it can be attached to specimens and used for reaction entry.
- **Keep inactive** saves the lot without activating it. It remains on the Panels list when **Show Inactive** is checked. You can activate it later with **Activate**.

Scores and rule-outs elsewhere in the application are analytical evidence, not a diagnosis. Activate a lot only after you confirm the antigram and the lot metadata.

**Help** opens this chapter.

---

# Import artifact

**Artifact** on the Panels tab opens the file that was kept when the selected lot was imported.

- The status text says whether the stored file is still present.
- **SHA-256** is the hash recorded at import. **Copy hash** puts it on the clipboard so you can compare it with a hash you compute yourself.
- **Path** is where the file was stored on this computer.
- **Preview** shows the contents when the file is text, such as CSV. PDF lots do not show a text preview; use **Open file** for those.
- **Open file** launches the file in the application Windows associates with it. **Open folder** opens the directory that contains it.

This view is the evidence of what was imported. It does not change the antigen grid. If the file is missing, the hash and path are still shown so you can see what was recorded.

**Close** returns to Panels. **Help** opens this chapter.

---

# Compare panels

**Compare…** on the Panels tab diffs the selected lot with another lot stored in the database.

1. The dialog titles the lot you started from.
2. Choose the other lot under **Compare with**.
3. The explanation summarizes how the two antigrams differ.
4. The grid lists each cell and antigen that does not match. **Selected** is the lot you started from. **Other** is the lot you picked. **Note** describes the difference.

Untyped antigens stay NT. An NT cell is not treated as negative, so a difference between NT and + is called out rather than hidden.

Use this when a new lot arrives and you want to see what changed from the previous lot, or when two imports of the same sheet should be identical. The comparison is a review aid. It does not change either panel and it is not an antibody identification.

**Close** returns to Panels. **Help** opens this chapter.

---

# Warehouse antigens

Some panels type antigens that are not part of the standard column set. **Add antigen…** on the Panels tab opens this list.

The prompt explains that the antigens you add become extra columns on this panel only. Antibody identification considers them only on panels that type them.

1. Select one or more antigens. The list shows the warehouse name for each antigen.
2. Click **Add**, or double-click a single antigen.
3. The new columns appear on the antigen grid. Press **Edit** and type each cell +, ++, −, or NT to match the lot sheet.

**Remove antigen…** opens the same style of list for columns that were added to this panel. Removing a column deletes that typing from this lot. It does not remove the antigen from the warehouse, and it does not change other panels.

**Cancel** closes the list without changing the panel. **Help** opens this chapter.

---

# Reactions

The Reactions tab is where you record IS, 37°C, AHG, and CC grades for each cell, plus any extra phases configured in Preferences.

## Choose the work

- **Find** filters the specimen list by accession, type, phenotype, previous antibodies, notes, or final ID.
- **Specimen** and **Panel** pick the accession and the lot. Grades load when you change specimen, panel, or run.
- **Run** selects which set of grades you are editing. The first run is the untreated panel. **+ Run** adds a treated or adsorbed run. **Delete Run** removes the run you are viewing. Changing the run updates the status line to name that run.

## While you enter grades

- **Compare** shows another run beside this one. Pick that run in the second list. Grade columns are named for the two runs. Rows that differ are highlighted, the Changed box is checked, and extra phases are included. The Changed box is display only.
- **Hide ruled-out** hides antigen columns already ruled out for this specimen. The setting can also be turned on by default in Preferences.
- **Show dosage** groups columns by blood-group system and marks homozygous ++, heterozygous +, or +/? when the antithetical antigen was not typed. This is display only. It does not change rule-out.
- **Load Reactions** reloads grades from the database.
- **Save** writes grades without running analysis. Ctrl+S does the same.
- **Save & Analyze** writes grades, runs analysis, and shows a green or red banner with the result.
- **Fill 0s** sets remaining NT phases to 0. Check cells become 2+ when AHG is 0. Grades you already entered are left alone.
- **Clear** removes the grades on this run.
- **Help** opens this chapter.

A blue banner names the cell or serum treatment when the run is not an ordinary untreated panel.

## The grid

Antigen columns come from the panel. A column that is NT on every cell is hidden. A typed minus stays. Red header text means that antibody is ruled out for this specimen. A strikethrough header means the antigen is destroyed by the cell treatment, so that column cannot support a rule-out on this run.

Row colors: blue when no grades have been entered, green when the row contributed to a rule-out. Phase cells use yellow for w+ or 1+, orange for 2+, red for 3+, 4+, or hemolysis, and purple for mixed field. A pink CC cell is invalid when AHG is 0.

## Keyboard

Click a phase cell, then type:

- **0–4** writes 0, 1+, 2+, 3+, or 4+.
- **W** writes w+. **M** writes MF. **H** writes hemolysis. **N** writes NT.
- **Enter** moves to the next phase, then to the next cell.

## Limit the rows you see

**Filter** shows only cells positive for one antigen. The autocontrol stays visible. Hidden cells are still saved and still analyzed.

When an antigen is selected, the zygosity list can limit the rows to homozygous or heterozygous expression. The sort list reorders the visible rows and does not change stored cell numbers. The text beside the lists explains the filter that is in effect.

w+, MF, and hemolysis count as reactive. They cannot be used to rule an antibody out. Extra-phase reactivity is recorded and shown, and it does not by itself rule out an antibody.

---

# Add a run

**+ Run** on the Reactions tab opens this dialog. A run is a second set of grades for the same specimen and panel, after a cell treatment or a serum treatment.

## Cell treatment

- **Untreated (None)** is an ordinary panel.
- **Ficin-treated** or **Papain-treated** is an enzyme-treated cell panel. Antigens destroyed by the enzyme are marked on the grid and are not used to rule antibodies out.
- **DTT-treated** is a dithiothreitol-treated cell panel. Kell-system antigens destroyed by DTT are handled the same way.

## Serum treatment

- **None** tests untreated serum.
- **Prewarmed serum** records a prewarmed test.
- **Absorbed: R1R1 (DCe/DCe)**, **Absorbed: R2R2 (DcE/DcE)**, and **Absorbed: rr (ce/ce)** are allogeneic adsorptions.
- **Autoadsorption** is an autoadsorbed aliquot.

The italic note under the lists explains what the selected combination will mean when you analyze.

## Label and grades

**Label** is optional. Leave it blank and the application names the run from the treatments you picked.

**Copy grades from the current run** starts the new run with the IS, 37°C, AHG, and CC grades already entered on the run you were viewing. Uncheck it to start from a blank grid. You can edit every grade after the run is created.

**Add Run** creates the run and selects it. **Cancel** leaves the runs unchanged. **Help** opens this chapter.

Compare the new run with the original from the Reactions tab by turning on **Compare**. Treatment inferences and adsorption findings appear on the Analysis tab under **Special Panels**.

---

# Analysis

Analysis computes rule-outs, scores, and pattern statistics from the grades and the antigram. The results are analytical evidence for a qualified professional. They are not a diagnosis, and the application does not release blood.

## Top of the tab

- **Find** filters specimens by accession, type, phenotype, previous antibodies, notes, or final ID.
- **Specimen** selects the accession to review. Selecting a specimen updates the status line to name that accession.
- An orange **Analysis is stale** warning means grades changed after the last run, or Preferences that change analysis were saved after that run. Click **Run Analysis** again before you rely on the saved result. A confirmed identification is not marked stale for a Preferences change.
- **Enter Reactions** jumps to the Reactions tab for this specimen.
- **Clinical Report** opens the clinical identification report.
- **Run Analysis** recalculates every inner tab.
- **Help** opens help for the inner tab you are viewing, or this overview if no inner topic is available.

When the engine has testing suggestions, they appear in the yellow **Suggestions** box under the toolbar. Read them as next steps, such as cells still worth testing.

## Inner tabs

- **Explain** lists every candidate with the evidence behind it.
- **Suspected Antibodies** shows scores and is where you enter and confirm the final ID.
- **Ruled Out** shows which antibodies met the rule-out count and which cells counted.
- **Pattern Matches**, **Combinations**, and **Dosage** are additional views of the same run.
- **Special Panels** covers enzyme and DTT rule-out gates, treatment inferences, and adsorption comparison.
- **Patient Typing** lines the candidates up against phenotype, genotype, and history.
- **Selected Cells** suggests unused panel cells that would separate the remaining candidates.
- **Summary** is a single text review of the run.

Press **F1** on this tab to open the chapter for the inner tab that is selected. Confirm an identification only from **Suspected Antibodies**, and only after you have reviewed the evidence.

---

# Explain

The Explain tab lists every antibody candidate that has reviewable evidence. Scores and rule-outs are analytical, not a diagnosis.

## Candidate grid

- **Antibody** is the specificity.
- **Status** summarizes where that candidate stands, such as suspected or ruled out.
- **Support** counts antigen-positive cells that reacted. Those cells support the candidate.
- **Blocks rule-out** counts antigen-positive cells that reacted, so they cannot be used to rule this antibody out. That column is not the same thing as **Conflicting cells (Ag− but reactive)** on the Suspected Antibodies tab. Conflicting cells are antigen-negative cells that still reacted.

Select a row to read the detail under the grid. The detail names the cells, the run, and the phases that produced the status, so you can check the arithmetic against the reaction grid. On a specimen with an untreated run and an autoadsorbed run, a supporting cell is cited on the run that supplied it. The citation includes the check-cell grade with the strongest phase, so a cell recorded as CC NT is not read as though a check cell was performed. A cell used for rule-out names the run, the zygosity, the nonreactive phase grades, and the check cell, so a nonreactive cell on a ficin run is not read as an untreated-run rule-out and a controlled AHG is not read as an unchecked negative. When that check cell is 0, the sentence says the check cell did not react. A check cell of 2+ does not. A cell that blocks rule-out names that run, the strongest phase, and the check cell, so a reactive cell is not cited as a bare cell number.

An antigen that is NT on every cell of the linked panel is described that way. It is not described as an antigen that was never imported. A typed antigen, including a typed minus, stays available for rule-out and support.

Use Explain when you need to see why a candidate is present before you look at the score table. To record a final identification, go to **Suspected Antibodies** and use **Confirm ID**.

---

# Suspected Antibodies

This tab lists antibodies whose combined score meets the threshold from Preferences, and it is where a qualified professional records the final identification.

## The score grid

- **Antibody**, **Score**, **Fisher p-value**, and **Pattern Score** describe the statistical support.
- **Ag+ reactive** is the number of antigen-positive cells that reacted.
- **Ag- nonreactive** is the number of antigen-negative cells that did not react.
- **ID rule** shows whether the row meets the identification cell rule from Preferences. The usual standard is 3 antigen-positive reactive cells and 3 antigen-negative nonreactive cells.

Green rows meet that rule. Yellow rows do not. Double-click a row to add it to **Final ID**. A green row is a candidate that meets the cell rule. A yellow row can still be added, and the tooltip says that it does not yet meet the rule.

## Supporting and conflicting cells

**Supporting cells** are antigen-positive cells that reacted, with the panel, run, cell number, phase grades, and the check cell.

**Conflicting cells (Ag− but reactive)** are cells that lack the antigen and still reacted. The same grades are shown, including the check cell. Conflicts mean another antibody, a typing discrepancy, or a reaction that the single specificity does not explain. Resolve them before you confirm.

## Final ID

- **Final ID** is the confirmed text, for example anti-E; anti-K. Separate specificities with a semicolon. Green rows are pre-filled when they meet the ID rule.
- **Add selected** appends the highlighted row.
- **Comment** is an optional note stored with the confirmation.
- **Initials** identifies who confirmed the ID. Preferences can pre-fill this, and a successful confirmation updates the default initials.
- **Confirm ID** locks the identification onto the specimen. Reports, Analytics, and the Worklist then treat the specimen as confirmed.
- **Clear** removes a confirmed ID. You must enter a reason. The reason is written to the audit log.

Confirm only after you agree with the evidence. The software does not confirm an identification for you.

---

# Ruled Out

This tab shows the rule-out count for each antibody and the cells that produced it.

## The count grid

- **Antibody** is the specificity.
- **Count** is how many qualifying cells ruled it out.
- **Needed** is the minimum required. That number comes from a rule on the Rules tab when one exists for the antibody, and otherwise from **Default rule-outs** in Preferences.
- **Status** says whether the needed count was met.
- **Explanation** states why the row passed or failed, including zygosity limits. When a counted cell's check cell is 0, the sentence names that cell and says the check cell did not react. A counted cell at CC 2+ is listed without that phrase.

Green rows met the required count. Yellow rows did not. Hover a row to read the same explanation.

## Cells that counted

Select a row. The lower grid lists the cells that counted toward that rule-out, with the panel, the run, and the grades. The check-cell grade sits beside AHG, so a rule-out cell shows whether the antiglobulin phase was controlled. When AHG is 0 and the check cell did not react, that CC cell is pink and the tooltip says check cells should react when AHG is 0. A reacting check cell, such as 2+, stays ordinary text. A cell counts when it is antigen-positive, nonreactive in the phases that count, and allowed by the zygosity rule. Homozygous cells are the usual standard. **Heterozygous OK** on a custom rule allows a heterozygous cell to count for that antibody.

Antigen-positive cells that reacted do not rule the antibody out. w+, mixed field, and hemolysis are reactive, so they do not count as rule-outs. Cells whose antigen was destroyed by ficin, papain, or DTT are listed on **Special Panels** as gated rule-outs instead of counting here.

The specimen detail pane on the Specimens tab repeats the ruled-out list as a summary. This tab is the one that shows the cells behind each count.

---

# Pattern Matches

Pattern Matches lists specificities that have at least one antigen-positive cell that reacted. It is a view of positive reactions, not a count of every cell on the panel.

The grid is empty when every panel cell is nonreactive, because there is no positive reaction to match. Antibodies that are already ruled out are left off this list. They stay on **Ruled Out**.

## Columns

- **Antibody** is the specificity.
- **Matches** counts antigen-positive cells that reacted.
- **Mismatches** counts antigen-negative cells that reacted.
- **Confidence** is matches divided by matches plus mismatches.

Nonreactive cells are not counted in either column. A cell whose antigen is NT is skipped. The autocontrol is skipped. A treated run skips an antigen that the treatment destroys.

## How to read a row

A high match count with few mismatches means the positive reactions line up with that antigen. Mismatches are antigen-negative cells that still reacted. Read those cells on **Suspected Antibodies** under conflicting reactions before you treat one specificity as the explanation.

This tab does not rule an antibody out and it does not confirm an identification. Confirm an identification only on **Suspected Antibodies**, after you have reviewed the evidence.

---

# Combinations

Combinations lists antibody pairs whose combination score is above 50 percent. That score suggests both antibodies may be present together when a single specificity leaves reactions unexplained.

The grid is empty when fewer than two antibodies are suspected. A pair is taken from the suspected list only. It is listed when at least one reactive cell carries both antigens and the combination score exceeds 50 percent. The score is the share of those reactive cells that carry one or both antigens. A workup with nothing suspected, including a nonreactive panel used for rule-out, has no pairs.

The note at the top of the tab states that threshold. The grid columns are:

- **Antibody Pair** — the two specificities.
- **Individual Scores** — each antibody’s own score.
- **Combination Score** — the score for the pair.
- **Both +** — cells that carry both antigens.
- **Ab1 Only** and **Ab2 Only** — cells that carry only one of the two.
- **Neither** — cells that carry neither antigen.

Read the pair against the reaction grid. A strong combination score is a prompt to look for two antibodies, not a confirmed identification. Confirm each specificity that you accept on **Suspected Antibodies**, and record both in **Final ID** separated by a semicolon, for example anti-E; anti-K.

---

# Dosage

Dosage compares reaction strength on homozygous cells with reaction strength on heterozygous cells for suspected antibodies.

A row appears only when a suspected antibody scores at least 60 percent, the antigen has an antithetical partner, both a homozygous cell and a heterozygous cell were graded, and the homozygous average is stronger by more than half a grade. The grid is empty when no suspected antibody meets that pattern. A workup where nothing is suspected, including a nonreactive panel used for rule-out, has no dosage rows.

The note at the top of the tab describes the comparison. The grid columns are:

- **Antibody** and **Antigen**.
- **Homo avg** and **Het avg** — average reaction strength on homozygous and heterozygous cells.
- **Homo n** and **Het n** — how many cells of each kind were included.
- **Severity** — how large the difference is.

A higher average on homozygous cells is the pattern expected when an antibody shows dosage. Small counts mean the average is based on very few cells, so read **Homo n** and **Het n** before you lean on the averages.

Dosage on this tab is evidence about reaction strength. The **Show dosage** checkbox on Reactions only changes how the antigram is drawn. Neither one changes the rule-out count. Zygosity still follows the Rules tab and the default rule-out setting.

---

# Special Panels

Special Panels collects the evidence that comes from treated cells and adsorbed serum. Create those runs with **+ Run** on the Reactions tab.

When the workup has only an untreated run, the comparison note is blank and the grids below are empty. There is no enzyme, DTT, or adsorption run to compare. That empty screen is the expected result, not a missing analysis.

## Treatment comparison

Enzyme and DTT runs are compared only with an untreated run on the same panel. The note under the heading names a run that was not compared, a comparison with fewer than three paired cells, or a comparison that fit no pattern. Scores in that note are evidence, not a diagnosis.

## Gated rule-outs

These rule-outs were suppressed because the antigen is destroyed by the cell treatment on that run. A ficin-treated or papain-treated panel cannot rule out an antibody to an enzyme-destroyed antigen. A DTT-treated panel cannot rule out an antibody to a DTT-destroyed antigen, including Kell-system antigens that DTT denatures.

**Antibody**, **Treatment**, and **Reason** name what was withheld and why. Those cells still appear on the reaction grid with a strikethrough antigen header. They do not add to the rule-out count on the Ruled Out tab.

## Treatment inferences

This grid interprets treated runs next to the untreated run. **Run**, **Antibody**, and **Observation** describe what changed, such as reactivity that disappeared after enzyme treatment. The observations are clinical interpretations for you to review.

## Absorption comparison

When a run used an adsorbed aliquot, this grid compares untreated serum with that aliquot on the same panel.

- **Adsorption** names the aliquot, such as R1R1, R2R2, rr, or autoadsorption.
- **Finding** and **Explanation** describe the analytical result.
- **Supporting cells** and **Conflicting cells** are the cell lists behind it.
- **Score** is an evidence score for review.
- One paired cell that loses reactivity while other paired cells remain reactive is still residual reactivity. That may be an underlying alloantibody or incomplete adsorption. It is not an identification.

The text above the grid states an important limit: ordinary probability and pattern scores omit specificities that the aliquot is expected to remove. The comparison is analytical evidence. It is not an identification. Confirm any specificity you accept on **Suspected Antibodies**.

---

# Patient Typing

Patient Typing lines each candidate up against the phenotype, genotype, and antibody history stored on the specimen.

The note on the tab states the rule: patient phenotype and historical antibodies are evidence for or against a candidate. Recent transfusion makes the phenotype uninterpretable. Record that transfusion in the specimen notes. This tab is not a diagnosis.

Columns:

- **Antibody** — the candidate.
- **Kind** — which piece of patient information was used, such as phenotype or previous antibody.
- **Patient** — the value on the specimen, such as antigen-negative or a historical antibody name.
- **Explanation** — how that value supports or argues against the candidate.

Genotype remains available for review after transfusion because it is a molecular prediction, not a fresh serologic type. It is still evidence for you to accept or set aside.

Edit phenotype, genotype, previous antibodies, and notes on the specimen record. Then run analysis again so this tab uses the updated context.

---

# Selected Cells

Selected Cells lists unused panel cells ranked by how well they separate the antibodies that are still candidates.

The note on the tab says these are testing suggestions, not identifications. The grid columns are:

- **Panel**, **Lot**, and **Cell** — where to find the cell.
- **Profile** — the antigen pattern that makes the cell useful.
- **Score** — how much the cell would discriminate among the remaining candidates.
- **Why this cell** — the short reason, such as which antibody it could rule out or support.

Use the list to choose the next cells to test. Enter the new grades on **Reactions**, then **Run Analysis** again. The suggestions box at the top of Analysis repeats the highest-priority next steps when the engine has them.

A suggestion does not add a grade and does not change the final ID. If you test the cell, the new grades become part of the evidence on the next analysis.

---

# Summary

Summary is a single read-only text review of the current analysis. It gathers the specimen, the panels and runs, the suspected antibodies, the rule-outs, and the final ID when one has been confirmed.

Use it when you want the whole workup on one page before you open **Reports**. The clinical identification report is the formatted report for the record. Summary is the on-screen digest and is not a separate saved document.

The text updates when you click **Run Analysis**. If the orange stale warning is showing, the summary still describes the previous analysis. Run analysis again so the summary matches the grades.

When the analysis has a panreactive, autoantibody, or high-prevalence pattern, Summary does not say that no antibodies are suspected. Those pattern sentences are the evidence. An ordinary workup with no such pattern still says no antibodies are suspected when the suspected list is empty.

You cannot type into the summary. Change grades on Reactions, change the final ID on **Suspected Antibodies**, and change clinical context on the specimen record.

---

# Clear a confirmed ID

**Clear** on the Suspected Antibodies tab asks for a reason before it removes a confirmed identification.

The prompt states what will be cleared. Type the reason in the box. A blank reason is not accepted. The reason is stored in the audit log with the operator initials, the specimen, and the time in UTC.

**OK** clears the confirmation and writes the audit event. The specimen returns to an unconfirmed state. Incomplete work can appear on the Worklist again, and Analytics no longer counts that confirmation.

**Cancel** leaves the confirmed ID in place.

Clearing an identification does not delete the reaction grades or the analysis. You can confirm again after you review the evidence. **Help** opens this chapter.

---

# Reports

The Reports tab previews a text report and can copy, print, or export it. Every report begins with the product name, version, and intended-use statement. That statement stays on the preview.

## Choose a report

The **Report** list has eight types:

1. **Specimen Summary** — one accession, its clinical context, linked panels, and final ID. Suspected and ruled-out rows follow the current analysis, not an older saved list. When a counted cell's check cell is 0, that ruled-out line names the cell and says the check cell did not react. A counted cell at CC 2+ keeps the count only. Requires a specimen.
2. **Panel Summary** — one lot’s identity and antigen grid. Requires a panel.
3. **Analysis Results** — the current analysis for one specimen, including pattern evidence when a panreactive or autoantibody pattern is present. When autoadsorption leaves cells reactive, that residual comparison is included. Suspected and ruled-out rows follow that analysis. An older saved list is not printed. When a counted cell's check cell is 0, that ruled-out line names the cell and says the check cell did not react. A counted cell at CC 2+ keeps the count only. Requires a specimen.
4. **All Specimens** — every specimen in the database.
5. **All Panels** — every panel lot. The name, lot, vendor, cell count, and expiration stay in their own columns, including when the name is long.
6. **Clinical Identification** — the identification report for one specimen, including the confirmed ID when you have confirmed one. A panreactive or autoantibody workup also lists the pattern evidence. Explain citations name the run, so a supporting cell on an autoadsorbed aliquot is not read as the untreated reaction. That wording is evidence, not a diagnosis. An ordinary alloantibody report does not add that block. Requires a specimen.
7. **Panel Antigram** — the bench antigram for one lot. Requires a panel.

Panel Summary, Panel Antigram, and the antigen grid on Clinical Identification leave out an antigen that is NT on every cell. A cell that is NT in a column typed on another cell prints NT. A typed minus stays a minus. The stored panel order is unchanged, and Edit on the Panels tab still shows the full order.
8. **Pending Work** — specimens and panels that still need attention. It does not ask for a specimen or a panel.

**Find** filters the specimen list by accession, type, phenotype, notes, or final ID. **Specimen** or **Panel** appears only for the report types that need one.

## Send it out

- **Copy** puts the full preview on the clipboard.
- **Print** prints the preview. Ctrl+P does the same while this tab is open.
- **Export CSV** saves a spreadsheet. Availability depends on the report type. Specimen and panel lists, and the single-specimen and single-panel summaries, export as CSV. Specimen Summary CSV uses the same current ruled-out list as the preview. When a counted cell's check cell is 0, the CheckCell column names that cell. A counted cell at CC 2+ leaves that column blank.
- **Export PDF** saves a PDF. Clinical Identification and Panel Antigram use a landscape page.

The preview is read-only. To change what it says, update the specimen, the panel, the grades, or the confirmed ID, then return to this tab. The preview refreshes when you change the report type or the selected specimen or panel. Changing the report updates the status line to name that report.

**Help** opens this chapter.

---

# Analytics

Analytics counts confirmed antibody identifications over a date range. It uses only identifications a user confirmed with **Confirm ID**. Suspected rows that were never confirmed are not included.

## Date range

- **From** includes confirmations on or after that date.
- **To** includes confirmations on or before that date.
- **30 days**, **90 days**, and **12 months** set the range through today.
- **All time** includes every confirmed identification, including any that have no confirmation date.
- **Refresh** reloads the cards, charts, and grid.
- **Export CSV** saves the detail rows.
- **Help** opens this chapter.

The line under the toolbar names the range that is actually in use. Changing the range updates the status line to name that range and the confirmed-specimen count.

## What the numbers mean

- **Confirmed specimens** is how many specimens had a confirmation in the range.
- **Distinct IDs** is how many different specificities appear.
- **Total occurrences** counts each specificity once per specimen. A call of anti-E; anti-K adds one to anti-E and one to anti-K.
- **Most common** is the specificity with the highest occurrence count.

The two charts show counts by specificity and the trend over time. When the range has no confirmed identification, each chart shows that title and does not draw a negative count scale. The grid under the charts lists the same breakdown. Use the grid when you need the exact counts behind a bar.

Analytics describes work this laboratory confirmed in the application. It is not a population frequency and it does not change any specimen.

---

# Search

Search finds panel cells whose antigen profile matches a pattern you build. Use it to locate cells for selected-cell testing or to see which lots carry a profile.

## Build the pattern

Each antigen is a button. Click it to cycle **Any → + → −**.

- **Any** ignores that antigen.
- **+** (green) means the cell must have the antigen.
- **−** (gray) means the cell must lack the antigen.

For antigens you marked +, **Positive cells** chooses zygosity:

- **Both** accepts any antigen-positive cell.
- **Homozygous** accepts an explicit ++ or an antigen-positive cell whose antithetical antigen is negative.
- **Heterozygous** accepts cells where both of the antithetical antigens are positive.

An untyped partner is not treated as negative. Antigens without an antithetical pair, such as D, stay a simple antigen-positive match.

## Run it

**Search** fills the results grid. **Clear** sets every antigen back to Any and sets Positive cells back to Both. The **Results** count is the number of matching cells. The status line names homozygous or heterozygous when that choice is in effect, so a smaller count is not read as an ordinary antigen-positive match. An E+ cell whose e is NT is left out of a homozygous search. Opening Search replaces a status line left by another screen. Leaving Search and coming back keeps the last search result on the status line. Selecting a result updates the status line to name that panel, lot, and cell. Double-click a result, or select it and press Enter or **Open**, to show that panel with that cell selected. The panel list brings that lot fully into view, including when it is the last lot. The status line names the cell. The antigen used in the search is brought into view on that grid, and the cell number stays on screen. When several antigens are set, those columns are shown first and stay on screen with the cell when that panel is scrolled, so the earlier one stays beside the later one. Choosing another lot restores the panel's own column order.

Each row is a panel, lot, and cell, followed by the stored antigen typing. A finished search brings the antigen used in the search into view on the result row. The panel, lot, and cell stay on screen. When several antigens are set, those columns are shown first and stay on screen with the panel, lot, and cell when the row is scrolled, so the earlier one stays beside the later one. A warehouse antigen that is blank on every matching cell is left off the row. Clearing the search, or searching on one antigen, restores the usual column order, including a warehouse column the matches left blank. The column headings stay visible while you scroll down the rows. The panel name and the lot are shown in full. A typed minus stays −. An antigen that was not tested shows NT, which is not the same as antigen-negative. A warehouse column that stays, because some matching cell types it, shows NT on a cell that was not typed for that antigen. An explicit ++ stays ++. Matching looks across the lots stored in the database, including extra warehouse antigens when a panel types them.

A − criterion matches only a typed minus. Cells stored as NT are left out of that search.

Search does not change grades or confirm an identification. To test a cell you found, enter the grades on the Reactions tab for the specimen you are working up.

---

# Rules

Rules are optional, per-antibody overrides of the default rule-out settings. When a rule exists for an antibody, analysis uses that rule. When it does not, analysis uses **Default rule-outs** from Preferences.

## The list

- **Name** is the label you gave the rule. Selecting a rule updates the status line to name that rule.
- **Antibody** is the specificity it applies to.
- **Het OK** means a heterozygous antigen-positive cell may count toward the rule-out.
- **Add** creates a rule. Ctrl+N does the same while this tab is selected. **Edit** opens the selected rule. **Delete** removes it after you confirm. **Refresh** reloads the list.
- **Help** opens this chapter.

## The detail pane

The right pane is read-only. It shows the name, antibody, exception antigen, whether heterozygous cells are allowed, the minimum rule-out count, and the description. Change those fields with **Edit**.

**Exception antigen** limits heterozygous permission to that antigen on the antibody named in the rule. A rule for anti-D with exception C does not let heterozygous C cells rule out anti-C. Leave the exception blank when the rule is only a count and a zygosity choice for its own antibody.

**Min Ruleout Count** replaces the default count for this antibody only. The Ruled Out tab shows that number in the **Needed** column and explains when the custom rule was used.

Rules apply the next time you run analysis. They do not rewrite a final ID you have already confirmed.

---

# Rule editor

This dialog adds or edits one custom rule-out rule.

- **Rule Name** is the label shown in the list.
- **Antibody** is the specificity. Pick one from the list or type a name.
- **Description** is a short note about why the laboratory uses this rule.
- **Exception Antigen** is optional. Pick or type an antigen when the rule depends on one.
- **Heterozygous OK** allows a heterozygous cell to count toward the rule-out. Leave it unchecked when only homozygous cells should count.
- **Min Ruleout Count** is how many qualifying antigen-positive nonreactive cells are required. The box starts at 3. Preferences limits the default count to 1–5; a custom rule replaces that default for this antibody.

**Save** stores the rule. The next analysis uses it for that antibody in place of the default count. **Cancel** discards the dialog. **Help** opens this chapter.

Delete a rule from the Rules tab if you want that antibody to return to the default.

---

# Preferences

Open **Tools → Preferences** to set laboratory defaults. **Save** writes them to this computer. **Cancel** discards the dialog. **Help** opens this chapter.

## Identity and thresholds

- **Lab name** and **Department** identify the laboratory on this workstation.
- **Score threshold** is the minimum combined score for the suspected list. The allowed range is 0.30 to 0.95.
- **ID cell rule** is how many antigen-positive reactive cells and antigen-negative nonreactive cells are required before a row is marked as meeting the identification rule. AABB 3 + 3 is the usual standard. The Suspected Antibodies tab turns those rows green.
- **ACS rule-outs** is how many times each clinically significant antibody must be ruled out before the application can suggest that all clinically significant antibodies are ruled out. The range is 1 to 5.
- **Default rule-outs** is the minimum number of qualifying antigen-positive nonreactive cells required to rule out an antibody when no custom rule exists. The range is 1 to 5. A rule on the Rules tab overrides this for that antibody.

## Specimens and the database

- **Default specimen type** pre-selects serum, plasma, or eluate on a new specimen.
- **Specimen dating (days)** pre-fills expiration when you add a specimen. 0 leaves the date blank. Three days is a typical antibody workup dating interval.
- **Expiration warning (days)** is how far ahead the Worklist and the lists mark a specimen or panel as expiring.
- **Max database size (MB)** warns when the database file reaches 80 percent of this size. The allowed range is 50 to 10240 MB. The warning can offer to open Purge Database.
- **Default initials** pre-fills **Initials** on Confirm ID. Confirming an identification updates this value to the initials you used.
- **Show inactive specimens and panels by default** turns on **Show Inactive** when those tabs open.

## Phases and the reaction grid

- **Hide ruled-out antigen columns on the Reactions tab** starts Reactions with **Hide ruled-out** checked.
- **Extra phases** adds optional columns after the classic IS / 37°C / AHG / CC grid. Leave the box blank to keep the classic grid.
- **RT**, **PEG**, **Gel**, and **Solid** are suggested columns. Gel, Solid, and PEG count as IAT-like evidence. You can also type custom names.
- **Titer grid (Dil1–Dil128)** adds serial-dilution columns. The last reactive tube is the titer endpoint. Dilution grades are not IAT, and they do not rule out or identify an antibody.

Changes apply to the next analysis and to new specimens. They do not rewrite identifications you have already confirmed. Saving a new score threshold, ID cell rule, ACS rule-out count, default rule-out count, or extra-phase list marks each unconfirmed analysis stale until you run analysis again. The lab name, department, and specimen defaults do not.

---

# Purge database

**Tools → Purge Database** removes old specimens so the database file can shrink. The application also offers this dialog at startup when the file is at or above 80 percent of the maximum size in Preferences.

The text at the top shows the current file size and the configured maximum.

## What to remove

- **Keep specimens from the last N days** deletes specimens older than that many days. The box starts at 365.
- **Delete specimens created before** a date you pick on the calendar.

The preview line states how many specimens match before you commit.

## Archive

**Create an archive of the purged data** is checked by default. Leave it checked unless you have another copy of the records. The archive holds specimen records and should be stored securely. The completion message includes the archive path. Open that file later with **Tools → Open Archive**.

**Purge** deletes the matching specimens after you confirm. Panels that are still needed by remaining specimens stay. **Cancel** closes the dialog without deleting anything.

After a purge, every tab reloads. The status bar reports how many specimens were removed and the new database size. **Help** opens this chapter.

---

# Open archive

**Tools → Open Archive** reads an archive created by Purge Database and can restore specimens that are no longer in the live database.

1. Click **Browse…** and choose the archive file. The path box shows the file you picked.
2. The summary states what the archive contains.
3. The grid lists each specimen with accession, type, created date, recorded evidence, and status. **Recorded** is the final identification when one was confirmed. Otherwise it is the antibodies stored with the specimen, plus DAT and notes. That text is the archive record, not a new identification. Status tells you whether that accession is already in the live database.
4. **Restore** copies missing specimens back into the database, including the saved analysis snapshot. That snapshot is the record of the last analysis, not a new identification. Restore stays disabled until an archive is loaded. Specimens that are already present are skipped and are not overwritten.
5. When the archive also contains panel lots that the live database does not have, those lots are restored with the specimens, including the saved antigen order. An order already stored for a lot that is still in the database is left as it is.

**Close** leaves the live database unchanged.

The completion message reports how many specimens were restored, how many were skipped because they were already present, and how many panels were restored. Every tab reloads after a successful restore. **Help** opens this chapter.

Store archive files with the same care as the live database. They contain specimen records.

---

# Audit log

**Tools → Audit Log** shows the append-only record of clinical actions. Events are never edited or deleted in this window.

Columns:

- **UTC** — when the event was recorded, in Coordinated Universal Time.
- **Operator** — the Windows login that recorded the action. Lab initials used on Confirm ID stay in the exported before-and-after detail.
- **Action** — what happened, such as confirming or clearing an identification, or saving a reaction.
- **Type** — the kind of record, such as a specimen.
- **Entity** — which record the event refers to.
- **Reason** — the text entered when a reason was required, including the reason from **Clear** on a confirmed ID.

**Find** narrows the newest 5,000 events by accession, action, Windows login, record type, or reason. **Export CSV…** saves those visible rows as a spreadsheet. **Close** returns to the main window. **Help** opens this chapter.

The log is a history of actions taken in the application. It is not the clinical report. Use **Reports** for the identification document, and use this dialog when you need to see who confirmed or cleared an ID and why.

---

# Demo data

**Tools → Load Demo Data** adds sample specimens, panels, and results so you can learn the screens. Existing data is not removed. The application asks you to confirm before it writes anything.

What the loader adds, when those records are not already present:

- 10 clinical specimens, accessions 2026-001 through 2026-010, on 5 shared panels.
- Enzyme (ficin) and absorption runs mixed into that workload.
- 11 DEMO- scenarios, including warehouse antigens with ficin and DTT runs.
- E2E- workups for warm allo, cold allo, warm and cold auto, mixed cold plus warm, combinations, exotic and HTLA patterns, a negative panel, and panreactivity with a negative autocontrol.

When it finishes, every tab refreshes. Open **Worklist** or **Specimens** and look for the 2026-, DEMO-, and E2E- accessions. You can enter grades, run analysis, and print reports against this data the same way you would for a real workup.

A DEMO- scenario that already has grades and has never been analyzed is analyzed when the application opens. The seeded grades and antigen typing stay as they are. Editing those grades afterward still marks the analysis stale.

Sample records are still stored in the same database as laboratory work. Delete individual DEMO- or 2026- specimens from the Specimens tab when you no longer want them, or exclude them by accession when you read Analytics.

**Help** for this action is this chapter. The menu item itself does not open a separate dialog before the confirmation prompt.

---

# Keyboard shortcuts

Press **F1** anywhere in the application to open help for the section you are in. On the Analysis tab, F1 follows the inner tab that is selected, such as Explain or Suspected Antibodies.

## Every tab

- **Ctrl+S** saves the current tab. On Panels it saves the antigen grid. On Reactions it saves grades without analyzing. Other tabs report that there is nothing to save.
- **Ctrl+R** refreshes every tab.
- **Ctrl+N** creates a new item for the tab you are on: a specimen, a panel, or a rule. Other tabs report that there is nothing to create.
- **Alt+F4** exits. You are asked to confirm.

## Worklist

- **Enter** opens the selected row.
- Click a count card to filter to that category. Click it again to show all.

## Reports

- **Ctrl+P** prints the current report.

## Analytics

Use **From** and **To**, or the **30 days**, **90 days**, **12 months**, and **All time** buttons, to set the confirmation range.

## Reaction entry

- **0–4** writes the grade 0, 1+, 2+, 3+, or 4+.
- **W** writes w+. **M** writes MF. **H** writes hemolysis. **N** writes NT.
- **Enter** moves to the next phase, then to the next cell.
- **Ctrl+S** saves grades without analyzing.

## Panel antigen grid

- Press **Edit** to enter antigen edit mode.
- Click a cell, or press **Enter** or **Space**, to cycle NT, +, ++, and −. Antigens without an antithetical pair skip ++.
- Green means the antigen is present (+ or ++). Light gray means it is absent (−).
- **Save** or **Cancel** leaves edit mode. Ctrl+S saves.
