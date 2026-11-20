using HemoIQ.Models;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

public class PatientSexTests
{
    [Fact]
    public void Normalize_AcceptsCodesAndLetters_DefaultsToUnknown()
    {
        Assert.Equal(PatientSex.Female, PatientSex.Normalize(" F "));
        Assert.Equal(PatientSex.Male, PatientSex.Normalize("Male"));
        Assert.Equal(PatientSex.Other, PatientSex.Normalize("x"));
        Assert.Equal(PatientSex.Unknown, PatientSex.Normalize(null));
        Assert.Equal(PatientSex.Unknown, PatientSex.Normalize("bogus"));
        Assert.Equal("Other / X", PatientSex.Label("other"));
    }

    [Fact]
    public void SavePatient_StoresSex_AuditsChange_NoOpWhenUnchanged_AndShowsOnDisplay()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.SavePatient(new Patient { Mrn = "PS-1", Name = "Doe, Jane", Sex = "F" });
        var p = iso.Db.GetPatient("PS-1")!;
        Assert.Equal(PatientSex.Female, p.Sex);
        Assert.Equal("Doe, Jane (PS-1), Female", p.Display);

        iso.Db.SavePatient(new Patient { Mrn = "PS-1", Name = "Doe, Jane", Sex = "female" });
        Assert.Single(iso.Db.GetAuditEventsForEntity("patient", "PS-1"));

        iso.Db.SavePatient(new Patient { Mrn = "PS-1", Name = "Doe, Jane", Sex = PatientSex.Male });
        var update = Assert.Single(iso.Db.GetAuditEventsForEntity("patient", "PS-1"), e => e.Action == "update_patient");
        Assert.Contains("\"sex\":\"female\"", update.BeforeJson);
        Assert.Contains("\"sex\":\"male\"", update.AfterJson);

        iso.Db.SavePatient(new Patient { Mrn = "PS-2", Name = "No Sex" });
        Assert.Equal(PatientSex.Unknown, iso.Db.GetPatient("PS-2")!.Sex);
        Assert.Equal("No Sex (PS-2)", iso.Db.GetPatient("PS-2")!.Display);
    }

    [Fact]
    public void Database_RejectsUnknownSexCode()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.SavePatient(new Patient { Mrn = "PS-3" });
        using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={iso.DbPath};Pooling=False");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE patients SET sex = 'm' WHERE mrn = 'PS-3'";
        Assert.Contains("CHECK", Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => cmd.ExecuteNonQuery()).Message);
    }

    [Fact]
    public void PregnancyConflict_OnlyForMalePatientWithPregnantOrRecent()
    {
        Assert.NotNull(PatientSexCheck.PregnancyConflict("male", PregnancyStatus.Pregnant, "M1"));
        Assert.Contains("recently pregnant", PatientSexCheck.PregnancyConflict("male", PregnancyStatus.Recent, "M1"));
        Assert.Null(PatientSexCheck.PregnancyConflict("male", PregnancyStatus.NotPregnant, "M1"));
        Assert.Null(PatientSexCheck.PregnancyConflict("female", PregnancyStatus.Pregnant, "M1"));
        Assert.Null(PatientSexCheck.PregnancyConflict("unknown", PregnancyStatus.Pregnant, "M1"));
        Assert.Null(PatientSexCheck.PregnancyConflict("other", PregnancyStatus.Pregnant, "M1"));
        Assert.Empty(PatientSexCheck.ReviewNotes(null, new Specimen { PregnancyStatus = PregnancyStatus.Pregnant }));
    }

    [Fact]
    public void Analysis_AddsReviewSuggestion_WhenMalePatientHasPregnancyRecorded()
    {
        using var iso = new IsolatedDatabase();
        iso.Db.SavePatient(new Patient { Mrn = "PS-4", Sex = PatientSex.Male });
        iso.Db.AddSpecimen("PS-4A", "serum", null);
        iso.Db.UpdateSpecimenMrn("PS-4A", "PS-4");
        var panel = iso.Db.AddPanel("PS P", "PSL", "V", 1, null, false, startTypedNegative: true);
        iso.Db.LinkSpecimenPanel("PS-4A", panel);
        iso.Db.SaveReaction("PS-4A", panel, "1", "0", "0", "0", "0");

        Assert.DoesNotContain(iso.Analyzer.AnalyzeSpecimen("PS-4A", updateDb: false).Suggestions, s => s.Contains("recorded as male"));
        iso.Db.UpdateSpecimenPregnancy("PS-4A", PregnancyStatus.Pregnant, null, null);
        var result = iso.Analyzer.AnalyzeSpecimen("PS-4A", updateDb: false);
        Assert.Contains(result.Suggestions, s => s.StartsWith("REVIEW: The specimen records pregnant, but patient PS-4 is recorded as male."));
    }
}
