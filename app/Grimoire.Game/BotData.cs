using System.Collections.Generic;

namespace Grimoire.Game
{
    public static class BotData
    {
        public enum State
        {
            Others,
            Transaction,
            Move,
            Combat,
            Rest,
            Quest
        }

        public static State BotState
        {
            get { return GameSessionState.Current.BotState; }
            set { GameSessionState.Current.BotState = value; }
        }

        public static string BotMap
        {
            get { return GameSessionState.Current.BotMap; }
            set { GameSessionState.Current.BotMap = value; }
        }

        public static string BotCell
        {
            get { return GameSessionState.Current.BotCell; }
            set { GameSessionState.Current.BotCell = value; }
        }

        public static string BotPad
        {
            get { return GameSessionState.Current.BotPad; }
            set { GameSessionState.Current.BotPad = value; }
        }

        public static string BotSkill
        {
            get { return GameSessionState.Current.BotSkill; }
            set { GameSessionState.Current.BotSkill = value; }
        }

        public static Dictionary<string, int> SkillSet
        {
            get { return GameSessionState.Current.SkillSet; }
            set { GameSessionState.Current.SkillSet = value; }
        }

        public static List<int> BotQuests
        {
            get { return GameSessionState.Current.BotQuests; }
            set { GameSessionState.Current.BotQuests = value; }
        }

    }
}
