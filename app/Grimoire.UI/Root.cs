using AxShockwaveFlashObjects;
using DarkUI.Controls;
using DarkUI.Forms;
using Grimoire.Botting;
using Grimoire.Clients;
using Grimoire.Game;
using Grimoire.Game.Data;
using Grimoire.Networking;
using Grimoire.Tools;
using Grimoire.Tools.Plugins;
using Grimoire.Utils;
using System;
using System.Drawing;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text;

namespace Grimoire.UI
{
    public partial class Root : GrimoireForm
    {
        private static readonly Color ToolboxBackgroundColor = Color.FromArgb(28, 32, 40);
        private static readonly Color QuickBotHoverColor = Color.FromArgb(57, 48, 68);

        private IContainer components;

        private NotifyIcon nTray;

        private ContextMenuStrip trayMenu;
        private bool startupComplete;
        private bool shutdownInProgress;
        private bool recoveryPromptShown;
        private bool updateShutdownRequested;
        private bool fullResetShutdownRequested;
        private bool updateOperationRunning;
        private string lastQuickBotPath;

        private WebBrowser homeBrowser;
        private GrimoireHomeBridge homeBridge;
        private Uri homeUri;
        private bool pendingGameOpen;
        private AccountCredentials pendingAccount;
        public MenuStrip MenuMain;
        public CancellationTokenSource login_cts;
        private Panel panel1;
        private Panel metricsPanel;
        private PictureBox picCpuMetric;
        private PictureBox picUptimeMetric;
        private PictureBox picNetworkMetric;
        private Label lblCpuMetric;
        private Label lblUptimeMetric;
        private Label lblNetworkMetric;
        private DarkButton btnToggleHeader;
        private DarkButton btnSettings;
        private ToolTip headerVisibilityToolTip;
        private System.Windows.Forms.Timer metricsTimer;
        private Process metricsProcess;
        private DateTime metricsStartedUtc = DateTime.UtcNow;
        private DateTime lastMetricsSampleUtc;
        private TimeSpan lastProcessorTime;
        private long lastNetworkReceived;
        private long lastNetworkSent;
        private NetworkInterface[] metricsNetworkAdapters = Array.Empty<NetworkInterface>();
        private DateTime metricsNetworkAdaptersRefreshedUtc = DateTime.MinValue;
        private Bitmap cpuMetricIcon;
        private Bitmap uptimeMetricIcon;
        private Bitmap networkMetricIcon;
        private Bitmap headerVisibleIcon;
        private Bitmap headerHiddenIcon;
        private Bitmap settingsIcon;
        private Bitmap updateAvailableIcon;
        private ToolStripMenuItem updateToolStripMenuItem;
        private ToolStripDropDownButton quickBotsMenuItem;
        private readonly List<FileSystemWatcher> quickBotWatchers = new List<FileSystemWatcher>();
        private List<string> quickBotCache = new List<string>();
        private CancellationTokenSource quickBotRefreshCancellation;
        private bool quickBotIndexDirty = true;
        private bool quickBotIndexing;
        private DateTime quickBotIndexRefreshedUtc = DateTime.MinValue;
        private UpdateRelease availableUpdate;
        private PreparedUpdate preparedUpdate;
        private readonly CancellationTokenSource updateCancellation = new CancellationTokenSource();
        public RichTextBox rtbPing;
        private DarkComboBox cbCells;
        private DarkComboBox2 cbPads;
        private DarkButton btnGetCurrentCell;
        private DarkButton btnJump;
        public DarkMenuStrip MenuStrip1;
        private ToolStripMenuItem homeToolStripMenuItem;
        private ToolStripMenuItem botToolStripMenuItem;
        private ToolStripMenuItem startToolStripMenuItem;
        public ToolStripMenuItem stopToolStripMenuItem;
        private ToolStripMenuItem managerToolStripMenuItem;
        private ToolStripMenuItem loadBotToolStripMenuItem;
        private ToolStripMenuItem toolsToolStripMenuItem;
        private ToolStripMenuItem fastTravelsToolStripMenuItem;
        private ToolStripMenuItem loadersgrabbersToolStripMenuItem;
        private ToolStripMenuItem hotkeysToolStripMenuItem;
        private ToolStripMenuItem pluginManagerToolStripMenuItem;
        private ToolStripMenuItem cosmeticsToolStripMenuItem;
        private ToolStripMenuItem bankToolStripMenuItem;
        private ToolStripMenuItem eyeDropperToolStripMenuItem;
        private ToolStripMenuItem logsToolStripMenuItem1;
        private ToolStripMenuItem notepadToolStripMenuItem1;
        public ToolStripMenuItem pingMonitorToolStripMenuItem;
        private ToolStripMenuItem FPSToolStripMenuItem;
        private ToolStripTextBox toolStripTextBox2;
        public ToolStripMenuItem loginToolStripMenuItem;
        public ToolStripComboBox toolStripComboBoxLoginServer;
        private ToolStripMenuItem DPSMeterToolStripMenuItem;
        private ToolStripMenuItem setsToolStripMenuItem;
        private ToolStripMenuItem commandeditornodeToolStripMenuItem;
        private ToolStripMenuItem packetsToolStripMenuItem;
        private ToolStripMenuItem snifferToolStripMenuItem;
        private ToolStripMenuItem spammerToolStripMenuItem;
        private ToolStripMenuItem tampererToolStripMenuItem;
        public ToolStripMenuItem optionsToolStripMenuItem;
        public ToolStripMenuItem infRangeToolStripMenuItem;
        public ToolStripMenuItem provokeToolStripMenuItem1;
        public ToolStripMenuItem enemyMagnetToolStripMenuItem;
        public ToolStripMenuItem lagKillerToolStripMenuItem;
        public ToolStripMenuItem hidePlayersToolStripMenuItem;
        public ToolStripMenuItem skipCutscenesToolStripMenuItem;
        public ToolStripMenuItem disableAnimationsToolStripMenuItem;
        private ToolStripMenuItem bankToolStripMenuItem1;
        private ToolStripMenuItem reloadToolStripMenuItem;
        public ToolStripMenuItem pluginsStrip;
        public ToolStripMenuItem provokeAllMonsterInMapToolStripMenuItem;
        public ToolStripMenuItem untargetSelfToolStripMenuItem;
        public ToolStripMenuItem autosaveStateToolStripMenuItem;
        private ToolStripMenuItem walkSpeedToolStripMenuItem;
        private System.Threading.CancellationTokenSource _cts;
        private DarkPanel GamePanel;
        private SettingsForm settingsForm;
        private ClientSessionManager clientSessions;
        private ClientSession primarySession;
        private ClientSession toolbarSession;
        private bool isBindingToolbar;

        public static Root Instance
        {
            get;
            private set;
        }

        public AxShockwaveFlash Client => clientSessions?.ActiveSession?.FlashBridge.Control ?? Game;

        public ClientSessionManager ClientSessions => clientSessions;

        private AxShockwaveFlash Game;

        public static string PluginsPath { get; private set; } = Grimoire.Utils.AppPaths.PluginsDirectory;

        // Preserve the legacy plugin-facing getter without constructing the
        // manager form during normal startup.
        public static PluginManager PreloadedPlugins => PluginManager.Instance;

        private static bool lightMode;

        public static bool LightMode
        {
            get { return lightMode; }
            set
            {
                if (lightMode == value)
                    return;

                lightMode = value;
                Root root = Instance;
                if (root == null || root.IsDisposed || !root.IsHandleCreated)
                    return;

                try
                {
                    if (root.InvokeRequired)
                    {
                        root.BeginInvoke(new Action(GrimoireForm.ApplyNativeTitleBarThemeToOpenForms));
                    }
                    else
                    {
                        GrimoireForm.ApplyNativeTitleBarThemeToOpenForms();
                    }
                }
                catch (InvalidOperationException)
                {
                    // The window may be closing while a theme change arrives.
                }
            }
        }

        public static bool LeanMode { get; set; } = false;

        public Root()
        {
            InitializeComponent();
            Instance = this;
            clientSessions = new ClientSessionManager(GetConfiguredMaximumDesktopClients(), true);
            clientSessions.ActiveSessionChanged += ClientSessions_ActiveSessionChanged;
            InitializeMultiClientWorkspace();
            InitializeHome();
            ApplyBranding();
            InitializeQuickBots();
            InitializeUpdateButton();
            InitializeTrayBehavior();
            pingMonitorToggle();
            clientHeaderToggle();
            InitializeMetrics();
        }

        private void ClientSessions_ActiveSessionChanged(object sender, ActiveClientSessionChangedEventArgs e)
        {
            if (toolbarSession != null)
                toolbarSession.LoginState.BusyChanged -= ActiveLoginState_BusyChanged;

            toolbarSession = e.Current;
            if (toolbarSession != null)
                toolbarSession.LoginState.BusyChanged += ActiveLoginState_BusyChanged;

            BindToolbarToSession(toolbarSession);
            OnActiveClientSessionChanged(e.Previous, e.Current);
        }

        private void ActiveLoginState_BusyChanged(object sender, EventArgs e)
        {
            ClientSession session = toolbarSession;
            if (session == null || IsDisposed || Disposing)
                return;

            Action update = delegate
            {
                if (ReferenceEquals(toolbarSession, session))
                    loginBoxToggle(!session.LoginState.IsBusy);
            };
            if (InvokeRequired)
                BeginInvoke(update);
            else
                update();
        }

        private void BindToolbarToSession(ClientSession session)
        {
            isBindingToolbar = true;
            try
            {
                bool hasSession = session != null && !session.IsDisposed;
                ClientOptionState options = hasSession ? session.OptionState : null;
                infRangeToolStripMenuItem.Checked = options != null && options.InfiniteRange;
                provokeToolStripMenuItem1.Checked = options != null && options.ProvokeMonsters;
                provokeAllMonsterInMapToolStripMenuItem.Checked = options != null && options.ProvokeAllMonsters;
                enemyMagnetToolStripMenuItem.Checked = options != null && options.EnemyMagnet;
                lagKillerToolStripMenuItem.Checked = options != null && options.LagKiller;
                hidePlayersToolStripMenuItem.Checked = options != null && options.HidePlayers;
                skipCutscenesToolStripMenuItem.Checked = options != null && options.SkipCutscenes;
                disableAnimationsToolStripMenuItem.Checked = options != null && options.DisableAnimations;
                untargetSelfToolStripMenuItem.Checked = options != null && options.Untarget;
                autosaveStateToolStripMenuItem.Checked = options != null && options.SaveStateEnabled;
                walkSpeedToolStripMenuItem.Checked = options != null && options.WalkSpeed > 8;
                FPSToolStripMenuItem.Checked = options != null && options.UseCustomFrameRate;
                toolStripTextBox2.Text = options == null
                    ? "60"
                    : options.DesiredFramesPerSecond.ToString(System.Globalization.CultureInfo.InvariantCulture);
                BindServersForSession(session);

                cbCells.Items.Clear();
                cbPads.Items.Clear();
                cbCells.Text = string.Empty;
                cbPads.Text = string.Empty;
                if (hasSession)
                {
                    using (ClientSessionContext.Push(session))
                    {
                        cbCells.Text = Player.Cell;
                        cbPads.Text = Player.Pad;
                    }
                }

                bool botRunning = hasSession && session.BotEngine != null && session.BotEngine.IsRunning;
                BotStateChanged(botRunning);
                loginBoxToggle(hasSession && !session.LoginState.IsBusy);
                UpdatePingDisplay(session, session?.LatestPingLatency);
            }
            finally
            {
                isBindingToolbar = false;
            }
        }

        private ClientSession CaptureActiveSession()
        {
            ClientSession session = clientSessions?.ActiveSession;
            return session != null && !session.IsDisposed ? session : null;
        }

        private bool HasRunningBots()
        {
            return clientSessions != null
                && clientSessions.Sessions.Any(session => session.BotEngine != null && session.BotEngine.IsRunning);
        }

        internal void BindServersForSession(ClientSession session)
        {
            bool previousBinding = isBindingToolbar;
            isBindingToolbar = true;
            try
            {
                toolStripComboBoxLoginServer.Items.Clear();
                if (session == null || session.IsDisposed || session.GameState.Servers == null)
                    return;

                toolStripComboBoxLoginServer.Items.AddRange(session.GameState.Servers.ToArray());
                if (toolStripComboBoxLoginServer.Items.Count == 0)
                    return;

                int preferredIndex = BotClientConfig.Instance.GetValue<int>("serverIndex");
                toolStripComboBoxLoginServer.SelectedIndex = Math.Max(
                    0,
                    Math.Min(preferredIndex, toolStripComboBoxLoginServer.Items.Count - 1));
            }
            finally
            {
                isBindingToolbar = previousBinding;
            }
        }

        private bool HasLoggedInClients()
        {
            return clientSessions != null
                && clientSessions.Sessions.Any(session => session.GameState.IsLoggedIn);
        }

