using DarkUI.Forms;
using Grimoire.UI;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Grimoire.Botting.Commands.Misc
{
    public class CmdStopBotWithMessage : IBotCommand
    {
        public string Message
        {
            get;
            set;
        }

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
                DarkMessageBox.Show
                (
                    new Form { TopMost = true, StartPosition = FormStartPosition.CenterScreen },
                    $"{(string.IsNullOrWhiteSpace(Message) ? "Bot has stopped!" : Message)}", "Bot has stopped!", MessageBoxIcon.Information
                );
            });
        }

        public override string ToString()
        {
            return "Stop bot with message";
        }
    }
}
