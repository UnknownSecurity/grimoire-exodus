using Grimoire.Tools;
using System.Threading.Tasks;
using Grimoire.Clients;

namespace Grimoire.Botting.Commands.Misc
{
    public class CmdSetFPS : IBotCommand
    {
        public int FPS
        {
            get;
            set;
        }

        public Task Execute(IBotEngine instance)
        {
            Flash.Call("SetFPS", FPS);
            ClientSession session = ClientSessionContext.Resolve();
            if (session != null)
            {
                session.OptionState.UseCustomFrameRate = true;
                session.OptionState.DesiredFramesPerSecond = FPS;
                session.OptionState.LastAppliedFramesPerSecond = FPS;
                session.OptionState.FrameRateManagedByPerformancePolicy = false;
            }
            return Task.CompletedTask;
        }

        public override string ToString()
        {
            return $"Set FPS: {FPS}";
        }
    }
}
