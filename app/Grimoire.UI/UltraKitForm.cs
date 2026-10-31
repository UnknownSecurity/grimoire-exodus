using DarkUI.Controls;
using Grimoire.Clients;
using Grimoire.Game;
using Grimoire.Game.Data;
using Grimoire.Tools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Grimoire.UI
{
    [DesignerCategory("Code")]
    internal sealed class UltraKitForm : GrimoireForm
    {
        private static readonly Color BackgroundColor = Color.FromArgb(28, 32, 40);
        private static readonly Color CardColor = Color.FromArgb(36, 40, 50);
        private static readonly Color SecondaryTextColor = Color.FromArgb(170, 174, 188);
        private static readonly Color AccentColor = Color.MediumPurple;
        private static readonly Color DuplicateClassColor = Color.FromArgb(176, 104, 32);
        private static readonly Color DuplicateClassSelectionColor = Color.FromArgb(204, 122, 38);

        private readonly System.Windows.Forms.Timer refreshTimer;
        private readonly Label statusLabel;
        private readonly Label locationLabel;
        private readonly Label playerCountLabel;
        private readonly Label monsterCountLabel;
        private readonly DarkDataGridView playersGrid;
        private readonly DarkDataGridView monstersGrid;
        private readonly DarkTextBox detailsBox;
        private readonly UltraCharacterClassLookup characterClassLookup;
        private bool applyingSnapshot;
        private bool refreshInProgress;

        public UltraKitForm()
        {
            Text = "Ultra Kit";
            Icon = (Icon)global::Properties.Resources.GrimoireIcon.Clone();
            BackColor = BackgroundColor;
            ForeColor = Color.Gainsboro;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            MinimumSize = new Size(400, 560);
            ClientSize = new Size(440, 720);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;

            statusLabel = CreateSummaryValue("No client selected");
            statusLabel.AccessibleName = "Ultra Kit status";
            locationLabel = CreateSummaryValue("—");
            locationLabel.AccessibleName = "Ultra Kit location";
            playerCountLabel = CreateSummaryValue("0");
            monsterCountLabel = CreateSummaryValue("0");
            playersGrid = CreateGrid("Ultra Kit players");
            monstersGrid = CreateGrid("Ultra Kit monsters");
            detailsBox = CreateDetailsBox();
            characterClassLookup = new UltraCharacterClassLookup();

            ConfigurePlayerColumns(playersGrid);
            ConfigureMonsterColumns(monstersGrid);
            Controls.Add(BuildLayout());

            playersGrid.SelectionChanged += Grid_SelectionChanged;
            monstersGrid.SelectionChanged += Grid_SelectionChanged;
            VisibleChanged += UltraKitForm_VisibleChanged;
            FormClosing += UltraKitForm_FormClosing;

            refreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            refreshTimer.Tick += RefreshTimer_Tick;
            ApplySnapshot(UltraKitSnapshot.Unavailable("No client selected"));
        }

        internal override bool SupportsClientSessionBinding => true;

        internal bool IsRefreshActive => refreshTimer.Enabled;

        internal static UltraKitSnapshot CaptureSnapshot(ClientSession session)
        {
            if (session == null || session.IsDisposed)
                return UltraKitSnapshot.Unavailable("No active client");

            if (!session.GameState.IsLoggedIn)
            {
                return UltraKitSnapshot.Unavailable(
                    "Waiting for " + session.DisplayName + " to enter the game",
                    session.DisplayName,
                    GetLocation(session));
            }

            List<UltraPlayerSnapshot> players = new List<UltraPlayerSnapshot>();
            List<UltraMonsterSnapshot> monsters = new List<UltraMonsterSnapshot>();
            string warning = null;

            using (ClientSessionContext.Push(session))
            {
                try
                {
                    bool hasLivePlayerData;
                    List<PlayerInfo> livePlayerList = CaptureMapPlayers(session, out hasLivePlayerData);
                    if (!hasLivePlayerData)
                        warning = "Player data is temporarily unavailable";
                    AddCurrentPlayerIfMissing(session, livePlayerList);
                    players = livePlayerList
                        .Select(UltraPlayerSnapshot.FromPlayer)
                        .OrderBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                catch (Exception)
                {
                    warning = "Player data is temporarily unavailable";
                    List<PlayerInfo> fallbackPlayers = SafePlayerFallback(session)
                        .Where(player => player != null)
                        .ToList();
                    AddCurrentPlayerIfMissing(session, fallbackPlayers);
                    players = fallbackPlayers
                        .Select(UltraPlayerSnapshot.FromPlayer)
                        .OrderBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                try
                {
                    List<Monster> liveMonsters = World.MonstersInMap;
                    IEnumerable<Monster> source = liveMonsters ?? SafeMonsterFallback(session);
                    monsters = source
                        .Where(monster => monster != null)
                        .Select(UltraMonsterSnapshot.FromMonster)
                        .OrderByDescending(monster => monster.MaxHealth)
                        .ThenBy(monster => monster.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                catch (Exception)
                {
                    warning = warning == null
                        ? "Monster data is temporarily unavailable"
                        : "Some live data is temporarily unavailable";
                    monsters = SafeMonsterFallback(session)
                        .Where(monster => monster != null)
                        .Select(UltraMonsterSnapshot.FromMonster)
                        .OrderByDescending(monster => monster.MaxHealth)
                        .ThenBy(monster => monster.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
            }

            string status = warning ?? "Live data · updated " + DateTime.Now.ToString("HH:mm:ss");
            return new UltraKitSnapshot(
                session.DisplayName,
                GetLocation(session),
                status,
                true,
                players,
                monsters);
        }

        internal void ApplySnapshot(UltraKitSnapshot snapshot)
        {
            if (snapshot == null)
                snapshot = UltraKitSnapshot.Unavailable("No live data");

            applyingSnapshot = true;
            try
            {
                statusLabel.Text = snapshot.Status;
                statusLabel.ForeColor = snapshot.Connected ? Color.PaleGreen : SecondaryTextColor;
                locationLabel.Text = string.IsNullOrWhiteSpace(snapshot.Location) ? "—" : snapshot.Location;
                playerCountLabel.Text = snapshot.Players.Count.ToString();
                monsterCountLabel.Text = snapshot.Monsters.Count.ToString();
                Dictionary<string, int> classCounts = snapshot.Players
                    .Select(player => NormalizeClassName(player.Class))
                    .Where(className => !string.IsNullOrEmpty(className))
                    .GroupBy(className => className, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

                playersGrid.Rows.Clear();
                foreach (UltraPlayerSnapshot player in snapshot.Players)
                {
                    int rowIndex = playersGrid.Rows.Add(
                        player.Name,
                        player.Class,
                        player.Level,
                        FormatPair(player.Health, player.MaxHealth),
                        player.State);
                    DataGridViewRow row = playersGrid.Rows[rowIndex];
                    row.Tag = player;
                    string normalizedClass = NormalizeClassName(player.Class);
                    int duplicateCount;
                    if (!string.IsNullOrEmpty(normalizedClass)
                        && classCounts.TryGetValue(normalizedClass, out duplicateCount)
                        && duplicateCount > 1)
                    {
                        row.DefaultCellStyle.BackColor = DuplicateClassColor;
                        row.DefaultCellStyle.SelectionBackColor = DuplicateClassSelectionColor;
                        row.DefaultCellStyle.ForeColor = Color.White;
                        row.DefaultCellStyle.SelectionForeColor = Color.White;
                        row.Cells[1].ToolTipText = duplicateCount
                            + " players are using " + player.Class + ".";
                    }
                }

                monstersGrid.Rows.Clear();
                foreach (UltraMonsterSnapshot monster in snapshot.Monsters)
                {
                    int rowIndex = monstersGrid.Rows.Add(
                        monster.Name,
                        monster.Level,
                        FormatPair(monster.Health, monster.MaxHealth),
                        monster.State);
                    monstersGrid.Rows[rowIndex].Tag = monster;
                }

                SelectFirstRow(monstersGrid);
                if (monstersGrid.Rows.Count > 0)
                    playersGrid.ClearSelection();
                else
                    SelectFirstRow(playersGrid);
                if (playersGrid.Rows.Count == 0 && monstersGrid.Rows.Count == 0)
                    detailsBox.Text = snapshot.Connected
                        ? "No nearby player or monster data is available yet."
                        : snapshot.Status;
            }
            finally
            {
                applyingSnapshot = false;
            }

            UpdateDetailsFromSelection();
        }

        protected override void OnClientSessionBindingChanged(ClientSession previous, ClientSession current)
        {
            base.OnClientSessionBindingChanged(previous, current);
            if (Visible)
                RefreshSnapshot();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                refreshTimer.Stop();
                refreshTimer.Tick -= RefreshTimer_Tick;
                refreshTimer.Dispose();
                characterClassLookup.Dispose();
            }
            base.Dispose(disposing);
        }

        private Control BuildLayout()
        {
            TableLayoutPanel layout = new TableLayoutPanel
            {
                BackColor = BackgroundColor,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Padding = new Padding(14),
                RowCount = 6
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 6F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 36F));

            Panel headingPanel = new Panel { BackColor = BackgroundColor, Dock = DockStyle.Fill };
            Label heading = new Label
            {
                Dock = DockStyle.Top,
                Font = new Font(Font.FontFamily, 13F, FontStyle.Bold),
                ForeColor = AccentColor,
                Height = 28,
                Text = "Ultra Kit"
            };
            Label description = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = SecondaryTextColor,
                Text = "Live encounter details for the client that opened this window."
            };
            headingPanel.Controls.Add(description);
            headingPanel.Controls.Add(heading);

            TableLayoutPanel summary = new TableLayoutPanel
            {
                BackColor = CardColor,
                ColumnCount = 6,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 0, 8),
                Padding = new Padding(10, 5, 10, 5),
                RowCount = 2
            };
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48F));
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48F));
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28F));
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52F));
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28F));
            summary.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            summary.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            summary.Controls.Add(CreateSummaryCaption("STATUS"), 0, 0);
            summary.Controls.Add(statusLabel, 1, 0);
            summary.SetColumnSpan(statusLabel, 5);
            summary.Controls.Add(CreateSummaryCaption("AREA"), 0, 1);
            summary.Controls.Add(locationLabel, 1, 1);
            summary.Controls.Add(CreateSummaryCaption("MOBS"), 2, 1);
            summary.Controls.Add(monsterCountLabel, 3, 1);
            summary.Controls.Add(CreateSummaryCaption("PLAYERS"), 4, 1);
            summary.Controls.Add(playerCountLabel, 5, 1);

            DarkSectionPanel monstersPanel = new DarkSectionPanel
            {
                Dock = DockStyle.Fill,
                SectionHeader = "Monsters & Bosses"
            };
            monstersPanel.Controls.Add(monstersGrid);

            DarkSectionPanel playersPanel = new DarkSectionPanel
            {
                Dock = DockStyle.Fill,
                SectionHeader = "Players"
            };
            playersPanel.Controls.Add(playersGrid);

            DarkSectionPanel detailsPanel = new DarkSectionPanel
            {
                Dock = DockStyle.Fill,
                SectionHeader = "Selected details"
            };
            detailsPanel.Controls.Add(detailsBox);

            layout.Controls.Add(headingPanel, 0, 0);
            layout.Controls.Add(summary, 0, 1);
            layout.Controls.Add(monstersPanel, 0, 2);
            layout.Controls.Add(playersPanel, 0, 4);
            layout.Controls.Add(detailsPanel, 0, 5);
            return layout;
        }

        private static Label CreateSummaryCaption(string text)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = SecondaryTextColor,
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static Label CreateSummaryValue(string text)
        {
            return new Label
            {
                AutoEllipsis = true,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.Gainsboro,
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static DarkDataGridView CreateGrid(string accessibleName)
        {
            DarkDataGridView grid = new DarkDataGridView
            {
                AccessibleName = accessibleName,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToOrderColumns = true,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = CardColor,
                BorderStyle = BorderStyle.FixedSingle,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeight = 28,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                Dock = DockStyle.Fill,
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(50, 55, 68),
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                RowTemplate = { Height = 26 },
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ShowCellErrors = false,
                ShowRowErrors = false
            };
            return grid;
        }

        private static DarkTextBox CreateDetailsBox()
        {
            return new DarkTextBox
            {
                AccessibleName = "Ultra Kit selected details",
                BackColor = CardColor,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                ForeColor = Color.Gainsboro,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.None,
                WordWrap = true
            };
        }

        private static void ConfigurePlayerColumns(DarkDataGridView grid)
        {
            AddColumn(grid, "Name", 92, 1.25F);
            AddColumn(grid, "Class", 82, 1.05F);
            AddColumn(grid, "Lvl", 38, 0.45F);
            AddColumn(grid, "HP", 72, 0.9F);
            AddColumn(grid, "State", 54, 0.65F);
        }

        private static void ConfigureMonsterColumns(DarkDataGridView grid)
        {
            AddColumn(grid, "Name", 122, 1.5F);
            AddColumn(grid, "Lvl", 40, 0.45F);
            AddColumn(grid, "HP", 92, 1.05F);
            AddColumn(grid, "State", 62, 0.65F);
        }

        private static void AddColumn(DarkDataGridView grid, string heading, int minimumWidth, float fillWeight)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn
            {
                HeaderText = heading,
                MinimumWidth = minimumWidth,
                FillWeight = fillWeight * 100F,
                SortMode = DataGridViewColumnSortMode.Automatic
            };
            grid.Columns.Add(column);
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshSnapshot();
        }

        private void RefreshSnapshot()
        {
            if (refreshInProgress || IsDisposed || Disposing || !Visible)
                return;

            refreshInProgress = true;
            try
            {
                ClientSession session = CaptureBoundClientSession();
                UltraKitSnapshot snapshot = CaptureSnapshot(session);
                ResolveUnknownPlayerClasses(snapshot);
                ApplySnapshot(snapshot);
            }
            finally
            {
                refreshInProgress = false;
            }
        }

        private void ResolveUnknownPlayerClasses(UltraKitSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.Connected)
                return;

            foreach (UltraPlayerSnapshot player in snapshot.Players)
            {
                string liveClass = NormalizeClassName(player.Class);
                if (!string.IsNullOrEmpty(liveClass))
                {
                    characterClassLookup.Remember(player.Name, liveClass);
                    continue;
                }

                string resolvedClass = characterClassLookup.ResolveOrQueue(player.Name);
                player.SetResolvedClass(resolvedClass);
            }
        }

        private void UltraKitForm_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                RefreshSnapshot();
                refreshTimer.Start();
            }
            else
            {
                refreshTimer.Stop();
            }
        }

        private void UltraKitForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (!applyingSnapshot)
                UpdateDetailsFromSelection();
        }

        private void UpdateDetailsFromSelection()
        {
            UltraPlayerSnapshot player = GetSelectedTag<UltraPlayerSnapshot>(playersGrid);
            if (playersGrid.Focused && player != null)
            {
                monstersGrid.ClearSelection();
                detailsBox.Text = player.ToDetails();
                return;
            }

            UltraMonsterSnapshot monster = GetSelectedTag<UltraMonsterSnapshot>(monstersGrid);
            if (monstersGrid.Focused && monster != null)
            {
                playersGrid.ClearSelection();
                detailsBox.Text = monster.ToDetails();
                return;
            }

            if (player != null)
                detailsBox.Text = player.ToDetails();
            else if (monster != null)
                detailsBox.Text = monster.ToDetails();
        }

        private static T GetSelectedTag<T>(DarkDataGridView grid)
            where T : class
        {
            return grid.SelectedRows.Count == 0 ? null : grid.SelectedRows[0].Tag as T;
        }

        private static void SelectFirstRow(DarkDataGridView grid)
        {
            if (grid.Rows.Count == 0)
                return;
            grid.ClearSelection();
            grid.Rows[0].Selected = true;
        }

        private static IEnumerable<PlayerInfo> SafePlayerFallback(ClientSession session)
        {
            try
            {
                return session.GameState.Players == null
                    ? Enumerable.Empty<PlayerInfo>()
                    : session.GameState.Players.Values.ToArray();
            }
            catch (InvalidOperationException)
            {
                return Enumerable.Empty<PlayerInfo>();
            }
        }

        private static List<PlayerInfo> CaptureMapPlayers(ClientSession session, out bool hasLiveData)
        {
            List<PlayerInfo> players = new List<PlayerInfo>();
            List<Avatar> avatars = TryRead(() => World.AvatarsInMap, null);
            List<string> mapUsernames = TryRead(() => World.PlayersInMap, null);
            List<Avatar> avatarDetails = TryRead(
                () => Flash.Call<List<Avatar>>("selectArrayObjects", "world.avatars", "objData"),
                null);
            string legacyResponse = TryRead(() => Flash.Call("Players", new object[0]), null);
            Dictionary<string, PlayerInfo> legacyPlayers = TryRead(
                () => string.IsNullOrWhiteSpace(legacyResponse)
                    ? null
                    : JsonConvert.DeserializeObject<Dictionary<string, PlayerInfo>>(legacyResponse),
                null);

            hasLiveData = avatars != null
                || mapUsernames != null
                || avatarDetails != null
                || legacyPlayers != null;
            bool hasAuthoritativeMapRoster = avatars != null || mapUsernames != null;

            if (avatars != null)
            {
                foreach (Avatar avatar in avatars.Where(value => value != null))
                    MergePlayer(players, FromAvatar(avatar), true, true);
            }

            if (mapUsernames != null)
            {
                foreach (string username in mapUsernames.Where(value => !string.IsNullOrWhiteSpace(value)))
                    MergePlayer(players, CreatePlayerPlaceholder(username), false, true);
            }

            MergeAvatarDetails(players, avatarDetails, hasAuthoritativeMapRoster);
            MergeLegacyPlayers(players, legacyPlayers, hasAuthoritativeMapRoster);
            MergeSessionPlayers(players, session, hasAuthoritativeMapRoster);

            if (players.Count == 0)
            {
                foreach (PlayerInfo fallback in SafePlayerFallback(session).Where(value => value != null))
                    MergePlayer(players, fallback, false, true);
            }

            return players;
        }

        private static void MergeAvatarDetails(
            IList<PlayerInfo> players,
            IEnumerable<Avatar> avatars,
            bool restrictToMapRoster)
        {
            if (avatars == null)
                return;

            foreach (Avatar avatar in avatars.Where(value => value != null))
            {
                PlayerInfo player = FromAvatar(avatar);
                bool alreadyInMap = FindPlayer(players, player) != null;
                if (!restrictToMapRoster || alreadyInMap)
                    MergePlayer(players, player, false, true);
            }
        }

        private static void MergeLegacyPlayers(
            IList<PlayerInfo> players,
            IDictionary<string, PlayerInfo> legacyPlayers,
            bool restrictToMapRoster)
        {
            if (legacyPlayers == null)
                return;

            foreach (PlayerInfo player in legacyPlayers.Values.Where(value => value != null))
            {
                bool alreadyInMap = FindPlayer(players, player) != null;
                if (!restrictToMapRoster || alreadyInMap)
                    MergePlayer(players, player, false, true);
            }
        }

        private static void MergeSessionPlayers(
            IList<PlayerInfo> players,
            ClientSession session,
            bool restrictToMapRoster)
        {
            IEnumerable<PlayerInfo> cachedPlayers = SafePlayerFallback(session);
            foreach (PlayerInfo player in cachedPlayers.Where(value => value != null))
            {
                bool alreadyInMap = FindPlayer(players, player) != null;
                if (!restrictToMapRoster || alreadyInMap)
                    MergePlayer(players, player, false, true);
            }
        }

        private static PlayerInfo FromAvatar(Avatar avatar)
        {
            return new PlayerInfo
            {
                Name = avatar.Name,
                Username = avatar.Username,
                Class = avatar.Class,
                HP = avatar.HP,
                MaxHP = avatar.MaxHP,
                MP = avatar.MP,
                AFK = avatar.AFK,
                EntID = avatar.EntID,
                Level = avatar.Level,
                Cell = avatar.Cell,
                Pad = avatar.Pad,
                X = avatar.X,
                Y = avatar.Y,
                CurrentState = (PlayerInfo.State)avatar.CurrentState
            };
        }

        private static PlayerInfo CreatePlayerPlaceholder(string username)
        {
            string trimmed = username == null ? string.Empty : username.Trim();
            return new PlayerInfo
            {
                Name = trimmed.ToLowerInvariant(),
                Username = trimmed
            };
        }

        private static void MergePlayer(
            IList<PlayerInfo> players,
            PlayerInfo incoming,
            bool preferLiveState,
            bool allowAdd)
        {
            if (incoming == null || string.IsNullOrWhiteSpace(GetPlayerIdentity(incoming)))
                return;

            PlayerInfo existing = FindPlayer(players, incoming);
            if (existing == null)
            {
                if (allowAdd)
                    players.Add(incoming);
                return;
            }

            MergePlayerDetails(existing, incoming, preferLiveState);
        }

        private static PlayerInfo FindPlayer(IEnumerable<PlayerInfo> players, PlayerInfo candidate)
        {
            string identity = GetPlayerIdentity(candidate);
            return players.FirstOrDefault(player => IsSamePlayer(player, identity));
        }

        private static string GetPlayerIdentity(PlayerInfo player)
        {
            if (player == null)
                return string.Empty;
            return !string.IsNullOrWhiteSpace(player.Username) ? player.Username : player.Name;
        }

        private static void MergePlayerDetails(PlayerInfo target, PlayerInfo source, bool preferLiveState)
        {
            if (string.IsNullOrWhiteSpace(target.Name) && !string.IsNullOrWhiteSpace(source.Name))
                target.Name = source.Name;
            if (string.IsNullOrWhiteSpace(target.Username) && !string.IsNullOrWhiteSpace(source.Username))
                target.Username = source.Username;
            if ((string.IsNullOrWhiteSpace(target.Class)
                    || string.Equals(target.Class, "Unknown", StringComparison.OrdinalIgnoreCase))
                && !string.IsNullOrWhiteSpace(source.Class))
            {
                target.Class = source.Class;
            }
            if (target.EntID == 0 && source.EntID != 0)
                target.EntID = source.EntID;
            if (target.Level == 0 && source.Level != 0)
                target.Level = source.Level;

            if (preferLiveState || target.MaxHP == 0)
            {
                target.HP = source.HP;
                target.MaxHP = source.MaxHP;
                target.MP = source.MP;
                target.AFK = source.AFK;
                target.CurrentState = source.CurrentState;
            }

            if ((preferLiveState || string.IsNullOrWhiteSpace(target.Cell))
                && !string.IsNullOrWhiteSpace(source.Cell))
            {
                target.Cell = source.Cell;
            }
            if ((preferLiveState || string.IsNullOrWhiteSpace(target.Pad))
                && !string.IsNullOrWhiteSpace(source.Pad))
            {
                target.Pad = source.Pad;
            }
            if (preferLiveState || (target.X == 0F && target.Y == 0F))
            {
                target.X = source.X;
                target.Y = source.Y;
            }
        }

        private static void AddCurrentPlayerIfMissing(ClientSession session, IList<PlayerInfo> players)
        {
            string username = session.GameState.Username;
            if (string.IsNullOrWhiteSpace(username))
                username = session.DisplayName;
            if (string.IsNullOrWhiteSpace(username))
                return;

            PlayerInfo existing = players.FirstOrDefault(player => IsSamePlayer(player, username));
            if (existing == null)
            {
                players.Add(CaptureCurrentPlayer(username));
                return;
            }

            if (string.IsNullOrWhiteSpace(existing.Class)
                || existing.MaxHP == 0
                || existing.Level == 0)
            {
                MergePlayerDetails(existing, CaptureCurrentPlayer(username), true);
            }
        }

        private static bool IsSamePlayer(PlayerInfo player, string username)
        {
            return player != null
                && (string.Equals(player.Username, username, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(player.Name, username, StringComparison.OrdinalIgnoreCase));
        }

        private static PlayerInfo CaptureCurrentPlayer(string username)
        {
            float[] position = TryRead(() => Player.Position, new float[0]) ?? new float[0];
            return new PlayerInfo
            {
                Name = username.ToLowerInvariant(),
                Username = username,
                Class = TryRead(() => Player.Class, "Unknown"),
                HP = TryRead(() => Player.Health, 0),
                MaxHP = TryRead(() => Player.HealthMax, 0),
                MP = TryRead(() => Player.Mana, 0),
                AFK = TryRead(() => Player.IsAfk, false),
                EntID = TryRead(() => Player.UserID, 0),
                Level = TryRead(() => Player.Level, 0),
                Cell = TryRead(() => Player.Cell, "—"),
                Pad = TryRead(() => Player.Pad, "—"),
                X = position.Length > 0 ? position[0] : 0F,
                Y = position.Length > 1 ? position[1] : 0F,
                CurrentState = (PlayerInfo.State)TryRead(() => (int)Player.CurrentState, 1)
            };
        }

        private static T TryRead<T>(Func<T> reader, T fallback)
        {
            try
            {
                return reader();
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        private static IEnumerable<Monster> SafeMonsterFallback(ClientSession session)
        {
            try
            {
                List<Monster> monsters = session.GameState.CurrentMap == null
                    ? null
                    : session.GameState.CurrentMap.Monsters;
                return monsters == null ? Enumerable.Empty<Monster>() : monsters.ToArray();
            }
            catch (InvalidOperationException)
            {
                return Enumerable.Empty<Monster>();
            }
        }

        private static string GetLocation(ClientSession session)
        {
            string area = session.GameState.MapArea;
            if (!string.IsNullOrWhiteSpace(area))
                return area;
            return session.GameState.CurrentMap == null
                ? string.Empty
                : session.GameState.CurrentMap.Name ?? string.Empty;
        }

        private static string FormatPair(int current, int maximum)
        {
            return maximum > 0 ? current + " / " + maximum : current.ToString();
        }

        private static string NormalizeClassName(string className)
        {
            if (string.IsNullOrWhiteSpace(className)
                || string.Equals(className.Trim(), "Unknown", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return className.Trim();
        }

    }

    internal sealed class UltraKitSnapshot
    {
        public UltraKitSnapshot(
            string clientName,
            string location,
            string status,
            bool connected,
            IList<UltraPlayerSnapshot> players,
            IList<UltraMonsterSnapshot> monsters)
        {
            ClientName = clientName ?? string.Empty;
            Location = location ?? string.Empty;
            Status = status ?? string.Empty;
            Connected = connected;
            Players = new List<UltraPlayerSnapshot>(players ?? new UltraPlayerSnapshot[0]).AsReadOnly();
            Monsters = new List<UltraMonsterSnapshot>(monsters ?? new UltraMonsterSnapshot[0]).AsReadOnly();
        }

        public string ClientName { get; }

        public string Location { get; }

        public string Status { get; }

        public bool Connected { get; }

        public IList<UltraPlayerSnapshot> Players { get; }

        public IList<UltraMonsterSnapshot> Monsters { get; }

        public static UltraKitSnapshot Unavailable(
            string status,
            string clientName = "",
            string location = "")
        {
            return new UltraKitSnapshot(
                clientName,
                location,
                status,
                false,
                new UltraPlayerSnapshot[0],
                new UltraMonsterSnapshot[0]);
        }
    }

    internal sealed class UltraPlayerSnapshot
    {
        public string Name { get; private set; }
        public string Class { get; private set; }
        public int Level { get; private set; }
        public int Health { get; private set; }
        public int MaxHealth { get; private set; }
        public int Mana { get; private set; }
        public bool Afk { get; private set; }
        public int EntityId { get; private set; }
        public string Cell { get; private set; }
        public string Pad { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }
        public string State { get; private set; }

        public static UltraPlayerSnapshot FromPlayer(PlayerInfo player)
        {
            return new UltraPlayerSnapshot
            {
                Name = FirstValue(player.Username, player.Name, "Unknown player"),
                Class = FirstValue(player.Class, "Unknown"),
                Level = player.Level,
                Health = player.HP,
                MaxHealth = player.MaxHP,
                Mana = player.MP,
                Afk = player.AFK,
                EntityId = player.EntID,
                Cell = FirstValue(player.Cell, "—"),
                Pad = FirstValue(player.Pad, "—"),
                X = player.X,
                Y = player.Y,
                State = player.AFK ? "AFK" : player.CurrentState.ToString()
            };
        }

        public string ToDetails()
        {
            return Name
                + Environment.NewLine + "Class: " + Class
                + Environment.NewLine + "Level: " + Level
                + Environment.NewLine + "Health: " + Health + " / " + MaxHealth
                + Environment.NewLine + "Mana: " + Mana
                + Environment.NewLine + "State: " + State
                + Environment.NewLine + "Cell / Pad: " + Cell + " / " + Pad
                + Environment.NewLine + "Position: " + X + ", " + Y
                + Environment.NewLine + "Entity ID: " + EntityId;
        }

        internal void SetResolvedClass(string className)
        {
            if ((string.IsNullOrWhiteSpace(Class)
                    || string.Equals(Class, "Unknown", StringComparison.OrdinalIgnoreCase))
                && !string.IsNullOrWhiteSpace(className))
            {
                Class = className.Trim();
            }
        }

        private static string FirstValue(params string[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        }
    }

    internal sealed class UltraMonsterSnapshot
    {
        public string Name { get; private set; }
        public string Race { get; private set; }
        public int Id { get; private set; }
        public int Level { get; private set; }
        public int Health { get; private set; }
        public int MaxHealth { get; private set; }
        public string State { get; private set; }

        public static UltraMonsterSnapshot FromMonster(Monster monster)
        {
            return new UltraMonsterSnapshot
            {
                Name = string.IsNullOrWhiteSpace(monster.Name) ? "Unknown monster" : monster.Name,
                Race = string.IsNullOrWhiteSpace(monster.Race) ? "Unknown" : monster.Race,
                Id = monster.Id,
                Level = monster.Level,
                Health = monster.Health,
                MaxHealth = monster.MaxHealth,
                State = ((Monster.State)monster.CurrentState).ToString()
            };
        }

        public string ToDetails()
        {
            return Name
                + Environment.NewLine + "Race: " + Race
                + Environment.NewLine + "Level: " + Level
                + Environment.NewLine + "Health: " + Health + " / " + MaxHealth
                + Environment.NewLine + "State: " + State
                + Environment.NewLine + "Monster ID: " + Id;
        }
    }
}
