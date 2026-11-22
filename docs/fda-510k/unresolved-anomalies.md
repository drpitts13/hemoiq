# Unresolved anomalies

| ID | Description | Risk | Disposition |
|---|---|---|---|
| UA-01 | `MathNet.Numerics` is referenced but the Fisher exact test is a custom implementation | Low — unused library | Track for removal or adoption |
| UA-02 | Finder-style duplicate files (`* 2.cs`) existed in the tree and were excluded from test compile | Low — confusion during maintenance | Resolved: none remain in the tree; the `Compile Remove` exclusion stays as a safeguard |
| UA-03 | Confirmation uses initials, not a Part 11 electronic signature | Accepted for 510(k)-only scope | Residual; lock + reason + Windows identity |
| UA-04 | No in-app user accounts or roles | Accepted for local single-workstation use | Resolved: registered users with Technologist, Supervisor, Administrator and Read-only roles; every database write checks a permission (`AccessControl`); user management is audited. Residual: sign-in is the Windows logon (no in-app passwords) |
