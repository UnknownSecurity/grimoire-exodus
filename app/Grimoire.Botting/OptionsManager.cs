using Grimoire.Game;
using Grimoire.Game.Data;
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
        private static bool _isRunning;

        private static bool _disableAnimation;

        private static bool _lagKiller;

        private static bool _hidePlayers;

        private static bool _infRange;

        private static bool _hideYulgar;

        private static bool _hideRoom;

        private static bool _afk;

        private static bool _afk2;

        private static bool saveState;

        private static CancellationTokenSource _settingsCts;

        private static Task _settingsTask;

        private static readonly object SettingsSync = new object();

        private static Task _saveStateTask;

        private static CancellationTokenSource _saveStateCts;

        private static Task _antiAfkTask;

        private static CancellationTokenSource _antiAfkCts;

        private static bool antiAfkEnabled;

        public static bool IsRunning
        {
            get
            {
                return _isRunning;
            }
            private set
            {
                _isRunning = value;
                StateChanged?.Invoke(value);
            }
        }

        public static bool ProvokeMonsters
        {
            get;
            set;
        }

        public static bool ProvokeAllMonster
        {
            get;
            set;
        }

        public static bool PacketSpam
        {
            get;
            set;
        }

        public static string ProvokePacket
        {
            get;
            set;
        } = "%xt%zm%aggroMon%1%MonMapID%";

        public static string SpamPacket
        {
            get;
            set;
        }

        public static bool EnemyMagnet
        {
            get;
            set;
        }

        public static bool LagKiller
        {
            get => _lagKiller;
            set
            {
                _lagKiller = value;
                SetLagKiller();
            }
        }

        public static bool SkipCutscenes
        {
            get;
            set;
        }

        public static bool DisableAnimations
        {
            get => _disableAnimation;
            set
            {
                _disableAnimation = value;
                RunDisableAnimations(value);
            }
        }

        public static bool HidePlayers
        {
            get => _hidePlayers;
            set
            {
                _hidePlayers = value;
                DestroyPlayers(value);
            }
        }

        public static bool InfiniteRange
        {
            get => _infRange;
            set
            {
                _infRange = value;
                SetInfiniteRange(value);
            }
        }

        public static int WalkSpeed
        {
            get;
            set;
        }

        public static int Timer
        {
            get;
            set;
        } = 250;

        public static int PacketDelay
        {
            get;
            set;
        }

        public static int RoomNumber
        {
            get;
            set;
        }

        public static bool Untarget
        {
            get;
            set;
        }

        public static bool AFK
        {
            get => _afk;
            set
            {
                _afk = value;
                if (value)
                    Proxy.Instance.RegisterHandler(HandlerAFK1);
                else
                    Proxy.Instance.UnregisterHandler(HandlerAFK1);
            }
        }

        public static bool HideRoom
        {
            get => _hideRoom;
            set
            {
                _hideRoom = value;
                if (value)
                    Proxy.Instance.RegisterHandler(HandlerHideRoom);
                else
                    Proxy.Instance.UnregisterHandler(HandlerHideRoom);
            }
        }

        public static bool ChangeChat
        {
            get;
            set;
        }

        public static bool WarningMsgFilter
        {
            get;
            set;
        }

        public static bool _saveState
        {
            get => saveState;
            set
            {
                lock (SettingsSync)
                {
                    saveState = value;
                    if (!value)
                    {
                        _saveStateCts?.Cancel(false);
                        return;
                    }

                    if (_saveStateTask == null || _saveStateTask.IsCompleted)
                    {
                        _saveStateCts?.Dispose();
                        _saveStateCts = new CancellationTokenSource();
                        _saveStateTask = RunSaveState(_saveStateCts.Token);
                        ObserveBackgroundTask(_saveStateTask, "Save-state loop");
                    }
                }
            }
        }

        public static bool _antiAfk
        {
            get => antiAfkEnabled;
            set
            {
                lock (SettingsSync)
                {
                    antiAfkEnabled = value;
                    if (!value)
                    {
                        _antiAfkCts?.Cancel(false);
                    }
                }
            }
        }

        public static bool HideYulgar
        {
            get => _hideYulgar;
            set
            {
                _hideYulgar = value;
                if (value)
                {
                    if (Player.IsLoggedIn && Player.Map.ToLower() == "yulgar")
                        DestroyPlayers(true);
                }
            }
        }

        private static readonly string[] empty = new string[0];

        public static event Action<bool> StateChanged;

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
            if (BotManager.Instance.chkLag.Checked)
            {
                LagKiller = true;
            }
            if (BotManager.Instance.chkProvoke.Checked)
            {
                ProvokeMonsters = true;
            }
            if (BotManager.Instance.chkProvokeAllMon.Checked)
            {
                ProvokeAllMonster = true;
            }
            if (BotManager.Instance.chkSaveState.Checked)
            {
                _saveState = true;
            }
            ObserveBackgroundTask(ApplySettings(), "Settings loop");
        }

        public static void Stop()
        {
            lock (SettingsSync)
            {
                IsRunning = false;
                _settingsCts?.Cancel(false);
            }
            if (BotManager.Instance.chkLag.Checked)
            {
                LagKiller = false;
            }
            if (BotManager.Instance.chkProvoke.Checked)
            {
                ProvokeMonsters = false;
            }
            if (BotManager.Instance.chkProvokeAllMon.Checked)
            {
                ProvokeAllMonster = false;
            }
            Configuration config = BotManager.Instance.ActiveBotEngine.Configuration;
            if (BotManager.Instance.chkProvoke.Checked || BotManager.Instance.chkProvokeAllMon.Checked || (config != null && (config.ProvokeMonsters || config.ProvokeAllMonster)) || ProvokeMonsters || ProvokeAllMonster)
            {
                BotUtilities.MoveToSelfCell();
            }
            if (BotManager.Instance.chkSaveState.Checked)
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
            lock (SettingsSync)
            {
                if (!saveState)
                {
                    return Task.CompletedTask;
                }

                if (_saveStateTask == null || _saveStateTask.IsCompleted)
                {
                    _saveStateCts?.Dispose();
                    _saveStateCts = new CancellationTokenSource();
                    _saveStateTask = RunSaveState(_saveStateCts.Token);
                    ObserveBackgroundTask(_saveStateTask, "Save-state loop");
                }
                return _saveStateTask;
            }
        }

        private static async Task RunSaveState(CancellationToken token)
        {
            try
            {
                while (Player.IsLoggedIn && saveState && !token.IsCancellationRequested)
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
            lock (SettingsSync)
            {
                if (!antiAfkEnabled || (_antiAfkTask != null && !_antiAfkTask.IsCompleted))
                {
                    return;
                }

                _antiAfkCts?.Dispose();
                _antiAfkCts = new CancellationTokenSource();
                _antiAfkTask = RunAntiAfk(_antiAfkCts.Token);
                ObserveBackgroundTask(_antiAfkTask, "Anti-AFK loop");
            }
        }

        private static async Task RunAntiAfk(CancellationToken token)
        {
            try
            {
                while (Player.IsLoggedIn && antiAfkEnabled && !token.IsCancellationRequested)
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
            lock (SettingsSync)
            {
                CancellationTokenSource previous = _settingsCts;
                Task previousTask = _settingsTask;
                previous?.Cancel(false);

                _settingsCts = new CancellationTokenSource();
                IsRunning = true;
                _settingsTask = RunSettings(_settingsCts);
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
                return _settingsTask;
            }
        }

        private static async Task RunSettings(CancellationTokenSource owner)
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
                lock (SettingsSync)
                {
                    if (ReferenceEquals(_settingsCts, owner))
                    {
                        _settingsCts = null;
                        IsRunning = false;
                        owner.Dispose();
                    }
                }
            }
        }

        private static IJsonMessageHandler HandlerHideRoom
        {
            get;
        }

        private static IXtMessageHandler HandlerAFK1
        {
            get;
        }

        static OptionsManager()
        {
            HandlerHideRoom = new HandlerMapJoin();
            HandlerAFK1 = new HandlerAFK();
            WalkSpeed = 8;
        }
    }
}
