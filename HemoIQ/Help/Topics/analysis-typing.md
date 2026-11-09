# Patient Typing

Patient Typing lines each candidate up against the phenotype, genotype, and antibody history stored on the specimen.

The note on the tab states the rule: patient phenotype and historical antibodies are evidence for or against a candidate. Recent transfusion makes the phenotype uninterpretable. Record the transfusion status and last-transfusion date on the specimen. A **[Transfusion]** line in Summary states which record decided this: the structured date and its day count against the Preferences window, or the Notes keyword check when the status is Unknown. This tab is not a diagnosis.

Columns:

- **Antibody** — the candidate.
- **Kind** — which piece of patient information was used, such as phenotype or previous antibody.
- **Patient** — the value on the specimen, such as antigen-negative or a historical antibody name.
- **Explanation** — how that value supports or argues against the candidate.

Historical rows also come from other specimens with the same MRN. Each antibody confirmed there is listed once, citing the earliest accession that confirmed it. An antibody already typed into Previous antibodies is not listed twice.

**Earlier (pre-transfusion) phenotype.** When this specimen's phenotype cannot be used, or none was entered, HemoIQ looks at the patient's other specimens (same MRN) for the most recent phenotype that still reflects the patient's own cells:
- The earlier specimen must have been collected before this specimen's last transfusion, or before this specimen when it is not transfused.
- Its own typing must have been usable when it was collected, judged by the same transfusion rules.
- It is not used when this specimen is marked Transfused with no date, because the earlier typing cannot be shown to predate the transfusion.
- It is not used for a patient with an allogeneic HPC transplant.

When used, each line names its source, for example "Earlier phenotype from 2026-001 (2026-03-02, before the current transfusion context): Patient types E+…". The **[Transfusion]** note says the earlier phenotype is used instead. It counts as patient-typing evidence like a current phenotype: an antigen-positive typing argues against the alloantibody and is flagged if that antibody is suspected. Review it as you would any historical record.

Genotype remains available for review after transfusion because it is a molecular prediction, not a fresh serologic type. It is still evidence for you to accept or set aside.

Edit phenotype, genotype, previous antibodies, and notes on the specimen record. Then run analysis again so this tab uses the updated context.
