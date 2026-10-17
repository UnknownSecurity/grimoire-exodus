using AxShockwaveFlashObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace Grimoire.Clients
{
    public sealed class ClientSessionEventArgs : EventArgs
    {
        public ClientSessionEventArgs(ClientSession session)
        {
            Session = session;
        }

        public ClientSession Session { get; }
    }

    public sealed class ActiveClientSessionChangedEventArgs : EventArgs
    {
        public ActiveClientSessionChangedEventArgs(ClientSession previous, ClientSession current)
        {
            Previous = previous;
            Current = current;
        }

        public ClientSession Previous { get; }

        public ClientSession Current { get; }
    }

    /// <summary>
    /// Owns client collection changes on the creating UI thread. The shell
    /// supplies the measured production limit and keeps one session selected
    /// for legacy active-client fallbacks.
    /// </summary>
    public sealed class ClientSessionManager : IDisposable
    {
        private readonly List<ClientSession> sessions = new List<ClientSession>();
        private readonly int ownerThreadId;
        private int maximumSessions;
        private ClientSession activeSession;
        private bool disposed;

        public ClientSessionManager(int maximumSessions, bool makeDefault = false)
        {
            if (maximumSessions < 1)
                throw new ArgumentOutOfRangeException(nameof(maximumSessions));

            this.maximumSessions = maximumSessions;
            ownerThreadId = Thread.CurrentThread.ManagedThreadId;
            if (makeDefault)
                Default = this;
        }

        public static ClientSessionManager Default { get; private set; }

        public int MaximumSessions => maximumSessions;

        public ClientSession ActiveSession => activeSession;

        public IReadOnlyList<ClientSession> Sessions => new ReadOnlyCollection<ClientSession>(sessions.ToArray());

        public event EventHandler<ClientSessionEventArgs> SessionAdded;

        public event EventHandler<ClientSessionEventArgs> SessionRemoving;

        public event EventHandler<ClientSessionEventArgs> SessionRemoved;

        public event EventHandler<ActiveClientSessionChangedEventArgs> ActiveSessionChanged;

        public void Add(ClientSession session)
        {
            VerifyAccess();
            ThrowIfDisposed();
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (session.IsDisposed)
                throw new ObjectDisposedException(nameof(session));
            if (sessions.Contains(session))
                return;
            if (sessions.Count >= maximumSessions)
                throw new InvalidOperationException("The current client limit has been reached.");

            sessions.Add(session);
            SessionAdded?.Invoke(this, new ClientSessionEventArgs(session));
            if (activeSession == null)
                SetActive(session);
        }

        public void SetMaximumSessions(int value)
        {
            VerifyAccess();
            ThrowIfDisposed();
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value));

            // Reducing the limit never closes a live client. It only prevents
            // another client from being added until the count is below the cap.
            maximumSessions = value;
        }

        public void SetActive(ClientSession session)
        {
            VerifyAccess();
            ThrowIfDisposed();
            if (session != null && !sessions.Contains(session))
                throw new InvalidOperationException("The active client must belong to this manager.");
            if (ReferenceEquals(activeSession, session))
                return;

            ClientSession previous = activeSession;
            activeSession = session;
            ActiveSessionChanged?.Invoke(this, new ActiveClientSessionChangedEventArgs(previous, session));
        }

        public bool Remove(ClientSession session)
        {
            VerifyAccess();
            ThrowIfDisposed();
            if (session == null || !sessions.Contains(session))
                return false;

            SessionRemoving?.Invoke(this, new ClientSessionEventArgs(session));
            bool wasActive = ReferenceEquals(activeSession, session);
            sessions.Remove(session);
            if (wasActive)
                SetActive(sessions.FirstOrDefault());
            session.Dispose();
            SessionRemoved?.Invoke(this, new ClientSessionEventArgs(session));
            return true;
        }

        public ClientSession FindByControl(AxShockwaveFlash control)
        {
            if (control == null)
                return null;
            return sessions.FirstOrDefault(session => ReferenceEquals(session.FlashBridge.Control, control));
        }

        private void VerifyAccess()
        {
            if (Thread.CurrentThread.ManagedThreadId != ownerThreadId)
                throw new InvalidOperationException("Client collection changes must run on the owning UI thread.");
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ClientSessionManager));
        }

        public void Dispose()
        {
            if (disposed)
                return;

            VerifyAccess();
            foreach (ClientSession session in sessions.ToArray())
                Remove(session);
            disposed = true;
            if (ReferenceEquals(Default, this))
                Default = null;
        }
    }
}
