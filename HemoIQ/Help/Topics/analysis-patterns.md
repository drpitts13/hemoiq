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
