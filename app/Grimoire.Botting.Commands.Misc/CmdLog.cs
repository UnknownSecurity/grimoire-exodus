using Grimoire.Game;
using Grimoire.Game.Data;
using Grimoire.UI;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Grimoire.Botting.Commands.Misc
{
    public class CmdLog : IBotCommand
    {
        private static readonly Regex ItemRegex = new Regex("{ITEM:\\s*(.*?)}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ItemMaxRegex = new Regex("{ITEM MAX:\\s*(.*?)}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RepXpRegex = new Regex("{REP XP:\\s*(.*?)}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RepCurrentRegex = new Regex("{REP CURRENT:\\s*(.*?)}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RepRemainingRegex = new Regex("{REP REMAINING:\\s*(.*?)}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RepRequiredRegex = new Regex("{REP REQUIRED:\\s*(.*?)}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RepRankRegex = new Regex("{REP RANK:\\s*(.*?)}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RepTotalRegex = new Regex("{REP TOTAL:\\s*(.*?)}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex IntValueRegex = new Regex("{INT VALUE:\\s*(.*?)}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public string Text
        {
            get;
            set;
        }

        public bool Debug
        {
            get;
            set;
        } = false;

        private bool Clear
        {
            get;
            set;
        } = false;

        public Task Execute(IBotEngine instance)
        {
            List<InventoryItem> inventory = Player.Inventory.Items ?? new List<InventoryItem>();
            List<Faction> factions = Player.Factions ?? new List<Faction>();
            string text = Text;
            text = text.Replace("{USERNAME}", Player.Username);
            text = text.Replace("{MAP}", Player.Map).Replace("{ROOM_ID}", World.RoomId.ToString()).Replace("{ROOM_NUM}", World.RoomNumber.ToString());
            text = text.Replace("{GOLD}", Player.Gold.ToString());
            text = text.Replace("{LEVEL}", Player.Level.ToString());
            text = text.Replace("{CELL}", Player.Cell).Replace("{PAD}", Player.Pad);
            text = text.Replace("{HEALTH}", Player.Health.ToString()).Replace("{MANA}", Player.Mana.ToString());
            text = text.Replace("{TIME}", $"{DateTime.Now:HH:mm:ss}");
            text = text.Replace("{TIME: 12}", $"{DateTime.Now:hh:mm:ss tt}");
            text = text.Replace("{TIME: 24}", $"{DateTime.Now:HH:mm:ss}");

            text = ItemRegex.Replace(text, (Match m) =>
                $"{(inventory.Find((InventoryItem x) => x.Name == m.Groups[1].Value) ?? new InventoryItem()).Quantity}");

            text = ItemMaxRegex.Replace(text, (Match m) =>
            $"{(inventory.Find((InventoryItem x) => x.Name == m.Groups[1].Value) ?? new InventoryItem()).MaxStack}");

            text = RepXpRegex.Replace(text, (Match m) =>
          $"{(factions.Find((Faction x) => x.Name == m.Groups[1].Value) ?? new Faction()).Rep}/" +
          $"{(factions.Find((Faction x) => x.Name == m.Groups[1].Value) ?? new Faction()).RequiredRep}");

            text = RepCurrentRegex.Replace(text, (Match m) =>
               $"{(factions.Find((Faction x) => x.Name == m.Groups[1].Value) ?? new Faction()).Rep}");

            text = RepRemainingRegex.Replace(text, (Match m) =>
                 $"{(factions.Find((Faction x) => x.Name == m.Groups[1].Value) ?? new Faction()).RemainingRep}");

            text = RepRequiredRegex.Replace(text, (Match m) =>
                $"{(factions.Find((Faction x) => x.Name == m.Groups[1].Value) ?? new Faction()).RequiredRep}");

            text = RepRankRegex.Replace(text, (Match m) =>
            $"{(factions.Find((Faction x) => x.Name == m.Groups[1].Value) ?? new Faction()).Rank}");

            text = RepTotalRegex.Replace(text, (Match m) =>
             $"{(factions.Find((Faction x) => x.Name == m.Groups[1].Value) ?? new Faction()).TotalRep}");

            text = IntValueRegex.Replace(text, (Match m) =>
             $"{Configuration.Tempvalues[m.Groups[1].Value]}");

            text = text + "\r\n";
            if (Debug)
                if (Clear)
                    LogForm.Instance.txtLogDebug.Clear();
                else
                    LogForm.Instance.AppendDebug(text);
            else if (Clear)
                LogForm.Instance.txtLogScript.Clear();
            else
                LogForm.Instance.AppendScript(text);
            return Task.CompletedTask;
        }

        public override string ToString()
        {
            string typetxt = Debug ? "Debug" : "Script";
            Clear = Text.Contains("{CLEAR}") ? true : false;
            return Clear ? $"Clear {typetxt}" : $"Log {typetxt}: {Text}";
        }
    }
}
