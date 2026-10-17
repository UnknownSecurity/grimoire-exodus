using AxShockwaveFlashObjects;
using Grimoire.Clients;
using Grimoire.Game;
using Grimoire.Game.Data;
using Grimoire.Networking;
using Grimoire.UI;
using Grimoire.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Grimoire.Tools
{
    public delegate void FlashCallHandler(AxShockwaveFlash flash, string function, params object[] args);

    public delegate void FlashErrorHandler(AxShockwaveFlash flash, Exception e, string function, params object[] args);

    public class Flash
    {
        private static Flash _instance;
        public static Flash Instance => _instance ?? (_instance = new Flash());

        public static AxShockwaveFlash flash;

        public static event FlashCallHandler FlashCall;

        public static event FlashErrorHandler FlashError;

        public static event Action<int> SwfLoadProgress;

        public static void ProcessFlashCall(object sender, _IShockwaveFlashEvents_FlashCallEvent e)
        {
            AxShockwaveFlash source = sender as AxShockwaveFlash ?? flash;
            ClientSession session = ClientSessionManager.Default?.FindByControl(source);
            ActiveXFlashBridge bridge = session?.FlashBridge as ActiveXFlashBridge;
            if (bridge != null)
            {
                bridge.ProcessFlashCall(sender, e);
                return;
            }

            // Compatibility path for plugin-hosted controls that have not yet
            // been registered as a ClientSession. Responses still go back to
            // the callback sender instead of the root window's active client.
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
                        NotifySwfLoadProgress(progress);
                    return;
                }

                if (function == "modifyServers" && source != null)
                {
                    string serverList = SecurityElement.Escape(ModifyServerList(argument.Trim()));
                    source.SetReturnValue("<string>" + serverList + "</string>");
                }
                Proxy.Instance?.LogPackets(function, argument);
            }
            catch (Exception ex)
            {
                NotifyFlashError(source, ex, "FlashCall", e.request);
            }
        }

        public string GetGameObject(string path)
        {
            return ResolveBridge()?.GetGameObject(path);
        }

        public string GetGameObjectStatic(string path)
        {
            return ResolveBridge()?.GetGameObjectStatic(path);
        }

        public T GetGameObject<T>(string path, T def = default)
        {
            try
            {
                IFlashBridge bridge = ResolveBridge();
                return bridge == null ? def : bridge.GetGameObject(path, def);
            }
            catch
            {
                return def;
            }
        }

        public static string CallGameFunction(string path, params object[] args)
        {
            IFlashBridge bridge = ResolveBridge();
            return bridge == null ? null : bridge.CallGameFunction(path, args);
        }

        public void SetGameObject(string path, object value)
        {
            ResolveBridge()?.SetGameObject(path, value);
        }

        public static string Call(string function, params object[] args)
        {
            return Call<string>(function, args);
        }

        public static T Call<T>(string function, params object[] args)
        {
            IFlashBridge bridge = ResolveBridge();
            return bridge == null ? default(T) : bridge.Call<T>(function, args);
        }

        public static object Call(string function, Type type, params object[] args)
        {
            return ResolveBridge()?.Call(function, type, args);
        }

        public static T Call<T>(string function, params string[] args)
        {
            IFlashBridge bridge = ResolveBridge();
            return bridge == null ? default(T) : bridge.CallWithStringArguments<T>(function, args);
        }

        public static string CallString(string function, params object[] args)
        {
            return ResolveBridge()?.CallString(function, args);
        }

        public static void Call(string function, params string[] args)
        {
            ResolveBridge()?.CallWithStringArguments<object>(function, args);
        }

        private static IFlashBridge ResolveBridge()
        {
            return ClientSessionContext.Resolve()?.FlashBridge;
        }

        public static string ToFlashXml(object o)
        {
            switch (o)
            {
                case null:
                    return "<null/>";

                case bool _:
                    return $"<{o.ToString().ToLower()}/>";

                case double _:
                case float _:
                case long _:
                case int _:
                    return $"<number>{o}</number>";

                case ExpandoObject _:
                    StringBuilder sb = new StringBuilder().Append("<object>");
                    foreach (KeyValuePair<string, object> kvp in o as IDictionary<string, object>)
                        sb.Append($"<property id=\"{kvp.Key}\">{ToFlashXml(kvp.Value)}</property>");
                    return sb.Append("</object>").ToString();

                default:
                    if (o is Array)
                    {
                        StringBuilder _sb = new StringBuilder().Append("<array>");
                        int k = 0;
                        foreach (object el in o as Array)
                            _sb.Append($"<property id=\"{k++}\">{ToFlashXml(el)}</property>");
                        return _sb.Append("</array>").ToString();
                    }
                    return $"<string>{SecurityElement.Escape(o.ToString())}</string>";
            }
        }

        internal static string ModifyServerList(string response)
        {
            if (response.StartsWith("{\"login\"") && response.EndsWith("}"))
            {
                return ServersFromJson(response);
            }
            return response;
        }

        private static string ServersFromJson(String json)
        {
            JObject packet = (JObject)JObject.Parse(json);
            JObject login = (JObject)packet["login"];
            JArray servers = (JArray)packet["servers"];
            Server[] array = new Server[servers.Count];

            login["iAge"] = 99;

            for (int i = 0; i < servers.Count; i++)
            {
                JObject server = (JObject)servers[i];
                array[i] = new Server
                {
                    IsChatRestricted = server.GetValue("iChat")?.ToString() == "0",
                    PlayerCount = int.Parse(server.GetValue("iCount")?.ToString()),
                    MaxCount = int.Parse(server.GetValue("iMax")?.ToString()),
                    IsMemberOnly = server.GetValue("bUpg")?.ToString() == "1",
                    IsOnline = server.GetValue("bOnline")?.ToString() == "1",
                    Name = server.GetValue("sName")?.ToString(),
                    Language = server.GetValue("sLang")?.ToString(),
                    Port = int.Parse(server.GetValue("iPort")?.ToString()),
                    Ip = server.GetValue("sIP")?.ToString()
                };
            }
            BotManager.Instance.OnServersLoaded(array);
            ParseAvatarDatas(login);
            return packet.ToString(Newtonsoft.Json.Formatting.None);
        }

        private static void ParseAvatarDatas(JObject loginResponse)
        {
            Player.Username = loginResponse["unm"].ToString();
            Player.CharUserID = int.Parse(loginResponse["userid"].ToString());
        }

        public static object FromFlashXml(XElement el)
        {
            switch (el.Name.ToString())
            {
                case "number":
                    return int.TryParse(el.Value, out int i) ? i : float.TryParse(el.Value, out float f) ? f : 0;
                case "true":
                    return true;
                case "false":
                    return false;
                case "null":
                    return null;
                case "array":
                    return el.Elements().Select(e => FromFlashXml(e)).ToArray();
                case "object":
                    dynamic d = new ExpandoObject();
                    el.Elements().ForEach(e => d[e.Attribute("id").Value] = FromFlashXml(e.Elements().First()));
                    return d;
                default:
                    return el.Value;
            }
        }

        public static void CallHandler(object sender, _IShockwaveFlashEvents_FlashCallEvent e)
        {
            AxShockwaveFlash source = sender as AxShockwaveFlash ?? flash;
            ClientSession session = ClientSessionManager.Default?.FindByControl(source);
            ActiveXFlashBridge bridge = session?.FlashBridge as ActiveXFlashBridge;
            if (bridge != null)
            {
                bridge.ProcessCallHandler(sender, e);
                return;
            }

            XElement el = XElement.Parse(e.request);
            string name = el.Attribute("name").Value;
            object[] args = el.Elements().Select(x => FromFlashXml(x)).ToArray();
            NotifyFlashCall(source, name, args);
        }

        internal static void NotifySwfLoadProgress(int progress)
        {
            SwfLoadProgress?.Invoke(progress);
        }

        internal static void NotifyFlashCall(AxShockwaveFlash source, string function, params object[] args)
        {
            FlashCall?.Invoke(source, function, args);
        }

        internal static void NotifyFlashError(
            AxShockwaveFlash source,
            Exception exception,
            string function,
            params object[] args)
        {
            FlashError?.Invoke(source, exception, function, args);
        }
    }
}
