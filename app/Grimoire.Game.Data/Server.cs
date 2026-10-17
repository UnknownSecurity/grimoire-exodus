using Grimoire.Tools;
using Grimoire.Game;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Grimoire.Game.Data
{
    public class Server
    {
        [JsonConverter(typeof(BoolConverter))]
        [JsonProperty("bOnline")]
        public bool IsOnline
        {
            get;
            set;
        }

        [JsonProperty("bCount")]
        public int PlayerCount
        {
            get;
            set;
        }

        [JsonProperty("iMax")]
        public int MaxCount
        {
            get;
            set;
        }

        [JsonProperty("sName")]
        public string Name
        {
            get;
            set;
        }

        [JsonProperty("sLang")]
        public string Language
        {
            get;
            set;
        }

        [JsonConverter(typeof(BoolConverter))]
        [JsonProperty("bUpg")]
        public bool IsMemberOnly
        {
            get;
            set;
        }

        [JsonConverter(typeof(BoolConverter))]
        [JsonProperty("iChat")]
        public bool IsChatRestricted
        {
            get;
            set;
        }

        [JsonProperty("iPort")]
        public int Port
        {
            get;
            set;
        }

        [JsonProperty("sIP")]
        public string Ip
        {
            get;
            set;
        }

        public static List<Server> List
        {
            get { return GameSessionState.Current.Servers; }
            set { GameSessionState.Current.Servers = value; }
        }

        public override string ToString()
        {
            return this.Name;
        }

        public bool ShouldSerializeIsOnline()
        {
            return false;
        }

        public bool ShouldSerializePlayerCount()
        {
            return false;
        }

        public bool ShouldSerializeIsMemberOnly()
        {
            return false;
        }

        public bool ShouldSerializeIsChatRestricted()
        {
            return false;
        }
    }
}
