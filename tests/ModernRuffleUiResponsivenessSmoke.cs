using Grimoire.Clients;
using Grimoire.UI.Modern;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Grimoire.Utils
{
    public static class AppPaths
    {
        public static string ApplicationDirectory => AppDomain.CurrentDomain.BaseDirectory;

        public static string ModernUiWebViewDataDirectory
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("GRIMOIRE_MODERN_SMOKE_PROFILE");
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidOperationException("The isolated Modern UI test profile was not configured.");
                return value;
            }
        }
    }

    public static class StartupDiagnostics
    {
        public static void RecordException(string context, Exception exception, bool fatal)
        {
        }
    }
}

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.AutoDual)]
public sealed class ModernRuffleSmokeHostObject
{
    public string Ping()
    {
        return "pong";
    }
}

internal static class ModernRuffleUiResponsivenessSmoke
{
    private const int OperationTimeoutMilliseconds = 7000;
    private const int MaximumUiResponseMilliseconds = 750;

    [STAThread]
    private static int Main()
    {
        Form form = null;
        ModernShellHost shell = null;
        LoopbackPageHost pageHost = null;
        LoopbackPageHost topPageHost = null;
        IRuffleFrame frame = null;
        try
        {
            Application.EnableVisualStyles();
            SmokeApplication application = new SmokeApplication();
            shell = new ModernShellHost(application);
            form = new Form
            {
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Left = -32000,
                Top = -32000,
                Width = 960,
                Height = 620
            };
            form.Controls.Add(shell);
            form.Show();

            pageHost = new LoopbackPageHost(
                "modernrufflesmoke1234",
                "<!doctype html><html><body><script>window.grimoireSmokeValue='42';</script></body></html>");
            topPageHost = new LoopbackPageHost(
                "modernshellsmoke1234",
                "<!doctype html><html><body><script>"
                + "window.__grimoireSmokeResponse=null;"
                + "window.chrome.webview.addEventListener('message',function(e){"
                + "if(e.data&&e.data.event==='engine.ruffle.mount'){"
                + "var f=document.createElement('iframe');f.name=e.data.payload.name;"
                + "f.src=e.data.payload.source;document.body.appendChild(f);return;}"
                + "if(e.data&&e.data.id==='modern-ruffle-smoke')window.__grimoireSmokeResponse=e.data;"
                + "});"
                + "</script></body></html>");
            WebView2 browser = (WebView2)shell.InvocationControl;
            CoreWebView2Environment environment = WaitForTask(
                CoreWebView2Environment.CreateAsync(null, Grimoire.Utils.AppPaths.ModernUiWebViewDataDirectory),
                OperationTimeoutMilliseconds,
                "Modern UI environment creation");
            WaitForTask(
                browser.EnsureCoreWebView2Async(environment),
                OperationTimeoutMilliseconds,
                "Modern UI browser creation");
            WirePrivateHandler(
                browser.CoreWebView2,
                "WebMessageReceived",
                shell,
                "CoreWebView2_WebMessageReceived",
                typeof(EventHandler<CoreWebView2WebMessageReceivedEventArgs>));
            WirePrivateHandler(
                browser.CoreWebView2,
                "FrameCreated",
                shell,
                "CoreWebView2_FrameCreated",
                typeof(EventHandler<CoreWebView2FrameCreatedEventArgs>));
            SetPrivateField(shell, "initialized", true);
            SetPrivateField(shell, "uiReady", true);

            TaskCompletionSource<bool> topReady = new TaskCompletionSource<bool>();
            EventHandler<CoreWebView2NavigationCompletedEventArgs> topNavigationCompleted = null;
            topNavigationCompleted = delegate(object sender, CoreWebView2NavigationCompletedEventArgs e)
            {
                browser.CoreWebView2.NavigationCompleted -= topNavigationCompleted;
                if (e.IsSuccess)
                    topReady.TrySetResult(true);
                else
                    topReady.TrySetException(new InvalidOperationException("The Modern UI test page could not load."));
            };
            browser.CoreWebView2.NavigationCompleted += topNavigationCompleted;
            browser.CoreWebView2.Navigate(topPageHost.PageUrl);
            WaitForTask(topReady.Task, OperationTimeoutMilliseconds, "Modern UI test-page navigation");

            string frameName = "grimoire-ruffle-modern-smoke";
            string sessionId = Guid.NewGuid().ToString("D");
            Task<IRuffleFrame> attach = shell.AttachRuffleFrameAsync(
                frameName,
                sessionId,
                new Uri(pageHost.PageUrl),
                new ModernRuffleSmokeHostObject());
            frame = WaitForTask(attach, OperationTimeoutMilliseconds, "Modern Ruffle frame attachment");
            WaitForFrameValue(frame, OperationTimeoutMilliseconds);
            application.Frame = frame;

            Stopwatch timer = Stopwatch.StartNew();
            ExecuteScript(
                browser,
                "window.chrome.webview.postMessage({"
                + "protocol:'1.0',id:'modern-ruffle-smoke',method:'options.toggle',"
                + "params:{sessionId:'" + sessionId + "',smoke:true}});",
                OperationTimeoutMilliseconds);

            JObject response = null;
            while (response == null && timer.ElapsedMilliseconds < OperationTimeoutMilliseconds)
            {
                string encoded = ExecuteScript(
                    browser,
                    "JSON.stringify(window.__grimoireSmokeResponse)",
                    OperationTimeoutMilliseconds);
                string responseText = JsonConvert.DeserializeObject<string>(encoded);
                if (!string.IsNullOrWhiteSpace(responseText) && responseText != "null")
                    response = JObject.Parse(responseText);
                else
                {
                    Application.DoEvents();
                    Thread.Sleep(5);
                }
            }

            if (response == null)
                throw new TimeoutException("The Modern UI request never received a native response.");
            if (timer.ElapsedMilliseconds > MaximumUiResponseMilliseconds)
            {
                throw new InvalidOperationException(
                    "The Modern UI request was blocked by the shared Ruffle frame for "
                    + timer.ElapsedMilliseconds
                    + " ms; the maximum responsive interval is "
                    + MaximumUiResponseMilliseconds
                    + " ms.");
            }
            if (response["ok"]?.Value<bool>() != true)
                throw new InvalidOperationException("The Modern UI action returned an error response.");
            if (application.FrameResult != "42")
                throw new InvalidOperationException("The Modern UI action did not receive the child-frame result.");

            Console.WriteLine(
                "PASS: Modern UI answered while a shared Ruffle-frame call completed ("
                + timer.ElapsedMilliseconds
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
            if (frame != null)
                frame.Dispose();
            if (pageHost != null)
                pageHost.Dispose();
            if (topPageHost != null)
                topPageHost.Dispose();
            if (shell != null)
                shell.Dispose();
            if (form != null)
                form.Dispose();
        }
    }

    private static string ExecuteScript(WebView2 browser, string script, int timeoutMilliseconds)
    {
        return WaitForTask(browser.ExecuteScriptAsync(script), timeoutMilliseconds, "Modern UI script execution");
    }

    private static void WaitForFrameValue(IRuffleFrame frame, int timeoutMilliseconds)
    {
        Stopwatch timer = Stopwatch.StartNew();
        while (timer.ElapsedMilliseconds < timeoutMilliseconds)
        {
            string encoded = WaitForTask(
                frame.ExecuteScriptAsync("window.grimoireSmokeValue"),
                timeoutMilliseconds,
                "Shared Ruffle frame preflight");
            string value = JsonConvert.DeserializeObject<string>(encoded);
            if (value == "42")
                return;
            Application.DoEvents();
            Thread.Sleep(10);
        }
        throw new TimeoutException("The shared Ruffle frame did not finish navigating.");
    }

    private static void WirePrivateHandler(
        object source,
        string eventName,
        object target,
        string methodName,
        Type delegateType)
    {
        EventInfo eventInfo = source.GetType().GetEvent(eventName);
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (eventInfo == null || method == null)
            throw new InvalidOperationException("The Modern UI host event boundary was not found.");
        eventInfo.AddEventHandler(source, Delegate.CreateDelegate(delegateType, target, method));
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            throw new InvalidOperationException("The Modern UI host state field was not found.");
        field.SetValue(target, value);
    }

    private static T WaitForTask<T>(Task<T> task, int timeoutMilliseconds, string operation)
    {
        Stopwatch timer = Stopwatch.StartNew();
        while (!task.IsCompleted && timer.ElapsedMilliseconds < timeoutMilliseconds)
        {
            Application.DoEvents();
            Thread.Sleep(1);
        }
        if (!task.IsCompleted)
            throw new TimeoutException(operation + " timed out.");
        return task.GetAwaiter().GetResult();
    }

    private static void WaitForTask(Task task, int timeoutMilliseconds, string operation)
    {
        Stopwatch timer = Stopwatch.StartNew();
        while (!task.IsCompleted && timer.ElapsedMilliseconds < timeoutMilliseconds)
        {
            Application.DoEvents();
            Thread.Sleep(1);
        }
        if (!task.IsCompleted)
            throw new TimeoutException(operation + " timed out.");
        task.GetAwaiter().GetResult();
    }

    private sealed class SmokeApplication : IModernUiApplication
    {
        public IRuffleFrame Frame { private get; set; }

        public string FrameResult { get; private set; }

        public JObject GetBootstrap()
        {
            return new JObject { ["smoke"] = true };
        }

        public JToken Invoke(string method, JObject parameters)
        {
            if (method == "app.getBootstrap")
                return GetBootstrap();
            if (method == "options.toggle" && parameters?["smoke"]?.Value<bool>() == true)
            {
                if (Frame == null)
                    throw new InvalidOperationException("The Modern Ruffle frame is not attached.");
                Task<string> task = Frame.ExecuteScriptAsync("window.grimoireSmokeValue");
                string encoded = WaitForTask(task, 2000, "Shared Ruffle frame callback");
                FrameResult = JsonConvert.DeserializeObject<string>(encoded);
                return new JObject { ["success"] = true };
            }
            return new JObject { ["success"] = true };
        }
    }

    private sealed class LoopbackPageHost : IDisposable
    {
        private readonly TcpListener listener;
        private readonly Thread worker;
        private volatile bool disposed;

        private readonly string page;

        public LoopbackPageHost(string token, string page)
        {
            this.page = page ?? throw new ArgumentNullException(nameof(page));
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            PageUrl = "http://127.0.0.1:" + port + "/" + token + "/index.html";
            worker = new Thread(Run) { IsBackground = true, Name = "ModernRuffleSmokeHost" };
            worker.Start();
        }

        public string PageUrl { get; }

        private void Run()
        {
            while (!disposed)
            {
                try
                {
                    using (TcpClient client = listener.AcceptTcpClient())
                    using (NetworkStream stream = client.GetStream())
                    {
                        ReadHeaders(stream);
                        byte[] body = Encoding.UTF8.GetBytes(page);
                        byte[] header = Encoding.ASCII.GetBytes(
                            "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: "
                            + body.Length
                            + "\r\nConnection: close\r\n\r\n");
                        stream.Write(header, 0, header.Length);
                        stream.Write(body, 0, body.Length);
                    }
                }
                catch (SocketException)
                {
                    if (!disposed)
                        throw;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (IOException)
                {
                    if (disposed)
                        return;
                }
            }
        }

        private static void ReadHeaders(Stream stream)
        {
            int matched = 0;
            while (matched < 4)
            {
                int value = stream.ReadByte();
                if (value < 0)
                    return;
                byte expected = matched == 0 || matched == 2 ? (byte)'\r' : (byte)'\n';
                matched = value == expected ? matched + 1 : 0;
            }
        }

        public void Dispose()
        {
            disposed = true;
            listener.Stop();
            if (worker.IsAlive)
                worker.Join(1000);
        }
    }
}
