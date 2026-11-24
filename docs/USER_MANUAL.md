# HemoIQ

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

HemoIQ helps licensed immunohematology professionals document antibody-panel reactions and compute rule-out and probability statistics. A qualified professional must confirm identification. This software does not issue or release blood units and is not a substitute for licensed clinical judgment.

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

## If HemoIQ cannot start

HemoIQ writes to its database file every time it runs, to keep the audit trail. If the file cannot be opened for writing, HemoIQ shows the file's path and the reason, then closes without changing anything:

- **Read-only.** The file or its folder is read-only, for example on a read-only network share. Fix the permission or ask IT to make the share writable.
- **In use.** Another program holds the file, for example a second copy of HemoIQ, a backup or a sync tool. Close it and start HemoIQ again.
- **Damaged.** The file is damaged or is not a HemoIQ database. Restore it from your laboratory's backup, and do not delete it.

## Menus

- **File → Exit** closes the application after you confirm.
- **Edit → Save** (Ctrl+S) saves the antigen grid on Panels or the grades on Reactions.
- **Edit → Refresh All** (Ctrl+R) reloads every tab.
- **Edit → New Item** (Ctrl+N) adds a specimen, panel, or rule, depending on the tab.
- Ctrl+S and Ctrl+N do only what the matching button would do. When that button is disabled (for example in a view-only session, or Save on Panels before you choose Edit), the shortcut does nothing and the status bar says why.
- **Tools** opens Preferences, Purge Database, Open Archive, Audit Log, and Load Demo Data.

The first time the application starts, it shows the intended-use statement. The status bar at the bottom reports the result of the last action. Opening a screen replaces that line with a line for the screen you are looking at, so a worklist message does not stay on Search. Returning to Search shows the last search result again, including the homozygous or heterozygous wording.

---

# Worklist

The Worklist is the operational list for specimens that still need work and for specimens or panels that are near or past expiration.

## Count cards

Five cards sit across the top:

- **Incomplete** — reaction grids that are not finished, for specimens that do not yet have a confirmed antibody ID.
- **Stale analysis** — grades were saved after the last analysis, the typing of a panel it used was corrected, a specimen or patient detail the analysis reads changed (for example transfusion status, DAT, phenotype or HPC transplant), or analysis rules or clinical Preferences changed after it, so the evidence on the Analysis tab is out of date. The detail says which. A specimen that has a panel and has never been analyzed is listed here with the detail "Never analyzed." Only investigations without a confirmed identification are listed.
- **Expiring** — specimens or panel lots inside the expiration warning window from Preferences.
- **Expired** — specimens or panel lots past their expiration date.
- **Review** — every other item: overdue testing, supervisor review, expired reagent use, untyped cells, check typing, duplicate lots, rule problems, data inconsistencies, and inconsistent status. These are described below.

Click a card to show only that category. Click the same card again to show every category that is still checked. A thicker border means that card is the active filter.

## Filters

- **Find** narrows the grid by accession, panel name, kind, urgency, or detail.
- **Show all** clears the card filter.
- The **Show** checkboxes turn Incomplete, Stale analysis, Expiring, Expired, and Review on or off.
- **Open** goes to the selected row. Double-click a row, or press **Enter**, to do the same. If that specimen can no longer be shown there (deleted on another workstation, or inactive when Open goes to Reactions), a *Not opened* message says why and you stay on the Worklist; HemoIQ never opens another specimen in its place. A panel item whose lot was deleted on another workstation says so the same way, and the Panels tab keeps its current lot.
- **Refresh** reloads the counts and the grid.
- **Help** opens this chapter.

## What a row opens

- Incomplete reactions open the Reactions tab for that specimen.
- A stale analysis opens the Analysis tab.
- Specimen rows open Specimens.
- Panel rows open Panels.

Row colors match the category: amber for incomplete, orange for stale, yellow for expiring, and red for expired.

The worklist does not include a confirmed identification as unfinished work. After you confirm an ID, incomplete and stale rows for that specimen drop off.

**Overdue testing** items list investigations whose status is **Awaiting additional testing** and that were received more days ago than the Preferences **Turnaround threshold**. The detail gives the received date, the age, and the testing the last analysis left open, for example clinically significant antibodies not yet ruled out or recommended selected cells. Open goes to Analysis. The item clears when the testing is recorded and analysis no longer leaves anything open, or when the identification is confirmed, cancelled, or the specimen made inactive. Investigations awaiting testing within the threshold are not listed; the status strip already shows them.

**Rule problem** items list lab rules that cannot work as written: a rule whose antibody is not a known antigen in its exact case (it never applies), or two or more rules for the same antibody (only the first by name is used). Such rules can only come from older data, because the Rule dialog now refuses them. Open goes to the Rules tab. Fix or merge the rules and the item clears.

**Data inconsistency** items list stored reactions that disagree with the specimen or panel. Nothing is changed automatically:
- **Reactions on detached panel:** a panel has grades for the specimen but is no longer attached. Analysis still reads those grades although the Specimens tab does not list the panel. Open goes to the specimen; re-attach the panel, then review or clear its reactions with a reason.
- **Grades for cells the panel lacks:** grades are stored for cell numbers the panel no longer has, so they cannot be matched to antigen typing. Open goes to Reactions.
- **Unrecognized grades:** a stored grade is not one of 0, w+, 1+–4+, MF, H or NT, for example from older or imported data. It is read as neither reactive nor nonreactive. Re-enter it on Reactions.

These states can come only from older or imported data: HemoIQ no longer detaches a panel that has grades.

**Supervisor review** items appear only when Preferences requires supervisor review. Each lists a confirmed identification that has not been reviewed yet. Open goes to the specimen, where **Supervisor Review…** records the review.

**Expired reagent** items list open investigations (not yet confirmed, or confirmed but not supervisor-reviewed) with reactions recorded on a panel lot after its expiration date. The recording date is the last day graded results were saved on that run. Open goes to Analysis, where the same finding is a **REVIEW** note at the top of the suggestions. HemoIQ does not exclude the results; the laboratory decides whether they may be used. The item clears once the investigation is confirmed and supervisor-reviewed.

**Untyped cells** items list active panels with cells that have no antigen typed positive. Every real red cell types positive for something: c or C, e or E, and k or K. A new panel's cells start typed negative, so a cell with nothing positive almost certainly has no typing entered yet. Analysis would read it as antigen-negative for every antibody. The item names the cells, or says the whole panel is untyped. Open the panel, enter the typing from the vendor antigram, and the item clears. An investigation that uses such a panel also shows a **REVIEW** note on Analysis.

**Inconsistent status** items list investigations that are cancelled but still hold a confirmed identification. HemoIQ does not let you cancel an investigation with an identification, or confirm one on a cancelled investigation, so this state only comes from older or imported data. Analytics does not count these records. Open the investigation and either **Reinstate** it to keep the identification, or clear the identification with a reason.

**Duplicate lot** items list a lot number stored on two or more panels when at least one of them is active. Lots match ignoring capitals and surrounding spaces. Vendors match ignoring capitals, and a panel with no vendor matches any vendor. The detail lists each copy with its number, name, vendor and active state. Results can end up recorded against the wrong copy, so keep one active and deactivate or correct the others. Adding, editing or copying a panel with a lot number that is already stored asks for confirmation first.

**Check typing** items list extra-antigen columns (for example Doa) that read negative on every cell of a panel with no record that anyone typed or verified them. Older versions filled a newly added extra antigen in as negative automatically, so such a column may be a default rather than real typing, and analysis would read it as antigen-negative. Compare the column with the vendor antigram. If it is correct, select the item and choose **Mark typing verified**; the confirmation is written to the audit log and the item clears (a Supervisor or Administrator is required once users are registered; any other role is told so before being asked to compare the antigram). If it is wrong, **Open** the panel and use **Edit** to correct the values or set them to NT. Nothing is changed automatically.

---

# Specimens

The Specimens tab is where you keep accessions and the clinical context that analysis uses as evidence.

## List

The left pane lists accession number, type, final ID, expiration, created date, and whether the specimen is active. The accession and the expiration stay on screen in full.

- **Find** filters by accession, type, phenotype, previous antibodies, notes, DAT, or final ID.
- **Add** opens a blank specimen record. **Edit** opens the selected record. Double-click a row to edit it.
- **Delete** removes the selected specimen, with its panel links, reactions, and analysis, after you confirm. It is meant for a specimen entered in error. A specimen with a confirmed identification cannot be deleted; use **Cancel** on the investigation instead, or have a supervisor clear the identification first. A specimen named as the source of an eluate cannot be deleted until that link is removed. Each deletion is written to the audit log with the MRN, type, received date, linked panels, and how many reaction rows were removed.
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

**Visual hemolysis** in the specimen dialog records how the specimen looked when received: Not recorded, None, Slight, Moderate, or Gross.
- A grossly hemolyzed specimen always adds a **REVIEW** note to analysis. Hemolysis in the test tubes cannot be told apart from the specimen's own, and free hemoglobin can hide weak agglutination, so consider a new specimen.
- Slight or moderate hemolysis adds a note only when a hemolysis (H) grade was recorded, naming those cells.
- No grade is changed or excluded.
- Changes are audited and shown on the Timeline. The field is locked once an identification is confirmed. The identification worksheet prints it.

**Linked Panels** are the lots used for this workup. **Attach Panel** picks a lot. **Detach** removes the selected link. A panel with grades for this specimen cannot be detached, because analysis would keep using those grades: clear its reactions on the Reactions tab (with a reason) first. Linking a panel makes it available when you enter reactions; the Reactions tab lists it straight away, even when that specimen is already selected there. If the specimen or the lot was deleted meanwhile (for example on another workstation), **Attach Panel** says so in a *Panel not attached* message, attaches nothing and refreshes the list. Grades cannot be saved on a panel that is no longer attached: if you detach a panel while unsaved grades for it are still on the Reactions tab, **Save** there says the panel is no longer attached and keeps the grades on screen (the detached panel stays listed there until you save or discard them); attach it again, then save. On a confirmed or cancelled investigation both buttons are disabled; hover over either for the reason and who can clear the identification or reinstate the investigation.

**Edit** is disabled the same way on a confirmed or cancelled investigation, because every field on it is locked. **Delete** is disabled on a confirmed one: a confirmed result stays on record. To withdraw it, clear the identification with a reason (a Supervisor or Administrator; it stays in the amendment history), then cancel the investigation. Cancelling while the identification is still confirmed is refused.

**Pattern evidence** appears when the latest analysis has a panreactive, autoantibody, or high-prevalence note, including a cold or warm phase sentence and an adsorption comparison when one was run. When that analysis left a panreactive untreated run out of ordinary scores, that exclusion is listed first, and the adsorption comparison follows it. Residual reactivity after autoadsorption is visible with the suspected antibodies, ahead of the warm and pan sentences. A specimen with no suspected specificity and no such note says so. The wording is evidence, not a diagnosis. An ordinary alloantibody score stays in Suspected Antibodies and does not add this block.

**Suspected Antibodies** and **Ruled-out Antibodies** follow the same current analysis as Pattern evidence. If that analysis cannot be read, the panes show the lists saved at the last Run Analysis instead, and an orange note above them says so, with the reason; those lists may be out of date, so open the Analysis tab and click **Run Analysis**. A cell whose partner antigen is NT does not rule an antibody out. When nothing meets the criteria, the ruled-out pane says so. When a counted cell's check cell is 0, the Check cell column names that cell and says the check cell did not react. A counted cell at CC 2+ leaves that column blank. Change them by entering grades and running analysis, not by typing in these grids.

## Specimen dating

**Collection** in Clinical Context shows the collection and receipt times. When the transfusion record shows a transfusion within the Preferences **Dating look-back** (default 90 days), or the patient is pregnant or was pregnant within that look-back, the specimen should not be used after collection date + **Specimen dating** days. A **REVIEW** note appears here, at the top of the analysis suggestions, and on the worksheet when:

- the recorded expiration is later than that date,
- no expiration is recorded, or
- no collection time is recorded, so dating cannot be checked.

The note is a prompt; HemoIQ never changes the expiration itself.

## Patient history

When the specimen has an MRN, the detail pane lists the patient's other specimens, newest first. Each line shows the date and encounter number, the confirmed identification, DAT, transfusion status, and the phenotype and genotype recorded on that specimen. An earlier phenotype can be the pre-transfusion typing to compare with when the current phenotype is unreliable after a transfusion; HemoIQ shows it for review and does not use it in analysis. Specimen Summary and the identification worksheet print the same list. Search and the specimen filter match MRN.

## Patient record

The **Patient (permanent, shared by MRN)** box shows information that belongs to the patient rather than to this specimen: name, date of birth, sex, antigen-negative units, other transfusion requirements, and permanent blood bank comments. Select **Edit Patient** to change it. The button needs an MRN on the specimen. Every change is written to the audit log with the old and new values. A confirmed identification on one specimen does not lock the patient record, because the record spans all of the patient's specimens.

