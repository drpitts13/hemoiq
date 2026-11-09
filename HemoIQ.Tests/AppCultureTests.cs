using System.Globalization;
using HemoIQ.Services;
using HemoIQ.Tests.Infrastructure;

namespace HemoIQ.Tests;

/// <summary>
/// Stored dates must be Gregorian "yyyy-MM-dd HH:mm" whatever the workstation's regional settings.
/// Each test first shows the defect under the raw culture, then that the normalized culture fixes it.
/// </summary>
[Collection("PersistentDatabase")]
public class AppCultureTests
{
    private static readonly DateTime When = new(2026, 11, 9, 10, 30, 0);

    private static T OnThread<T>(CultureInfo culture, Func<T> body)
    {
        T result = default!;
        Exception? error = null;
        var t = new Thread(() =>
        {
            try { result = body(); }
            catch (Exception ex) { error = ex; }
        }) { CurrentCulture = culture };
        t.Start();
        t.Join();
        if (error != null) throw new Xunit.Sdk.XunitException(error.ToString());
        return result;
    }

    [Theory]
    [InlineData("th-TH")] // Thai Buddhist calendar: year 2569
    [InlineData("ar-SA")] // Um al-Qura calendar
    public void NonGregorianCalendars_FormatTheGregorianYearAfterNormalizing(string name)
    {
        var raw = CultureInfo.GetCultureInfo(name);
        Assert.NotEqual("2026-11-09", OnThread(raw, () => When.ToString("yyyy-MM-dd")));
        Assert.Equal("2026-11-09 10:30", OnThread(AppCulture.Normalize(raw), () => When.ToString("yyyy-MM-dd HH:mm")));
    }

    [Fact]
    public void TimeSeparator_IsColon_AfterNormalizing()
    {
        var fi = CultureInfo.GetCultureInfo("fi-FI");
        Assert.Equal("10.30", OnThread(fi, () => When.ToString("HH:mm")));
        Assert.Equal("10:30", OnThread(AppCulture.Normalize(fi), () => When.ToString("HH:mm")));
    }

    [Fact]
    public void StoredIsoDates_ParseAsTheSameDay_AfterNormalizing()
    {
        var th = AppCulture.Normalize(CultureInfo.GetCultureInfo("th-TH"));
        var parsed = OnThread(th, () => DateTime.TryParse("2026-11-09", out var d) ? d : DateTime.MinValue);
        Assert.Equal(new DateTime(2026, 11, 9), parsed);
    }

    [Fact]
    public void Normalizing_KeepsTheLanguageAndAGregorianCultureUnchangedInEffect()
    {
        var us = CultureInfo.GetCultureInfo("en-US");
        var n = AppCulture.Normalize(us);
        Assert.Equal("en-US", n.Name);
        Assert.Equal(When.ToString("yyyy-MM-dd HH:mm", us), When.ToString("yyyy-MM-dd HH:mm", n));
        Assert.Equal("November", n.DateTimeFormat.GetMonthName(11));
    }

    [Fact]
    public void AppStartup_AppliesTheCultureFirst()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HemoIQ", "HemoIQ.csproj"))) dir = dir.Parent;
        Assert.NotNull(dir);
        var src = File.ReadAllText(Path.Combine(dir!.FullName, "HemoIQ", "App.xaml.cs"));
        var body = src[src.IndexOf("OnStartup(", StringComparison.Ordinal)..];
        var apply = body.IndexOf("AppCulture.Apply();", StringComparison.Ordinal);
        Assert.True(apply > 0, "App.OnStartup must call AppCulture.Apply().");
        Assert.True(apply < body.IndexOf("AppDataPaths.", StringComparison.Ordinal),
            "AppCulture.Apply() must run before anything reads or writes dates.");
    }

    [Fact]
    public void Database_StoresGregorianTimes_UnderANormalizedThaiCulture()
    {
        var stored = OnThread(AppCulture.Normalize(CultureInfo.GetCultureInfo("th-TH")), () =>
        {
            using var iso = new IsolatedDatabase();
            iso.Db.AddSpecimen("CU-1", "serum", null);
            iso.Db.UpdateSpecimenCollection("CU-1", "2026-11-08 09:15", null);
            iso.Db.SetSpecimenFinalCall("CU-1", "anti-E", null, "TE", identifiedAt: When);
            var s = iso.Db.GetSpecimen("CU-1")!;
            return (s.CollectedAt, s.IdentifiedAt);
        });
        Assert.Equal("2026-11-08 09:15", stored.CollectedAt);
        Assert.Equal("2026-11-09 10:30", stored.IdentifiedAt);
    }
}
