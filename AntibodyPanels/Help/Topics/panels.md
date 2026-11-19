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
