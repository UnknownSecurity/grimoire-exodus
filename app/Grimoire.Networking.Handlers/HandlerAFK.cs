using Grimoire.Game;
using Grimoire.Botting;
using Grimoire.UI;

namespace Grimoire.Networking.Handlers
{
    public class HandlerAFK : IXtMessageHandler
    {
        public string[] HandledCommands
        {
            get;
        } = new string[1]
        {
            "afk"
        };

        public void Handle(XtMessage message)
        {
            if (message?.Arguments == null || message.Arguments.Length <= 5)
                return;

            switch (message.Arguments[5])
            {
                case "true":
                    if (Bot.Instance.IsRunning
                        && Bot.Instance.Configuration != null
                        && Bot.Instance.Configuration.AutoRelogin
                        && Bot.Instance.Configuration.AFK)
                    {
                        Player.Logout();
                    }
                    //if (OptionsManager._antiAfk)
                    //{
                    //    await Task.Delay(750);
                    //    Proxy.Instance.SendToServer("%xt%zm%afk%1%false%");
                    //}
                    break;
            }
        }
    }
}
