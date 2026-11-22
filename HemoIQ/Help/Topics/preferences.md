# Preferences

Open **Tools → Preferences** to set laboratory defaults. **Save** writes them to this computer. **Cancel** discards the dialog. **Help** opens this chapter.

On a small screen the settings scroll; **Save**, **Cancel** and the other buttons stay at the bottom of the window.

## Identity and thresholds

- **Lab name** and **Department** identify the laboratory on this workstation.
- **Facility name**, **Facility address** and **Contact information** form the letterhead printed at the top of every report (preview, print and PDF). Type the address one line per line, and phone, fax or email one per line. Blank lines and extra spaces are dropped. Each is optional: anything left blank is left off the report, and with all three blank, reports start with the product line as before. Limits: name 120 characters, address 400, contact information 200.
- **Score threshold** is the minimum combined score for the suspected list. The allowed range is 0.30 to 0.95. Type it with a decimal point or a decimal comma (0.70 or 0,70).
- **ID cell rule** is how many antigen-positive reactive cells and antigen-negative nonreactive cells are required before a row is marked as meeting the identification rule. AABB 3 + 3 is the usual standard. The Suspected Antibodies tab turns those rows green.
- **Multiple-Ab separation** applies when two or more antibodies are called together. It is the number of reactive cells each antibody needs that carry its antigen and are negative for every other called antigen. For anti-e, anti-f, and anti-Lea, that means reactive e+ f- Lea- cells, reactive e- f+ Lea- cells, and reactive e- f- Lea+ cells. The range is 0 to 5; 0 turns the check off, and the default is 1. An antibody known from patient history needs no cells of its own, and a rule on the Rules tab can exempt an antibody. This is a suggested criterion: HemoIQ warns but does not block.
- **ACS rule-outs** is how many times each clinically significant antibody must be ruled out before the application can suggest that all clinically significant antibodies are ruled out. The range is 1 to 5. When every one is ruled out but some antibody still has a combined score of 0.95 or more, HemoIQ offers ACS with that antibody named as an exception. The combined score is the average of 1 minus the Fisher p-value and the share of reactive cells that carry the antigen. It is a score for ranking evidence, not a probability, and reports word it that way.
- **ACS antigens added** lists the antigens, beyond the fixed core, that the all-ruled-out result also covers:
  - The core, which cannot be removed, is D, C, c, E, e, K, k, Fya, Fyb, Jka, Jkb, S and s.
  - The default additions are Lea, Leb, M and N. Add others your procedure requires, such as P1 or Kpa, or clear the box to use the core only.
  - Names are case-sensitive and must be known antigens; an unknown name is refused when you save.
  - The analysis report lists the antibodies evaluated, the rule trace records the list, and changing it marks open analyses as stale.
- **Default rule-outs** is the minimum number of qualifying antigen-positive nonreactive cells required to rule out an antibody when no custom rule exists. The range is 1 to 5. A rule on the Rules tab overrides this for that antibody.

## Specimens and the database

- **Default specimen type** pre-selects serum, plasma, or eluate on a new specimen.
- **Specimen dating (days)** pre-fills expiration when you add a specimen. 0 leaves the date blank. Three days is a typical antibody workup dating interval.
- **Expiration warning (days)** is how far ahead the Worklist and the lists mark a specimen or panel as expiring.
- **Require supervisor review before an investigation is Completed** adds a sixth status step. After an identification is confirmed, a second person must record **Supervisor Review…** on the Specimens tab before the investigation counts as Completed. Until then it shows *Awaiting supervisor review* and appears on the Worklist.
- **Dating look-back (days)** is 1–365, default 90. A transfusion this recent before collection makes the specimen-dating check apply (collection + Specimen dating days).
- **RhIG passive window (days)** is 1–365, default 84 (12 weeks). Anti-D within this many days after the recorded RhIG date is flagged for review as possibly passive.
- **Turnaround threshold (days)** is 1–60, default 3. Open investigations older than this are listed as overdue on Analytics, and those still awaiting additional testing appear as **Overdue testing** on the Worklist.
- **Recent transfusion (days)** is 1–365, default 90. If the specimen is Transfused and its last transfusion is this many days or fewer before the specimen date, the serologic phenotype is treated as possible donor cells and is not used against an alloantibody. The value is saved with each analysis snapshot.
- **Max database size (MB)** warns when the database file reaches 80 percent of this size. The allowed range is 50 to 10240 MB. The warning can offer to open Purge Database.
- **Default initials** pre-fills **Initials** on Confirm ID. Confirming an identification updates this value to the initials you used.
- **Show inactive specimens and panels by default** turns on **Show Inactive** when those tabs open.

## Phases and the reaction grid

- **Hide ruled-out antigen columns on the Reactions tab** starts Reactions with **Hide ruled-out** checked.
- **Extra phases** adds optional columns after the classic IS / 37°C / AHG / CC grid. Leave the box blank to keep the classic grid.
- **RT**, **PEG**, **Gel**, and **Solid** are suggested columns. Gel, Solid, and PEG count as IAT-like evidence. You can also type custom names.
- **Titer grid (Dil1–Dil128)** adds serial-dilution columns. The last reactive tube is the titer endpoint. Dilution grades are not IAT, and they do not rule out or identify an antibody.

