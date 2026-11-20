using HemoIQ.Models;
using HemoIQ.Tests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace HemoIQ.Tests;

public class ValidationStressTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("VALID-001")]
    [InlineData("ACC-2024-001")]
    [InlineData("A'B\"C;--")]
    [InlineData("日本語-001")]
    public void AddSpecimen_AcceptsVariousAccessionFormats(string accession)
    {
        if (string.IsNullOrWhiteSpace(accession)) return;
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen(accession.Trim(), "serum", null);
        Assert.NotNull(iso.Db.GetSpecimen(accession.Trim()));
    }

    [Fact]
    public void SqlInjectionInAccession_DoesNotCorruptDatabase()
    {
        using var iso = new IsolatedDatabase();
        var malicious = "'; DROP TABLE specimens; --";
        iso.Db.AddSpecimen(malicious, "serum", null);
        var retrieved = iso.Db.GetSpecimen(malicious);
        Assert.NotNull(retrieved);
        Assert.True(iso.Db.GetAllSpecimens().Count >= 1);
    }

    [Fact]
    public void SqlInjectionInPanelName_DoesNotCorruptDatabase()
    {
        using var iso = new IsolatedDatabase();
        var name = "Panel'; DELETE FROM panels; --";
        var panelId = iso.Db.AddPanel(name, "L1", "V", 1, null, false);
        Assert.NotNull(iso.Db.GetPanel(panelId));
        Assert.Equal(name, iso.Db.GetPanel(panelId)!.Name);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1+")]
    [InlineData("2+")]
    [InlineData("3+")]
    [InlineData("4+")]
    [InlineData("NT")]
    public void SaveReaction_AcceptsValidReactionGrades(string grade)
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("RXN-VAL", "serum", null);
        var panelId = iso.Db.AddPanel("P", "L", "V", 1, null, false);
        iso.Db.LinkSpecimenPanel("RXN-VAL", panelId);
        iso.Db.SaveReaction("RXN-VAL", panelId, "1", grade, grade, grade, grade);
        var rxn = iso.Db.GetReactions("RXN-VAL", panelId).Single();
        Assert.Equal(grade, rxn.AHG);
    }

    [Fact]
    public void SaveReaction_UnrecognizedValues_AreRefused_AndNothingIsStored()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("RXN-BAD", "serum", null);
        var panelId = iso.Db.AddPanel("P", "L", "V", 1, null, false);
        iso.Db.LinkSpecimenPanel("RXN-BAD", panelId);
        var ex = Assert.Throws<ArgumentException>(() =>
            iso.Db.SaveReaction("RXN-BAD", panelId, "1", "INVALID", "999", "??", "5+"));
        Assert.Equal("Cell 1 IS: \"INVALID\" is not a recognized grade (0, w+, 1+ to 4+, MF, H or NT).", ex.Message);
        Assert.Empty(iso.Db.GetReactions("RXN-BAD", panelId));
    }

    [Theory]
    [InlineData("+")]
    [InlineData("-")]
    public void UpdatePanelCellAntigen_AcceptsValidAntigenValues(string value)
    {
        using var iso = new IsolatedDatabase();
        var panelId = iso.Db.AddPanel("P", "L", "V", 1, null, false);
        var cell = iso.Db.GetPanelCells(panelId).Single();
        iso.Db.UpdatePanelCellAntigen(cell.Id, "D", value);
        Assert.Equal(value, iso.Db.GetPanelCells(panelId).Single().GetAntigen("D"));
    }

    [Fact]
    public void UpdatePanelCellAntigen_InvalidAntigenName_ThrowsSqliteException()
    {
        using var iso = new IsolatedDatabase();
        var panelId = iso.Db.AddPanel("P", "L", "V", 1, null, false);
        var cell = iso.Db.GetPanelCells(panelId).Single();
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(
            () => iso.Db.UpdatePanelCellAntigen(cell.Id, "INVALID-AG", "+"));
    }

    [Fact]
    public void SearchCellsByProfile_IgnoresInvalidCriteria()
    {
        using var iso = new IsolatedDatabase();
        var panelId = iso.Db.AddPanel("Search P", "L", "V", 2, null, false);
        var cells = iso.Db.GetPanelCells(panelId);
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "D", "+");

        var emptyCriteria = new Dictionary<string, string> { { "INVALID", "+" } };
        Assert.Empty(iso.Db.SearchCellsByProfile(emptyCriteria));

        var invalidValue = new Dictionary<string, string> { { "D", "X" } };
        Assert.Empty(iso.Db.SearchCellsByProfile(invalidValue));
    }

    [Fact]
    public void SearchCellsByProfile_FindsMatchingCells()
    {
        using var iso = new IsolatedDatabase();
        var panelId = iso.Db.AddPanel("Search P", "L", "V", 2, null, false);
        var cells = iso.Db.GetPanelCells(panelId);
        iso.Db.UpdatePanelCellAntigen(cells[0].Id, "D", "+");
        iso.Db.UpdatePanelCellAntigen(cells[1].Id, "D", "-");

        var results = iso.Db.SearchCellsByProfile(new Dictionary<string, string> { { "D", "+" } });
        Assert.Single(results);
        Assert.Equal("+", results[0].cell.GetAntigen("D"));
    }

    [Fact]
    public void DuplicateSpecimenPrimaryKey_ThrowsSqliteException()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("DUP-001", "serum", null);
        Assert.Throws<SqliteException>(() => iso.Db.AddSpecimen("DUP-001", "plasma", null));
    }

    [Fact]
    public void DeleteNonexistentSpecimen_DoesNotThrow()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.DeleteSpecimen("DOES-NOT-EXIST");
        Assert.Null(iso.Db.GetSpecimen("DOES-NOT-EXIST"));
    }

    [Fact]
    public void AddPanel_ZeroCells_CreatesNoDataCells()
    {
        using var iso = new IsolatedDatabase();
        var panelId = iso.Db.AddPanel("Empty", "L", "V", 0, null, false);
        Assert.Empty(iso.Db.GetPanelCells(panelId));
    }

    [Fact]
    public void LargePanel_HundredCells_HandlesGracefully()
    {
        using var iso = new IsolatedDatabase();
        var panelId = iso.Db.AddPanel("Large Panel", "L", "V", 100, null, false);
        Assert.Equal(100, iso.Db.GetPanelCells(panelId).Count);
    }

    [Fact]
    public void ManySpecimens_BulkInsert_Succeeds()
    {
        using var iso = new IsolatedDatabase();
        for (int i = 0; i < 500; i++)
            iso.Db.AddSpecimen($"BULK-{i:D4}", "serum", null);
        Assert.Equal(500, iso.Db.GetAllSpecimens().Count);
    }

    [Fact]
    public void AnalyzeSpecimen_WithGarbageReactionValues_DoesNotCrash()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.AddSpecimen("GARBAGE", "serum", null);
        var panelId = iso.Db.AddPanel("P", "L", "V", 3, null, false);
        iso.Db.LinkSpecimenPanel("GARBAGE", panelId);
        iso.Db.SaveReaction("GARBAGE", panelId, "1", "0", "0", "0", "0");
        iso.Db.SaveReaction("GARBAGE", panelId, "2", "0", "0", "0", "0");
        // Older or imported data can still hold unrecognized grades; analysis must cope with them.
        using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={iso.DbPath};Pooling=False"))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE reactions SET \"IS\" = 'BAD', C37 = 'BAD', AHG = 'BAD', CC = 'BAD' WHERE cell_number = '1'";
            Assert.Equal(1, cmd.ExecuteNonQuery());
        }
        iso.Db.SaveReaction("GARBAGE", panelId, "3", "NT", "NT", "NT", "NT");

        var result = iso.Analyzer.AnalyzeSpecimen("GARBAGE");
        Assert.NotNull(result);
    }

    [Fact]
    public void RuleCrud_InvalidMinRuleoutCount_IsRefused()
    {
        // Analysis clamps the count to 1-5, so storing -1 would show a rule that is not what analysis uses.
        using var iso = new IsolatedDatabase();
        var ex = Assert.Throws<ArgumentException>(() => iso.Db.AddRule("Edge Rule", null, "anti-D", null, false, -1));
        Assert.Equal("Rule-out cells must be a whole number from 1 to 5; -1 was entered.", ex.Message);
        Assert.DoesNotContain(iso.Db.GetAllRules(), r => r.Name == "Edge Rule");
    }
}
