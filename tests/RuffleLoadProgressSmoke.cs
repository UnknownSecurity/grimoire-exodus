using Grimoire.Clients;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

internal static class RuffleLoadProgressSmoke
{
    private const int TimeoutMilliseconds = 60000;
    private const int ReadyProgress = 100;

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length != 1 || !File.Exists(args[0]))
        {
            Console.Error.WriteLine("A Grimoire SWF path is required.");
            return 2;
        }

        Form host = null;
        Panel workspace = null;
        RuffleFlashBridge bridge = null;
        ClientSession session = null;
        try
        {
            int progress = 0;
            string loadError = null;
            string failureStage = null;
            bool callbackProbeStarted = false;
            bool callbackProbeCompleted = false;
            host = new Form
            {
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Left = -32000,
                Top = -32000,
                Width = 960,
                Height = 620
            };
            bridge = new RuffleFlashBridge();
            session = new ClientSession("Ruffle load test", bridge);
            session.MarkInitializing();
            bridge.SwfLoadProgress += delegate(int value)
            {
                progress = value;
                Console.WriteLine("progress: " + value);
            };
            bridge.FlashError += delegate(
                AxShockwaveFlashObjects.AxShockwaveFlash flash,
                Exception exception,
                string function,
                object[] callbackArguments)
            {
                failureStage = function;
                loadError = exception == null ? "Unknown Ruffle failure." : exception.Message;
            };
            bridge.FlashCall += delegate(
                AxShockwaveFlashObjects.AxShockwaveFlash flash,
                string function,
                object[] callbackArguments)
            {
                if (!string.Equals(function, "debug", StringComparison.Ordinal)
                    || callbackArguments == null
                    || callbackArguments.Length == 0
                    || !string.Equals(
                        Convert.ToString(callbackArguments[0]),
                        "ruffle-reentry-probe",
                        StringComparison.Ordinal))
                {
                    return;
                }

                Stopwatch callbackTimer = Stopwatch.StartNew();
                bridge.Call<string>("Username");
                callbackTimer.Stop();
                if (callbackTimer.ElapsedMilliseconds >= 5000)
                {
                    failureStage = "Host callback re-entry";
                    loadError = "A deferred Ruffle callback could not call back into ActionScript without blocking.";
                    return;
                }
                callbackProbeCompleted = true;
            };

            Stopwatch timeout = Stopwatch.StartNew();
            Timer timer = new Timer { Interval = 50 };
            timer.Tick += delegate
            {
                if (progress >= ReadyProgress && !callbackProbeStarted)
                {
                    callbackProbeStarted = true;
                    WebView2 browser = bridge.HostControl as WebView2;
                    if (browser == null || browser.CoreWebView2 == null)
                    {
                        failureStage = "Host callback probe";
                        loadError = "The Ruffle browser was unavailable for the callback probe.";
                    }
                    else
                    {
                        browser.ExecuteScriptAsync(
                            "window.debug('ruffle-reentry-probe');");
                    }
                }

                if ((progress >= ReadyProgress && callbackProbeCompleted)
                    || loadError != null
                    || timeout.ElapsedMilliseconds >= TimeoutMilliseconds)
                    host.Close();
            };
            host.Shown += delegate
            {
                host.BeginInvoke(new Action(delegate
                {
                    try
                    {
                        bridge.LoadMovie(File.ReadAllBytes(args[0]));
                    }
                    catch (Exception exception)
                    {
                        failureStage = "Initialization";
                        loadError = exception.Message;
                        host.Close();
                    }
                }));
            };
            workspace = new Panel
            {
                BackColor = System.Drawing.Color.Black,
                Dock = DockStyle.Fill,
                Visible = ClientSurfacePolicy.ShouldRenderClientSurface(
                    true,
                    false,
                    false,
                    bridge.Engine == GameEngineKind.Ruffle
                        && session.State == ClientSessionState.Initializing)
            };
            workspace.Controls.Add(bridge.HostControl);
            host.Controls.Add(workspace);
            timer.Start();
            Application.Run(host);
            timer.Stop();
            timer.Dispose();

            if (loadError != null)
            {
                throw new InvalidOperationException(
                    "Ruffle load failure at " + (failureStage ?? "unknown") + ": " + loadError);
            }
            if (progress < ReadyProgress)
                throw new TimeoutException("The Ruffle AQW bootstrap did not reach 100% within 60 seconds.");
            if (!callbackProbeCompleted)
                throw new TimeoutException("The live Ruffle host callback re-entry probe did not complete within 60 seconds.");
            if (session.LoadProgress < ReadyProgress || !session.IsReady)
                throw new InvalidOperationException("The live Ruffle completion callback did not ready its client session.");

            Console.WriteLine("PASS: visible Ruffle initialization reached 100%, readied its session, and deferred host callback re-entry remained responsive.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            if (session != null)
                session.Dispose();
            else if (bridge != null)
                bridge.Dispose();
            if (workspace != null)
                workspace.Dispose();
            if (host != null)
                host.Dispose();
        }
    }
}
