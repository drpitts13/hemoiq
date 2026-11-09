using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

public class SearchByPanelLotTests
{
    [Fact]
    public void Search_FindsInvestigationsByPanelLotAndManufacturer_AndShowsPanelsAndEncounter()
    {
        using var iso = new IsolatedDatabase();
        var resolve = iso.Db.AddPanel("Resolve A", "LOT-4411", "Ortho", 2, null, false);
        var other = iso.Db.AddPanel("Panocell", "LOT-9000", "Immucor", 2, null, false);
        iso.Db.AddSpecimen("SL-1", "serum", null);
        iso.Db.AddSpecimen("SL-2", "serum", null);
        iso.Db.AddSpecimen("SL-3", "serum", null);
        iso.Db.LinkSpecimenPanel("SL-1", resolve);
        iso.Db.LinkSpecimenPanel("SL-2", other);
        iso.Db.UpdateSpecimenEncounter("SL-1", "V-55");

        var hit = Assert.Single(InvestigationSearch.Search(iso.Db, "lot-4411"));
        Assert.Equal("SL-1", hit.Accession);
        Assert.Equal("Resolve A (Ortho, lot LOT-4411)", hit.Panels);
        Assert.Equal("V-55", hit.Encounter);

        Assert.Equal("SL-2", Assert.Single(InvestigationSearch.Search(iso.Db, "immucor")).Accession);
        Assert.Equal(string.Empty, InvestigationSearch.Search(iso.Db, "SL-3").Single().Panels);
    }

    [Fact]
    public void PanelLinkedAndRunTwice_IsListedOnce()
    {
        using var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("Selected", "SEL-7", null, 2, null, false);
        iso.Db.AddSpecimen("SL-4", "serum", null);
        iso.Db.LinkSpecimenPanel("SL-4", panel);
        iso.Db.AddPanelRun("SL-4", panel, CellTreatment.Ficin, SerumTreatment.None, "ficin");

        var map = iso.Db.GetPanelsBySpecimen();
        Assert.Single(map["SL-4"]);
        Assert.Equal("Selected (lot SEL-7)", InvestigationSearch.Search(iso.Db, "sel-7").Single().Panels);
    }

    [Fact]
    public void DescribePanels_JoinsNameVendorLot_AndSkipsBlanks()
    {
        Assert.Equal("A (V, lot 1); B; C (lot 3)", InvestigationSearch.DescribePanels(new (string, string?, string?)[]
        {
            ("A", "V", "1"), ("B", " ", null), ("C", null, " 3 "),
        }));
    }
}
