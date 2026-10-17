using Grimoire.Botting;
using Grimoire.Clients;
using Grimoire.Game;
using Grimoire.Game.Data;
using Grimoire.Tools;
using Grimoire.UI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Grimoire.Networking.Handlers
{
    public class HandlerLogin : IXtMessageHandler
    {
        public string[] HandledCommands
        {
            get;
        } = new string[3]
        {
            "loginResponse",
            "bankToInv",
            "bankFromInv"
        };

        private readonly CancellationToken sessionToken;

        private List<string> eventModifiers = new List<string>
        {
            "ModifyBtnSend",
            "OnShopEvents",
            "OnInventoryEvents",
            "OnHouseInventoryEvents",
            "OnPortraitTargetEvents",
            "OnMenuBtnItemEvents"
        };

        public HandlerLogin()
        {
            ClientSession session = ClientSessionContext.Resolve();
            sessionToken = session == null ? CancellationToken.None : session.LifetimeToken;
        }

        public void Handle(XtMessage message)
        {
            HandlerTask.Observe(HandleAsync(message), nameof(HandlerLogin));
        }

        private async Task HandleAsync(XtMessage message)
        {
            if (message.Command == "loginResponse")
            {
                LogForm.Instance.AppendDebug($"[{DateTime.Now:hh:mm:ss}] Logged in to {World.ServerName()} server.\r\n");
                await Bot.Instance.WaitUntil(() => AutoRelogin.GameLabel, () => !sessionToken.IsCancellationRequested, 10, 1000);
                Player.Quests.LoadedQuests.Clear();
                await Bot.Instance.WaitUntil(() => !World.uiLock, () => !sessionToken.IsCancellationRequested, -1, 500);
                Player.Bank.Load();
                World.SendClientPacket("%xt%server%-1%Type \"/help\" without the quotation marks to bring information about Chat Commands into the chat.%");
                InstallSettings();
                OptionsManager.AntiAfk();
                Player.EnableChat();
                OptionsManager.DisableDeathAds();
                BotUtilities.ShouldUseSkill = true;
                await Bot.Instance.WaitUntil(() => Player.Bank.IsBankLoaded, () => !sessionToken.IsCancellationRequested, 20, 500);
            }
            Player.Bank.SavedItems = Flash.Call<List<InventoryItem>>("GetBankItems", new string[0]);
        }

        public void InstallSettings()
        {
            foreach (string modifier in eventModifiers)
            {
                Flash.Call(modifier, new string[0]);
            }
            if (Root.Instance.rtbPing.Visible)
            {
                Flash.Call("StartLatencyTimer", new string[0]);
            }
            if (Travel.Instance.chkCustomChatTrigger.Checked)
            {
                Flash.Call("LoadTravelTriggers", BotClientConfig.Instance.GetValue<string>("customTravels"));
            }
        }
    }
}
