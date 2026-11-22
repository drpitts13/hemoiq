# Demo data

**Tools → Load Demo Data** adds sample specimens, panels, and results so you can learn the screens. Existing data is not removed. The application asks you to confirm before it writes anything.

Demo specimens use realistic accession numbers and demo lots use real vendor names, so demo data is loaded only into an **empty database** or one that already holds only demo data. In a database with laboratory records it is refused and nothing is added. To try the demo alongside real work, start HemoIQ with a separate database file (set the `HEMOIQ_DB` environment variable to a new file name). The whole load is one step: if anything fails part-way, nothing from it is kept.

What the loader adds, when those records are not already present:

- 10 clinical specimens, accessions 2026-001 through 2026-010, on 5 shared panels.
- Enzyme (ficin) and absorption runs mixed into that workload.
- 11 DEMO- scenarios, including warehouse antigens with ficin and DTT runs.
- E2E- workups for warm allo, cold allo, warm and cold auto, mixed cold plus warm, combinations, exotic and HTLA patterns, a negative panel, and panreactivity with a negative autocontrol.

When it finishes, every tab refreshes. Open **Worklist** or **Specimens** and look for the 2026-, DEMO-, and E2E- accessions. You can enter grades, run analysis, and print reports against this data the same way you would for a real workup.

A DEMO- scenario that already has grades and has never been analyzed is analyzed when the application opens. The seeded grades and antigen typing stay as they are. Editing those grades afterward still marks the analysis stale.

Sample records are still stored in the same database as laboratory work. Delete individual DEMO- or 2026- specimens from the Specimens tab when you no longer want them, or exclude them by accession when you read Analytics.

**Help** for this action is this chapter. The menu item itself does not open a separate dialog before the confirmation prompt.

A view-only session (a Read-only user or an unregistered login) cannot load demo data, because it adds specimens and lots. It is told so before the confirmation, and nothing is added.
