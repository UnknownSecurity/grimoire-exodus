using AxShockwaveFlashObjects;
using Grimoire.Clients;
using System;

internal static class ClientLimitSmoke
{
    [STAThread]
    private static int Main()
    {
        ClientSessionManager manager = new ClientSessionManager(6);
        ClientSession seventh = null;
        ClientSession eighth = null;
        try
        {
            for (int index = 1; index <= 6; index++)
                manager.Add(CreateSession(index));

            seventh = CreateSession(7);
            AssertRejected(manager, seventh, "The six-client safety limit was not enforced.");

            manager.SetMaximumSessions(int.MaxValue);
            manager.Add(seventh);
            seventh = null;
            AssertEqual(7, manager.Sessions.Count, "The unlimited override did not permit a seventh client.");

            manager.SetMaximumSessions(6);
            AssertEqual(7, manager.Sessions.Count, "Reducing the cap closed an existing client.");

            eighth = CreateSession(8);
            AssertRejected(
                manager,
                eighth,
                "Re-enabling the six-client cap allowed another client while the count was above the cap.");

            manager.Remove(manager.Sessions[0]);
            AssertEqual(6, manager.Sessions.Count, "Closing one excess client changed the wrong sessions.");
            AssertRejected(
                manager,
                eighth,
                "The manager allowed another client while the count was equal to the restored cap.");

            manager.Remove(manager.Sessions[0]);
            manager.Add(eighth);
            eighth = null;
            AssertEqual(6, manager.Sessions.Count, "The restored cap did not allow a client after the count fell below six.");

            Console.WriteLine("PASS: the configured six-client safety limit, unlimited override, and live-client preservation behave correctly.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            if (seventh != null)
                seventh.Dispose();
            if (eighth != null)
                eighth.Dispose();
            manager.Dispose();
        }
    }

    private static ClientSession CreateSession(int index)
    {
        return new ClientSession("Client " + index.ToString("00"), new FakeFlashBridge());
    }

    private static void AssertRejected(
        ClientSessionManager manager,
        ClientSession session,
        string message)
    {
        try
        {
            manager.Add(session);
        }
        catch (InvalidOperationException)
        {
            return;
        }
        throw new InvalidOperationException(message);
    }

    private static void AssertEqual(int expected, int actual, string message)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException(
                message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }

    private sealed class FakeFlashBridge : IFlashBridge
    {
        public AxShockwaveFlash Control
        {
            get { return null; }
        }

        public event Action<int> SwfLoadProgress;

        public event FlashBridgeCallHandler FlashCall;

        public event FlashBridgeErrorHandler FlashError;

        public T Call<T>(string function, params object[] args)
        {
            return default(T);
        }

        public object Call(string function, Type resultType, params object[] args)
        {
            return null;
        }

        public T CallWithStringArguments<T>(string function, params string[] args)
        {
            return default(T);
        }

        public string CallString(string function, params object[] args)
        {
            return null;
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
    }
}