Enter antigen-negative requirements in **Antigen-negative units** as antigen codes, for example `E, K, Fya` or `Fy(a-)`. A trailing `-`, `neg`, or `negative` is accepted. Capital and lower-case letters are different antigens (C is not c, K is not k). HemoIQ stores the list in panel order and prints it as, for example, E−, K−, Fy(a−). A word that is not an antigen, such as `irradiated`, is refused; write other requirements such as irradiated, CMV-safe, or washed in **Transfusion requirements**.

When an antibody is known for the MRN (Previous antibodies, a confirmed call on this specimen, or a confirmed call on another specimen with the same MRN) and its antigen is not in **Antigen-negative units**, analysis lists a **REVIEW** suggestion at the top. If the requirement is written only in the free-text **Transfusion requirements** (for example from before this field existed), the suggestion asks you to add it to **Antigen-negative units** so it is explicit. HemoIQ never copies it for you. It is a prompt to check the record, not a transfusion order.

**Sex** is Not recorded, Female, Male, or Other / X. It is shown after the date of birth wherever the patient is named, including reports.
- If a specimen records **Pregnant** or **Recently pregnant** for a patient recorded as male, saving the specimen asks for confirmation, and analysis lists a **REVIEW** suggestion. The specimen may belong to another patient, or one of the records may be wrong.
- The pregnancy context still applies to specimen dating until someone corrects it. Nothing is changed automatically.

**HPC transplant** records a stem-cell or marrow transplant (None, Autologous, or Allogeneic) and its date.
- **Allogeneic:** for specimens collected on or after the transplant date, analysis treats this patient's serologic phenotype as unreliable, as it does after a recent transfusion. Red cells may come from the donor or be mixed. The genotype is also listed as possibly the donor's: DNA from blood or marrow after an allogeneic transplant is the donor's, so the genotype counts for the patient only if it was done before the transplant or on a non-hematopoietic sample such as a buccal swab. If no date is recorded, every specimen is treated this way.
- **Autologous:** typing is unaffected, because the cells are the patient's own.
- **Where it appears:** the patient box, the Clinical Identification report, and the patient-typing notes on Analysis.
- **What it never does:** add or remove an antibody.

## Investigation status

The grey strip at the top of the detail pane shows where the investigation stands. It is worked out from what has been recorded, so it cannot fall out of date:

- **Steps:** ✓ Received, Panel attached, Reactions complete, Analysis current, ID confirmed. ○ marks a step that is not done. **Analysis current** follows the same rule as the Analysis tab warning and the Worklist: it clears when grades, analysis rules or clinical Preferences change after the last analysis.
- **Status:** Awaiting panel, Testing in progress, Analysis needed, **Awaiting additional testing**, Awaiting identification, Completed, **Amended** (an identification was cleared and confirmed again), **Reopened** (cleared and not yet confirmed again), or Inactive – not completed.
- **Next:** the specific next action, for example `Finish reactions: Panel A (1/2).`

**Receipt** in Clinical Context is a checklist of what to record when the specimen arrives. It reads **Complete (8 of 8)** or lists what is missing, and hovering over it gives each item's reason.
- **Required:** patient MRN, collection date/time, transfusion status, and the last transfusion date when the patient was transfused. These change how the analysis is read: patient history needs the MRN, and the transfusion window and specimen dating count from collection. While one is missing, the line is shaded amber, the **Received** step stays open, and the next action asks you to complete receipt with **Edit** before attaching a panel.
- **Advisory:** received date/time, visual hemolysis, pregnancy status and DAT. These complete the record but do not hold the Received step.
- **Patient history:** when another specimen with the same MRN records a red-cell transfusion within the recent-transfusion window, and this specimen says Not transfused or Unknown, **Transfusion status matches patient history** becomes a required gap. The same conflict appears as a REVIEW note on the analysis. A Transfused record whose date is older than the one on file adds an advisory item.
- The checklist never blocks testing. The Clinical Identification worksheet prints the same line.

**Awaiting additional testing** means the analysis is current but left questions that more testing could settle. Each analysis saves the open items, and **Next** lists them:
- clinically significant antibodies the panels type that are not yet ruled out;
- suspected antibodies that have not met the identification rule;
- multiple-antibody separation shortfalls;
- up to three recommended selected cells.

An antibody the patient's own reliable typing or genotype argues against is not listed.

Some reaction patterns change what is listed:
- **Panreactive or autoantibody-like:** **Next** points to autocontrol, DAT and adsorption instead of a list of antibodies and cells, because selected cells cannot separate specificities when every cell reacts.
- **High-prevalence or HTLA:** **Next** points to rare antigen-negative cells, titration or neutralization, with the recommended cells. This is a suggestion, not a block: you can still record the identification. Analyses saved before this version show **Awaiting identification** until analysis is run again.

The identification worksheet prints the same status line.

### Cancel and reinstate

Use **Cancel Investigation…** to stop an investigation without deleting it, for example for a mislabeled or hemolyzed specimen or a cancelled order. A reason is required. A confirmed identification must be cleared first. A cancelled investigation:

- shows status **Cancelled** with the date and reason,
- is locked: reactions, panels, analysis saves, and confirming an identification are refused,
- is removed from every Worklist queue.

**Reinstate…** reopens it and also requires a reason. Both actions are written to the audit log and appear on the Timeline.

### Supervisor review

When Preferences requires supervisor review, a sixth step, **Supervisor reviewed**, appears. Select **Supervisor Review…** and enter your initials. Rules:

- There must be a confirmed identification.
- The reviewer cannot be the person who confirmed it (initials are compared without regard to case).
- A specimen can be reviewed once. Clearing or re-confirming the identification removes the review, so the new call must be reviewed again.

Each review is written to the audit log with the confirmed antibodies and who confirmed them. The worksheet prints *Supervisor reviewed by …* or *Supervisor review: PENDING*.

## Timeline

Expand **Timeline** to see the investigation in time order, with who did each step where it is known:

- Specimen received, panel attached or removed, panel run started, and additional testing (ficin, papain, DTT, adsorption runs) started. **Specimen received** uses the received time recorded on the specimen, or the date it was entered when none is recorded. When the specimen was entered on a different day (for example from a downtime log), a separate **Specimen entered in HemoIQ** line shows that date.
- Reactions entered. Saves on the same run less than 15 minutes apart are combined, for example *3 save(s), 2 cell(s) on Panel A, Untreated*.
- Each analysis run, with the rule-engine version.
- Identification confirmed, cleared (with the reason), and supervisor review.
- Changes to MRN, transfusion record, DAT, visual hemolysis, and the patient record.
- Lab configuration changes made while the investigation was open: analysis rules added, changed or deleted, and changed clinical Preferences. Each entry names what changed, for example *Analysis rule changed: Jka dosage; minimum rule-out cells 3 → 2*. "Open" means after receipt, before an identification is confirmed or the investigation is cancelled, and again after one is cleared or reinstated. An analysis run before such an entry used the previous setting. The Worklist lists the investigation as **Stale analysis** until it is analyzed again.

Receipt and run starts record only a date, so they show first on their day. The identification worksheet prints the same timeline. Panels attached before this version were not audited, so only their run start dates appear.

## Eluate evidence

Enter an eluate as its own specimen with Type **eluate**, set **Eluate of** to the source accession, attach a panel, and grade it like any panel. **Eluate link** in Clinical Context shows *Eluate of X* on the eluate and *Eluates: …* on the source.

When the source specimen is analyzed, each linked eluate adds a note:

- **No reactions entered yet** — the eluate is linked but not tested.
- **Specificity** — the eluate's confirmed identification, or its suspected antibodies from its own analysis, with reactive and evaluated cell counts. After a recent transfusion, the note says this supports alloantibody coating transfused donor cells (possible delayed hemolytic or serologic transfusion reaction). An antibody in the eluate but not suspected in the source is named, because it may be bound to the red cells.
- **Panreactive** — four or more evaluated cells, at least 80% reactive. This supports IgG autoantibody; adsorption is suggested before ruling out an alloantibody after transfusion.
- **Nonreactive** — three or more evaluated cells, none reactive. With a positive DAT, the note lists drug-related antibodies, antibodies to antigens not on the panel, and complement-only coating. With a C3-only DAT, a nonreactive eluate is expected.

These notes never add, remove, or rule out an antibody.

---

# Specimen record

Use this dialog to add or edit an accession. Open it with **Add** or **Edit** on the Specimens tab, or with Ctrl+N while Specimens is selected.

## Sections

The fields are on four tabs. Press Ctrl+Tab and Ctrl+Shift+Tab to move between them.

- **Specimen**: accession, MRN and encounter, type, eluate source, collection and receipt, origin, expiration, and Active.
- **Clinical context**: transfusion status and date, pregnancy, and RhIG.
- **Serology**: DAT results, phenotype, genotype, and previous antibodies.
- **Notes**: free text.

The **Context** line under the tabs always lists what is recorded on Clinical context and Serology, for example *Context: Transfused (last 2026-10-30) · DAT IgG 2+ · previous anti-K*, so a value on a tab you have not opened is never hidden. Unknown, Not transfused, Not pregnant, and NT are left out. When **Save** finds a problem, it opens the tab that holds the field and puts the cursor there.

## Fields

- **Accession Number** identifies the specimen. Use the accession your laboratory already uses.
- **MRN** is the patient's medical record number. It is stored in capitals. Specimens with the same MRN share history: an antibody confirmed on an earlier specimen appears on this specimen's Patient Typing tab as historical evidence, with the accession it came from. Suspected antibodies that were never confirmed are not carried forward. MRN changes are audited and locked after an identification is confirmed.
- **Collected** and **Received** are date and time (HH:mm, 24-hour). Received defaults to now on a new specimen. Neither may be in the future, and Received cannot be before Collected. On a new specimen, choosing a collection date sets Expiration to collection + the Preferences specimen-dating days. Changes are audited and locked after an identification is confirmed.
- **Encounter** (beside MRN) is the encounter or account number the specimen was drawn under. It is optional, up to 64 characters, and spaces are removed. It is searchable on Search, prints on the worksheet's Origin line, and appears on the Timeline. Changes are audited and locked after an identification is confirmed.
  - If the same encounter number is already on a specimen with a different MRN, you are warned before saving, because one of the specimens may be mislabeled or the number mistyped. You can save anyway or go back. If you save anyway, the warning is recorded in the audit log, shown on the Timeline ("Saved past a warning"), and counted under **Warnings overridden** on Analytics.
- **Collected by / at** is who collected the specimen and where (unit, clinic, or draw site). **Ordered by / from** is the ordering provider and ordering location. Each is optional free text of up to 128 characters. They print on the worksheet, are searchable on Search, and the ordering location feeds **By ordering location** on Analytics. Changes are audited and locked after an identification is confirmed.
- **Type** is serum, plasma, or eluate.
- **Eluate of** is available when Type is eluate. Enter the accession of the serum or plasma specimen whose red cells were eluted. The eluate's results then appear in that specimen's analysis as reviewable notes (see *Eluate evidence* in the Specimens chapter). The link is audited and cannot point to the eluate itself or to another eluate. When both specimens have an MRN they must match, because the eluate's findings go into the source patient's analysis. A link to another patient's specimen is refused, and so is a later MRN change on either side that would separate them. Correct the MRN, or link the eluate to this patient's own specimen. The default comes from Preferences.
- **Expiration Date** is the specimen dating date. Preferences can pre-fill it from the specimen-dating setting. Clear the date if this specimen should not expire.
- **DAT polyspecific**, **DAT IgG**, and **DAT C3** are the direct antiglobulin test results: NT, Negative, W+, 1+–4+, or MF (mixed field). Any reactive result makes the DAT positive for the pattern notes. The DAT is negative only when every recorded result is negative. C3 reactive with IgG negative is noted as more typical of cold or complement-mediated reactivity. A mixed-field DAT is read against the transfusion record in the same way as a mixed-field autocontrol. IgG and C3 changes are audited and locked after an identification is confirmed.
- **Transfusion status** is Unknown, Not transfused, or Transfused. Analysis reads this field before it reads Notes.
- **Last transfusion** is the date of the most recent red-cell transfusion. It is available only when the status is Transfused. A date in the future is refused.
- **Products / units** records what was given, if known: Red cells, Whole blood, Granulocytes, Platelets, Plasma, or Cryoprecipitate, and the number of units (1–999).
  - If the products are recorded and none carry red cells (only platelets, plasma or cryoprecipitate), a recent transfusion does not make the phenotype unreliable, and the analysis note says why.
  - Plasma-containing products (whole blood, platelets, plasma) add a note that antibodies can be passively transferred.
  - Leave them unticked when not known; the phenotype is then treated as after a red-cell transfusion.
  - Changes are audited and shown on the Timeline.
