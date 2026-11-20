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

Confirm only after you agree with the evidence. The software does not confirm an identification for you.
