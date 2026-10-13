using Grimoire.Game;
using Grimoire.Game.Data;
using Grimoire.UI;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Grimoire.Botting.Commands.Item
{
    public class CmdEquipSet : IBotCommand
    {
        public string ItemName
        {
            get;
            set;
        }

        private readonly JsonSerializerSettings _serializerSettings =
            BotJson.CreateSettings(DefaultValueHandling.Include);

        public async Task Execute(IBotEngine instance)
        {

            string path = Path.IsPathRooted(ItemName)
                ? ItemName
                : Path.Combine(Application.StartupPath, "Sets", ItemName ?? string.Empty);
            if (!File.Exists(path))
                throw new FileNotFoundException("Equipment set was not found.", path);

            SetItem jsonObj = JsonConvert.DeserializeObject<SetItem>(File.ReadAllText(path), _serializerSettings);
            if (jsonObj?.Set == null)
                return;

            foreach (Grimoire.UI.Item obj in jsonObj.Set.OfType<Grimoire.UI.Item>())
            {
                InventoryItem item = Player.Inventory.Items.FirstOrDefault((InventoryItem i) => i.Name.Equals(obj.Name, StringComparison.OrdinalIgnoreCase) && i.IsEquippable);
                if (item == null)
                    continue;

                int attempts = 0;
                while (instance.IsRunning && !IsEquipped(item.Id) && attempts++ < 10)
                {
                    BotData.BotState = BotData.State.Transaction;
                    while (instance.IsRunning && Player.CurrentState == Player.State.InCombat)
                    {
                        Player.MoveToCell(Player.Cell, Player.Pad);
                        await Task.Delay(1000);
                    }
                    await instance.WaitUntil(() => World.IsActionAvailable(LockActions.EquipItem));
                    Player.Equip(item.Id);
                    await Task.Delay(250);
                }
            }
        }

        public bool IsEquipped(int ItemID)
        {
            return Player.Inventory.Items.FirstOrDefault((InventoryItem it) => it.IsEquipped && it.Id == ItemID) != null;
        }

        public override string ToString()
        {
            return "Equip Set: " + ItemName;
        }
    }
}
