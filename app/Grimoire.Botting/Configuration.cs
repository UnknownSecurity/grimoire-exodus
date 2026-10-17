using Grimoire.Game.Data;
using Grimoire.Game;
using System.Collections.Generic;

namespace Grimoire.Botting
{
    public class Configuration
    {
        public static Configuration Instance => GameSessionState.Current.DefaultConfiguration;

        public List<IBotCommand> Commands
        {
            get;
            set;
        }

        public List<Skill> Skills
        {
            get;
            set;
        }

        public List<Quest> Quests
        {
            get;
            set;
        }

        public string Author
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public List<InventoryItem> Boosts
        {
            get;
            set;
        }

        public List<string> Drops
        {
            get;
            set;
        }

        public Server Server
        {
            get;
            set;
        }

        public int SkillDelay
        {
            get;
            set;
        }

        public bool ExitCombatBeforeRest
        {
            get;
            set;
        }

        public bool ExitCombatBeforeQuest
        {
            get;
            set;
        }

        public bool ExitCombatBeforeStop
        {
            get;
            set;
        } = true;

        public bool EnablePickup
        {
            get;
            set;
        }

        public bool EnableRejection
        {
            get;
            set;
        }

        public bool EnablePickupAll
        {
            get;
            set;
        }

        public bool EnablePickupAcTagged
        {
            get;
            set;
        }

        public bool EnableRejectAll
        {
            get;
            set;
        }

        public bool AutoRelogin
        {
            get;
            set;
        }

        public int RelogDelay
        {
            get;
            set;
        }

        public bool RelogRetryUponFailure
        {
            get;
            set;
        }

        public int BotDelay
        {
            get;
            set;
        }

        public bool WaitForAllSkills
        {
            get;
            set;
        } = false;

        public bool WaitForSkill
        {
            get;
            set;
        }

        public bool SkipDelayIndexIf
        {
            get;
            set;
        }

        public bool InfiniteAttackRange
        {
            get;
            set;
        }

        public bool ProvokeMonsters
        {
            get;
            set;
        } = false;

        public bool ProvokeAllMonster
        {
            get;
            set;
        } = false;

        public bool Untarget
        {
            get;
            set;
        }

        public bool EnemyMagnet
        {
            get;
            set;
        }

        public bool LagKiller
        {
            get;
            set;
        }

        public bool HidePlayers
        {
            get;
            set;
        }

        public bool SkipCutscenes
        {
            get;
            set;
        }

        public bool DisableAnimations
        {
            get;
            set;
        }

        public bool AutoSaveState
        {
            get;
            set;
        }

        public int WalkSpeed
        {
            get;
            set;
        }

        public List<string> NotifyUponDrop
        {
            get;
            set;
        }

        public bool RestIfMp
        {
            get;
            set;
        }

        public bool RestIfHp
        {
            get;
            set;
        }

        public int RestMp
        {
            get;
            set;
        }

        public int RestHp
        {
            get;
            set;
        }

        public bool RestartUponDeath
        {
            get;
            set;
        }

        public List<string> Items
        {
            get;
            set;
        }

        public bool UnbankOnStart
        {
            get;
            set;
        }

        public bool BankOnStop
        {
            get;
            set;
        }

        public bool NotifyItemOnPickup
        {
            get;
            set;
        }

        public bool RelogUponQuestFailure
        {
            get;
            set;
        }

        public bool RelogUponItemBuyFailure
        {
            get;
            set;
        }

        public bool AFK
        {
            get;
            set;
        }

        public int DropDelay
        {
            get;
            set;
        }

        public static Dictionary<string, int> Tempvalues => GameSessionState.Current.TempValues;
        public static Dictionary<string, string> Tempvariable => GameSessionState.Current.TempVariables;

        public Configuration()
        {
            Commands = new List<IBotCommand>();
            Skills = new List<Skill>();
            Quests = new List<Quest>();
            Boosts = new List<InventoryItem>();
            Drops = new List<string>();
            Items = new List<string>();
            NotifyUponDrop = new List<string>();
        }
    }
}
