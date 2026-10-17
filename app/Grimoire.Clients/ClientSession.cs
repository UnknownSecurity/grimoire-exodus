using System;
using System.Threading;
using Grimoire.Botting;
using Grimoire.Game;
using Grimoire.Networking;
using Grimoire.Tools;

namespace Grimoire.Clients
{
    /// <summary>
    /// Owns the lifetime and Flash boundary for one client. More session-owned
    /// services are added behind this boundary as the legacy static APIs are
    /// migrated.
    /// </summary>
    public sealed class ClientSession : IDisposable
    {
        private readonly CancellationTokenSource lifetimeCancellation;
        private bool disposed;
        private int loadProgress;
        private int latestPingLatency = int.MinValue;
        private ClientSessionState state;

        public ClientSession(string displayName, IFlashBridge flashBridge, IBotEngine botEngine = null)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("A client display name is required.", nameof(displayName));
            if (flashBridge == null)
                throw new ArgumentNullException(nameof(flashBridge));

            Id = Guid.NewGuid();
            DisplayName = displayName;
            FlashBridge = flashBridge;
            BotEngine = botEngine;
            lifetimeCancellation = new CancellationTokenSource();
            GameState = new GameSessionState();
            OptionState = new ClientOptionState();
            BotDocument = new BotDocumentState();
            LoginState = new ClientLoginState(lifetimeCancellation.Token);
            PacketSpammer = new Spammer(this);
            state = ClientSessionState.Created;
            FlashBridge.SwfLoadProgress += FlashBridge_SwfLoadProgress;

            ActiveXFlashBridge activeXBridge = flashBridge as ActiveXFlashBridge;
            activeXBridge?.BindSession(this);
            Bot bot = botEngine as Bot;
            bot?.BindSession(this);
            if (BotEngine != null)
                BotEngine.ConfigurationChanged += BotEngine_ConfigurationChanged;
            using (ClientSessionContext.Push(this))
                GameState.Initialize();
        }

        public Guid Id { get; }

        public string DisplayName { get; }

        public IFlashBridge FlashBridge { get; }

        public IBotEngine BotEngine { get; }

        public GameSessionState GameState { get; }

        public ClientOptionState OptionState { get; }

        public BotDocumentState BotDocument { get; }

        public ClientLoginState LoginState { get; }

        public Spammer PacketSpammer { get; }

        public Proxy PacketDispatcher { get; private set; }

        public CancellationToken LifetimeToken => lifetimeCancellation.Token;

        public int LoadProgress => loadProgress;

        public int? LatestPingLatency
        {
            get
            {
                int value = Volatile.Read(ref latestPingLatency);
                return value == int.MinValue ? (int?)null : value;
            }
        }

        public ClientSessionState State => state;

        public bool IsReady => state == ClientSessionState.Ready || state == ClientSessionState.Running;

        public bool IsDisposed => disposed;

        public event EventHandler StateChanged;

        public event EventHandler LoadProgressChanged;

        private void BotEngine_ConfigurationChanged(Configuration configuration)
        {
            if (configuration != null)
                BotDocument.UpdateRuntimeConfiguration(configuration);
        }

        internal void AttachPacketDispatcher(Proxy packetDispatcher)
        {
            if (packetDispatcher == null)
                throw new ArgumentNullException(nameof(packetDispatcher));
            ThrowIfDisposed();
            if (PacketDispatcher != null && !ReferenceEquals(PacketDispatcher, packetDispatcher))
                throw new InvalidOperationException("A client session already owns a packet dispatcher.");
            PacketDispatcher = packetDispatcher;
        }

        internal void ReportPingLatency(int? latency)
        {
            Volatile.Write(ref latestPingLatency, latency ?? int.MinValue);
        }

        public void MarkInitializing()
        {
            SetState(ClientSessionState.Initializing);
        }

        public void MarkRunning()
        {
            ThrowIfDisposed();
            if (!IsReady)
                throw new InvalidOperationException("The client cannot run before its SWF is ready.");
            SetState(ClientSessionState.Running);
        }

        public void MarkFaulted()
        {
            SetState(ClientSessionState.Faulted);
        }

        private void FlashBridge_SwfLoadProgress(int progress)
        {
            int normalized = Math.Max(0, Math.Min(100, progress));
            if (loadProgress != normalized)
            {
                loadProgress = normalized;
                LoadProgressChanged?.Invoke(this, EventArgs.Empty);
            }

            if (normalized >= 100 && state != ClientSessionState.Running)
            {
                SetState(ClientSessionState.Ready);
            }
        }

        private void SetState(ClientSessionState value)
        {
            ThrowIfDisposed();
            if (state == value)
                return;

            state = value;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ClientSession));
        }

        public void Dispose()
        {
            if (disposed)
                return;

            if (state != ClientSessionState.Closing)
            {
                state = ClientSessionState.Closing;
                StateChanged?.Invoke(this, EventArgs.Empty);
            }

            disposed = true;
            if (!lifetimeCancellation.IsCancellationRequested)
                lifetimeCancellation.Cancel();

            FlashBridge.SwfLoadProgress -= FlashBridge_SwfLoadProgress;
            if (BotEngine != null)
                BotEngine.ConfigurationChanged -= BotEngine_ConfigurationChanged;
            LoginState.Dispose();
            PacketSpammer.Dispose();
            OptionState.Dispose();
            PacketDispatcher?.Dispose();
            PacketDispatcher = null;
            FlashBridge.Dispose();
            lifetimeCancellation.Dispose();
            state = ClientSessionState.Disposed;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
