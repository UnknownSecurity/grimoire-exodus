using Grimoire.Game;
using Grimoire.Game.Data;
using Grimoire.Clients;
using Grimoire.Tools;
using Grimoire.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Grimoire.Botting
{
    public class Bot : IBotEngine
    {
        public bool IsVar(string value)
        {
            return Regex.IsMatch(value, @"\[(?!Class Item)([^\)]*)\]");
        }

        public string GetVar(string value)
        {
            return Regex.Replace(value, @"[\[\]']+", "");
        }

        public string Value(string var)
        {
            if (!IsVar(var))
                return var;

            string key = GetVar(var);
            return Configuration.Tempvariable.TryGetValue(key, out string value) ? value : var;
        }

        private static readonly Bot LegacyInstance = new Bot();

        public static Bot Instance
        {
            get
            {
                return ClientSessionContext.Resolve()?.BotEngine as Bot ?? LegacyInstance;
            }
        }

        private IBotCommand cmd;

        private int _index;

        private Configuration _config;

        private bool _isRunning;

        private int _resumeIndex;

        private Task _botTask;

        private Task _questTask;

        private Task _dropTask;

        private readonly SemaphoreSlim _runTransition = new SemaphoreSlim(1, 1);

        private BotRunContext _currentRun;

        private ClientSession owningSession;

        private CancellationTokenRegistration owningSessionCancellation;

        public CancellationTokenSource _ctsBot;

        internal void BindSession(ClientSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (owningSession != null
                && !ReferenceEquals(owningSession, session)
                && !owningSession.IsDisposed)
                throw new InvalidOperationException("A bot engine cannot be moved to another client session.");
            owningSessionCancellation.Dispose();
            owningSession = session;
            owningSessionCancellation = session.LifetimeToken.Register(CancelCurrentRun);
        }

        private sealed class BotRunContext : IDisposable
        {
            public BotRunContext(Configuration configuration)
            {
                Configuration = configuration;
                Cancellation = new CancellationTokenSource();
            }

            public Configuration Configuration { get; }

            public CancellationTokenSource Cancellation { get; }

            public Task BotTask { get; set; }

            public Task QuestTask { get; set; }

            public Task DropTask { get; set; }

            public void Dispose()
            {
                Cancellation.Dispose();
            }
        }

        public int Index
        {
            get
            {
                return _index;
            }
            set
            {
                int count = Configuration?.Commands?.Count ?? 0;
                _index = count == 0 || value < 0 || value >= count ? 0 : value;
            }
        }

        public Configuration Configuration
        {
            get
            {
                return _config;
            }
            set
            {
                if (value != _config)
                {
                    _config = value;
                    this.ConfigurationChanged?.Invoke(_config);
                }
            }
        }

        public static Dictionary<int, Configuration> Configurations => GameSessionState.Current.BotConfigurations;

        public static Dictionary<int, int> OldIndex => GameSessionState.Current.BotOldIndexes;

        public static Dictionary<string, string> ProtectedMonsters = new Dictionary<string, string>
        {
                { "Escherion"     , "Staff of Inversion"},
                { "Vath"          , "Stalagbite"},
                //{ "Ultra Engineer", "Attack Drone,Defense Drone"},
        };

        public int CurrentConfiguration { get; set; } = 0;

        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }
            set
            {
                _isRunning = value;
                this.IsRunningChanged?.Invoke(_isRunning);
            }
        }

        public event Action<bool> IsRunningChanged;

        public event Action<int> IndexChanged;

        public event Action<Configuration> ConfigurationChanged;

        public int selectedIndex { get; set; }

        public void Start(Configuration config)
        {
            LogForm.Instance.AppendBot($"[{DateTime.Now:hh:mm:ss}] Starting the bot.\r\n");
            ScheduleLifecycle(() => StartLifecycleAsync(config));
        }

        private async Task StartLifecycleAsync(Configuration config)
        {
            BotData.BotMap = null;
            BotData.BotCell = null;
            BotData.BotPad = null;
            BotData.BotSkill = null;
            Flash.Call("advancedOpt", OptionsManager.Untarget ? true : false);
            await StartRunAsync(config, 0);
            LogForm.Instance.AppendDebug($"[{DateTime.Now:hh:mm:ss}] The bot has been started.\r\n");
        }

        public void Stop()
        {
            LogForm.Instance.AppendBot($"[{DateTime.Now:hh:mm:ss}] Stopping the bot.\r\n");
            CancelCurrentRun();
            ScheduleLifecycle(StopLifecycleAsync);
        }

        private async Task StopLifecycleAsync()
        {
            Flash.Call("advancedOpt", OptionsManager.Untarget ? true : false);
            Flash.Call("dropUIOpt", true);
            IsRunning = false;
            await StopRunAsync(bankItems: true);
            LogForm.Instance.AppendDebug($"[{DateTime.Now:hh:mm:ss}] The bot has been stopped.\r\n");
        }

        public void Resume(Configuration config)
        {
            LogForm.Instance.AppendBot($"[{DateTime.Now:hh:mm:ss}] Resuming the bot.\r\n");
            ScheduleLifecycle(() => ResumeLifecycleAsync(config));
        }

        private async Task ResumeLifecycleAsync(Configuration config)
        {
            selectedIndex = _resumeIndex;
            await StartRunAsync(config, _resumeIndex);
            LogForm.Instance.AppendDebug($"[{DateTime.Now:hh:mm:ss}] The bot has been resumed.\r\n");
        }

        public void Pause()
        {
            LogForm.Instance.AppendBot($"[{DateTime.Now:hh:mm:ss}] Pausing the bot.\r\n");
            _resumeIndex = Index;
            CancelCurrentRun();
            ScheduleLifecycle(PauseLifecycleAsync);
        }

        private async Task PauseLifecycleAsync()
        {
            await StopRunAsync(bankItems: true);
            LogForm.Instance.AppendDebug($"[{DateTime.Now:hh:mm:ss}] The bot has been paused.\r\n");
        }

        private void ScheduleLifecycle(Func<Task> operation)
        {
            ClientSession capturedSession = owningSession ?? ClientSessionContext.Resolve();
            Func<Task> scopedOperation = () => RunInSessionAsync(capturedSession, operation);
            Root root = Root.Instance;
            if (root != null && !root.IsDisposed && root.IsHandleCreated && root.InvokeRequired)
            {
                try
                {
                    root.BeginInvoke((Action)(() => ObserveLifecycle(scopedOperation())));
                    return;
                }
                catch (InvalidOperationException)
                {
                    // The window can close between the handle checks and BeginInvoke.
                }
            }

            ObserveLifecycle(scopedOperation());
        }

        private static async Task RunInSessionAsync(ClientSession session, Func<Task> operation)
        {
            if (session == null)
            {
                await operation();
                return;
            }

            using (ClientSessionContext.Push(session))
            {
                await operation();
            }
        }

        private static void ObserveLifecycle(Task task)
        {
            task.ContinueWith(
                completed =>
                {
                    Exception exception = completed.Exception?.GetBaseException();
                    if (exception != null)
                    {
                        LogForm.Instance.AppendDebug($"[{DateTime.Now:hh:mm:ss}] Bot lifecycle error: {exception.Message}\r\n");
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        private void CancelCurrentRun()
        {
            BotRunContext run = _currentRun;
            if (run != null && !run.Cancellation.IsCancellationRequested)
            {
                run.Cancellation.Cancel(throwOnFirstException: false);
            }
        }

        private async Task StartRunAsync(Configuration config, int initialIndex)
        {
            await _runTransition.WaitAsync();
            try
            {
                if (config == null)
                    throw new ArgumentNullException(nameof(config));

                await StopCurrentRunAsync(bankItems: false);

                BotRunContext run = new BotRunContext(config);
                _currentRun = run;
                _ctsBot = run.Cancellation;
                IsRunning = true;
                Player.CancelTarget();
                BotUtilities.ActiveBot = true;
                BotUtilities.HasLoadedQuests = false;
                Configuration = config;
                Index = initialIndex;
                _resumeIndex = Index;
            //_questDelayCounter = new Stopwatch();
            //_boostDelayCounter = new Stopwatch();
                World.ItemDropped -= OnItemDropped;
                Player.Quests.QuestsLoaded -= OnQuestsLoaded;
                Player.Quests.QuestCompleted -= OnQuestCompleted;
                World.ItemDropped += OnItemDropped;
                Player.Quests.QuestsLoaded += OnQuestsLoaded;
                Player.Quests.QuestCompleted += OnQuestCompleted;
            //_questDelayCounter.Start();
            //_boostDelayCounter.Start();
                BotData.BotState = BotData.State.Others;
                BotData.SkillSet.Clear();
                BotUtilities.AddConfigSkills(config);
                if (config.Items.Count > 0 && config.UnbankOnStart)
                {
                    foreach (string item in config.Items)
                    {
                        run.Cancellation.Token.ThrowIfCancellationRequested();
                        if (!Player.Inventory.ContainsItem(item, "*") && Player.Bank.ContainsItem(item))
                        {
                            await Player.Bank.TransferFromBank(item);
                            await Task.Delay(70, run.Cancellation.Token);
                            LogForm.Instance.AppendBot($"[{DateTime.Now:hh:mm:ss}] Items List - Item transferred to Inventory: {item}.\r\n");
                        }
                        else if (Player.Inventory.ContainsItem(item, "*"))
                        {
                            LogForm.Instance.AppendBot($"[{DateTime.Now:hh:mm:ss}] Items List - Item already exists in Inventory: {item}.\r\n");
                        }
                    }
                }
                await this.LoadBotQuests();
                run.Cancellation.Token.ThrowIfCancellationRequested();
                run.QuestTask = CheckQuests(run);
                run.DropTask = World.DropStack.RejectDrops(config, run.Cancellation.Token);
                OptionsManager.Start(config);
                run.BotTask = Task.Run(() => Activate(run), run.Cancellation.Token);
                _questTask = run.QuestTask;
                _dropTask = run.DropTask;
                _botTask = run.BotTask;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                FailBot(ex, _currentRun);
            }
            finally
            {
                _runTransition.Release();
            }
        }

        private async Task StopRunAsync(bool bankItems)
        {
            await _runTransition.WaitAsync();
            try
            {
                await StopCurrentRunAsync(bankItems);
            }
            catch (Exception ex)
            {
                LogForm.Instance.AppendDebug($"[{DateTime.Now:hh:mm:ss}] Bot shutdown error: {ex.Message}\r\n");
            }
            finally
            {
                _runTransition.Release();
            }
        }

        private async Task StopCurrentRunAsync(bool bankItems)
        {
            BotRunContext run = _currentRun;
            if (run == null)
            {
                return;
            }

            if (!run.Cancellation.IsCancellationRequested)
            {
                run.Cancellation.Cancel(throwOnFirstException: false);
            }

            Player.CancelTarget();
            if (run.Configuration.ExitCombatBeforeStop)
            {
                await BotUtilities.MoveOutOfCombat();
            }
            BotUtilities.ActiveBot = false;
            BotUtilities.HasLoadedQuests = false;
            World.ItemDropped -= OnItemDropped;
            Player.Quests.QuestsLoaded -= OnQuestsLoaded;
            Player.Quests.QuestCompleted -= OnQuestCompleted;
            OptionsManager.Stop(run.Configuration);
            BotData.BotState = BotData.State.Others;

            Task[] tasks = new[] { run.BotTask, run.QuestTask, run.DropTask }
                .Where(task => task != null)
                .ToArray();
            if (tasks.Length > 0)
            {
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    LogForm.Instance.AppendDebug($"[{DateTime.Now:hh:mm:ss}] Bot worker shutdown error: {ex.GetBaseException().Message}\r\n");
                }
            }

            if (bankItems && run.Configuration.Items.Count > 0 && run.Configuration.BankOnStop)
            {
                foreach (InventoryItem item in Player.Inventory.Items.ToList())
                {
                    if (!item.IsEquipped && item.Category != "Class" && item.Name != "Treasure Potion" && run.Configuration.Items.Contains(item.Name))
                    {
                        await Player.Bank.TransferToBank(item.Name);
                        await Task.Delay(70);
                        LogForm.Instance.AppendBot($"[{DateTime.Now:hh:mm:ss}] Items List - Item transferred to Bank: {item.Name}.\r\n");
                    }
                }
            }

            if (ReferenceEquals(_currentRun, run))
            {
                _currentRun = null;
                _ctsBot = null;
                _botTask = null;
                _questTask = null;
                _dropTask = null;
            }
            run.Dispose();
        }

        private async Task Activate(BotRunContext run)
        {
            CancellationToken token = run.Cancellation.Token;
            Configuration config = run.Configuration;
            try
            {
                while (!token.IsCancellationRequested && ReferenceEquals(_currentRun, run))
                {
                    if (!Player.IsLoggedIn)
                    {
                        if (!config.AutoRelogin)
                        {
                            break;
                        }
                        OptionsManager.Stop(config);
                        await AutoRelogin.Login(config.Server, config.RelogDelay, run.Cancellation, config.RelogRetryUponFailure);
                        BotUtilities.HasLoadedQuests = false;
                        Index = 0;
                        OptionsManager.Start(config);
                        await this.LoadBotQuests();
                    }
                    if (Player.IsLoggedIn && !Player.IsAlive)
                    {
                        World.SetSpawnPoint();
                        await this.WaitUntil(() => Player.IsAlive, () => !token.IsCancellationRequested && Player.IsLoggedIn, -1);
                        await Task.Delay(1000, token);
                        BotUtilities.ShouldUseSkill = true;
                        Index = config.RestartUponDeath ? 0 : Math.Max(0, Index - 1);
                    }
                    if (config.Boosts.Count > 0)
                    {
                        await CheckBoosts(token);
                    }
                    if (config.RestIfHp)
                    {
                        await RestHealth(token);
                    }
                    if (config.RestIfMp)
                    {
                        await RestMana(token);
                    }
                    if (config.Commands == null || config.Commands.Count == 0)
                    {
                        throw new InvalidOperationException("The bot has no commands to run.");
                    }

                    IndexChanged?.Invoke(Index);
                    cmd = config.Commands[Index];
                    if (cmd.ShouldLog())
                        LogForm.Instance.AppendBot($"[{DateTime.Now:hh:mm:ss}] (Index {Index}) {cmd.LogSpecificCmd()}{cmd.ToString()}.\r\n");
                    await cmd.Execute(this);
                    token.ThrowIfCancellationRequested();
                    if (config.BotDelay > 0 && (!config.SkipDelayIndexIf || (config.SkipDelayIndexIf && cmd.RequiresDelay())))
                    {
                        await Task.Delay(config.BotDelay, token);
                    }
                    Index++;
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                FailBot(ex, run);
            }
        }

        private void FailBot(Exception ex, BotRunContext run)
        {
            if (run != null && !ReferenceEquals(_currentRun, run))
            {
                return;
            }
            IsRunning = false;
            run?.Cancellation.Cancel(false);
            BotUtilities.ActiveBot = false;
            string message = ex?.Message ?? "Unknown bot error";
            LogForm.Instance.AppendDebug($"[{DateTime.Now:hh:mm:ss}] Bot stopped after an error: {message}\r\n");
            LogForm.Instance.AppendBot($"[{DateTime.Now:hh:mm:ss}] Bot error: {message}\r\n");
            if (run != null)
            {
                ScheduleLifecycle(() => StopRunAsync(bankItems: false));
            }
        }

        private async Task RestHealth(CancellationToken token)
        {
            if (Player.Health / (double)Player.HealthMax <= Configuration.RestHp / 100.0)
            {
                BotData.State TempState = BotData.BotState;
                BotData.BotState = BotData.State.Rest;
                if (Configuration.ExitCombatBeforeRest)
                {
                    await BotUtilities.MoveOutOfCombat();
                }
                Player.Rest();
                await this.WaitUntil(() => Player.Health == Player.HealthMax, () => !token.IsCancellationRequested, 20, 500);
                BotData.BotState = TempState;
            }
        }

        private async Task RestMana(CancellationToken token)
        {
            if (Player.Mana / (double)Player.ManaMax <= Configuration.RestMp / 100.0)
            {
                BotData.State TempState = BotData.BotState;
                BotData.BotState = BotData.State.Rest;
                if (Configuration.ExitCombatBeforeRest)
                {
                    await BotUtilities.MoveOutOfCombat();
                }
                Player.Rest();
                await this.WaitUntil(() => Player.Mana >= Player.ManaMax, () => !token.IsCancellationRequested, 20, 500);
                BotData.BotState = TempState;
            }
        }

        private async Task CheckBoosts(CancellationToken token)
        {
            List<InventoryItem> boosts = Configuration.Boosts.FindAll((InventoryItem b) => !Player.HasActiveBoost(b.Name));
            if (boosts.Count == 0)
            {
                return;
            }
            foreach (InventoryItem boost in boosts)
            {
                Player.UseBoost(boost.Id);
                await this.WaitUntil(() => Player.HasActiveBoost(boost.Name), () => !token.IsCancellationRequested, 6, 500);
            }
        }

        private async Task CheckQuests(BotRunContext run)
        {
            CancellationToken token = run.Cancellation.Token;
            Configuration config = run.Configuration;
            List<Quest> quests = new List<Quest>();
            while (!token.IsCancellationRequested && ReferenceEquals(_currentRun, run))
            {
                try
                {
                    await Task.Delay(250, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                quests = config.Quests?.Where((Quest q) => q.CanComplete).ToList() ?? new List<Quest>();
                if (quests.Count == 0)
                {
                    continue;
                }
                for (int i = 0; i < quests.Count; i++)
                {
                    await this.WaitUntil(() => World.IsActionAvailable(LockActions.TryQuestComplete), () => !token.IsCancellationRequested, 10, 500);
                    quests[i].Complete();
                    await this.WaitUntil(() => !Player.Quests.IsInProgress(quests[i].Id), () => !token.IsCancellationRequested, 10, 500);
                }
            }
        }

        public int DropDelay { get; set; } = 0;

        public async void OnItemDropped(InventoryItem drop)
        {
            NotifyDrop(drop);
            if (Configuration.EnablePickupAll)
            {
                await Task.Delay(Math.Max(0, Configuration.DropDelay));
                await World.DropStack.GetDrop(drop.Id);
            }
            else if (Configuration.EnablePickup && Configuration.Drops.Any((string d) => d.Equals(drop.Name, StringComparison.OrdinalIgnoreCase)))
            {
                await Task.Delay(Math.Max(0, Configuration.DropDelay));
                await World.DropStack.GetDrop(drop.Id);
            }
            else if (Configuration.EnablePickupAcTagged)
            {
                if (drop.IsAcItem)
                {
                    await Task.Delay(Math.Max(0, Configuration.DropDelay));
                    await World.DropStack.GetDrop(drop.Id);
                }
            }
        }

        private void NotifyDrop(InventoryItem drop)
        {
            if (Configuration.NotifyUponDrop.Count > 0 && Configuration.NotifyUponDrop.Any((string d) => d.Equals(drop.Name, StringComparison.OrdinalIgnoreCase)))
            {
                for (int i = 0; i < 10; i++)
                {
                    Console.Beep();
                }
            }
        }

        public async void OnQuestsLoaded(List<Quest> quests)
        {
            await Task.Delay(10);
            List<Quest> configQuests = quests.Where(q => Configuration.Quests.Any((Quest cq) => cq.Id == q.Id)).ToList();
            if (configQuests.Count == 0)
            {
                return;
            }
            for (int i = 0; i < configQuests.Count; i++)
            {
                await this.WaitUntil(() => World.IsActionAvailable(LockActions.AcceptQuest), () => IsRunning, 10, 500);
                configQuests[i].Accept();
            }
        }

        public async void OnQuestCompleted(CompletedQuest quest)
        {
            await Task.Delay(10);
            Quest configQuest = Configuration.Quests.Find(q => q.Id == quest.Id);
            if (configQuest == null)
            {
                return;
            }
            await this.WaitUntil(() => World.IsActionAvailable(LockActions.AcceptQuest), () => IsRunning, 10, 500);
            configQuest.Accept();
        }

        public static List<string> DropsInInventory => GameSessionState.Current.BotDropsInInventory;

        public bool HasDropInInventory(string item)
        {
            if (DropsInInventory.Contains(item))
            {
                DropsInInventory.Remove(item);
                return true;
            }
            return false;
        }

        public Stopwatch questFailureCounter = new Stopwatch();

        public int questCompletionFailure = 0;

        public async void ReloginOnQuestFailure()
        {
            if (questFailureCounter.ElapsedMilliseconds <= 15000 && questCompletionFailure >= 3)
            {
                questFailureCounter.Stop();
                questCompletionFailure = 0;
                Server server = Server.List.Find((Server s) => s.Name.Equals(World.ServerName(), StringComparison.OrdinalIgnoreCase));
                Player.Logout();
                if (Configuration != null && !Configuration.AutoRelogin)
                {
                    CancellationTokenSource loginCancellation = CreateSessionLinkedCancellation();
                    try
                    {
                        await this.WaitUntil(() => AutoRelogin.LoginLabel, () => !loginCancellation.IsCancellationRequested, 5, 1500);
                        AutoRelogin.LoginExecute();
                        await this.WaitUntil(() => !AutoRelogin.IsClientLoading("Account"), () => !loginCancellation.IsCancellationRequested, 10, 500);
                        await AutoRelogin.ForceLogin(server, loginCancellation);
                    }
                    finally
                    {
                        loginCancellation.Dispose();
                    }
                }
            }
        }

        public Stopwatch buyFailureCounter = new Stopwatch();

        public int itemBuyFailure = 0;

        public async void ReloginOnItemBuyFailure()
        {
            if (buyFailureCounter.ElapsedMilliseconds <= 15000 && itemBuyFailure >= 3)
            {
                buyFailureCounter.Stop();
                itemBuyFailure = 0;
                Server server = Server.List.Find((Server s) => s.Name.Equals(World.ServerName(), StringComparison.OrdinalIgnoreCase));
                Player.Logout();
                if (Configuration != null && !Configuration.AutoRelogin)
                {
                    CancellationTokenSource loginCancellation = CreateSessionLinkedCancellation();
                    try
                    {
                        await this.WaitUntil(() => AutoRelogin.LoginLabel, () => !loginCancellation.IsCancellationRequested, 5, 1500);
                        AutoRelogin.LoginExecute();
                        await this.WaitUntil(() => !AutoRelogin.IsClientLoading("Account"), () => !loginCancellation.IsCancellationRequested, 10, 500);
                        await AutoRelogin.ForceLogin(server, loginCancellation);
                    }
                    finally
                    {
                        loginCancellation.Dispose();
                    }
                }
            }
        }

        private CancellationTokenSource CreateSessionLinkedCancellation()
        {
            ClientSession session = owningSession ?? ClientSessionContext.Resolve();
            return session == null
                ? new CancellationTokenSource()
                : CancellationTokenSource.CreateLinkedTokenSource(session.LifetimeToken);
        }

    }
}
