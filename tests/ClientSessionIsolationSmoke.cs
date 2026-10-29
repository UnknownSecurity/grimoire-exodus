using AxShockwaveFlashObjects;
using Grimoire.Botting;
using Grimoire.Botting.Commands.Misc;
using Grimoire.Clients;
using Grimoire.Game;
using Grimoire.Game.Data;
using Grimoire.Networking;
using Grimoire.Tools;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

internal static class ClientSessionIsolationSmoke
{
    [STAThread]
    private static int Main()
    {
        try
        {
            Run();
            Console.WriteLine("PASS: client context, identity and account reservations, mutable state, load state, and disposal are isolated.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void Run()
    {
        AssertEqual(
            false,
            ClientSurfacePolicy.ShouldRenderClientSurface(true, true, false),
            "The AQW surface remained visible behind the Home workspace.");
        AssertEqual(
            true,
            ClientSurfacePolicy.ShouldRenderClientSurface(false, true, false),
            "The selected AQW surface was hidden.");
        AssertEqual(
            false,
            ClientSurfacePolicy.ShouldRenderClientSurface(false, false, false),
            "An inactive idle AQW surface remained visible behind the selected client.");
        AssertEqual(
            true,
            ClientSurfacePolicy.ShouldRenderClientSurface(false, false, true),
            "An inactive bot-owned AQW surface was hidden and could lose legacy timing behavior.");
        AssertEqual(
            true,
            ClientSurfacePolicy.ShouldRenderClientSurface(true, false, true),
            "A bot-owned AQW surface was hidden behind Home and could lose legacy timing behavior.");
        AssertEqual(
            true,
            ClientSurfacePolicy.ShouldRenderClientSurface(true, false, false, true),
            "An initializing Ruffle surface was hidden behind Home and could not make progress.");

        ClientSessionManager manager = new ClientSessionManager(2, true);
        FakeFlashBridge firstBridge = new FakeFlashBridge("first");
        FakeFlashBridge secondBridge = new FakeFlashBridge("second");
        ClientSession first = new ClientSession("Client 01", firstBridge, new Bot());
        ClientSession second = new ClientSession("Client 02", secondBridge, new Bot());
        Task firstOptionsTask = null;
        Task secondOptionsTask = null;
        CancellationToken firstLoginToken = CancellationToken.None;
        int firstIdentityChanges = 0;
        int secondIdentityChanges = 0;
        try
        {
            manager.Add(first);
            manager.Add(second);
            first.IdentityChanged += delegate { firstIdentityChanges++; };
            second.IdentityChanged += delegate { secondIdentityChanges++; };

            first.AssignAccount("account-one", " first ");
            second.AssignAccount("account-two", "second");
            AssertEqual(true, first.IsAssignedToAccount("ACCOUNT-ONE", null), "The first account reservation was not normalized by identifier.");
            AssertEqual(true, first.IsAssignedToAccount(null, "FIRST"), "The first account reservation was not normalized by username.");
            AssertEqual(false, second.IsAssignedToAccount("account-one", "first"), "An account reservation crossed client sessions.");
            AssertEqual(1, firstIdentityChanges, "The first account reservation did not publish one identity change.");
            AssertEqual(1, secondIdentityChanges, "The second account reservation did not publish one identity change.");

            AssertEqual("first", Flash.Call<string>("identity", new object[0]), "The initial active client was not used.");
            manager.SetActive(second);
            AssertEqual("second", Flash.Call<string>("identity", new object[0]), "The active-client fallback was not updated.");

            using (ClientSessionContext.Push(first))
            {
                AssertEqual("first", Flash.Call<string>("identity", new object[0]), "A scoped call was retargeted to the active client.");
                PopulateState("first", 101, true);
                using (ClientSessionContext.Push(second))
                {
                    AssertEqual("second", Flash.Call<string>("identity", new object[0]), "A nested client scope did not take effect.");
                    PopulateState("second", 202, false);
                    AssertState("second", 202, false);
                }
                AssertEqual("first", Flash.Call<string>("identity", new object[0]), "A nested scope did not restore its owner.");
                AssertState("first", 101, true);
            }

            using (ClientSessionContext.Push(second))
                AssertState("second", 202, false);
            AssertEqual("first", first.DisplayName, "The logged-in character name was not applied to its client title.");
            AssertEqual("Client 02", second.DisplayName, "A logged-out client title used stale character identity.");
            AssertEqual(true, first.IsAssignedToAccount(null, "first"), "The authenticated account was not recognized as active.");
            AssertEqual(true, second.IsAssignedToAccount("account-two", null), "A pending account reservation was lost before login.");

            using (ClientSessionContext.Push(first))
                Player.IsLoggedIn = false;
            AssertEqual("Client 01", first.DisplayName, "Logging out did not restore the client's generic title.");
            AssertEqual(false, first.IsAssignedToAccount("account-one", "first"), "Logging out did not release the account reservation.");
            AssertEqual(true, second.IsAssignedToAccount("account-two", "second"), "Logging out one client released another client's account.");
            using (ClientSessionContext.Push(first))
                Player.IsLoggedIn = true;
            AssertEqual("first", first.DisplayName, "Logging back in did not restore the detected character title.");

            if (ReferenceEquals(first.GameState.Inventory, second.GameState.Inventory)
                || ReferenceEquals(first.GameState.DropStack, second.GameState.DropStack)
                || ReferenceEquals(first.GameState.TempValues, second.GameState.TempValues)
                || ReferenceEquals(first.GameState.BotConfigurations, second.GameState.BotConfigurations)
                || ReferenceEquals(first.OptionState, second.OptionState)
                || ReferenceEquals(first.BotDocument, second.BotDocument)
                || ReferenceEquals(first.LoginState, second.LoginState)
                || ReferenceEquals(first.PacketSpammer, second.PacketSpammer))
            {
                throw new InvalidOperationException("Mutable game or bot containers are shared between client sessions.");
            }

            first.BotDocument.Replace(new Configuration { Author = "first document" }, "first.gbot");
            second.BotDocument.Replace(new Configuration { Author = "second document" }, "second.gbot");
            AssertEqual("first document", first.BotDocument.Configuration.Author, "The first bot document was overwritten.");
            AssertEqual("second document", second.BotDocument.Configuration.Author, "The second bot document was overwritten.");
            AssertEqual("first", first.BotDocument.DisplayName, "The first bot document name was not isolated.");
            AssertEqual("second", second.BotDocument.DisplayName, "The second bot document name was not isolated.");

            Configuration runtimeConfiguration = new Configuration { Author = "first runtime" };
            first.BotEngine.Configuration = runtimeConfiguration;
            if (!ReferenceEquals(runtimeConfiguration, first.BotDocument.Configuration))
                throw new InvalidOperationException("The owning session did not capture its bot engine configuration.");
            AssertEqual("second document", second.BotDocument.Configuration.Author, "A runtime configuration crossed sessions.");

            CancellationTokenSource firstLogin = first.LoginState.BeginOperation();
            firstLoginToken = firstLogin.Token;
            CancellationTokenSource secondLogin = second.LoginState.BeginOperation();
            CancellationTokenSource replacementLogin = first.LoginState.BeginOperation();
            if (!firstLoginToken.IsCancellationRequested)
                throw new InvalidOperationException("Replacing a login operation did not cancel the old operation for that client.");
            if (secondLogin.IsCancellationRequested)
                throw new InvalidOperationException("Replacing one client's login operation canceled another client.");
            first.LoginState.CompleteOperation(firstLogin);
            second.LoginState.CompleteOperation(secondLogin);
            firstLoginToken = replacementLogin.Token;

            int firstPackets = 0;
            int secondPackets = 0;
            using (ClientSessionContext.Push(first))
            {
                InitializeProxyHandlers();
                Proxy.ReceivedFromGame += delegate { firstPackets++; };
                Proxy.Instance.LogPackets("packetFromServer", UnknownPacket());
            }
            using (ClientSessionContext.Push(second))
            {
                InitializeProxyHandlers();
                Proxy.ReceivedFromGame += delegate { secondPackets++; };
                Proxy.Instance.LogPackets("packetFromServer", UnknownPacket());
            }
            using (ClientSessionContext.Push(first))
            {
                Proxy.Instance.LogPackets("packetFromServer", UnknownPacket());
                Proxy.OnPingLatency(75);
            }
            AssertEqual(2, firstPackets, "The first packet stream lost or received another session's packet.");
            AssertEqual(1, secondPackets, "The second packet stream lost or received another session's packet.");
            AssertEqual(75, first.LatestPingLatency, "The first client's ping was not retained by its session.");
            AssertEqual<int?>(null, second.LatestPingLatency, "Ping state leaked into another client.");

            first.GameState.PlayerQuests.LoadedQuests.Clear();
            second.GameState.PlayerQuests.LoadedQuests.Clear();
            manager.SetActive(second);
            first.PacketDispatcher.LogPackets("packetFromServer", QuestPacket(1001, "First quest"));
            manager.SetActive(first);
            second.PacketDispatcher.LogPackets("packetFromServer", QuestPacket(2002, "Second quest"));
            AssertEqual(1, first.GameState.PlayerQuests.LoadedQuests.Count, "The first client's quest response was routed through the active tab.");
            AssertEqual(1001, first.GameState.PlayerQuests.LoadedQuests[0].Id, "The first client received another client's quest.");
            AssertEqual(1, second.GameState.PlayerQuests.LoadedQuests.Count, "The second client's quest response was routed through the active tab.");
            AssertEqual(2002, second.GameState.PlayerQuests.LoadedQuests[0].Id, "The second client received another client's quest.");

            int firstSpamIterations = 0;
            int secondSpamIterations = 0;
            first.PacketSpammer.IndexChanged += delegate { Interlocked.Increment(ref firstSpamIterations); };
            second.PacketSpammer.IndexChanged += delegate { Interlocked.Increment(ref secondSpamIterations); };
            first.PacketSpammer.Start(new List<string> { "first-packet" }, 5);
            second.PacketSpammer.Start(new List<string> { "second-packet" }, 5);
            DateTime spamDeadline = DateTime.UtcNow.AddSeconds(5);
            while ((firstBridge.GameFunctionCallCount < 1
                    || secondBridge.GameFunctionCallCount < 1)
                && first.PacketSpammer.LastError == null
                && second.PacketSpammer.LastError == null
                && DateTime.UtcNow < spamDeadline)
            {
                Thread.Sleep(10);
            }
            if (first.PacketSpammer.LastError != null || second.PacketSpammer.LastError != null)
            {
                throw new InvalidOperationException(
                    "A per-session packet spammer faulted.",
                    first.PacketSpammer.LastError ?? second.PacketSpammer.LastError);
            }
            if (firstBridge.GameFunctionCallCount < 1 || secondBridge.GameFunctionCallCount < 1)
            {
                throw new InvalidOperationException(
                    "The per-session packet spammers did not both make progress. First calls: "
                        + firstBridge.GameFunctionCallCount
                        + "; first running: "
                        + first.PacketSpammer.IsRunning
                        + "; first iterations: "
                        + Volatile.Read(ref firstSpamIterations)
                        + "; second calls: "
                        + secondBridge.GameFunctionCallCount
                        + "; second running: "
                        + second.PacketSpammer.IsRunning
                        + "; second iterations: "
                        + Volatile.Read(ref secondSpamIterations)
                        + ".");
            }
            int firstSpamCount = firstBridge.GameFunctionCallCount;
            int secondSpamCount = secondBridge.GameFunctionCallCount;
            int secondIterationCount = Volatile.Read(ref secondSpamIterations);
            first.PacketSpammer.Stop();
            WaitUntil(
                () => secondBridge.GameFunctionCallCount > secondSpamCount
                    || Volatile.Read(ref secondSpamIterations) > secondIterationCount,
                "Stopping one packet spammer stalled another client.");
            Thread.Sleep(50);
            if (firstBridge.GameFunctionCallCount > firstSpamCount + 1)
                throw new InvalidOperationException("The first packet spammer continued after it was stopped.");
            if (secondBridge.GameFunctionCallCount <= secondSpamCount)
                throw new InvalidOperationException("The second packet spammer did not route another packet through its own bridge.");
            second.PacketSpammer.Stop();

            TaskCompletionSource<bool> entered = new TaskCompletionSource<bool>();
            TaskCompletionSource<bool> continueCall = new TaskCompletionSource<bool>();
            Task<string> scopedCall = Task.Run(async delegate
            {
                using (ClientSessionContext.Push(first))
                {
                    entered.SetResult(true);
                    await continueCall.Task;
                    return Flash.Call<string>("identity", new object[0]);
                }
            });

            entered.Task.GetAwaiter().GetResult();
            manager.SetActive(second);
            continueCall.SetResult(true);
            AssertEqual("first", scopedCall.GetAwaiter().GetResult(), "An awaited operation moved to another client after selection changed.");

            first.MarkInitializing();
            second.MarkInitializing();
            firstBridge.ReportProgress(47);
            AssertEqual(47, first.LoadProgress, "The first client's progress was not recorded.");
            AssertEqual(0, second.LoadProgress, "Progress leaked into another client.");
            firstBridge.ReportProgress(100);
            if (!first.IsReady || second.IsReady)
                throw new InvalidOperationException("Client readiness was not isolated.");

            using (ClientSessionContext.Push(first))
            {
                new CmdSetFPS { FPS = 73 }.Execute(first.BotEngine).GetAwaiter().GetResult();
            }
            AssertEqual<int?>(73, firstBridge.LastFrameRate, "The FPS command did not target its owning client.");
            AssertEqual(73, first.OptionState.DesiredFramesPerSecond, "The FPS command target was not retained for client switching.");
            AssertEqual(true, first.OptionState.UseCustomFrameRate, "The FPS command was not marked as an explicit per-client target.");
            AssertEqual<int?>(null, secondBridge.LastFrameRate, "The FPS command crossed into another client.");

            using (ClientSessionContext.Push(first))
            {
                OptionsManager.ProvokeMonsters = false;
                OptionsManager.PacketSpam = false;
                OptionsManager.EnemyMagnet = false;
                OptionsManager.SkipCutscenes = false;
                firstOptionsTask = OptionsManager.ApplySettings();
            }
            using (ClientSessionContext.Push(second))
            {
                Player.IsLoggedIn = true;
                OptionsManager.ProvokeMonsters = false;
                OptionsManager.PacketSpam = false;
                OptionsManager.EnemyMagnet = false;
                OptionsManager.SkipCutscenes = false;
                secondOptionsTask = OptionsManager.ApplySettings();
            }

            bool rejected = false;
            try
            {
                manager.Add(new ClientSession("Client 03", new FakeFlashBridge("third")));
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            if (!rejected)
                throw new InvalidOperationException("The manager did not enforce its measured client limit.");

            manager.Remove(first);
            if (!firstLoginToken.IsCancellationRequested)
                throw new InvalidOperationException("Removing a client did not cancel its login operation.");
            AssertCompletes(firstOptionsTask, "Removing a client did not cancel its option worker.");
            if (secondOptionsTask.IsCompleted)
                throw new InvalidOperationException("Removing one client canceled another client's option worker.");
            if (!firstBridge.Disposed || secondBridge.Disposed)
                throw new InvalidOperationException("Removing one session disposed the wrong Flash bridge.");
            AssertEqual("second", Flash.Call<string>("identity", new object[0]), "Removing a background client changed the active target.");
        }
        finally
        {
            manager.Dispose();
        }

        if (!secondBridge.Disposed)
            throw new InvalidOperationException("Manager disposal did not dispose its remaining client.");
        AssertCompletes(secondOptionsTask, "Manager disposal did not cancel the remaining option worker.");
    }

    private static void AssertCompletes(Task task, string message)
    {
        if (task == null)
            throw new InvalidOperationException(message);
        if (Task.WhenAny(task, Task.Delay(2000)).GetAwaiter().GetResult() != task)
            throw new InvalidOperationException(message);
        task.GetAwaiter().GetResult();
    }

    private static void WaitUntil(Func<bool> condition, string message)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline)
            Thread.Sleep(10);
        if (!condition())
            throw new InvalidOperationException(message);
    }

    private static void InitializeProxyHandlers()
    {
        MethodInfo initialize = typeof(Proxy).GetMethod(
            "InitializeProxyHandlers",
            BindingFlags.Static | BindingFlags.NonPublic);
        if (initialize == null)
            throw new InvalidOperationException("The proxy initialization seam was not found.");
        initialize.Invoke(null, null);
    }

    private static string UnknownPacket()
    {
        return "{\"t\":\"xt\",\"b\":{\"r\":-1,\"o\":{\"cmd\":\"sessionIsolationProbe\"}}}";
    }

    private static string QuestPacket(int id, string name)
    {
        return "{\"t\":\"xt\",\"b\":{\"r\":-1,\"o\":{\"cmd\":\"getQuests\",\"quests\":{\""
            + id
            + "\":{\"QuestID\":"
            + id
            + ",\"sName\":\""
            + name
            + "\"}}}}}";
    }

    private static void PopulateState(string name, int seed, bool loggedIn)
    {
        Player.Username = name;
        Player.CharUserID = seed;
        Player.IsLoggedIn = loggedIn;
        World.Drops.Add(new InventoryItem { Id = seed + 1, Name = name + " drop" });
        World.CurrentMap = new Map { Name = name + " map" };
        Map.ID = seed + 2;
        Map.Area = name + "-1";
        Server.List = new List<Server> { new Server { Name = name + " server" } };
        BotData.BotState = loggedIn ? BotData.State.Combat : BotData.State.Rest;
        BotData.BotMap = name + " bot map";
        BotData.SkillSet["[" + name.ToUpperInvariant() + "]"] = seed;
        Configuration.Tempvalues[name] = seed;
        Configuration.Tempvariable[name] = name + " value";
        Configuration.Instance.Author = name + " default";
        Bot.Configurations[seed] = new Configuration { Author = name };
        Bot.OldIndex[seed] = seed;
        Bot.DropsInInventory.Add(name + " inventory drop");
        BotUtilities.ActiveBot = loggedIn;
        BotUtilities.HasLoadedQuests = loggedIn;
        BotUtilities.ShouldUseSkill = loggedIn;
        OptionsManager.ProvokeMonsters = loggedIn;
        OptionsManager.PacketSpam = loggedIn;
        OptionsManager.WalkSpeed = seed;
        OptionsManager.Timer = seed + 3;
        OptionsManager.Untarget = loggedIn;
        ClientSessionContext.Current.OptionState.UseCustomFrameRate = loggedIn;
        ClientSessionContext.Current.OptionState.DesiredFramesPerSecond = seed;
    }

    private static void AssertState(string name, int seed, bool loggedIn)
    {
        AssertEqual(name, Player.Username, "Player identity leaked between sessions.");
        AssertEqual(seed, Player.CharUserID, "Player ID leaked between sessions.");
        AssertEqual(loggedIn, Player.IsLoggedIn, "Login state leaked between sessions.");
        AssertEqual(name + " drop", World.Drops[0].Name, "World drops leaked between sessions.");
        AssertEqual(name + " map", World.CurrentMap.Name, "Current map leaked between sessions.");
        AssertEqual(seed + 2, Map.ID, "Map ID leaked between sessions.");
        AssertEqual(name + "-1", Map.Area, "Map area leaked between sessions.");
        AssertEqual(name + " server", Server.List[0].Name, "Server list leaked between sessions.");
        AssertEqual(loggedIn ? BotData.State.Combat : BotData.State.Rest, BotData.BotState, "Bot state leaked between sessions.");
        AssertEqual(name + " bot map", BotData.BotMap, "Bot map leaked between sessions.");
        AssertEqual(seed, BotData.SkillSet["[" + name.ToUpperInvariant() + "]"], "Bot skill state leaked between sessions.");
        AssertEqual(seed, Configuration.Tempvalues[name], "Temporary integer values leaked between sessions.");
        AssertEqual(name + " value", Configuration.Tempvariable[name], "Temporary string values leaked between sessions.");
        AssertEqual(name + " default", Configuration.Instance.Author, "Default configuration leaked between sessions.");
        AssertEqual(name, Bot.Configurations[seed].Author, "Nested bot configurations leaked between sessions.");
        AssertEqual(seed, Bot.OldIndex[seed], "Nested bot indexes leaked between sessions.");
        AssertEqual(name + " inventory drop", Bot.DropsInInventory[0], "Bot inventory-drop state leaked between sessions.");
        AssertEqual(loggedIn, BotUtilities.ActiveBot, "Active-bot state leaked between sessions.");
        AssertEqual(loggedIn, BotUtilities.HasLoadedQuests, "Loaded-quest state leaked between sessions.");
        AssertEqual(loggedIn, BotUtilities.ShouldUseSkill, "Skill-toggle state leaked between sessions.");
        AssertEqual(loggedIn, OptionsManager.ProvokeMonsters, "Provoke state leaked between sessions.");
        AssertEqual(loggedIn, OptionsManager.PacketSpam, "Packet-spam state leaked between sessions.");
        AssertEqual(seed, OptionsManager.WalkSpeed, "Walk-speed state leaked between sessions.");
        AssertEqual(seed + 3, OptionsManager.Timer, "Option timer state leaked between sessions.");
        AssertEqual(loggedIn, OptionsManager.Untarget, "Untarget state leaked between sessions.");
        AssertEqual(loggedIn, ClientSessionContext.Current.OptionState.UseCustomFrameRate, "Frame-rate enablement leaked between sessions.");
        AssertEqual(seed, ClientSessionContext.Current.OptionState.DesiredFramesPerSecond, "Frame-rate value leaked between sessions.");

        Bot expectedEngine = ClientSessionContext.Current.BotEngine as Bot;
        if (!ReferenceEquals(expectedEngine, Bot.Instance))
            throw new InvalidOperationException("The Bot.Instance compatibility facade resolved another session's engine.");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
    }

    private sealed class FakeFlashBridge : IFlashBridge
    {
        private readonly string identity;

        private int gameFunctionCallCount;
        private int lastFrameRate = int.MinValue;

        public FakeFlashBridge(string identity)
        {
            this.identity = identity;
        }

        public AxShockwaveFlash Control
        {
            get { return null; }
        }

        public GameEngineKind Engine { get { return GameEngineKind.FlashActiveX; } }

        public System.Windows.Forms.Control HostControl { get { return null; } }

        public bool Disposed { get; private set; }

        public int GameFunctionCallCount
        {
            get { return Volatile.Read(ref gameFunctionCallCount); }
        }

        public int? LastFrameRate
        {
            get
            {
                int value = Volatile.Read(ref lastFrameRate);
                return value == int.MinValue ? (int?)null : value;
            }
        }

        public event Action<int> SwfLoadProgress;

        public event FlashBridgeCallHandler FlashCall;

        public event FlashBridgeErrorHandler FlashError;

        public void LoadMovie(byte[] swf)
        {
        }

        public void ReportProgress(int progress)
        {
            Action<int> handler = SwfLoadProgress;
            if (handler != null)
                handler(progress);
        }

        public T Call<T>(string function, params object[] args)
        {
            if (function == "SetFPS" && args != null && args.Length == 1)
                Volatile.Write(ref lastFrameRate, Convert.ToInt32(args[0]));
            return typeof(T) == typeof(string) ? (T)(object)identity : default(T);
        }

        public object Call(string function, Type resultType, params object[] args)
        {
            return resultType == typeof(string) ? identity : null;
        }

        public T CallWithStringArguments<T>(string function, params string[] args)
        {
            return Call<T>(function, args);
        }

        public string CallString(string function, params object[] args)
        {
            return identity;
        }

        public string CallGameFunction(string path, params object[] args)
        {
            Interlocked.Increment(ref gameFunctionCallCount);
            return identity;
        }

        public string GetGameObject(string path)
        {
            return identity;
        }

        public string GetGameObjectStatic(string path)
        {
            return identity;
        }

        public T GetGameObject<T>(string path, T defaultValue = default(T))
        {
            return defaultValue;
        }

        public void SetGameObject(string path, object value)
        {
        }

        public void Dispose()
        {
            Disposed = true;
            FlashCall = null;
            FlashError = null;
        }
    }
}
