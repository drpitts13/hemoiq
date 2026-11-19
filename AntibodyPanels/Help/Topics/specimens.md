# Specimens

The Specimens tab is where you keep accessions and the clinical context that analysis uses as evidence.

## List

The left pane lists accession number, type, final ID, expiration, created date, and whether the specimen is active. The accession and the expiration stay on screen in full.

- **Find** filters by accession, type, phenotype, previous antibodies, notes, DAT, or final ID.
- **Add** opens a blank specimen record. **Edit** opens the selected record. Double-click a row to edit it.
- **Delete** removes the selected specimen after you confirm.
- **Refresh** reloads the list.
- **Show Inactive** includes specimens that were marked inactive.
- **Help** opens this chapter.

Row colors: green when a final ID is confirmed (the tooltip shows the confirmed antibodies), yellow when the specimen is nearing expiration, and red when it is expired.

## Details

The right pane follows the selected specimen. Selecting a specimen updates the status line to name that accession.

- **Enter Reactions** opens Reactions for this accession.
- **Analyze** opens Analysis.
- **Clinical Report** opens Reports on the clinical identification report.

**Clinical Context** shows phenotype, genotype, previous antibodies, DAT, notes, and the confirmed final ID. These fields are edited from the specimen record.

**Linked Panels** are the lots used for this workup. **Attach Panel** picks a lot. **Detach** removes the selected link. Linking a panel makes it available when you enter reactions.

**Pattern evidence** appears when the latest analysis has a panreactive, autoantibody, or high-prevalence note, including a cold or warm phase sentence and an adsorption comparison when one was run. When that analysis left a panreactive untreated run out of ordinary scores, that exclusion is listed first, and the adsorption comparison follows it. Residual reactivity after autoadsorption is visible with the suspected antibodies, ahead of the warm and pan sentences. A specimen with no suspected specificity and no such note says so. The wording is evidence, not a diagnosis. An ordinary alloantibody score stays in Suspected Antibodies and does not add this block.

**Suspected Antibodies** and **Ruled-out Antibodies** follow the same current analysis as Pattern evidence. An older saved list is not shown in their place. A cell whose partner antigen is NT does not rule an antibody out. When nothing meets the criteria, the ruled-out pane says so. When a counted cell's check cell is 0, the Check cell column names that cell and says the check cell did not react. A counted cell at CC 2+ leaves that column blank. Change them by entering grades and running analysis, not by typing in these grids.