- **Pregnancy** is Unknown, Not pregnant, Pregnant, or Recently pregnant. Recently pregnant requires the date the pregnancy ended. Pregnant, or a pregnancy that ended within the Preferences dating look-back, applies the specimen-dating check in the same way as a recent transfusion.
- **RhIG given** is the date Rh immune globulin was last given. If anti-D is suspected or confirmed within the Preferences RhIG window after that date, analysis notes that passive anti-D is possible.
- **Phenotype** is the serologic type, for example R1r, K−, Fy(a−b+). Analysis does not use the phenotype to exclude alloantibodies when the last transfusion falls within the Preferences recent-transfusion window, or when the status is Transfused with no date, unless the recorded products carry no red cells.
- **Genotype** is the predicted type from molecular results, for example RHCE*ce/ce. It stays available for review after transfusion. It is evidence for the reader, not a diagnosis.
- **Previous antibodies** records antibodies already known for this patient, for example anti-E.
- **Active** hides the specimen from the default lists when it is unchecked. Turn on **Show Inactive** to see it again.
- **Notes** holds free text such as medications or why the workup was ordered. When Transfusion status is Unknown, analysis still checks Notes and Phenotype for wording such as "transfused". Analysis names that keyword check in Patient Typing, so record the status instead. When the status is Not transfused, the status decides and any transfusion wording in Notes is named for review.

Every field is audited. Creating a specimen writes an audit entry. Changing type, expiration, Active, notes, phenotype, genotype, previous antibodies, or DAT polyspecific writes one entry listing only the fields that changed, with their old and new values, and the timeline shows *Specimen details changed* with the field names. Saving without changing anything writes nothing. Each change to the transfusion status or date is written to the audit log with the old and new values. The fields cannot be changed after an identification is confirmed.

**Save** writes the record. The whole record is saved together: if any value is refused (for example an eluate source that does not exist, or a record locked by a confirmed identification), nothing from that save is kept, no audit entries are written for it, and the message says so. **Cancel** discards the changes. **Help** opens this chapter.

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

- **Typing** shows how completely each lot is typed, counting the cells but not the autocontrol. For example, *24 antigens · Fya partial · cell 3 untyped*:
  - the number is how many antigens are typed on every cell;
  - **partial** antigens are typed on some cells but NT on others;
  - **untyped** cells have nothing typed positive.

  Partial and untyped usually mean the antigram entry is unfinished, and the column turns amber. Antigens that are NT on every cell are simply not on the antigram. A new lot shows **Not typed**. The column updates when you save typing.
- **Find** filters by name, lot, vendor, or expiration date. Find is unavailable while you are editing antigens. The name and lot stay on screen in full. If the list is narrowed, a long name is shortened and the full name shows when you point at it.
- Selecting a lot updates the status line to name that panel and its lot. Opening a cell from Search brings that lot fully into view in the list, including when it is the last lot, names that cell, highlights it on the antigen grid, and brings the antigen from the search into view. When several antigens are set, those columns are shown first and stay on screen with the cell when that panel is scrolled, so the earlier one stays beside the later one. Choosing another lot restores the panel's own column order.
- **Add** creates a lot. **Details** edits the name, lot, vendor, cell count, and expiration. The audit log keeps the old and new value of every field you change, so a corrected lot number or expiry stays traceable. Ticking **Active** on an inactive lot saves the other details first and then asks you to confirm activation, exactly as **Activate** does (antigram review, antigen coverage against the previous lot); answer No and the lot stays inactive. **Copy** duplicates a lot so you can start a new lot from an existing antigram. The copy is saved **inactive**, because each lot has its own antigram. Check the copied typing against the new lot's sheet and correct it with **Edit**, then **Activate**. Ticking Active in the Copy dialog asks for the same activation review. The audit log records which lot the typing was copied from.
- **Edit** turns on antigen editing for the selected panel. **Save** writes only the cells you changed and records each change (for example *Cell 3 Jka: + → -*) in the audit log. If the lot holds test history (recorded reactions or a confirmed identification), Save lists the changes and asks for a reason, such as the vendor's corrected antigram; without one nothing is saved. The edit then appears on the timeline of every specimen tested with the lot. Open investigations tested with the lot (no confirmed identification, not cancelled) are marked **Stale analysis** until analysis is run again, because their results were computed from the old typing; the status line says how many. Correcting only a cell's donor ID or Rh phenotype label is saved and audited the same way but does not mark analyses stale, because analysis does not read those labels. Special types do count. Confirmed identifications are not reopened. **Delete** removes the lot after you confirm. A lot that holds test history cannot be deleted: if any reactions were recorded on it, or it is linked to a specimen with a confirmed identification, HemoIQ refuses and says why. Deactivate it instead (choose **Details** and clear **Active**) so it is no longer offered for new testing. Deleting a lot that is only linked (no reactions, no confirmed identification) removes the links too, and the audit entry lists the specimens. Importing a lot that is already stored never replaces one that holds test history, and an import or copy that fails part-way leaves the stored panels exactly as they were.
- **Show Inactive** includes lots that are not active. Imported lots stay inactive until you review them.
- Yellow rows are nearing expiration. Red rows are expired.

## Bring a lot in

- **Import** creates a panel from a CSV lot sheet. The file uses a header row such as Cell, D, C, c, and the rest of the antigens. An antigen with no column is stored as NT. In a complete row, a blank value reads as negative (−), as on most lab sheets. A row with fewer columns than the header is refused, and so is the whole file, because a cut-off file must not leave its missing values typed negative. Re-export the sheet and import it again.
- **Download…** opens the vendor catalog, or lets you import a vendor PDF or CSV you already have.
- After an import, the review dialog lets you **Activate now** or **Keep inactive**. You can also select an inactive lot later and click **Activate**. If the lot's expiration date has passed, every activation route warns first. That covers **Activate**, Details, Copy and the import review. Reactions recorded on an expired lot are flagged on the Worklist as expired-reagent use. The audit entry records "Activated after expiry".
- **Antigen coverage at activation:** activating a lot compares it with the previous lot from the same vendor and product line. The prompt lists antigens the previous lot typed on every cell but this lot types on fewer cells, or none, for example "Fya (2 of 3 cells)". Antigens on the ACS list are named, because fewer cells will be available to rule them out. Untyped cells stay NT and are never used as antigen-negative. If you activate anyway, from the Panels tab or **Activate now** after an import, the audit log records the gaps and the previous lot.
- **Artifact** opens the retained import file and its SHA-256 hash.
- **Export** saves the selected antigen grid as CSV. When any cell has a donor ID, Rh phenotype or special types, the CSV adds **Donor** and **Rh** columns after Cell and a **Special types** column at the end, and **Import** reads them back (also under the headings *Donor ID*, *Rh phenotype* or *Special*), so a lot moved between workstations keeps them. JSON and XML imports keep each cell's donor ID and Rh phenotype. **Antigram** saves a bench-copy PDF that leaves out an antigen column that is NT on every cell. A cell that is NT in a shown column prints NT. A typed minus stays a minus. The suggested file name is the panel name and lot, for example *Resolve A lot L100 antigram.pdf*. Characters Windows does not allow in a file name, such as / or :, are replaced with _. If the file cannot be written, Export says so and nothing is saved.

## Antigen grid

The grid on the right is the antigram. Press **Edit** before changing it.

Each antigen value is one of **+**, **-**, **++** (homozygous) or **NT** (not tested). HemoIQ stores nothing else: a value such as 0 or w is refused with the cell and antigen named, and an import lists such values in its review instead of storing them.

- Click a cell, or press Space or Enter, to cycle **NT → + → ++ → −**. Antigens without an antithetical pair skip ++.
- Press **+**, **−** or **N** to set the current cell to +, − or NT directly.
- **Fill NT with −** types every NT cell in the selected antigen column as negative, skipping the autocontrol. For a new lot, enter the positives from the vendor antigram, then fill each column the antigram lists. Leave antigens the antigram does not list as NT. Nothing is stored until **Save**, which audits each change.
- A new cell starts as NT. Green is + or ++. Light gray is a typed −. NT means the antigen was not typed, which is not the same as negative.
- An antigen column that is NT on every cell is hidden. Press **Edit** to show those columns and type them. Save or Cancel hides them again if they are still NT.
- Drag column headers to reorder while editing.
- **Save** writes the grid (Ctrl+S does the same on this tab). **Cancel** leaves edit mode without saving.

**Add antigen…** adds a non-standard antigen from the warehouse catalog to this panel only. Every cell starts as NT (not tested), so analysis treats the antigen as unknown until you type each cell with **Edit**; a new column is never assumed negative. **Remove antigen…** drops one of those extra columns. If any cell is typed for that antigen and the lot holds test history, removal is refused because the typing is evidence behind past results; mark wrong values NT with **Edit** instead. Adding, removing, and reordering antigen columns are recorded in the audit log. **Compare…** diffs this antigram against another stored lot.

Antigen typing on the grid is the profile analysis uses. A blank or NT antigen is not treated as negative.

---

# Panel details

This dialog creates a lot, edits its details, or copies an existing lot. Antigen typing for each cell is entered on the Panels grid after you save the details. The cells of a new lot start as NT (not tested), never as negative, so a lot whose antigram has not been entered cannot be read as antigen-negative. The Worklist lists it as **Untyped cells** until the typing is in.

Editing the details keeps every cell's typing. Changing **Number of Cells**, **Starting Cell #** or **Include AC** adds the new cells as NT and removes only the cells that fall outside the new range. Removing a cell with recorded grades is refused, because its typing is the evidence behind those results. Keep the range, or copy the panel to a new lot. A lot number that another stored panel already uses asks for confirmation before saving.

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

While a catalog loads or a lot downloads, the vendor list and the Refresh and Import buttons are disabled, so only one download or import runs at a time. Closing the dialog during a download cancels it, and nothing is stored.

## File you already have

**Import vendor file…** asks for a vendor PDF or CSV. The same review step follows either path: the new lot stays inactive until you activate it, and the source file is kept so you can open it from **Artifact**.

**Close** leaves the dialog. **Help** opens this chapter.

A downloaded antigram is still source material. Compare it with the lot sheet before you activate the panel and use it for a workup.

---

# Review an import

Imported panels stay inactive until a qualified user reviews them. This dialog opens after **Import**, **Download…**, or **Activate** on a lot that still needs review.

The text box summarizes what was read: panel name, lot, vendor, cell count, and any parse notes. Read it against the manufacturer antigram.

It also covers:
- **Prior lot:** when an earlier lot of the same vendor and product line is stored, the summary says which antigen columns were added or removed.
- **Typing:** how completely the new lot is typed, in the same format as the Panels list Typing column.
- **Partly typed antigens:** any antigen the prior lot typed on every cell but the new lot types on only some, for example *Fya (1 of 3 cells)*. That usually means a blank column on the vendor sheet or a parsing gap. The cells left blank are stored as NT, never as negative. Check those columns before activating.

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
3. **Typing** shows how completely each lot is typed, as in the Panels list Typing column: antigens typed on every cell, partially typed antigens, and untyped cells.
4. The explanation summarizes how the two antigrams differ. It names any antigen typed on every cell of the selected lot but not of the other, for example *Fya (9 of 11 cells)*. It also names cells on the other lot with nothing typed positive. Different donors explain different + and − values, but an antigen the earlier lot typed fully and the new lot types only partly usually means the new antigram entry is unfinished.
5. The grid lists each cell and antigen that does not match. **Selected** is the lot you started from. **Other** is the lot you picked. **Note** describes the difference.

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

The Reactions tab is where you record IS, 37°C, AHG, and CC grades for each cell, plus any extra phases configured in Preferences. A phase that was graded on the run but has since been removed from Preferences keeps its column, headed *(stored)*, because analysis still reads those grades.

## Choose the work

- **Find** filters the specimen list by accession, type, phenotype, previous antibodies, notes, or final ID.
- **Specimen** and **Panel** pick the accession and the lot. Grades load when you change specimen, panel, or run.
- **Run** selects which set of grades you are editing. The first run is the untreated panel. **+ Run** adds a treated or adsorbed run. It is refused while the run you are viewing has unsaved grades: save them first (or reselect the run and discard them), so no grades are lost when the tab moves to the new run. **Delete Run** removes the run you are viewing. If it holds unsaved grades, the question says they are discarded too; after the delete the tab moves to the next run without asking about them again. Changing the run updates the status line to name that run.

## While you enter grades

