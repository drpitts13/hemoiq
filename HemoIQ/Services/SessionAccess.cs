using HemoIQ.Data;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    public sealed record SessionInfo(string Text, string Detail, bool CanRecord, bool ViewOnly);

    /// <summary>
    /// Who is using the application and whether they may change laboratory data, for the status
    /// bar and for disabling change commands. Screens use the cached <see cref="CanRecord"/>; the
    /// database still checks every write (<see cref="Permission.RecordResults"/>), so a stale
    /// cache can only hide a button, never allow a change.
    /// </summary>
    public static class SessionAccess
    {
        public static SessionInfo Current { get; private set; } =
            new("Users not registered", "", CanRecord: true, ViewOnly: false);

        public static bool CanRecord => Current.CanRecord;

        public static SessionInfo Describe(DatabaseService db)
        {
            if (!AccessControl.RegistryEnabled(db))
                return new SessionInfo("Users not registered — all actions allowed",
                    "No HemoIQ users are registered, so every Windows login may do everything. Register users in Preferences > Users & roles.",
                    CanRecord: true, ViewOnly: false);
            var canRecord = AccessControl.Can(db, Permission.RecordResults);
            var user = AccessControl.CurrentUser(db);
            if (user == null)
                return new SessionInfo($"Login '{AccessControl.CurrentLogin}' not registered — view only",
                    "This Windows login is not an active HemoIQ user, so it can view but not change laboratory data. " +
                    "Ask an Administrator to register it in Preferences > Users & roles.",
                    canRecord, ViewOnly: !canRecord);
            var role = UserRole.Label(user.Role);
            return new SessionInfo(
                $"Signed in: {user.DisplayName} ({role}){(canRecord ? "" : " — view only")}",
                canRecord
                    ? $"Windows login '{user.Login}', initials {user.Initials}. Changes are recorded under this login."
                    : $"The {role} role can view investigations, reports and analytics but not change laboratory data.",
                canRecord, ViewOnly: !canRecord);
        }

        public static SessionInfo Refresh(DatabaseService db) => Current = Describe(db);
    }
}
