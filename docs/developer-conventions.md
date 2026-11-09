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

## Clinically important changes are audited, never silent

- Each specimen, patient, rule and settings change goes through a `DatabaseService` method. The
  method validates the value and calls `AppendAudit(action, entity, id, reason, beforeJson,
  afterJson)`.
- A save that changes nothing writes no audit entry.
- **Guard:** `AuditCoverageGuardTests` scans `DatabaseService`. A method that runs UPDATE, DELETE
  or INSERT must call `AppendAudit` or be on the test's reviewed list with a reason, such as
  derived analysis output, a timestamp, or a helper whose callers audit.
- Specimen fields lock after a confirmed identification (`EnsureSpecimenUnlocked`).
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

## Tests

- Use `IsolatedDatabase` for anything that touches the database.
- A test class that changes `AppSettings.Current` must be in `[Collection("PersistentDatabase")]`.
  Otherwise it runs in parallel with tests that read the settings.
- Test the intermediate analysis (rule-out evaluations, conflicting cells, suggestions), not
  only the final answer.
