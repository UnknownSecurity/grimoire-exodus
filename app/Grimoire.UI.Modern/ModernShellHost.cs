using Grimoire.Utils;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Grimoire.UI.Modern
{
    internal sealed class ModernShellHost : UserControl
    {
        private const string VirtualHost = "grimoire.local";
        private const string ShellUri = "https://grimoire.local/Shell/index.html";
        private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(15);

        private readonly IModernUiApplication application;
        private readonly WebView2 webView;
        private readonly Timer stateTimer;
        private readonly TaskCompletionSource<bool> readyCompletion;
        private bool initialized;
        private bool uiReady;
        private bool disposed;
        private long eventSequence;
        private string lastStateJson;

        public ModernShellHost(IModernUiApplication application)
        {
            this.application = application ?? throw new ArgumentNullException(nameof(application));
            readyCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            BackColor = Color.FromArgb(9, 10, 12);
            Dock = DockStyle.Fill;
            Name = "modernShellHost";
            TabStop = false;

            webView = new WebView2
            {
                BackColor = Color.FromArgb(9, 10, 12),
                DefaultBackgroundColor = Color.FromArgb(9, 10, 12),
                Dock = DockStyle.Fill,
                Name = "modernShellWebView",
                TabIndex = 0
            };
            Controls.Add(webView);

            stateTimer = new Timer { Interval = 500 };
            stateTimer.Tick += StateTimer_Tick;
        }

        public event Action<ModernGameViewport> GameViewportChanged;

        public event Action<Exception> ShellFailed;

        public async Task InitializeAsync()
        {
            if (initialized)
                return;

            string shellPath = Path.Combine(AppPaths.ApplicationDirectory, "Resources", "Shell", "index.html");
            string resourcesPath = Path.Combine(AppPaths.ApplicationDirectory, "Resources");
            if (!File.Exists(shellPath))
                throw new FileNotFoundException("The modern UI shell is not installed.", shellPath);

            Directory.CreateDirectory(AppPaths.ModernUiWebViewDataDirectory);
            CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(
                null,
                AppPaths.ModernUiWebViewDataDirectory);
            await webView.EnsureCoreWebView2Async(environment);
            ConfigureBrowser(resourcesPath);
            initialized = true;
            webView.CoreWebView2.Navigate(ShellUri);
            await WaitForShellReadyAsync();
            stateTimer.Start();
        }

        private async Task WaitForShellReadyAsync()
        {
            Task completed = await Task.WhenAny(readyCompletion.Task, Task.Delay(StartupTimeout));
            if (!ReferenceEquals(completed, readyCompletion.Task))
                throw new TimeoutException("The modern UI did not become ready in time.");

            await readyCompletion.Task;
        }

        public void RequestStatePublication()
        {
            if (!uiReady || disposed)
                return;
            PublishState(false);
        }

        private void ConfigureBrowser(string resourcesPath)
        {
            CoreWebView2Settings settings = webView.CoreWebView2.Settings;
            settings.AreBrowserAcceleratorKeysEnabled = false;
            settings.AreDefaultContextMenusEnabled = false;
            settings.AreDevToolsEnabled = false;
            settings.IsBuiltInErrorPageEnabled = false;
            settings.IsStatusBarEnabled = false;
            settings.IsZoomControlEnabled = false;

            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                VirtualHost,
                resourcesPath,
                CoreWebView2HostResourceAccessKind.DenyCors);
            webView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            webView.CoreWebView2.PermissionRequested += CoreWebView2_PermissionRequested;
            webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            webView.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed;
        }

        private static bool IsTrustedDocument(string uri)
        {
            return Uri.TryCreate(uri, UriKind.Absolute, out Uri candidate)
                && string.Equals(candidate.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                && string.Equals(candidate.Host, VirtualHost, StringComparison.OrdinalIgnoreCase)
                && candidate.IsDefaultPort
                && string.Equals(candidate.AbsolutePath, "/Shell/index.html", StringComparison.Ordinal)
                && string.IsNullOrEmpty(candidate.Query);
        }

        private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (!IsTrustedDocument(e.Uri))
                e.Cancel = true;
        }

        private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess && IsTrustedDocument(webView.Source?.AbsoluteUri))
                return;

            RaiseFailure(new InvalidOperationException("The local modern UI page could not be loaded."));
        }

        private static void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
        }

        private static void CoreWebView2_PermissionRequested(object sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            e.State = CoreWebView2PermissionState.Deny;
        }

        private void CoreWebView2_ProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs e)
        {
            if (e.ProcessFailedKind == CoreWebView2ProcessFailedKind.BrowserProcessExited
                || e.ProcessFailedKind == CoreWebView2ProcessFailedKind.RenderProcessExited
                || e.ProcessFailedKind == CoreWebView2ProcessFailedKind.FrameRenderProcessExited)
            {
                RaiseFailure(new InvalidOperationException(
                    "The modern UI browser process stopped unexpectedly (" + e.ProcessFailedKind + ")."));
            }
        }

        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string json = e.WebMessageAsJson;
            if (!ModernUiProtocol.TryParseRequest(json, out ModernUiRequest request, out JObject error))
            {
                Post(error);
                return;
            }

            try
            {
                JToken result;
                switch (request.Method)
                {
                    case "ui.ready":
                        uiReady = true;
                        readyCompletion.TrySetResult(true);
                        result = new JObject { ["ready"] = true };
                        break;
                    case "ui.setGameViewport":
                        if (!ModernUiProtocol.TryParseViewport(request.Parameters, out ModernGameViewport viewport))
                        {
                            Post(ModernUiProtocol.CreateError(
                                request.Id,
                                "INVALID_VIEWPORT",
                                "The native game viewport is invalid."));
                            return;
                        }
                        GameViewportChanged?.Invoke(viewport);
                        result = new JObject { ["accepted"] = true };
                        break;
                    default:
                        result = application.Invoke(request.Method, request.Parameters);
                        break;
                }

                Post(ModernUiProtocol.CreateSuccess(request.Id, result));
                if (request.Method == "ui.ready")
                    PublishState(true);
                else
                    PublishState(false);
            }
            catch (ModernUiActionException exception)
            {
                Post(ModernUiProtocol.CreateError(
                    request.Id,
                    exception.Code,
                    exception.Message,
                    exception.Retryable));
            }
            catch (Exception exception)
            {
                StartupDiagnostics.RecordException("Modern UI action / " + request.Method, exception, false);
                Post(ModernUiProtocol.CreateError(
                    request.Id,
                    "ACTION_FAILED",
                    "Grimoire could not complete that action."));
            }
        }

        private void StateTimer_Tick(object sender, EventArgs e)
        {
            PublishState(false);
        }

        private void PublishState(bool force)
        {
            if (!initialized || !uiReady || disposed || webView.CoreWebView2 == null)
                return;

            try
            {
                JObject state = application.GetBootstrap();
                string stateJson = state.ToString(Formatting.None);
                if (!force && string.Equals(lastStateJson, stateJson, StringComparison.Ordinal))
                    return;

                lastStateJson = stateJson;
                Post(ModernUiProtocol.CreateEvent("app.stateChanged", ++eventSequence, state));
            }
            catch (Exception exception)
            {
                StartupDiagnostics.RecordException("Modern UI state publication", exception, false);
            }
        }

        private void Post(JObject message)
        {
            if (message == null || disposed || webView.CoreWebView2 == null)
                return;
            webView.CoreWebView2.PostWebMessageAsJson(message.ToString(Formatting.None));
        }

        private void RaiseFailure(Exception exception)
        {
            if (disposed)
                return;
            stateTimer.Stop();
            if (!uiReady && readyCompletion.TrySetException(exception))
                return;
            ShellFailed?.Invoke(exception);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                disposed = true;
                readyCompletion.TrySetCanceled();
                stateTimer.Stop();
                stateTimer.Tick -= StateTimer_Tick;
                stateTimer.Dispose();
                if (webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.NavigationStarting -= CoreWebView2_NavigationStarting;
                    webView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
                    webView.CoreWebView2.NewWindowRequested -= CoreWebView2_NewWindowRequested;
                    webView.CoreWebView2.PermissionRequested -= CoreWebView2_PermissionRequested;
                    webView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
                    webView.CoreWebView2.ProcessFailed -= CoreWebView2_ProcessFailed;
                }
                webView.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal sealed class ModernUiActionException : Exception
    {
        public ModernUiActionException(string code, string message, bool retryable = false)
            : base(message)
        {
            Code = code;
            Retryable = retryable;
        }

        public string Code { get; }

        public bool Retryable { get; }
    }
}
