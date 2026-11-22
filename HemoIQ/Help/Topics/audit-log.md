# Audit log

**Tools → Audit Log** shows the append-only record of clinical actions. Events are never edited or deleted in this window.

Columns:

- **UTC** — when the event was recorded, in Coordinated Universal Time.
- **Operator** — the Windows login that recorded the action. Lab initials used on Confirm ID stay in the exported before-and-after detail.
- **Action** — what happened, such as confirming or clearing an identification, or saving a reaction.
- **Type** — the kind of record, such as a specimen.
- **Entity** — which record the event refers to.
- **Reason** — the text entered when a reason was required, including the reason from **Clear** on a confirmed ID.

The window lists the newest 5,000 events. When the log holds more, the count reads, for example, *Newest 5000 of 12000*, and a line under **Find** says so. **Find** narrows the listed events by accession, action, Windows login, record type, or reason. **Export CSV…** saves every event in the whole log that matches **Find** (all of them when Find is empty), newest first, not only the listed rows, and then says how many it wrote. The export itself is written to the audit log (action **export_data**) with the file name and a SHA-256 fingerprint of the file. A text value that a spreadsheet would run as a formula (one starting with =, +, -, @ or a tab) is written with a leading apostrophe, so opening the file cannot run it. Panel typing (+, ++, -) and numbers are written unchanged. **Close** returns to the main window. **Help** opens this chapter.

The log is a history of actions taken in the application. It is not the clinical report. Use **Reports** for the identification document, and use this dialog when you need to see who confirmed or cleared an ID and why.