- **Compare** shows another run beside this one. Pick that run in the second list. Grade columns are named for the two runs. Rows that differ are highlighted, the Changed box is checked, and extra phases are included. The Changed box is display only.
- **Hide ruled-out** hides antigen columns already ruled out for this specimen. The setting can also be turned on by default in Preferences.
- **Show dosage** groups columns by blood-group system and marks homozygous ++, heterozygous +, or +/? when the antithetical antigen was not typed. This is display only. It does not change rule-out.
- **Load Reactions** reloads grades from the database. If grades on screen are not saved, it first asks whether to discard them; **No** (the default) keeps them on screen.
- **Ctrl+V** on a phase cell (IS, 37°C, AHG, CC or an extra phase) pastes grades copied from a spreadsheet, starting at the selected cell.
  - One line fills one cell, going down.
  - A single column fills the selected phase. Several columns fill the selected phase and the phases to its right, in the order they appear on screen. For example, IS, 37°C and AHG copied together, with the IS cell selected.
  - Values are read like keyed grades: 0, w or w+, 1–4 or 1+–4+, MF, H, NT. A blank value is NT, and a line with fewer columns is padded with NT.
  - Any other value cancels the whole paste and lists the line and column to fix, because an unrecognized grade would otherwise count as reactive.
  - The paste is also refused if it has more lines than there are cells below the selection, or more columns than phases to the right.
  - If any cell already has a different grade, HemoIQ lists the changes and asks before replacing them.
  - Pasted grades are unsaved until **Save**, and the unsaved-grade warnings apply to them.
- **Save** writes grades without running analysis. Ctrl+S does the same.
- A confirmed or cancelled investigation is locked. An orange banner gives the reason and who can unlock it (clear the identification, or reinstate the investigation). The grades are shown read-only, and **Save**, **Save & Analyze**, **Clear**, **Fill negatives** and the run buttons are disabled. The lock is checked again whenever you open the Reactions tab, so a confirmation made on the Analysis tab shows here at once.
- **Save & Analyze** writes grades, runs analysis, and shows a green or red banner with the result. A save that fails writes nothing and says *Not saved: …*; the grades stay on screen. If the run itself was deleted meanwhile (for example on another workstation), the message says the run no longer exists: reselect the specimen and enter the grades on a run that exists. If the grades are saved but the analysis then fails, the banner says *Grades saved, but analysis did not run*, so run analysis again rather than re-entering the grades.
- **Unsaved grades** are protected in three places:
  - Changing the specimen, panel or run asks whether to discard them. **No** (keep them) is the default, so pressing Enter never throws grades away.
  - **Run Analysis** on the Analysis tab, for the same specimen, explains that analysis uses saved grades only. **Yes** saves them first, with the usual expired-lot and change-reason checks. **No** analyzes without them. **Cancel** does nothing.
  - Quitting HemoIQ lists the unsaved grades, and an unfinished antigram edit on Panels, and asks before quitting. **No** is the default.
- **Recovery after a crash:** while grades are unsaved, HemoIQ keeps a recovery copy on this computer, in `%LocalAppData%\HemoIQ\drafts`.
- **View only:** when the status bar says "view only" (a Read-only user, or a login that is not registered once users exist), the grid shows grades but cannot be edited. Grade keys and paste do nothing, Save and Fill negatives are disabled, no recovery copy is written, and the progress line starts with "View only".
- **Checked on save:** every grade must be 0, w+, 1+ to 4+, MF, H or NT. Common spellings such as "W", "2" or "mixed field" are stored in that standard form; anything else, such as "pos", is refused with the cell and phase named. Grades can only be saved for cells the panel has (and the autocontrol). If any row is refused, none of the grid is saved, so fix the value and save again.
  - **Save**, discarding the grades, or quitting normally removes it.
  - If HemoIQ closed unexpectedly (a crash, power loss, or the process ended), the next start says which specimen, panel and run, how many cells, and when they were last edited.
  - **Yes** puts the grades back on the Reactions tab, still unsaved, for you to review and **Save**. **No** discards them.
  - Recovered values are checked as grades, so an unrecognized value is not restored. Cells matching what is already saved are left out.
  - If the identification was confirmed, or the investigation cancelled, in the meantime, the grades are listed but not restored, with who can clear or reinstate it; the recovered grades are then discarded.
  - In a view-only session the grades are listed but not restored, and they are kept so a login that can record results can restore and save them.
- Saving graded results on a panel lot that has expired asks for confirmation first. If you record them anyway, analysis shows a REVIEW note and the Worklist lists the investigation as **Expired reagent**.
- Saving graded results on a specimen past its expiration date also asks first, because the results may not represent the patient's current antibodies. **No** leaves the grades unsaved. Expired specimens are made inactive when HemoIQ starts, so this prompt appears for a specimen that expired while HemoIQ was open (for example overnight). If you record them anyway, analysis shows a REVIEW note naming each run recorded after the specimen expired, the reports print it, and the Worklist lists the investigation as **Graded after specimen expiry** while the specimen is still active (besides the usual **Expired specimen** item).
- The grid is saved as one unit: if any cell cannot be saved, none is. Cells you did not change are not rewritten and add nothing to the audit log.
- After the specimen has been analyzed, changing a recorded result (anything other than NT) asks for a reason. The prompt lists each change, for example *Cell 3 AHG: 2+ → 0*. Without a reason nothing is saved. The reason and the original value go into the audit log, and the timeline shows the reason on that entry. Entering a result in an empty or NT cell never needs a reason.
- **Fill 0s** sets remaining NT phases to 0. Check cells become 2+ when AHG is 0. Grades you already entered are left alone.
- **Clear** removes the grades on this run.
- **Clear** and **Delete Run** keep a copy of every deleted grade in the audit log and appear on the investigation timeline. After the specimen has been analyzed, deleting graded cells asks for a reason instead of Yes/No; without one nothing is deleted. Neither is possible once an identification is confirmed. After **Clear**, the run's cells are shown again with no grades, ready for new entry; if unsaved grades were on screen, the question says they are discarded too.
- **Help** opens this chapter.

A blue banner names the cell or serum treatment when the run is not an ordinary untreated panel.

## The grid

Antigen columns come from the panel. A column that is NT on every cell is hidden. A typed minus stays. Red header text means that antibody is ruled out for this specimen. A strikethrough header means the antigen is destroyed by the cell treatment, so that column cannot support a rule-out on this run.

Row colors: blue when no grades have been entered, green when the row contributed to a rule-out. Phase cells use yellow for w+ or 1+, orange for 2+, red for 3+, 4+, or hemolysis, and purple for mixed field. A pink CC cell is invalid when AHG is 0.

Mixed field on the autocontrol (AC) row is read against the specimen's transfusion record. After a recent transfusion, analysis notes that alloantibody may be coating surviving donor cells and suggests a DAT and an eluate. If the status is Not transfused, the note asks you to review transfusion history, transplant, and the specimen. If the status is Unknown, it asks you to record the status. Mixed field on a panel cell is still treated as reactive evidence. None of these notes changes the suspected or ruled-out lists.

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
- **Absorbed: R1R1 (DCe/DCe)**, **Absorbed: R2R2 (DcE/DcE)**, and **Absorbed: rr (ce/ce)** are allogeneic adsorptions. R1R1 cells are expected to remove anti-D, anti-C and anti-e; R2R2 cells anti-D, anti-c and anti-E; rr cells anti-c, anti-e and anti-f (rr cells carry f, the compound of c and e). An absorbed aliquot is not used to rule out the antibodies it is expected to remove.
- **Autoadsorption** is an autoadsorbed aliquot. If the specimen records a red-cell transfusion within the recent-transfusion window (Preferences), or the transfusion status is Unknown and the notes mention transfusion, or the patient record shows an allogeneic HPC transplant on or before collection, or another specimen for the patient records a transfusion within the window, donor cells in the patient's sample may have adsorbed alloantibodies. The autoadsorbed aliquot is then not used to rule out any alloantibody; its negatives are listed on **Special Panels** with the reason. Consider allogeneic (differential) adsorption instead.

The italic note under the lists explains what the selected combination will mean when you analyze. It is built from the same treatment tables analysis uses, so it lists exactly the antigens analysis treats as destroyed or enhanced (for example, ficin and papain enhance Lea and Leb) and the antibodies each adsorption removes.

## Label and grades

**Label** is optional. Leave it blank and the application names the run from the treatments you picked. If you type a label that does not mention the treatment, the treatment is added in parentheses wherever the run is shown, for example **Repeat (Ficin-treated)**, so a report never hides that the run's cells or serum were treated.

**Pre-fill with grades from the current run** is off by default and is available only for an untreated repeat (no cell or serum treatment). A treated run exists to show how the treatment changes the reactions, so its grades must be read from the treated test; copying untreated grades into it would claim the treatment changed nothing. When you use it, the grades are placed in the new run's grid as **unsaved** values, and the status line says so. Check each one against the test, change any that differ, then **Save**. Nothing is recorded until you save. The grades copied are the current run's saved grades: **+ Run** is refused while that run has unsaved grades, so save them first.

**Add Run** creates the run and selects it. **Cancel** leaves the runs unchanged. **Help** opens this chapter.

Compare the new run with the original from the Reactions tab by turning on **Compare**. Treatment inferences and adsorption findings appear on the Analysis tab under **Special Panels**.

---

# Analysis

Analysis computes rule-outs, scores, and pattern statistics from the grades and the antigram. The results are analytical evidence for a qualified professional. They are not a diagnosis, and the application does not release blood.

## Top of the tab