Changes apply to the next analysis and to new specimens. They do not rewrite identifications you have already confirmed. Saving a new score threshold, ID cell rule, ACS rule-out count or antigens, default rule-out count, separation count, extra-phase list, recent-transfusion window, specimen dating days, dating look-back or RhIG passive window marks each unconfirmed analysis stale until you run analysis again. Each such change is recorded in the audit log with the old and new values, whether made here or by importing Preferences. The lab name, department, facility letterhead, and specimen defaults do not.

## Users and roles

**Users & roles…** registers people by their Windows login, with initials, name, and a role:

| Role | Can also |
|---|---|
| Read-only | View specimens, panels, analyses, reports, Analytics and the audit log; print and export. Cannot change any laboratory data |
| Technologist | Everything not listed below: specimens, reactions, analysis, confirming an identification, cancelling, reports |
| Supervisor | Record supervisor review, clear a confirmed identification, reinstate a cancelled investigation, add, edit or delete analysis rules |
| Administrator | Everything a Supervisor can, plus change Preferences and manage users |

While **no user is registered**, everyone may do everything, as in earlier versions. Once the first user is saved, the rules apply to every Windows login. An unregistered login can then view but not change anything: it is treated like a Read-only user, so that every change is made by a registered person. To start, select **Use my login**, enter your initials, and save yourself as an **Administrator**. The first user must be an Administrator, the last active Administrator cannot be demoted or deactivated, and initials must be unique among active users.

The right end of the status bar shows who is signed in and their role, for example "Signed in: Tom Tech (TE) (Technologist)". When the session can only view (a Read-only user, or a login that is not registered), the label turns amber, reads "view only", and the buttons that change laboratory data are disabled. These buttons are Add, Edit, Delete, Attach and Cancel on Specimens; Add, Edit, Copy, Import, Activate and the extra-antigen buttons on Panels; Save, Clear, Fill negatives and the run buttons on Reactions; Confirm ID and Clear on Analysis; and Add, Edit, Delete and Import on Rules. Hover over the label for details. The label updates when you close Preferences, so a role change takes effect immediately.

If the Preferences file cannot be written (read-only, locked by another program, or a full disk), **Save** says so and the window stays open with your edits. The Preferences saved before stay in effect, so analysis never runs on settings that were not saved or audited. An import that cannot be saved changes nothing, including its audit entries. The file is replaced only once the new copy is complete, so a full disk never leaves a damaged file that would reset Preferences to their defaults at the next start.

If the Preferences file exists but cannot be read at start-up (damaged, cut short, or locked), HemoIQ starts with the default Preferences and says so before anything else, naming the file. The unreadable file is first copied beside it as *settings.unreadable-<date-time>.json*, so a later save cannot overwrite the lab's settings; re-enter them from that copy, or import a Preferences or configuration export, before testing.

Messages that name a next step follow your role. A Technologist asked to change a confirmed identification, or to edit a cancelled investigation, is told to ask a Supervisor or Administrator to clear or reinstate it, rather than to click a command the role refuses.

When users are registered, the initials entered for a supervisor review must be the signed-in user's own. Every audit entry records the signed-in Windows login, and user changes are audited.

## Export and import

**Export…** saves the laboratory-wide Preferences, as last saved, to a JSON file. Use it to set up a second workstation the same way, or to keep a copy with your procedure documents. The file covers:
- lab name and department;
- facility name, address and contact information;
- probability threshold;
- identification, ACS, separation and default rule-out counts, and the ACS antigens added;
- default specimen type;
- expiration warning, specimen dating, transfusion window, dating look-back and RhIG window;
- supervisor review and turnaround threshold;
- extra phases.

Personal and workstation settings are not included: default initials, Worklist filters, display toggles and the database size limit.

**Import…** reads such a file and changes nothing until you confirm:
1. HemoIQ lists each setting it would change, as old → new.
2. It also lists values it refuses, such as a count outside its allowed range, the wrong kind of value or a blank lab name, and names it does not transfer. Out-of-range values are refused, never adjusted.
3. When you confirm, the changes are applied and saved, and this window closes. Unsaved edits in the window are discarded.
4. Every import is written to the audit log. If analysis settings changed, open investigations analyzed before the import appear as **Stale analysis** on the Worklist, as after a manual change.
5. Importing needs the Administrator role once users are registered.

Analysis rules are exported and imported separately, on the Rules tab, or together with these Preferences (see below).

## Whole lab configuration

**Tools > Export Lab Configuration…** saves the rules and these Preferences together in one file. Analysis depends on both, so this is the safer way to set up a second workstation or restore one.

**Tools > Import Lab Configuration…** previews both sections, then applies them in one step:
- The preview shows the same rule and Preferences changes as the separate imports. It also lists your stored rules that are not in the file.
- If the file has any refused entry, nothing is applied, because a partial copy would not match the source configuration.
- You choose whether to keep or remove stored rules that are not in the file. Removing them makes your rules match the file exactly; each removal is audited.
- Everything is written together. If any part fails, for example because of a missing role, nothing changes.
- Importing needs the role for rule changes, and the Administrator role when Preferences change.
