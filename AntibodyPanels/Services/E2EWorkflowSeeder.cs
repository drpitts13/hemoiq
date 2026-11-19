using System;
using System.Collections.Generic;
using AntibodyPanels.Data;
using AntibodyPanels.Models;

namespace AntibodyPanels.Services
{
    /// <summary>
    /// Seeds persistent end-to-end workups (E2E-*) into the application database.
    /// Skips an accession that already exists. Does not write 2026- or DEMO- rows.
    /// </summary>
    public static class E2EWorkflowSeeder
    {
        public const string WarmK = "E2E-WARM-K";
        public const string WarmFya = "E2E-WARM-FYA";
        public const string WarmJka = "E2E-WARM-JKA";
        public const string ColdM = "E2E-COLD-M";
        public const string ColdLea = "E2E-COLD-LEA";
        public const string ColdP1 = "E2E-COLD-P1";
        public const string WarmAuto = "E2E-WARM-AUTO";
        public const string ColdAuto = "E2E-COLD-AUTO";
        public const string WarmAutoAllos = "E2E-WARM-AUTO-ALLOS";
        public const string Mixed = "E2E-MIXED-COLD-WARM";
        public const string ComboEc = "E2E-COMBO-EC";
        public const string ComboKFya = "E2E-COMBO-KFYA";
        public const string ComboJk = "E2E-COMBO-JK";
        public const string ExoVel = "E2E-EXO-VEL";
        public const string ExoK = "E2E-EXO-K";
        public const string ExoU = "E2E-EXO-U";
        public const string ExoDia = "E2E-EXO-DIA";
        public const string ExoWra = "E2E-EXO-WRA";
        public const string ExoKpb = "E2E-EXO-KPB";
        public const string HtlaCh = "E2E-HTLA-CH";
        public const string Negative = "E2E-NEGATIVE";
        public const string PanAcNeg = "E2E-PAN-AC-NEG";
        public const string InvalidCc = "E2E-CC-INVALID";

        public static readonly string[] SpecimenIds =
        {
            WarmK, WarmFya, WarmJka, ColdM, ColdLea, ColdP1,
            WarmAuto, ColdAuto, WarmAutoAllos, Mixed,
            ComboEc, ComboKFya, ComboJk,
            ExoVel, ExoK, ExoU, ExoDia, ExoWra, ExoKpb, HtlaCh,
            Negative, PanAcNeg, InvalidCc
        };

        public static void SeedIfNeeded(DatabaseService db, AntibodyAnalyzer analyzer)
        {
            foreach (var id in SpecimenIds)
            {
                if (db.GetSpecimen(id) != null) continue;
                SeedOne(db, id);
                analyzer.AnalyzeSpecimen(id, updateDb: true);
            }
            RepairBlanketNegatives(db);
        }

        /// <summary>
        /// Older E2E lots stored every untyped antigen as "-". A column that is
        /// negative on every cell, including the autocontrol, was never typed.
        /// Those cells become NT. A column with any + or ++ is left alone, so a
        /// typed negative on a donor cell stays negative. The seeder does not
        /// type the autocontrol, so a "-" left on AC becomes NT.
        /// </summary>
        public static void RepairBlanketNegatives(DatabaseService db)
        {
            foreach (var panel in db.GetAllPanels())
            {
                if (!string.Equals(panel.Vendor, "E2E", StringComparison.Ordinal)) continue;
                var cells = db.GetPanelCells(panel.PanelId);
                if (cells.Count == 0) continue;
                foreach (var ag in AntigenConstants.Antigens)
                {
                    if (!cells.All(c => c.Antigens.TryGetValue(ag, out var v) && v == "-"))
                        continue;
                    foreach (var cell in cells)
                        db.UpdatePanelCellAntigen(cell.Id, ag, AntigenConstants.AntigenNotTested);
                }
                foreach (var cell in cells)
                {
                    if (!string.Equals(cell.CellNumber, "AC", StringComparison.Ordinal))
                        continue;
                    foreach (var pair in cell.Antigens)
                    {
                        if (pair.Value != "-") continue;
                        db.UpdatePanelCellAntigen(cell.Id, pair.Key, AntigenConstants.AntigenNotTested);
                    }
                }
            }
        }

