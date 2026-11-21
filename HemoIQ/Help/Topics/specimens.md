# Specimens

The Specimens tab is where you keep accessions and the clinical context that analysis uses as evidence.

## List

The left pane lists accession number, type, final ID, expiration, created date, and whether the specimen is active. The accession and the expiration stay on screen in full.

- **Find** filters by accession, type, phenotype, previous antibodies, notes, DAT, or final ID.
- **Add** opens a blank specimen record. **Edit** opens the selected record. Double-click a row to edit it.
- **Delete** removes the selected specimen, with its panel links, reactions, and analysis, after you confirm. It is meant for a specimen entered in error. A specimen with a confirmed identification cannot be deleted; use **Cancel** on the investigation instead, or have a supervisor clear the identification first. A specimen named as the source of an eluate cannot be deleted until that link is removed. Each deletion is written to the audit log with the MRN, type, received date, linked panels, and how many reaction rows were removed.
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

**Visual hemolysis** in the specimen dialog records how the specimen looked when received: Not recorded, None, Slight, Moderate, or Gross.
- A grossly hemolyzed specimen always adds a **REVIEW** note to analysis. Hemolysis in the test tubes cannot be told apart from the specimen's own, and free hemoglobin can hide weak agglutination, so consider a new specimen.
- Slight or moderate hemolysis adds a note only when a hemolysis (H) grade was recorded, naming those cells.
- No grade is changed or excluded.
- Changes are audited and shown on the Timeline. The field is locked once an identification is confirmed. The identification worksheet prints it.

**Linked Panels** are the lots used for this workup. **Attach Panel** picks a lot. **Detach** removes the selected link. A panel with grades for this specimen cannot be detached, because analysis would keep using those grades: clear its reactions on the Reactions tab (with a reason) first. Linking a panel makes it available when you enter reactions.

**Pattern evidence** appears when the latest analysis has a panreactive, autoantibody, or high-prevalence note, including a cold or warm phase sentence and an adsorption comparison when one was run. When that analysis left a panreactive untreated run out of ordinary scores, that exclusion is listed first, and the adsorption comparison follows it. Residual reactivity after autoadsorption is visible with the suspected antibodies, ahead of the warm and pan sentences. A specimen with no suspected specificity and no such note says so. The wording is evidence, not a diagnosis. An ordinary alloantibody score stays in Suspected Antibodies and does not add this block.

**Suspected Antibodies** and **Ruled-out Antibodies** follow the same current analysis as Pattern evidence. An older saved list is not shown in their place. A cell whose partner antigen is NT does not rule an antibody out. When nothing meets the criteria, the ruled-out pane says so. When a counted cell's check cell is 0, the Check cell column names that cell and says the check cell did not react. A counted cell at CC 2+ leaves that column blank. Change them by entering grades and running analysis, not by typing in these grids.

## Specimen dating

**Collection** in Clinical Context shows the collection and receipt times. When the transfusion record shows a transfusion within the Preferences **Dating look-back** (default 90 days), or the patient is pregnant or was pregnant within that look-back, the specimen should not be used after collection date + **Specimen dating** days. A **REVIEW** note appears here, at the top of the analysis suggestions, and on the worksheet when:

- the recorded expiration is later than that date,
- no expiration is recorded, or
- no collection time is recorded, so dating cannot be checked.

The note is a prompt; HemoIQ never changes the expiration itself.

## Patient history

When the specimen has an MRN, the detail pane lists the patient's other specimens, newest first. Each line shows the date and encounter number, the confirmed identification, DAT, transfusion status, and the phenotype and genotype recorded on that specimen. An earlier phenotype can be the pre-transfusion typing to compare with when the current phenotype is unreliable after a transfusion; HemoIQ shows it for review and does not use it in analysis. Specimen Summary and the identification worksheet print the same list. Search and the specimen filter match MRN.

## Patient record

The **Patient (permanent, shared by MRN)** box shows information that belongs to the patient rather than to this specimen: name, date of birth, sex, antigen-negative units, other transfusion requirements, and permanent blood bank comments. Select **Edit Patient** to change it. The button needs an MRN on the specimen. Every change is written to the audit log with the old and new values. A confirmed identification on one specimen does not lock the patient record, because the record spans all of the patient's specimens.

