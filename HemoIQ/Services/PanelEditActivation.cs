namespace HemoIQ.Services
{
    /// <summary>
    /// The Edit panel dialog carries an Active box. Switching an inactive lot on is not a detail edit: it must go
    /// through Activate, which shows the antigram review prompt, checks antigen coverage against the prior lot and
    /// writes the activate_panel audit entry. Switching a lot off, or leaving it as it was, saves with the edit.
    /// </summary>
    public static class PanelEditActivation
    {
        /// <summary>True when the edit asks to activate a lot that is stored inactive.</summary>
        public static bool NeedsActivationReview(bool storedActive, bool requestedActive) =>
            !storedActive && requestedActive;

        /// <summary>The active flag the detail edit itself may save: never turns an inactive lot on.</summary>
        public static bool ActiveToSave(bool storedActive, bool requestedActive) =>
            storedActive && requestedActive;
    }
}