        private static void SeedOne(DatabaseService db, string id)
        {
            switch (id)
            {
                case WarmK: SeedWarmK(db); break;
                case WarmFya: SeedWarmFya(db); break;
                case WarmJka: SeedWarmJka(db); break;
                case ColdM: SeedCold(db, ColdM, "M", "N"); break;
                case ColdLea: SeedCold(db, ColdLea, "Lea", "Leb"); break;
                case ColdP1: SeedColdP1(db); break;
                case WarmAuto: SeedWarmAuto(db); break;
                case ColdAuto: SeedColdAuto(db); break;
                case WarmAutoAllos: SeedWarmAutoAllos(db); break;
                case Mixed: SeedMixed(db); break;
                case ComboEc: SeedCombo(db, ComboEc, "E", "c"); break;
                case ComboKFya: SeedCombo(db, ComboKFya, "K", "Fya"); break;
                case ComboJk: SeedComboJk(db); break;
                case ExoVel: SeedVel(db); break;
                case ExoK: SeedHighPrev(db, ExoK, "k", "K", dtt: false); break;
                case ExoU: SeedRare(db, ExoU, "U", lowFrequency: false); break;
                case ExoDia: SeedRare(db, ExoDia, "Dia", lowFrequency: true); break;
                case ExoWra: SeedRare(db, ExoWra, "Wra", lowFrequency: true); break;
                case ExoKpb: SeedHighPrev(db, ExoKpb, "Kpb", "Kpa", dtt: true); break;
                case HtlaCh: SeedHtla(db); break;
                case Negative: SeedNegative(db); break;
                case PanAcNeg: SeedPanAcNeg(db); break;
                case InvalidCc: SeedInvalidCc(db); break;
            }
        }

        private static void SeedWarmK(DatabaseService db)
        {
            var panel = Panel(db, "E2E warm anti-K", WarmK, 6, false);
            Type(db, panel, "1 K+ k-", "2 K+ k+", "3 K- k+", "4 K- k+", "5 K+ k-", "6 K- k+");
            var run = Open(db, WarmK, "serum", panel);
            ByAntigen(db, run, panel, "K", ahg: "2+");
        }

        private static void SeedWarmFya(DatabaseService db)
        {
            var panel = Panel(db, "E2E warm anti-Fya", WarmFya, 6, false);
            Type(db, panel, "1 Fya+ Fyb-", "2 Fya+ Fyb+", "3 Fya- Fyb+", "4 Fya- Fyb+", "5 Fya+ Fyb-", "6 Fya- Fyb+");
            var run = Open(db, WarmFya, "serum", panel);
            ByAntigen(db, run, panel, "Fya", ahg: "2+");
            var ficin = db.AddPanelRun(WarmFya, panel, CellTreatment.Ficin, SerumTreatment.None, "Ficin panel");
            AllNegative(db, ficin, panel);
        }

        private static void SeedWarmJka(DatabaseService db)
        {
            var panel = Panel(db, "E2E warm anti-Jka dosage", WarmJka, 6, false);
            Type(db, panel, "1 Jka++ Jkb-", "2 Jka+ Jkb+", "3 Jka- Jkb+", "4 Jka++ Jkb-", "5 Jka+ Jkb+", "6 Jka- Jkb++");
            var run = Open(db, WarmJka, "serum", panel);
            ByDosage(db, run, panel, "Jka", homo: "3+", hetero: "1+");
        }

