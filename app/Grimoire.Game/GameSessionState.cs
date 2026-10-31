using Grimoire.Clients;
using Grimoire.Botting;
using Grimoire.Game.Data;
using System;
using System.Collections.Generic;

namespace Grimoire.Game
{
    /// <summary>
    /// Mutable game data owned by exactly one client session. Legacy static
    /// game APIs resolve this container through ClientSessionContext.
    /// </summary>
    public sealed class GameSessionState
    {
        private static readonly GameSessionState LegacyState = new GameSessionState();
        private string username;
        private bool isLoggedIn;

        static GameSessionState()
        {
            LegacyState.Initialize();
        }

        public GameSessionState()
        {
            Bank = new Bank();
            Inventory = new Inventory();
            TempInventory = new TempInventory();
            House = new House();
            PlayerQuests = new Quests();
            Maps = new List<Map>();
            CurrentMap = new Map();
            Shops = new List<ShopInfo>();
            CurrentShop = new ShopInfo();
            Drops = new List<InventoryItem>();
            CurrentServer = new Server();
            WorldQuests = new List<Quest>();
            Players = new Dictionary<string, PlayerInfo>();
            PlayersSyncRoot = new object();
            LoadedShops = new List<ShopInfo>();
            PlayerAuras = new List<Aura>();
            EnemyAuras = new List<Aura>();
            AuraSyncRoot = new object();
            SkillSet = new Dictionary<string, int>();
            BotQuests = new List<int>();
            TempValues = new Dictionary<string, int>();
            TempVariables = new Dictionary<string, string>();
            BotConfigurations = new Dictionary<int, Configuration>();
            BotOldIndexes = new Dictionary<int, int>();
            BotDropsInInventory = new List<string>();
            DefaultConfiguration = new Configuration();
        }

        public static GameSessionState Current
        {
            get
            {
                ClientSession session = ClientSessionContext.Resolve();
                return session?.GameState ?? LegacyState;
            }
        }

        public Bank Bank { get; }

        public Inventory Inventory { get; }

        public TempInventory TempInventory { get; }

        public House House { get; }

        public Quests PlayerQuests { get; }

        public int CharUserId { get; set; }

        public string Username
        {
            get { return username; }
            set
            {
                if (string.Equals(username, value, StringComparison.Ordinal))
                    return;

                username = value;
                IdentityChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsLoggedIn
        {
            get { return isLoggedIn; }
            set
            {
                if (isLoggedIn == value)
                    return;

                isLoggedIn = value;
                LoginStateChanged?.Invoke(this, EventArgs.Empty);
                IdentityChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler IdentityChanged;

        public event EventHandler LoginStateChanged;

        public List<Map> Maps { get; set; }

        public Map CurrentMap { get; set; }

        public List<ShopInfo> Shops { get; set; }

        public ShopInfo CurrentShop { get; set; }

        public List<InventoryItem> Drops { get; set; }

        public Server CurrentServer { get; set; }

        public List<Quest> WorldQuests { get; set; }

        public Dictionary<string, PlayerInfo> Players { get; set; }

        internal object PlayersSyncRoot { get; }

        public List<ShopInfo> LoadedShops { get; set; }

        public DropStack DropStack { get; private set; }

        public int MapId { get; set; }

        public string MapArea { get; set; }

        public List<Server> Servers { get; set; }

        public List<Aura> PlayerAuras { get; set; }

        public List<Aura> EnemyAuras { get; set; }

        internal object AuraSyncRoot { get; }

        internal Action<InventoryItem> ItemDropped;

        internal Action<ShopInfo> ShopLoaded;

        internal Action<Aura> AuraCountdown;

        public BotData.State BotState { get; set; } = BotData.State.Others;

        public string BotMap { get; set; }

        public string BotCell { get; set; }

        public string BotPad { get; set; }

        public string BotSkill { get; set; }

        public Dictionary<string, int> SkillSet { get; set; }

        public List<int> BotQuests { get; set; }

        public Dictionary<string, int> TempValues { get; }

        public Dictionary<string, string> TempVariables { get; }

        public Dictionary<int, Configuration> BotConfigurations { get; }

        public Dictionary<int, int> BotOldIndexes { get; }

        public List<string> BotDropsInInventory { get; }

        public Configuration DefaultConfiguration { get; }

        public bool ActiveBot { get; set; }

        public bool BusyClient { get; set; }

        public bool HasLoadedQuests { get; set; }

        public bool ShouldUseSkill { get; set; }

        public object ChatCombatCommand { get; set; }

        public bool ChatCombatEnabled { get; set; }

        public bool ChatProvokeEnabled { get; set; }

        public string CustomCharacterName { get; set; }

        public string CustomGuildName { get; set; }

        public string CustomClassName { get; set; }

        internal void Initialize()
        {
            if (DropStack == null)
                DropStack = new DropStack();
        }
    }
}
