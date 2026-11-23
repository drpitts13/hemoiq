using HemoIQ.Data;

namespace HemoIQ.ViewModels
{
    /// <summary>
    /// Opening a specimen from the Worklist, Search or another tab. A tab whose cached list does not hold the specimen
    /// (deleted or added on another workstation since the list loaded, or inactive on the Reactions tab) must not fall
    /// back to the first specimen in its list: that is another patient's workup, opened under the user's expectation
    /// of this one. The tab reloads its list once; if the specimen still cannot be shown, this says why and the
    /// caller leaves the tab and its selection alone.
    /// </summary>
    public static class SpecimenNavigation
    {
        /// <summary>Why <paramref name="accessionNumber"/> cannot be opened on a tab, or null when it can.</summary>
        public static string? CannotOpen(DatabaseService db, string accessionNumber, string tabName, bool activeOnly)
        {
            var specimen = db.GetSpecimen(accessionNumber);
            if (specimen == null)
                return $"Specimen {accessionNumber} was not found, so the {tabName} tab was not opened on it. It may " +
                       "have been deleted on another workstation; refresh the list you opened it from.";
            if (activeOnly && !specimen.IsActive)
                return $"Specimen {accessionNumber} is inactive, so the {tabName} tab does not list it. Reactivate it " +
                       "on the Specimens tab (Edit, then Active) to work on it there.";
            return null;
        }
    }
}