        private static void SeedCold(DatabaseService db, string id, string antigen, string partner)
        {
            var panel = Panel(db, "E2E cold anti-" + antigen, id, 6, false);
            Type(db, panel,
                $"1 {antigen}+ {partner}-",
                $"2 {antigen}- {partner}+",
                $"3 {antigen}+ {partner}-",
                $"4 {antigen}- {partner}+",
                $"5 {antigen}+ {partner}+",
                $"6 {antigen}- {partner}+");
            var run = Open(db, id, "plasma", panel);
            ByAntigen(db, run, panel, antigen, ahg: "0", isP: "2+", c37: "0");
            var ficin = db.AddPanelRun(id, panel, CellTreatment.Ficin, SerumTreatment.None, "Ficin panel");
            AllNegative(db, ficin, panel);
        }

        private static void SeedColdP1(DatabaseService db)
        {
            var panel = Panel(db, "E2E cold anti-P1", ColdP1, 6, false);
            Type(db, panel, "1 P1+", "2 P1-", "3 P1+", "4 P1-", "5 P1+", "6 P1-");
            var run = Open(db, ColdP1, "plasma", panel);
            ByAntigen(db, run, panel, "P1", ahg: "0", isP: "2+", c37: "0");
        }

        private static void SeedWarmAuto(DatabaseService db)
        {
            var panel = Panel(db, "E2E warm auto", WarmAuto, 6, true);
            Type(db, panel,
                "1 E+ K- Fya+", "2 E- K+ Fya-", "3 E+ K+ Fya-",
                "4 E- K- Fya+", "5 E+ K- Fya+", "6 E- K+ Fya-");
            var run = Open(db, WarmAuto, "serum", panel, dat: "Positive");
            Pan(db, run, panel, isP: "0", c37: "2+", ahg: "3+", acAhg: "3+");
            var adsorbed = db.AddPanelRun(WarmAuto, panel, CellTreatment.None, SerumTreatment.AutoAdsorption, "Autoadsorbed");
            AllNegative(db, adsorbed, panel);
        }

        private static void SeedColdAuto(DatabaseService db)
        {
            var panel = Panel(db, "E2E cold auto", ColdAuto, 6, true);
            Type(db, panel, "1 M+ E-", "2 M- E+", "3 M+ E+", "4 M- E-", "5 M+ E-", "6 M- E+");
            var run = Open(db, ColdAuto, "plasma", panel, dat: "Positive (C3)",
                notes: "Cold auto, anti-I-like. Panreactive at IS, nonreactive at AHG.");
            Pan(db, run, panel, isP: "3+", c37: "0", ahg: "0", acAhg: "0", acIs: "3+");
        }

        private static void SeedWarmAutoAllos(DatabaseService db)
        {
            var panel = Panel(db, "E2E warm auto plus allos", WarmAutoAllos, 6, true);
            Type(db, panel,
                "1 E+ K- c-", "2 E- K+ c+", "3 E+ K+ c+",
                "4 E- K- c+", "5 E+ K- c-", "6 E- K+ c-");
            var run = Open(db, WarmAutoAllos, "serum", panel, dat: "Positive",
                notes: "Warm autoantibody with possible underlying alloantibodies.");
            Pan(db, run, panel, isP: "0", c37: "2+", ahg: "3+", acAhg: "3+");
            var adsorbed = db.AddPanelRun(WarmAutoAllos, panel, CellTreatment.None, SerumTreatment.AutoAdsorption, "Autoadsorbed");
            ByAny(db, adsorbed, panel, new[] { "E", "K" }, ahg: "2+");
        }

        private static void SeedMixed(DatabaseService db)
        {
            var panel = Panel(db, "E2E mixed cold and warm", Mixed, 6, false);
            Type(db, panel, "1 M+ E-", "2 M- E+", "3 M+ E+", "4 M- E-", "5 M+ E-", "6 M- E+");
            var run = Open(db, Mixed, "serum", panel, notes: "IS anti-M with AHG anti-E. Prewarm separates them.");
            foreach (var cell in db.GetPanelCells(panel))
            {
                if (cell.CellNumber == "AC") continue;
                var isP = cell.IsAntigenPositive("M") ? "2+" : "0";
                var ahg = cell.IsAntigenPositive("E") ? "2+" : "0";
                Save(db, run, cell.CellNumber, isP, "0", ahg);
            }
            var prewarm = db.AddPanelRun(Mixed, panel, CellTreatment.None, SerumTreatment.Prewarmed, "Prewarmed");
            ByAntigen(db, prewarm, panel, "E", ahg: "2+", isP: "0", c37: "0");
        }

