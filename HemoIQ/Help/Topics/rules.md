# Rules

Rules are optional, per-antibody overrides of the default rule-out settings. When a rule exists for an antibody, analysis uses that rule. When it does not, analysis uses **Default rule-outs** from Preferences.

## The list

- **Name** is the label you gave the rule. Selecting a rule updates the status line to name that rule.
- **Antibody** is the specificity it applies to. Capitals in "Anti-" do not matter, but the antigen is case-sensitive: a rule for anti-K does not apply to anti-k, and the same holds for C and c, E and e, and S and s.
- **Het OK** means a heterozygous antigen-positive cell may count toward the rule-out.
- **Add** creates a rule. Ctrl+N does the same while this tab is selected. **Edit** opens the selected rule. Changing what analysis reads (the antibody, exception antigen, zygosity, rule-out count or separation exemption) marks open analyses stale. Changing only the name or description is audited but does not, and the status line says so; saving with nothing changed says *unchanged; nothing was saved*. **Import** and **Tools > Import Lab Configuration** follow the same rule: their status line says open analyses are stale only when the import changed something analysis reads (a rule's antibody, counts or exemptions, an added or removed rule, or a clinical Preference); an import that only renames rules or changes display Preferences says analyses stay current. **Delete** removes it after you confirm. **Refresh** reloads the list.
- Rules change which antibodies are ruled out. Once users are registered, adding, editing and deleting rules needs the Supervisor or Administrator role; other users see the reason and nothing changes.
- Every add, edit and delete is written to the audit log with all the rule's settings: name, antibody, exception antigen, heterozygous cells, minimum count and separation exemption. An edit keeps the old and new values, and a delete keeps the deleted rule. Saving an edit that changes nothing writes no entry.
- After a rule changes, open investigations analyzed before the change appear on the Worklist as **Stale analysis** until they are analyzed again. A changed clinical Preference has the same effect. Confirmed identifications are not reopened. HemoIQ orders the change against each analysis by the audit log, not by the clock, so a change made in the same second as an analysis, or while the clock repeats an hour at the end of daylight saving time, is still caught.
- **Help** opens this chapter.

## Export and import

**Export…** saves every rule to a JSON file, for example to copy a validated rule set to another HemoIQ installation or keep it with your procedure documents.

**Import…** reads such a file and changes nothing until you confirm:
1. HemoIQ checks the file and lists what it would do: rules to **add**, rules to **update** (with each changed setting, such as "rule-out cells 2 → 3"), rules that are **unchanged**, and entries it **refuses**, with the reason.
2. Rules are matched by antibody, so a file rule for anti-K updates your anti-K rule whatever it is named.
3. Entries are refused when the antibody is not a known antigen in its exact case, the name is missing, the count is outside 1–5, the exception antigen is unknown, a separation exemption lists an unknown antigen (the same check as the Rule dialog), or the file has two rules for one antibody.
4. When you confirm, the adds and updates are written together. Each is audited like a manual add or edit. If any write fails, none are kept.
5. Rules that are not in the file are kept; this import never deletes a rule. To copy rules and Preferences together, and optionally remove rules the file lacks, use **Tools > Import Lab Configuration…** (see Preferences help).
6. Importing needs the same role as editing rules. Open investigations analyzed before the import appear as **Stale analysis** on the Worklist.

## The detail pane

The right pane is read-only. It shows the name, antibody, exception antigen, whether heterozygous cells are allowed, the minimum rule-out count, and the description. Change those fields with **Edit**. The minimum rule-out count must be a whole number from 1 to 5; anything else is refused when you save. A rule saved by an earlier version with a count outside that range is listed as a problem, with the count analysis actually uses.

**Exception antigen** limits heterozygous permission to that antigen on the antibody named in the rule. A rule for anti-D with exception C does not let heterozygous C cells rule out anti-C. Leave the exception blank when the rule is only a count and a zygosity choice for its own antibody.

**Min Ruleout Count** replaces the default count for this antibody only. The Ruled Out tab shows that number in the **Needed** column and explains when the custom rule was used.

Rules apply the next time you run analysis. They do not rewrite a final ID you have already confirmed.

**Tools > Import Lab Configuration** checks your role before it changes anything. A role that can change neither rules nor Preferences (a Technologist) is told before choosing a file. A Supervisor may import rules, but a file that also changes Preferences, which need the Administrator role, is refused after its preview and before the confirmation; nothing is changed.
