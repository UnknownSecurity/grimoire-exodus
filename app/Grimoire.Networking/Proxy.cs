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
            _handlersJson.Remove(handler);
        }

        public void UnregisterHandler(IXmlMessageHandler handler)
        {
            _handlersXml.Remove(handler);
        }

        public void UnregisterHandler(IXtMessageHandler handler)
        {
            _handlersXt.Remove(handler);
        }

        private void RegisterHandler<T>(T handler, List<T> list)
        {
            if (handler != null && !list.Contains(handler))
            {
                list.Add(handler);
            }
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
                    foreach (IXtMessageHandler item in Instance._handlersXt.Where(h => h != null && h.HandledCommands.Contains(xtMessage.Command)).ToArray())
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
                    foreach (IJsonMessageHandler item in Instance._handlersJson.Where(h => h != null && h.HandledCommands.Contains(jsonMessage.Command)).ToArray())
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
                    foreach (IXmlMessageHandler item in Instance._handlersXml.Where(h => h != null && h.HandledCommands.Contains(xmlMessage.Command)).ToArray())
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
                await Task.Delay(100);
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
                PacketLogger.Instance.txtPackets.AppendText(packet + Environment.NewLine);
            if (_catchAllPackets)
                PacketTamperer.Instance.txtReceive.AppendText("From client: " + packet + Environment.NewLine);
        }

        private void PublishServerPacket(string packet)
        {
            if (string.IsNullOrWhiteSpace(packet))
                return;

            Message message = CreateMessage(packet);
            if (message != null)
                ReceivedFromGame?.Invoke(message);
            if (_catchAllPackets)
                PacketTamperer.Instance.txtReceive.AppendText("From server: " + packet + Environment.NewLine + Environment.NewLine);
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