        private static void SeedCombo(DatabaseService db, string id, string a, string b)
        {
            var panel = Panel(db, "E2E " + a + "+" + b, id, 6, false);
            Type(db, panel,
                $"1 {a}+ {b}-", $"2 {a}- {b}+", $"3 {a}+ {b}+",
                $"4 {a}- {b}-", $"5 {a}+ {b}-", $"6 {a}- {b}+");
            var run = Open(db, id, "serum", panel);
            ByAny(db, run, panel, new[] { a, b }, ahg: "2+");
        }

        private static void SeedComboJk(DatabaseService db)
        {
            var panel = Panel(db, "E2E anti-Jka+Jkb", ComboJk, 6, false);
            Type(db, panel, "1 Jka+ Jkb-", "2 Jka- Jkb+", "3 Jka+ Jkb+", "4 Jka- Jkb-", "5 Jka+ Jkb-", "6 Jka- Jkb+");
            var run = Open(db, ComboJk, "serum", panel);
            ByAny(db, run, panel, new[] { "Jka", "Jkb" }, ahg: "2+");
        }

        private static void SeedVel(DatabaseService db)
        {
            var panel = Panel(db, "E2E anti-Vel", ExoVel, 6, false, "Vel");
            Type(db, panel, "1 Vel+", "2 Vel+", "3 Vel+", "4 Vel+", "5 Vel+", "6 Vel-");
            var run = Open(db, ExoVel, "serum", panel);
            ByAntigen(db, run, panel, "Vel", ahg: "1+");
            var ficin = db.AddPanelRun(ExoVel, panel, CellTreatment.Ficin, SerumTreatment.None, "Ficin panel");
            ByAntigen(db, ficin, panel, "Vel", ahg: "3+");
        }

        private static void SeedHighPrev(DatabaseService db, string id, string antigen, string partner, bool dtt)
        {
            var panel = Panel(db, "E2E anti-" + antigen, id, 6, true, Warehouse(antigen), Warehouse(partner));
            Type(db, panel,
                $"1 {antigen}+ {partner}-",
                $"2 {antigen}+ {partner}-",
                $"3 {antigen}+ {partner}+",
                $"4 {antigen}+ {partner}-",
                $"5 {antigen}+ {partner}-",
                $"6 {antigen}- {partner}+");
            var run = Open(db, id, "serum", panel, dat: "Negative");
            ByAntigen(db, run, panel, antigen, ahg: "2+", acAhg: "0");
            if (!dtt) return;
            var treated = db.AddPanelRun(id, panel, CellTreatment.DTT, SerumTreatment.None, "DTT panel");
            AllNegative(db, treated, panel);
        }

        private static void SeedRare(DatabaseService db, string id, string antigen, bool lowFrequency)
        {
            var panel = Panel(db, "E2E anti-" + antigen, id, 6, false, antigen);
            if (lowFrequency)
            {
                Type(db, panel, $"1 {antigen}+", $"2 {antigen}-", $"3 {antigen}+", $"4 {antigen}-", $"5 {antigen}+", $"6 {antigen}-");
            }
            else
            {
                Type(db, panel, $"1 {antigen}+", $"2 {antigen}+", $"3 {antigen}+", $"4 {antigen}+", $"5 {antigen}+", $"6 {antigen}-");
            }
            var run = Open(db, id, "serum", panel);
            ByAntigen(db, run, panel, antigen, ahg: "2+");
        }

