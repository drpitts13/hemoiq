# Rules

Rules are optional, per-antibody overrides of the default rule-out settings. When a rule exists for an antibody, analysis uses that rule. When it does not, analysis uses **Default rule-outs** from Preferences.

## The list

- **Name** is the label you gave the rule. Selecting a rule updates the status line to name that rule.
- **Antibody** is the specificity it applies to.
- **Het OK** means a heterozygous antigen-positive cell may count toward the rule-out.
- **Add** creates a rule. Ctrl+N does the same while this tab is selected. **Edit** opens the selected rule. **Delete** removes it after you confirm. **Refresh** reloads the list.
- **Help** opens this chapter.

## The detail pane

The right pane is read-only. It shows the name, antibody, exception antigen, whether heterozygous cells are allowed, the minimum rule-out count, and the description. Change those fields with **Edit**.

**Exception antigen** limits heterozygous permission to that antigen on the antibody named in the rule. A rule for anti-D with exception C does not let heterozygous C cells rule out anti-C. Leave the exception blank when the rule is only a count and a zygosity choice for its own antibody.

**Min Ruleout Count** replaces the default count for this antibody only. The Ruled Out tab shows that number in the **Needed** column and explains when the custom rule was used.

Rules apply the next time you run analysis. They do not rewrite a final ID you have already confirmed.
