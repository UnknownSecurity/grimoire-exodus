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
            instance.Stop();
            await Task.Delay(2000);
            await BotManager.Instance.RunOnUiThreadAsync(() =>
            {
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