        private static void SeedHtla(DatabaseService db)
        {
            var panel = Panel(db, "E2E HTLA anti-Ch", HtlaCh, 6, true, "Ch");
            Type(db, panel, "1 Ch+", "2 Ch+", "3 Ch+", "4 Ch+", "5 Ch+", "6 Ch-");
            var run = Open(db, HtlaCh, "serum", panel, dat: "Negative",
                notes: "AHG titer 64; plasma neutralization of Chido reactivity.");
            var neut = new Dictionary<string, string> { ["Neut"] = "0" };
            foreach (var cell in db.GetPanelCells(panel))
            {
                if (cell.CellNumber == "AC")
                {
                    Save(db, run, "AC", "0", "0", "0");
                    continue;
                }
                if (cell.IsAntigenPositive("Ch"))
                    Save(db, run, cell.CellNumber, "0", "0", "w+", neut);
                else
                    Save(db, run, cell.CellNumber, "0", "0", "0");
            }
        }

        private static void SeedInvalidCc(DatabaseService db)
        {
            var panel = Panel(db, "E2E invalid check cell", InvalidCc, 3, true);
            Type(db, panel, "1 K+ k-", "2 K- k+", "3 K+ k-");
            var run = Open(db, InvalidCc, "serum", panel, dat: "Negative",
                notes: "Cell 1 is a nonreactive K+ cell whose check cell did not react.");
            foreach (var cell in db.GetPanelCells(panel))
            {
                if (cell.CellNumber == "AC")
                {
                    db.SaveReaction(run, "AC", "0", "0", "0", "0");
                    continue;
                }
                var cc = cell.CellNumber == "1" ? "0" : "2+";
                db.SaveReaction(run, cell.CellNumber, "0", "0", "0", cc);
            }
        }

        private static void SeedNegative(DatabaseService db)
        {
            var panel = Panel(db, "E2E no antibody", Negative, 4, true);
            Type(db, panel, "1 E+ K-", "2 E- K+", "3 E+ K+", "4 E- K-");
            var run = Open(db, Negative, "serum", panel, dat: "Negative");
            AllNegative(db, run, panel);
        }

        private static void SeedPanAcNeg(DatabaseService db)
        {
            var panel = Panel(db, "E2E panreactive negative autocontrol", PanAcNeg, 6, true);
            Type(db, panel,
                "1 D+ C+ c- E- e+ K- Fya+ Jka+",
                "2 D- C- c+ E+ e- K+ Fya- Jka-",
                "3 D+ C- c+ E- e+ K- Fya+ Jka-",
                "4 D- C+ c- E+ e- K+ Fya- Jka+",
                "5 D+ C+ c+ E- e+ K- Fya- Jka+",
                "6 D- C- c+ E+ e+ K+ Fya+ Jka-");
            var run = Open(db, PanAcNeg, "serum", panel, dat: "Negative",
                notes: "All panel cells reactive. Autocontrol negative. No single common allo fits.");
            Pan(db, run, panel, isP: "0", c37: "0", ahg: "2+", acAhg: "0");
        }

        private static int Open(DatabaseService db, string accession, string type, int panelId,
            string? notes = null, string? dat = null)
        {
            db.AddSpecimen(accession, type, Days(45), notes: notes, datResult: dat);
            db.LinkSpecimenPanel(accession, panelId);
            return db.GetOrCreateDefaultRun(accession, panelId);
        }

        private static int Panel(DatabaseService db, string name, string lot, int cells, bool ac, params string?[] extras)
        {
            var id = db.AddPanel(name, lot, "E2E", cells, Days(365), ac, 1);
            foreach (var extra in extras)
            {
                if (!string.IsNullOrEmpty(extra))
                    db.AddPanelExtraAntigen(id, extra);
            }
            return id;
        }

        private static string? Warehouse(string antigen) =>
            AntigenConstants.IsWarehouse(antigen) ? antigen : null;

