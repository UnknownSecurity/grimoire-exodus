using System.Threading.Tasks;
namespace Grimoire.Botting.Commands.Misc
{
    public class CmdStop : IBotCommand
    {
        public async Task Execute(IBotEngine instance)
        {
            instance.Stop();
            await Task.Delay(2000);
        }

        public override string ToString()
        {
            return "Stop bot";
        }
    }
}
