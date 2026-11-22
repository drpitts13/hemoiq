using System;
using System.Windows.Input;
using HemoIQ.Services;

namespace HemoIQ.ViewModels
{
    /// <summary>
    /// A command reached from a keyboard shortcut (Ctrl+S, Ctrl+N) runs only when its button would be enabled.
    /// <see cref="RelayCommand.Execute"/> does not check <see cref="ICommand.CanExecute"/>, so without this a
    /// shortcut opened the Add dialogs, or saved grades, in a view-only session whose buttons were disabled,
    /// and the user was refused only after filling the form in.
    /// </summary>
    public static class ShortcutCommand
    {
        /// <summary>Runs the command and returns null, or returns the status explaining why it did not run.</summary>
        public static string? Run(ICommand command, string action, SessionInfo session)
        {
            if (command.CanExecute(null))
            {
                command.Execute(null);
                return null;
            }
            return session.ViewOnly
                ? $"{action} is not available: {session.Text}. Commands that change laboratory data are disabled."
                : $"{action} is not available right now.";
        }
    }
}
