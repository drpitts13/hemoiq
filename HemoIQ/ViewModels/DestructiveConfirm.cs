using System.Windows;

namespace HemoIQ.ViewModels
{
    /// <summary>
    /// The confirmation for any action that deletes data. The answer under the keyboard is No, so a stray Enter
    /// (or two, after a dialog's own Enter) never deletes records; Yes needs a deliberate choice.
    /// </summary>
    public static class DestructiveConfirm
    {
        public const MessageBoxResult DefaultAnswer = MessageBoxResult.No;

        public static bool Ask(string message, string title, Window? owner = null) =>
            (owner == null
                ? MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning, DefaultAnswer)
                : MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning, DefaultAnswer))
            == MessageBoxResult.Yes;
    }
}
