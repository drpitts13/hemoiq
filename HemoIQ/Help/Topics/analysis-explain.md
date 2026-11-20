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
