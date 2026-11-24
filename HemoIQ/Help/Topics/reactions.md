# Reactions

The Reactions tab is where you record IS, 37°C, AHG, and CC grades for each cell, plus any extra phases configured in Preferences. A phase that was graded on the run but has since been removed from Preferences keeps its column, headed *(stored)*, because analysis still reads those grades.

## Choose the work

- **Find** filters the specimen list by accession, type, phenotype, previous antibodies, notes, or final ID.
- **Specimen** and **Panel** pick the accession and the lot. Grades load when you change specimen, panel, or run.
- **Run** selects which set of grades you are editing. The first run is the untreated panel. **+ Run** adds a treated or adsorbed run. **Delete Run** removes the run you are viewing. If it holds unsaved grades, the question says they are discarded too; after the delete the tab moves to the next run without asking about them again. Changing the run updates the status line to name that run.

## While you enter grades

- **Compare** shows another run beside this one. Pick that run in the second list. Grade columns are named for the two runs. Rows that differ are highlighted, the Changed box is checked, and extra phases are included. The Changed box is display only.
- **Hide ruled-out** hides antigen columns already ruled out for this specimen. The setting can also be turned on by default in Preferences.
- **Show dosage** groups columns by blood-group system and marks homozygous ++, heterozygous +, or +/? when the antithetical antigen was not typed. This is display only. It does not change rule-out.
- **Load Reactions** reloads grades from the database.
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