        private static void Type(DatabaseService db, int panelId, params string[] rows)
        {
            var cells = db.GetPanelCells(panelId);
            foreach (var row in rows)
            {
                var parts = row.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;
                var cell = cells.Find(c => c.CellNumber == parts[0]);
                if (cell == null) continue;
                for (var i = 1; i < parts.Length; i++)
                {
                    var token = parts[i];
                    string name;
                    string value;
                    if (token.EndsWith("++", StringComparison.Ordinal))
                    {
                        name = token[..^2];
                        value = "++";
                    }
                    else if (token.EndsWith('+'))
                    {
                        name = token[..^1];
                        value = "+";
                    }
                    else if (token.EndsWith('-'))
                    {
                        name = token[..^1];
                        value = "-";
                    }
                    else continue;
                    db.UpdatePanelCellAntigen(cell.Id, name, value);
                }
            }
        }

        private static void ByAntigen(DatabaseService db, int runId, int panelId, string antigen,
            string ahg, string isP = "NT", string c37 = "NT", string acAhg = "0", string acIs = "0")
        {
            foreach (var cell in db.GetPanelCells(panelId))
            {
                if (cell.CellNumber == "AC")
                {
                    Save(db, runId, "AC", acIs, "0", acAhg);
                    continue;
                }
                if (cell.IsAntigenPositive(antigen))
                    Save(db, runId, cell.CellNumber, isP, c37, ahg);
                else
                    Save(db, runId, cell.CellNumber, "0", "0", "0");
            }
        }

        private static void ByAny(DatabaseService db, int runId, int panelId, string[] antigens, string ahg)
        {
            foreach (var cell in db.GetPanelCells(panelId))
            {
                if (cell.CellNumber == "AC")
                {
                    Save(db, runId, "AC", "0", "0", "0");
                    continue;
                }
                var hit = false;
                foreach (var ag in antigens)
                {
                    if (cell.IsAntigenPositive(ag)) { hit = true; break; }
                }
                Save(db, runId, cell.CellNumber, hit ? "NT" : "0", hit ? "NT" : "0", hit ? ahg : "0");
            }
        }

        private static void ByDosage(DatabaseService db, int runId, int panelId, string antigen, string homo, string hetero)
        {
            foreach (var cell in db.GetPanelCells(panelId))
            {
                if (cell.CellNumber == "AC")
                {
                    Save(db, runId, "AC", "0", "0", "0");
                    continue;
                }
                if (!cell.IsAntigenPositive(antigen))
                    Save(db, runId, cell.CellNumber, "0", "0", "0");
                else if (cell.IsHomozygousFor(antigen))
                    Save(db, runId, cell.CellNumber, "NT", "NT", homo);
                else
                    Save(db, runId, cell.CellNumber, "NT", "NT", hetero);
            }
        }

        private static void Pan(DatabaseService db, int runId, int panelId,
            string isP, string c37, string ahg, string acAhg, string acIs = "0")
        {
            foreach (var cell in db.GetPanelCells(panelId))
            {
                if (cell.CellNumber == "AC")
                    Save(db, runId, "AC", acIs, c37, acAhg);
                else
                    Save(db, runId, cell.CellNumber, isP, c37, ahg);
            }
        }

        private static void AllNegative(DatabaseService db, int runId, int panelId)
        {
            foreach (var cell in db.GetPanelCells(panelId))
                Save(db, runId, cell.CellNumber, "0", "0", "0");
        }

        private static void Save(DatabaseService db, int runId, string cell, string isP, string c37, string ahg,
            IReadOnlyDictionary<string, string>? extra = null)
        {
            var cc = ahg == "0" ? "2+" : "NT";
            if (cell == "AC") cc = "0";
            db.SaveReaction(runId, cell, isP, c37, ahg, cc, extra);
        }

        private static string Days(int days) => DateTime.Now.AddDays(days).ToString("yyyy-MM-dd");
    }
}
