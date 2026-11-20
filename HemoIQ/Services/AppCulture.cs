using System;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace HemoIQ.Services
{
    /// <summary>
    /// HemoIQ stores dates as text ("yyyy-MM-dd", "yyyy-MM-dd HH:mm") and compares them as text,
    /// and much of the code formats and parses them with the current culture. On a workstation
    /// whose regional settings use another calendar (for example Thai Buddhist, where "yyyy" is
    /// 2569 in 2026) or another time separator (for example "." in Finnish), stored dates would be
    /// corrupted and date comparisons would break. At startup the user's culture is kept for
    /// language and number display but its calendar is forced to Gregorian and its time
    /// separator to ":"; if the culture cannot use the Gregorian calendar, invariant date
    /// formatting is used instead.
    /// </summary>
    public static class AppCulture
    {
        /// <summary>A copy of <paramref name="culture"/> that formats and parses dates the way HemoIQ stores them.</summary>
        public static CultureInfo Normalize(CultureInfo culture)
        {
            var c = (CultureInfo)culture.Clone();
            var gregorian = c.OptionalCalendars.OfType<GregorianCalendar>()
                .FirstOrDefault(g => g.CalendarType == GregorianCalendarTypes.Localized)
                ?? c.OptionalCalendars.OfType<GregorianCalendar>().FirstOrDefault();
            if (gregorian != null)
            {
                if (c.DateTimeFormat.Calendar is not GregorianCalendar)
                    c.DateTimeFormat.Calendar = gregorian;
            }
            else
            {
                c.DateTimeFormat = (DateTimeFormatInfo)CultureInfo.InvariantCulture.DateTimeFormat.Clone();
            }
            c.DateTimeFormat.TimeSeparator = ":";
            return c;
        }

        /// <summary>Applies <see cref="Normalize"/> to the current and all future threads.</summary>
        public static void Apply()
        {
            var culture = Normalize(CultureInfo.CurrentCulture);
            var ui = CultureInfo.CurrentUICulture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = ui;
            Thread.CurrentThread.CurrentCulture = culture;
        }
    }
}
