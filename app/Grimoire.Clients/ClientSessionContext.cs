using System;
using System.Threading;

namespace Grimoire.Clients
{
    /// <summary>
    /// Carries the owning client across awaits while legacy static APIs are
    /// migrated to explicit session services.
    /// </summary>
    public static class ClientSessionContext
    {
        private static readonly AsyncLocal<ClientSession> AmbientSession = new AsyncLocal<ClientSession>();

        public static ClientSession Current => AmbientSession.Value;

        public static ClientSession Resolve()
        {
            return Current ?? ClientSessionManager.Default?.ActiveSession;
        }

        public static IDisposable Push(ClientSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (session.IsDisposed)
                throw new ObjectDisposedException(nameof(session));

            ClientSession previous = AmbientSession.Value;
            AmbientSession.Value = session;
            return new SessionScope(session, previous);
        }

        private sealed class SessionScope : IDisposable
        {
            private readonly ClientSession expected;
            private readonly ClientSession previous;
            private bool disposed;

            public SessionScope(ClientSession expected, ClientSession previous)
            {
                this.expected = expected;
                this.previous = previous;
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;
                if (ReferenceEquals(AmbientSession.Value, expected))
                    AmbientSession.Value = previous;
            }
        }
    }
}
