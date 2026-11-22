using System;
using System.Collections.Generic;
using System.Linq;
using HemoIQ.Data;
using HemoIQ.Models;

namespace HemoIQ.Services
{
    public enum Permission
    {
        SupervisorReview,
        ClearIdentification,
        ReinstateInvestigation,
        ConfigurePreferences,
        ConfigureRules,
        ManageUsers,
        PurgeData,
        /// <summary>Record or change laboratory data: specimens, patients, panels, reactions, identifications.</summary>
        RecordResults,
    }

    /// <summary>
    /// Role-based permissions over the local user registry. While no active user is
    /// registered, every action is allowed (the behavior before roles existed). Once
    /// users exist, the signed-in Windows login must be a registered, active user whose
    /// role grants the permission. Every data change needs at least
    /// <see cref="Permission.RecordResults"/>, so an unregistered login or a Read-only
    /// user can view but not change anything.
    /// </summary>
    public static class AccessControl
    {
        /// <summary>Windows login of the person using the application. Tests may replace it.</summary>
        public static string CurrentLogin { get; set; } = Environment.UserName ?? string.Empty;

        private static readonly Dictionary<Permission, string[]> Grants = new()
        {
            [Permission.SupervisorReview] = new[] { UserRole.Supervisor, UserRole.Administrator },
            [Permission.ClearIdentification] = new[] { UserRole.Supervisor, UserRole.Administrator },
            [Permission.ReinstateInvestigation] = new[] { UserRole.Supervisor, UserRole.Administrator },
            [Permission.ConfigurePreferences] = new[] { UserRole.Administrator },
            [Permission.ConfigureRules] = new[] { UserRole.Supervisor, UserRole.Administrator },
            [Permission.ManageUsers] = new[] { UserRole.Administrator },
            [Permission.PurgeData] = new[] { UserRole.Administrator },
            [Permission.RecordResults] = new[] { UserRole.Technologist, UserRole.Supervisor, UserRole.Administrator },
        };

        public static string Describe(Permission p) => p switch
        {
            Permission.SupervisorReview => "record a supervisor review",
            Permission.ClearIdentification => "clear a confirmed identification",
            Permission.ReinstateInvestigation => "reinstate a cancelled investigation",
            Permission.ConfigurePreferences => "change Preferences",
            Permission.ConfigureRules => "add, edit, or delete analysis rules",
            Permission.ManageUsers => "manage users and roles",
            Permission.PurgeData => "purge specimens from the database",
            Permission.RecordResults => "record or change laboratory data",
            _ => p.ToString(),
        };

        /// <summary>Who holds the permission, as "a Supervisor or Administrator", for messages that send the user to them.</summary>
        public static string WhoCan(Permission permission)
        {
            var labels = Grants[permission].Select(UserRole.Label).ToList();
            var article = "AEIOU".Contains(labels[0][0]) ? "an" : "a";
            return $"{article} {string.Join(" or ", labels)}";
        }

        /// <summary>
        /// The next step a message names: <paramref name="step"/> when the current user may take it, otherwise
        /// "Ask a Supervisor or Administrator to " + <paramref name="askFor"/>, so no message sends a user to a
        /// command their role will refuse.
        /// </summary>
        public static string NextStep(DatabaseService db, Permission permission, string step, string askFor) =>
            Can(db, permission) ? step : $"Ask {WhoCan(permission)} to {askFor}";

        public static bool RegistryEnabled(DatabaseService db) => db.GetUsers().Any(u => u.Active);

        /// <summary>The active registered user for the current login, or null.</summary>
        public static AppUser? CurrentUser(DatabaseService db) =>
            db.GetUsers().FirstOrDefault(u => u.Active &&
                string.Equals(u.Login, CurrentLogin, StringComparison.OrdinalIgnoreCase));

        public static bool Can(DatabaseService db, Permission permission)
        {
            if (!RegistryEnabled(db)) return true;
            var user = CurrentUser(db);
            return user != null && Grants[permission].Contains(user.Role);
        }

        /// <summary>
        /// Why the current user may not use the permission (the same message <see cref="Demand"/> throws), or null.
        /// Screens check it before a dialog is filled in, so a refusal never comes only after the work is done.
        /// </summary>
        public static string? Refusal(DatabaseService db, Permission permission)
        {
            try { Demand(db, permission); return null; }
            catch (UnauthorizedAccessException ex) { return ex.Message; }
        }

        /// <summary>The start-up question when the database is nearly full: an offer to purge, or who can purge.</summary>
        public static string PurgeOffer(DatabaseService db) =>
            NextStep(db, Permission.PurgeData, "Would you like to purge old specimen data?",
                "purge old specimen data (Tools > Purge Database).");

        /// <summary>Throws <see cref="UnauthorizedAccessException"/> when the current user lacks the permission.</summary>
        public static void Demand(DatabaseService db, Permission permission)
        {
            if (Can(db, permission)) return;
            var user = CurrentUser(db);
            var who = user == null
                ? $"Windows login '{CurrentLogin}' is not an active HemoIQ user"
                : $"{user.DisplayName} ({UserRole.Label(user.Role)})";
            var allowed = string.Join(" or ", Grants[permission].Select(UserRole.Label));
            throw new UnauthorizedAccessException(
                $"{who} cannot {Describe(permission)}. This requires the {allowed} role.");
        }
    }
}