- **Find** filters specimens by accession, type, phenotype, previous antibodies, notes, or final ID.
- **Specimen** selects the accession to review. Selecting a specimen updates the status line to name that accession.
- An orange **Analysis is stale** warning says why the results shown are out of date. Either grades were saved after the last run; or the typing of a panel it used was corrected on the Panels tab; or a specimen detail the analysis reads changed (transfusion status, date or products, DAT, visual hemolysis, collection time, pregnancy or RhIG, phenotype, genotype, previous antibodies, notes or specimen type, or the patient's HPC transplant, antigen-negative requirement or transfusion requirements); or another specimen of the same patient had its transfusion record, collection time or phenotype changed, since transfusion history and an earlier phenotype feed the analysis; or an eluate of this specimen was linked, unlinked, analyzed or confirmed, since the analysis lists the eluate's findings; or the specimen was moved to another MRN, or an identification was confirmed or cleared on another specimen of the same patient, since patient history feeds the analysis; or analysis rules or clinical Preferences changed after it (for investigations without a confirmed identification). Click **Run Analysis** again before you rely on the tabs. The status strip on Specimens, the Worklist and the Pending Work report all use this same rule. Grades saved after an analysis always mark it stale, even within the same second or when the computer clock has gone back (at the end of daylight saving time, for example). In that case the saved time shown can be up to an hour later than the clock.
- **Enter Reactions** jumps to the Reactions tab for this specimen.
- **Clinical Report** opens the clinical identification report.
- **Run Analysis** recalculates every inner tab.
- Selecting an analyzed specimen rebuilds its results. If that fails, the Summary says *The analysis of … could not be shown* with the reason, and the status line repeats it: the empty lists are then not a result. Click **Run Analysis** to try again, or check the specimen's panels and grades.
- **Help** opens help for the inner tab you are viewing, or this overview if no inner topic is available.

When the engine has testing suggestions, they appear in the yellow **Suggestions** box under the toolbar. Read them as next steps, such as cells still worth testing. The box is ordered by priority. Warnings and REVIEW items come first, then important next steps, then informational notes.

The box shows the first three suggestions and stays short, so the tabs below remain visible. It scrolls if those three run long. **Show all N (+M more)** at its top right expands it to the full list, still in a scrolling box of limited height. **Show fewer** collapses it again. Each new analysis starts collapsed. The Analysis Results report and the clinical report print every suggestion.

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

- **Separation** appears when two or more rows meet the ID rule. It counts reactive cells that carry this antigen and are negative for every other called antigen, against the number set in Preferences (**Multiple-Ab separation**), for example **2 / 1**. A cell with an untested (NT) partner antigen does not count. **Exempt (history)** means the antibody is known from this patient's history, either in the specimen's previous antibodies or from a confirmed identification on another specimen with the same MRN. **Exempt (rule)** means a rule on the Rules tab exempts it. A count that falls short is shown in bold amber, and Suggestions says which phenotype to test next.

Green rows meet that rule. Yellow rows do not. Double-click a row to add it to **Final ID**. A green row is a candidate that meets the cell rule. A yellow row can still be added, and the tooltip says that it does not yet meet the rule.

## Supporting and conflicting cells

**Supporting cells** are antigen-positive cells that reacted, with the panel, run, cell number, phase grades, and the check cell.

**Conflicting cells (Ag− but reactive)** are cells that lack the antigen and still reacted. The same grades are shown, including the check cell. Conflicts mean another antibody, a typing discrepancy, or a reaction that the single specificity does not explain. Resolve them before you confirm.

## Final ID

- **Final ID** is the confirmed text, for example anti-E; anti-K. Separate specificities with a semicolon. Green rows are pre-filled when they meet the ID rule.
- **Add selected** appends the highlighted row.
- **Comment** is an optional note stored with the confirmation.
- **Initials** identifies who confirmed the ID. Preferences can pre-fill this, and a successful confirmation updates the default initials.
- **Confirm ID** locks the identification onto the specimen. If the specimen was never analyzed, or the analysis is out of date (the orange warning), HemoIQ says why and asks whether to confirm anyway, with **No** as the default. If the latest analysis left testing open (for example clinically significant antibodies not yet ruled out, an ID-rule shortfall or recommended selected cells), HemoIQ lists the open items and asks whether to confirm anyway, with **No** as the default. When Final ID lists two or more antibodies and one falls short of the separation criterion, HemoIQ lists the shortfall and asks whether to confirm anyway. It does not block confirmation.
  - Each warning you confirm past is recorded with the confirmation in the audit log. It is shown on the Timeline ("WARNINGS OVERRIDDEN"), on the identification worksheet ("Warning overridden at confirmation"), and in the Worklist's supervisor-review item, so the reviewer sees exactly what was overridden.
  - Clearing the identification removes the overrides from the current record. The audit log keeps them. Reports, Analytics, and the Worklist then treat the specimen as confirmed.
- **Clear** removes a confirmed ID. You must enter a reason. The reason is written to the audit log.
- A confirmed ID cannot be replaced by editing Final ID and clicking **Confirm ID** again. HemoIQ says the specimen already has a confirmed identification. To change it, click **Clear**, give the reason, then confirm the new identification. The earlier identification, who confirmed it and the reason for the change appear in the amendment history on the clinical report.
- If the specimen was deleted meanwhile (for example on another workstation), **Confirm ID** and **Clear** say it no longer exists and record nothing; refresh the list.

Confirm only after you agree with the evidence. The software does not confirm an identification for you.

---

# Ruled Out

This tab shows the rule-out count for each antibody and the cells that produced it.

## The count grid

- **Antibody** is the specificity.
- **Count** is how many qualifying cells ruled it out.
- **Needed** is the minimum required. That number comes from a rule on the Rules tab when one exists for the antibody, and otherwise from **Default rule-outs** in Preferences.
- **Status** says whether the needed count was met.
- **Explanation** states why the row passed or failed, including zygosity limits.

Green rows met the required count. Yellow rows did not. Hover a row to read the same explanation.

## Cells that counted

Select a row. The lower grid lists the cells that counted toward that rule-out, with the panel, the run, and the grades. The check-cell grade sits beside AHG, so a rule-out cell shows that the antiglobulin phase was controlled. A cell with AHG 0 whose check cell was recorded as 0 did not show that the antiglobulin phase worked: the result is invalid, it does not count toward any rule-out, and **Special Panels** lists it with "Repeat the test". A check cell that was not recorded (NT) still counts; the Reactions grid shows that CC cell pink as a reminder. A cell counts when it is antigen-positive, nonreactive in the phases that count, and allowed by the zygosity rule. Homozygous cells are the usual standard. **Heterozygous OK** on a custom rule allows a heterozygous cell to count for that antibody.

**Cells not counted for dosage.** For antigens with an antithetical partner (for example Jka/Jkb), a nonreactive cell counts only when it is homozygous, unless heterozygous cells are allowed. A cell is treated as heterozygous when the partner is typed +, and as of **unknown zygosity** when the partner is not typed. It never counts as homozygous. These cells are now named rather than silently skipped:
- When no cell counts, the antibody still gets a row. Its explanation reads, for example, "Anti-Jka cannot currently be ruled out because the only nonreactive Jka-positive cells are heterozygous (cells 1, 2), and heterozygous cells are not allowed for this antibody (dosage)", and says what to test next.
- When some cells count but not enough, the explanation adds "Not counted for dosage:" with the heterozygous and unknown-zygosity cells.
- The same sentences appear in the analysis suggestions.
- On the grid, a row with no counted cell has the status **Blocked by dosage**. Selecting any row lists all its nonreactive antigen-positive cells: first the counted ones (**Counted: Yes**), then those left out (**No (dosage)**). The Zygosity column says Homozygous, Heterozygous, or Unknown (for example "Unknown (Jkb not typed)").
- **Explain** lists the same cells, each marked "not counted: heterozygous" or "not counted: zygosity unknown", and gives such an antibody the status **Not ruled out (dosage)**.
- **Selected Cells** recommends untested inventory cells that are homozygous for that antigen, for example "It is homozygous Jka+ Jkb- and, if nonreactive, would count toward ruling out anti-Jka (0 of 1 so far)". Cells carrying the antigen of a suspected antibody are skipped, because they are expected to react.

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

These rule-outs were suppressed because the antigen is destroyed by the cell treatment on that run. A ficin-treated or papain-treated panel cannot rule out an antibody to an enzyme-destroyed antigen. A DTT-treated panel cannot rule out an antibody to a DTT-destroyed antigen, including Kell-system antigens that DTT denatures. Lewis antigens (Lea, Leb) are not destroyed by ficin or papain; anti-Lea and anti-Leb usually react more strongly with enzyme-treated cells, so those cells can rule them out and a pattern that disappears on enzyme-treated cells does not point to Lewis. Indian antigens (Inb) are destroyed by ficin, papain and DTT, so none of those panels can rule out anti-Inb.

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

The note on the tab states the rule: patient phenotype and historical antibodies are evidence for or against a candidate. Recent transfusion makes the phenotype uninterpretable. Record the transfusion status and last-transfusion date on the specimen. A **[Transfusion]** line in Summary states which record decided this: the structured date and its day count against the Preferences window, or the Notes keyword check when the status is Unknown. This tab is not a diagnosis.

Columns:

- **Antibody** — the candidate.
- **Kind** — which piece of patient information was used, such as phenotype or previous antibody.
- **Patient** — the value on the specimen, such as antigen-negative or a historical antibody name.
- **Explanation** — how that value supports or argues against the candidate.

Historical rows also come from other specimens with the same MRN. Each antibody confirmed there is listed once, citing the earliest accession that confirmed it. An antibody already typed into Previous antibodies is not listed twice.

**Earlier (pre-transfusion) phenotype.** When this specimen's phenotype cannot be used, or none was entered, HemoIQ looks at the patient's other specimens (same MRN) for the most recent phenotype that still reflects the patient's own cells:
- The earlier specimen must have been collected before this specimen's last transfusion, or before this specimen when it is not transfused.
- Its own typing must have been usable when it was collected, judged by the same transfusion rules.
- It is not used when this specimen is marked Transfused with no date, because the earlier typing cannot be shown to predate the transfusion.
- It is not used for a patient with an allogeneic HPC transplant.

When used, each line names its source, for example "Earlier phenotype from 2026-001 (2026-03-02, before the current transfusion context): Patient types E+…". The **[Transfusion]** note says the earlier phenotype is used instead. It counts as patient-typing evidence like a current phenotype: an antigen-positive typing argues against the alloantibody and is flagged if that antibody is suspected. Review it as you would any historical record.

**Transfusions recorded on the patient's other specimens** are also checked. If another specimen with the same MRN records a red-cell transfusion within the Preferences **Recent transfusion** window before this collection:
- **Not transfused** on this specimen is contradicted. The suggestions start with a **REVIEW** note naming that specimen and date, and patient typing is treated as unreliable until you correct or confirm the status.
- **Unknown** on this specimen: patient typing is treated as unreliable, and the note gives the date to record.
- **Transfused** with an older last-transfusion date: a note asks you to update the date so the window is counted correctly. Reliability is otherwise decided as usual.

Transfusions recorded only as platelets, plasma or cryo, transfusions after this collection, and specimens without an MRN are not used.

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

When two or more antibodies are called together and one falls short of the multiple-antibody separation criterion (see **Separation** on Suspected Antibodies), cells with the missing phenotype rank first. For anti-E with anti-K, a K+ E- cell would rank first if anti-K falls short. **Why this cell** says the cell would count toward the criterion if the serum reacts with it, and Suggestions names these cells. A rule on the Rules tab that exempts a partner antigen removes that antigen from the phenotype needed.

Use the list to choose the next cells to test. Enter the new grades on **Reactions**, then **Run Analysis** again. The suggestions box at the top of Analysis repeats the highest-priority next steps when the engine has them.

A suggestion does not add a grade and does not change the final ID. If you test the cell, the new grades become part of the evidence on the next analysis.

---

# Summary

Summary is a single read-only text review of the current analysis. It gathers the specimen, the panels and runs, the suspected antibodies, the rule-outs, and the final ID when one has been confirmed. Each suspected antibody shows its combined score, for example *anti-E (score 85.0%)*, labelled as on the reports because it ranks the evidence and is not a probability.

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

The Reports tab previews a text report and can copy, print, or export it. Every report begins with the facility letterhead set in **Preferences** (facility name, address and contact information), then the product name, version, and intended-use statement. Any part of the letterhead that is not set is left off; with none set, the report starts with the product name. The intended-use statement stays on the preview.

## Choose a report

The **Report** list has eight types:

1. **Specimen Summary** — one accession, its clinical context, linked panels, and final ID. It prints the investigation status; collection and receipt; encounter, collector and ordering details when recorded; pregnancy and visual hemolysis; the confirmed identification with who confirmed it, the supervisor review and the comment; and a cancellation with its reason. Its CSV begins with a **Specimen** section (accession, MRN, type, eluate source, status, collection and receipt, final ID, who confirmed and reviewed it, and any cancellation) so the file still says which investigation it describes when opened elsewhere. For an eluate, both this report and the Clinical Identification worksheet print **Eluate of** with the red-cell specimen it was made from, and that specimen's MRN when it differs. Suspected and ruled-out rows follow the current analysis, not an older saved list. When a counted cell's check cell is 0, that ruled-out line names the cell and says the check cell did not react. A counted cell at CC 2+ keeps the count only. Requires a specimen.
2. **Panel Summary** — one lot’s identity, typing completeness and antigen grid. Requires a panel. If the lot has untyped cells, partially typed antigens, or a lot number stored on another panel, a **Data quality (review before use)** section lists them. These are the same checks the Worklist uses.
3. **Analysis Results** — the current analysis for one specimen, including pattern evidence when a panreactive or autoantibody pattern is present. When autoadsorption leaves cells reactive, that residual comparison is included. Suspected and ruled-out rows follow that analysis, then the same **Suggestions** as the Analysis tab, with warnings and REVIEW notes (hemolysis, expired reagent, transfusion requirements, patient typing that argues against a suspected antibody) first. An older saved list is not printed. When a counted cell's check cell is 0, that ruled-out line names the cell and says the check cell did not react. A counted cell at CC 2+ keeps the count only. Requires a specimen.
4. **All Specimens** — every specimen in the database.
5. **All Panels** — every panel lot, with its typing completeness. The name, lot, vendor, cell count, and expiration stay in their own columns, including when the name is long.
6. **Clinical Identification** — the identification report for one specimen, including the confirmed ID when you have confirmed one. A panreactive or autoantibody workup also lists the pattern evidence. Explain citations name the run, so a supporting cell on an autoadsorbed aliquot is not read as the untreated reaction. That wording is evidence, not a diagnosis. An ordinary alloantibody report does not add that block. Requires a specimen. When two or more antibodies are called, the interpretation includes a **Multiple-antibody separation** section. It shows each antibody's isolated reactive cells against the required number, marked **SHORT** when they fall short, or the exemption. After confirmation it checks the confirmed antibodies; before confirmation it checks the suspected antibodies that meet the ID rule.

Panel Summary, Panel Antigram, and each run on Clinical Identification also print what the vendor sheet records beyond the grid: **ENZYME-TREATED** for a treated lot, the product line and catalog number, the lot notes, and a **Cell details** list with each cell's donor ID, Rh phenotype and special types (for example *Cell 3: donor D-100; Rh R1R1; special types Yt(a-)*). Analysis reads the special types, so the printed lot shows them. A lot with none of these prints nothing extra. **Export CSV** of a Panel Summary starts with a **Panel** section (name, lot, vendor, expiration, status, enzyme treatment, product, catalog and lot notes) and ends each cell row with Donor, Rh and SpecialTypes columns. The All Specimens CSV adds the final identification in full (the printed column is shortened), and the All Panels CSV adds the Typing summary, so each CSV has the columns of its printed list.

Each run on Clinical Identification prints its extra-phase grades after CC: the phases configured in Preferences and any other phase graded on that run, so the printed worksheet shows every grade analysis read. **Export PDF** shrinks the text so the widest line fits the page (down to 4.5 point), and wraps a line that is still too wide, indenting the continuation, so no grade at the end of a long row is cut off at the page edge. **Print** wraps long lines the same way.

Panel Summary, Panel Antigram, and the antigen grid on Clinical Identification leave out an antigen that is NT on every cell. A cell that is NT in a column typed on another cell prints NT. A typed minus stays a minus. The stored panel order is unchanged, and Edit on the Panels tab still shows the full order.
7. **Panel Antigram** — the bench antigram for one lot. Requires a panel.

8. **Pending Work** — specimens and panels that still need attention. It does not ask for a specimen or a panel. A cancelled investigation is counted on its own line and is not listed as pending. When Preferences require supervisor review, confirmed identifications not yet reviewed are listed under **Confirmed — awaiting supervisor review**, and "No pending work" appears only when none remain. **Other Worklist items** then lists everything else the Worklist raises, with its kind, title, urgency and detail: overdue additional testing, expired reagent use, incomplete reactions, expiring and expired specimens and lots, untyped cells, duplicate lots, rule conflicts and data inconsistencies. "No pending work" is printed only when the Worklist is empty as well; otherwise the report says how many Worklist items still need attention.

**Find** filters the specimen list by accession, type, phenotype, notes, or final ID. **Specimen** or **Panel** appears only for the report types that need one.

## Send it out

- The Clinical Identification report describes the first eight Explain Analysis candidates in full (suspected and unresolved ones come first). Any further candidates are named on one line with their status, for example *4 more candidate(s) not described here: anti-c (Ruled out); …*, so the printed record lists every candidate the analysis considered.
- **Copy** puts the full preview on the clipboard.
- **Print** prints the preview. Ctrl+P does the same while this tab is open.
- The preview is rebuilt whenever you open the Reports tab, and Copy and Print rebuild the report from the current data at that moment, as Export PDF and Export CSV always have. A re-analysis, new grades or a confirmed identification recorded on another tab is therefore never printed from an older preview. If the selected specimen or lot was deleted meanwhile (for example on another workstation), Copy, Print and both exports say so in a *Report not issued* message, issue nothing, record nothing in the audit log, and reload the list.
- Specimen reports compute the current analysis. If that cannot be done, the Analysis Results and Specimen Summary reports print the suspected and ruled-out lists saved at the last Run Analysis under a *NOTE: The current analysis could not be computed* line with the reason and the save time, and the Specimen Summary CSV carries the same note as an *AnalysisNote* row. Run analysis again before relying on such a report. The Clinical Identification report is not built from saved lists at all: it shows *Error generating report* instead, and cannot be printed or exported until the analysis runs.
- Suspected antibodies are printed with their combined score, for example *anti-E  score 85.0%*. The score is the average of 1 minus the Fisher p-value and the share of reactive cells that carry the antigen. It ranks the evidence and is not a probability, which is why reports label it "score". The CSV column is headed Score.
- **Export CSV** saves a spreadsheet. Availability depends on the report type. Specimen and panel lists, and the single-specimen and single-panel summaries, export as CSV. Specimen Summary CSV uses the same current ruled-out list as the preview. When a counted cell's check cell is 0, the CheckCell column names that cell. A counted cell at CC 2+ leaves that column blank. A text value that a spreadsheet would run as a formula (one starting with =, +, -, @ or a tab) is written with a leading apostrophe, so opening the file cannot run it. Panel typing (+, ++, -) and numbers are written unchanged.
- The Specimen Summary, Analysis Results and Clinical Identification reports list **Rule-outs not counted** after the rule-outs, and the Specimen Summary CSV adds a **RuleoutsNotCounted** section (Antibody, Reason). Because a cell whose check cell did not react no longer counts, it appears in that section and the CSV's CheckCell column stays blank. Each line gives the antibody and the reason a nonreactive cell did not count (the treatment destroys the antigen, the aliquot was absorbed, autoadsorption after a recent transfusion or allogeneic transplant, or a check cell that did not react). It is the same list as **Special Panels** on the Analysis tab, so the printed record shows why an antibody is not ruled out and what to repeat.
- The Panel Summary and Panel Antigram reports print the lot's **Status**: Active; INACTIVE with the import date and format for a lot imported but not yet reviewed and activated; INACTIVE for a lot taken out of use; or EXPIRED with the date. A printed antigram of a lot that is not in use says so. The All Panels list and its CSV have a **Status** column (Active, Imported, inactive, Inactive, Expired). The All Specimens list and its CSV show each investigation's status: Cancelled, Confirmed, Not analyzed, Stale or Analyzed.
- **Export PDF** saves a PDF. Clinical Identification and Panel Antigram use a landscape page.

The preview is read-only. To change what it says, update the specimen, the panel, the grades, or the confirmed ID, then return to this tab. The preview refreshes when you change the report type or the selected specimen or panel. Changing the report updates the status line to name that report.

**Help** opens this chapter.

## Report record

Every **Print**, **Export PDF**, and **Export CSV** is written to the audit log. The record holds the report type, the output, the file name, the software version, and a SHA-256 fingerprint of exactly what was issued. Specimen reports also record the confirmed identification and supervisor review at that moment, and appear on the specimen's Timeline, for example *Report exported to PDF: Clinical Identification (A123.pdf); ID at issue: anti-E; fingerprint 3F2A…*. Panel reports appear in the panel's audit history. An export that fails writes no record.

If a Clinical Identification report was issued and the identification was later cleared and confirmed again, the specimen's status says **Reissue the Clinical Identification report** until a new one is printed or exported.

A Clinical Identification report for a specimen whose identification was ever cleared is marked in its header:
- **AMENDED REPORT** when an identification is confirmed again. Under the final identification, an **Amendment history** lists each earlier identification in order: what it was, who confirmed it and when, who cleared it and when, and the reason given.
- **PREVIOUS IDENTIFICATION WITHDRAWN** when no identification is confirmed now. The same history appears in the interpretation.

Below the history, **Superseded reports** lists each Clinical Identification report printed or exported before the identification was last cleared. Each entry shows when it was issued, how (print, PDF or CSV) and the file name, who issued it, the identification it carried, and the start of its fingerprint, so those copies can be found and recalled or marked superseded. Reports issued after the clear are not listed.

The history comes from the audit log, so the report and the log always agree.

---

# Analytics

Analytics counts confirmed antibody identifications over a date range. It uses only identifications a user confirmed with **Confirm ID**. Suspected rows that were never confirmed are not included. Cancelled investigations are left out of every figure on this tab: antibody counts, turnaround and workload.

## Date range

- **From** includes confirmations on or after that date.
- **To** includes confirmations on or before that date.
- **30 days**, **90 days**, and **12 months** set the range through today.
- **All time** includes every confirmed identification, including any that have no confirmation date.
- **Refresh** reloads the cards, charts, and grid. Opening the Analytics tab does the same, so a cancellation, reinstatement, review, edit or deletion made on another tab is counted.
- **Export CSV** saves the detail rows.
- **Help** opens this chapter.

The line under the toolbar names the range that is actually in use. Changing the range updates the status line to name that range and the confirmed-specimen count.

## What the numbers mean

- **Confirmed specimens** is how many specimens had a confirmation in the range.
- **Distinct IDs** is how many different specificities appear.
- **Total occurrences** counts each specificity once per specimen. A call of anti-E; anti-K adds one to anti-E and one to anti-K. Antigen names are case-sensitive, so anti-E and anti-e are counted separately; only the "anti-" prefix ignores capitals.
- **Most common** is the specificity with the highest occurrence count.

The two charts show counts by specificity and the trend over time. When the range has no confirmed identification, each chart shows that title and does not draw a negative count scale. The grid under the charts lists the same breakdown. Use the grid when you need the exact counts behind a bar.

## Drill-down

Select a row to list the investigations behind its count in the panel beside the antibody grid. Then choose **Open**, or double-click, to go to the investigation on the Specimens tab. You can select a row in:
- the antibody grid: the confirmed investigations with that specificity in the range, newest first, with who confirmed each;
- **Workload by technologist**: that person's confirmations and supervisor reviews in the range;
- **By ordering location**: the investigations received from that location in the range, marked Confirmed, Open, or Inactive.

Each list uses the same rules as the count it came from, so the number of rows matches the count. Changing the date range or choosing **Refresh** clears the list, because the old list may no longer match the new counts.

Analytics describes work this laboratory confirmed in the application. It is not a population frequency and it does not change any specimen.

## Turnaround

The purple and red cards measure workflow time for investigations **received** in the selected date range. Cancelled investigations are not counted.

- **Received → confirmed** is the median time from the received date to the confirmed identification, in days, with the 90th percentile and the number of investigations. When a received time is recorded on the specimen it is used exactly; otherwise the received date is counted from midnight and the figure is an upper bound.
- **Confirmed → reviewed** is the median time from confirmation to supervisor review, in hours, for investigations that have a review.
- **Overdue open investigations** counts active investigations with no confirmed identification that are older than the Preferences **Turnaround threshold (days)**, default 3. Age runs from the recorded receipt time (the date the specimen was entered when no receipt time is recorded), the same as the Worklist's overdue-testing item.
- **Warnings overridden** counts warnings a user chose to continue past in the range:
  - an identification confirmed with the analysis missing or out of date, testing still open, or the multiple-antibody separation criterion not met;
  - a specimen saved with an encounter number already on another patient's specimen, or a pregnancy recorded for a patient recorded as male.
  - **Show cases** lists them in the investigations list below, newest first, with the warnings that were overridden and who confirmed. Select one and choose **Open**.
  - Overrides are listed newest first, with the warnings and who continued past them.
  - A confirmation that was later cleared still counts, because the override happened; its detail starts "No longer the current identification."
  - **Export CSV** adds the list at the end.

The overdue list at the bottom shows each case, oldest first, with its current status. Select one and choose **Open** to go to it. **Export CSV** adds the turnaround figures and the overdue list after the antibody counts. The file contains accession numbers, so each export is written to the audit log with the file name and a SHA-256 fingerprint of the file. The 90th percentile uses the nearest-rank method.

## Workload by technologist

The table beside the overdue list shows work done in the selected date range by each set of initials.

- **Confirmed** counts identifications whose **Identified by** is these initials and whose confirmation date is in the range.
- **Multiple Ab** counts the confirmations that name more than one antibody, such as anti-E; anti-K.
- **Reviews** counts supervisor reviews recorded under these initials with a review date in the range.
- **Median TAT** is the median time from receipt to confirmation, in days, for this person's confirmations.

Initials are matched regardless of case. When the initials belong to a user in **Preferences > Users & roles**, the name is shown too. Cancelled investigations are not counted. **Export CSV** adds this table after the turnaround figures.

## By ordering location

The **By ordering location** tab beside **Workload by technologist** groups investigations **received** in the date range by the ordering location recorded on the specimen. Locations match regardless of case and surrounding spaces. Specimens with no ordering location are listed last as **(not recorded)**.

- **Received** is how many investigations came from that location.
- **Confirmed** is how many of them have a confirmed identification.
- **Open** is how many are active with no confirmed identification.
- **Median TAT** is the median time from receipt to confirmation, in days.

Cancelled investigations are not counted. **Export CSV** adds this table at the end.

## Activity by user

The **Activity by user** tab counts work recorded in the audit log, by Windows login and by the local date of each event in the range. It covers reaction entry and amendments, which initials-based workload cannot.

- **Entered** is reaction cells saved for the first time, or saved where every phase was still NT.
- **Changed** is reaction cells whose recorded result was replaced. The audit entry keeps the original value.
- **Specimens** is how many different specimens the user saved reactions on, confirmed, cleared, or reviewed.
- **Confirmed**, **Cleared**, and **Reviews** count identifications confirmed, identifications cleared (amendments), and supervisor reviews.
- **Overrides** counts the warnings this login continued past: identifications confirmed past a Confirm ID warning, and specimens saved past a warning. It is the per-person view of the **Warnings overridden** card, and is not added to the activity total. Select a user's row to list those cases in the investigations list below, with the warnings that were overridden; select a case and choose **Open** to go to it.

When the login belongs to a user in **Preferences > Users & roles**, the name and initials are shown with it. Saving a reaction grid without changing a cell writes nothing to the audit log, so it is not counted. **Export CSV** adds this table at the end.

---

# Search

The Search tab has two sections: **Investigations** and **Reagent cells by antigen**.

## Investigations

Type into the search box and press Enter or select **Search**. It matches accession, MRN, patient name, date of birth, final antibody, previous antibodies, final comment, the initials of whoever confirmed the identification, collector, locations, ordering provider, encounter number, and the name, manufacturer or lot number of any panel the specimen was tested on. To find every investigation tested on a lot (for example after a reagent recall or a lot problem), search for the lot number. Matching ignores case and finds partial text, so `mrn-12` finds MRN-1234. Every word must match, so `doe jka` finds Doe's anti-Jka investigation and nothing else.

**From** and **To** limit results to specimens received in that range, including both end dates. The received date is the received time recorded on the specimen, or the date it was entered when none is recorded, the same date Analytics counts by. The **Date** column shows it. A date range can be used without search text. An empty search is refused.

Results are newest first and show the encounter, the derived investigation status, and the panels tested (name, manufacturer and lot). Double-click a row or select **Open** to go to that specimen on the Specimens tab.

## Reagent cells by antigen


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

Each row is a panel, lot, and cell, followed by the stored antigen typing. A finished search brings the antigen used in the search into view on the result row. The panel, lot, and cell stay on screen. When several antigens are set, those columns are shown first and stay on screen with the panel, lot, and cell when the row is scrolled, so the earlier one stays beside the later one. A warehouse antigen that is blank on every matching cell is left off the row. Clearing the search, or searching on one antigen, restores the usual column order, including a warehouse column the matches left blank. The column headings stay visible while you scroll down the rows. The panel name and the lot are shown in full. A cell on a lot that is not in use is marked in the Lot column in red italics, for example *LOT-7 · inactive* or *LOT-3 · expired 2026-10-31*, and the result count says how many matches are on such lots. Choose selected cells from a lot in current use. A typed minus stays −. An antigen that was not tested shows NT, which is not the same as antigen-negative. A warehouse column that stays, because some matching cell types it, shows NT on a cell that was not typed for that antigen. An explicit ++ stays ++. Matching looks across the lots stored in the database, including extra warehouse antigens when a panel types them.

A − criterion matches only a typed minus. Cells stored as NT are left out of that search.

Opening the Search tab runs the antigen search and the investigation search behind the results on screen again, with the criteria and text they were run with, and keeps the selected row when it still matches. A lot deactivated or expired, or an investigation confirmed or cancelled, on another tab is therefore shown as it is now. Criteria you changed but have not searched with yet are left for you to run. Clear stops the antigen search from being run again.

Search does not change grades or confirm an identification. To test a cell you found, enter the grades on the Reactions tab for the specimen you are working up.

---

# Rules

Rules are optional, per-antibody overrides of the default rule-out settings. When a rule exists for an antibody, analysis uses that rule. When it does not, analysis uses **Default rule-outs** from Preferences.

## The list

- **Name** is the label you gave the rule. Selecting a rule updates the status line to name that rule.
- **Antibody** is the specificity it applies to. Capitals in "Anti-" do not matter, but the antigen is case-sensitive: a rule for anti-K does not apply to anti-k, and the same holds for C and c, E and e, and S and s.
- **Het OK** means a heterozygous antigen-positive cell may count toward the rule-out.
- **Add** creates a rule. Ctrl+N does the same while this tab is selected. **Edit** opens the selected rule. Changing what analysis reads (the antibody, exception antigen, zygosity, rule-out count or separation exemption) marks open analyses stale. Changing only the name or description is audited but does not, and the status line says so; saving with nothing changed says *unchanged; nothing was saved*. If the rule was deleted meanwhile (for example on another workstation), saving the edit says the rule no longer exists and saves nothing; refresh the list and add the rule again if it is still needed. **Import** and **Tools > Import Lab Configuration** follow the same rule: their status line says open analyses are stale only when the import changed something analysis reads (a rule's antibody, counts or exemptions, an added or removed rule, or a clinical Preference); an import that only renames rules or changes display Preferences says analyses stay current. **Delete** removes it after you confirm. **Refresh** reloads the list.
- Rules change which antibodies are ruled out. Once users are registered, adding, editing and deleting rules needs the Supervisor or Administrator role; other users see the reason and nothing changes.
- Every add, edit and delete is written to the audit log with all the rule's settings: name, antibody, exception antigen, heterozygous cells, minimum count and separation exemption. An edit keeps the old and new values, and a delete keeps the deleted rule. Saving an edit that changes nothing writes no entry.
- After a rule changes, open investigations analyzed before the change appear on the Worklist as **Stale analysis** until they are analyzed again. A changed clinical Preference has the same effect. Confirmed identifications are not reopened. HemoIQ orders the change against each analysis by the audit log, not by the clock, so a change made in the same second as an analysis, or while the clock repeats an hour at the end of daylight saving time, is still caught.
- **Help** opens this chapter.

## Export and import

**Export…** saves every rule to a JSON file, for example to copy a validated rule set to another HemoIQ installation or keep it with your procedure documents.

**Import…** reads such a file and changes nothing until you confirm:
1. HemoIQ checks the file and lists what it would do: rules to **add**, rules to **update** (with each changed setting, such as "rule-out cells 2 → 3"), rules that are **unchanged**, and entries it **refuses**, with the reason.
2. Rules are matched by antibody, so a file rule for anti-K updates your anti-K rule whatever it is named.
3. Entries are refused when the antibody is not a known antigen in its exact case, the name is missing, the count is outside 1–5, the exception antigen is unknown, a separation exemption lists an unknown antigen (the same check as the Rule dialog), or the file has two rules for one antibody.
4. When you confirm, the adds and updates are written together. Each is audited like a manual add or edit. If any write fails, none are kept.
5. Rules that are not in the file are kept; this import never deletes a rule. To copy rules and Preferences together, and optionally remove rules the file lacks, use **Tools > Import Lab Configuration…** (see Preferences help).
6. Importing needs the same role as editing rules. Open investigations analyzed before the import appear as **Stale analysis** on the Worklist.

## The detail pane

The right pane is read-only. It shows the name, antibody, exception antigen, whether heterozygous cells are allowed, the minimum rule-out count, and the description. Change those fields with **Edit**. The minimum rule-out count must be a whole number from 1 to 5; anything else is refused when you save. A rule saved by an earlier version with a count outside that range is listed as a problem, with the count analysis actually uses.

**Exception antigen** limits heterozygous permission to that antigen on the antibody named in the rule. A rule for anti-D with exception C does not let heterozygous C cells rule out anti-C. Leave the exception blank when the rule is only a count and a zygosity choice for its own antibody.

**Min Ruleout Count** replaces the default count for this antibody only. The Ruled Out tab shows that number in the **Needed** column and explains when the custom rule was used.

Rules apply the next time you run analysis. They do not rewrite a final ID you have already confirmed.

**Tools > Import Lab Configuration** checks your role before it changes anything. A role that can change neither rules nor Preferences (a Technologist) is told before choosing a file. A Supervisor may import rules, but a file that also changes Preferences, which need the Administrator role, is refused after its preview and before the confirmation; nothing is changed.

---

# Rule editor

This dialog adds or edits one custom rule-out rule.

- **Rule Name** is the label shown in the list.
- **Antibody** is the specificity. Pick one from the list or type a name such as anti-K.
  - The antigen must be a known antigen in its exact case. For example anti-fya is refused with "Did you mean anti-Fya?", because a rule that names no real antigen would never apply.
  - Each antibody can have only one rule, because only one is used for its rule-out count and separation settings. A second rule for the same antibody is refused and names the existing rule. Edit that rule instead.
  - anti-K and anti-k are different antibodies and can each have a rule.
- **Description** is a short note about why the laboratory uses this rule.
- **Exception Antigen** is optional. Pick or type an antigen when the rule depends on one.
- **Heterozygous OK** allows a heterozygous cell to count toward the rule-out. Leave it unchecked when only homozygous cells should count.
- **Min Ruleout Count** is how many qualifying antigen-positive nonreactive cells are required. The box starts at 3. Preferences limits the default count to 1–5; a custom rule replaces that default for this antibody.

- **Separation not required** exempts this antibody from the multiple-antibody separation criterion. Normally, when two or more antibodies are called together, each needs reactive cells that carry its antigen and are negative for the other called antigens.
- **Exempt from (antigens)** limits the exemption to the listed partner antigens, separated by commas. For example, anti-f with **e, c** (f-positive cells are always c-positive and e-positive). Leave it blank to exempt the antibody from every partner. Antigen names are case-sensitive: E and e are different.

**Save** stores the rule. The next analysis uses it for that antibody in place of the default count. **Cancel** discards the dialog. **Help** opens this chapter.

Delete a rule from the Rules tab if you want that antibody to return to the default.

---

# Preferences

Open **Tools → Preferences** to set laboratory defaults. **Save** writes them to this computer. **Cancel** discards the dialog. **Help** opens this chapter.

On a small screen the settings scroll; **Save**, **Cancel** and the other buttons stay at the bottom of the window.

## Identity and thresholds

- **Lab name** and **Department** identify the laboratory on this workstation.
- **Facility name**, **Facility address** and **Contact information** form the letterhead printed at the top of every report (preview, print and PDF). Type the address one line per line, and phone, fax or email one per line. Blank lines and extra spaces are dropped. Each is optional: anything left blank is left off the report, and with all three blank, reports start with the product line as before. Limits: name 120 characters, address 400, contact information 200.
- **Score threshold** is the minimum combined score for the suspected list. The allowed range is 0.30 to 0.95. Type it with a decimal point or a decimal comma (0.70 or 0,70).
- **ID cell rule** is how many antigen-positive reactive cells and antigen-negative nonreactive cells are required before a row is marked as meeting the identification rule. AABB 3 + 3 is the usual standard. The Suspected Antibodies tab turns those rows green.
- **Multiple-Ab separation** applies when two or more antibodies are called together. It is the number of reactive cells each antibody needs that carry its antigen and are negative for every other called antigen. For anti-e, anti-f, and anti-Lea, that means reactive e+ f- Lea- cells, reactive e- f+ Lea- cells, and reactive e- f- Lea+ cells. The range is 0 to 5; 0 turns the check off, and the default is 1. An antibody known from patient history needs no cells of its own, and a rule on the Rules tab can exempt an antibody. This is a suggested criterion: HemoIQ warns but does not block.
- **ACS rule-outs** is how many times each clinically significant antibody must be ruled out before the application can suggest that all clinically significant antibodies are ruled out. The range is 1 to 5. When every one is ruled out but some antibody still has a combined score of 0.95 or more, HemoIQ offers ACS with that antibody named as an exception. After **Run Analysis**, **Yes** only fills in **Final ID** and the comment (with the rule-out counts); the prompt says when that would replace what is already typed, and nothing is resulted until you review the fields and choose **Confirm ID**. The combined score is the average of 1 minus the Fisher p-value and the share of reactive cells that carry the antigen. It is a score for ranking evidence, not a probability, and reports word it that way.
- **ACS antigens added** lists the antigens, beyond the fixed core, that the all-ruled-out result also covers:
  - The core, which cannot be removed, is D, C, c, E, e, K, k, Fya, Fyb, Jka, Jkb, S and s.
  - The default additions are Lea, Leb, M and N. Add others your procedure requires, such as P1 or Kpa, or clear the box to use the core only.
  - Names are case-sensitive and must be known antigens; an unknown name is refused when you save.
  - The analysis report lists the antibodies evaluated, the rule trace records the list, and changing it marks open analyses as stale.
- **Default rule-outs** is the minimum number of qualifying antigen-positive nonreactive cells required to rule out an antibody when no custom rule exists. The range is 1 to 5. A rule on the Rules tab overrides this for that antibody.

## Specimens and the database

- **Default specimen type** pre-selects serum, plasma, or eluate on a new specimen.
- **Specimen dating (days)** pre-fills expiration when you add a specimen. 0 leaves the date blank. Three days is a typical antibody workup dating interval.
- **Expiration warning (days)** is how far ahead the Worklist and the lists mark a specimen or panel as expiring.
- **Require supervisor review before an investigation is Completed** adds a sixth status step. After an identification is confirmed, a second person must record **Supervisor Review…** on the Specimens tab before the investigation counts as Completed. Until then it shows *Awaiting supervisor review* and appears on the Worklist.
- **Dating look-back (days)** is 1–365, default 90. A transfusion this recent before collection makes the specimen-dating check apply (collection + Specimen dating days).
- **RhIG passive window (days)** is 1–365, default 84 (12 weeks). Anti-D within this many days after the recorded RhIG date is flagged for review as possibly passive.
- **Turnaround threshold (days)** is 1–60, default 3. Open investigations older than this are listed as overdue on Analytics, and those still awaiting additional testing appear as **Overdue testing** on the Worklist.
- **Recent transfusion (days)** is 1–365, default 90. If the specimen is Transfused and its last transfusion is this many days or fewer before the specimen date, the serologic phenotype is treated as possible donor cells and is not used against an alloantibody. The value is saved with each analysis snapshot.
- **Max database size (MB)** warns when the database file reaches 80 percent of this size. The allowed range is 50 to 10240 MB. The warning can offer to open Purge Database.
- **Default initials** pre-fills **Initials** on Confirm ID. Confirming an identification updates this value to the initials you used.
- **Show inactive specimens and panels by default** turns on **Show Inactive** when those tabs open.

## Phases and the reaction grid

- **Hide ruled-out antigen columns on the Reactions tab** starts Reactions with **Hide ruled-out** checked.
- **Extra phases** adds optional columns after the classic IS / 37°C / AHG / CC grid. Leave the box blank to keep the classic grid.
- **RT**, **PEG**, **Gel**, and **Solid** are suggested columns. Gel, Solid, and PEG count as IAT-like evidence. You can also type custom names.
- **Titer grid (Dil1–Dil128)** adds serial-dilution columns. The last reactive tube is the titer endpoint. Dilution grades are not IAT, and they do not rule out or identify an antibody.

Changes apply to the next analysis and to new specimens. They do not rewrite identifications you have already confirmed. Saving a new score threshold, ID cell rule, ACS rule-out count or antigens, default rule-out count, separation count, extra-phase list, recent-transfusion window, specimen dating days, dating look-back or RhIG passive window marks each unconfirmed analysis stale until you run analysis again, and the status line after **Save** says so. A save that changes only display or specimen-default settings says *Preferences saved.* alone. When the database is near its size limit, the warning stays on the same status line. Each such change is recorded in the audit log with the old and new values, whether made here or by importing Preferences. The lab name, department, facility letterhead, and specimen defaults do not.

## Users and roles

**Users & roles…** registers people by their Windows login, with initials, name, and a role:

| Role | Can also |
|---|---|
| Read-only | View specimens, panels, analyses, reports, Analytics and the audit log; print and export. Cannot change any laboratory data |
| Technologist | Everything not listed below: specimens, reactions, analysis, confirming an identification, cancelling, reports |
| Supervisor | Record supervisor review, clear a confirmed identification, reinstate a cancelled investigation, add, edit or delete analysis rules |
| Administrator | Everything a Supervisor can, plus change Preferences and manage users |

While **no user is registered**, everyone may do everything, as in earlier versions. Once the first user is saved, the rules apply to every Windows login. An unregistered login can then view but not change anything: it is treated like a Read-only user, so that every change is made by a registered person. To start, select **Use my login**, enter your initials, and save yourself as an **Administrator**. The first user must be an Administrator, the last active Administrator cannot be demoted or deactivated, and initials must be unique among active users.

The right end of the status bar shows who is signed in and their role, for example "Signed in: Tom Tech (TE) (Technologist)". When the session can only view (a Read-only user, or a login that is not registered), the label turns amber, reads "view only", and the buttons that change laboratory data are disabled. These buttons are Add, Edit, Delete, Attach and Cancel on Specimens; Add, Edit, Copy, Import, Activate and the extra-antigen buttons on Panels; Save, Clear, Fill negatives and the run buttons on Reactions; Run Analysis, Confirm ID and Clear on Analysis (an existing analysis is still shown; running one stores new results, so it needs a role that can record); and Add, Edit, Delete and Import on Rules. Hover over the label for details. The label updates when you close Preferences, so a role change takes effect immediately.

If the Preferences file cannot be written (read-only, locked by another program, or a full disk), **Save** says so and the window stays open with your edits. The Preferences saved before stay in effect, so analysis never runs on settings that were not saved or audited. An import that cannot be saved changes nothing, including its audit entries. The file is replaced only once the new copy is complete, so a full disk never leaves a damaged file that would reset Preferences to their defaults at the next start.

If the Preferences file exists but cannot be read at start-up (damaged, cut short, or locked), HemoIQ starts with the default Preferences and says so before anything else, naming the file. The unreadable file is first copied beside it as *settings.unreadable-<date-time>.json*, so a later save cannot overwrite the lab's settings; re-enter them from that copy, or import a Preferences or configuration export, before testing.

If the file holds an analysis setting outside its allowed range (for example a separation count of 9, or a score threshold below 0.30), HemoIQ replaces it at start-up and lists each one as *setting: stored → used*. Some are replaced by their default rather than the nearest limit. The file as it was is kept as *settings.adjusted-<date-time>.json*. Review the values in Preferences before testing. Imports never do this: an out-of-range value in an imported file is refused, not replaced.

Messages that name a next step follow your role. A Technologist asked to change a confirmed identification, or to edit a cancelled investigation, is told to ask a Supervisor or Administrator to clear or reinstate it, rather than to click a command the role refuses. **Clear** (Analysis), **Supervisor Review** and **Reinstate** (Specimens) check your role before asking for a reason or initials; a role that cannot take the step is told so, and who can, straight away. **Users & roles** opened by anyone but an Administrator shows the registered users with *(view only)* in the title and **Save user** disabled; the signed-in line and the button's tooltip give the reason. Before any user is registered, anyone may add the first one, who must be an Administrator. Preferences opened by a role that cannot change them show *(view only)* in the title, with **Save** and **Import…** disabled; hover over either for the reason. **Tools > Purge Database** tells a role that cannot purge before anything is chosen, and the start-up *Database nearly full* message asks such a user to have an Administrator purge, instead of offering the purge.

When users are registered, the initials entered for a supervisor review must be the signed-in user's own. Every audit entry records the signed-in Windows login, and user changes are audited.

## Export and import

**Export…** saves the laboratory-wide Preferences, as last saved, to a JSON file. Use it to set up a second workstation the same way, or to keep a copy with your procedure documents. The file covers:
- lab name and department;
- facility name, address and contact information;
- probability threshold;
- identification, ACS, separation and default rule-out counts, and the ACS antigens added;
- default specimen type;
- expiration warning, specimen dating, transfusion window, dating look-back and RhIG window;
- supervisor review and turnaround threshold;
- extra phases.

Personal and workstation settings are not included: default initials, Worklist filters, display toggles and the database size limit.

**Import…** reads such a file and changes nothing until you confirm:
1. HemoIQ lists each setting it would change, as old → new.
2. It also lists values it refuses, such as a count outside its allowed range, the wrong kind of value or a blank lab name, and names it does not transfer. Out-of-range values are refused, never adjusted.
3. When you confirm, the changes are applied and saved, and this window closes. Unsaved edits in the window are discarded.
4. Every import is written to the audit log. If analysis settings changed, open investigations analyzed before the import appear as **Stale analysis** on the Worklist, as after a manual change.
5. Importing needs the Administrator role once users are registered.

Analysis rules are exported and imported separately, on the Rules tab, or together with these Preferences (see below).

## Whole lab configuration

**Tools > Export Lab Configuration…** saves the rules and these Preferences together in one file. Analysis depends on both, so this is the safer way to set up a second workstation or restore one.

**Tools > Import Lab Configuration…** previews both sections, then applies them in one step:
- The preview shows the same rule and Preferences changes as the separate imports. It also lists your stored rules that are not in the file.
- If the file has any refused entry, nothing is applied, because a partial copy would not match the source configuration.
- You choose whether to keep or remove stored rules that are not in the file. Removing them makes your rules match the file exactly; each removal is audited.
- Everything is written together. If any part fails, for example because of a missing role, nothing changes.
- Importing needs the role for rule changes, and the Administrator role when Preferences change.

---

# Purge database

**Tools → Purge Database** removes old specimens so the database file can shrink. The application also offers this dialog at startup when the file is at or above 80 percent of the maximum size in Preferences.

The text at the top shows the current file size and the configured maximum.

## What to remove

- **Keep specimens from the last N days** deletes specimens older than that many days. The box starts at 365.
- **Delete specimens created before** a date you pick on the calendar.

The preview line states how many specimens match before you commit.

## Archive

**Create an archive of the purged data** is checked by default. Leave it checked unless you have another copy of the records. The archive holds specimen records and should be stored securely. The completion message includes the archive path. Open that file later with **Tools → Open Archive**. Each purge writes a new archive file; the suggested name includes the date and time. HemoIQ will not replace an existing file, because an earlier archive may be the only copy of specimens already purged. If you pick a file that exists, HemoIQ asks for a new name and nothing is purged.

**Purge** deletes the matching specimens after you confirm. Pressing Enter does not purge: click **Purge**, and in the confirmation the highlighted answer is **No**, so choose **Yes** deliberately. Every other confirmation that deletes data (a specimen, panel, rule, run, extra antigen, attached panel or reactions) also defaults to **No**. Once users are registered, only an Administrator can purge. If any matching specimen has a confirmed identification, an archive is required: without one, the purge is refused and nothing is deleted. The deletion and its audit entry are saved together. Panels that are still needed by remaining specimens stay. **Cancel** closes the dialog without deleting anything.

After a purge, every tab reloads. The status bar reports how many specimens were removed and the new database size. **Help** opens this chapter.

Only an Administrator can purge once users are registered. Any other role is told so when it opens Purge Database, before choosing a cutoff or an archive, and the start-up *Database nearly full* message asks it to have an Administrator purge.

---

# Open archive

**Tools → Open Archive** reads an archive created by Purge Database and can restore specimens that are no longer in the live database.

1. Click **Browse…** and choose the archive file. The path box shows the file you picked.
2. The summary states what the archive contains.
3. The grid lists each specimen with accession, type, created date, recorded evidence, and status. **Recorded** is the final identification when one was confirmed. Otherwise it is the antibodies stored with the specimen, plus DAT and notes. That text is the archive record, not a new identification. Status tells you whether that accession is already in the live database.
4. **Restore** copies missing specimens back into the database, including the saved analysis snapshot. That snapshot is the record of the last analysis, not a new identification. Restore stays disabled until an archive is loaded. A view-only session (a Read-only user or an unregistered login) can open and read an archive, but Restore stays disabled; the summary line ends with *View only:* and the reason, which also shows when you hover over Restore. Specimens that are already present are skipped and are not overwritten.
5. When the archive also contains panel lots that the live database does not have, those lots are restored with the specimens, including the saved antigen order. An order already stored for a lot that is still in the database is left as it is.

**Close** leaves the live database unchanged.

The completion message reports how many specimens were restored, how many were skipped because they were already present, and how many panels were restored. Every tab reloads after a successful restore. **Help** opens this chapter.

Restore copies archived rows exactly as stored. An older archive can hold values that HemoIQ now refuses to save. The completion message then shows a warning and lists them under **Review before use**:

- grades HemoIQ does not recognize, such as "pos";
- grades for cells the panel does not have, or for a panel that is no longer linked to the specimen;
- panel typing other than +, −, ++ or NT on a restored panel lot. Analysis reads such a value as not typed. Correct it with **Edit** on the Panels tab.

Nothing is changed or blocked; each line says what to review. The message shows up to eight lines. The full list is kept in the audit log entry for the restore.

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

The window lists the newest 5,000 events. When the log holds more, the count reads, for example, *Newest 5000 of 12000*, and a line under **Find** says so. **Find** narrows the listed events by accession, action, Windows login, record type, or reason. **Export CSV…** saves every event in the whole log that matches **Find** (all of them when Find is empty), newest first, not only the listed rows, and then says how many it wrote. The export itself is written to the audit log (action **export_data**) with the file name and a SHA-256 fingerprint of the file. A text value that a spreadsheet would run as a formula (one starting with =, +, -, @ or a tab) is written with a leading apostrophe, so opening the file cannot run it. Panel typing (+, ++, -) and numbers are written unchanged. **Close** returns to the main window. **Help** opens this chapter.

The log is a history of actions taken in the application. It is not the clinical report. Use **Reports** for the identification document, and use this dialog when you need to see who confirmed or cleared an ID and why.

---

# Demo data

**Tools → Load Demo Data** adds sample specimens, panels, and results so you can learn the screens. Existing data is not removed. The application asks you to confirm before it writes anything.

Demo specimens use realistic accession numbers and demo lots use real vendor names, so demo data is loaded only into an **empty database** or one that already holds only demo data. In a database with laboratory records it is refused and nothing is added. To try the demo alongside real work, start HemoIQ with a separate database file (set the `HEMOIQ_DB` environment variable to a new file name). The whole load is one step: if anything fails part-way, nothing from it is kept.

What the loader adds, when those records are not already present:

- 10 clinical specimens, accessions 2026-001 through 2026-010, on 5 shared panels.
- Enzyme (ficin) and absorption runs mixed into that workload.
- 11 DEMO- scenarios, including warehouse antigens with ficin and DTT runs.
- E2E- workups for warm allo, cold allo, warm and cold auto, mixed cold plus warm, combinations, exotic and HTLA patterns, a negative panel, and panreactivity with a negative autocontrol.

When it finishes, every tab refreshes. Open **Worklist** or **Specimens** and look for the 2026-, DEMO-, and E2E- accessions. You can enter grades, run analysis, and print reports against this data the same way you would for a real workup.

A DEMO- scenario that already has grades and has never been analyzed is analyzed when the application opens. The seeded grades and antigen typing stay as they are. Editing those grades afterward still marks the analysis stale.

Sample records are still stored in the same database as laboratory work. Delete individual DEMO- or 2026- specimens from the Specimens tab when you no longer want them, or exclude them by accession when you read Analytics.

**Help** for this action is this chapter. The menu item itself does not open a separate dialog before the confirmation prompt.

A view-only session (a Read-only user or an unregistered login) cannot load demo data, because it adds specimens and lots. It is told so before the confirmation, and nothing is added.

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
