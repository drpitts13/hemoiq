using System;

namespace HemoIQ.Models
{
    public class RecordLockedException : InvalidOperationException
    {
        public string AccessionNumber { get; }

        public bool Cancelled { get; }

        /// <param name="askWho">
        /// Null when the current user may unlock the record; otherwise who to ask (for example "a Supervisor or
        /// Administrator"), so the message does not send the user to a command their role refuses.
        /// </param>
        public RecordLockedException(string accessionNumber, bool cancelled = false, string? askWho = null)
            : base(Describe(accessionNumber, cancelled, askWho))
        {
            AccessionNumber = accessionNumber;
            Cancelled = cancelled;
        }

        public static string Describe(string accessionNumber, bool cancelled, string? askWho) => (cancelled, askWho) switch
        {
            (true, null) => $"Investigation {accessionNumber} is cancelled and locked. Reinstate it with a reason before editing reactions, panels, analysis, or the identification.",
            (true, _) => $"Investigation {accessionNumber} is cancelled and locked. Ask {askWho} to reinstate it with a reason before reactions, panels, analysis, or the identification can be edited.",
            (false, null) => $"Specimen {accessionNumber} has a confirmed identification and is locked. " +
                             "Clear the confirmation with a reason before editing reactions, panels, or analysis.",
            _ => $"Specimen {accessionNumber} has a confirmed identification and is locked. " +
                 $"Ask {askWho} to clear the confirmation with a reason before reactions, panels, or analysis can be edited.",
        };

        /// <summary>A locked record with a message specific to the refused action.</summary>
        public RecordLockedException(string accessionNumber, string message) : base(message)
        {
            AccessionNumber = accessionNumber;
        }
    }
}
