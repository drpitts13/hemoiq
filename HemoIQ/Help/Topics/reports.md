# Reports

The Reports tab previews a text report and can copy, print, or export it. Every report begins with the facility letterhead set in **Preferences** (facility name, address and contact information), then the product name, version, and intended-use statement. Any part of the letterhead that is not set is left off; with none set, the report starts with the product name. The intended-use statement stays on the preview.

## Choose a report

The **Report** list has eight types:

1. **Specimen Summary** — one accession, its clinical context, linked panels, and final ID. Suspected and ruled-out rows follow the current analysis, not an older saved list. When a counted cell's check cell is 0, that ruled-out line names the cell and says the check cell did not react. A counted cell at CC 2+ keeps the count only. Requires a specimen.
2. **Panel Summary** — one lot’s identity, typing completeness and antigen grid. Requires a panel. If the lot has untyped cells, partially typed antigens, or a lot number stored on another panel, a **Data quality (review before use)** section lists them. These are the same checks the Worklist uses.
3. **Analysis Results** — the current analysis for one specimen, including pattern evidence when a panreactive or autoantibody pattern is present. When autoadsorption leaves cells reactive, that residual comparison is included. Suspected and ruled-out rows follow that analysis. An older saved list is not printed. When a counted cell's check cell is 0, that ruled-out line names the cell and says the check cell did not react. A counted cell at CC 2+ keeps the count only. Requires a specimen.
4. **All Specimens** — every specimen in the database.
5. **All Panels** — every panel lot, with its typing completeness. The name, lot, vendor, cell count, and expiration stay in their own columns, including when the name is long.
6. **Clinical Identification** — the identification report for one specimen, including the confirmed ID when you have confirmed one. A panreactive or autoantibody workup also lists the pattern evidence. Explain citations name the run, so a supporting cell on an autoadsorbed aliquot is not read as the untreated reaction. That wording is evidence, not a diagnosis. An ordinary alloantibody report does not add that block. Requires a specimen. When two or more antibodies are called, the interpretation includes a **Multiple-antibody separation** section. It shows each antibody's isolated reactive cells against the required number, marked **SHORT** when they fall short, or the exemption. After confirmation it checks the confirmed antibodies; before confirmation it checks the suspected antibodies that meet the ID rule.

Panel Summary, Panel Antigram, and the antigen grid on Clinical Identification leave out an antigen that is NT on every cell. A cell that is NT in a column typed on another cell prints NT. A typed minus stays a minus. The stored panel order is unchanged, and Edit on the Panels tab still shows the full order.
7. **Panel Antigram** — the bench antigram for one lot. Requires a panel.

8. **Pending Work** — specimens and panels that still need attention. It does not ask for a specimen or a panel. A cancelled investigation is counted on its own line and is not listed as pending. When Preferences require supervisor review, confirmed identifications not yet reviewed are listed under **Confirmed — awaiting supervisor review**, and "No pending work" appears only when none remain.

**Find** filters the specimen list by accession, type, phenotype, notes, or final ID. **Specimen** or **Panel** appears only for the report types that need one.

## Send it out

- The Clinical Identification report describes the first eight Explain Analysis candidates in full (suspected and unresolved ones come first). Any further candidates are named on one line with their status, for example *4 more candidate(s) not described here: anti-c (Ruled out); …*, so the printed record lists every candidate the analysis considered.
- **Copy** puts the full preview on the clipboard.
- **Print** prints the preview. Ctrl+P does the same while this tab is open.
- The preview is rebuilt whenever you open the Reports tab, and Copy and Print rebuild the report from the current data at that moment, as Export PDF and Export CSV always have. A re-analysis, new grades or a confirmed identification recorded on another tab is therefore never printed from an older preview.
- Suspected antibodies are printed with their combined score, for example *anti-E  score 85.0%*. The score is the average of 1 minus the Fisher p-value and the share of reactive cells that carry the antigen. It ranks the evidence and is not a probability, which is why reports label it "score". The CSV column is headed Score.
- **Export CSV** saves a spreadsheet. Availability depends on the report type. Specimen and panel lists, and the single-specimen and single-panel summaries, export as CSV. Specimen Summary CSV uses the same current ruled-out list as the preview. When a counted cell's check cell is 0, the CheckCell column names that cell. A counted cell at CC 2+ leaves that column blank. A text value that a spreadsheet would run as a formula (one starting with =, +, -, @ or a tab) is written with a leading apostrophe, so opening the file cannot run it. Panel typing (+, ++, -) and numbers are written unchanged.
- The Specimen Summary, Analysis Results and Clinical Identification reports list **Rule-outs not counted** after the rule-outs, and the Specimen Summary CSV adds a **RuleoutsNotCounted** section (Antibody, Reason). Because a cell whose check cell did not react no longer counts, it appears in that section and the CSV's CheckCell column stays blank. Each line gives the antibody and the reason a nonreactive cell did not count (the treatment destroys the antigen, the aliquot was absorbed, autoadsorption after a recent transfusion or allogeneic transplant, or a check cell that did not react). It is the same list as **Special Panels** on the Analysis tab, so the printed record shows why an antibody is not ruled out and what to repeat.
- The Panel Summary and Panel Antigram reports print the lot's **Status**: Active; INACTIVE with the import date and format for a lot imported but not yet reviewed and activated; INACTIVE for a lot taken out of use; or EXPIRED with the date. A printed antigram of a lot that is not in use says so. The All Panels list and its CSV have a **Status** column (Active, Imported, inactive, Inactive, Expired). The All Specimens list and its CSV show each investigation's status: Cancelled, Confirmed, Not analyzed, Stale or Analyzed.
- **Export PDF** saves a PDF. Clinical Identification and Panel Antigram use a landscape page.

The preview is read-only. To change what it says, update the specimen, the panel, the grades, or the confirmed ID, then return to this tab. The preview refreshes when you change the report type or the selected specimen or panel. Changing the report updates the status line to name that report.

**Help** opens this chapter.

## Report record

Every **Print**, **Export PDF**, and **Export CSV** is written to the audit log. The record holds the report type, the output, the file name, the software version, and a SHA-256 fingerprint of exactly what was issued. Specimen reports also record the confirmed identification and supervisor review at that moment, and appear on the specimen's Timeline, for example *Report exported to PDF: Clinical Identification (A123.pdf); ID at issue: anti-E; fingerprint 3F2A…*. Panel reports appear in the panel's audit history. An export that fails writes no record.

If a Clinical Identification report was issued and the identification was later cleared and confirmed again, the specimen's status says **Reissue the Clinical Identification report** until a new one is printed or exported.

A Clinical Identification report for a specimen whose identification was ever cleared is marked in its header:
- **AMENDED REPORT** when an identification is confirmed again. Under the final identification, an **Amendment history** lists each earlier identification in order: what it was, who confirmed it and when, who cleared it and when, and the reason given.
- **PREVIOUS IDENTIFICATION WITHDRAWN** when no identification is confirmed now. The same history appears in the interpretation.

Below the history, **Superseded reports** lists each Clinical Identification report printed or exported before the identification was last cleared. Each entry shows when it was issued, how (print, PDF or CSV) and the file name, who issued it, the identification it carried, and the start of its fingerprint, so those copies can be found and recalled or marked superseded. Reports issued after the clear are not listed.

The history comes from the audit log, so the report and the log always agree.