        internal void UpdatePingDisplay(ClientSession session, int? latency)
        {
            if (session != null && !ReferenceEquals(clientSessions?.ActiveSession, session))
                return;
            if (IsDisposed || Disposing || !IsHandleCreated)
                return;

            Action update = delegate
            {
                if (session != null && !ReferenceEquals(clientSessions?.ActiveSession, session))
                    return;
                if (latency == null)
                {
                    if (rtbPing.TextLength > 0)
                        rtbPing.Clear();
                    return;
                }
                if (!rtbPing.Visible)
                    return;

                if (latency <= 100)
                    rtbPing.ForeColor = Color.LimeGreen;
                else if (latency <= 200)
                    rtbPing.ForeColor = Color.Orange;
                else
                    rtbPing.ForeColor = Color.Red;
                rtbPing.Clear();
                rtbPing.AppendText("⬤  " + latency.Value + " ms");
            };

            try
            {
                if (InvokeRequired)
                    BeginInvoke(update);
                else
                    update();
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void ApplyBranding()
        {
            Color accentPurple = Color.MediumPurple;
            Text = "Grimoire Exodus";
            nTray.Text = "Grimoire Exodus";
            Icon = (Icon)global::Properties.Resources.GrimoireIcon.Clone();
            nTray.Icon = (Icon)global::Properties.Resources.GrimoireIcon.Clone();
            MenuStrip1.Renderer = new PurpleMenuRenderer();
            MenuStrip1.ForeColor = accentPurple;
            ApplyMenuColor(MenuStrip1.Items, accentPurple);
        }

        private static void ApplyMenuColor(ToolStripItemCollection items, Color color)
        {
            foreach (ToolStripItem item in items)
            {
                if (!(item is ToolStripMenuItem menuItem))
                {
                    continue;
                }

                menuItem.ForeColor = color;
                ApplyMenuColor(menuItem.DropDownItems, color);
            }
        }

        private void InitializeTrayBehavior()
        {
            trayMenu = new ContextMenuStrip(components)
            {
                BackColor = Color.FromArgb(28, 32, 40),
                ForeColor = Color.Gainsboro,
                ShowImageMargin = false
            };

            ToolStripMenuItem showItem = new ToolStripMenuItem("Show Grimoire Exodus");
            showItem.Click += (sender, args) => RestoreFromTray();
            ToolStripMenuItem hideItem = new ToolStripMenuItem("Hide Grimoire Exodus");
            hideItem.Click += (sender, args) => HideToTray(false);
            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (sender, args) =>
            {
                RestoreFromTray();
                Close();
            };

            trayMenu.Items.Add(showItem);
            trayMenu.Items.Add(hideItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(exitItem);
            nTray.ContextMenuStrip = trayMenu;
            nTray.BalloonTipClicked += (sender, args) => RestoreFromTray();
        }

        private void InitializeUpdateButton()
        {
            updateAvailableIcon = CreateStatusIcon(StatusIcon.UpdateAvailable, Color.MediumPurple);
            updateToolStripMenuItem = new ToolStripMenuItem
            {
                AccessibleDescription = "Downloads and installs a verified Grimoire Exodus update.",
                AccessibleName = "Update available",
                AutoToolTip = true,
                BackColor = Color.FromArgb(28, 32, 40),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Image = updateAvailableIcon,
                ImageScaling = ToolStripItemImageScaling.None,
                Name = "updateToolStripMenuItem",
                Padding = new Padding(4, 0, 4, 0),
                Size = new Size(28, 20),
                ToolTipText = "Checking for updates...",
                Visible = false
            };
            updateToolStripMenuItem.Click += updateToolStripMenuItem_Click;
            MenuStrip1.Items.Add(updateToolStripMenuItem);
        }

        private void InitializeQuickBots()
        {
            quickBotsMenuItem = new ToolStripDropDownButton
            {
                AccessibleDescription = "Quickly loads a bundled or user-managed GBot.",
                AccessibleName = "Quick load GBot",
                Alignment = ToolStripItemAlignment.Right,
                AutoSize = false,
                AutoToolTip = true,
                BackColor = ToolboxBackgroundColor,
                ForeColor = Color.Gainsboro,
                Name = "quickBotsMenuItem",
                Padding = new Padding(8, 0, 4, 0),
                ShowDropDownArrow = true,
                Size = new Size(235, 23),
                Text = "Quick load GBot...",
                TextAlign = ContentAlignment.MiddleLeft,
                ToolTipText = "Quick load a GBot"
            };
            quickBotsMenuItem.DropDown.Name = "quickBotsDropDown";
            quickBotsMenuItem.DropDown.BackColor = ToolboxBackgroundColor;
            quickBotsMenuItem.DropDown.ForeColor = Color.Gainsboro;
            quickBotsMenuItem.DropDownOpening += quickBotsMenuItem_DropDownOpening;
            MenuStrip1.Items.Add(quickBotsMenuItem);
            InitializeQuickBotWatchers();
            RefreshQuickBots();
        }

        private void quickBotsMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            RefreshQuickBots();
        }

        private void RefreshQuickBots()
        {
            if (DateTime.UtcNow - quickBotIndexRefreshedUtc > TimeSpan.FromMinutes(1))
            {
                quickBotIndexDirty = true;
            }
            RenderQuickBots(quickBotCache, quickBotIndexing);
            if (quickBotIndexDirty && !quickBotIndexing)
            {
                ObserveQuickBotRefresh(RefreshQuickBotIndexAsync());
            }
        }

        private static void ObserveQuickBotRefresh(Task refreshTask)
        {
            refreshTask.ContinueWith(
                task => Debug.WriteLine("Unable to refresh quick GBots: " + task.Exception?.GetBaseException()),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        private async Task RefreshQuickBotIndexAsync()
        {
            quickBotIndexing = true;
            RenderQuickBots(quickBotCache, true);
            try
            {
                List<string> bots = await Task.Run(() =>
                {
                    IEnumerable<string> indexed = GetQuickBots(AppPaths.CommunityBotsDirectory)
                        .Concat(GetQuickBots(AppPaths.ExternalBotsDirectory));
                    List<string> result = indexed
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    result.Sort(CompareQuickBotPaths);
                    return result;
                });

                if (IsDisposed || Disposing)
                    return;
                quickBotCache = bots;
                quickBotIndexDirty = false;
                quickBotIndexRefreshedUtc = DateTime.UtcNow;
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Unable to refresh quick GBots: " + exception);
            }
            finally
            {
                quickBotIndexing = false;
                if (!IsDisposed && !Disposing)
                {
                    RenderQuickBots(quickBotCache, false);
                    RefreshHomeState();
                }
            }
        }

        private void RenderQuickBots(IReadOnlyCollection<string> bots, bool indexing)
        {
            if (quickBotsMenuItem == null || IsDisposed)
                return;

            quickBotsMenuItem.DropDownItems.Clear();
            if (bots.Count == 0)
            {
                quickBotsMenuItem.DropDownItems.Add(new QuickBotStatusMenuItem(indexing ? "Scanning GBots..." : "No GBots found"));
            }
            else
            {
                AddQuickBotItems(bots);
            }

            int botCount = bots.Count;
            bool currentExists = IsQuickBotPath(lastQuickBotPath);
            if (!currentExists)
            {
                lastQuickBotPath = null;
            }
            quickBotsMenuItem.Text = currentExists
                ? Path.GetFileNameWithoutExtension(lastQuickBotPath)
                : "Quick load GBot...";
            quickBotsMenuItem.ToolTipText = indexing
                ? "Refreshing the GBot list..."
                : botCount == 0
                    ? "Add .gbot files to Documents\\Grimoire\\Bot"
                    : "Quick load one of " + botCount + " GBot" + (botCount == 1 ? string.Empty : "s");
        }

        private static IEnumerable<string> GetQuickBots(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return Array.Empty<string>();
            }

            try
            {
                return Directory.EnumerateFiles(directory, "*.gbot", SearchOption.AllDirectories).ToArray();
            }
            catch (IOException exception)
            {
                Debug.WriteLine("Unable to index GBots in '" + directory + "': " + exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.WriteLine("Unable to index GBots in '" + directory + "': " + exception.Message);
            }
            return Array.Empty<string>();
        }

        private void InitializeQuickBotWatchers()
        {
            AddQuickBotWatcher(AppPaths.CommunityBotsDirectory);
            AddQuickBotWatcher(AppPaths.ExternalBotsDirectory);
        }

        private void AddQuickBotWatcher(string directory)
        {
            if (!Directory.Exists(directory))
                return;

            try
            {
                FileSystemWatcher watcher = new FileSystemWatcher(directory, "*.gbot")
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
                };
                watcher.Changed += QuickBotDirectoryChanged;
                watcher.Created += QuickBotDirectoryChanged;
                watcher.Deleted += QuickBotDirectoryChanged;
                watcher.Renamed += QuickBotDirectoryChanged;
                watcher.Error += QuickBotWatcherError;
                watcher.EnableRaisingEvents = true;
                quickBotWatchers.Add(watcher);
            }
            catch (Exception exception) when (exception is IOException
                || exception is UnauthorizedAccessException
                || exception is ArgumentException)
            {
                Debug.WriteLine("Unable to watch GBot directory '" + directory + "': " + exception.Message);
            }
        }

        private void QuickBotWatcherError(object sender, ErrorEventArgs e)
        {
            quickBotIndexDirty = true;
            if (!IsDisposed && !Disposing && IsHandleCreated)
            {
                try
                {
                    BeginInvoke((Action)RefreshQuickBots);
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        private void QuickBotDirectoryChanged(object sender, FileSystemEventArgs e)
        {
            quickBotIndexDirty = true;
            CancellationTokenSource next = new CancellationTokenSource();
            CancellationTokenSource previous = Interlocked.Exchange(ref quickBotRefreshCancellation, next);
            previous?.Cancel();
            previous?.Dispose();
            Task.Delay(300, next.Token).ContinueWith(
                task =>
                {
                    if (task.IsCanceled || IsDisposed || Disposing || !IsHandleCreated)
                        return;
                    BeginInvoke((Action)RefreshQuickBots);
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private static int CompareQuickBotPaths(string left, string right)
        {
            int titleComparison = StringComparer.OrdinalIgnoreCase.Compare(
                Path.GetFileName(left),
                Path.GetFileName(right));
            return titleComparison != 0
                ? titleComparison
                : StringComparer.OrdinalIgnoreCase.Compare(left, right);
        }

        private void AddQuickBotItems(IEnumerable<string> botPaths)
        {
            foreach (string path in botPaths)
            {
                ToolStripMenuItem item = new ToolStripMenuItem
                {
                    BackColor = ToolboxBackgroundColor,
                    ForeColor = Color.Gainsboro,
                    Tag = path,
                    Text = Path.GetFileNameWithoutExtension(path),
                    ToolTipText = path
                };
                item.Click += quickBotMenuItem_Click;
                quickBotsMenuItem.DropDownItems.Add(item);
            }
        }

        private void quickBotMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
            string path = menuItem?.Tag as string;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;

            try
            {
                ClientSession session = CaptureActiveSession();
                if (session?.BotEngine != null && session.BotEngine.IsRunning)
                {
                    DarkMessageBox.Show(
                        this,
                        "Stop the running bot before loading another GBot.",
                        "Quick load GBot",
                        MessageBoxIcon.Warning);
                    return;
                }

                if (BotManager.Instance.LoadBotFromFile(path, true, false))
                {
                    lastQuickBotPath = path;
                    quickBotsMenuItem.Text = Path.GetFileNameWithoutExtension(path);
                    ShowForm(BotManager.Instance);
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Unable to quick load GBot: " + exception);
                DarkMessageBox.Show(
                    this,
                    "Grimoire could not load this GBot.\r\n\r\n" + exception.Message,
                    "Quick load GBot",
                    MessageBoxIcon.Error);
            }
        }

        public void UpdateQuickBotSelection(string path)
        {
            if (quickBotsMenuItem == null || IsDisposed)
                return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(UpdateQuickBotSelection), path);
                return;
            }

            bool isQuickBot = IsQuickBotPath(path);
            lastQuickBotPath = isQuickBot ? path : null;
            quickBotsMenuItem.Text = string.IsNullOrWhiteSpace(lastQuickBotPath)
                ? "Quick load GBot..."
                : Path.GetFileNameWithoutExtension(lastQuickBotPath);
        }

        private static bool IsQuickBotPath(string path)
        {
            return (IsPathInsideDirectory(path, AppPaths.CommunityBotsDirectory)
                    || IsPathInsideDirectory(path, AppPaths.ExternalBotsDirectory))
                && string.Equals(Path.GetExtension(path), ".gbot", StringComparison.OrdinalIgnoreCase)
                && File.Exists(path);
        }

        private static bool IsPathInsideDirectory(string path, string directory)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(directory))
                return false;
            try
            {
                string root = Path.GetFullPath(directory)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                string candidate = Path.GetFullPath(path);
                return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private sealed class QuickBotStatusMenuItem : ToolStripMenuItem
        {
            public QuickBotStatusMenuItem(string text)
            {
                AutoToolTip = false;
                BackColor = ToolboxBackgroundColor;
                Enabled = false;
                ForeColor = Color.FromArgb(145, 145, 160);
                Text = text;
            }
        }

        private enum StatusIcon
        {
            Cpu,
            Uptime,
            Network,
            EyeOpen,
            EyeClosed,
            Settings,
            UpdateAvailable
        }

        private static Bitmap CreateStatusIcon(StatusIcon icon, Color color)
        {
            Bitmap bitmap = new Bitmap(16, 16);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (Pen pen = new Pen(color, 1.5f))
            using (SolidBrush brush = new SolidBrush(color))
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                switch (icon)
                {
                    case StatusIcon.Cpu:
                        graphics.DrawRectangle(pen, 4, 4, 8, 8);
                        graphics.DrawRectangle(pen, 6, 6, 4, 4);
                        for (int offset = 5; offset <= 11; offset += 3)
                        {
                            graphics.DrawLine(pen, offset, 2, offset, 4);
                            graphics.DrawLine(pen, offset, 12, offset, 14);
                            graphics.DrawLine(pen, 2, offset, 4, offset);
                            graphics.DrawLine(pen, 12, offset, 14, offset);
                        }
                        break;

                    case StatusIcon.Uptime:
                        graphics.DrawEllipse(pen, 2.5f, 2.5f, 11, 11);
                        graphics.DrawLine(pen, 8, 8, 8, 5);
                        graphics.DrawLine(pen, 8, 8, 10.5f, 9.5f);
                        break;

                    case StatusIcon.Network:
                        graphics.FillRectangle(brush, 2, 10, 2, 4);
                        graphics.FillRectangle(brush, 6, 7, 2, 7);
                        graphics.FillRectangle(brush, 10, 4, 2, 10);
                        graphics.FillRectangle(brush, 14, 1, 2, 13);
                        break;

                    case StatusIcon.EyeOpen:
                    case StatusIcon.EyeClosed:
                    {
                        bool closed = icon == StatusIcon.EyeClosed;
                        using (System.Drawing.Drawing2D.GraphicsPath eye = new System.Drawing.Drawing2D.GraphicsPath())
                        {
                            eye.AddBezier(1.5f, 8f, 4f, 3.5f, 12f, 3.5f, 14.5f, 8f);
                            eye.AddBezier(14.5f, 8f, 12f, 12.5f, 4f, 12.5f, 1.5f, 8f);
                            graphics.DrawPath(pen, eye);
                        }

                        if (closed)
                        {
                            graphics.DrawLine(pen, 2.5f, 2.5f, 13.5f, 13.5f);
                        }
                        else
                        {
                            graphics.FillEllipse(brush, 6f, 6f, 4f, 4f);
                        }
                        break;
                    }

                    case StatusIcon.UpdateAvailable:
                        graphics.DrawEllipse(pen, 2f, 2f, 12f, 12f);
                        graphics.DrawLine(pen, 8f, 4f, 8f, 10f);
                        graphics.DrawLine(pen, 5.5f, 7.5f, 8f, 10f);
                        graphics.DrawLine(pen, 10.5f, 7.5f, 8f, 10f);
                        graphics.DrawLine(pen, 5.5f, 12f, 10.5f, 12f);
                        break;

                    case StatusIcon.Settings:
                        graphics.DrawEllipse(pen, 5.25f, 5.25f, 5.5f, 5.5f);
                        graphics.DrawEllipse(pen, 2.5f, 2.5f, 11f, 11f);
                        for (int angle = 0; angle < 360; angle += 45)
                        {
                            double radians = angle * Math.PI / 180d;
                            float innerX = 8f + (float)Math.Cos(radians) * 5.5f;
                            float innerY = 8f + (float)Math.Sin(radians) * 5.5f;
                            float outerX = 8f + (float)Math.Cos(radians) * 7f;
                            float outerY = 8f + (float)Math.Sin(radians) * 7f;
                            graphics.DrawLine(pen, innerX, innerY, outerX, outerY);
                        }
                        break;
                }
            }
            return bitmap;
        }

        private void InitializeMetrics()
        {
            metricsProcess = Process.GetCurrentProcess();
            ApplyPreferences();
        }

        public void ApplyPreferences()
        {
            AppPreferenceSnapshot preferences = AppPreferences.Load();
            desktopPreferences = preferences;
            SetDeviceStatsVisible(!preferences.HideDeviceStats);
            if (clientSessions != null)
            {
                clientSessions.SetMaximumSessions(
                    preferences.AllowUnlimitedClients
                        ? UnlimitedDesktopClients
                        : DefaultMaximumDesktopClients);
                ApplyClientFrameRatePolicies();
                UpdateClientStripButtons();
                RefreshHomeState();
            }
        }

        private void SetDeviceStatsVisible(bool visible)
        {
            picCpuMetric.Visible = visible;
            lblCpuMetric.Visible = visible;
            picUptimeMetric.Visible = visible;
            lblUptimeMetric.Visible = visible;
            picNetworkMetric.Visible = visible;
            lblNetworkMetric.Visible = visible;

            if (!visible)
            {
                metricsTimer.Stop();
                return;
            }

            if (metricsProcess == null)
            {
                metricsProcess = Process.GetCurrentProcess();
            }
            lastMetricsSampleUtc = DateTime.UtcNow;
            lastProcessorTime = metricsProcess.TotalProcessorTime;
            GetNetworkTotals(out lastNetworkReceived, out lastNetworkSent);
            UpdateMetrics();
            metricsTimer.Start();
        }

        private void MetricsTimer_Tick(object sender, EventArgs e)
        {
            UpdateMetrics();
        }

        private void UpdateMetrics()
        {
            DateTime now = DateTime.UtcNow;
            double elapsedSeconds = Math.Max((now - lastMetricsSampleUtc).TotalSeconds, 0.001d);

            metricsProcess.Refresh();
            TimeSpan processorTime = metricsProcess.TotalProcessorTime;
            double cpuPercent = (processorTime - lastProcessorTime).TotalSeconds
                / elapsedSeconds
                / Math.Max(Environment.ProcessorCount, 1)
                * 100d;
            cpuPercent = Math.Max(0d, Math.Min(100d, cpuPercent));

            long received;
            long sent;
            GetNetworkTotals(out received, out sent);
            double receivedPerSecond = Math.Max(0d, received - lastNetworkReceived) / elapsedSeconds;
            double sentPerSecond = Math.Max(0d, sent - lastNetworkSent) / elapsedSeconds;

            TimeSpan uptime = now - metricsStartedUtc;
            string uptimeText = uptime.TotalDays >= 1d
                ? $"{(int)uptime.TotalDays}d {uptime.Hours:00}:{uptime.Minutes:00}:{uptime.Seconds:00}"
                : $"{uptime.Hours:00}:{uptime.Minutes:00}:{uptime.Seconds:00}";

            lblCpuMetric.Text = $"CPU  {cpuPercent:0}%";
            lblUptimeMetric.Text = $"UPTIME  {uptimeText}";
            lblNetworkMetric.Text = $"SYSTEM NET  ↓ {FormatRate(receivedPerSecond)}  ↑ {FormatRate(sentPerSecond)}";

            lastMetricsSampleUtc = now;
            lastProcessorTime = processorTime;
            lastNetworkReceived = received;
            lastNetworkSent = sent;
        }

        private void GetNetworkTotals(out long received, out long sent)
        {
            received = 0L;
            sent = 0L;
            try
            {
                if (metricsNetworkAdapters.Length == 0
                    || DateTime.UtcNow - metricsNetworkAdaptersRefreshedUtc > TimeSpan.FromMinutes(1))
                {
                    metricsNetworkAdapters = NetworkInterface.GetAllNetworkInterfaces();
                    metricsNetworkAdaptersRefreshedUtc = DateTime.UtcNow;
                }

                foreach (NetworkInterface adapter in metricsNetworkAdapters)
                {
                    if (adapter.OperationalStatus != OperationalStatus.Up
                        || adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback
                        || adapter.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                    {
                        continue;
                    }

                    IPv4InterfaceStatistics statistics = adapter.GetIPv4Statistics();
                    received += statistics.BytesReceived;
                    sent += statistics.BytesSent;
                }
            }
            catch
            {
                received = 0L;
                sent = 0L;
            }
        }

        private static string FormatRate(double bytesPerSecond)
        {
            if (bytesPerSecond >= 1024d * 1024d)
            {
                return $"{bytesPerSecond / 1024d / 1024d:0.0} MB/s";
            }
            if (bytesPerSecond >= 1024d)
            {
                return $"{bytesPerSecond / 1024d:0} KB/s";
            }
            return $"{bytesPerSecond:0} B/s";
        }

        private void MetricsPanel_Paint(object sender, PaintEventArgs e)
        {
            using (Pen separator = new Pen(Color.FromArgb(76, 56, 98)))
            {
                e.Graphics.DrawLine(separator, 0, 0, metricsPanel.ClientSize.Width, 0);
            }
        }

        private void btnToggleHeader_Click(object sender, EventArgs e)
        {
            SetHeaderVisible(!panel1.Visible);
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            if (settingsForm == null || settingsForm.IsDisposed)
            {
                settingsForm = new SettingsForm(this);
                settingsForm.FormClosed += delegate { settingsForm = null; };
                settingsForm.Show(this);
            }
            else
            {
                if (settingsForm.WindowState == FormWindowState.Minimized)
                    settingsForm.WindowState = FormWindowState.Normal;
                settingsForm.BringToFront();
                settingsForm.Activate();
            }
        }

        private void SetHeaderVisible(bool visible)
        {
            panel1.Visible = visible;
            LayoutClientWorkspace();
            btnToggleHeader.Image = visible ? headerVisibleIcon : headerHiddenIcon;
            btnToggleHeader.AccessibleName = visible ? "Hide upper toolbar" : "Show upper toolbar";
            headerVisibilityToolTip.SetToolTip(
                btnToggleHeader,
                visible ? "Hide upper toolbar" : "Show upper toolbar");

            RestoreWorkspaceZOrder();
        }

        private sealed class PurpleMenuRenderer : DarkUI.Renderers.DarkMenuRenderer
        {
            protected override void InitializeItem(ToolStripItem item)
            {
                base.InitializeItem(item);
                if (item is QuickBotStatusMenuItem)
                {
                    item.ForeColor = Color.FromArgb(145, 145, 160);
                }
                else if (item is ToolStripMenuItem)
                {
                    item.ForeColor = Color.MediumPurple;
                }
                else if (item.Name == "quickBotsMenuItem")
                {
                    item.ForeColor = Color.MediumPurple;
                }
            }

            protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
            {
                if (e.Item.Name != "quickBotsMenuItem")
                {
                    base.OnRenderDropDownButtonBackground(e);
                    return;
                }

                Color background = e.Item.Selected || e.Item.Pressed
                    ? QuickBotHoverColor
                    : ToolboxBackgroundColor;
                using (SolidBrush brush = new SolidBrush(background))
                {
                    e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
                }
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                if (e.ToolStrip.Name != "quickBotsDropDown")
                {
                    base.OnRenderToolStripBackground(e);
                    return;
                }

                using (SolidBrush brush = new SolidBrush(ToolboxBackgroundColor))
                {
                    e.Graphics.FillRectangle(brush, e.AffectedBounds);
                }
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (e.Item.Owner?.Name != "quickBotsDropDown")
                {
                    base.OnRenderMenuItemBackground(e);
                    return;
                }

                Color background = e.Item.Selected && e.Item.Enabled
                    ? QuickBotHoverColor
                    : ToolboxBackgroundColor;
                using (SolidBrush brush = new SolidBrush(background))
                {
                    e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
                }
            }

            protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
            {
                if (e.Item.Name == "quickBotsMenuItem")
                {
                    e.ArrowColor = Color.MediumPurple;
                }
                base.OnRenderArrow(e);
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                if (e.Item is QuickBotStatusMenuItem)
                {
                    e.TextColor = Color.FromArgb(145, 145, 160);
                }
                else if (e.Item is ToolStripMenuItem)
                {
                    e.TextColor = Color.MediumPurple;
                }
                else if (e.Item.Name == "quickBotsMenuItem")
                {
                    e.TextColor = Color.MediumPurple;
                }
                base.OnRenderItemText(e);
            }
        }

        private void Root_Load(object sender, EventArgs e)
        {
            startupComplete = false;
            List<string> failures = new List<string>();
            AppPreferenceSnapshot startupPreferences = AppPreferences.Load();
            if (startupPreferences.SkipHomePage)
            {
                homeBrowser.Visible = false;
            }

            RunStartupStage("Window", delegate
            {
                WindowState = FormWindowState.Minimized;
                Show();
                WindowState = FormWindowState.Normal;
            }, failures);
            RunStartupStage("Hotkeys", Hotkeys.Instance.LoadHotkeys, failures);
            RunStartupStage("Plugins", delegate
            {
                Directory.CreateDirectory(PluginsPath);
                foreach (string pluginPath in Directory.GetFiles(PluginsPath, "*.dll"))
                {
                    GrimoirePlugin plugin = new GrimoirePlugin(pluginPath);
                    if (!plugin.Load())
                    {
                        Debug.WriteLine("Plugin startup load failed for '" + pluginPath + "': " + plugin.LastError);
                    }
                }
            }, failures);
            RunStartupStage("Home", delegate
            {
                if (startupPreferences.SkipHomePage)
                {
                    homeBrowser.Visible = false;
                    RequestGameOpen(null);
                }
                else
                {
                    ShowHome();
                }
            }, failures);
            RunStartupStage("Flash", InitializeGame, failures);
            ClientSession proxySession = clientSessions.ActiveSession;
            bool proxyReady = RunStartupStage("Proxy handlers", delegate
            {
                if (proxySession == null)
                    throw new InvalidOperationException("The packet proxy cannot start without an initialized client session.");

                InitializeClientProxy(proxySession);
            }, failures);
            if (proxyReady)
            {
                RunStartupStage("Proxy client", delegate
                {
                    StartClientProxy(proxySession);
                }, failures);
            }

            startupComplete = true;
            ProcessPendingAdditionalLaunchRequest();
            StartupDiagnostics.SetStage(failures.Count == 0 ? "Ready" : "Ready with startup errors");
            StartupDiagnostics.UpdateActivity(null, false, false);

            if (failures.Count > 0)
            {
                BeginInvoke(new Action(delegate
                {
                    DarkMessageBox.Show(
                        this,
                        "Grimoire Exodus started, but these components reported errors: "
                            + string.Join(", ", failures)
                            + ".\r\n\r\nDiagnostics were saved to:\r\n"
                            + StartupDiagnostics.StartupLogFile,
                        "Startup diagnostics",
                        MessageBoxIcon.Warning);
                }));
            }
        }

        private static bool RunStartupStage(string stage, Action action, ICollection<string> failures)
        {
            StartupDiagnostics.SetStage(stage);
            try
            {
                action();
                return true;
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Startup stage '" + stage + "' failed: " + exception);
                StartupDiagnostics.RecordException("Startup / " + stage, exception, false);
                failures.Add(stage);
                return false;
            }
        }

        private async void Root_Shown(object sender, EventArgs e)
        {
            Program.CheckVersion();
            SetDRM(Root.Instance, !enableCapture);
            BeginInvoke(new Action(ShowCrashRecovery));
            await CheckForApplicationUpdateAsync();
        }

        public async Task<UpdateRelease> CheckForApplicationUpdateAsync(bool suppressErrors = true)
        {
            try
            {
                UpdateRelease release = await UpdateService.CheckForUpdateAsync(updateCancellation.Token);
                if (release == null || IsDisposed || Disposing)
                    return null;

                availableUpdate = release;
                updateToolStripMenuItem.ToolTipText = "Grimoire Exodus "
                    + release.Version.ToString(3)
                    + (release.IsPrerelease ? " prerelease" : string.Empty)
                    + " is available";
                updateToolStripMenuItem.AccessibleName = "Update to Grimoire Exodus " + release.Version.ToString(3);
                updateToolStripMenuItem.Visible = true;
                return release;
            }
            catch (OperationCanceledException)
            {
                if (!suppressErrors)
                    throw;
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Update check failed: " + exception);
                if (!suppressErrors)
                    throw;
            }

            return null;
        }

        private async void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (availableUpdate == null || updateOperationRunning)
                return;

            updateOperationRunning = true;
            updateToolStripMenuItem.Enabled = false;
            try
            {
                if (preparedUpdate == null)
                {
                    updateToolStripMenuItem.ToolTipText = "Downloading Grimoire Exodus "
                        + availableUpdate.Version.ToString(3)
                        + "...";
                    Progress<int> progress = new Progress<int>(percent =>
                    {
                        updateToolStripMenuItem.ToolTipText = "Downloading Grimoire Exodus "
                            + availableUpdate.Version.ToString(3)
                            + " — "
                            + percent
                            + "%";
                    });
                    preparedUpdate = await UpdateService.PrepareUpdateAsync(
                        availableUpdate,
                        progress,
                        updateCancellation.Token);
                }

                bool botIsRunning = HasRunningBots();

                string warning = "Grimoire Exodus "
                    + availableUpdate.Version.ToString(3)
                    + " is downloaded and verified. Apply it now?\r\n\r\n"
                    + (botIsRunning
                        ? "A bot is running and will be stopped. "
                        : string.Empty)
                    + "Grimoire will close, merge the update, and restart automatically.";
                DialogResult result = DarkMessageBox.Show(
                    this,
                    warning,
                    "Install Grimoire Exodus update",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                {
                    updateToolStripMenuItem.ToolTipText = "Update "
                        + availableUpdate.Version.ToString(3)
                        + " is ready — click to install";
                    return;
                }

                Process updater = UpdateService.LaunchUpdater(preparedUpdate, Process.GetCurrentProcess().Id);
                if (updater == null)
                    throw new InvalidOperationException("The update helper could not be started.");

                StartupDiagnostics.SetStage("Applying update " + availableUpdate.Version.ToString(3));
                updateShutdownRequested = true;
                Close();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Update failed: " + exception);
                StartupDiagnostics.RecordException("Application update", exception, false);
                DarkMessageBox.Show(
                    this,
                    "Grimoire Exodus could not prepare the update. The current installation was not changed.\r\n\r\n"
                        + exception.Message,
                    "Update failed",
                    MessageBoxIcon.Error);
                preparedUpdate = null;
                updateToolStripMenuItem.ToolTipText = "Update failed — click to try again";
            }
            finally
            {
                updateOperationRunning = false;
                if (!IsDisposed && !updateShutdownRequested)
                {
                    updateToolStripMenuItem.Enabled = true;
                }
            }
        }

        private void ShowCrashRecovery()
        {
            if (recoveryPromptShown)
                return;

            recoveryPromptShown = true;
            RecoveryInfo recovery = StartupDiagnostics.PreviousSession;
            if (recovery == null)
                return;

            string stage = string.IsNullOrWhiteSpace(recovery.LastStage) ? "an unknown stage" : recovery.LastStage;
            bool botCanBeReloaded = !string.IsNullOrWhiteSpace(recovery.LastBotPath)
                && File.Exists(recovery.LastBotPath);

            if (botCanBeReloaded)
            {
                string botName = Path.GetFileName(recovery.LastBotPath);
                DialogResult result = DarkMessageBox.Show(
                    this,
                    "The previous Grimoire session did not close normally. Its last recorded stage was '"
                        + stage
                        + "'.\r\n\r\nReload '"
                        + botName
                        + "'? The bot will be loaded for review and will not start automatically.",
                    "Recover previous session",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (result == DialogResult.Yes && BotManager.Instance.LoadBotFromFile(recovery.LastBotPath, true))
                {
                    if (!BotManager.Instance.Visible)
                    {
                        ShowForm(BotManager.Instance);
                    }
                    else
                    {
                        BotManager.Instance.BringToFront();
                        BotManager.Instance.Focus();
                    }
                }
                return;
            }

            DarkMessageBox.Show(
                this,
                "The previous Grimoire session did not close normally. Its last recorded stage was '"
                    + stage
                    + "'. A recovery report was saved to:\r\n\r\n"
                    + AppPaths.CrashReportsDirectory,
                "Previous session recovered",
                MessageBoxIcon.Warning);
        }

        private void InitializeGame()
        {
            if (primarySession != null && !primarySession.IsDisposed)
                clientSessions.Remove(primarySession);

            primarySession = CreateClientSession("Client 01", false);
            if (pendingGameOpen)
            {
                AccountCredentials credentials = pendingAccount;
                pendingGameOpen = false;
                pendingAccount = null;
                RequestGameOpen(primarySession, credentials, false);
            }

            homeBrowser.BringToFront();
        }

        private void InitializeHome()
        {
            string homePath = Path.Combine(Application.StartupPath, "Resources", "Home", "index.html");
            homeUri = new Uri(homePath);
            homeBridge = new GrimoireHomeBridge(this);
            homeBrowser.ObjectForScripting = homeBridge;
            homeBrowser.Navigate(homeUri);
        }

        private void HomeBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (e.Url == null || e.Url.Scheme.Equals("about", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (homeUri == null || !string.Equals(
                e.Url.LocalPath,
                homeUri.LocalPath,
                StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
            }
        }

        private void HomeBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            RefreshHomeState();
        }

        public bool IsGameReady => CaptureActiveSession()?.IsReady == true;

        public int GameLoadProgress => CaptureActiveSession()?.LoadProgress ?? 0;

        public bool IsClientRunning => clientSessions != null
            && clientSessions.Sessions.Any(session => session.State == ClientSessionState.Running);

        public string[] QuickBotNames => quickBotCache
            .Select(Path.GetFileNameWithoutExtension)
            .ToArray();

        public bool IsQuickBotIndexing => quickBotIndexing;

        public string AppVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Development";

        public void RequestGameOpen(AccountCredentials credentials)
        {
            ClientSession session = CaptureActiveSession();
            if (session == null)
            {
                pendingAccount = credentials;
                pendingGameOpen = true;
                return;
            }

            if (!IsHandleCreated || IsDisposed)
            {
                return;
            }

            RequestGameOpen(session, credentials, false);
        }

        public bool ShowRunningClient()
        {
            ClientSession session = CaptureActiveSession();
            if (session == null || session.State != ClientSessionState.Running)
                session = clientSessions?.Sessions.FirstOrDefault(candidate => candidate.State == ClientSessionState.Running);
            if (session == null || !session.IsReady)
                return false;

            ShowClientSession(session);
            return true;
        }

        private void ShowHome()
        {
            pendingGameOpen = false;
            StopCredentialFill();
            ShowHomeWorkspace();
        }

        private void RefreshHomeState()
        {
            if (homeBrowser.Document == null)
            {
                return;
            }

            try
            {
                homeBrowser.Document.InvokeScript("refreshState");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to refresh Grimoire Home: {ex}");
            }
        }

        public void RefreshHomeAccounts()
        {
            RefreshHomeState();
        }

        private void fastTravelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(Travel.Instance);
        }

        private void loadersgrabbersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(Loaders.Instance);
        }

        private void hotkeysToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(Hotkeys.Instance);
        }

        private void pluginManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(PluginManager.Instance);
        }

        private void snifferToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(PacketLogger.Instance);
        }

        private void spammerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(PacketSpammer.Instance);
        }

        private void tampererToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(PacketTamperer.Instance);
        }

        public void ShowForm(Form form)
        {
            GrimoireForm grimoireForm = form as GrimoireForm;
            if (!form.Visible)
                grimoireForm?.BindClientSession(CaptureActiveSession());

            if (form.WindowState == FormWindowState.Minimized)
            {
                form.WindowState = FormWindowState.Normal;
                form.Show();
                form.BringToFront();
                form.Focus();
            }
            else if (form.Visible)
            {
                form.Hide();
            }
            else
            {
                form.Show();
                form.BringToFront();
                form.Focus();
            }
        }

        private void cbCells_Click(object sender, EventArgs e)
        {
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                if (Player.IsLoggedIn)
                {
                    if (cbCells.Items.Count > 0)
                        cbCells.Items.Clear();
                    cbCells.Items.AddRange(World.Cells);
                }
            }
        }

        private string[] presetPads = new string[]
        {
            "Spawn",
            "Center",
            "Left",
            "Right",
            "Top",
            "Bottom",
            "Up",
            "Down"
        };

        private void cbPads_Click(object sender, EventArgs e)
        {
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                if (Player.IsLoggedIn)
                {
                    if (cbPads.Items.Count > 0)
                        cbPads.Items.Clear();
                    List<string> pads = World.Pads ?? new List<string>();
                    if (pads.Count > 0)
                        cbPads.Items.AddRange(pads.ToArray());
                    foreach (string pad in presetPads)
                    {
                        if (pads.Find((string p) => p.Equals(pad)) == null)
                            cbPads.Items.Add(pad);
                    }
                }
            }
        }

