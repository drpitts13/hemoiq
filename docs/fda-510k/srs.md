# Software requirements specification (starter)

| ID | Requirement |
|---|---|
| SRS-IU-01 | The software shall display the intended-use statement in About, first-run acknowledgment, Help, and report headers. |
| SRS-IU-02 | Reports and About shall display the assembly / informational version, not a hardcoded marketing string. |
| SRS-SEED-01 | Normal application launch shall not insert clinical or demo seed specimens. |
| SRS-SEED-02 | Seed data may be loaded only by Tools > Load Demo Data or the `--seed-clinical` switch, only into an empty database or one holding only demo records, and as a single transaction. |
| SRS-AUD-01 | Clinical actions shall write an append-only audit event with UTC time, Windows operator, action, entity, optional reason, and before/after JSON. |
| SRS-AUD-02 | Confirm ID, clear ID, reaction save, panel/rule edits, settings changes, purge, and restore shall be audited. |
| SRS-AUD-03 | Operators shall be able to view and export the audit log. |
| SRS-LOCK-01 | A specimen with a confirmed identification shall reject reaction, panel-link, run, and analysis-write mutations. |
| SRS-LOCK-02 | Clearing a confirmed identification shall require a non-empty reason and shall be audited. |
| SRS-SNAP-01 | Each persisted analysis shall store software version, clinical settings JSON, input fingerprint, rule-out, suspected, and ACS summaries. |
| SRS-SNAP-02 | Analysis and clinical reports shall print version, settings, and input fingerprint. |
| SRS-LOG-01 | Unhandled exceptions shall be written to `%AppData%\HemoIQ\logs` with software version and without reaction grades or record identifiers: HemoIQ's own exception messages (which name accessions, MRNs and lots) are withheld and replaced by the exception type and throwing method. |
| SRS-CYB-01 | Operator identity on audit events shall be the Windows user name. Once users are registered, each action shall require its role permission (record results, supervisor review, clear identification, reinstate, configure preferences or rules, manage users, purge) and be refused otherwise; access to the database file relies on the Windows login and NTFS ACLs. |
| SRS-SAFE-01 | Recording results on an expired lot or specimen shall ask first, activating an expired lot shall warn and be audited, and results recorded after expiry shall be reported for review. |
| SRS-SAFE-02 | Evidence from one specimen shall not enter another patient's analysis: eluate links and MRN changes shall be refused when the MRNs differ. |
| SRS-SAFE-03 | Imported and copied panel lots shall be stored inactive, and every activation shall go through the antigram review prompt; panel detail edits shall record old and new values. |
| SRS-SAFE-04 | A new run shall never be saved with another run's grades; pre-fill shall be offered only for an untreated repeat, as unsaved values. |
| SRS-SAFE-05 | A truncated or malformed lot file shall be refused as a whole; missing typing shall never be read as negative. |
| SRS-SAFE-06 | Every confirmation that deletes data shall default to No, and Enter shall not trigger Purge. |
| SRS-SAFE-07 | A database that cannot be written shall be refused at start-up with its path and reason, and the application shall close; a failed save shall roll back and keep the unsaved entries. |
| SRS-SAFE-08 | Combined scores shall be labelled as scores, never described as probabilities. |
