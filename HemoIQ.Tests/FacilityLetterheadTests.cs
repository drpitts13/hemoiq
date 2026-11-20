using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// Reports carry the facility name, address and contact information set in Preferences. Anything not set is left
/// off, and with nothing set the report starts with the product line as before.
/// </summary>
[Collection("PersistentDatabase")]
public class FacilityLetterheadTests
{
    private static void WithFacility(string name, string address, string contact, Action body)
    {
        var s = AppSettings.Current;
        var (n, a, c) = (s.FacilityName, s.FacilityAddress, s.FacilityContact);
        try
        {
            s.FacilityName = name;
            s.FacilityAddress = address;
            s.FacilityContact = contact;
            body();
        }
        finally
        {
            s.FacilityName = n;
            s.FacilityAddress = a;
            s.FacilityContact = c;
        }
    }

    private static IsolatedDatabase WithSpecimenAndPanel()
    {
        var iso = new IsolatedDatabase();
        var panel = iso.Db.AddPanel("LH", "LH-1", "V", 2, null, false, startTypedNegative: true);
        iso.Db.AddSpecimen("LH-1", "serum", null);
        iso.Db.LinkSpecimenPanel("LH-1", panel);
        iso.Db.SaveReaction("LH-1", panel, "1", "0", "0", "0", "2+");
        return iso;
    }

    public static IEnumerable<object[]> AllReportTypes() =>
        Enum.GetValues<ReportType>().Select(t => new object[] { t });

    [Theory]
    [MemberData(nameof(AllReportTypes))]
    public void EveryReport_StartsWithTheLetterhead_WhenSet(ReportType type)
    {
        using var iso = WithSpecimenAndPanel();
        var panelId = iso.Db.GetAllPanels().Single().PanelId;
        WithFacility("St. Example Medical Center", "  100 Main Street  \n\n Springfield, ST 01234 ", "Phone (555) 010-0000\nbloodbank@example.org",
            () =>
            {
                var text = iso.Reports.GeneratePreviewText(type, "LH-1", panelId);
                var lines = text.Replace("\r\n", "\n").Split('\n');
                Assert.Equal("St. Example Medical Center", lines[0]);
                Assert.Equal("100 Main Street", lines[1]);
                Assert.Equal("Springfield, ST 01234", lines[2]);
                Assert.Equal("Phone (555) 010-0000", lines[3]);
                Assert.Equal("bloodbank@example.org", lines[4]);
                Assert.Equal("", lines[5]);
                Assert.StartsWith(SoftwareIdentity.ProductName, lines[6]);
            });
    }

    [Fact]
    public void NothingSet_NoLetterhead_ReportStartsWithTheProductLine()
    {
        using var iso = WithSpecimenAndPanel();
        WithFacility("", "", "", () =>
        {
            Assert.Equal("", ReportService.FacilityHeader(AppSettings.Current));
            var text = iso.Reports.GeneratePreviewText(ReportType.ClinicalIdentification, "LH-1");
            Assert.StartsWith(SoftwareIdentity.ProductName, text);
        });
    }

    [Fact]
    public void OnlyContactSet_OnlyContactIsPrinted()
    {
        WithFacility("  ", "", "Fax (555) 010-0001", () =>
        {
            Assert.Equal(new[] { "Fax (555) 010-0001" }, AppSettings.Current.FacilityHeaderLines());
            Assert.Equal("Fax (555) 010-0001" + Environment.NewLine + Environment.NewLine,
                ReportService.FacilityHeader(AppSettings.Current));
        });
    }

    [Fact]
    public void NormalizeMultiline_TrimsLines_AndDropsBlankOnes()
    {
        Assert.Equal("A\nB", LabSettings.NormalizeMultiline("  A \r\n\r\n   \r\nB  "));
        Assert.Equal("", LabSettings.NormalizeMultiline("  \n \r\n"));
        Assert.Equal("", LabSettings.NormalizeMultiline(null));
    }

    [Fact]
    public void PreferencesFile_CarriesTheLetterhead_AndRefusesOverlongValues()
    {
        var source = new LabSettings
        {
            FacilityName = "St. Example Medical Center",
            FacilityAddress = "100 Main Street\nSpringfield",
            FacilityContact = "Phone (555) 010-0000",
        };
        var json = PreferenceTransfer.Export(source, new DateTime(2026, 11, 20));
        var preview = PreferenceTransfer.Preview(json, new LabSettings());
        Assert.Equal("St. Example Medical Center", Assert.Single(preview.Changes, c => c.Name == "facilityName").After);
        Assert.Contains(preview.Changes, c => c.Name == "facilityAddress");
        Assert.Contains(preview.Changes, c => c.Name == "facilityContact");

        var tooLong = json.Replace("\"St. Example Medical Center\"", "\"" + new string('x', 121) + "\"");
        Assert.Contains(PreferenceTransfer.Preview(tooLong, new LabSettings()).Refused,
            r => r.StartsWith("facilityName: is longer than 120 characters"));
    }
}
