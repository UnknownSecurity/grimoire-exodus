using AxShockwaveFlashObjects;
using Grimoire.Clients;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

internal static class RuffleBridgeResponsivenessSmoke
{
    private const int CallbackCount = 24;
    private const int CallbackCostMilliseconds = 1;
    private const int MaximumHeartbeatDelayMilliseconds = 100;
    private const int TestTimeoutMilliseconds = 5000;

    [STAThread]
    private static int Main()
    {
        RuffleFlashBridge bridge = null;
        Form host = null;
        try
        {
            Application.EnableVisualStyles();
            host = new Form
            {
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Left = -32000,
                Top = -32000,
                Width = 320,
                Height = 240
            };
            bridge = new RuffleFlashBridge();
            host.Controls.Add(bridge.HostControl);
            host.Show();

            int processed = 0;
            Stopwatch timer = new Stopwatch();
            long heartbeatDelay = -1;
            bridge.FlashCall += delegate(AxShockwaveFlash flash, string function, object[] arguments)
            {
                if (processed == 0)
                {
                    timer.Start();
                    host.BeginInvoke(new Action(delegate
                    {
                        heartbeatDelay = timer.ElapsedMilliseconds;
                    }));
                }
                int sequence = Convert.ToInt32(arguments[0]);
                if (sequence != processed)
                    throw new InvalidOperationException("The Ruffle callback queue changed packet order.");
                Thread.Sleep(CallbackCostMilliseconds);
                processed++;
            };

            MethodInfo dispatch = typeof(RuffleFlashBridge).GetMethod(
                "Dispatch",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (dispatch == null)
                throw new InvalidOperationException("The Ruffle host callback entry point was not found.");

            for (int index = 0; index < CallbackCount; index++)
                dispatch.Invoke(bridge, new object[] { "packetFromServer", "[" + index + "]" });

            Stopwatch testTimer = Stopwatch.StartNew();
            while (heartbeatDelay < 0 && testTimer.ElapsedMilliseconds < TestTimeoutMilliseconds)
            {
                Application.DoEvents();
                Thread.Sleep(1);
            }

            if (heartbeatDelay < 0)
                throw new TimeoutException("The UI heartbeat was never processed.");
            if (heartbeatDelay > MaximumHeartbeatDelayMilliseconds)
            {
                throw new InvalidOperationException(
                    "The Ruffle callback burst starved the UI thread for "
                    + heartbeatDelay
                    + " ms; the maximum responsive interval is "
                    + MaximumHeartbeatDelayMilliseconds
                    + " ms.");
            }
            if (processed <= 0 || processed >= CallbackCount)
            {
                throw new InvalidOperationException(
                    "The responsiveness probe did not observe a time-sliced callback drain.");
            }

            Stopwatch completionTimer = Stopwatch.StartNew();
            while (processed < CallbackCount && completionTimer.ElapsedMilliseconds < TestTimeoutMilliseconds)
            {
                Application.DoEvents();
                Thread.Sleep(1);
            }
            if (processed != CallbackCount)
                throw new InvalidOperationException("The Ruffle callback queue did not preserve every callback.");

            Console.WriteLine(
                "PASS: Ruffle callback bursts preserve UI responsiveness and ordered delivery (heartbeat "
                + heartbeatDelay
                + " ms)." );
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            if (bridge != null)
                bridge.Dispose();
            if (host != null)
                host.Dispose();
        }
    }
}
