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
