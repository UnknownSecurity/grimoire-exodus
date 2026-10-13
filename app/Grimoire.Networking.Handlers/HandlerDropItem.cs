using Grimoire.Botting;
using Grimoire.Game;
using Grimoire.Game.Data;
using Grimoire.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grimoire.Networking.Handlers
{
    public class HandlerDrops : IJsonMessageHandler
    {
        public string[] HandledCommands
        {
            get;
        } = new string[3]
        {
            "dropItem",
            "getDrop",
            "addItems"
        };

        public JToken items;

        public InventoryItem drop;

        public async void Handle(JsonMessage message)
        {
            try
            {
                switch (message.Command)
                {
                    case "dropItem":
                        items = message.DataObject?["items"];
                        if (items == null)
                            return;
                        drop = items.ToObject<Dictionary<int, InventoryItem>>().FirstOrDefault().Value;
                        if (drop != null)
                            World.OnItemDropped(drop);
                        break;
                    case "getDrop":
                        await Task.Delay(10);
                        ParseLog(message, true);
                        break;
                    case "addItems":
                        ParseLog(message, false);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogForm.Instance.AppendDebug($"[{DateTime.Now:hh:mm:ss}] Drop packet failed: {ex.Message}\r\n");
            }
        }

        private void ParseLog(JsonMessage content, bool manual)
        {
            switch (manual)
            {
                case true:
                    if (content.DataObject["bSuccess"].Value<int>() != 1)
                    {
                        return;
                    }
                    int id = content.DataObject["ItemID"].Value<int>();
                    World.DropStack.RemoveAll(id);
                    if (BotManager.Instance.ActiveBotEngine.IsRunning)
                    {
                        bool inBank = content.DataObject["bBank"].Value<int>() != 0;
                        int dropQty = content.DataObject["iQty"].Value<int>();
                        if (inBank)
                        {
                            InventoryItem bankItem = Player.Bank.SavedItems?.Find(i => i.Id.Equals(id));
                            if (bankItem != null)
                                bankItem.Quantity += dropQty;
                        }
                        IEnumerable<InventoryItem> receivedItems = (inBank ? Player.Bank.SavedItems : Player.Inventory.Items) ?? Enumerable.Empty<InventoryItem>();
                        drop = receivedItems.FirstOrDefault(i => i.Id.Equals(id)) ?? Player.House.Items?.FirstOrDefault(i => i.Id.Equals(id));
                        if (drop == null)
                            return;
                        LogForm.Instance.AppendBot($"[{DateTime.Now:hh:mm:ss}] Item accepted: {drop.Name} x {dropQty}.\r\n");
                        if (BotManager.Instance.chkNotifyItemOnPickup.Checked && BotManager.Instance.ActiveBotEngine.Configuration.Items.Any(i => i.Equals(drop.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            World.GamePopup($"{(inBank ? "Bank" : "Inventory")}: {drop.Name} {drop.Quantity}/{drop.MaxStack}");
                        }
                    }
                    break;
                case false:
                    items = content.DataObject?["items"];
                    if (items == null)
                    {
                        return;
                    }
                    foreach (JProperty item in items)
                    {
                        drop = Player.Inventory.Items?.FirstOrDefault(i => i.Id.Equals(int.Parse(item.Name)));
                        if (drop == null)
                        {
                            continue;
                        }
                        string dropQty = item.Value["iQty"].ToString();
                        string dropQtyNow = item.Value["iQtyNow"].ToString();
                        if (BotManager.Instance.ActiveBotEngine.IsRunning)
                        {
                            LogForm.Instance.AppendBot($"[{DateTime.Now:hh:mm:ss}] Item accepted: {drop.Name} x {dropQty}.\r\n");
                            if (BotManager.Instance.chkNotifyItemOnPickup.Checked && BotManager.Instance.ActiveBotEngine.Configuration.Items.Any(i => i.Equals(drop.Name, StringComparison.OrdinalIgnoreCase)))
                            {
                                World.GamePopup($"Inventory: {drop.Name} {dropQtyNow}/{drop.MaxStack}");
                            }
                        }
                        if (!Bot.DropsInInventory.Contains(drop.Name))
                        {
                            Bot.DropsInInventory.Add(drop.Name);
                        }
                        LogForm.Instance.AppendDrops($"[Item Drop] ({dropQtyNow}) {drop.Name} x {dropQty} at {DateTime.Now:hh:mm:ss tt}.\r\n");
                    }
                    break;
            }
        }
    }
}
