using Grimoire.Game;
using Grimoire.Clients;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grimoire.Botting.Commands.Combat
{
    public class CmdKillFor : IBotCommand
    {
        public string Monster
        {
            get;
            set;
        }

        public string ItemName
        {
            get;
            set;
        }

        public ItemType ItemType
        {
            get;
            set;
        }

        public string Quantity
        {
            get;
            set;
        }

        private bool notInInventory(string item, string quantity)
        {
            if (ItemType == ItemType.Items)
            {
                return !Player.Inventory.ContainsItem(item, quantity);
            }
            else
            {
                return !Player.TempInventory.ContainsItem(item, quantity);
            }
        }

        public async Task Execute(IBotEngine instance)
        {
            CmdKill kill = new CmdKill
            {
                Monster = instance.Value(Monster)
            };
            while (instance.IsRunning && notInInventory(instance.Value(ItemName), instance.Value(Quantity)))
            {
                Func<bool> itemQuantityIncreased = null;
                if (ClientSessionContext.Resolve()?.FlashBridge?.Engine == GameEngineKind.Ruffle)
                {
                    string itemName = instance.Value(ItemName);
                    int quantityBeforeKill = GetItemQuantity(itemName);
                    itemQuantityIncreased = () => GetItemQuantity(itemName) > quantityBeforeKill;
                }
                await kill.ExecuteForKillFor(instance, itemQuantityIncreased);
            }
            Player.CancelTarget();
        }

        private int GetItemQuantity(string itemName)
        {
            if (ItemType == ItemType.Items)
            {
                var item = Player.Inventory.Items?.FirstOrDefault(
                    candidate => candidate.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
                return item?.Quantity ?? 0;
            }

            var tempItem = Player.TempInventory.Items?.FirstOrDefault(
                candidate => candidate.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
            return tempItem?.Quantity ?? 0;
        }

        public override string ToString()
        {
            return "Kill for " + ((ItemType == ItemType.Items) ? "items" : "temporary items") + ": " + Monster;
        }
    }
}
