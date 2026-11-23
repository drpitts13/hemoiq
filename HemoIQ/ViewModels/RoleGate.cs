using System.Windows;
using HemoIQ.Data;
using HemoIQ.Services;

namespace HemoIQ.ViewModels
{
    /// <summary>
    /// Checks a role before a reason or initials dialog opens (Clear identification, Supervisor review, Reinstate),
    /// so a role that may not take the step is told why straight away instead of after typing the reason. The
    /// database still checks every write.
    /// </summary>
    public static class RoleGate
    {
        /// <summary>Shows the refusal and returns false when the current user may not use the permission; otherwise true.</summary>
        public static bool AllowedOrExplained(DatabaseService db, Permission permission, string title)
        {
            if (AccessControl.Refusal(db, permission) is not string refusal) return true;
            MessageBox.Show(refusal, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }
}
