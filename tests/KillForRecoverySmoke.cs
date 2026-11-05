using AxShockwaveFlashObjects;
using Grimoire.Botting;
using Grimoire.Botting.Commands;
using Grimoire.Botting.Commands.Combat;
using Grimoire.Clients;
using Grimoire.Game;
using Grimoire.Game.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

internal static class KillForRecoverySmoke
{
    private const string MonsterName = "Test Monster";
    private const string TempItemName = "Test Drop";
    private const int RequiredQuantity = 2;

    [STAThread]
    private static int Main()
    {
        try
        {
            Run().GetAwaiter().GetResult();
            Console.WriteLine("PASS: Kill-for recovers a stale room-clear target without disturbing valid combat or serialized command contracts.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static async Task Run()
    {
        AssertPublicCommandSurface();
        await AssertRoomClearStaleTargetRecovers();
        await AssertLongRunningTargetIsNotInterrupted();
        await AssertBriefAvailabilityPulseIsDebounced();
    }

    private static async Task AssertRoomClearStaleTargetRecovers()
    {
        FakeFlashBridge bridge = new FakeFlashBridge(CombatScenario.StaleTargetAfterRoomClear);
        FakeBotEngine engine = CreateEngine();
        ClientSession session = new ClientSession("Kill-for recovery", bridge, engine);
        try
        {
            Task commandTask;
            using (ClientSessionContext.Push(session))
            {
                PrepareSession(session);
                commandTask = CreateCommand().Execute(engine);
                Task winner = await Task.WhenAny(commandTask, Task.Delay(4000));
                if (!ReferenceEquals(winner, commandTask))
                {
                    int attackCount = bridge.AttackCount;
                    int skillCount = bridge.SkillCallCount;
                    engine.IsRunning = false;
                    await AwaitBounded(commandTask, "The stalled Kill-for command did not stop during test cleanup.");
                    throw new InvalidOperationException(
                        "Kill-for remained blocked on the stale target after the room cleared "
                        + "(attacks=" + attackCount + ", skills=" + skillCount + ").");
                }

                await commandTask;
            }

            if (bridge.AttackCount != 2)
                throw new InvalidOperationException("Kill-for did not reacquire the respawned monster exactly once.");
            if (bridge.TempQuantity < RequiredQuantity)
                throw new InvalidOperationException("Kill-for completed before the required temporary-item quantity was reached.");
            if (bridge.SkillCallCount == 0)
                throw new InvalidOperationException("The reproduction did not exercise the continuing skill loop.");
        }
        finally
        {
            engine.IsRunning = false;
            session.Dispose();
        }
    }

    private static async Task AssertLongRunningTargetIsNotInterrupted()
    {
        FakeFlashBridge bridge = new FakeFlashBridge(CombatScenario.ValidLongFight);
        FakeBotEngine engine = CreateEngine();
        ClientSession session = new ClientSession("Long fight control", bridge, engine);
        try
        {
            Task commandTask;
            using (ClientSessionContext.Push(session))
            {
                PrepareSession(session);
                commandTask = CreateCommand().Execute(engine);
                await Task.Delay(1400);
                if (commandTask.IsCompleted)
                    throw new InvalidOperationException("Kill-for interrupted a valid long-running target.");
                if (bridge.AttackCount != 1)
                    throw new InvalidOperationException("Kill-for retargeted during a valid long-running fight.");
                if (bridge.CancelTargetCount != 0)
                    throw new InvalidOperationException("Kill-for cancelled a valid long-running target.");

                engine.IsRunning = false;
                await AwaitBounded(commandTask, "The long-fight control did not stop during cleanup.");
            }
        }
        finally
        {
            engine.IsRunning = false;
            session.Dispose();
        }
    }

    private static async Task AssertBriefAvailabilityPulseIsDebounced()
    {
        FakeFlashBridge bridge = new FakeFlashBridge(CombatScenario.SingleUnavailablePulse);
        FakeBotEngine engine = CreateEngine();
        ClientSession session = new ClientSession("Availability pulse control", bridge, engine);
        try
        {
            Task commandTask;
            using (ClientSessionContext.Push(session))
            {
                PrepareSession(session);
                commandTask = CreateCommand().Execute(engine);
                await Task.Delay(1400);
                if (commandTask.IsCompleted)
                    throw new InvalidOperationException("Kill-for treated one unavailable sample as a stale target.");
                if (bridge.AttackCount != 1)
                    throw new InvalidOperationException("Kill-for retargeted after one unavailable sample.");
                if (bridge.CancelTargetCount != 0)
                    throw new InvalidOperationException("Kill-for cancelled the target after one unavailable sample.");

                engine.IsRunning = false;
                await AwaitBounded(commandTask, "The availability-pulse control did not stop during cleanup.");
            }
        }
        finally
        {
            engine.IsRunning = false;
            session.Dispose();
        }
    }

    private static FakeBotEngine CreateEngine()
    {
        return new FakeBotEngine
        {
            IsRunning = true,
            Configuration = new Configuration
            {
                SkillDelay = 10
            }
        };
    }

    private static CmdKillFor CreateCommand()
    {
        return new CmdKillFor
        {
            Monster = MonsterName,
            ItemName = TempItemName,
            ItemType = ItemType.TempItems,
            Quantity = RequiredQuantity.ToString()
        };
    }

    private static void PrepareSession(ClientSession session)
    {
        session.GameState.IsLoggedIn = true;
        session.GameState.ShouldUseSkill = true;
    }

    private static async Task AwaitBounded(Task task, string failureMessage)
    {
        Task winner = await Task.WhenAny(task, Task.Delay(3000));
        if (!ReferenceEquals(winner, task))
            throw new InvalidOperationException(failureMessage);
        await task;
    }

    private static void AssertPublicCommandSurface()
    {
        AssertPublicProperties(typeof(CmdKill), "Monster", "Packet");
        AssertPublicProperties(typeof(CmdKillFor), "ItemName", "ItemType", "Monster", "Quantity");
    }

    private static void AssertPublicProperties(Type type, params string[] expected)
    {
        string[] actual = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        string[] sortedExpected = expected.OrderBy(name => name, StringComparer.Ordinal).ToArray();
        if (!actual.SequenceEqual(sortedExpected))
        {
            throw new InvalidOperationException(
                type.Name + " public serialized surface changed. Expected ["
                + string.Join(", ", sortedExpected) + "] but found ["
                + string.Join(", ", actual) + "].");
        }
    }

    private enum CombatScenario
    {
        StaleTargetAfterRoomClear,
        ValidLongFight,
        SingleUnavailablePulse
    }

    private sealed class FakeBotEngine : IBotEngine
    {
        public bool IsRunning { get; set; }

        public int Index { get; set; }

        public Configuration Configuration { get; set; }

        public int CurrentConfiguration { get; set; }

        public event Action<bool> IsRunningChanged;

        public event Action<int> IndexChanged;

        public event Action<Configuration> ConfigurationChanged;

        public bool IsVar(string value)
        {
            return false;
        }

        public string GetVar(string value)
        {
            return value;
        }

        public string Value(string value)
        {
            return value;
        }

        public void Start(Configuration config)
        {
            Configuration = config;
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public void Resume(Configuration config)
        {
            Start(config);
        }

        public void Pause()
        {
            IsRunning = false;
        }
    }

    private sealed class FakeFlashBridge : IFlashBridge
    {
        private readonly object syncRoot = new object();
        private readonly CombatScenario scenario;
        private bool hasTarget;
        private bool monsterAvailable = true;
        private int availabilityChecksAfterAttack;
        private int attackCount;
        private int cancelTargetCount;
        private int skillCallCount;
        private int tempQuantity;

        public FakeFlashBridge(CombatScenario scenario)
        {
            this.scenario = scenario;
        }

        public int AttackCount { get { return Volatile.Read(ref attackCount); } }

        public int CancelTargetCount { get { return Volatile.Read(ref cancelTargetCount); } }

        public int SkillCallCount { get { return Volatile.Read(ref skillCallCount); } }

        public int TempQuantity { get { return Volatile.Read(ref tempQuantity); } }

        public GameEngineKind Engine { get { return GameEngineKind.FlashActiveX; } }

        public AxShockwaveFlash Control { get { return null; } }

        public Control HostControl { get { return null; } }

        public event Action<int> SwfLoadProgress;

        public event FlashBridgeCallHandler FlashCall;

        public event FlashBridgeErrorHandler FlashError;

        public void LoadMovie(byte[] swf)
        {
        }

        public T Call<T>(string function, params object[] args)
        {
            return ConvertResult<T>(HandleCall(function));
        }

        public object Call(string function, Type resultType, params object[] args)
        {
            object value = HandleCall(function);
            if (value == null)
                return resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
            return value;
        }

        public T CallWithStringArguments<T>(string function, params string[] args)
        {
            return ConvertResult<T>(HandleCall(function));
        }

        public string CallString(string function, params object[] args)
        {
            object value = HandleCall(function);
            return value == null ? null : value.ToString();
        }

        public string CallGameFunction(string path, params object[] args)
        {
            return null;
        }

        public string GetGameObject(string path)
        {
            return null;
        }

        public string GetGameObjectStatic(string path)
        {
            return null;
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
            SwfLoadProgress = null;
            FlashCall = null;
            FlashError = null;
        }

        private object HandleCall(string function)
        {
            lock (syncRoot)
            {
                switch (function)
                {
                    case "AttackMonster":
                        attackCount++;
                        hasTarget = attackCount == 1;
                        if (scenario == CombatScenario.StaleTargetAfterRoomClear && attackCount == 1)
                            monsterAvailable = false;
                        if (attackCount >= 2)
                        {
                            tempQuantity = RequiredQuantity;
                            hasTarget = false;
                            monsterAvailable = true;
                        }
                        return null;

                    case "CancelTarget":
                        cancelTargetCount++;
                        hasTarget = false;
                        if (scenario == CombatScenario.StaleTargetAfterRoomClear && attackCount == 1)
                            monsterAvailable = true;
                        return null;

                    case "HasTarget":
                        return hasTarget;

                    case "IsMonsterAvailable":
                        if (scenario == CombatScenario.SingleUnavailablePulse && attackCount == 1)
                        {
                            availabilityChecksAfterAttack++;
                            return availabilityChecksAfterAttack != 1;
                        }
                        return monsterAvailable;

                    case "GetTempItems":
                        return tempQuantity == 0
                            ? new List<TempItem>()
                            : new List<TempItem>
                            {
                                new TempItem { Name = TempItemName, Quantity = tempQuantity }
                            };

                    case "GetInventoryItems":
                        return new List<InventoryItem>();

                    case "Health":
                    case "HealthMax":
                    case "Mana":
                    case "ManaMax":
                        return 100;

                    case "CheckSkillAvailability":
                        return true;

                    case "UseSkill":
                        skillCallCount++;
                        return null;

                    case "Cell":
                        return "Enter";

                    case "Pad":
                        return "Spawn";

                    default:
                        return null;
                }
            }
        }

        private static T ConvertResult<T>(object value)
        {
            if (value == null)
                return default(T);
            return (T)value;
        }
    }
}
