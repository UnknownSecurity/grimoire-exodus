using Grimoire.Networking;
using Grimoire.Networking.Handlers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grimoire.Botting
{
    /// <summary>
    /// Mutable option values and worker ownership for one client session.
    /// </summary>
    public sealed class ClientOptionState : IDisposable
    {
        private bool disposed;

        public ClientOptionState()
        {
            ProvokePacket = "%xt%zm%aggroMon%1%MonMapID%";
            Timer = 250;
            WalkSpeed = 8;
            DesiredFramesPerSecond = 60;
            LastAppliedFramesPerSecond = -1;
            SyncRoot = new object();
            HandlerHideRoom = new HandlerMapJoin();
            HandlerAfk = new HandlerAFK();
        }

        internal object SyncRoot { get; }

        internal CancellationTokenSource SettingsCancellation { get; set; }

        internal Task SettingsTask { get; set; }

        internal CancellationTokenSource SaveStateCancellation { get; set; }

        internal Task SaveStateTask { get; set; }

        internal CancellationTokenSource AntiAfkCancellation { get; set; }

        internal Task AntiAfkTask { get; set; }

        internal IJsonMessageHandler HandlerHideRoom { get; }

        internal IXtMessageHandler HandlerAfk { get; }

        internal Action<bool> StateChanged;

        public bool IsRunning { get; set; }

        public bool DisableAnimations { get; set; }

        public bool LagKiller { get; set; }

        public bool HidePlayers { get; set; }

        public bool InfiniteRange { get; set; }

        public bool HideYulgar { get; set; }

        public bool HideRoom { get; set; }

        public bool Afk { get; set; }

        public bool SaveStateEnabled { get; set; }

        public bool AntiAfkEnabled { get; set; }

        public bool ProvokeMonsters { get; set; }

        public bool ProvokeAllMonsters { get; set; }

        public bool PacketSpam { get; set; }

        public string ProvokePacket { get; set; }

        public string SpamPacket { get; set; }

        public bool EnemyMagnet { get; set; }

        public bool SkipCutscenes { get; set; }

        public int WalkSpeed { get; set; }

        public bool UseCustomFrameRate { get; set; }

        public int DesiredFramesPerSecond { get; set; }

        internal int LastAppliedFramesPerSecond { get; set; }

        internal bool FrameRateManagedByPerformancePolicy { get; set; }

        public int Timer { get; set; }

        public int PacketDelay { get; set; }

        public int RoomNumber { get; set; }

        public bool Untarget { get; set; }

        public bool ChangeChat { get; set; }

        public bool WarningMessageFilter { get; set; }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            lock (SyncRoot)
            {
                SettingsCancellation?.Cancel(false);
                SaveStateCancellation?.Cancel(false);
                AntiAfkCancellation?.Cancel(false);
            }
        }
    }
}
