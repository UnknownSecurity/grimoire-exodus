using AxShockwaveFlashObjects;
using Grimoire.Networking;
using Grimoire.Tools;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Web;
using System.Xml.Linq;

namespace Grimoire.Clients
{
    /// <summary>
    /// Session-bound adapter for one ActiveX Flash control.
    /// </summary>
    public sealed class ActiveXFlashBridge : IFlashBridge
    {
        private readonly AxShockwaveFlash control;
        private readonly int ownerThreadId;
        private ClientSession session;
        private bool disposed;

        public ActiveXFlashBridge(AxShockwaveFlash control)
        {
            this.control = control ?? throw new ArgumentNullException(nameof(control));
            ownerThreadId = Thread.CurrentThread.ManagedThreadId;
            control.FlashCall += ProcessFlashCall;
        }

        public AxShockwaveFlash Control => control;

        public event Action<int> SwfLoadProgress;

        public event FlashBridgeCallHandler FlashCall;

        public event FlashBridgeErrorHandler FlashError;

        internal void BindSession(ClientSession value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (session != null && !ReferenceEquals(session, value))
                throw new InvalidOperationException("A Flash bridge cannot be moved to another client session.");
            session = value;
        }

        internal void ProcessFlashCall(object sender, _IShockwaveFlashEvents_FlashCallEvent e)
        {
            AxShockwaveFlash callbackControl = sender as AxShockwaveFlash ?? control;
            IDisposable scope = session == null ? null : ClientSessionContext.Push(session);
            try
            {
                XElement request = XElement.Parse(e.request);
                string function = request.Attribute("name")?.Value;
                string argument = request.Element("arguments")?.Value ?? string.Empty;
                if (function == null)
                    return;

                if (function == "progress")
                {
                    if (int.TryParse(argument, out int progress))
                    {
                        SwfLoadProgress?.Invoke(progress);
                        Flash.NotifySwfLoadProgress(progress);
                    }
                    return;
                }

                if (function == "modifyServers")
                {
                    string serverList = SecurityElement.Escape(Flash.ModifyServerList(argument.Trim()));
                    callbackControl.SetReturnValue("<string>" + serverList + "</string>");
                }

                Proxy.Instance?.LogPackets(function, argument);
            }
            catch (Exception exception)
            {
                RaiseError(callbackControl, exception, "FlashCall", e.request);
            }
            finally
            {
                scope?.Dispose();
            }
        }

        public T Call<T>(string function, params object[] args)
        {
            try
            {
                object value = Call(function, typeof(T), args);
                return value == null ? default(T) : (T)value;
            }
            catch
            {
                return default(T);
            }
        }

        public object Call(string function, Type resultType, params object[] args)
        {
            if (resultType == null)
                throw new ArgumentNullException(nameof(resultType));

            try
            {
                string result = CallFunction(BuildObjectRequest(function, args));
                XElement element = XElement.Parse(result);
                return element.FirstNode == null
                    ? null
                    : Convert.ChangeType(element.FirstNode.ToString(), resultType);
            }
            catch (Exception exception)
            {
                RaiseError(control, exception, function, args);
                return null;
            }
        }

        public T CallWithStringArguments<T>(string function, params string[] args)
        {
            try
            {
                string response = HttpUtility.HtmlDecode(
                    XElement.Parse(CallFunction(BuildStringRequest(function, args))).FirstNode?.ToString()
                        ?? string.Empty);
                return JsonConvert.DeserializeObject<T>(response);
            }
            catch (Exception exception)
            {
                RaiseError(control, exception, function, args?.Cast<object>().ToArray() ?? Array.Empty<object>());
                return default(T);
            }
        }

        public string CallString(string function, params object[] args)
        {
            return Call<string>(function, args);
        }

        public string CallGameFunction(string path, params object[] args)
        {
            return args != null && args.Length > 0
                ? Call<string>("callGameFunction", new object[] { path }.Concat(args).ToArray())
                : CallWithStringArguments<string>("callGameFunction0", path);
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

        private string CallFunction(string request)
        {
            ThrowIfDisposed();
            if (!control.IsHandleCreated && Thread.CurrentThread.ManagedThreadId != ownerThreadId)
            {
                throw new InvalidOperationException(
                    "The Flash control must create its handle on the owning UI thread before background calls begin.");
            }

            if (control.InvokeRequired)
                return (string)control.Invoke(new Func<string>(() => control.CallFunction(request)));
            return control.CallFunction(request);
        }

        private static string BuildObjectRequest(string function, object[] args)
        {
            StringBuilder request = new StringBuilder()
                .Append("<invoke name=\"")
                .Append(function)
                .Append("\" returntype=\"xml\">");
            if (args != null && args.Length > 0)
            {
                request.Append("<arguments>");
                foreach (object argument in args)
                    request.Append(Flash.ToFlashXml(argument));
                request.Append("</arguments>");
            }
            return request.Append("</invoke>").ToString();
        }

        private static string BuildStringRequest(string function, string[] args)
        {
            StringBuilder request = new StringBuilder()
                .Append("<invoke name=\"")
                .Append(function)
                .Append("\" returntype=\"xml\">");
            if (args != null && args.Length > 0)
            {
                request.Append("<arguments>");
                foreach (string argument in args)
                {
                    request.Append("<string>")
                        .Append(SecurityElement.Escape(argument ?? string.Empty))
                        .Append("</string>");
                }
                request.Append("</arguments>");
            }
            return request.Append("</invoke>").ToString();
        }

        internal void ProcessCallHandler(object sender, _IShockwaveFlashEvents_FlashCallEvent e)
        {
            AxShockwaveFlash callbackControl = sender as AxShockwaveFlash ?? control;
            IDisposable scope = session == null ? null : ClientSessionContext.Push(session);
            try
            {
                XElement request = XElement.Parse(e.request);
                string function = request.Attribute("name")?.Value;
                object[] args = request.Elements().Select(Flash.FromFlashXml).ToArray();
                FlashCall?.Invoke(callbackControl, function, args);
                Flash.NotifyFlashCall(callbackControl, function, args);
            }
            catch (Exception exception)
            {
                RaiseError(callbackControl, exception, "FlashCall", e.request);
            }
            finally
            {
                scope?.Dispose();
            }
        }

        private void RaiseError(AxShockwaveFlash source, Exception exception, string function, params object[] args)
        {
            FlashError?.Invoke(source, exception, function, args);
            Flash.NotifyFlashError(source, exception, function, args);
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ActiveXFlashBridge));
        }

        public void Dispose()
        {
            if (disposed)
                return;

            if (Thread.CurrentThread.ManagedThreadId != ownerThreadId)
                throw new InvalidOperationException("Flash controls must be disposed on their owning UI thread.");

            disposed = true;
            control.FlashCall -= ProcessFlashCall;
            control.Dispose();
        }
    }
}
