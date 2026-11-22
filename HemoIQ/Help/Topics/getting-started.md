# Getting started

HemoIQ helps licensed immunohematology professionals document antibody-panel reactions and compute rule-out and probability statistics. A qualified professional must confirm identification. This software does not issue or release blood units and is not a substitute for licensed clinical judgment.

## How to open help

- Press **F1** to open help for the section you are in. On Analysis, F1 follows the inner tab that is selected.
- Click **Help** on a tab or dialog to open that section.
- Choose **Help → User Manual** to open this manual at the beginning.
- Choose **Help → Topics** to jump to any chapter.
- **Help → Keyboard Shortcuts** opens the shortcut list. **Help → About** shows the product name, version, and intended use.

The manual stays open while you work. Use **Find** to search titles and chapter text. **Print chapter** prints the page you are reading. **Print manual** prints every chapter in order. **Save manual** writes the whole manual, including the intended-use statement and the contents, as a PDF, a Markdown file, or plain text. **Close**, or Escape, dismisses the manual and leaves the screen you were on.

## A typical workup

1. Set **Tools → Preferences** for the lab name, identification cell rule, rule-out counts, and extra phases.
2. On **Panels**, add a lot or import one, review the antigram, and activate it.
3. On **Specimens**, add the accession and clinical context, then attach the panel.
4. On **Reactions**, enter grades and choose **Save & Analyze**.
5. On **Analysis**, review the evidence and **Confirm ID** with your initials.
6. On **Reports**, print or export the clinical identification report.

**Worklist** shows incomplete reactions, stale analyses, and specimens or panels that are expiring or expired. **Analytics** summarizes confirmed identifications. **Search** finds panel cells by antigen profile. **Rules** stores per-antibody rule-out overrides.

## If HemoIQ cannot start

HemoIQ writes to its database file every time it runs, to keep the audit trail. If the file cannot be opened for writing, HemoIQ shows the file's path and the reason, then closes without changing anything:

- **Read-only.** The file or its folder is read-only, for example on a read-only network share. Fix the permission or ask IT to make the share writable.
- **In use.** Another program holds the file, for example a second copy of HemoIQ, a backup or a sync tool. Close it and start HemoIQ again.
- **Damaged.** The file is damaged or is not a HemoIQ database. Restore it from your laboratory's backup, and do not delete it.

## Menus

- **File → Exit** closes the application after you confirm.
- **Edit → Save** (Ctrl+S) saves the antigen grid on Panels or the grades on Reactions.
- **Edit → Refresh All** (Ctrl+R) reloads every tab.
- **Edit → New Item** (Ctrl+N) adds a specimen, panel, or rule, depending on the tab.
- Ctrl+S and Ctrl+N do only what the matching button would do. When that button is disabled (for example in a view-only session, or Save on Panels before you choose Edit), the shortcut does nothing and the status bar says why.
- **Tools** opens Preferences, Purge Database, Open Archive, Audit Log, and Load Demo Data.

The first time the application starts, it shows the intended-use statement. The status bar at the bottom reports the result of the last action. Opening a screen replaces that line with a line for the screen you are looking at, so a worklist message does not stay on Search. Returning to Search shows the last search result again, including the homozygous or heterozygous wording.