        private void cbPads_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (!cbPads.DroppedDown && !cbPads.DrawDropdownHoverOutline)
            {
                SolidBrush brush = new SolidBrush(BackColor);
                e.Graphics.FillRectangle(brush, e.Bounds);
            }
            else
            {
                e.DrawBackground();
            }
            if (cbPads.Items.Count > e.Index && e.Index > -1)
            {
                using (SolidBrush brush2 = new SolidBrush(ForeColor))
                {
                    string s = cbPads.Items[e.Index].ToString();
                    List<string> pads = World.Pads ?? new List<string>();
                    if (pads.Count > 0 && pads.Find((string p) => p.Equals(s)) != null)
                    {
                        e.Graphics.DrawString(s, e.Font, new SolidBrush(Color.LimeGreen), e.Bounds, StringFormat.GenericDefault);
                    }
                    else
                    {
                        e.Graphics.DrawString(s, e.Font, brush2, e.Bounds, StringFormat.GenericDefault);
                    }
                }
                if (cbPads.DrawDropdownHoverOutline)
                {
                    e.DrawFocusRectangle();
                }
            }
        }

        private void Root_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing
                && !updateShutdownRequested
                && !fullResetShutdownRequested
                && AppPreferences.Load().ConfirmOnClose)
            {
                bool botIsRunning = HasRunningBots();
                bool gameSessionIsActive = IsClientRunning || HasLoggedInClients();
                try
                {
                    ClientSession session = CaptureActiveSession();
                    gameSessionIsActive = gameSessionIsActive || (session != null && session.GameState.IsLoggedIn);
                }
                catch
                {
                    // The confirmation still protects the user if client state
                    // cannot be queried while Windows is closing the form.
                }

                string warning = botIsRunning
                    ? "A bot is still running. Closing Grimoire will stop it immediately and end the current game session."
                    : gameSessionIsActive
                        ? "An AQW session is still open. Closing Grimoire will end the client and stop any active automation."
                        : "Closing Grimoire will end the current client session.";

                DialogResult result = DarkMessageBox.Show(
                    this,
                    warning + "\r\n\r\nAre you sure you want to close Grimoire Exodus?",
                    "Close Grimoire Exodus?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            if (shutdownInProgress)
                return;

            shutdownInProgress = true;
            StartupDiagnostics.SetStage("Shutdown");
            RunShutdownStep("Hotkeys", delegate
            {
                Hotkeys.InstalledHotkeys.ForEach(delegate (Hotkey h)
                {
                    h.Uninstall();
                });
            });
            RunShutdownStep("Proxy", delegate
            {
                if (Proxy.AppClosingToken != null && !Proxy.AppClosingToken.IsCancellationRequested)
                {
                    Proxy.AppClosingToken.Cancel(true);
                }
            });
            RunShutdownStep("Keyboard hook", delegate { KeyboardHook.Instance.Dispose(); });
            RunShutdownStep("Command colors", delegate { CommandColorForm.Instance.Dispose(); });
            RunShutdownStep("Credential fill", StopCredentialFill);
            RunShutdownStep("Client sessions", delegate
            {
                clientSessions?.Dispose();
                primarySession = null;
                Game = null;
                Flash.flash = null;
            });
            RunShutdownStep("Tray icon", delegate
            {
                nTray.Visible = false;
                nTray.Icon?.Dispose();
                nTray.Icon = null;
                nTray.Dispose();
            });
        }

        private static void RunShutdownStep(string name, Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Shutdown step '" + name + "' failed: " + exception);
                StartupDiagnostics.RecordException("Shutdown / " + name, exception, false);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                metricsTimer?.Stop();
                metricsProcess?.Dispose();
                cpuMetricIcon?.Dispose();
                uptimeMetricIcon?.Dispose();
                networkMetricIcon?.Dispose();
                headerVisibleIcon?.Dispose();
                headerHiddenIcon?.Dispose();
                settingsIcon?.Dispose();
                updateAvailableIcon?.Dispose();
                updateCancellation.Cancel();
                updateCancellation.Dispose();
                quickBotRefreshCancellation?.Cancel();
                quickBotRefreshCancellation?.Dispose();
                foreach (FileSystemWatcher watcher in quickBotWatchers)
                {
                    watcher.Dispose();
                }
                quickBotWatchers.Clear();
                if (clientSessions != null)
                {
                    clientSessions.Dispose();
                    clientSessions = null;
                    primarySession = null;
                    Game = null;
                    Flash.flash = null;
                }
            }
            if (disposing && components != null)
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Root));
            this.nTray = new System.Windows.Forms.NotifyIcon(this.components);
            this.homeBrowser = new System.Windows.Forms.WebBrowser();
            this.MenuMain = new System.Windows.Forms.MenuStrip();
            this.panel1 = new System.Windows.Forms.Panel();
            this.metricsPanel = new System.Windows.Forms.Panel();
            this.picCpuMetric = new System.Windows.Forms.PictureBox();
            this.picUptimeMetric = new System.Windows.Forms.PictureBox();
            this.picNetworkMetric = new System.Windows.Forms.PictureBox();
            this.lblCpuMetric = new System.Windows.Forms.Label();
            this.lblUptimeMetric = new System.Windows.Forms.Label();
            this.lblNetworkMetric = new System.Windows.Forms.Label();
            this.btnToggleHeader = new DarkUI.Controls.DarkButton();
            this.btnSettings = new DarkUI.Controls.DarkButton();
            this.headerVisibilityToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.metricsTimer = new System.Windows.Forms.Timer(this.components);
            this.rtbPing = new System.Windows.Forms.RichTextBox();
            this.cbCells = new DarkUI.Controls.DarkComboBox();
            this.cbPads = new DarkUI.Controls.DarkComboBox2();
            this.btnGetCurrentCell = new DarkUI.Controls.DarkButton();
            this.btnJump = new DarkUI.Controls.DarkButton();
            this.MenuStrip1 = new DarkUI.Controls.DarkMenuStrip();
            this.homeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.botToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stopToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.managerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadBotToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fastTravelsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadersgrabbersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hotkeysToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pluginManagerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cosmeticsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bankToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.eyeDropperToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.logsToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.notepadToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.pingMonitorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FPSToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripTextBox2 = new System.Windows.Forms.ToolStripTextBox();
            this.loginToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripComboBoxLoginServer = new System.Windows.Forms.ToolStripComboBox();
            this.DPSMeterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.commandeditornodeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.packetsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.snifferToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.spammerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tampererToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.infRangeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.provokeToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.provokeAllMonsterInMapToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.enemyMagnetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lagKillerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hidePlayersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.skipCutscenesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.disableAnimationsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.untargetSelfToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.autosaveStateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.walkSpeedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bankToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.reloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pluginsStrip = new System.Windows.Forms.ToolStripMenuItem();
            this.GamePanel = new DarkUI.Controls.DarkPanel();
            this.panel1.SuspendLayout();
            this.metricsPanel.SuspendLayout();
            this.MenuStrip1.SuspendLayout();
            this.GamePanel.SuspendLayout();
            this.SuspendLayout();
            //
            // nTray
            //
            this.nTray.Icon = ((System.Drawing.Icon)(resources.GetObject("nTray.Icon")));
            this.nTray.Text = "Grimoire Exodus";
            this.nTray.Visible = true;
            this.nTray.MouseClick += new System.Windows.Forms.MouseEventHandler(this.nTray_MouseClick);
            //
            // homeBrowser
            //
            this.homeBrowser.AllowWebBrowserDrop = false;
            this.homeBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.homeBrowser.IsWebBrowserContextMenuEnabled = false;
            this.homeBrowser.Location = new System.Drawing.Point(0, 0);
            this.homeBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.homeBrowser.Name = "homeBrowser";
            this.homeBrowser.ScriptErrorsSuppressed = true;
            this.homeBrowser.ScrollBarsEnabled = false;
            this.homeBrowser.Size = new System.Drawing.Size(960, 551);
            this.homeBrowser.TabIndex = 0;
            this.homeBrowser.WebBrowserShortcutsEnabled = false;
            this.homeBrowser.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.HomeBrowser_DocumentCompleted);
            this.homeBrowser.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.HomeBrowser_Navigating);
            // 
            // MenuMain
            // 
            this.MenuMain.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.MenuMain.Dock = System.Windows.Forms.DockStyle.None;
            this.MenuMain.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.MenuMain.Location = new System.Drawing.Point(0, 0);
            this.MenuMain.Name = "MenuMain";
            this.MenuMain.Padding = new System.Windows.Forms.Padding(3, 2, 0, 2);
            this.MenuMain.Size = new System.Drawing.Size(202, 24);
            this.MenuMain.TabIndex = 37;
            this.MenuMain.Text = "pluginHolder";
            this.MenuMain.Visible = false;
            this.MenuMain.ItemAdded += new System.Windows.Forms.ToolStripItemEventHandler(this.pluginAdded);
            this.MenuMain.ItemRemoved += new System.Windows.Forms.ToolStripItemEventHandler(this.pluginRemoved);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.panel1.Controls.Add(this.rtbPing);
            this.panel1.Controls.Add(this.MenuStrip1);
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.panel1.Size = new System.Drawing.Size(960, 24);
            this.panel1.TabIndex = 39;
            //
            // metricsPanel
            //
            this.metricsPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.metricsPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.metricsPanel.Controls.Add(this.picCpuMetric);
            this.metricsPanel.Controls.Add(this.lblCpuMetric);
            this.metricsPanel.Controls.Add(this.picUptimeMetric);
            this.metricsPanel.Controls.Add(this.lblUptimeMetric);
            this.metricsPanel.Controls.Add(this.picNetworkMetric);
            this.metricsPanel.Controls.Add(this.lblNetworkMetric);
            this.metricsPanel.Controls.Add(this.btnToggleHeader);
            this.metricsPanel.Controls.Add(this.btnSettings);
            this.metricsPanel.Controls.Add(this.cbCells);
            this.metricsPanel.Controls.Add(this.cbPads);
            this.metricsPanel.Controls.Add(this.btnGetCurrentCell);
            this.metricsPanel.Controls.Add(this.btnJump);
            this.metricsPanel.Font = this.panel1.Font;
            this.metricsPanel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.metricsPanel.Location = new System.Drawing.Point(0, 596);
            this.metricsPanel.Name = "metricsPanel";
            this.metricsPanel.Size = new System.Drawing.Size(960, 24);
            this.metricsPanel.TabIndex = 41;
            this.metricsPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.MetricsPanel_Paint);
            //
            // picCpuMetric
            //
            this.cpuMetricIcon = CreateStatusIcon(StatusIcon.Cpu, System.Drawing.Color.MediumPurple);
            this.picCpuMetric.BackColor = System.Drawing.Color.Transparent;
            this.picCpuMetric.Image = this.cpuMetricIcon;
            this.picCpuMetric.Location = new System.Drawing.Point(78, 4);
            this.picCpuMetric.Name = "picCpuMetric";
            this.picCpuMetric.Size = new System.Drawing.Size(16, 16);
            this.picCpuMetric.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.picCpuMetric.TabStop = false;
            //
            // lblCpuMetric
            //
            this.lblCpuMetric.BackColor = System.Drawing.Color.Transparent;
            this.lblCpuMetric.Font = new System.Drawing.Font("Segoe UI", 7.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCpuMetric.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.lblCpuMetric.Location = new System.Drawing.Point(97, 2);
            this.lblCpuMetric.Name = "lblCpuMetric";
            this.lblCpuMetric.Size = new System.Drawing.Size(72, 20);
            this.lblCpuMetric.TabIndex = 0;
            this.lblCpuMetric.Text = "CPU  --%";
            this.lblCpuMetric.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // picUptimeMetric
            //
            this.uptimeMetricIcon = CreateStatusIcon(StatusIcon.Uptime, System.Drawing.Color.MediumPurple);
            this.picUptimeMetric.BackColor = System.Drawing.Color.Transparent;
            this.picUptimeMetric.Image = this.uptimeMetricIcon;
            this.picUptimeMetric.Location = new System.Drawing.Point(178, 4);
            this.picUptimeMetric.Name = "picUptimeMetric";
            this.picUptimeMetric.Size = new System.Drawing.Size(16, 16);
            this.picUptimeMetric.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.picUptimeMetric.TabStop = false;
            //
            // lblUptimeMetric
            //
            this.lblUptimeMetric.BackColor = System.Drawing.Color.Transparent;
            this.lblUptimeMetric.Font = new System.Drawing.Font("Segoe UI", 7.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblUptimeMetric.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.lblUptimeMetric.Location = new System.Drawing.Point(197, 2);
            this.lblUptimeMetric.Name = "lblUptimeMetric";
            this.lblUptimeMetric.Size = new System.Drawing.Size(130, 20);
            this.lblUptimeMetric.TabIndex = 1;
            this.lblUptimeMetric.Text = "UPTIME  00:00:00";
            this.lblUptimeMetric.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // picNetworkMetric
            //
            this.networkMetricIcon = CreateStatusIcon(StatusIcon.Network, System.Drawing.Color.MediumPurple);
            this.picNetworkMetric.BackColor = System.Drawing.Color.Transparent;
            this.picNetworkMetric.Image = this.networkMetricIcon;
            this.picNetworkMetric.Location = new System.Drawing.Point(336, 4);
            this.picNetworkMetric.Name = "picNetworkMetric";
            this.picNetworkMetric.Size = new System.Drawing.Size(16, 16);
            this.picNetworkMetric.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.picNetworkMetric.TabStop = false;
            //
            // lblNetworkMetric
            //
            this.lblNetworkMetric.BackColor = System.Drawing.Color.Transparent;
            this.lblNetworkMetric.Font = new System.Drawing.Font("Segoe UI", 7.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNetworkMetric.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.lblNetworkMetric.Location = new System.Drawing.Point(355, 2);
            this.lblNetworkMetric.Name = "lblNetworkMetric";
            this.lblNetworkMetric.Size = new System.Drawing.Size(275, 20);
            this.lblNetworkMetric.TabIndex = 2;
            this.lblNetworkMetric.Text = "NET  ↓ 0 B/s  ↑ 0 B/s";
            this.lblNetworkMetric.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // btnToggleHeader
            //
            this.headerVisibleIcon = CreateStatusIcon(StatusIcon.EyeOpen, System.Drawing.Color.MediumPurple);
            this.headerHiddenIcon = CreateStatusIcon(StatusIcon.EyeClosed, System.Drawing.Color.MediumPurple);
            this.btnToggleHeader.AccessibleDescription = "Controls visibility of the upper toolbar.";
            this.btnToggleHeader.AccessibleName = "Hide upper toolbar";
            this.btnToggleHeader.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.btnToggleHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(40)))), ((int)(((byte)(50)))));
            this.btnToggleHeader.BackColorUseGeneric = false;
            this.btnToggleHeader.Checked = false;
            this.btnToggleHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnToggleHeader.Image = this.headerVisibleIcon;
            this.btnToggleHeader.Location = new System.Drawing.Point(4, 2);
            this.btnToggleHeader.Name = "btnToggleHeader";
            this.btnToggleHeader.Padding = new System.Windows.Forms.Padding(0);
            this.btnToggleHeader.Size = new System.Drawing.Size(30, 18);
            this.btnToggleHeader.TabIndex = 3;
            this.btnToggleHeader.Text = "";
            this.btnToggleHeader.Click += new System.EventHandler(this.btnToggleHeader_Click);
            this.headerVisibilityToolTip.SetToolTip(this.btnToggleHeader, "Hide upper toolbar");
            //
            // btnSettings
            //
            this.settingsIcon = CreateStatusIcon(StatusIcon.Settings, System.Drawing.Color.MediumPurple);
            this.btnSettings.AccessibleDescription = "Opens global Grimoire settings.";
            this.btnSettings.AccessibleName = "Settings";
            this.btnSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.btnSettings.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(40)))), ((int)(((byte)(50)))));
            this.btnSettings.BackColorUseGeneric = false;
            this.btnSettings.Checked = false;
            this.btnSettings.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.btnSettings.Image = this.settingsIcon;
            this.btnSettings.Location = new System.Drawing.Point(38, 2);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Padding = new System.Windows.Forms.Padding(0);
            this.btnSettings.Size = new System.Drawing.Size(30, 18);
            this.btnSettings.TabIndex = 4;
            this.btnSettings.Text = "";
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
            this.headerVisibilityToolTip.SetToolTip(this.btnSettings, "Open settings");
            //
            // metricsTimer
            //
            this.metricsTimer.Interval = 1000;
            this.metricsTimer.Tick += new System.EventHandler(this.MetricsTimer_Tick);
            // 
            // rtbPing
            // 
            this.rtbPing.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbPing.AutoWordSelection = true;
            this.rtbPing.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.rtbPing.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbPing.ForeColor = System.Drawing.Color.Gainsboro;
            this.rtbPing.Location = new System.Drawing.Point(656, 4);
            this.rtbPing.Margin = new System.Windows.Forms.Padding(0);
            this.rtbPing.Multiline = false;
            this.rtbPing.Name = "rtbPing";
            this.rtbPing.ReadOnly = true;
            this.rtbPing.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.rtbPing.Size = new System.Drawing.Size(56, 15);
            this.rtbPing.TabIndex = 37;
            this.rtbPing.Text = "";
            this.rtbPing.Visible = false;
            // 
            // cbCells
            // 
            this.cbCells.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbCells.FormattingEnabled = true;
            this.cbCells.Location = new System.Drawing.Point(719, 2);
            this.cbCells.MaxDropDownItems = 50;
            this.cbCells.Name = "cbCells";
            this.cbCells.onUnique = false;
            this.cbCells.Size = new System.Drawing.Size(82, 21);
            this.cbCells.TabIndex = 18;
            this.cbCells.DropDown += new System.EventHandler(this.cbCells_Click);
            // 
            // cbPads
            // 
            this.cbPads.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbPads.FormattingEnabled = true;
            this.cbPads.ItemHeight = 15;
            this.cbPads.Location = new System.Drawing.Point(802, 2);
            this.cbPads.MaxDropDownItems = 20;
            this.cbPads.Name = "cbPads";
            this.cbPads.onUnique = false;
            this.cbPads.Size = new System.Drawing.Size(75, 21);
            this.cbPads.Sorted = true;
            this.cbPads.TabIndex = 19;
            this.cbPads.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.cbPads_DrawItem);
            this.cbPads.DropDown += new System.EventHandler(this.cbPads_Click);
            // 
            // btnGetCurrentCell
            // 
            this.btnGetCurrentCell.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGetCurrentCell.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnGetCurrentCell.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(51)))), ((int)(((byte)(66)))));
            this.btnGetCurrentCell.BackColorUseGeneric = false;
            this.btnGetCurrentCell.Checked = false;
            this.btnGetCurrentCell.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.btnGetCurrentCell.Location = new System.Drawing.Point(878, 2);
            this.btnGetCurrentCell.Name = "btnGetCurrentCell";
            this.btnGetCurrentCell.Size = new System.Drawing.Size(18, 21);
            this.btnGetCurrentCell.TabIndex = 36;
            this.btnGetCurrentCell.Text = "<";
            this.btnGetCurrentCell.Click += new System.EventHandler(this.btnGetCurrentCell_Click);
            // 
            // btnJump
            // 
            this.btnJump.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnJump.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnJump.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(51)))), ((int)(((byte)(66)))));
            this.btnJump.BackColorUseGeneric = false;
            this.btnJump.Checked = false;
            this.btnJump.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.btnJump.Location = new System.Drawing.Point(895, 2);
            this.btnJump.Name = "btnJump";
            this.btnJump.Size = new System.Drawing.Size(62, 21);
            this.btnJump.TabIndex = 28;
            this.btnJump.Text = "Jump";
            this.btnJump.Click += new System.EventHandler(this.btnJump_Click);
            // 
            // MenuStrip1
            // 
            this.MenuStrip1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.MenuStrip1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.MenuStrip1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.MenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.homeToolStripMenuItem,
            this.botToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.packetsToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.bankToolStripMenuItem1,
            this.pluginsStrip});
            this.MenuStrip1.Location = new System.Drawing.Point(0, 0);
            this.MenuStrip1.Name = "MenuStrip1";
            this.MenuStrip1.Padding = new System.Windows.Forms.Padding(2, 2, 0, 2);
            this.MenuStrip1.Size = new System.Drawing.Size(960, 24);
            this.MenuStrip1.TabIndex = 20;
            this.MenuStrip1.Text = "darkMenuStrip1";
            this.MenuStrip1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MenuMain_MouseDown);
            //
            // homeToolStripMenuItem
            //
            this.homeToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.homeToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.homeToolStripMenuItem.ForeColor = System.Drawing.Color.MediumPurple;
            this.homeToolStripMenuItem.Name = "homeToolStripMenuItem";
            this.homeToolStripMenuItem.Size = new System.Drawing.Size(49, 20);
            this.homeToolStripMenuItem.Text = "Home";
            this.homeToolStripMenuItem.Click += new System.EventHandler(this.homeToolStripMenuItem_Click);
            // 
            // botToolStripMenuItem
            // 
            this.botToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.botToolStripMenuItem.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.botToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.startToolStripMenuItem,
            this.stopToolStripMenuItem,
            this.managerToolStripMenuItem,
            this.loadBotToolStripMenuItem});
            this.botToolStripMenuItem.ForeColor = System.Drawing.Color.MediumPurple;
            this.botToolStripMenuItem.Name = "botToolStripMenuItem";
            this.botToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.botToolStripMenuItem.Text = "Bot";
            // 
            // startToolStripMenuItem
            // 
            this.startToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.startToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.startToolStripMenuItem.Name = "startToolStripMenuItem";
            this.startToolStripMenuItem.Size = new System.Drawing.Size(121, 22);
            this.startToolStripMenuItem.Text = "Start";
            this.startToolStripMenuItem.Click += new System.EventHandler(this.startToolStripMenuItem_Click);
            // 
            // stopToolStripMenuItem
            // 
            this.stopToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.stopToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.stopToolStripMenuItem.Enabled = false;
            this.stopToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(81)))), ((int)(((byte)(81)))), ((int)(((byte)(81)))));
            this.stopToolStripMenuItem.Name = "stopToolStripMenuItem";
            this.stopToolStripMenuItem.Size = new System.Drawing.Size(121, 22);
            this.stopToolStripMenuItem.Text = "Stop";
            this.stopToolStripMenuItem.Click += new System.EventHandler(this.stopToolStripMenuItem_Click);
            // 
            // managerToolStripMenuItem
            // 
            this.managerToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.managerToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.managerToolStripMenuItem.Name = "managerToolStripMenuItem";
            this.managerToolStripMenuItem.Size = new System.Drawing.Size(121, 22);
            this.managerToolStripMenuItem.Text = "Manager";
            this.managerToolStripMenuItem.Click += new System.EventHandler(this.managerToolStripMenuItem_Click);
            // 
            // loadBotToolStripMenuItem
            // 
            this.loadBotToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.loadBotToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.loadBotToolStripMenuItem.Name = "loadBotToolStripMenuItem";
            this.loadBotToolStripMenuItem.Size = new System.Drawing.Size(121, 22);
            this.loadBotToolStripMenuItem.Text = "Load Bot";
            this.loadBotToolStripMenuItem.Click += new System.EventHandler(this.loadBotToolStripMenuItem_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fastTravelsToolStripMenuItem,
            this.loadersgrabbersToolStripMenuItem,
            this.hotkeysToolStripMenuItem,
            this.pluginManagerToolStripMenuItem,
            this.cosmeticsToolStripMenuItem,
            this.bankToolStripMenuItem,
            this.eyeDropperToolStripMenuItem,
            this.logsToolStripMenuItem1,
            this.notepadToolStripMenuItem1,
            this.pingMonitorToolStripMenuItem,
            this.FPSToolStripMenuItem,
            this.loginToolStripMenuItem,
            this.DPSMeterToolStripMenuItem,
            this.setsToolStripMenuItem,
            this.commandeditornodeToolStripMenuItem});
            this.toolsToolStripMenuItem.ForeColor = System.Drawing.Color.MediumPurple;
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // fastTravelsToolStripMenuItem
            // 
            this.fastTravelsToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.fastTravelsToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.fastTravelsToolStripMenuItem.Name = "fastTravelsToolStripMenuItem";
            this.fastTravelsToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.fastTravelsToolStripMenuItem.Text = "Fast Travels";
            this.fastTravelsToolStripMenuItem.Click += new System.EventHandler(this.fastTravelsToolStripMenuItem_Click);
            // 
            // loadersgrabbersToolStripMenuItem
            // 
            this.loadersgrabbersToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.loadersgrabbersToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.loadersgrabbersToolStripMenuItem.Name = "loadersgrabbersToolStripMenuItem";
            this.loadersgrabbersToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.loadersgrabbersToolStripMenuItem.Text = "Loaders/Grabbers";
            this.loadersgrabbersToolStripMenuItem.Click += new System.EventHandler(this.loadersgrabbersToolStripMenuItem_Click);
            // 
            // hotkeysToolStripMenuItem
            // 
            this.hotkeysToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.hotkeysToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.hotkeysToolStripMenuItem.Name = "hotkeysToolStripMenuItem";
            this.hotkeysToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.hotkeysToolStripMenuItem.Text = "Hotkeys";
            this.hotkeysToolStripMenuItem.Click += new System.EventHandler(this.hotkeysToolStripMenuItem_Click);
            // 
            // pluginManagerToolStripMenuItem
            // 
            this.pluginManagerToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.pluginManagerToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.pluginManagerToolStripMenuItem.Name = "pluginManagerToolStripMenuItem";
            this.pluginManagerToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.pluginManagerToolStripMenuItem.Text = "Plugin Manager";
            this.pluginManagerToolStripMenuItem.Click += new System.EventHandler(this.pluginManagerToolStripMenuItem_Click);
            // 
            // cosmeticsToolStripMenuItem
            // 
            this.cosmeticsToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.cosmeticsToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.cosmeticsToolStripMenuItem.Name = "cosmeticsToolStripMenuItem";
            this.cosmeticsToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.cosmeticsToolStripMenuItem.Text = "Cosmetics";
            this.cosmeticsToolStripMenuItem.Click += new System.EventHandler(this.cosmeticsToolStripMenuItem_Click);
            // 
            // bankToolStripMenuItem
            // 
            this.bankToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.bankToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.bankToolStripMenuItem.Name = "bankToolStripMenuItem";
            this.bankToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.bankToolStripMenuItem.Text = "Bank Items";
            this.bankToolStripMenuItem.Click += new System.EventHandler(this.bankToolStripMenuItem_Click);
            // 
            // eyeDropperToolStripMenuItem
            // 
            this.eyeDropperToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.eyeDropperToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.eyeDropperToolStripMenuItem.Name = "eyeDropperToolStripMenuItem";
            this.eyeDropperToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.eyeDropperToolStripMenuItem.Text = "Eye Dropper";
            this.eyeDropperToolStripMenuItem.Click += new System.EventHandler(this.eyeDropperToolStripMenuItem_Click_1);
            // 
            // logsToolStripMenuItem1
            // 
            this.logsToolStripMenuItem1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.logsToolStripMenuItem1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.logsToolStripMenuItem1.Name = "logsToolStripMenuItem1";
            this.logsToolStripMenuItem1.Size = new System.Drawing.Size(187, 22);
            this.logsToolStripMenuItem1.Text = "Logs";
            this.logsToolStripMenuItem1.Click += new System.EventHandler(this.logsToolStripMenuItem1_Click);
            // 
            // notepadToolStripMenuItem1
            // 
            this.notepadToolStripMenuItem1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.notepadToolStripMenuItem1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.notepadToolStripMenuItem1.Name = "notepadToolStripMenuItem1";
            this.notepadToolStripMenuItem1.Size = new System.Drawing.Size(187, 22);
            this.notepadToolStripMenuItem1.Text = "Notepad";
            this.notepadToolStripMenuItem1.Click += new System.EventHandler(this.notepadToolStripMenuItem1_Click);
            // 
            // pingMonitorToolStripMenuItem
            // 
            this.pingMonitorToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.pingMonitorToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.pingMonitorToolStripMenuItem.Name = "pingMonitorToolStripMenuItem";
            this.pingMonitorToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.pingMonitorToolStripMenuItem.Text = "Ping Monitor";
            this.pingMonitorToolStripMenuItem.Click += new System.EventHandler(this.pingMonitorToolStripMenuItem_Click);
            // 
            // FPSToolStripMenuItem
            // 
            this.FPSToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.FPSToolStripMenuItem.CheckOnClick = true;
            this.FPSToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripTextBox2});
            this.FPSToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.FPSToolStripMenuItem.Name = "FPSToolStripMenuItem";
            this.FPSToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.FPSToolStripMenuItem.Text = "Set FPS";
            this.FPSToolStripMenuItem.Click += new System.EventHandler(this.FPSToolStripMenuItem_Click);
            // 
            // toolStripTextBox2
            // 
            this.toolStripTextBox2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.toolStripTextBox2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.toolStripTextBox2.Name = "toolStripTextBox2";
            this.toolStripTextBox2.Size = new System.Drawing.Size(100, 23);
            this.toolStripTextBox2.Text = "60";
            // 
            // loginToolStripMenuItem
            // 
            this.loginToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.loginToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripComboBoxLoginServer});
            this.loginToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.loginToolStripMenuItem.Name = "loginToolStripMenuItem";
            this.loginToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.loginToolStripMenuItem.Text = "Immediate Login";
            this.loginToolStripMenuItem.Click += new System.EventHandler(this.loginToolStripMenuItem_Click);
            // 
            // toolStripComboBoxLoginServer
            // 
            this.toolStripComboBoxLoginServer.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.toolStripComboBoxLoginServer.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.toolStripComboBoxLoginServer.Name = "toolStripComboBoxLoginServer";
            this.toolStripComboBoxLoginServer.Size = new System.Drawing.Size(121, 23);
            this.toolStripComboBoxLoginServer.Text = "Server";
            this.toolStripComboBoxLoginServer.SelectedIndexChanged += new System.EventHandler(this.toolStripComboBoxLoginServer_Click);
            // 
            // DPSMeterToolStripMenuItem
            // 
            this.DPSMeterToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.DPSMeterToolStripMenuItem.Enabled = false;
            this.DPSMeterToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(81)))), ((int)(((byte)(81)))), ((int)(((byte)(81)))));
            this.DPSMeterToolStripMenuItem.Name = "DPSMeterToolStripMenuItem";
            this.DPSMeterToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.DPSMeterToolStripMenuItem.Text = "DPS Meter";
            this.DPSMeterToolStripMenuItem.Visible = false;
            this.DPSMeterToolStripMenuItem.Click += new System.EventHandler(this.dPSMeterToolStripMenuItem_Click);
            // 
            // setsToolStripMenuItem
            // 
            this.setsToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.setsToolStripMenuItem.Enabled = false;
            this.setsToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(81)))), ((int)(((byte)(81)))), ((int)(((byte)(81)))));
            this.setsToolStripMenuItem.Name = "setsToolStripMenuItem";
            this.setsToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.setsToolStripMenuItem.Text = "Sets";
            this.setsToolStripMenuItem.Visible = false;
            this.setsToolStripMenuItem.Click += new System.EventHandler(this.setsToolStripMenuItem_Click);
            // 
            // commandeditornodeToolStripMenuItem
            // 
            this.commandeditornodeToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.commandeditornodeToolStripMenuItem.Enabled = false;
            this.commandeditornodeToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(81)))), ((int)(((byte)(81)))), ((int)(((byte)(81)))));
            this.commandeditornodeToolStripMenuItem.Name = "commandeditornodeToolStripMenuItem";
            this.commandeditornodeToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.commandeditornodeToolStripMenuItem.Text = "commandeditornode";
            this.commandeditornodeToolStripMenuItem.Visible = false;
            this.commandeditornodeToolStripMenuItem.Click += new System.EventHandler(this.commandeditornodeToolStripMenuItem_Click);
            // 
            // packetsToolStripMenuItem
            // 
            this.packetsToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.packetsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.snifferToolStripMenuItem,
            this.spammerToolStripMenuItem,
            this.tampererToolStripMenuItem});
            this.packetsToolStripMenuItem.ForeColor = System.Drawing.Color.MediumPurple;
            this.packetsToolStripMenuItem.Name = "packetsToolStripMenuItem";
            this.packetsToolStripMenuItem.Size = new System.Drawing.Size(59, 20);
            this.packetsToolStripMenuItem.Text = "Packets";
            // 
            // snifferToolStripMenuItem
            // 
            this.snifferToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.snifferToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.snifferToolStripMenuItem.Name = "snifferToolStripMenuItem";
            this.snifferToolStripMenuItem.Size = new System.Drawing.Size(125, 22);
            this.snifferToolStripMenuItem.Text = "Sniffer";
            this.snifferToolStripMenuItem.Click += new System.EventHandler(this.snifferToolStripMenuItem_Click);
            // 
            // spammerToolStripMenuItem
            // 
            this.spammerToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.spammerToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.spammerToolStripMenuItem.Name = "spammerToolStripMenuItem";
            this.spammerToolStripMenuItem.Size = new System.Drawing.Size(125, 22);
            this.spammerToolStripMenuItem.Text = "Spammer";
            this.spammerToolStripMenuItem.Click += new System.EventHandler(this.spammerToolStripMenuItem_Click);
            // 
            // tampererToolStripMenuItem
            // 
            this.tampererToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.tampererToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.tampererToolStripMenuItem.Name = "tampererToolStripMenuItem";
            this.tampererToolStripMenuItem.Size = new System.Drawing.Size(125, 22);
            this.tampererToolStripMenuItem.Text = "Tamperer";
            this.tampererToolStripMenuItem.Click += new System.EventHandler(this.tampererToolStripMenuItem_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.infRangeToolStripMenuItem,
            this.provokeToolStripMenuItem1,
            this.provokeAllMonsterInMapToolStripMenuItem,
            this.enemyMagnetToolStripMenuItem,
            this.lagKillerToolStripMenuItem,
            this.hidePlayersToolStripMenuItem,
            this.skipCutscenesToolStripMenuItem,
            this.disableAnimationsToolStripMenuItem,
            this.untargetSelfToolStripMenuItem,
            this.autosaveStateToolStripMenuItem,
            this.walkSpeedToolStripMenuItem});
            this.optionsToolStripMenuItem.ForeColor = System.Drawing.Color.MediumPurple;
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.optionsToolStripMenuItem.Text = "Options";
            // 
            // infRangeToolStripMenuItem
            // 
            this.infRangeToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.infRangeToolStripMenuItem.CheckOnClick = true;
            this.infRangeToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.infRangeToolStripMenuItem.Name = "infRangeToolStripMenuItem";
            this.infRangeToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.infRangeToolStripMenuItem.Text = "Infinite Range";
            this.infRangeToolStripMenuItem.CheckedChanged += new System.EventHandler(this.infRangeToolStripMenuItem_Click);
            // 
            // provokeToolStripMenuItem1
            // 
            this.provokeToolStripMenuItem1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.provokeToolStripMenuItem1.CheckOnClick = true;
            this.provokeToolStripMenuItem1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.provokeToolStripMenuItem1.Name = "provokeToolStripMenuItem1";
            this.provokeToolStripMenuItem1.Size = new System.Drawing.Size(209, 22);
            this.provokeToolStripMenuItem1.Text = "Provoke Monsters in Cell";
            this.provokeToolStripMenuItem1.CheckedChanged += new System.EventHandler(this.provokeToolStripMenuItem1_Click);
            this.provokeToolStripMenuItem1.Click += new System.EventHandler(this.provokeToolStripMenuItem1_Clicked);
            // 
            // provokeAllMonsterInMapToolStripMenuItem
            // 
            this.provokeAllMonsterInMapToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.provokeAllMonsterInMapToolStripMenuItem.CheckOnClick = true;
            this.provokeAllMonsterInMapToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.provokeAllMonsterInMapToolStripMenuItem.Name = "provokeAllMonsterInMapToolStripMenuItem";
            this.provokeAllMonsterInMapToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.provokeAllMonsterInMapToolStripMenuItem.Text = "Provoke Monsters in Map";
            this.provokeAllMonsterInMapToolStripMenuItem.CheckedChanged += new System.EventHandler(this.provokeAllMonsterInMapToolStripMenuItem_Click);
            // 
            // enemyMagnetToolStripMenuItem
            // 
            this.enemyMagnetToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.enemyMagnetToolStripMenuItem.CheckOnClick = true;
            this.enemyMagnetToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.enemyMagnetToolStripMenuItem.Name = "enemyMagnetToolStripMenuItem";
            this.enemyMagnetToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.enemyMagnetToolStripMenuItem.Text = "Target Magnet";
            this.enemyMagnetToolStripMenuItem.CheckedChanged += new System.EventHandler(this.enemyMagnetToolStripMenuItem_Click);
            // 
            // lagKillerToolStripMenuItem
            // 
            this.lagKillerToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.lagKillerToolStripMenuItem.CheckOnClick = true;
            this.lagKillerToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.lagKillerToolStripMenuItem.Name = "lagKillerToolStripMenuItem";
            this.lagKillerToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.lagKillerToolStripMenuItem.Text = "Lag Killer";
            this.lagKillerToolStripMenuItem.CheckedChanged += new System.EventHandler(this.lagKillerToolStripMenuItem_Click);
            // 
            // hidePlayersToolStripMenuItem
            // 
            this.hidePlayersToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.hidePlayersToolStripMenuItem.CheckOnClick = true;
            this.hidePlayersToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.hidePlayersToolStripMenuItem.Name = "hidePlayersToolStripMenuItem";
            this.hidePlayersToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.hidePlayersToolStripMenuItem.Text = "Hide Players";
            this.hidePlayersToolStripMenuItem.CheckedChanged += new System.EventHandler(this.hidePlayersToolStripMenuItem_Click);
            // 
            // skipCutscenesToolStripMenuItem
            // 
            this.skipCutscenesToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.skipCutscenesToolStripMenuItem.CheckOnClick = true;
            this.skipCutscenesToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.skipCutscenesToolStripMenuItem.Name = "skipCutscenesToolStripMenuItem";
            this.skipCutscenesToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.skipCutscenesToolStripMenuItem.Text = "Skip Cutscenes";
            this.skipCutscenesToolStripMenuItem.CheckedChanged += new System.EventHandler(this.skipCutscenesToolStripMenuItem_Click);
            // 
            // disableAnimationsToolStripMenuItem
            // 
            this.disableAnimationsToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.disableAnimationsToolStripMenuItem.CheckOnClick = true;
            this.disableAnimationsToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.disableAnimationsToolStripMenuItem.Name = "disableAnimationsToolStripMenuItem";
            this.disableAnimationsToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.disableAnimationsToolStripMenuItem.Text = "Disable Skill Animations";
            this.disableAnimationsToolStripMenuItem.CheckedChanged += new System.EventHandler(this.disableAnimationsToolStripMenuItem_Click);
            // 
            // untargetSelfToolStripMenuItem
            // 
            this.untargetSelfToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.untargetSelfToolStripMenuItem.CheckOnClick = true;
            this.untargetSelfToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.untargetSelfToolStripMenuItem.Name = "untargetSelfToolStripMenuItem";
            this.untargetSelfToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.untargetSelfToolStripMenuItem.Text = "Untarget Self";
            this.untargetSelfToolStripMenuItem.CheckedChanged += new System.EventHandler(this.untargetSelfToolStripMenuItem_Click);
            // 
            // autosaveStateToolStripMenuItem
            // 
            this.autosaveStateToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.autosaveStateToolStripMenuItem.CheckOnClick = true;
            this.autosaveStateToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.autosaveStateToolStripMenuItem.Name = "autosaveStateToolStripMenuItem";
            this.autosaveStateToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.autosaveStateToolStripMenuItem.Text = "Auto-save State";
            this.autosaveStateToolStripMenuItem.CheckedChanged += new System.EventHandler(this.autosaveStateToolStripMenuItem_Click);
            // 
            // walkSpeedToolStripMenuItem
            // 
            this.walkSpeedToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.walkSpeedToolStripMenuItem.CheckOnClick = true;
            this.walkSpeedToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.walkSpeedToolStripMenuItem.Name = "walkSpeedToolStripMenuItem";
            this.walkSpeedToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.walkSpeedToolStripMenuItem.Text = "Fast Walk Speed";
            this.walkSpeedToolStripMenuItem.CheckedChanged += new System.EventHandler(this.walkSpeedToolStripMenuItem_Click);
            // 
            // bankToolStripMenuItem1
            // 
            this.bankToolStripMenuItem1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.bankToolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.reloadToolStripMenuItem});
            this.bankToolStripMenuItem1.ForeColor = System.Drawing.Color.MediumPurple;
            this.bankToolStripMenuItem1.Name = "bankToolStripMenuItem1";
            this.bankToolStripMenuItem1.Size = new System.Drawing.Size(45, 20);
            this.bankToolStripMenuItem1.Text = "Bank";
            this.bankToolStripMenuItem1.Click += new System.EventHandler(this.bankToolStripMenuItem1_Click);
            // 
            // reloadToolStripMenuItem
            // 
            this.reloadToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.reloadToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(32)))), ((int)(((byte)(71)))));
            this.reloadToolStripMenuItem.Name = "reloadToolStripMenuItem";
            this.reloadToolStripMenuItem.Size = new System.Drawing.Size(110, 22);
            this.reloadToolStripMenuItem.Text = "Reload";
            this.reloadToolStripMenuItem.Click += new System.EventHandler(this.reloadToolStripMenuItem_Click);
            // 
            // pluginsStrip
            // 
            this.pluginsStrip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.pluginsStrip.ForeColor = System.Drawing.Color.MediumPurple;
            this.pluginsStrip.Name = "pluginsStrip";
            this.pluginsStrip.Size = new System.Drawing.Size(58, 20);
            this.pluginsStrip.Text = "Plugins";
            this.pluginsStrip.Click += new System.EventHandler(this.pluginsStrip_Click);
            // 
            // GamePanel
            // 
            this.GamePanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GamePanel.ForeColor = System.Drawing.Color.Transparent;
            this.GamePanel.Controls.Add(this.homeBrowser);
            this.GamePanel.Location = new System.Drawing.Point(0, 24);
            this.GamePanel.Name = "GamePanel";
            this.GamePanel.Size = new System.Drawing.Size(960, 572);
            this.GamePanel.TabIndex = 40;
            // 
            // Root
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(32)))), ((int)(((byte)(40)))));
            this.ClientSize = new System.Drawing.Size(960, 620);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.GamePanel);
            this.Controls.Add(this.metricsPanel);
            this.Controls.Add(this.MenuMain);
            this.ForeColor = System.Drawing.Color.Gainsboro;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Name = "Root";
            this.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Grimoire Exodus";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Root_FormClosing);
            this.Load += new System.EventHandler(this.Root_Load);
            this.Shown += new System.EventHandler(this.Root_Shown);
            this.Resize += new System.EventHandler(this.Root_Resize);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.customTravel_KeyPress);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.metricsPanel.ResumeLayout(false);
            this.MenuStrip1.ResumeLayout(false);
            this.MenuStrip1.PerformLayout();
            this.GamePanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void btnJump_Click(object sender, EventArgs e)
        {
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                if (Player.IsAlive && Player.IsLoggedIn)
                {
                    string Cell = (string)this.cbCells.SelectedItem;
                    string Pad = (string)this.cbPads.SelectedItem;
                    Player.MoveToCell(Cell ?? Player.Cell, Pad ?? Player.Pad);
                }
            }
        }

        private void btnGetCurrentCell_Click(object sender, EventArgs e)
        {
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                if (Player.IsLoggedIn)
                {
                    if (cbCells.Items.Count > 0)
                        cbCells.Items.Clear();
                    cbCells.Items.AddRange(World.Cells);
                    cbCells.Text = Player.Cell;
                    string currentPad = Player.Pad;
                    if (!string.IsNullOrWhiteSpace(currentPad) && !cbPads.Items.Contains(currentPad))
                        cbPads.Items.Add(currentPad);
                    cbPads.SelectedItem = currentPad;
                    cbPads.Text = currentPad;
                }
            }
        }

        private void cosmeticsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(CosmeticForm.Instance);
        }

        private void bankToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(BankForm.Instance);
        }

        private void setsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(Set.Instance);
        }

        public void BotStateChanged(bool IsRunning)
        {
            if (IsRunning)
            {
                startToolStripMenuItem.Enabled = false;
                loadBotToolStripMenuItem.Enabled = false;
                stopToolStripMenuItem.Enabled = true;
            }
            else
            {
                startToolStripMenuItem.Enabled = true;
                loadBotToolStripMenuItem.Enabled = true;
                stopToolStripMenuItem.Enabled = false;
            }

            if (quickBotsMenuItem != null)
            {
                quickBotsMenuItem.Enabled = !IsRunning;
                quickBotsMenuItem.ToolTipText = IsRunning
                    ? "Stop the running bot before loading another GBot"
                    : "Quick load a GBot";
            }

            ClientSession session = CaptureActiveSession();
            string botName = session?.BotDocument.DisplayName ?? "Untitled Bot";
            string botPath = session?.BotDocument.SourcePath;
            UpdateBotTitle(IsRunning, botName);
            StartupDiagnostics.UpdateActivity(
                botPath,
                IsRunning,
                !Visible);
        }

        public void UpdateBotTitle(bool isRunning, string botName)
        {
            string displayName = string.IsNullOrWhiteSpace(botName) ? "Untitled Bot" : botName.Trim();
            displayName = displayName.Replace("\r", " ").Replace("\n", " ");
            Text = isRunning
                ? $"Grimoire Exodus - Running: {displayName}"
                : "Grimoire Exodus";
        }

        private void nTray_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                RestoreFromTray();
            }
        }

        private void Root_Resize(object sender, EventArgs e)
        {
            LayoutClientWorkspace();
            if (startupComplete
                && WindowState == FormWindowState.Minimized
                && AppPreferences.Load().MinimizeToTray)
            {
                BeginInvoke(new Action(delegate { HideToTray(true); }));
            }
        }

        private void HideToTray(bool showHint)
        {
            if (IsDisposed || shutdownInProgress)
                return;

            Hide();
            ShowInTaskbar = false;
            UpdateRecoveryActivity(true);

            if (!showHint)
                return;

            try
            {
                if (!Config.Instance.GetValue<bool>("trayHintShown"))
                {
                    nTray.BalloonTipTitle = "Grimoire Exodus is still running";
                    nTray.BalloonTipText = "Use the tray icon to restore Grimoire, hide it, or exit safely.";
                    nTray.BalloonTipIcon = ToolTipIcon.Info;
                    nTray.ShowBalloonTip(5000);
                    Config.Instance.SetValue("trayHintShown", "true");
                }
            }
            catch (Exception exception)
            {
                StartupDiagnostics.RecordException("Tray notification", exception, false);
            }
        }

        private void RestoreFromTray()
        {
            if (IsDisposed || shutdownInProgress)
                return;

            ShowInTaskbar = true;
            Show();
            WindowState = FormWindowState.Normal;
            BringToFront();
            Activate();
            UpdateRecoveryActivity(false);
        }

        private static void UpdateRecoveryActivity(bool hiddenToTray)
        {
            try
            {
                ClientSession session = Instance?.CaptureActiveSession();
                StartupDiagnostics.UpdateActivity(
                    session?.BotDocument.SourcePath,
                    session?.BotEngine != null && session.BotEngine.IsRunning,
                    hiddenToTray);
            }
            catch (Exception exception)
            {
                StartupDiagnostics.RecordException("Recovery activity", exception, false);
            }
        }

        private void eyeDropperToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            ShowForm(EyeDropper.Instance);
        }

        private void logsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowForm(LogForm.Instance);
        }

        private void notepadToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowForm(Notepad.Instance);
        }

        private void infRangeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isBindingToolbar)
                return;
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                OptionsManager.InfiniteRange = infRangeToolStripMenuItem.Checked;
                BotManager.Instance.chkInfiniteRange.Checked = infRangeToolStripMenuItem.Checked;
            }
        }

        private void provokeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (isBindingToolbar)
                return;
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                OptionsManager.ProvokeMonsters = provokeToolStripMenuItem1.Checked;
                BotManager.Instance.chkProvoke.Checked = provokeToolStripMenuItem1.Checked;
                if (provokeToolStripMenuItem1.Checked && !OptionsManager.IsRunning)
                    _ = OptionsManager.ApplySettings();
            }
        }

        private void provokeToolStripMenuItem1_Clicked(object sender, EventArgs e)
        {
            if (isBindingToolbar)
                return;
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                if (!provokeToolStripMenuItem1.Checked && Player.CurrentState == Player.State.InCombat)
                    BotUtilities.MoveToSelfCell();
            }
        }

        private void provokeAllMonsterInMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isBindingToolbar)
                return;
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                OptionsManager.ProvokeAllMonster = provokeAllMonsterInMapToolStripMenuItem.Checked;
                BotManager.Instance.chkProvokeAllMon.Checked = provokeAllMonsterInMapToolStripMenuItem.Checked;
                if (!provokeAllMonsterInMapToolStripMenuItem.Checked && Player.CurrentState == Player.State.InCombat)
                    BotUtilities.MoveToSelfCell();
                if (provokeAllMonsterInMapToolStripMenuItem.Checked && !OptionsManager.IsRunning)
                    _ = OptionsManager.ApplySettings();
            }
        }

        private void enemyMagnetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isBindingToolbar)
                return;
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                OptionsManager.EnemyMagnet = enemyMagnetToolStripMenuItem.Checked;
                BotManager.Instance.chkMagnet.Checked = enemyMagnetToolStripMenuItem.Checked;
                if (enemyMagnetToolStripMenuItem.Checked && !OptionsManager.IsRunning)
                    _ = OptionsManager.ApplySettings();
            }
        }

        private void lagKillerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isBindingToolbar)
                return;
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                OptionsManager.LagKiller = lagKillerToolStripMenuItem.Checked;
                BotManager.Instance.chkLag.Checked = lagKillerToolStripMenuItem.Checked;
            }
        }

        private void hidePlayersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isBindingToolbar)
                return;
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                OptionsManager.HidePlayers = hidePlayersToolStripMenuItem.Checked;
                BotManager.Instance.chkHidePlayers.Checked = hidePlayersToolStripMenuItem.Checked;
            }
        }

        private void skipCutscenesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isBindingToolbar)
                return;
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                OptionsManager.SkipCutscenes = skipCutscenesToolStripMenuItem.Checked;
                BotManager.Instance.chkSkipCutscenes.Checked = skipCutscenesToolStripMenuItem.Checked;
                if (skipCutscenesToolStripMenuItem.Checked && !OptionsManager.IsRunning)
                    _ = OptionsManager.ApplySettings();
            }
        }

        private void disableAnimationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isBindingToolbar)
                return;
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                OptionsManager.DisableAnimations = disableAnimationsToolStripMenuItem.Checked;
                BotManager.Instance.chkDisableAnims.Checked = disableAnimationsToolStripMenuItem.Checked;
            }
        }

        private void untargetSelfToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isBindingToolbar)
                return;
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                OptionsManager.Untarget = untargetSelfToolStripMenuItem.Checked;
                BotManager.Instance.chkUntarget.Checked = untargetSelfToolStripMenuItem.Checked;
                if (untargetSelfToolStripMenuItem.Checked && !OptionsManager.IsRunning)
                    _ = OptionsManager.ApplySettings();
            }
        }

        private void autosaveStateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isBindingToolbar)
                return;
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                OptionsManager._saveState = autosaveStateToolStripMenuItem.Checked;
                BotManager.Instance.chkSaveState.Checked = autosaveStateToolStripMenuItem.Checked;
            }
        }

        private void walkSpeedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isBindingToolbar)
                return;
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                OptionsManager.WalkSpeed = walkSpeedToolStripMenuItem.Checked ? 40 : 8;
                if (walkSpeedToolStripMenuItem.Checked && !OptionsManager.IsRunning)
                    _ = OptionsManager.ApplySettings();
            }
        }

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        private void MenuMain_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void bankToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                if (Player.IsAlive && Player.IsLoggedIn)
                    Player.Bank.Show();
            }
        }

        private async void reloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                if (Player.IsAlive && Player.IsLoggedIn)
                {
                    await Proxy.Instance.SendToServer($"%xt%zm%loadBank%{World.RoomId}%All%");
                    Player.Bank.SavedItems = Flash.Call<List<InventoryItem>>("GetBankItems", new string[0]);
                }
            }
        }

        private async void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                if (Player.IsAlive && Player.IsLoggedIn && BotManager.Instance.lstCommands.Items.Count > 0)
                {
                    if (!BotManager.Instance.IsHandleCreated)
                    {
                        ShowForm(BotManager.Instance);
                        await Task.Delay(10);
                        BotManager.Instance.Hide();
                    }
                    if (!ReferenceEquals(CaptureActiveSession(), session)
                        || !ReferenceEquals(BotManager.Instance.BoundSession, session))
                        return;
                    BotManager.Instance.btnBotStop.Enabled = false;
                    BotManager.Instance.btnBotPause.Enabled = false;
                    BotManager.Instance.CustomCommandToggle(false);
                    BotManager.Instance.SelectionModeToggle(false);
                    BotManager.Instance.OnBotExecute(true);
                    BotManager.Instance.BotStateChanged(IsRunning: true);
                    await Task.Delay(2000);
                    if (ReferenceEquals(CaptureActiveSession(), session))
                    {
                        BotStateChanged(IsRunning: true);
                        BotManager.Instance.btnBotPause.Enabled = true;
                        BotManager.Instance.btnBotStop.Enabled = true;
                    }
                }
            }
        }

        public void StartBotFromShortcut()
        {
            ClientSession session = CaptureActiveSession();
            if (session?.BotEngine != null && !session.BotEngine.IsRunning)
            {
                startToolStripMenuItem_Click(this, EventArgs.Empty);
            }
        }

        private async void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClientSession session = CaptureActiveSession();
            IBotEngine engine = session?.BotEngine;
            if (session == null || engine == null)
                return;
            BotManager.Instance.btnBotStart.Enabled = false;
            stopToolStripMenuItem.Enabled = false;
            using (ClientSessionContext.Push(session))
                engine.Stop();
            BotManager.Instance.CustomCommandToggle(true);
            BotManager.Instance.SelectionModeToggle(true);
            BotManager.Instance.BotStateChanged(IsRunning: false);
            await Task.Delay(2000);
            if (ReferenceEquals(CaptureActiveSession(), session))
            {
                BotStateChanged(IsRunning: false);
                BotManager.Instance.btnBotStart.Enabled = true;
            }
        }

        public void StopBotFromShortcut()
        {
            ClientSession session = CaptureActiveSession();
            if (session?.BotEngine != null && session.BotEngine.IsRunning)
            {
                stopToolStripMenuItem_Click(this, EventArgs.Empty);
            }
        }

        public void ToggleLagKillerFromShortcut()
        {
            lagKillerToolStripMenuItem.Checked = !lagKillerToolStripMenuItem.Checked;
        }

        public void ToggleHidePlayersFromShortcut()
        {
            hidePlayersToolStripMenuItem.Checked = !hidePlayersToolStripMenuItem.Checked;
        }

        public void RestartAfterFullReset()
        {
            fullResetShutdownRequested = true;
            Application.Restart();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                {
                    cp.Style |= 0x20000 | 0x80000 | 0x40000; //WS_MINIMIZEBOX | WS_SYSMENU | WS_SIZEBOX;
                }
                return cp;
            }
        }

        private void homeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowHome();
        }

        private void FPSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isBindingToolbar)
                return;
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                int framesPerSecond;
                if (!int.TryParse(
                    toolStripTextBox2.Text,
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out framesPerSecond))
                {
                    framesPerSecond = 60;
                }
                framesPerSecond = Math.Max(1, Math.Min(240, framesPerSecond));
                session.OptionState.UseCustomFrameRate = FPSToolStripMenuItem.Checked;
                session.OptionState.DesiredFramesPerSecond = framesPerSecond;
                toolStripTextBox2.Text = framesPerSecond.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (FPSToolStripMenuItem.Checked)
                {
                    Flash.Call("SetFPS", framesPerSecond);
                    session.OptionState.LastAppliedFramesPerSecond = framesPerSecond;
                }
                else
                {
                    Flash.Call("SetFPS", 24);
                    session.OptionState.LastAppliedFramesPerSecond = 24;
                }
                session.OptionState.FrameRateManagedByPerformancePolicy = false;
                ApplyClientFrameRatePolicy(session);
            }
        }

        private void pluginAdded(object sender, ToolStripItemEventArgs e)
        {
            pluginsStrip.DropDownItems.Add(e.Item);
        }

        private void pluginRemoved(object sender, ToolStripItemEventArgs e)
        {
            pluginsStrip.DropDownItems.Remove(e.Item);
        }

        private void managerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(BotManager.Instance);
        }

        private void dPSMeterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(DPSForm.Instance);
        }

        private void commandeditornodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(NodeEditor.Instance);
        }

        private void loadBotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (BotManager.Instance.ShowLoadBotDialog(this))
            {
                ShowForm(BotManager.Instance);
            }
        }

        private void pluginsStrip_Click(object sender, EventArgs e)
        {
            if (!pluginsStrip.HasDropDownItems)
            {
                DarkMessageBox.Show(new Form { TopMost = true, StartPosition = FormStartPosition.CenterScreen }, "No plugins were found. In order to use the plugins, you may have to load them first\r\n" +
       "from the Plugin Manager (which is in Tools' dropdown list on the main menu).", "Plugin Manager", MessageBoxIcon.Error);
            }
        }

        private async void loginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClientSession session = clientSessions?.ActiveSession;
            if (session == null || session.IsDisposed)
                return;

            CancellationTokenSource operation = session.LoginState.BeginOperation();
            login_cts = operation;
            loginBoxToggle(false);
            try
            {
                using (ClientSessionContext.Push(session))
                {
                    if (AutoRelogin.IsTemporarilyKicked)
                        return;

                    IBotEngine engine = session.BotEngine ?? Bot.Instance;
                    if (Player.IsLoggedIn)
                    {
                        Player.Logout();
                        await engine.WaitUntil(() => AutoRelogin.LoginLabel, () => !operation.IsCancellationRequested, 5, 1500);
                    }
                    operation.Token.ThrowIfCancellationRequested();
                    AutoRelogin.LoginExecute();
                    await engine.WaitUntil(() => !AutoRelogin.IsClientLoading("Account"), () => !operation.IsCancellationRequested, 10, 500);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                session.LoginState.CompleteOperation(operation);
                if (ReferenceEquals(login_cts, operation))
                    login_cts = null;
                if (ReferenceEquals(clientSessions?.ActiveSession, session))
                    loginBoxToggle(true);
            }
        }

        private async void toolStripComboBoxLoginServer_Click(object sender, EventArgs e)
        {
            if (isBindingToolbar)
                return;
            if (toolStripComboBoxLoginServer.SelectedIndex == -1)
            {
                return;
            }

            ClientSession session = clientSessions?.ActiveSession;
            Server server = toolStripComboBoxLoginServer.SelectedItem as Server;
            if (session == null || session.IsDisposed || server == null)
                return;

            CancellationTokenSource operation = session.LoginState.BeginOperation();
            login_cts = operation;
            loginBoxToggle(false);
            try
            {
                using (ClientSessionContext.Push(session))
                {
                    if (!AutoRelogin.AreServersLoaded)
                        return;

                    IBotEngine engine = session.BotEngine ?? Bot.Instance;
                    if (Player.IsLoggedIn)
                    {
                        Player.Logout();
                        await engine.WaitUntil(() => AutoRelogin.LoginLabel, () => !operation.IsCancellationRequested, 5, 1500);
                    }
                    operation.Token.ThrowIfCancellationRequested();
                    if (!AutoRelogin.ServerLabel)
                    {
                        AutoRelogin.LoginExecute();
                        await engine.WaitUntil(() => !AutoRelogin.IsClientLoading("Account"), () => !operation.IsCancellationRequested, 10, 500);
                    }
                    operation.Token.ThrowIfCancellationRequested();
                    await AutoRelogin.ForceLogin(server, operation);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
            }
            finally
            {
                session.LoginState.CompleteOperation(operation);
                if (ReferenceEquals(login_cts, operation))
                    login_cts = null;
                if (ReferenceEquals(clientSessions?.ActiveSession, session))
                    loginBoxToggle(true);
            }
        }

        public void loginBoxToggle(bool Type)
        {
            ClientSession session = clientSessions?.ActiveSession;
            if (Type && session != null && session.LoginState.IsBusy)
                Type = false;
            loginToolStripMenuItem.Enabled = Type;
            toolStripComboBoxLoginServer.Enabled = Type;
        }

        public void serverCatch()
        {
            bool previousBinding = isBindingToolbar;
            isBindingToolbar = true;
            try
            {
                if (toolStripComboBoxLoginServer.Items.Count == 0)
                    return;

                int index;
                if (!int.TryParse(BotClientConfig.Instance.GetValue<string>("serverIndex") ?? "0", out index))
                    index = 0;
                toolStripComboBoxLoginServer.SelectedIndex = Math.Max(
                    0,
                    Math.Min(index, toolStripComboBoxLoginServer.Items.Count - 1));
            }
            finally
            {
                isBindingToolbar = previousBinding;
            }
        }

        public void pingMonitorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbPing.Visible = !rtbPing.Visible;
            if (rtbPing.Visible)
            {
                DialogResult result = DarkMessageBox.Show(new Form { TopMost = true, StartPosition = FormStartPosition.CenterScreen }, "Do you wish to save this setting?", "Save Config", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    BotClientConfig.Instance.SetValue("pingMonitor", rtbPing.Visible.ToString());
                }
            }
        }

        public void pingMonitorToggle()
        {
            rtbPing.Visible = BotClientConfig.Instance.GetValue<bool>("pingMonitor");
        }

        public void Root_MenuChanged()
        {
            SetHeaderVisible(!panel1.Visible);
        }

        public void clientHeaderToggle()
        {
            if (BotClientConfig.Instance.GetValue<bool>("clientHeaderToggle"))
            {
                this.Text = "";
                this.Icon = (Icon)global::Properties.Resources.GrimoireIcon.Clone();
            }
        }

        private bool customTravel { get; set; } = true;
        private bool travelInProgress { get; set; } = false;

        private async void customTravel_KeyPress(object sender, KeyEventArgs e)
        {
            ClientSession session = CaptureActiveSession();
            if (session == null)
                return;
            using (ClientSessionContext.Push(session))
            {
                if (Travel.Instance.chkCustomHotkeys.Checked && ModifierKeys == Keys.Shift)
                {
                    if (!Player.IsLoggedIn)
                        return;
                    if (Travel.Instance.travels.Count == 0)
                    {
                        DarkMessageBox.Show(new Form { TopMost = true, StartPosition = FormStartPosition.CenterScreen },
                            "Failed to execute a Custom Travel. Please make sure that you have a pre-existing list.", "Custom Travels", MessageBoxIcon.Error);
                        return;
                    }
                    switch (e.KeyCode)
                    {
                        case Keys.Right:
                            if (!travelInProgress)
                            {
                                World.GameMessage($"Executing the {Travel.Instance.travels[Travel.Instance.cbCustomTravels.SelectedIndex].Split('`')[0]} Travel in 2 seconds.");
                                await Task.Delay(2000);
                                if (!customTravel)
                                    return;
                                Travel.Instance.executeCustomTravel();
                            }
                            break;
                        case Keys.Up:
                            if (!travelInProgress && Travel.Instance.cbCustomTravels.SelectedIndex <= Travel.Instance.cbCustomTravels.Items.Count - 1 && Travel.Instance.cbCustomTravels.SelectedIndex >= 1)
                            {
                                travelInProgress = true;
                                Travel.Instance.cbCustomTravels.SelectedIndex--;
                                string[] newTravel = Travel.Instance.travels[Travel.Instance.cbCustomTravels.SelectedIndex].Split('`');
                                World.GameMessage($"Changing the travel target to {newTravel[0]} Travel.");
                                travelInProgress = false;
                            }
                            break;
                        case Keys.Down:
                            if (!travelInProgress && Travel.Instance.cbCustomTravels.SelectedIndex < Travel.Instance.cbCustomTravels.Items.Count - 1)
                            {
                                travelInProgress = true;
                                Travel.Instance.cbCustomTravels.SelectedIndex++;
                                string[] newTravel = Travel.Instance.travels[Travel.Instance.cbCustomTravels.SelectedIndex].Split('`');
                                World.GameMessage($"Changing the travel target to {newTravel[0]} Travel.");
                                travelInProgress = false;
                            }
                            break;
                        case Keys.Left:
                            if (!travelInProgress)
                            {
                                travelInProgress = true;
                                customTravel = false;
                                World.GameMessage("Travel execution has been canceled. Enabling the travel hotkeys back in 3 seconds.");
                                await Task.Delay(3000);
                                customTravel = true;
                                travelInProgress = false;
                                World.GameMessage("Travel hotkeys has been enabled again.");
                            }
                            break;
                    }
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Retained for plugins that used the legacy initialization entry point.
        /// Normal startup now restores these form-specific options lazily.
        /// </summary>
        public void triggerToggle()
        {
            enableCapture = true;
            BotManager.Instance.chkHideYulgarPlayers.Checked = BotClientConfig.Instance.GetValue<bool>("hideYulgarPlayers");
            BotManager.Instance.chkAntiAfk.Checked = BotClientConfig.Instance.GetValue<bool>("antiAFK");
            BotManager.Instance.chkChangeRoomTag.Checked = BotClientConfig.Instance.GetValue<bool>("anonymousRoom");
            BotManager.Instance.chkChangeChat.Checked = BotClientConfig.Instance.GetValue<bool>("anonymousUser");
            BotManager.Instance.chkExitCombatUponStop.Checked = BotClientConfig.Instance.GetValue<bool>("exitCombatUponStop");
            Travel.Instance.chkCustomHotkeys.Checked = BotClientConfig.Instance.GetValue<bool>("customTravelHotkeys");
            Travel.Instance.chkCustomChatTrigger.Checked = BotClientConfig.Instance.GetValue<bool>("customTravelTrigger");
            Travel.Instance.scanCustomTravel();
            if (Travel.Instance.travels.Count > 0)
            {
                Travel.Instance.cbCustomTravels.SelectedIndex = 0;
            }
        }

        public bool enableCapture { get; set; } = true;
        private const uint WDA_NONE = 0x0;
        private const uint WDA_MONITOR = 0x1;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        public static bool SetDRM(Form winForm, bool Protect)
        {
            bool result = false;
            if (winForm != null)
            {
                if (Protect)
                    result = SetWindowDisplayAffinity(winForm.Handle, WDA_MONITOR);
                else if (winForm.ActiveControl != null)
                {
                    result = SetWindowDisplayAffinity(winForm.Handle, WDA_NONE);
                }
            }
            return result;
        }

    }
}
