using Grimoire.Botting;
using Grimoire.Game;
using Grimoire.Game.Data;
using Grimoire.Networking.Handlers;
using Grimoire.Tools;
using Grimoire.UI;
using Grimoire.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Grimoire.Networking
{
    public class Proxy
    {
        public delegate void Receive(Message message);

        private List<IJsonMessageHandler> _handlersJson;

        private List<IXtMessageHandler> _handlersXt;

        private List<IXmlMessageHandler> _handlersXml;

        private readonly object _handlerSync = new object();

        private Dictionary<string, IJsonMessageHandler[]> _jsonHandlersByCommand =
            new Dictionary<string, IJsonMessageHandler[]>(StringComparer.Ordinal);

        private Dictionary<string, IXtMessageHandler[]> _xtHandlersByCommand =
            new Dictionary<string, IXtMessageHandler[]>(StringComparer.Ordinal);

        private Dictionary<string, IXmlMessageHandler[]> _xmlHandlersByCommand =
            new Dictionary<string, IXmlMessageHandler[]>(StringComparer.Ordinal);

        public static CancellationTokenSource AppClosingToken;

        public bool _catchXtPackets { get; set; } = false;

        public bool _catchAllPackets { get; set; } = false;

        public bool firstJoin { get; set; } = true;

        public static Proxy Instance
        {
            get;
            set;
        }

        public static event Receive ReceivedFromGame;

        private Proxy()
        {
        }

        internal protected static void InitializeProxyHandlers()
        {
            Instance = new Proxy();
            AppClosingToken = new CancellationTokenSource();
            Instance._handlersJson = new List<IJsonMessageHandler>
            {
                new HandlerDrops(),
                new HandlerGetQuests(),
                new HandlerQuestComplete(),
                new HandlerLoadShop(),
                new HandlerBuyItem(),
                new HandlerSkills(),
                new HandlerArea(),
                new HandlerBotSkills()
            };
            Instance._handlersXt = new List<IXtMessageHandler>
            {
                new HandlerAFK(),
                new HandlerWarningsXt(),
                new HandlerLogin(),
                new HandlerChat(),
                new HandlerXtJoin(),
                new HandlerXtCellJoin(),
                new HandlerConnection()
            };
            Instance._handlersXml = new List<IXmlMessageHandler>
            {
            };
            Instance.RebuildHandlerIndexes();
            ReceivedFromGame += Instance.ProcessMessage;
        }

        public void RegisterHandler(IJsonMessageHandler handler)
        {
            RegisterHandler(handler, _handlersJson);
        }

        public void RegisterHandler(IXmlMessageHandler handler)
        {
            RegisterHandler(handler, _handlersXml);
        }

        public void RegisterHandler(IXtMessageHandler handler)
        {
            RegisterHandler(handler, _handlersXt);
        }

        public void UnregisterHandler(IJsonMessageHandler handler)
        {
            lock (_handlerSync)
            {
                _handlersJson.Remove(handler);
                RebuildHandlerIndexes();
            }
        }

        public void UnregisterHandler(IXmlMessageHandler handler)
        {
            lock (_handlerSync)
            {
                _handlersXml.Remove(handler);
                RebuildHandlerIndexes();
            }
        }

        public void UnregisterHandler(IXtMessageHandler handler)
        {
            lock (_handlerSync)
            {
                _handlersXt.Remove(handler);
                RebuildHandlerIndexes();
            }
        }

        private void RegisterHandler<T>(T handler, List<T> list)
        {
            lock (_handlerSync)
            {
                if (handler != null && !list.Contains(handler))
                {
                    list.Add(handler);
                    RebuildHandlerIndexes();
                }
            }
        }

        private void RebuildHandlerIndexes()
        {
            _jsonHandlersByCommand = BuildHandlerIndex(_handlersJson, handler => handler.HandledCommands);
            _xtHandlersByCommand = BuildHandlerIndex(_handlersXt, handler => handler.HandledCommands);
            _xmlHandlersByCommand = BuildHandlerIndex(_handlersXml, handler => handler.HandledCommands);
        }

        private static Dictionary<string, T[]> BuildHandlerIndex<T>(
            IEnumerable<T> handlers,
            Func<T, string[]> handledCommands)
        {
            Dictionary<string, List<T>> index = new Dictionary<string, List<T>>(StringComparer.Ordinal);
            foreach (T handler in handlers.Where(item => item != null))
            {
                foreach (string command in handledCommands(handler) ?? Array.Empty<string>())
                {
                    if (string.IsNullOrEmpty(command))
                    {
                        continue;
                    }

                    if (!index.TryGetValue(command, out List<T> commandHandlers))
                    {
                        commandHandlers = new List<T>();
                        index.Add(command, commandHandlers);
                    }
                    commandHandlers.Add(handler);
                }
            }

            return index.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);
        }

        public Task SendToServer(string data)
        {
            string text = data?.Replace("{ROOM_ID}", World.RoomId.ToString());
            if (!string.IsNullOrWhiteSpace(text))
            {
                bool json = text.StartsWith("{");
                LogClientPacket(text);
                World.SendPacket(text, json ? "Json" : "String");
            }
            return Task.CompletedTask;
        }

        public Task SendToClient(string data)
        {
            if (!string.IsNullOrWhiteSpace(data))
            {
                bool json = data.StartsWith("{");
                World.SendClientPacket(data, json ? "json" : "str");
            }
            return Task.CompletedTask;
        }

        private void ProcessMessage(Message message)
        {
            switch (message)
            {
                case XtMessage xtMessage:
                    if (!_xtHandlersByCommand.TryGetValue(xtMessage.Command, out IXtMessageHandler[] xtHandlers))
                        break;
                    foreach (IXtMessageHandler item in xtHandlers)
                    {
                        try
                        {
                            item.Handle(xtMessage);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Packet handler {item.GetType().Name} failed: {ex}");
                        }
                    }
                    break;
                case JsonMessage jsonMessage:
                    if (!_jsonHandlersByCommand.TryGetValue(jsonMessage.Command, out IJsonMessageHandler[] jsonHandlers))
                        break;
                    foreach (IJsonMessageHandler item in jsonHandlers)
                    {
                        try
                        {
                            item.Handle(jsonMessage);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Packet handler {item.GetType().Name} failed: {ex}");
                        }
                    }
                    break;
                case XmlMessage xmlMessage:
                    if (!_xmlHandlersByCommand.TryGetValue(xmlMessage.Command, out IXmlMessageHandler[] xmlHandlers))
                        break;
                    foreach (IXmlMessageHandler item in xmlHandlers)
                    {
                        try
                        {
                            item.Handle(xmlMessage);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Packet handler {item.GetType().Name} failed: {ex}");
                        }
                    }
                    break;
            }
        }

        private Message CreateMessage(string raw)
        {
            if (raw != null && raw.Length > 0)
            {
                switch (raw[0])
                {
                    case '<':
                        return new XmlMessage(raw);

                    case '%':
                        return new XtMessage(raw);

                    case '{':
                        return new JsonMessage(raw);
                }
            }
            return null;
        }

        static Proxy()
        {
        }

        public static async void DisableBotSkill()
        {
            if (BotUtilities.ShouldUseSkill)
            {
                await Task.Delay(10);
                BotUtilities.ShouldUseSkill = false;
                Player.CancelAutoAttack();
                World.GamePopup("Disabled the bot's ability to attack.");
            }
        }

        public static async void EnableBotSkill()
        {
            if (!BotUtilities.ShouldUseSkill)
            {
                await Task.Delay(10);
                BotUtilities.ShouldUseSkill = true;
                World.GamePopup("Enabled the bot's ability to attack.");
            }
        }

        public static void OnPingLatency(int latency)
        {
            if (Root.Instance.rtbPing.Visible)
            {
                if (latency >= 0 && latency <= 100)
                {
                    Root.Instance.rtbPing.ForeColor = System.Drawing.Color.LimeGreen;
                }
                else if (latency >= 101 && latency <= 200)
                {
                    Root.Instance.rtbPing.ForeColor = System.Drawing.Color.Orange;
                }
                else if (latency >= 201)
                {
                    Root.Instance.rtbPing.ForeColor = System.Drawing.Color.Red;
                }
                Root.Instance.rtbPing.Clear();
                Root.Instance.rtbPing.AppendText($"⬤  {latency} ms");
            }
        }

        public async Task ClientExecute()
        {
            try
            {
                while (!AppClosingToken.IsCancellationRequested)
                {
                    if (Player.IsLoggedIn)
                    {
                        if (BotManager.Instance.ActiveBotEngine.IsRunning && BotManager.Instance.ActiveBotEngine.Configuration.EnableRejection && !World.VisibleDropUI)
                        {
                            Flash.Call("dropUIOpt", false);
                        }
                    }
                    else if (Root.Instance.rtbPing.Text != "")
                        Root.Instance.rtbPing.Clear();
                    await Task.Delay(Player.IsLoggedIn ? 250 : 1000, AppClosingToken.Token);
                }
            }
            catch (OperationCanceledException) when (AppClosingToken.IsCancellationRequested)
            {
            }
        }

        public void LogPackets(string text, string text2)
        {
            switch (text)
            {
                case "packet":
                case "packetFromServer":
                    PublishServerPacket(text2);
                    if (text2.Contains("<msg t='sys'><body action='logout' r='0'></body></msg>"))
                    {
                        LogForm.Instance.AppendDebug($"[{DateTime.Now:hh:mm:ss}] Safely logged out from the game.\r\n");
                        Player.IsLoggedIn = false;
                    }
                    break;
                case "xtPacket":
                case "packetFromClient":
                    LogClientPacket(text2);
                    break;
                case "pext":
                    PublishExtensionResponse(text2);
                    break;
                case "ping":
                    if (int.TryParse(text2, out int latency))
                        OnPingLatency(latency);
                    break;
                case "chatCommand":
                    ChatCommand.ExecuteCommand(text2);
                    break;
                case "openWebsite":
                    OpenWebAddress(text2);
                    break;
            }
        }

        private static void OpenWebAddress(string address)
        {
            if (Uri.TryCreate(address, UriKind.Absolute, out Uri uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                Process.Start(uri.AbsoluteUri);
            }
        }

        private void LogClientPacket(string packet)
        {
            if (string.IsNullOrWhiteSpace(packet))
                return;

            if (_catchXtPackets)
                PacketLogger.Instance.AppendPacket(packet + Environment.NewLine);
            if (_catchAllPackets)
                PacketTamperer.Instance.AppendCapturedPacket("From client: " + packet + Environment.NewLine);
        }

        private void PublishServerPacket(string packet)
        {
            if (string.IsNullOrWhiteSpace(packet))
                return;

            Message message = CreateMessage(packet);
            if (message != null)
                ReceivedFromGame?.Invoke(message);
            if (_catchAllPackets)
                PacketTamperer.Instance.AppendCapturedPacket("From server: " + packet + Environment.NewLine + Environment.NewLine);
        }

        private void PublishExtensionResponse(string response)
        {
            try
            {
                JObject envelope = JObject.Parse(response);
                JToken parameters = envelope["params"];
                string type = parameters?["type"]?.Value<string>();
                JToken data = parameters?["dataObj"];
                if (data == null || string.IsNullOrEmpty(type))
                    return;

                switch (type.ToLowerInvariant())
                {
                    case "json":
                        JObject wrapped = new JObject
                        {
                            ["t"] = "xt",
                            ["b"] = new JObject
                            {
                                ["r"] = parameters["roomId"] ?? -1,
                                ["o"] = data.DeepClone()
                            }
                        };
                        PublishServerPacket(wrapped.ToString(Formatting.None));
                        break;
                    case "str":
                        if (data is JArray values)
                            PublishServerPacket("%" + string.Join("%", values.Select(value => value.ToString())) + "%");
                        else
                            PublishServerPacket(data.ToString());
                        break;
                    case "xml":
                        PublishServerPacket(data.Type == JTokenType.String ? data.Value<string>() : data.ToString(Formatting.None));
                        break;
                }
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Unable to parse SmartFox extension response: {ex.Message}");
            }
        }

    }
}
