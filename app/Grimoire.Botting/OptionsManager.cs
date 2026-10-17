using Grimoire.Game;
using Grimoire.Game.Data;
using Grimoire.Clients;
using Grimoire.Networking;
using Grimoire.Networking.Handlers;
using Grimoire.Tools;
using Grimoire.UI;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Grimoire.Botting
{
    public class OptionsManager
    {
        private static readonly ClientOptionState LegacyState = new ClientOptionState();

        private static ClientOptionState State
        {
            get { return ClientSessionContext.Resolve()?.OptionState ?? LegacyState; }
        }

        public static bool IsRunning
        {
            get
            {
                return State.IsRunning;
            }
            private set
            {
                SetIsRunning(State, value);
            }
        }

        private static void SetIsRunning(ClientOptionState state, bool value)
        {
            state.IsRunning = value;
            state.StateChanged?.Invoke(value);
        }

        private static CancellationTokenSource CreateSessionCancellation()
        {
            ClientSession session = ClientSessionContext.Resolve();
            return session == null
                ? new CancellationTokenSource()
                : CancellationTokenSource.CreateLinkedTokenSource(session.LifetimeToken);
        }

        public static bool ProvokeMonsters
        {
            get { return State.ProvokeMonsters; }
            set { State.ProvokeMonsters = value; }
        }

        public static bool ProvokeAllMonster
        {
            get { return State.ProvokeAllMonsters; }
            set { State.ProvokeAllMonsters = value; }
        }

        public static bool PacketSpam
        {
            get { return State.PacketSpam; }
            set { State.PacketSpam = value; }
        }

        public static string ProvokePacket
        {
            get { return State.ProvokePacket; }
            set { State.ProvokePacket = value; }
        }

        public static string SpamPacket
        {
            get { return State.SpamPacket; }
            set { State.SpamPacket = value; }
        }

        public static bool EnemyMagnet
        {
            get { return State.EnemyMagnet; }
            set { State.EnemyMagnet = value; }
        }

        public static bool LagKiller
        {
            get => State.LagKiller;
            set
            {
                State.LagKiller = value;
                SetLagKiller();
            }
        }

        public static bool SkipCutscenes
        {
            get { return State.SkipCutscenes; }
            set { State.SkipCutscenes = value; }
        }

        public static bool DisableAnimations
        {
            get => State.DisableAnimations;
            set
            {
                State.DisableAnimations = value;
                RunDisableAnimations(value);
            }
        }

        public static bool HidePlayers
        {
            get => State.HidePlayers;
            set
            {
                State.HidePlayers = value;
                DestroyPlayers(value);
            }
        }

        public static bool InfiniteRange
        {
            get => State.InfiniteRange;
            set
            {
                State.InfiniteRange = value;
                SetInfiniteRange(value);
            }
        }

        public static int WalkSpeed
        {
            get { return State.WalkSpeed; }
            set { State.WalkSpeed = value; }
        }

        public static int Timer
        {
            get { return State.Timer; }
            set { State.Timer = value; }
        }

        public static int PacketDelay
        {
            get { return State.PacketDelay; }
            set { State.PacketDelay = value; }
        }

        public static int RoomNumber
        {
            get { return State.RoomNumber; }
            set { State.RoomNumber = value; }
        }

        public static bool Untarget
        {
            get { return State.Untarget; }
            set { State.Untarget = value; }
        }

        public static bool AFK
        {
            get => State.Afk;
            set
            {
                ClientOptionState state = State;
                state.Afk = value;
                if (value)
                    Proxy.Instance.RegisterHandler(state.HandlerAfk);
                else
                    Proxy.Instance.UnregisterHandler(state.HandlerAfk);
            }
        }

        public static bool HideRoom
        {
            get => State.HideRoom;
            set
            {
                ClientOptionState state = State;
                state.HideRoom = value;
                if (value)
                    Proxy.Instance.RegisterHandler(state.HandlerHideRoom);
                else
                    Proxy.Instance.UnregisterHandler(state.HandlerHideRoom);
            }
        }

        public static bool ChangeChat
        {
            get { return State.ChangeChat; }
            set { State.ChangeChat = value; }
        }

        public static bool WarningMsgFilter
        {
            get { return State.WarningMessageFilter; }
            set { State.WarningMessageFilter = value; }
        }

        public static bool _saveState
        {
            get => State.SaveStateEnabled;
            set
            {
                ClientOptionState state = State;
                lock (state.SyncRoot)
                {
                    state.SaveStateEnabled = value;
                    if (!value)
                    {
                        state.SaveStateCancellation?.Cancel(false);
                        return;
                    }

                    if (state.SaveStateTask == null || state.SaveStateTask.IsCompleted)
                    {
                        state.SaveStateCancellation?.Dispose();
                        state.SaveStateCancellation = CreateSessionCancellation();
                        state.SaveStateTask = RunSaveState(state, state.SaveStateCancellation.Token);
                        ObserveBackgroundTask(state.SaveStateTask, "Save-state loop");
                    }
                }
            }
        }

        public static bool _antiAfk
        {
            get => State.AntiAfkEnabled;
            set
            {
                ClientOptionState state = State;
                lock (state.SyncRoot)
                {
                    state.AntiAfkEnabled = value;
                    if (!value)
                    {
                        state.AntiAfkCancellation?.Cancel(false);
                    }
                }
            }
        }

        public static bool HideYulgar
        {
            get => State.HideYulgar;
            set
            {
                State.HideYulgar = value;
                if (value)
                {
                    if (Player.IsLoggedIn && Player.Map.ToLower() == "yulgar")
                        DestroyPlayers(true);
                }
            }
        }

        private static readonly string[] empty = new string[0];

        public static event Action<bool> StateChanged
        {
            add { State.StateChanged += value; }
            remove { State.StateChanged -= value; }
        }

        public static void SetInfiniteRange(bool Toggle) => Flash.Call("SetInfiniteRange", Toggle);

        public static void SetProvokeMonsters() => Flash.Call("SetProvokeMonsters", empty);

        public static void SetEnemyMagnet() => Flash.Call("SetEnemyMagnet", empty);

        public static void SetLagKiller() => Flash.Call("SetLagKiller", LagKiller ? false : true);

        public static void DestroyPlayers(bool Enabled) => Flash.Call("DestroyPlayers", Enabled);

        public static void SetSkipCutscenes() => Flash.Call("SetSkipCutscenes", (BotData.BotCell == null) ? "Enter" : BotData.BotCell, (BotData.BotPad == null) ? "Spawn" : BotData.BotPad);

        public static void SetWalkSpeed() => Flash.Call("SetWalkSpeed", WalkSpeed);

        public static void DisableDeathAds() => Flash.Call("DisableDeathAd", empty);

        public static void RunDisableAnimations(bool Enabled) => Flash.Call("DisableAnimations", Enabled);

        public static void Start()
        {
            Start(Bot.Instance.Configuration);
        }

        internal static void Start(Configuration configuration)
        {
            if (configuration == null)
            {
                ObserveBackgroundTask(ApplySettings(), "Settings loop");
                return;
            }

            if (configuration.LagKiller)
            {
                LagKiller = true;
            }
            if (configuration.ProvokeMonsters)
            {
                ProvokeMonsters = true;
            }
            if (configuration.ProvokeAllMonster)
            {
                ProvokeAllMonster = true;
            }
            if (configuration.AutoSaveState)
            {
                _saveState = true;
            }
            ObserveBackgroundTask(ApplySettings(), "Settings loop");
        }

        public static void Stop()
        {
            Stop(Bot.Instance.Configuration);
        }

        internal static void Stop(Configuration configuration)
        {
            ClientOptionState state = State;
            lock (state.SyncRoot)
            {
                SetIsRunning(state, false);
                state.SettingsCancellation?.Cancel(false);
            }
            if (state.LagKiller || (configuration != null && configuration.LagKiller))
            {
                LagKiller = false;
            }
            bool shouldMoveOutOfCombat = state.ProvokeMonsters
                || state.ProvokeAllMonsters
                || (configuration != null && (configuration.ProvokeMonsters || configuration.ProvokeAllMonster));
            if (state.ProvokeMonsters)
            {
                ProvokeMonsters = false;
            }
            if (state.ProvokeAllMonsters)
            {
                ProvokeAllMonster = false;
            }
            if (shouldMoveOutOfCombat)
            {
                BotUtilities.MoveToSelfCell();
            }
            if (state.SaveStateEnabled || (configuration != null && configuration.AutoSaveState))
            {
                _saveState = false;
            }
        }

        public static void SetClientLevel()
        {
            Proxy.Instance.SendToClient("{\"t\":\"xt\",\"b\":{\"r\":-1,\"o\":{\"cmd\":\"levelUp\",\"intExpToLevel\":\"0\",\"intLevel\":" + Player.levelCap + "}}}");
        }

        public static void SetProvokeAllMonster()
        {
            if (!ProvokePacket.Contains("MonMapID"))
            {
                _ = Proxy.Instance.SendToServer($"{ProvokePacket}");
            }
            else
            {
                _ = Proxy.Instance.SendToServer($"%xt%zm%aggroMon%{World.RoomId}%{string.Join("%", World.Monsters.ConvertAll<int>((Monster m) => m.MonMapID))}%");
            }
        }

        public static async Task SetPacketSpam()
        {
            await SetPacketSpam(CancellationToken.None);
        }

        private static async Task SetPacketSpam(CancellationToken token)
        {
            await Proxy.Instance.SendToServer($"{SpamPacket}");
            await Task.Delay(Math.Max(1, PacketDelay), token);
        }

        public static Task SaveState()
        {
            ClientOptionState state = State;
            lock (state.SyncRoot)
            {
                if (!state.SaveStateEnabled)
                {
                    return Task.CompletedTask;
                }

                if (state.SaveStateTask == null || state.SaveStateTask.IsCompleted)
                {
                    state.SaveStateCancellation?.Dispose();
                    state.SaveStateCancellation = CreateSessionCancellation();
                    state.SaveStateTask = RunSaveState(state, state.SaveStateCancellation.Token);
                    ObserveBackgroundTask(state.SaveStateTask, "Save-state loop");
                }
                return state.SaveStateTask;
            }
        }

        private static async Task RunSaveState(ClientOptionState state, CancellationToken token)
        {
            try
            {
                while (Player.IsLoggedIn && state.SaveStateEnabled && !token.IsCancellationRequested)
                {
                    await Proxy.Instance.SendToServer($"%xt%zm%whisper%1%Save State: Ensures any gold, experience, reputation, and class points gained are saved. Triggers after every 5 minutes passed.%{Player.Username}%");
                    LogForm.Instance.AppendDebug($"[{DateTime.Now:hh:mm:ss tt}] Save state has been triggered.");
                    await Task.Delay(300000, token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
        }

        public static void AntiAfk()
        {
            ClientOptionState state = State;
            lock (state.SyncRoot)
            {
                if (!state.AntiAfkEnabled || (state.AntiAfkTask != null && !state.AntiAfkTask.IsCompleted))
                {
                    return;
                }

                state.AntiAfkCancellation?.Dispose();
                state.AntiAfkCancellation = CreateSessionCancellation();
                state.AntiAfkTask = RunAntiAfk(state, state.AntiAfkCancellation.Token);
                ObserveBackgroundTask(state.AntiAfkTask, "Anti-AFK loop");
            }
        }

        private static async Task RunAntiAfk(ClientOptionState state, CancellationToken token)
        {
            try
            {
                while (Player.IsLoggedIn && state.AntiAfkEnabled && !token.IsCancellationRequested)
                {
                    Player.cancelAfk();
                    await Task.Delay(60000, token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
        }

        private static void ObserveBackgroundTask(Task task, string operation)
        {
            if (task == null)
                return;

            _ = task.ContinueWith(
                failed => System.Diagnostics.Debug.WriteLine(
                    operation + " failed: " + failed.Exception?.GetBaseException()),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }
        
        private static void Provoke()
        {
            if (ProvokeMonsters)
            {
                World.SendPacket($"%xt%zm%aggroMon%{World.RoomId}%{string.Join("%", World.MonstersInCell(Player.Cell)?.ConvertAll<int>(m => m.MonMapID))}%");
            }
            if (ProvokeAllMonster)
            {
                World.SendPacket(!ProvokePacket.Contains("MonMapID") ? $"{ProvokePacket}" : $"%xt%zm%aggroMon%{World.RoomId}%{string.Join("%", World.Monsters?.ConvertAll<int>(m => m.MonMapID))}%");
            }
        }



        public static Task ApplySettings()
        {
            ClientOptionState state = State;
            lock (state.SyncRoot)
            {
                CancellationTokenSource previous = state.SettingsCancellation;
                Task previousTask = state.SettingsTask;
                previous?.Cancel(false);

                state.SettingsCancellation = CreateSessionCancellation();
                SetIsRunning(state, true);
                state.SettingsTask = RunSettings(state, state.SettingsCancellation);
                if (previous != null)
                {
                    if (previousTask == null)
                    {
                        previous.Dispose();
                    }
                    else
                    {
                        _ = previousTask.ContinueWith(
                            _ => previous.Dispose(),
                            CancellationToken.None,
                            TaskContinuationOptions.ExecuteSynchronously,
                            TaskScheduler.Default);
                    }
                }
                return state.SettingsTask;
            }
        }

        private static async Task RunSettings(ClientOptionState state, CancellationTokenSource owner)
        {
            CancellationToken token = owner.Token;
            try
            {
                while (Player.IsLoggedIn && !token.IsCancellationRequested)
                {
                    if (PacketSpam && Player.IsAlive)
                        await SetPacketSpam(token);
                    if (EnemyMagnet && Player.IsAlive)
                        SetEnemyMagnet();
                    if (Untarget && Player.targetOnSelf)
                        Player.CancelTargetSelf();
                    if (SkipCutscenes && World.ActiveCutscene > 0)
                        SetSkipCutscenes();
                    if (Player.walkSpeed != WalkSpeed)
                        SetWalkSpeed();
                    Provoke();
                    await Task.Delay(Math.Max(1, Timer), token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            finally
            {
                lock (state.SyncRoot)
                {
                    if (ReferenceEquals(state.SettingsCancellation, owner))
                    {
                        state.SettingsCancellation = null;
                        SetIsRunning(state, false);
                        owner.Dispose();
                    }
                }
            }
        }
    }
}
