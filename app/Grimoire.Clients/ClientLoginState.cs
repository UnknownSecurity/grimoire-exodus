using System;
using System.Threading;

namespace Grimoire.Clients
{
    /// <summary>
    /// Owns immediate-login cancellation for one client. Starting a newer
    /// login operation cancels only the previous operation for that client.
    /// </summary>
    public sealed class ClientLoginState : IDisposable
    {
        private readonly object syncRoot = new object();
        private readonly CancellationToken sessionToken;
        private CancellationTokenSource currentOperation;
        private bool disposed;

        internal ClientLoginState(CancellationToken sessionToken)
        {
            this.sessionToken = sessionToken;
        }

        public bool IsBusy
        {
            get
            {
                lock (syncRoot)
                    return currentOperation != null;
            }
        }

        public event EventHandler BusyChanged;

        public CancellationTokenSource BeginOperation()
        {
            CancellationTokenSource previous;
            CancellationTokenSource next;
            lock (syncRoot)
            {
                ThrowIfDisposed();
                previous = currentOperation;
                next = CancellationTokenSource.CreateLinkedTokenSource(sessionToken);
                currentOperation = next;
            }

            previous?.Cancel(false);
            BusyChanged?.Invoke(this, EventArgs.Empty);
            return next;
        }

        public void CompleteOperation(CancellationTokenSource operation)
        {
            if (operation == null)
                return;

            bool changed = false;
            lock (syncRoot)
            {
                if (ReferenceEquals(currentOperation, operation))
                {
                    currentOperation = null;
                    changed = true;
                }
            }

            operation.Dispose();
            if (changed)
                BusyChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ClientLoginState));
        }

        public void Dispose()
        {
            CancellationTokenSource operation;
            lock (syncRoot)
            {
                if (disposed)
                    return;

                disposed = true;
                operation = currentOperation;
                currentOperation = null;
            }

            operation?.Cancel(false);
            operation?.Dispose();
            BusyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
