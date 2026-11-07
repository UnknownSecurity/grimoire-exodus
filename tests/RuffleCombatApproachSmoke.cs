using AxShockwaveFlashObjects;
using Grimoire.Botting;
using Grimoire.Clients;
using Grimoire.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

internal static class RuffleCombatApproachSmoke
{
    [STAThread]
    private static int Main()
    {
        try
        {
            AssertRuffleMovesTowardDistantTarget();
            AssertActiveXBehaviorIsUnchanged();
            AssertInfiniteRangeDoesNotForceMovement();
            AssertInvalidTargetCoordinatesDoNotMove();
            Console.WriteLine("PASS: Ruffle combat approaches distant targets without changing ActiveX or Infinite Range behavior.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void AssertRuffleMovesTowardDistantTarget()
    {
        FakeFlashBridge bridge = new FakeFlashBridge(GameEngineKind.Ruffle, new[] { 100f, 100f }, 450d, 120d);
        using (ClientSession session = CreateSession(bridge, false))
        using (ClientSessionContext.Push(session))
        {
            Player.AttackMonster("Skeleton");
        }

        RequireCalls(bridge, "AttackMonster", "Position", "WalkToPoint");
        RecordedCall movement = bridge.Calls.Last();
        Require(movement.Arguments.Length == 2, "Ruffle movement did not include both coordinates.");
        Require((string)movement.Arguments[0] == "450" && (string)movement.Arguments[1] == "120",
            "Ruffle movement did not use invariant target coordinates.");
    }

    private static void AssertActiveXBehaviorIsUnchanged()
    {
        FakeFlashBridge bridge = new FakeFlashBridge(GameEngineKind.FlashActiveX, new[] { 100f, 100f }, 450d, 120d);
        using (ClientSession session = CreateSession(bridge, false))
        using (ClientSessionContext.Push(session))
        {
            Player.AttackMonster("Skeleton");
        }

        RequireCalls(bridge, "AttackMonster");
    }

    private static void AssertInfiniteRangeDoesNotForceMovement()
    {
        FakeFlashBridge bridge = new FakeFlashBridge(GameEngineKind.Ruffle, new[] { 100f, 100f }, 450d, 120d);
        using (ClientSession session = CreateSession(bridge, true))
        using (ClientSessionContext.Push(session))
        {
            Player.AttackMonster("Skeleton");
        }

        RequireCalls(bridge, "AttackMonster");
    }

    private static void AssertInvalidTargetCoordinatesDoNotMove()
    {
        FakeFlashBridge bridge = new FakeFlashBridge(GameEngineKind.Ruffle, new[] { 100f, 100f }, double.NaN, double.NaN);
        using (ClientSession session = CreateSession(bridge, false))
        using (ClientSessionContext.Push(session))
        {
            Player.AttackMonster("Skeleton");
        }

        RequireCalls(bridge, "AttackMonster", "Position");
    }

    private static ClientSession CreateSession(FakeFlashBridge bridge, bool infiniteRange)
    {
        ClientSession session = new ClientSession("Combat test", bridge);
        session.OptionState.InfiniteRange = infiniteRange;
        using (ClientSessionContext.Push(session))
            BotUtilities.ShouldUseSkill = true;
        bridge.Calls.Clear();
        return session;
    }

    private static void RequireCalls(FakeFlashBridge bridge, params string[] expected)
    {
        string[] actual = bridge.Calls.Select(call => call.Function).ToArray();
        if (!actual.SequenceEqual(expected))
        {
            throw new InvalidOperationException(
                "Expected calls " + string.Join(", ", expected)
                + " but received " + string.Join(", ", actual)
                + "; game-object reads were " + string.Join(", ", bridge.GameObjectRequests) + ".");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class RecordedCall
    {
        public RecordedCall(string function, object[] arguments)
        {
            Function = function;
            Arguments = arguments ?? new object[0];
        }

        public string Function { get; private set; }

        public object[] Arguments { get; private set; }
    }

    private sealed class FakeFlashBridge : IFlashBridge
    {
        private readonly float[] playerPosition;
        private readonly double targetX;
        private readonly double targetY;

        public FakeFlashBridge(GameEngineKind engine, float[] playerPosition, double targetX, double targetY)
        {
            Engine = engine;
            this.playerPosition = playerPosition;
            this.targetX = targetX;
            this.targetY = targetY;
            Calls = new List<RecordedCall>();
            GameObjectRequests = new List<string>();
        }

        public GameEngineKind Engine { get; private set; }

        public AxShockwaveFlash Control { get { return null; } }

        public Control HostControl { get { return null; } }

        public List<RecordedCall> Calls { get; private set; }

        public List<string> GameObjectRequests { get; private set; }

        public event Action<int> SwfLoadProgress;

        public event FlashBridgeCallHandler FlashCall;

        public event FlashBridgeErrorHandler FlashError;

        public void LoadMovie(byte[] swf)
        {
        }

        public T Call<T>(string function, params object[] args)
        {
            Record(function, args);
            if (function == "Position")
                return (T)(object)playerPosition;
            return default(T);
        }

        public object Call(string function, Type resultType, params object[] args)
        {
            Record(function, args);
            if (function == "Position")
                return playerPosition;
            return resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
        }

        public T CallWithStringArguments<T>(string function, params string[] args)
        {
            Record(function, args == null ? null : args.Cast<object>().ToArray());
            if (function == "Position")
                return (T)(object)playerPosition;
            return default(T);
        }

        public string CallString(string function, params object[] args)
        {
            Record(function, args);
            return string.Empty;
        }

        public string CallGameFunction(string path, params object[] args)
        {
            return string.Empty;
        }

        public string GetGameObject(string path)
        {
            return string.Empty;
        }

        public string GetGameObjectStatic(string path)
        {
            return string.Empty;
        }

        public T GetGameObject<T>(string path, T defaultValue = default(T))
        {
            GameObjectRequests.Add(path);
            double value;
            if (path == "world.myAvatar.target.pMC.x")
                value = targetX;
            else if (path == "world.myAvatar.target.pMC.y")
                value = targetY;
            else
                return defaultValue;
            if (typeof(T) == typeof(double))
                return (T)(object)value;
            return (T)Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
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

        private void Record(string function, object[] args)
        {
            Calls.Add(new RecordedCall(function, args));
        }
    }
}