Enter antigen-negative requirements in **Antigen-negative units** as antigen codes, for example `E, K, Fya` or `Fy(a-)`. A trailing `-`, `neg`, or `negative` is accepted. Capital and lower-case letters are different antigens (C is not c, K is not k). HemoIQ stores the list in panel order and prints it as, for example, E−, K−, Fy(a−). A word that is not an antigen, such as `irradiated`, is refused; write other requirements such as irradiated, CMV-safe, or washed in **Transfusion requirements**.

When an antibody is known for the MRN (Previous antibodies, a confirmed call on this specimen, or a confirmed call on another specimen with the same MRN) and its antigen is not in **Antigen-negative units**, analysis lists a **REVIEW** suggestion at the top. If the requirement is written only in the free-text **Transfusion requirements** (for example from before this field existed), the suggestion asks you to add it to **Antigen-negative units** so it is explicit. HemoIQ never copies it for you. It is a prompt to check the record, not a transfusion order.

**Sex** is Not recorded, Female, Male, or Other / X. It is shown after the date of birth wherever the patient is named, including reports.
- If a specimen records **Pregnant** or **Recently pregnant** for a patient recorded as male, saving the specimen asks for confirmation, and analysis lists a **REVIEW** suggestion. The specimen may belong to another patient, or one of the records may be wrong.
- The pregnancy context still applies to specimen dating until someone corrects it. Nothing is changed automatically.

**HPC transplant** records a stem-cell or marrow transplant (None, Autologous, or Allogeneic) and its date.
- **Allogeneic:** for specimens collected on or after the transplant date, analysis treats this patient's serologic phenotype as unreliable, as it does after a recent transfusion. Red cells may come from the donor or be mixed. The genotype is also listed as possibly the donor's: DNA from blood or marrow after an allogeneic transplant is the donor's, so the genotype counts for the patient only if it was done before the transplant or on a non-hematopoietic sample such as a buccal swab. If no date is recorded, every specimen is treated this way.
- **Autologous:** typing is unaffected, because the cells are the patient's own.
- **Where it appears:** the patient box, the Clinical Identification report, and the patient-typing notes on Analysis.
- **What it never does:** add or remove an antibody.

## Investigation status

The grey strip at the top of the detail pane shows where the investigation stands. It is worked out from what has been recorded, so it cannot fall out of date:

- **Steps:** ✓ Received, Panel attached, Reactions complete, Analysis current, ID confirmed. ○ marks a step that is not done. **Analysis current** follows the same rule as the Analysis tab warning and the Worklist: it clears when grades, analysis rules or clinical Preferences change after the last analysis.
- **Status:** Awaiting panel, Testing in progress, Analysis needed, **Awaiting additional testing**, Awaiting identification, Completed, **Amended** (an identification was cleared and confirmed again), **Reopened** (cleared and not yet confirmed again), or Inactive – not completed.
- **Next:** the specific next action, for example `Finish reactions: Panel A (1/2).`

**Receipt** in Clinical Context is a checklist of what to record when the specimen arrives. It reads **Complete (8 of 8)** or lists what is missing, and hovering over it gives each item's reason.
- **Required:** patient MRN, collection date/time, transfusion status, and the last transfusion date when the patient was transfused. These change how the analysis is read: patient history needs the MRN, and the transfusion window and specimen dating count from collection. While one is missing, the line is shaded amber, the **Received** step stays open, and the next action asks you to complete receipt with **Edit** before attaching a panel.
- **Advisory:** received date/time, visual hemolysis, pregnancy status and DAT. These complete the record but do not hold the Received step.
- **Patient history:** when another specimen with the same MRN records a red-cell transfusion within the recent-transfusion window, and this specimen says Not transfused or Unknown, **Transfusion status matches patient history** becomes a required gap. The same conflict appears as a REVIEW note on the analysis. A Transfused record whose date is older than the one on file adds an advisory item.
- The checklist never blocks testing. The Clinical Identification worksheet prints the same line.

**Awaiting additional testing** means the analysis is current but left questions that more testing could settle. Each analysis saves the open items, and **Next** lists them:
- clinically significant antibodies the panels type that are not yet ruled out;
- suspected antibodies that have not met the identification rule;
- multiple-antibody separation shortfalls;
- up to three recommended selected cells.

