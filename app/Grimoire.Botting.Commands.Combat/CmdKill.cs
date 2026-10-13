using Grimoire.Game;
using Grimoire.Game.Data;
using Grimoire.UI;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grimoire.Botting.Commands.Combat
{
    public class CmdKill : IBotCommand
    {
        public string Monster
        {
            get;
            set;
        }

        public bool Packet
        {
            get;
            set;
        }

        public async Task Execute(IBotEngine instance)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                Task skillTask = null;
                try
                {
                    BotData.BotState = BotData.State.Combat;
                    BotUtilities.MoveToBotCell();
                    await instance.WaitUntil(
                        () => World.IsMonsterAvailable(instance.Value(Monster)),
                        () => instance.IsRunning && !cts.IsCancellationRequested,
                        16,
                        500);
                    if (!instance.IsRunning || !World.IsMonsterAvailable(instance.Value(Monster)))
                        return;

                    if (instance.Configuration.WaitForAllSkills)
                    {
                        await Task.Delay(Player.AllSkillsAvailable, cts.Token);
                    }
                    Player.AttackMonster(instance.Value(Monster));
                    skillTask = UseSkills(instance, cts.Token);
                    await instance.WaitUntil(
                        () => !Player.HasTarget,
                        () => instance.IsRunning && !cts.IsCancellationRequested,
                        -1,
                        500);
                }
                finally
                {
                    cts.Cancel();
                    if (skillTask != null)
                    {
                        try
                        {
                            await skillTask;
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    }
                    Player.CancelTarget();
                }
            }
        }

        private async Task UseSkills(IBotEngine instance, CancellationToken token)
        {
            var monster = instance.Value(Monster);
            int ClassIndex = (BotData.SkillSet != null && BotData.SkillSet.ContainsKey("[" + BotData.BotSkill + "]")) ? (BotData.SkillSet["[" + BotData.BotSkill + "]"]) + 1 : -1;
            int count = instance.Configuration.Skills.Count - 1;
            int index = ClassIndex;
            while (instance.IsRunning && !token.IsCancellationRequested)
            {
                if (!BotUtilities.ShouldUseSkill)
                {
                    Player.CancelAutoAttack();
                    await instance.WaitUntil(() => BotUtilities.ShouldUseSkill, () => instance.IsRunning && !token.IsCancellationRequested, 30, 500);
                }
                if (!Player.IsLoggedIn)
                {
                    return;
                }
                if (!Player.IsAlive)
                {
                    Player.CancelTarget();
                    return;
                }
                if (Bot.ProtectedMonsters.ContainsKey(monster) && World.IsMonsterAvailable(Bot.ProtectedMonsters[monster]))
                {
                    Player.AttackMonster(Bot.ProtectedMonsters[monster]);
                }
                if (ClassIndex != -1)
                {
                    Skill skill = instance.Configuration.Skills[index];
                    if (skill.Type == Skill.SkillType.Label)
                    {
                        index = ClassIndex;
                        continue;
                    }
                    if (instance.Configuration.WaitForSkill)
                    {
                        BotManager.Instance.OnSkillIndexChanged(index);
                        await Task.Delay(Player.SkillAvailable(skill.Index), token);
                    }
                    if (skill.Type == Skill.SkillType.Safe)
                    {
                        skill.UseSafeSkill();
                    }
                    else
                    {
                        Player.UseSkill(skill.Index);
                    }
                    index = index >= count ? ClassIndex : index + 1;
                    await Task.Delay(instance.Configuration.SkillDelay, token);
                }
                else
                {
                    for (int i = 1; i <= 5; i++)
                    {
                        if (i == 5 && Player.Inventory.Items.FirstOrDefault((InventoryItem item) => item.IsEquipped && item.Category == "Item") == null)
                        {
                            break;
                        }
                        if (Player.IsSkillAvailable(i.ToString()))
                        {
                            Player.UseSkill(i.ToString());
                        }
                        await Task.Delay(instance.Configuration.SkillDelay, token);
                    }
                }
            }
        }

        public override string ToString()
        {
            return "Kill: " + Monster;
        }
    }
}
