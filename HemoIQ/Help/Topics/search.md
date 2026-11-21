# Search

The Search tab has two sections: **Investigations** and **Reagent cells by antigen**.

## Investigations

Type into the search box and press Enter or select **Search**. It matches accession, MRN, patient name, date of birth, final antibody, previous antibodies, final comment, the initials of whoever confirmed the identification, collector, locations, ordering provider, encounter number, and the name, manufacturer or lot number of any panel the specimen was tested on. To find every investigation tested on a lot (for example after a reagent recall or a lot problem), search for the lot number. Matching ignores case and finds partial text, so `mrn-12` finds MRN-1234. Every word must match, so `doe jka` finds Doe's anti-Jka investigation and nothing else.

**From** and **To** limit results to specimens received in that range, including both end dates. A date range can be used without search text. An empty search is refused.

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

Search does not change grades or confirm an identification. To test a cell you found, enter the grades on the Reactions tab for the specimen you are working up.