An antibody the patient's own reliable typing or genotype argues against is not listed.

Some reaction patterns change what is listed:
- **Panreactive or autoantibody-like:** **Next** points to autocontrol, DAT and adsorption instead of a list of antibodies and cells, because selected cells cannot separate specificities when every cell reacts.
- **High-prevalence or HTLA:** **Next** points to rare antigen-negative cells, titration or neutralization, with the recommended cells. This is a suggestion, not a block: you can still record the identification. Analyses saved before this version show **Awaiting identification** until analysis is run again.

The identification worksheet prints the same status line.

### Cancel and reinstate

Use **Cancel Investigation…** to stop an investigation without deleting it, for example for a mislabeled or hemolyzed specimen or a cancelled order. A reason is required. A confirmed identification must be cleared first. A cancelled investigation:

- shows status **Cancelled** with the date and reason,
- is locked: reactions, panels, analysis saves, and confirming an identification are refused,
- is removed from every Worklist queue.

**Reinstate…** reopens it and also requires a reason. Both actions are written to the audit log and appear on the Timeline.

### Supervisor review

When Preferences requires supervisor review, a sixth step, **Supervisor reviewed**, appears. Select **Supervisor Review…** and enter your initials. Rules:

- There must be a confirmed identification.
- The reviewer cannot be the person who confirmed it (initials are compared without regard to case).
- A specimen can be reviewed once. Clearing or re-confirming the identification removes the review, so the new call must be reviewed again.

Each review is written to the audit log with the confirmed antibodies and who confirmed them. The worksheet prints *Supervisor reviewed by …* or *Supervisor review: PENDING*.

## Timeline

Expand **Timeline** to see the investigation in time order, with who did each step where it is known:

- Specimen received, panel attached or removed, panel run started, and additional testing (ficin, papain, DTT, adsorption runs) started. **Specimen received** uses the received time recorded on the specimen, or the date it was entered when none is recorded. When the specimen was entered on a different day (for example from a downtime log), a separate **Specimen entered in HemoIQ** line shows that date.
- Reactions entered. Saves on the same run less than 15 minutes apart are combined, for example *3 save(s), 2 cell(s) on Panel A, Untreated*.
- Each analysis run, with the rule-engine version.
- Identification confirmed, cleared (with the reason), and supervisor review.
- Changes to MRN, transfusion record, DAT, visual hemolysis, and the patient record.
- Lab configuration changes made while the investigation was open: analysis rules added, changed or deleted, and changed clinical Preferences. Each entry names what changed, for example *Analysis rule changed: Jka dosage; minimum rule-out cells 3 → 2*. "Open" means after receipt, before an identification is confirmed or the investigation is cancelled, and again after one is cleared or reinstated. An analysis run before such an entry used the previous setting. The Worklist lists the investigation as **Stale analysis** until it is analyzed again.

Receipt and run starts record only a date, so they show first on their day. The identification worksheet prints the same timeline. Panels attached before this version were not audited, so only their run start dates appear.

## Eluate evidence

Enter an eluate as its own specimen with Type **eluate**, set **Eluate of** to the source accession, attach a panel, and grade it like any panel. **Eluate link** in Clinical Context shows *Eluate of X* on the eluate and *Eluates: …* on the source.

When the source specimen is analyzed, each linked eluate adds a note:

- **No reactions entered yet** — the eluate is linked but not tested.
- **Specificity** — the eluate's confirmed identification, or its suspected antibodies from its own analysis, with reactive and evaluated cell counts. After a recent transfusion, the note says this supports alloantibody coating transfused donor cells (possible delayed hemolytic or serologic transfusion reaction). An antibody in the eluate but not suspected in the source is named, because it may be bound to the red cells.
- **Panreactive** — four or more evaluated cells, at least 80% reactive. This supports IgG autoantibody; adsorption is suggested before ruling out an alloantibody after transfusion.
- **Nonreactive** — three or more evaluated cells, none reactive. With a positive DAT, the note lists drug-related antibodies, antibodies to antigens not on the panel, and complement-only coating. With a C3-only DAT, a nonreactive eluate is expected.

These notes never add, remove, or rule out an antibody.
