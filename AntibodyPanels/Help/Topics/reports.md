# Reports

The Reports tab previews a text report and can copy, print, or export it. Every report begins with the product name, version, and intended-use statement. That statement stays on the preview.

## Choose a report

The **Report** list has eight types:

1. **Specimen Summary** — one accession, its clinical context, linked panels, and final ID. Suspected and ruled-out rows follow the current analysis, not an older saved list. When a counted cell's check cell is 0, that ruled-out line names the cell and says the check cell did not react. A counted cell at CC 2+ keeps the count only. Requires a specimen.
2. **Panel Summary** — one lot’s identity and antigen grid. Requires a panel.
3. **Analysis Results** — the current analysis for one specimen, including pattern evidence when a panreactive or autoantibody pattern is present. When autoadsorption leaves cells reactive, that residual comparison is included. Suspected and ruled-out rows follow that analysis. An older saved list is not printed. When a counted cell's check cell is 0, that ruled-out line names the cell and says the check cell did not react. A counted cell at CC 2+ keeps the count only. Requires a specimen.
4. **All Specimens** — every specimen in the database.
5. **All Panels** — every panel lot. The name, lot, vendor, cell count, and expiration stay in their own columns, including when the name is long.
6. **Clinical Identification** — the identification report for one specimen, including the confirmed ID when you have confirmed one. A panreactive or autoantibody workup also lists the pattern evidence. Explain citations name the run, so a supporting cell on an autoadsorbed aliquot is not read as the untreated reaction. That wording is evidence, not a diagnosis. An ordinary alloantibody report does not add that block. Requires a specimen.
7. **Panel Antigram** — the bench antigram for one lot. Requires a panel.

Panel Summary, Panel Antigram, and the antigen grid on Clinical Identification leave out an antigen that is NT on every cell. A cell that is NT in a column typed on another cell prints NT. A typed minus stays a minus. The stored panel order is unchanged, and Edit on the Panels tab still shows the full order.
8. **Pending Work** — specimens and panels that still need attention. It does not ask for a specimen or a panel.

**Find** filters the specimen list by accession, type, phenotype, notes, or final ID. **Specimen** or **Panel** appears only for the report types that need one.

## Send it out

- **Copy** puts the full preview on the clipboard.
- **Print** prints the preview. Ctrl+P does the same while this tab is open.
- **Export CSV** saves a spreadsheet. Availability depends on the report type. Specimen and panel lists, and the single-specimen and single-panel summaries, export as CSV. Specimen Summary CSV uses the same current ruled-out list as the preview. When a counted cell's check cell is 0, the CheckCell column names that cell. A counted cell at CC 2+ leaves that column blank.
- **Export PDF** saves a PDF. Clinical Identification and Panel Antigram use a landscape page.

The preview is read-only. To change what it says, update the specimen, the panel, the grades, or the confirmed ID, then return to this tab. The preview refreshes when you change the report type or the selected specimen or panel. Changing the report updates the status line to name that report.

**Help** opens this chapter.
