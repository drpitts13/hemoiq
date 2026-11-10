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

Select a row. The lower grid lists the cells that counted toward that rule-out, with the panel and the grades. A cell counts when it is antigen-positive, nonreactive in the phases that count, and allowed by the zygosity rule. Homozygous cells are the usual standard. **Heterozygous OK** on a custom rule allows a heterozygous cell to count for that antibody.

**Cells not counted for dosage.** For antigens with an antithetical partner (for example Jka/Jkb), a nonreactive cell counts only when it is homozygous, unless heterozygous cells are allowed. A cell is treated as heterozygous when the partner is typed +, and as of **unknown zygosity** when the partner is not typed. It never counts as homozygous. These cells are now named rather than silently skipped:
- When no cell counts, the antibody still gets a row. Its explanation reads, for example, "Anti-Jka cannot currently be ruled out because the only nonreactive Jka-positive cells are heterozygous (cells 1, 2), and heterozygous cells are not allowed for this antibody (dosage)", and says what to test next.
- When some cells count but not enough, the explanation adds "Not counted for dosage:" with the heterozygous and unknown-zygosity cells.
- The same sentences appear in the analysis suggestions.
- On the grid, a row with no counted cell has the status **Blocked by dosage**. Selecting any row lists all its nonreactive antigen-positive cells: first the counted ones (**Counted: Yes**), then those left out (**No (dosage)**). The Zygosity column says Homozygous, Heterozygous, or Unknown (for example "Unknown (Jkb not typed)").
- **Explain** lists the same cells, each marked "not counted: heterozygous" or "not counted: zygosity unknown", and gives such an antibody the status **Not ruled out (dosage)**.

Antigen-positive cells that reacted do not rule the antibody out. w+, mixed field, and hemolysis are reactive, so they do not count as rule-outs. Cells whose antigen was destroyed by ficin, papain, or DTT are listed on **Special Panels** as gated rule-outs instead of counting here.

The specimen detail pane on the Specimens tab repeats the ruled-out list as a summary. This tab is the one that shows the cells behind each count.
