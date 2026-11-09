# HemoIQ developer conventions

Rules that keep analysis correct and records traceable. Each rule has an automated test or a
documented reason.

## Antigen and antibody names are case-sensitive

K and k, C and c, E and e, and S and s are different antigens, and their antibodies are different
antibodies. Iterations 107 and 114 found eleven places that compared or keyed these names
ignoring case. Each one silently merged two antigens: rule-out counts, conflicting cells, lot
comparisons, the import schema check, and Final ID entries.

- **Antibody names:** compare with `RuleoutPolicy.SameAntibody(a, b)`. It accepts "Anti-" or "anti-"
  and compares the antigen exactly. `PatientHistory.AntibodyKey` gives the normalized key
  (`"anti-" + antigen`).
- **Antigen names:** compare, key dictionaries and build sets with `StringComparer.Ordinal`.
- **Case-insensitive is fine** for sorting, the "anti-" prefix, cell numbers, phase names, vendor
  names, file names and SQL column names.
- **Guard:** `AntigenCaseGuardTests` scans the source. A case-insensitive comparison on a line
  that mentions antigens or antibodies fails the build unless it is in the test's reviewed list
  with a reason. Prefer fixing the comparison over adding an entry.

## Dates are stored as Gregorian text

- Dates are stored as `yyyy-MM-dd` and times as `yyyy-MM-dd HH:mm`, and they are compared as
  text.
- `AppCulture.Apply()`, called first in `App.OnStartup`, keeps the user's culture for language
  and numbers but forces the Gregorian calendar and ":" as the time separator. Without it, a
  Thai Buddhist or Um al-Qura workstation stores year 2569 or 1448, and a Finnish one stores
  "10.30".
- Prefer `CultureInfo.InvariantCulture` when formatting a stored date in new code.
  `AppCultureTests` checks the normalization and that startup applies it.

- Numbers written to files (CSV exports, the Preferences export) use `CultureInfo.InvariantCulture`,
  so they have a decimal point under every regional setting. Typed decimals go through
  `LabSettings.TryParseDecimal`, which accepts "0.70" or "0,70". `CommaDecimalCultureTests` runs
  the exports under German settings.

## Clinically important changes are audited, never silent

- Each specimen, patient, rule and settings change goes through a `DatabaseService` method. The
  method validates the value and calls `AppendAudit(action, entity, id, reason, beforeJson,
  afterJson)`.
- A save that changes nothing writes no audit entry.
- **Guard:** `AuditCoverageGuardTests` scans `DatabaseService`. A method that runs UPDATE, DELETE
  or INSERT must call `AppendAudit` or be on the test's reviewed list with a reason, such as
  derived analysis output, a timestamp, or a helper whose callers audit.
- Specimen fields lock after a confirmed identification (`EnsureSpecimenUnlocked`).
- Panel typing behind a confirmed identification changes only through `SavePanelCellEdits`, which
  requires a reason and audits the change. The direct writers (`UpdatePanelCell`,
  `UpdatePanelCellAntigen`, `UpdatePanelCellMetadata`, `DeletePanelCells`, `ReplacePanelCells`,
  `CopyPanelCells`) refuse such a panel. They stay available for new panels, imports and seed data.
- Rules and lab-wide Preferences are audited, and changing them marks open analyses stale
  (`AnalysisCurrency`, `LastAnalysisConfigurationChange`).

## Imports preview before they write

- Panel, rule and Preferences imports follow the same steps: parse, validate, preview, explicit
  confirmation, then one transaction (`RunInTransaction`).
- Invalid values are refused with a reason, never clamped silently.
- Imports never delete records that are missing from the file.

## Schema changes

- Add columns with `GetColumnNames` plus `ALTER TABLE ... ADD COLUMN` in the `Migrate*` methods.
- Add a `CHECK` constraint where the value set or length is fixed.
- New columns go at the end of the table, because the archive copies tables with `SELECT *`.

## Dialog layout

- The Specimen dialog uses fixed-height tabs. `SpecimenDialogSectionsTests.EveryTab_FitsWithoutClipping`
  renders the dialog off-screen and fails if a tab's fields need more height than the tab has.
- The Specimen tab is nearly full: there is room for only about one more 36 px row. Put new
  inputs beside related ones, as was done for encounter and sex, or on another tab.

- The Preferences dialog sizes itself to its content, but is capped at the screen's working area.
  Its settings scroll while the button row stays docked at the bottom.
  `SettingsDialogLayoutTests` checks that Save stays visible at a 420 px limit.

- `DialogHeightGuardTests` shows each dialog off-screen and fails if its natural height exceeds
  728 px, the work area of a 1366×768 screen, unless the dialog scrolls and caps itself. When
  adding a dialog, add it to that test.

## Tests

- Use `IsolatedDatabase` for anything that touches the database.
- A test class that changes `AppSettings.Current` must be in `[Collection("PersistentDatabase")]`.
  Otherwise it runs in parallel with tests that read the settings.
- Test the intermediate analysis (rule-out evaluations, conflicting cells, suggestions), not
  only the final answer.
