using Grimoire.UI;
using System.Threading.Tasks;
namespace Grimoire.Botting.Commands.Misc
{
    public class CmdStop : IBotCommand
    {
        public async Task Execute(IBotEngine instance)
        {
            await BotManager.Instance.RunOnUiThreadAsync(() =>
            {
                BotManager.Instance.btnBotStart.Enabled = false;
                Root.Instance.stopToolStripMenuItem.Enabled = false;
                BotManager.Instance.ActiveBotEngine.Stop();
                BotManager.Instance.CustomCommandToggle(true);
                BotManager.Instance.SelectionModeToggle(false);
                BotManager.Instance.BotStateChanged(IsRunning: false);
            });
            await Task.Delay(2000);
            await BotManager.Instance.RunOnUiThreadAsync(() =>
            {
                Root.Instance.BotStateChanged(IsRunning: false);
                BotManager.Instance.btnBotStart.Enabled = true;
            });
        }

        public override string ToString()
        {
            return "Stop bot";
        }
    }
}
