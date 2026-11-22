using System.Windows;

namespace HemoIQ.ViewModels
{
    /// <summary>
    /// The confirmation for going ahead past a safety warning ("Save anyway?", "Record these reactions anyway?",
    /// "Replace it?"). The answer under the keyboard is No, so a stray Enter keeps the safe choice and the override
    /// that gets recorded is always a deliberate one, the same as the stale-analysis and open-testing prompts.
    /// </summary>
    public static class OverrideConfirm
    {
        public const MessageBoxResult DefaultAnswer = MessageBoxResult.No;

        public static bool Ask(string message, string title, Window? owner = null) =>
            (owner == null
                ? MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning, DefaultAnswer)
                : MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning, DefaultAnswer))
            == MessageBoxResult.Yes;
    }
}
