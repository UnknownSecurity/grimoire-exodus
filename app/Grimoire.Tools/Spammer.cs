using Grimoire.Clients;
using Grimoire.Networking;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Grimoire.Tools
{
    public class Spammer : IDisposable
    {
        private static readonly Spammer LegacyInstance = new Spammer();

        public List<string> _packets;

        private int _delay;

        private CancellationTokenSource _cancellationTokenSource;

        private Task _spamTask;

        private readonly ClientSession owningSession;

        private bool disposed;

        public Exception LastError { get; private set; }

        public bool IsRunning => _spamTask != null && !_spamTask.IsCompleted;

        public static Spammer Instance
        {
            get
            {
                return ClientSessionContext.Resolve()?.PacketSpammer ?? LegacyInstance;
            }
        }

        public event Action<int> IndexChanged;

        public Spammer()
        {
        }

        internal Spammer(ClientSession session)
        {
            owningSession = session ?? throw new ArgumentNullException(nameof(session));
        }

        public void Start(List<string> packets, int delay)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(Spammer));

            Stop();
            LastError = null;
            _packets = packets == null ? new List<string>() : new List<string>(packets);
            _delay = delay;
            CancellationTokenSource operation = owningSession == null
                ? new CancellationTokenSource()
                : CancellationTokenSource.CreateLinkedTokenSource(owningSession.LifetimeToken);
            _cancellationTokenSource = operation;
            CancellationToken token = operation.Token;
            List<string> packetSnapshot = _packets;
            int delaySnapshot = Math.Max(1, _delay);
            ClientSession capturedSession = owningSession ?? ClientSessionContext.Resolve();
            _spamTask = Task.Run(
                () => RunInSessionAsync(capturedSession, packetSnapshot, delaySnapshot, token),
                token);
            _spamTask.ContinueWith(
                task =>
                {
                    if (ReferenceEquals(_cancellationTokenSource, operation))
                        _cancellationTokenSource = null;
                    operation.Dispose();
                    if (task.IsFaulted && ReferenceEquals(_spamTask, task))
                    {
                        LastError = task.Exception?.GetBaseException();
                        System.Diagnostics.Debug.WriteLine("Packet spammer failed: " + task.Exception?.GetBaseException());
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel(throwOnFirstException: false);
        }

        private async Task RunInSessionAsync(
            ClientSession session,
            IReadOnlyList<string> packets,
            int delay,
            CancellationToken token)
        {
            await Spam(session, packets, delay, token);
        }

        private async Task Spam(
            ClientSession session,
            IReadOnlyList<string> packets,
            int delay,
            CancellationToken token)
        {
            if (packets == null || packets.Count == 0)
            {
                return;
            }

            int index = 0;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (index >= packets.Count)
                    {
                        index = 0;
                    }
                    this.IndexChanged?.Invoke(index);
                    if (session == null)
                    {
                        await Proxy.Instance.SendToServer(packets[index++]);
                    }
                    else
                    {
                        using (ClientSessionContext.Push(session))
                            await session.PacketDispatcher.SendToServer(packets[index++]);
                    }
                    await Task.Delay(delay, token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            Stop();
        }
    }
}
