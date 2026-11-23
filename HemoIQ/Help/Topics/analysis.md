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
