using AxShockwaveFlashObjects;
using Grimoire.Botting;
using Grimoire.Clients;
using Grimoire.Game;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

internal static class MultiClientBotIsolationSmoke
{
    [STAThread]
    private static int Main()
    {
        try
        {
            Run();
            Console.WriteLine("PASS: concurrent bot runs retain their owning client through active-session switches and independent stops.");
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
        ClientSessionManager manager = new ClientSessionManager(2, true);
        ClientSession first = new ClientSession("Client 01", new FakeFlashBridge("first"), new Bot());
        ClientSession second = new ClientSession("Client 02", new FakeFlashBridge("second"), new Bot());
        try
        {
            manager.Add(first);
            manager.Add(second);
            PrepareLoggedInClient(first);
            PrepareLoggedInClient(second);

            RecordingCommand firstCommand = new RecordingCommand(first, "first");
            RecordingCommand secondCommand = new RecordingCommand(second, "second");
            Configuration firstConfiguration = CreateConfiguration("first configuration", firstCommand);
            Configuration secondConfiguration = CreateConfiguration("second configuration", secondCommand);

            using (ClientSessionContext.Push(first))
                first.BotEngine.Start(firstConfiguration);
            using (ClientSessionContext.Push(second))
                second.BotEngine.Start(secondConfiguration);

            WaitFor(firstCommand.FirstExecution, "The first bot never executed its command.");
            WaitFor(secondCommand.FirstExecution, "The second bot never executed its command.");

            for (int i = 0; i < 20; i++)
                manager.SetActive((i & 1) == 0 ? first : second);

            WaitFor(firstCommand.ExecutionAfterAwait, "The first bot did not retain its client across an await.");
            WaitFor(secondCommand.ExecutionAfterAwait, "The second bot did not retain its client across an await.");
            firstCommand.ThrowIfFailed();
            secondCommand.ThrowIfFailed();

            if (!ReferenceEquals(firstConfiguration, first.BotEngine.Configuration)
                || !ReferenceEquals(secondConfiguration, second.BotEngine.Configuration))
            {
                throw new InvalidOperationException("A bot run adopted another client's configuration.");
            }

            using (ClientSessionContext.Push(first))
                first.BotEngine.Stop();
            WaitUntilStopped(first.BotEngine, "The first bot did not stop within the bounded test window.");
            if (!second.BotEngine.IsRunning)
                throw new InvalidOperationException("Stopping the first bot also stopped the second bot.");

            int secondCountBefore = secondCommand.ExecutionCount;
            WaitUntil(
                () => secondCommand.ExecutionCount > secondCountBefore,
                "The second bot stopped making progress when the first bot stopped.");

            using (ClientSessionContext.Push(second))
                second.BotEngine.Stop();
            WaitUntilStopped(second.BotEngine, "The second bot did not stop within the bounded test window.");
            firstCommand.ThrowIfFailed();
            secondCommand.ThrowIfFailed();
        }
        finally
        {
            if (first.BotEngine.IsRunning)
            {
                using (ClientSessionContext.Push(first))
                    first.BotEngine.Stop();
                WaitUntilStopped(first.BotEngine, "The first bot could not be stopped during cleanup.");
            }
            if (second.BotEngine.IsRunning)
            {
                using (ClientSessionContext.Push(second))
                    second.BotEngine.Stop();
                WaitUntilStopped(second.BotEngine, "The second bot could not be stopped during cleanup.");
            }
            manager.Dispose();
        }
    }

    private static Configuration CreateConfiguration(string author, IBotCommand command)
    {
        return new Configuration
        {
            Author = author,
            BotDelay = 1,
            Commands = new List<IBotCommand> { command }
        };
    }

    private static void PrepareLoggedInClient(ClientSession session)
    {
        using (ClientSessionContext.Push(session))
        {
            Player.IsLoggedIn = true;
            BotUtilities.ShouldUseSkill = true;
        }
    }

    private static void WaitFor(Task task, string failureMessage)
    {
        if (Task.WhenAny(task, Task.Delay(5000)).GetAwaiter().GetResult() != task)
            throw new InvalidOperationException(failureMessage);
        task.GetAwaiter().GetResult();
    }

    private static void WaitUntilStopped(IBotEngine engine, string failureMessage)
    {
        WaitUntil(() => !engine.IsRunning, failureMessage);
    }

    private static void WaitUntil(Func<bool> condition, string failureMessage)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline)
            Thread.Sleep(10);
        if (!condition())
            throw new InvalidOperationException(failureMessage);
    }

    private sealed class RecordingCommand : IBotCommand
    {
        private readonly ClientSession expectedSession;
        private readonly string expectedIdentity;
        private readonly TaskCompletionSource<bool> firstExecution = new TaskCompletionSource<bool>();
        private readonly TaskCompletionSource<bool> executionAfterAwait = new TaskCompletionSource<bool>();
        private Exception failure;
        private int executionCount;

        public RecordingCommand(ClientSession expectedSession, string expectedIdentity)
        {
            this.expectedSession = expectedSession;
            this.expectedIdentity = expectedIdentity;
        }

        public Task FirstExecution
        {
            get { return firstExecution.Task; }
        }

        public Task ExecutionAfterAwait
        {
            get { return executionAfterAwait.Task; }
        }

        public int ExecutionCount
        {
            get { return Volatile.Read(ref executionCount); }
        }

        public async Task Execute(IBotEngine instance)
        {
            try
            {
                AssertOwner(instance, "before await");
                Interlocked.Increment(ref executionCount);
                firstExecution.TrySetResult(true);
                await Task.Delay(5);
                AssertOwner(instance, "after await");
                executionAfterAwait.TrySetResult(true);
            }
            catch (Exception exception)
            {
                Interlocked.CompareExchange(ref failure, exception, null);
                firstExecution.TrySetException(exception);
                executionAfterAwait.TrySetException(exception);
                throw;
            }
        }

        public void ThrowIfFailed()
        {
            Exception exception = Volatile.Read(ref failure);
            if (exception != null)
                throw new InvalidOperationException("A concurrent bot run crossed client ownership.", exception);
        }

        private void AssertOwner(IBotEngine instance, string stage)
        {
            if (!ReferenceEquals(ClientSessionContext.Current, expectedSession))
                throw new InvalidOperationException("The command lost its owning session " + stage + ".");
            if (!ReferenceEquals(instance, expectedSession.BotEngine))
                throw new InvalidOperationException("The command received another client's bot engine " + stage + ".");
            string identity = expectedSession.FlashBridge.Call<string>("identity");
            if (!string.Equals(identity, expectedIdentity, StringComparison.Ordinal))
                throw new InvalidOperationException("The command reached another client's Flash bridge " + stage + ".");
        }

        public override string ToString()
        {
            return "Record session ownership";
        }
    }

    private sealed class FakeFlashBridge : IFlashBridge
    {
        private readonly string identity;

        public FakeFlashBridge(string identity)
        {
            this.identity = identity;
        }

        public AxShockwaveFlash Control
        {
            get { return null; }
        }

        public event Action<int> SwfLoadProgress;

        public event FlashBridgeCallHandler FlashCall;

        public event FlashBridgeErrorHandler FlashError;

        public T Call<T>(string function, params object[] args)
        {
            object value = null;
            if (typeof(T) == typeof(string))
                value = identity;
            else if (typeof(T) == typeof(int))
                value = 100;
            else if (typeof(T) == typeof(bool))
                value = false;
            return value == null ? default(T) : (T)value;
        }

        public object Call(string function, Type resultType, params object[] args)
        {
            if (resultType == typeof(string))
                return identity;
            if (resultType == typeof(int))
                return 100;
            if (resultType == typeof(bool))
                return false;
            return null;
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
            SwfLoadProgress = null;
            FlashCall = null;
            FlashError = null;
        }
    }
}
