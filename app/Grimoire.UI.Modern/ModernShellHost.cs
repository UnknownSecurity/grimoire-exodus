using Grimoire.Clients;
using Grimoire.Utils;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Grimoire.UI.Modern
{
    internal sealed class ModernShellHost : UserControl, IRuffleFrameHost
    {
        private const string VirtualHost = "grimoire.local";
        private const string ShellUri = "https://grimoire.local/Shell/index.html";
        private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(15);

        private readonly IModernUiApplication application;
        private readonly WebView2 webView;
        private readonly Timer stateTimer;
        private readonly TaskCompletionSource<bool> readyCompletion;
        private readonly Dictionary<string, PendingRuffleFrame> pendingRuffleFrames =
            new Dictionary<string, PendingRuffleFrame>(StringComparer.Ordinal);
        private readonly Dictionary<string, ModernRuffleFrame> activeRuffleFrames =
            new Dictionary<string, ModernRuffleFrame>(StringComparer.Ordinal);
        private readonly Queue<ModernUiRequest> pendingRequests = new Queue<ModernUiRequest>();
        private bool initialized;
        private bool uiReady;
        private bool disposed;
        private bool requestDispatchScheduled;
        private bool statePublicationScheduled;
        private bool forceStatePublication;
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

        public Control InvocationControl => webView;

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
            webView.CoreWebView2.FrameCreated += CoreWebView2_FrameCreated;
        }

        public Task<IRuffleFrame> AttachRuffleFrameAsync(
            string frameName,
            string sessionId,
            Uri source,
            object hostObject)
        {
            if (disposed || !initialized || !uiReady || webView.CoreWebView2 == null)
            {
                return Task.FromException<IRuffleFrame>(
                    new InvalidOperationException("The modern UI is not ready to host Ruffle."));
            }
            if (!IsSafeFrameName(frameName))
            {
                return Task.FromException<IRuffleFrame>(
                    new ArgumentException("The Ruffle frame identifier is invalid.", nameof(frameName)));
            }
            if (!Guid.TryParse(sessionId, out Guid ignoredSessionId))
            {
                return Task.FromException<IRuffleFrame>(
                    new ArgumentException("The Ruffle client session identifier is invalid.", nameof(sessionId)));
            }
            if (!IsTrustedRuffleDocument(source))
            {
                return Task.FromException<IRuffleFrame>(
                    new InvalidOperationException("The Ruffle frame source is not a trusted loopback document."));
            }
            if (hostObject == null)
            {
                return Task.FromException<IRuffleFrame>(
                    new ArgumentNullException(nameof(hostObject)));
            }
            if (pendingRuffleFrames.ContainsKey(frameName) || activeRuffleFrames.ContainsKey(frameName))
            {
                return Task.FromException<IRuffleFrame>(
                    new InvalidOperationException("That Ruffle frame is already attached."));
            }

            PendingRuffleFrame pending = new PendingRuffleFrame(
                frameName,
                sessionId,
                source,
                hostObject);
            pendingRuffleFrames.Add(frameName, pending);
            PostRuffleEvent("engine.ruffle.mount", frameName, sessionId, source.AbsoluteUri);
            return pending.Completion.Task;
        }

        public void DetachRuffleFrame(string frameName)
        {
            if (string.IsNullOrWhiteSpace(frameName))
                return;

            if (pendingRuffleFrames.TryGetValue(frameName, out PendingRuffleFrame pending))
            {
                pendingRuffleFrames.Remove(frameName);
                pending.Completion.TrySetCanceled();
            }
            if (activeRuffleFrames.TryGetValue(frameName, out ModernRuffleFrame active))
            {
                activeRuffleFrames.Remove(frameName);
                active.Detach();
            }
            PostRuffleEvent("engine.ruffle.unmount", frameName, null, null);
        }

        private void CoreWebView2_FrameCreated(object sender, CoreWebView2FrameCreatedEventArgs e)
        {
            CoreWebView2Frame frame = e.Frame;
            if (TryBindRuffleFrame(frame))
                return;

            EventHandler<object> nameChanged = null;
            nameChanged = delegate
            {
                if (!TryBindRuffleFrame(frame) && frame.IsDestroyed() == 0)
                    return;
                frame.NameChanged -= nameChanged;
            };
            frame.NameChanged += nameChanged;
        }

        private bool TryBindRuffleFrame(CoreWebView2Frame frame)
        {
            PendingRuffleFrame pending;
            if (frame == null
                || frame.IsDestroyed() != 0
                || string.IsNullOrWhiteSpace(frame.Name)
                || !pendingRuffleFrames.TryGetValue(frame.Name, out pending))
            {
                return false;
            }

            pendingRuffleFrames.Remove(frame.Name);
            try
            {
                frame.AddHostObjectToScript(
                    "grimoire",
                    pending.HostObject,
                    new[] { pending.Source.GetLeftPart(UriPartial.Authority) });
                ModernRuffleFrame binding = new ModernRuffleFrame(
                    this,
                    pending.FrameName,
                    pending.Source,
                    frame);
                activeRuffleFrames.Add(pending.FrameName, binding);
                pending.Completion.TrySetResult(binding);
            }
            catch (Exception exception)
            {
                pending.Completion.TrySetException(exception);
                PostRuffleEvent("engine.ruffle.unmount", pending.FrameName, null, null);
            }
            return true;
        }

        private static bool IsSafeFrameName(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 96)
                return false;
            foreach (char character in value)
            {
                if (!char.IsLetterOrDigit(character) && character != '-' && character != '_')
                    return false;
            }
            return true;
        }

        private static bool IsTrustedRuffleDocument(Uri source)
        {
            if (source == null
                || !source.IsAbsoluteUri
                || !string.Equals(source.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(source.Host, "127.0.0.1", StringComparison.Ordinal)
                || source.IsDefaultPort
                || !string.IsNullOrEmpty(source.Query)
                || !string.IsNullOrEmpty(source.Fragment)
                || !source.AbsolutePath.EndsWith("/index.html", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string[] segments = source.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            return segments.Length == 2
                && segments[0].Length >= 16
                && segments[0].Length <= 128
                && string.Equals(segments[1], "index.html", StringComparison.OrdinalIgnoreCase);
        }

        private void PostRuffleEvent(
            string eventName,
            string frameName,
            string sessionId,
            string source)
        {
            if (disposed || !uiReady)
                return;

            JObject payload = new JObject
            {
                ["name"] = frameName
            };
            if (!string.IsNullOrWhiteSpace(sessionId))
                payload["sessionId"] = sessionId;
            if (!string.IsNullOrWhiteSpace(source))
                payload["source"] = source;
            Post(ModernUiProtocol.CreateEvent(eventName, ++eventSequence, payload));
        }

        private void HandleRuffleFrameDestroyed(ModernRuffleFrame binding)
        {
            if (binding == null)
                return;
            if (activeRuffleFrames.TryGetValue(binding.FrameName, out ModernRuffleFrame current)
                && ReferenceEquals(current, binding))
            {
                activeRuffleFrames.Remove(binding.FrameName);
            }
            PostRuffleEvent("engine.ruffle.unmount", binding.FrameName, null, null);
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

            pendingRequests.Enqueue(request);
            ScheduleRequestDispatch();
        }

        private void ScheduleRequestDispatch()
        {
            if (requestDispatchScheduled || disposed || !webView.IsHandleCreated)
                return;

            requestDispatchScheduled = true;
            webView.BeginInvoke(new Action(DispatchNextRequest));
        }

        private void DispatchNextRequest()
        {
            if (disposed || pendingRequests.Count == 0)
            {
                requestDispatchScheduled = false;
                return;
            }

            ModernUiRequest request = pendingRequests.Dequeue();
            ProcessRequest(request);

            if (disposed || pendingRequests.Count == 0)
            {
                requestDispatchScheduled = false;
                return;
            }

            webView.BeginInvoke(new Action(DispatchNextRequest));
        }

        private void ProcessRequest(ModernUiRequest request)
        {
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
                ScheduleStatePublication(request.Method == "ui.ready");
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

        private void ScheduleStatePublication(bool force)
        {
            forceStatePublication |= force;
            if (statePublicationScheduled || disposed || !webView.IsHandleCreated)
                return;

            statePublicationScheduled = true;
            webView.BeginInvoke(new Action(delegate
            {
                bool publishForce = forceStatePublication;
                forceStatePublication = false;
                statePublicationScheduled = false;
                PublishState(publishForce);
            }));
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
                foreach (string frameName in new List<string>(activeRuffleFrames.Keys))
                    DetachRuffleFrame(frameName);
                foreach (PendingRuffleFrame pending in pendingRuffleFrames.Values)
                    pending.Completion.TrySetCanceled();
                pendingRuffleFrames.Clear();
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
                    webView.CoreWebView2.FrameCreated -= CoreWebView2_FrameCreated;
                }
                webView.Dispose();
            }
            base.Dispose(disposing);
        }

        private sealed class PendingRuffleFrame
        {
            public PendingRuffleFrame(
                string frameName,
                string sessionId,
                Uri source,
                object hostObject)
            {
                FrameName = frameName;
                SessionId = sessionId;
                Source = source;
                HostObject = hostObject;
                Completion = new TaskCompletionSource<IRuffleFrame>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public string FrameName { get; }

            public string SessionId { get; }

            public Uri Source { get; }

            public object HostObject { get; }

            public TaskCompletionSource<IRuffleFrame> Completion { get; }
        }

        private sealed class ModernRuffleFrame : IRuffleFrame
        {
            private readonly ModernShellHost owner;
            private readonly Uri source;
            private CoreWebView2Frame frame;
            private bool disposed;

            public ModernRuffleFrame(
                ModernShellHost owner,
                string frameName,
                Uri source,
                CoreWebView2Frame frame)
            {
                this.owner = owner;
                FrameName = frameName;
                this.source = source;
                this.frame = frame;
                frame.NavigationStarting += Frame_NavigationStarting;
                frame.NavigationCompleted += Frame_NavigationCompleted;
                frame.Destroyed += Frame_Destroyed;
            }

            public string FrameName { get; }

            public event Action<Exception> Failed;

            public Task<string> ExecuteScriptAsync(string script)
            {
                if (disposed || frame == null || frame.IsDestroyed() != 0)
                {
                    return Task.FromException<string>(
                        new InvalidOperationException("The Ruffle frame is no longer available."));
                }
                return frame.ExecuteScriptAsync(script);
            }

            private void Frame_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
            {
                if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out Uri candidate)
                    || !string.Equals(candidate.AbsoluteUri, source.AbsoluteUri, StringComparison.Ordinal))
                {
                    e.Cancel = true;
                }
            }

            private void Frame_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
            {
                if (!e.IsSuccess)
                {
                    Failed?.Invoke(new InvalidOperationException(
                        "The embedded Ruffle page could not be loaded."));
                }
            }

            private void Frame_Destroyed(object sender, object e)
            {
                if (disposed)
                    return;
                disposed = true;
                frame = null;
                owner.HandleRuffleFrameDestroyed(this);
                Failed?.Invoke(new InvalidOperationException(
                    "The embedded Ruffle frame stopped unexpectedly."));
            }

            public void Dispose()
            {
                owner.DetachRuffleFrame(FrameName);
            }

            internal void Detach()
            {
                if (disposed)
                    return;
                disposed = true;
                CoreWebView2Frame current = frame;
                frame = null;
                if (current == null || current.IsDestroyed() != 0)
                    return;
                current.NavigationStarting -= Frame_NavigationStarting;
                current.NavigationCompleted -= Frame_NavigationCompleted;
                current.Destroyed -= Frame_Destroyed;
                try
                {
                    current.RemoveHostObjectFromScript("grimoire");
                }
                catch
                {
                }
            }
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
