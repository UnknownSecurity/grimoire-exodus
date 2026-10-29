using AxShockwaveFlashObjects;
using Grimoire.Networking;
using Grimoire.Tools;
using Grimoire.Utils;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Grimoire.Clients
{
    /// <summary>
    /// Ruffle/WebView2 implementation of the legacy synchronous game bridge.
    /// Every browser, loopback adapter, socket map, and callback stays owned by
    /// one ClientSession.
    /// </summary>
    public sealed class RuffleFlashBridge : IFlashBridge
    {
        private const int ScriptTimeoutMilliseconds = 15000;
        private const int MaximumBridgeFunctionCharacters = 128;
        private const int MaximumBridgeMessageCharacters = 4 * 1024 * 1024;
        private const int MaximumQueuedHostCallbacks = 4096;

        private readonly WebView2 webView;
        private readonly int ownerThreadId;
        private readonly RuffleServerMap serverMap = new RuffleServerMap();
        private readonly RuffleHostObject hostObject;
        private readonly string profileDirectory;
        private readonly object callbackSync = new object();
        private readonly Queue<QueuedHostCallback> callbackQueue = new Queue<QueuedHostCallback>();
        private Uri trustedDocumentUri;
        private ClientSession session;
        private RuffleLocalHost localHost;
        private bool navigationReady;
        private bool scriptInvocationInProgress;
        private bool callbackDrainScheduled;
        private bool disposed;

        public RuffleFlashBridge()
        {
            ownerThreadId = Thread.CurrentThread.ManagedThreadId;
            profileDirectory = Path.Combine(AppPaths.RuffleWebViewDataDirectory, Guid.NewGuid().ToString("N"));
            webView = new WebView2
            {
                BackColor = System.Drawing.Color.Black,
                DefaultBackgroundColor = System.Drawing.Color.Black,
                Dock = DockStyle.Fill,
                TabIndex = 0
            };
            hostObject = new RuffleHostObject(this);
        }

        public GameEngineKind Engine => GameEngineKind.Ruffle;

        public AxShockwaveFlash Control => null;

        public Control HostControl => webView;

        public event Action<int> SwfLoadProgress;

        public event FlashBridgeCallHandler FlashCall;

        public event FlashBridgeErrorHandler FlashError;

        internal void BindSession(ClientSession value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (session != null && !ReferenceEquals(session, value))
                throw new InvalidOperationException("A Ruffle bridge cannot be moved to another client session.");
            session = value;
        }

        public void LoadMovie(byte[] swf)
        {
            if (swf == null || swf.Length == 0)
                throw new ArgumentException("The AQW client SWF is empty.", nameof(swf));
            ThrowIfDisposed();
            if (Thread.CurrentThread.ManagedThreadId != ownerThreadId)
                throw new InvalidOperationException("Ruffle must be initialized on its owning UI thread.");

            string runtimeDirectory = Path.Combine(AppPaths.ApplicationDirectory, "Ruffle");
            string pagePath = Path.Combine(AppPaths.ApplicationDirectory, "Resources", "Ruffle", "index.html");
            string page = File.ReadAllText(pagePath);
            localHost = new RuffleLocalHost(runtimeDirectory, swf, page, serverMap);
            trustedDocumentUri = new Uri(localHost.BaseUrl + "index.html");

            Directory.CreateDirectory(profileDirectory);
            CoreWebView2Environment environment = WaitForUiTask(
                CoreWebView2Environment.CreateAsync(null, profileDirectory),
                ScriptTimeoutMilliseconds);
            WaitForUiTask(webView.EnsureCoreWebView2Async(environment), ScriptTimeoutMilliseconds);
            ConfigureBrowser();
            webView.CoreWebView2.AddHostObjectToScript("grimoire", hostObject);
            webView.NavigationCompleted += WebView_NavigationCompleted;
            webView.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed;
            webView.CoreWebView2.Navigate(localHost.BaseUrl + "index.html");
        }

        private void ConfigureBrowser()
        {
            CoreWebView2Settings settings = webView.CoreWebView2.Settings;
            settings.AreBrowserAcceleratorKeysEnabled = false;
            settings.AreDefaultContextMenusEnabled = false;
            settings.AreDevToolsEnabled = false;
            settings.IsBuiltInErrorPageEnabled = false;
            settings.IsStatusBarEnabled = false;
            settings.IsZoomControlEnabled = false;
            webView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            webView.CoreWebView2.NewWindowRequested += delegate(object sender, CoreWebView2NewWindowRequestedEventArgs e)
            {
                e.Handled = true;
            };
            webView.CoreWebView2.PermissionRequested += delegate(object sender, CoreWebView2PermissionRequestedEventArgs e)
            {
                e.State = CoreWebView2PermissionState.Deny;
            };
        }

        private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (!IsTrustedTopLevelDocument(e.Uri))
                e.Cancel = true;
        }

        private bool IsTrustedTopLevelDocument(string value)
        {
            return trustedDocumentUri != null
                && Uri.TryCreate(value, UriKind.Absolute, out Uri candidate)
                && string.Equals(candidate.Scheme, trustedDocumentUri.Scheme, StringComparison.OrdinalIgnoreCase)
                && string.Equals(candidate.Host, trustedDocumentUri.Host, StringComparison.OrdinalIgnoreCase)
                && candidate.Port == trustedDocumentUri.Port
                && string.Equals(candidate.AbsolutePath, trustedDocumentUri.AbsolutePath, StringComparison.Ordinal)
                && string.IsNullOrEmpty(candidate.Query)
                && string.IsNullOrEmpty(candidate.Fragment);
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            bool trustedSource = IsTrustedTopLevelDocument(webView.Source?.AbsoluteUri);
            if (e.IsSuccess && trustedSource)
            {
                navigationReady = true;
                return;
            }

            // A cancelled attempt to leave the tokenized local document must
            // not take an already-running client offline.
            if (navigationReady && trustedSource)
                return;

            navigationReady = false;
            if (!e.IsSuccess || !trustedSource)
                HandleFailure(new InvalidOperationException("The local Ruffle page could not be loaded."), "Navigation");
        }

        private void CoreWebView2_ProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs e)
        {
            // Chromium restarts recoverable child processes such as the GPU,
            // utility, and plug-in processes. Treating those restarts as a
            // dead client hides an otherwise live Ruffle surface behind the
            // initialization-failed placeholder. Only loss of the browser or
            // a renderer that owns page state is terminal for this session.
            if (!IsFatalProcessFailure(e.ProcessFailedKind))
                return;

            navigationReady = false;
            HandleFailure(
                new InvalidOperationException(
                    "The Ruffle browser process stopped unexpectedly (" + e.ProcessFailedKind + ")."),
                "ProcessFailed");
            if (session != null && !session.IsDisposed)
                session.MarkFaulted();
        }

        internal static bool IsFatalProcessFailure(CoreWebView2ProcessFailedKind kind)
        {
            return kind == CoreWebView2ProcessFailedKind.BrowserProcessExited
                || kind == CoreWebView2ProcessFailedKind.RenderProcessExited
                || kind == CoreWebView2ProcessFailedKind.FrameRenderProcessExited;
        }

        internal string Dispatch(string function, string argumentsJson)
        {
            if (disposed
                || string.IsNullOrWhiteSpace(function)
                || function.Length > MaximumBridgeFunctionCharacters
                || (argumentsJson != null && argumentsJson.Length > MaximumBridgeMessageCharacters))
            {
                return null;
            }

            JArray arguments;
            try
            {
                arguments = string.IsNullOrWhiteSpace(argumentsJson)
                    ? new JArray()
                    : JArray.Parse(argumentsJson);
            }
            catch (JsonException exception)
            {
                HandleFailure(exception, function);
                return null;
            }

            IDisposable scope = session == null ? null : ClientSessionContext.Push(session);
            try
            {
                string first = arguments.Count == 0 || arguments[0].Type == JTokenType.Null
                    ? string.Empty
                    : arguments[0].Type == JTokenType.String
                        ? arguments[0].Value<string>()
                        : arguments[0].ToString(Formatting.None);
                if (RequiresDeferredHostCallback(function))
                {
                    object[] callbackArguments = arguments
                        .Select(argument => argument.ToObject<object>())
                        .ToArray();
                    QueueHostCallback(function, first, callbackArguments);
                    return null;
                }

                switch (function)
                {
                    case "progress":
                        if (int.TryParse(first, NumberStyles.Integer, CultureInfo.InvariantCulture, out int progress))
                        {
                            SwfLoadProgress?.Invoke(progress);
                            Flash.NotifySwfLoadProgress(progress);
                        }
                        return null;
                    case "modifyServers":
                        return serverMap.Rewrite(first.Trim());
                    case "bridgeReady":
                        return null;
                    case "loadError":
                        HandleFailure(new InvalidOperationException(string.IsNullOrWhiteSpace(first)
                            ? "Ruffle reported a client loading error."
                            : first), function);
                        if (session != null && !session.IsDisposed && !session.IsReady)
                            session.MarkFaulted();
                        return null;
                    default:
                        return null;
                }
            }
            catch (Exception exception)
            {
                HandleFailure(exception, function);
                return null;
            }
            finally
            {
                scope?.Dispose();
            }
        }

        private void QueueHostCallback(string function, string firstArgument, object[] arguments)
        {
            lock (callbackSync)
            {
                if (disposed)
                    return;
                if (callbackQueue.Count >= MaximumQueuedHostCallbacks)
                    throw new InvalidOperationException("Ruffle produced callbacks faster than Grimoire could safely process them.");

                callbackQueue.Enqueue(new QueuedHostCallback(function, firstArgument, arguments));
            }

            ScheduleHostCallbackDrain();
        }

        private void ScheduleHostCallbackDrain()
        {
            lock (callbackSync)
            {
                if (disposed || callbackQueue.Count == 0 || callbackDrainScheduled)
                    return;
                callbackDrainScheduled = true;
            }

            if (!webView.IsHandleCreated || webView.IsDisposed)
            {
                ClearQueuedHostCallbacks();
                return;
            }

            try
            {
                // A WebView host-object call blocks Ruffle's JavaScript stack
                // until Dispatch returns. Packet handlers can synchronously
                // call back into ActionScript, so running them in that stack
                // deadlocks at the first server login response. Posting one
                // ordered drain lets the host call return before any handler
                // is allowed to invoke Ruffle again.
                webView.BeginInvoke(new Action(DrainHostCallbacks));
            }
            catch (InvalidOperationException)
            {
                ClearQueuedHostCallbacks();
            }
        }

        private void DrainHostCallbacks()
        {
            lock (callbackSync)
                callbackDrainScheduled = false;
            if (scriptInvocationInProgress)
                return;

            while (true)
            {
                QueuedHostCallback callback;
                lock (callbackSync)
                {
                    if (disposed || callbackQueue.Count == 0)
                    {
                        callbackQueue.Clear();
                        return;
                    }
                    callback = callbackQueue.Dequeue();
                }

                IDisposable scope = session == null ? null : ClientSessionContext.Push(session);
                try
                {
                    FlashCall?.Invoke(null, callback.Function, callback.Arguments);
                    Flash.NotifyFlashCall(null, callback.Function, callback.Arguments);
                    Proxy.Instance?.LogPackets(callback.Function, callback.FirstArgument);
                }
                catch (Exception exception)
                {
                    HandleFailure(exception, callback.Function);
                }
                finally
                {
                    scope?.Dispose();
                }
            }
        }

        private void ClearQueuedHostCallbacks()
        {
            lock (callbackSync)
            {
                callbackQueue.Clear();
                callbackDrainScheduled = false;
            }
        }

        internal static bool RequiresDeferredHostCallback(string function)
        {
            return !string.Equals(function, "progress", StringComparison.Ordinal)
                && !string.Equals(function, "modifyServers", StringComparison.Ordinal)
                && !string.Equals(function, "bridgeReady", StringComparison.Ordinal)
                && !string.Equals(function, "loadError", StringComparison.Ordinal);
        }

        public T Call<T>(string function, params object[] args)
        {
            try
            {
                JToken value = Invoke(function, args);
                if (value == null || value.Type == JTokenType.Null)
                    return default(T);
                return (T)ConvertDirectResult(value, typeof(T));
            }
            catch (Exception exception)
            {
                HandleFailure(exception, function, args);
                return default(T);
            }
        }

        public object Call(string function, Type resultType, params object[] args)
        {
            if (resultType == null)
                throw new ArgumentNullException(nameof(resultType));
            try
            {
                JToken value = Invoke(function, args);
                return ConvertDirectResult(value, resultType);
            }
            catch (Exception exception)
            {
                HandleFailure(exception, function, args);
                return null;
            }
        }

        public T CallWithStringArguments<T>(string function, params string[] args)
        {
            try
            {
                JToken value = Invoke(function, args?.Cast<object>().ToArray());
                if (value == null || value.Type == JTokenType.Null)
                    return default(T);
                return (T)ConvertSerializedResult(value, typeof(T));
            }
            catch (Exception exception)
            {
                HandleFailure(exception, function, args?.Cast<object>().ToArray() ?? Array.Empty<object>());
                return default(T);
            }
        }

        public string CallString(string function, params object[] args)
        {
            return Call<string>(function, args);
        }

        public string CallGameFunction(string path, params object[] args)
        {
            if (args == null || args.Length == 0)
                return CallWithStringArguments<string>("callGameFunction0", path);
            object[] callArguments = new object[args.Length + 1];
            callArguments[0] = path;
            Array.Copy(args, 0, callArguments, 1, args.Length);
            return Call<string>("callGameFunction", callArguments);
        }

        public string GetGameObject(string path)
        {
            return CallWithStringArguments<string>("getGameObject", path);
        }

        public string GetGameObjectStatic(string path)
        {
            return CallWithStringArguments<string>("getGameObjectS", path);
        }

        public T GetGameObject<T>(string path, T defaultValue = default(T))
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(GetGameObject(path));
            }
            catch
            {
                return defaultValue;
            }
        }

        public void SetGameObject(string path, object value)
        {
            Call<object>("setGameObject", path, value);
        }

        internal static object ConvertDirectResult(JToken value, Type resultType)
        {
            if (resultType == null)
                throw new ArgumentNullException(nameof(resultType));
            return value == null || value.Type == JTokenType.Null
                ? null
                : value.ToObject(resultType);
        }

        internal static object ConvertSerializedResult(JToken value, Type resultType)
        {
            if (resultType == null)
                throw new ArgumentNullException(nameof(resultType));
            if (value == null || value.Type == JTokenType.Null)
                return null;
            string serialized = value.Type == JTokenType.String
                ? value.Value<string>()
                : value.ToString(Formatting.None);
            return JsonConvert.DeserializeObject(serialized, resultType);
        }

        private JToken Invoke(string function, object[] args)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(function)
                || function.Length > MaximumBridgeFunctionCharacters)
            {
                throw new ArgumentException("A game callback name is required.", nameof(function));
            }
            if (!navigationReady || webView.CoreWebView2 == null)
                throw new InvalidOperationException("Ruffle is not ready yet.");

            if (webView.InvokeRequired)
                return (JToken)webView.Invoke(new Func<JToken>(() => InvokeOnUi(function, args)));
            return InvokeOnUi(function, args);
        }

        private JToken InvokeOnUi(string function, object[] args)
        {
            if (scriptInvocationInProgress)
                throw new InvalidOperationException("Ruffle is already processing a game callback.");

            scriptInvocationInProgress = true;
            try
            {
                string envelope = JsonConvert.SerializeObject(new
                {
                    name = function,
                    args = args ?? Array.Empty<object>()
                });
                if (envelope.Length > MaximumBridgeMessageCharacters)
                    throw new InvalidOperationException("The Ruffle game callback exceeded the safety limit.");

                string script = "window.grimoireInvoke(" + JsonConvert.SerializeObject(envelope) + ");";
                string encoded = WaitForUiTask(webView.ExecuteScriptAsync(script), ScriptTimeoutMilliseconds);
                if (encoded == null || encoded.Length > MaximumBridgeMessageCharacters)
                    throw new InvalidOperationException("The Ruffle game response exceeded the safety limit.");

                string responseText = JsonConvert.DeserializeObject<string>(encoded);
                if (responseText == null || responseText.Length > MaximumBridgeMessageCharacters)
                    throw new InvalidOperationException("The Ruffle game response exceeded the safety limit.");
                JObject response = JObject.Parse(responseText);
                if (response["ok"]?.Value<bool>() != true)
                    throw new InvalidOperationException(response["error"]?.Value<string>() ?? "The Ruffle game callback failed.");
                return response["value"];
            }
            finally
            {
                scriptInvocationInProgress = false;
                ScheduleHostCallbackDrain();
            }
        }

        private static T WaitForUiTask<T>(Task<T> task, int timeoutMilliseconds)
        {
            Stopwatch timer = Stopwatch.StartNew();
            while (!task.IsCompleted && timer.ElapsedMilliseconds < timeoutMilliseconds)
            {
                Application.DoEvents();
                Thread.Sleep(1);
            }
            if (!task.IsCompleted)
                throw new TimeoutException("Ruffle did not respond before the safety timeout.");
            return task.GetAwaiter().GetResult();
        }

        private static void WaitForUiTask(Task task, int timeoutMilliseconds)
        {
            Stopwatch timer = Stopwatch.StartNew();
            while (!task.IsCompleted && timer.ElapsedMilliseconds < timeoutMilliseconds)
            {
                Application.DoEvents();
                Thread.Sleep(1);
            }
            if (!task.IsCompleted)
                throw new TimeoutException("Ruffle did not initialize before the safety timeout.");
            task.GetAwaiter().GetResult();
        }

        private void HandleFailure(Exception exception, string function, params object[] args)
        {
            FlashError?.Invoke(null, exception, function, args);
            Flash.NotifyFlashError(null, exception, function, args);
            StartupDiagnostics.RecordException("Ruffle engine", exception, false);
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(RuffleFlashBridge));
        }

        public void Dispose()
        {
            if (disposed)
                return;
            if (Thread.CurrentThread.ManagedThreadId != ownerThreadId)
                throw new InvalidOperationException("Ruffle must be disposed on its owning UI thread.");

            disposed = true;
            navigationReady = false;
            ClearQueuedHostCallbacks();
            localHost?.Dispose();
            localHost = null;
            if (webView.CoreWebView2 != null)
            {
                webView.CoreWebView2.NavigationStarting -= CoreWebView2_NavigationStarting;
                webView.CoreWebView2.ProcessFailed -= CoreWebView2_ProcessFailed;
                try
                {
                    webView.CoreWebView2.RemoveHostObjectFromScript("grimoire");
                }
                catch
                {
                }
                try
                {
                    webView.CoreWebView2.Profile.Delete();
                }
                catch
                {
                }
            }
            webView.NavigationCompleted -= WebView_NavigationCompleted;
            webView.Dispose();
            TryDeleteProfileDirectory();
            SwfLoadProgress = null;
            FlashCall = null;
            FlashError = null;
        }

        private sealed class QueuedHostCallback
        {
            public QueuedHostCallback(string function, string firstArgument, object[] arguments)
            {
                Function = function;
                FirstArgument = firstArgument;
                Arguments = arguments ?? Array.Empty<object>();
            }

            public string Function { get; }

            public string FirstArgument { get; }

            public object[] Arguments { get; }
        }

        private void TryDeleteProfileDirectory()
        {
            try
            {
                string root = Path.GetFullPath(AppPaths.RuffleWebViewDataDirectory)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string target = Path.GetFullPath(profileDirectory)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (!string.Equals(Path.GetDirectoryName(target), root, StringComparison.OrdinalIgnoreCase))
                    return;
                if (Directory.Exists(target))
                    Directory.Delete(target, true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        [ComVisible(true)]
        [ClassInterface(ClassInterfaceType.AutoDual)]
        public sealed class RuffleHostObject
        {
            private readonly RuffleFlashBridge bridge;

            internal RuffleHostObject(RuffleFlashBridge bridge)
            {
                this.bridge = bridge;
            }

            public string Dispatch(string function, string argumentsJson)
            {
                return bridge.Dispatch(function, argumentsJson);
            }
        }
    }
}
