using AxShockwaveFlashObjects;
using Grimoire.Botting;
using Grimoire.Clients;
using Grimoire.Networking.Handlers;
using Grimoire.UI;
using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;

internal static class PingMetricSmoke
{
    [STAThread]
    private static int Main()
    {
        ClientSession session = null;
        Root root = null;
        FieldInfo instanceField = null;
        object previousRoot = null;
        try
        {
            FakeFlashBridge bridge = new FakeFlashBridge();
            session = new ClientSession("Ping metric", bridge, new Bot());

            instanceField = typeof(Root).GetField(
                "<Instance>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (instanceField == null)
                throw new InvalidOperationException("The Root instance backing field could not be located.");

            previousRoot = instanceField.GetValue(null);
            root = (Root)FormatterServices.GetUninitializedObject(typeof(Root));
            root.rtbPing = new System.Windows.Forms.RichTextBox { Visible = false };
            instanceField.SetValue(null, root);

            using (ClientSessionContext.Push(session))
                new HandlerLogin().InstallSettings();

            if (bridge.LatencyTimerStartCount != 1)
            {
                throw new InvalidOperationException(
                    "Latency monitoring did not start when the legacy toolbar ping monitor was hidden. "
                    + "Expected: 1; actual: " + bridge.LatencyTimerStartCount + ".");
            }

            Console.WriteLine("PASS: bottom-panel latency monitoring starts independently of the legacy toolbar monitor.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            if (instanceField != null)
                instanceField.SetValue(null, previousRoot);
            if (root != null && root.rtbPing != null)
                root.rtbPing.Dispose();
            if (session != null)
                session.Dispose();
        }
    }

    private sealed class FakeFlashBridge : IFlashBridge
    {
        private int latencyTimerStartCount;

        public AxShockwaveFlash Control { get { return null; } }

        public GameEngineKind Engine { get { return GameEngineKind.FlashActiveX; } }

        public System.Windows.Forms.Control HostControl { get { return null; } }

        public bool Disposed { get; private set; }

        public int LatencyTimerStartCount
        {
            get { return Volatile.Read(ref latencyTimerStartCount); }
        }

        public event Action<int> SwfLoadProgress;

        public event FlashBridgeCallHandler FlashCall;

        public event FlashBridgeErrorHandler FlashError;

        public void LoadMovie(byte[] swf)
        {
        }

        public T Call<T>(string function, params object[] args)
        {
            if (function == "StartLatencyTimer")
                Interlocked.Increment(ref latencyTimerStartCount);
            return default(T);
        }

        public object Call(string function, Type resultType, params object[] args)
        {
            return null;
        }

        public T CallWithStringArguments<T>(string function, params string[] args)
        {
            return Call<T>(function, args);
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
            Disposed = true;
            SwfLoadProgress = null;
            FlashCall = null;
            FlashError = null;
        }
    }
}
