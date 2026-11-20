using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

[Collection("PersistentDatabase")]
public class PanelTypingValueTests
{
    [Theory]
    [InlineData("+", "+")]
    [InlineData("-", "-")]
    [InlineData("++", "++")]
    [InlineData("NT", "NT")]
    [InlineData(" + ", "+")]
    [InlineData("", "NT")]
    public void AcceptedValues_AreStored_Trimmed(string given, string stored)
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("PT", "PT-1", "V", 1, null, false);
        var cell = iso.Db.GetPanelCells(panel).Single();
        iso.Db.UpdatePanelCellAntigen(cell.Id, "Jka", given);
        Assert.Equal(stored, iso.Db.GetPanelCells(panel).Single().Antigens["Jka"]);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("w")]
    [InlineData("+w")]
    [InlineData("pos")]
    public void OtherValues_AreRefused_AndTheTypingIsUnchanged(string given)
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("PT", "PT-2", "V", 1, null, false);
        var cell = iso.Db.GetPanelCells(panel).Single();
        iso.Db.UpdatePanelCellAntigen(cell.Id, "Jka", "+");

        var ex = Assert.Throws<ArgumentException>(() => iso.Db.UpdatePanelCellAntigen(cell.Id, "Jka", given));
        Assert.Equal($"Cell 1 Jka: \"{given}\" is not a panel typing value (+, -, ++ for homozygous, or NT).", ex.Message);
        Assert.Equal("+", iso.Db.GetPanelCells(panel).Single().Antigens["Jka"]);
    }

    [Fact]
    public void WarehouseAntigens_AreValidatedToo()
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("PT", "PT-3", "V", 1, null, false);
        iso.Db.AddPanelExtraAntigen(panel, "Doa");
        var cell = iso.Db.GetPanelCells(panel).Single();
        Assert.Throws<ArgumentException>(() => iso.Db.UpdatePanelCellAntigen(cell.Id, "Doa", "weak"));
        iso.Db.UpdatePanelCellAntigen(cell.Id, "Doa", "+");
        Assert.Equal("+", iso.Db.GetPanelCells(panel).Single().Antigens["Doa"]);
    }
}
