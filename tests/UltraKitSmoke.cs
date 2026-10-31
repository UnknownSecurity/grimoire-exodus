using AxShockwaveFlashObjects;
using DarkUI.Controls;
using Grimoire.Clients;
using Grimoire.Game;
using Grimoire.Game.Data;
using Grimoire.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

internal static class UltraKitSmoke
{
    private static Assembly clientAssembly;

    [STAThread]
    private static int Main()
    {
        try
        {
            clientAssembly = typeof(ClientSession).Assembly;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveClientAssembly;
            Type rootType = clientAssembly.GetType("Grimoire.UI.Root", true);
            Type formType = clientAssembly.GetType("Grimoire.UI.UltraKitForm", true);

            VerifyToolsEntry(rootType);
            VerifyCharacterPageClassParser();
            VerifyFormAndSessionIsolation(formType);

            Console.WriteLine("PASS: Ultra Kit is available under Tools, renders live encounter rows, and remains client-session isolated.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void VerifyCharacterPageClassParser()
    {
        Type lookupType = clientAssembly.GetType(
            "Grimoire.UI.UltraCharacterClassLookup",
            true);
        MethodInfo parse = lookupType.GetMethod(
            "ParseClassFromCharacterPage",
            BindingFlags.Static | BindingFlags.NonPublic);
        Require(parse != null, "The Ultra Kit character-page class parser was not found.");

        const string fixture = "<param name=\"FlashVars\" value=\"&amp;strName=RemoteHero"
            + "&amp;strClassName=Lord%20of%20Order&amp;strClassFile=class.swf\" />";
        string parsed = Convert.ToString(parse.Invoke(null, new object[] { fixture }));
        Require(parsed == "Lord of Order",
            "Ultra Kit did not parse an out-of-cell player's class from the official character page.");
        Require(string.IsNullOrEmpty(Convert.ToString(parse.Invoke(null, new object[] { "<html></html>" }))),
            "Ultra Kit invented a class when the official character page did not expose one.");

        object lookup = Activator.CreateInstance(lookupType, true);
        try
        {
            MethodInfo remember = lookupType.GetMethod("Remember", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo resolve = lookupType.GetMethod("ResolveOrQueue", BindingFlags.Instance | BindingFlags.Public);
            Require(remember != null && resolve != null, "The Ultra Kit class cache seam was not found.");
            remember.Invoke(lookup, new object[] { "RemoteHero", parsed });
            Require(Convert.ToString(resolve.Invoke(lookup, new object[] { "RemoteHero" })) == "Lord of Order",
                "Ultra Kit did not retain a known class without starting another network lookup.");
        }
        finally
        {
            ((IDisposable)lookup).Dispose();
        }
    }

    private static void VerifyToolsEntry(Type rootType)
    {
        FieldInfo field = rootType.GetField(
            "ultraKitToolStripMenuItem",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(field != null, "The Tools menu does not retain its Ultra Kit item.");

        MethodInfo factory = rootType.GetMethod(
            "CreateUltraKitMenuItem",
            BindingFlags.Static | BindingFlags.NonPublic);
        Require(factory != null, "The Ultra Kit menu factory was not found.");
        using (ToolStripMenuItem item = (ToolStripMenuItem)factory.Invoke(null, null))
        {
            Require(item.Text == "Ultra Kit", "The Tools menu label is not Ultra Kit.");
            Require(!string.IsNullOrWhiteSpace(item.AccessibleDescription), "The Ultra Kit menu item has no accessible description.");
        }
    }

    private static void VerifyFormAndSessionIsolation(Type formType)
    {
        ConstructorInfo constructor = formType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null);
        MethodInfo capture = formType.GetMethod(
            "CaptureSnapshot",
            BindingFlags.Static | BindingFlags.NonPublic);
        MethodInfo apply = formType.GetMethod(
            "ApplySnapshot",
            BindingFlags.Instance | BindingFlags.NonPublic);
        PropertyInfo refreshActive = formType.GetProperty(
            "IsRefreshActive",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(constructor != null && capture != null && apply != null && refreshActive != null,
            "An Ultra Kit validation seam is missing.");

        FakeFlashBridge firstBridge = new FakeFlashBridge(
            "FirstHero",
            "Lord of Order",
            "FirstMapAlly",
            "Lord of Order",
            "First Boss",
                500000,
                false);
        FakeFlashBridge secondBridge = new FakeFlashBridge(
            "SecondHero",
            "ArchPaladin",
            "SecondMapAlly",
            "StoneCrusher",
            "Second Boss",
                700000,
                false);
        using (ClientSession first = new ClientSession("Client 01", firstBridge))
        using (ClientSession second = new ClientSession("Client 02", secondBridge))
        using (Form form = (Form)constructor.Invoke(null))
        {
            first.GameState.Username = "FirstHero";
            first.GameState.IsLoggedIn = true;
            first.GameState.MapArea = "ultra-first-1";
            second.GameState.Username = "SecondHero";
            second.GameState.IsLoggedIn = true;
            second.GameState.MapArea = "ultra-second-2";
            CachePlayerClassFromPacket(first, "FirstMapAlly", "Lord of Order", true);
            CachePlayerClassFromPacket(second, "SecondMapAlly", "StoneCrusher", true);

            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-32000, -32000);
            form.Opacity = 0.01d;
            form.Show();
            PumpMessages();
            Require(form.ClientSize.Height > form.ClientSize.Width,
                "Ultra Kit is not using a portrait companion-window layout.");
            Label status = FindControl<Label>(form, "Ultra Kit status");
            Require(status != null && status.Text.IndexOf("No active client", StringComparison.OrdinalIgnoreCase) >= 0,
                "Ultra Kit does not open safely without a live client.");
            Require((bool)refreshActive.GetValue(form), "Ultra Kit does not refresh while visible.");
            form.Hide();
            PumpMessages();
            Require(!(bool)refreshActive.GetValue(form), "Ultra Kit continues polling while hidden.");

            object firstSnapshot = capture.Invoke(null, new object[] { first });
            object secondSnapshot = capture.Invoke(null, new object[] { second });
            Require(GetString(firstSnapshot, "ClientName") == "FirstHero", "The first snapshot lost its client identity.");
            Require(GetString(secondSnapshot, "ClientName") == "SecondHero", "The second snapshot lost its client identity.");
            Require(GetString(firstSnapshot, "Location") == "ultra-first-1", "The first snapshot used another client's area.");
            Require(GetString(secondSnapshot, "Location") == "ultra-second-2", "The second snapshot used another client's area.");
            Require(ContainsName(firstSnapshot, "Players", "FirstHero"), "The first snapshot lost its current player.");
            Require(ContainsName(secondSnapshot, "Players", "SecondHero"), "The second snapshot lost its current player.");
            Require(ContainsName(firstSnapshot, "Players", "FirstMapAlly"),
                "Ultra Kit omitted another user reported by the first client's current-map callback.");
            Require(ContainsName(secondSnapshot, "Players", "SecondMapAlly"),
                "Ultra Kit omitted another user reported by the second client's current-map callback.");
            Require(!ContainsName(firstSnapshot, "Players", "SecondMapAlly"),
                "The first snapshot received another client's map user.");
            Require(!ContainsName(secondSnapshot, "Players", "FirstMapAlly"),
                "The second snapshot received another client's map user.");
            object firstMapAlly = GetNamedItem(firstSnapshot, "Players", "FirstMapAlly");
            Require(GetString(firstMapAlly, "Class") == "Lord of Order",
                "Ultra Kit did not merge the map user's class details.");
            Require(Convert.ToInt32(firstMapAlly.GetType().GetProperty("Level").GetValue(firstMapAlly)) == 95,
                "Ultra Kit did not retain the map user's level.");
            Require(GetString(firstMapAlly, "State") == "InCombat",
                "Ultra Kit did not retain the map user's combat status.");
            Require(GetString(firstMapAlly, "Cell") == "Boss",
                "Ultra Kit did not retain the map user's cell.");
            Require(GetString(firstMapAlly, "Pad") == "Left",
                "Ultra Kit did not retain the map user's pad.");
            Require(Convert.ToSingle(firstMapAlly.GetType().GetProperty("X").GetValue(firstMapAlly)) == 525F
                    && Convert.ToSingle(firstMapAlly.GetType().GetProperty("Y").GetValue(firstMapAlly)) == 310F,
                "Ultra Kit did not retain the map user's position.");
            Require(GetFirstName(firstSnapshot, "Monsters") == "First Boss", "The first snapshot received another client's monster.");
            Require(GetFirstName(secondSnapshot, "Monsters") == "Second Boss", "The second snapshot received another client's monster.");

            apply.Invoke(form, new[] { firstSnapshot });
            DarkDataGridView players = FindControl<DarkDataGridView>(form, "Ultra Kit players");
            DarkDataGridView monsters = FindControl<DarkDataGridView>(form, "Ultra Kit monsters");
            Require(players != null && monsters != null, "An Ultra Kit encounter table was not rendered.");
            Require(players.Rows.Count == 2, "The Ultra Kit player table did not render every user in the current map.");
            Require(monsters.Rows.Count == 1, "The Ultra Kit monster table did not render its snapshot.");
            Require(GridContainsName(players, "FirstHero"), "The player table lost the current account.");
            Require(GridContainsName(players, "FirstMapAlly"), "The player table omitted another user in the current map.");
            DataGridViewRow currentPlayerRow = FindGridRow(players, "FirstHero");
            DataGridViewRow mapAllyRow = FindGridRow(players, "FirstMapAlly");
            Color duplicateClassColor = Color.FromArgb(176, 104, 32);
            Require(currentPlayerRow.DefaultCellStyle.BackColor == duplicateClassColor
                    && mapAllyRow.DefaultCellStyle.BackColor == duplicateClassColor,
                "Ultra Kit did not highlight every player using a duplicated class.");
            Require(currentPlayerRow.Cells[1].ToolTipText.IndexOf("2 players", StringComparison.OrdinalIgnoreCase) >= 0,
                "The duplicate-class highlight does not explain the conflict.");
            Require(Convert.ToString(monsters.Rows[0].Cells[0].Value) == "First Boss", "The monster row contains the wrong client data.");
            Require(monsters.PointToScreen(Point.Empty).Y < players.PointToScreen(Point.Empty).Y,
                "Monsters and bosses are not shown above the player list.");

            VerifyRemoteClassEnrichment(formType, form, capture, apply, players);

            using (ClientSessionManager manager = new ClientSessionManager(2, true))
            {
                manager.Add(first);
                manager.Add(second);
                manager.SetActive(second);
                MethodInfo bind = formType.BaseType.GetMethod(
                    "BindClientSession",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Require(bind != null, "The Ultra Kit client-binding seam was not found.");
                bind.Invoke(form, new object[] { first });
                form.Show();
                PumpMessages();
                Require((bool)refreshActive.GetValue(form), "Ultra Kit did not resume refresh after reopening.");
                Require(Convert.ToString(players.Rows[0].Cells[0].Value) == "FirstHero",
                    "Ultra Kit followed the newly active tab instead of its bound client.");
                form.Hide();
            }
        }

        FakeFlashBridge omittedSelfBridge = new FakeFlashBridge(
            "CurrentHero",
            "Dragon of Time",
            "NearbyHero",
            "Verus DoomKnight",
            "Current Boss",
            900000,
            true);
        using (ClientSession omittedSelf = new ClientSession("Client 03", omittedSelfBridge))
        {
            omittedSelf.GameState.Username = "CurrentHero";
            omittedSelf.GameState.IsLoggedIn = true;
            CachePlayerClassFromPacket(omittedSelf, "NearbyHero", "Verus DoomKnight", false);
            object snapshot = capture.Invoke(null, new object[] { omittedSelf });
            Require(ContainsName(snapshot, "Players", "CurrentHero"),
                "Ultra Kit omitted the currently logged-in account when the nearby-player callback excluded it.");
            Require(ContainsName(snapshot, "Players", "NearbyHero"),
                "Ultra Kit omitted another user reported by the current-map callback.");
        }

        FakeFlashBridge legacyBridge = new FakeFlashBridge(
            "LegacyHero",
            "Void Highlord",
            "UnusedMapHero",
            "Unknown",
            "Legacy Boss",
            450000,
            false,
            false);
        using (ClientSession legacySession = new ClientSession("Client 04", legacyBridge))
        {
            legacySession.GameState.Username = "LegacyHero";
            legacySession.GameState.IsLoggedIn = true;
            object snapshot = capture.Invoke(null, new object[] { legacySession });
            Require(ContainsName(snapshot, "Players", "LegacyHero"),
                "Ultra Kit lost compatibility with clients that only expose the legacy player callback.");
        }
    }

    private static void VerifyRemoteClassEnrichment(
        Type formType,
        Form form,
        MethodInfo capture,
        MethodInfo apply,
        DarkDataGridView players)
    {
        FakeFlashBridge bridge = new FakeFlashBridge(
            "RosterOwner",
            "StoneCrusher",
            "RemoteRosterAlly",
            "Unknown",
            "Remote Boss",
            600000,
            false);
        using (ClientSession session = new ClientSession("Client 05", bridge))
        {
            session.GameState.Username = "RosterOwner";
            session.GameState.IsLoggedIn = true;
            session.GameState.MapArea = "ultra-remote-1";
            object snapshot = capture.Invoke(null, new object[] { session });
            object remotePlayer = GetNamedItem(snapshot, "Players", "RemoteRosterAlly");
            Require(remotePlayer != null, "Ultra Kit omitted the out-of-cell roster player.");
            Require(GetString(remotePlayer, "Class") == "Unknown",
                "The remote-player regression fixture unexpectedly started with a known class.");

            FieldInfo lookupField = formType.GetField(
                "characterClassLookup",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo enrich = formType.GetMethod(
                "ResolveUnknownPlayerClasses",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Require(lookupField != null && enrich != null,
                "The Ultra Kit class-enrichment seam was not found.");
            object lookup = lookupField.GetValue(form);
            MethodInfo remember = lookup.GetType().GetMethod(
                "Remember",
                BindingFlags.Instance | BindingFlags.Public);
            Require(remember != null, "The Ultra Kit class cache cannot retain a resolved class.");

            remember.Invoke(lookup, new object[] { "RemoteRosterAlly", "StoneCrusher" });
            enrich.Invoke(form, new[] { snapshot });
            Require(GetString(remotePlayer, "Class") == "StoneCrusher",
                "Ultra Kit did not enrich an out-of-cell player's unknown class.");

            apply.Invoke(form, new[] { snapshot });
            DataGridViewRow currentPlayerRow = FindGridRow(players, "RosterOwner");
            DataGridViewRow remotePlayerRow = FindGridRow(players, "RemoteRosterAlly");
            Color duplicateClassColor = Color.FromArgb(176, 104, 32);
            Require(currentPlayerRow != null && remotePlayerRow != null,
                "The enriched player roster was not rendered.");
            Require(currentPlayerRow.DefaultCellStyle.BackColor == duplicateClassColor
                    && remotePlayerRow.DefaultCellStyle.BackColor == duplicateClassColor,
                "The enriched remote class did not participate in duplicate-class highlighting.");
        }
    }

    private static string GetString(object instance, string propertyName)
    {
        return Convert.ToString(instance.GetType().GetProperty(propertyName).GetValue(instance));
    }

    private static void CachePlayerClassFromPacket(
        ClientSession session,
        string username,
        string className,
        bool multiple)
    {
        string player = "{\"uid\":2301,\"data\":{"
            + "\"uoName\":\"" + username.ToLowerInvariant() + "\","
            + "\"strUsername\":\"" + username + "\","
            + "\"strClassName\":\"" + className + "\","
            + "\"intHP\":4200,\"intHPMax\":5500,\"intMP\":87,"
            + "\"entID\":23,\"intLevel\":95,\"strFrame\":\"Boss\","
            + "\"strPad\":\"Left\",\"tx\":525,\"ty\":310,\"intState\":2}}";
        string playerData = "{\"uoName\":\"" + username.ToLowerInvariant() + "\","
            + "\"strUsername\":\"" + username + "\","
            + "\"strClassName\":\"" + className + "\","
            + "\"intHP\":4200,\"intHPMax\":5500,\"intMP\":87,"
            + "\"entID\":23,\"intLevel\":95,\"strFrame\":\"Boss\","
            + "\"strPad\":\"Left\",\"tx\":525,\"ty\":310,\"intState\":2}";
        string payload = multiple
            ? "{\"b\":{\"o\":{\"cmd\":\"initUserDatas\",\"a\":[" + player + "]}}}"
            : "{\"b\":{\"o\":{\"cmd\":\"initUserData\",\"uid\":2301,\"data\":"
                + playerData + "}}}";

        Type handlerType = clientAssembly.GetType(
            "Grimoire.Networking.Handlers.HandlerUserData",
            true);
        object handler = Activator.CreateInstance(handlerType);
        MethodInfo handle = handlerType.GetMethod("Handle", BindingFlags.Instance | BindingFlags.Public);
        Require(handle != null, "The live user-data handler is unavailable.");
        using (ClientSessionContext.Push(session))
            handle.Invoke(handler, new object[] { new JsonMessage(payload) });

        Require(session.GameState.Players.Values.Any(playerInfo =>
                string.Equals(playerInfo.Username, username, StringComparison.OrdinalIgnoreCase)
                && string.Equals(playerInfo.Class, className, StringComparison.Ordinal)),
            "The live user-data handler did not cache the player's class.");

        string updatedClass = className + " Test";
        string updatePayload = "{\"b\":{\"o\":{\"cmd\":\"updateClass\",\"uid\":2301,"
            + "\"sClassName\":\"" + updatedClass + "\"}}}";
        using (ClientSessionContext.Push(session))
            handle.Invoke(handler, new object[] { new JsonMessage(updatePayload) });
        Require(session.GameState.Players.Values.Any(playerInfo =>
                playerInfo.UserID == 2301
                && string.Equals(playerInfo.Class, updatedClass, StringComparison.Ordinal)),
            "The live class-change packet did not refresh the cached class.");

        string restorePayload = "{\"b\":{\"o\":{\"cmd\":\"updateClass\",\"uid\":2301,"
            + "\"sClassName\":\"" + className + "\"}}}";
        using (ClientSessionContext.Push(session))
            handle.Invoke(handler, new object[] { new JsonMessage(restorePayload) });
    }

    private static string GetFirstName(object snapshot, string collectionProperty)
    {
        IList list = snapshot.GetType().GetProperty(collectionProperty).GetValue(snapshot) as IList;
        Require(list != null && list.Count == 1, "Unexpected Ultra Kit snapshot count for " + collectionProperty + ".");
        return GetString(list[0], "Name");
    }

    private static bool ContainsName(object snapshot, string collectionProperty, string expectedName)
    {
        return GetNamedItem(snapshot, collectionProperty, expectedName) != null;
    }

    private static object GetNamedItem(object snapshot, string collectionProperty, string expectedName)
    {
        IList list = snapshot.GetType().GetProperty(collectionProperty).GetValue(snapshot) as IList;
        Require(list != null, "Ultra Kit snapshot did not expose " + collectionProperty + ".");
        foreach (object item in list)
        {
            if (string.Equals(GetString(item, "Name"), expectedName, StringComparison.Ordinal))
                return item;
        }
        return null;
    }

    private static bool GridContainsName(DarkDataGridView grid, string expectedName)
    {
        return FindGridRow(grid, expectedName) != null;
    }

    private static DataGridViewRow FindGridRow(DarkDataGridView grid, string expectedName)
    {
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (string.Equals(Convert.ToString(row.Cells[0].Value), expectedName, StringComparison.Ordinal))
                return row;
        }
        return null;
    }

    private static T FindControl<T>(Control parent, string accessibleName)
        where T : Control
    {
        foreach (Control child in parent.Controls)
        {
            T match = child as T;
            if (match != null && string.Equals(match.AccessibleName, accessibleName, StringComparison.Ordinal))
                return match;
            T nested = FindControl<T>(child, accessibleName);
            if (nested != null)
                return nested;
        }
        return null;
    }

    private static Assembly ResolveClientAssembly(object sender, ResolveEventArgs args)
    {
        AssemblyName requested = new AssemblyName(args.Name);
        return string.Equals(requested.Name, "Grimoire", StringComparison.OrdinalIgnoreCase)
            ? clientAssembly
            : null;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void PumpMessages()
    {
        for (int index = 0; index < 8; index++)
        {
            Application.DoEvents();
            Thread.Sleep(2);
        }
    }

    private sealed class FakeFlashBridge : IFlashBridge
    {
        private readonly string playerName;
        private readonly string playerClass;
        private readonly string mapPlayerName;
        private readonly string mapPlayerClass;
        private readonly string monsterName;
        private readonly int monsterHealth;
        private readonly bool omitCurrentPlayer;
        private readonly bool exposeMapRoster;

        public FakeFlashBridge(
            string playerName,
            string playerClass,
            string mapPlayerName,
            string mapPlayerClass,
            string monsterName,
            int monsterHealth,
            bool omitCurrentPlayer,
            bool exposeMapRoster = true)
        {
            this.playerName = playerName;
            this.playerClass = playerClass;
            this.mapPlayerName = mapPlayerName;
            this.mapPlayerClass = mapPlayerClass;
            this.monsterName = monsterName;
            this.monsterHealth = monsterHealth;
            this.omitCurrentPlayer = omitCurrentPlayer;
            this.exposeMapRoster = exposeMapRoster;
        }

        public AxShockwaveFlash Control { get { return null; } }
        public GameEngineKind Engine { get { return GameEngineKind.FlashActiveX; } }
        public Control HostControl { get { return null; } }
        public event Action<int> SwfLoadProgress;
        public event FlashBridgeCallHandler FlashCall;
        public event FlashBridgeErrorHandler FlashError;

        public void LoadMovie(byte[] swf) { }

        public T Call<T>(string function, params object[] args)
        {
            if (function == "Players" && typeof(T) == typeof(string))
            {
                if (omitCurrentPlayer)
                    return (T)(object)"{}";
                string json = "{\"" + playerName.ToLowerInvariant() + "\":{"
                    + "\"uoName\":\"" + playerName.ToLowerInvariant() + "\","
                    + "\"strUsername\":\"" + playerName + "\","
                    + "\"strClassName\":\"" + playerClass + "\","
                    + "\"intHP\":5000,\"intHPMax\":6000,\"intMP\":100,"
                    + "\"entID\":17,\"intLevel\":100,\"strFrame\":\"Enter\","
                    + "\"strPad\":\"Spawn\",\"tx\":400,\"ty\":300,\"intState\":1}}";
                return (T)(object)json;
            }
            if (function == "Class" && typeof(T) == typeof(string))
                return (T)(object)playerClass;
            if (function == "Cell" && typeof(T) == typeof(string))
                return (T)(object)"Enter";
            if (function == "Pad" && typeof(T) == typeof(string))
                return (T)(object)"Spawn";
            if (function == "Position" && typeof(T) == typeof(float[]))
                return (T)(object)new[] { 400F, 300F };
            if (function == "Health" && typeof(T) == typeof(int))
                return (T)(object)5000;
            if (function == "HealthMax" && typeof(T) == typeof(int))
                return (T)(object)6000;
            if (function == "Mana" && typeof(T) == typeof(int))
                return (T)(object)100;
            if (function == "UserID" && typeof(T) == typeof(int))
                return (T)(object)17;
            if (function == "Level" && typeof(T) == typeof(int))
                return (T)(object)100;
            if (function == "State" && typeof(T) == typeof(int))
                return (T)(object)1;
            if (function == "IsAfk" && typeof(T) == typeof(bool))
                return (T)(object)false;
            return default(T);
        }

        public object Call(string function, Type resultType, params object[] args)
        {
            return Call<string>(function, args);
        }

        public T CallWithStringArguments<T>(string function, params string[] args)
        {
            if (exposeMapRoster && function == "PlayersInMap" && typeof(T) == typeof(List<string>))
            {
                return (T)(object)new List<string>
                {
                    playerName,
                    mapPlayerName
                };
            }
            if (exposeMapRoster && function == "AvatarsInMap" && typeof(T) == typeof(List<Avatar>))
            {
                return (T)(object)new List<Avatar>
                {
                    new Avatar
                    {
                        Name = mapPlayerName.ToLowerInvariant(),
                        Username = mapPlayerName,
                        HP = 4200,
                        MaxHP = 5500,
                        MP = 87,
                        AFK = false,
                        EntID = 23,
                        Level = 95,
                        Cell = "Boss",
                        Pad = "Left",
                        CurrentState = 2
                    }
                };
            }
            if (exposeMapRoster
                && function == "selectArrayObjects"
                && typeof(T) == typeof(List<Avatar>)
                && args.Length == 2
                && args[0] == "world.avatars"
                && args[1] == "objData")
            {
                return (T)(object)new List<Avatar>
                {
                    new Avatar
                    {
                        Name = mapPlayerName.ToLowerInvariant(),
                        Username = mapPlayerName,
                        Class = string.Empty,
                        EntID = 23,
                        Level = 95,
                        X = 525F,
                        Y = 310F
                    }
                };
            }
            if (function == "MonstersInMap" && typeof(T) == typeof(List<Monster>))
            {
                return (T)(object)new List<Monster>
                {
                    new Monster
                    {
                        Name = monsterName,
                        Race = "Chaos",
                        Id = 42,
                        Level = 100,
                        CurrentState = 1,
                        Health = monsterHealth - 1000,
                        MaxHealth = monsterHealth
                    }
                };
            }
            return Call<T>(function, args);
        }

        public string CallString(string function, params object[] args) { return Call<string>(function, args); }
        public string CallGameFunction(string path, params object[] args) { return null; }
        public string GetGameObject(string path) { return null; }
        public string GetGameObjectStatic(string path) { return null; }
        public T GetGameObject<T>(string path, T defaultValue = default(T)) { return defaultValue; }
        public void SetGameObject(string path, object value) { }

        public void Dispose()
        {
            SwfLoadProgress = null;
            FlashCall = null;
            FlashError = null;
        }
    }
}
