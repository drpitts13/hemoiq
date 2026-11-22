using System;
using System.Threading;

namespace HemoIQ.Services.Vendors
{
    /// <summary>
    /// One download or import at a time in the vendor dialog, and nothing stored after the dialog is closed.
    /// A catalog download takes seconds; without this, a second import could start while the first was still
    /// downloading, and closing the dialog mid-download would still store the lot when the download finished,
    /// an import the user walked away from.
    /// </summary>
    public sealed class VendorDialogGate : IDisposable
    {
        private readonly CancellationTokenSource _cts = new();

        public bool IsBusy { get; private set; }
        public bool IsClosed { get; private set; }

        /// <summary>Cancelled when the dialog closes, so a download in progress stops.</summary>
        public CancellationToken Token => _cts.Token;

        /// <summary>Starts a download or import; false while another is running or after the dialog closed.</summary>
        public bool TryBegin()
        {
            if (IsBusy || IsClosed) return false;
            IsBusy = true;
            return true;
        }

        public void End() => IsBusy = false;

        /// <summary>True while the finished work may still be stored and shown: the dialog has not been closed.</summary>
        public bool MayApply => !IsClosed && !_cts.IsCancellationRequested;

        public void Close()
        {
            if (IsClosed) return;
            IsClosed = true;
            _cts.Cancel();
        }

        public void Dispose()
        {
            Close();
            _cts.Dispose();
        }
    }
}
