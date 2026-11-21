# Add a run

**+ Run** on the Reactions tab opens this dialog. A run is a second set of grades for the same specimen and panel, after a cell treatment or a serum treatment.

## Cell treatment

- **Untreated (None)** is an ordinary panel.
- **Ficin-treated** or **Papain-treated** is an enzyme-treated cell panel. Antigens destroyed by the enzyme are marked on the grid and are not used to rule antibodies out.
- **DTT-treated** is a dithiothreitol-treated cell panel. Kell-system antigens destroyed by DTT are handled the same way.

## Serum treatment

- **None** tests untreated serum.
- **Prewarmed serum** records a prewarmed test.
- **Absorbed: R1R1 (DCe/DCe)**, **Absorbed: R2R2 (DcE/DcE)**, and **Absorbed: rr (ce/ce)** are allogeneic adsorptions. R1R1 cells are expected to remove anti-D, anti-C and anti-e; R2R2 cells anti-D, anti-c and anti-E; rr cells anti-c, anti-e and anti-f (rr cells carry f, the compound of c and e). An absorbed aliquot is not used to rule out the antibodies it is expected to remove.
- **Autoadsorption** is an autoadsorbed aliquot. If the specimen records a red-cell transfusion within the recent-transfusion window (Preferences), or the transfusion status is Unknown and the notes mention transfusion, or the patient record shows an allogeneic HPC transplant on or before collection, or another specimen for the patient records a transfusion within the window, donor cells in the patient's sample may have adsorbed alloantibodies. The autoadsorbed aliquot is then not used to rule out any alloantibody; its negatives are listed on **Special Panels** with the reason. Consider allogeneic (differential) adsorption instead.

The italic note under the lists explains what the selected combination will mean when you analyze. It is built from the same treatment tables analysis uses, so it lists exactly the antigens analysis treats as destroyed or enhanced (for example, ficin and papain enhance Lea and Leb) and the antibodies each adsorption removes.

## Label and grades

**Label** is optional. Leave it blank and the application names the run from the treatments you picked. If you type a label that does not mention the treatment, the treatment is added in parentheses wherever the run is shown, for example **Repeat (Ficin-treated)**, so a report never hides that the run's cells or serum were treated.

**Pre-fill with grades from the current run** is off by default and is available only for an untreated repeat (no cell or serum treatment). A treated run exists to show how the treatment changes the reactions, so its grades must be read from the treated test; copying untreated grades into it would claim the treatment changed nothing. When you use it, the grades are placed in the new run's grid as **unsaved** values, and the status line says so. Check each one against the test, change any that differ, then **Save**. Nothing is recorded until you save.

**Add Run** creates the run and selects it. **Cancel** leaves the runs unchanged. **Help** opens this chapter.

Compare the new run with the original from the Reactions tab by turning on **Compare**. Treatment inferences and adsorption findings appear on the Analysis tab under **Special Panels**.
