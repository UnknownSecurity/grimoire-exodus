using AxShockwaveFlashObjects;
using DarkUI.Forms;
using Grimoire.Botting;
using Grimoire.Clients;
using Grimoire.Game;
using Grimoire.Networking;
using Grimoire.Tools;
using Grimoire.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Grimoire.UI
{
    public partial class Root
    {
        private const int DefaultMaximumDesktopClients = 6;
        private const int UnlimitedDesktopClients = int.MaxValue;
        private const int ClientStripHeight = 30;
        private const int ClientTabWidth = 150;
        private const int ClientTabSpacing = 4;

        private readonly Dictionary<ClientSession, ClientWorkspaceState> clientWorkspaces =
            new Dictionary<ClientSession, ClientWorkspaceState>();
        private Panel clientStripPanel;
        private Panel clientTabsViewport;
        private FlowLayoutPanel clientTabsPanel;
        private Button btnPreviousClientTabs;
        private Button btnNextClientTabs;
        private Button btnNewClient;
        private Button btnCloseClient;
        private ToolTip clientWorkspaceToolTip;
        private int clientTabScrollOffset;
        private bool homeWorkspaceVisible = true;
        private bool pendingAdditionalLaunchRequest;
        private byte[] clientSwfBytes;
        private AppPreferenceSnapshot desktopPreferences;

        private static int GetConfiguredMaximumDesktopClients()
        {
            return AppPreferences.Load().AllowUnlimitedClients
                ? UnlimitedDesktopClients
                : DefaultMaximumDesktopClients;
        }

        private sealed class ClientWorkspaceState
        {
            public ClientSession Session;
            public Panel Host;
            public AxShockwaveFlash FlashControl;
            public Label Placeholder;
            public Button TabButton;
            public bool OpenRequested;
            public bool ShowWhileLoading;
            public AccountCredentials PendingCredentials;
            public int CredentialFillAttempts;
            public System.Windows.Forms.Timer CredentialFillTimer;
            public System.Windows.Forms.Timer FrameRateReadyTimer;
            public bool FrameRatePolicyInitialized;
            public EventHandler StateChanged;
            public EventHandler LoadProgressChanged;
            public EventHandler IdentityChanged;
            public Action<bool> BotRunningChanged;
            public Task ProxyTask;
        }

        private void InitializeMultiClientWorkspace()
        {
            clientWorkspaceToolTip = new ToolTip(components);
            clientStripPanel = new Panel
            {
                AccessibleName = "AQW clients",
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = ToolboxBackgroundColor,
                Height = ClientStripHeight,
                Name = "clientStripPanel",
                TabIndex = 39
            };

            btnCloseClient = CreateClientStripButton("−", "Close selected client", 31);
            btnCloseClient.AccessibleName = "Close selected client";
            btnCloseClient.Dock = DockStyle.Fill;
            btnCloseClient.Click += CloseSelectedClient_Click;

            btnNewClient = CreateClientStripButton("+", "Add AQW client", 31);
            btnNewClient.AccessibleName = "Add AQW client";
            btnNewClient.Dock = DockStyle.Fill;
            btnNewClient.Click += AddClient_Click;

            btnPreviousClientTabs = CreateClientStripButton("‹", "Show earlier client tabs", 28);
            btnPreviousClientTabs.AccessibleName = "Show earlier client tabs";
            btnPreviousClientTabs.Dock = DockStyle.Fill;
            btnPreviousClientTabs.Click += PreviousClientTabs_Click;

            btnNextClientTabs = CreateClientStripButton("›", "Show later client tabs", 28);
            btnNextClientTabs.AccessibleName = "Show later client tabs";
            btnNextClientTabs.Dock = DockStyle.Fill;
            btnNextClientTabs.Click += NextClientTabs_Click;

            clientTabsViewport = new Panel
            {
                AccessibleName = "Client tab viewport",
                AutoScroll = false,
                BackColor = ToolboxBackgroundColor,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                TabIndex = 0
            };
            clientTabsViewport.Resize += ClientTabsViewport_Resize;
            clientTabsViewport.MouseWheel += ClientTabs_MouseWheel;

            clientTabsPanel = new FlowLayoutPanel
            {
                AutoScroll = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = ToolboxBackgroundColor,
                FlowDirection = FlowDirection.LeftToRight,
                Height = 24,
                Location = new Point(4, 3),
                Margin = Padding.Empty,
                Name = "clientTabsPanel",
                Padding = new Padding(4, 3, 4, 2),
                TabIndex = 0,
                WrapContents = false
            };
            clientTabsPanel.MouseWheel += ClientTabs_MouseWheel;

            TableLayoutPanel clientStripLayout = new TableLayoutPanel
            {
                BackColor = ToolboxBackgroundColor,
                ColumnCount = 5,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                RowCount = 1
            };
            clientStripLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28F));
            clientStripLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            clientStripLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28F));
            clientStripLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 31F));
            clientStripLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 31F));
            clientStripLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            clientTabsViewport.Controls.Add(clientTabsPanel);
            clientStripLayout.Controls.Add(btnPreviousClientTabs, 0, 0);
            clientStripLayout.Controls.Add(clientTabsViewport, 1, 0);
            clientStripLayout.Controls.Add(btnNextClientTabs, 2, 0);
            clientStripLayout.Controls.Add(btnNewClient, 3, 0);
            clientStripLayout.Controls.Add(btnCloseClient, 4, 0);

            clientStripPanel.Controls.Add(clientStripLayout);
            Controls.Add(clientStripPanel);
            clientStripPanel.BringToFront();

            clientSessions.SessionRemoving += ClientSessions_SessionRemoving;
            clientSessions.SessionRemoved += ClientSessions_SessionRemoved;
            LayoutClientWorkspace();
            UpdateClientStripButtons();
        }

        private void PreviousClientTabs_Click(object sender, EventArgs e)
        {
            ScrollClientTabs(-(ClientTabWidth + ClientTabSpacing));
        }

        private void NextClientTabs_Click(object sender, EventArgs e)
        {
            ScrollClientTabs(ClientTabWidth + ClientTabSpacing);
        }

        private void ClientTabs_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta == 0)
                return;
            ScrollClientTabs(e.Delta > 0
                ? -(ClientTabWidth + ClientTabSpacing)
                : ClientTabWidth + ClientTabSpacing);
        }

        private void ClientTabsViewport_Resize(object sender, EventArgs e)
        {
            UpdateClientTabNavigation();
        }

        private void ScrollClientTabs(int delta)
        {
            clientTabScrollOffset += delta;
            UpdateClientTabNavigation();
        }

        private void EnsureClientTabVisible(Button tabButton)
        {
            if (tabButton == null || clientTabsViewport == null || clientTabsPanel == null)
                return;

            int viewportWidth = Math.Max(0, clientTabsViewport.ClientSize.Width - 8);
            if (viewportWidth <= 0)
                return;

            int tabLeft = tabButton.Left;
            int tabRight = tabButton.Right;
            if (tabLeft < clientTabScrollOffset)
                clientTabScrollOffset = tabLeft;
            else if (tabRight > clientTabScrollOffset + viewportWidth)
                clientTabScrollOffset = tabRight - viewportWidth;
            UpdateClientTabNavigation();
        }

        private void UpdateClientTabNavigation()
        {
            if (clientTabsViewport == null
                || clientTabsPanel == null
                || btnPreviousClientTabs == null
                || btnNextClientTabs == null)
            {
                return;
            }

            int viewportWidth = Math.Max(0, clientTabsViewport.ClientSize.Width - 8);
            clientTabsPanel.PerformLayout();
            int contentWidth = clientTabsPanel.PreferredSize.Width;
            int maximumOffset = Math.Max(0, contentWidth - viewportWidth);
            clientTabScrollOffset = Math.Max(0, Math.Min(clientTabScrollOffset, maximumOffset));
            clientTabsPanel.Location = new Point(4 - clientTabScrollOffset, 0);
            btnPreviousClientTabs.Enabled = clientTabScrollOffset > 0;
            btnNextClientTabs.Enabled = clientTabScrollOffset < maximumOffset;
        }

        private Button CreateClientStripButton(string text, string toolTip, int width)
        {
            Button button = new Button
            {
                BackColor = Color.FromArgb(36, 40, 50),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.MediumPurple,
                Height = ClientStripHeight,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                TabStop = true,
                Text = text,
                UseVisualStyleBackColor = false,
                Width = width
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(76, 56, 98);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(68, 52, 84);
            button.FlatAppearance.MouseOverBackColor = QuickBotHoverColor;
            clientWorkspaceToolTip.SetToolTip(button, toolTip);
            return button;
        }

        private void LayoutClientWorkspace()
        {
            if (clientStripPanel == null || metricsPanel == null || GamePanel == null)
                return;

            int toolbarHeight = panel1 != null && panel1.Visible ? panel1.Height : 0;
            clientStripPanel.Location = new Point(0, toolbarHeight);
            clientStripPanel.Width = ClientSize.Width;
            int gameTop = toolbarHeight + ClientStripHeight;
            GamePanel.Dock = DockStyle.None;
            GamePanel.Location = new Point(0, gameTop);
            GamePanel.Size = new Size(
                ClientSize.Width,
                Math.Max(1, ClientSize.Height - gameTop - metricsPanel.Height));
        }

        private void AddClient_Click(object sender, EventArgs e)
        {
            TryAddClient(true);
        }

        internal void HandleAdditionalLaunchRequest()
        {
            if (IsDisposed || Disposing)
                return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(HandleAdditionalLaunchRequest));
                }
                catch (InvalidOperationException)
                {
                }
                return;
            }

            if (!startupComplete)
            {
                pendingAdditionalLaunchRequest = true;
                return;
            }

            RestoreFromTray();
            TryAddClient(true);
        }

        private void ProcessPendingAdditionalLaunchRequest()
        {
            if (!pendingAdditionalLaunchRequest || IsDisposed || Disposing)
                return;

            pendingAdditionalLaunchRequest = false;
            RestoreFromTray();
            TryAddClient(true);
        }

        public bool TryAddClient(bool showErrors)
        {
            if (clientSessions == null || clientSessions.Sessions.Count >= clientSessions.MaximumSessions)
            {
                if (showErrors)
                {
                    DarkMessageBox.Show(
                        this,
                        "The six-client safety limit has been reached. You can remove this cap from Settings > Advanced, but additional Flash clients may exhaust the 32-bit process resources.",
                        "Client limit reached",
                        MessageBoxIcon.Information);
                }
                return false;
            }

            ClientSession session = null;
            try
            {
                session = CreateClientSession(GetNextClientDisplayName(), true);
                InitializeClientProxy(session);
                StartClientProxy(session);
                ShowClientSession(session);
                return true;
            }
            catch (Exception exception)
            {
                if (session != null && clientSessions.Sessions.Contains(session))
                    clientSessions.Remove(session);
                StartupDiagnostics.RecordException("Additional client initialization", exception, false);
                if (showErrors)
                {
                    DarkMessageBox.Show(
                        this,
                        "Grimoire could not create another AQW client. The existing client was not changed.\r\n\r\n"
                            + exception.Message,
                        "Add client failed",
                        MessageBoxIcon.Error);
                }
                return false;
            }
        }

        public bool TryOpenAccountInNewClient(AccountCredentials credentials, out string message)
        {
            if (credentials == null)
                throw new ArgumentNullException(nameof(credentials));

            if (clientSessions != null && clientSessions.Sessions.Any(candidate =>
                !candidate.IsDisposed && candidate.IsAssignedToAccount(credentials.Id, credentials.Username)))
            {
                message = "That account is already assigned to an open client.";
                return false;
            }

            if (clientSessions == null || clientSessions.Sessions.Count >= clientSessions.MaximumSessions)
            {
                message = "The six-client safety limit has been reached.";
                return false;
            }

            ClientSession session = null;
            try
            {
                session = CreateClientSession(GetNextClientDisplayName(), true);
                InitializeClientProxy(session);
                StartClientProxy(session);
                session.AssignAccount(credentials.Id, credentials.Username);
                RequestGameOpen(session, credentials, true);
                message = session.IsReady
                    ? "Opening the selected account in a new AQW client."
                    : "Creating a new AQW client. The selected account will log in when it is ready.";
                return true;
            }
            catch (Exception exception)
            {
                if (session != null && clientSessions.Sessions.Contains(session))
                    clientSessions.Remove(session);
                StartupDiagnostics.RecordException("Account client initialization", exception, false);
                message = "Grimoire could not create a client for that account. Existing clients were not changed.";
                return false;
            }
        }

        private string GetNextClientDisplayName()
        {
            for (int index = 1; index <= clientSessions.Sessions.Count + 1; index++)
            {
                string candidate = "Client " + index.ToString("00");
                if (!clientSessions.Sessions.Any(session =>
                    string.Equals(session.DefaultDisplayName, candidate, StringComparison.OrdinalIgnoreCase)))
                {
                    return candidate;
                }
            }
            return "Client " + (clientSessions.Sessions.Count + 1).ToString("00");
        }

        private ClientSession CreateClientSession(string displayName, bool openWhenReady)
        {
            if (clientSessions.Sessions.Count >= clientSessions.MaximumSessions)
                throw new InvalidOperationException("The current client limit has been reached.");

            Panel host = new Panel
            {
                BackColor = Color.Black,
                Dock = DockStyle.Fill,
                Name = "workspace" + displayName.Replace(" ", string.Empty),
                Visible = false
            };
            Label placeholder = new Label
            {
                BackColor = ToolboxBackgroundColor,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                ForeColor = Color.Gainsboro,
                Text = displayName + " · Loading AQW 0%",
                TextAlign = ContentAlignment.MiddleCenter
            };
            AxShockwaveFlash flashControl = new AxShockwaveFlash();
            ActiveXFlashBridge bridge = null;
            ClientSession session = null;
            ClientWorkspaceState workspace = null;

            try
            {
                flashControl.BeginInit();
                flashControl.Name = "Flash" + displayName.Replace(" ", string.Empty);
                flashControl.Dock = DockStyle.Fill;
                flashControl.TabIndex = 0;
                host.Controls.Add(flashControl);
                flashControl.EndInit();
                host.Controls.Add(placeholder);
                placeholder.BringToFront();
                GamePanel.Controls.Add(host);
                ApplyFlashPresentationSettings(flashControl, GetDesktopPreferences());

                bridge = new ActiveXFlashBridge(flashControl);
                session = new ClientSession(displayName, bridge, new Bot());
                workspace = new ClientWorkspaceState
                {
                    Session = session,
                    Host = host,
                    FlashControl = flashControl,
                    Placeholder = placeholder,
                    OpenRequested = openWhenReady,
                    ShowWhileLoading = openWhenReady
                };
                clientWorkspaces.Add(session, workspace);
                AttachClientWorkspace(workspace);
                session.MarkInitializing();
                clientSessions.Add(session);

                if (primarySession == null || primarySession.IsDisposed)
                    primarySession = session;
                Game = clientSessions.ActiveSession?.FlashBridge.Control ?? flashControl;
                Flash.flash = Game;

                ApplyClientSwfState(flashControl);
                RestoreWorkspaceZOrder();
                UpdateClientStripButtons();
                return session;
            }
            catch (Exception exception)
            {
                if (session != null && clientSessions.Sessions.Contains(session))
                    clientSessions.Remove(session);
                else
                {
                    if (workspace != null)
                    {
                        DetachClientWorkspace(workspace);
                        clientWorkspaces.Remove(session);
                    }
                    bridge?.Dispose();
                    if (bridge == null)
                        flashControl.Dispose();
                    host.Dispose();
                }

                StartupDiagnostics.RecordException("Flash initialization", exception, false);
                if (!Program.CheckFlashVersion())
                {
                    DarkMessageBox.Show(
                        new Form { TopMost = true, StartPosition = FormStartPosition.CenterScreen },
                        exception.Message,
                        "Error in Loading the Game!",
                        MessageBoxIcon.Error);
                }
                throw;
            }
        }

        private void ApplyClientSwfState(AxShockwaveFlash flashControl)
        {
            if (clientSwfBytes == null)
            {
                string swfPath = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "grimoire-exodus.swf");
                clientSwfBytes = File.ReadAllBytes(swfPath);
            }

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(8 + clientSwfBytes.Length);
                writer.Write(1432769894);
                writer.Write(clientSwfBytes.Length);
                writer.Write(clientSwfBytes);
                writer.Seek(0, SeekOrigin.Begin);
                flashControl.OcxState = new AxHost.State(stream, 1, false, null);
            }
        }

        private AppPreferenceSnapshot GetDesktopPreferences()
        {
            return desktopPreferences ?? (desktopPreferences = AppPreferences.Load());
        }

        private static void ApplyFlashPresentationSettings(
            AxShockwaveFlash flashControl,
            AppPreferenceSnapshot preferences)
        {
            if (flashControl == null || preferences == null)
                return;

            if (preferences.RenderingProfile != FlashRenderingProfile.Automatic)
            {
                string requestedMode = preferences.RenderingProfile == FlashRenderingProfile.Direct
                    ? "direct"
                    : "gpu";
                string previousMode = null;
                try
                {
                    previousMode = flashControl.WMode;
                    flashControl.WMode = requestedMode;
                }
                catch (Exception exception)
                {
                    TryRestoreWindowMode(flashControl, previousMode);
                    StartupDiagnostics.RecordWarning(
                        "Flash rendering profile fallback (" + preferences.RenderingProfile + ")",
                        exception);
                }
            }

            if (preferences.RenderQuality != FlashRenderQuality.Automatic)
            {
                string previousQuality = null;
                try
                {
                    previousQuality = flashControl.Quality2;
                    flashControl.Quality2 = preferences.RenderQuality.ToString();
                }
                catch (Exception exception)
                {
                    TryRestoreRenderQuality(flashControl, previousQuality);
                    StartupDiagnostics.RecordWarning(
                        "Flash render quality fallback (" + preferences.RenderQuality + ")",
                        exception);
                }
            }
        }

        private static void TryRestoreWindowMode(AxShockwaveFlash flashControl, string previousMode)
        {
            if (string.IsNullOrWhiteSpace(previousMode))
                return;
            try
            {
                flashControl.WMode = previousMode;
            }
            catch
            {
            }
        }

        private static void TryRestoreRenderQuality(AxShockwaveFlash flashControl, string previousQuality)
        {
            if (string.IsNullOrWhiteSpace(previousQuality))
                return;
            try
            {
                flashControl.Quality2 = previousQuality;
            }
            catch
            {
            }
        }

        private void AttachClientWorkspace(ClientWorkspaceState workspace)
        {
            Button tabButton = CreateClientStripButton(
                workspace.Session.DisplayName,
                workspace.Session.DisplayName,
                ClientTabWidth);
            tabButton.AccessibleName = "Show " + workspace.Session.DisplayName;
            tabButton.Height = 24;
            tabButton.Margin = new Padding(0, 0, 4, 0);
            tabButton.Tag = workspace.Session;
            tabButton.Click += ClientTab_Click;
            tabButton.MouseWheel += ClientTabs_MouseWheel;
            workspace.TabButton = tabButton;
            clientTabsPanel.Controls.Add(tabButton);
            UpdateClientTabNavigation();

            workspace.StateChanged = delegate { QueueClientWorkspaceRefresh(workspace); };
            workspace.LoadProgressChanged = delegate { QueueClientWorkspaceRefresh(workspace); };
            workspace.IdentityChanged = delegate { QueueClientWorkspaceRefresh(workspace); };
            workspace.BotRunningChanged = delegate { QueueClientWorkspaceRefresh(workspace); };
            workspace.Session.StateChanged += workspace.StateChanged;
            workspace.Session.LoadProgressChanged += workspace.LoadProgressChanged;
            workspace.Session.IdentityChanged += workspace.IdentityChanged;
            if (workspace.Session.BotEngine != null)
                workspace.Session.BotEngine.IsRunningChanged += workspace.BotRunningChanged;
            UpdateClientWorkspace(workspace);
        }

        private void QueueClientWorkspaceRefresh(ClientWorkspaceState workspace)
        {
            if (IsDisposed || Disposing || workspace == null)
                return;

            Action update = delegate
            {
                if (!workspace.Session.IsDisposed && clientWorkspaces.ContainsKey(workspace.Session))
                    UpdateClientWorkspace(workspace);
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

        private void UpdateClientWorkspace(ClientWorkspaceState workspace)
        {
            ClientSession session = workspace.Session;
            int progress = session.LoadProgress;
            bool ready = session.IsReady;
            workspace.Placeholder.Text = session.State == ClientSessionState.Faulted
                ? session.DisplayName + " · Client initialization failed"
                : ready
                    ? session.DisplayName + " · Ready"
                    : session.DisplayName + " · Loading AQW " + progress + "%";
            workspace.Placeholder.Visible = !ready;
            if (ready && workspace.OpenRequested)
                CompleteGameOpen(workspace);

            if (ready)
                ScheduleInitialFrameRatePolicy(workspace);

            UpdateClientSurfaceVisibility();
            UpdateClientTab(workspace);
            RefreshHomeState();
        }

        private void ScheduleInitialFrameRatePolicy(ClientWorkspaceState workspace)
        {
            if (workspace == null
                || workspace.FrameRatePolicyInitialized
                || workspace.FrameRateReadyTimer != null)
            {
                return;
            }

            // A 100% download callback arrives just before the SWF registers
            // its ExternalInterface methods. Retry once after that event has
            // completed instead of treating an early SetFPS call as applied.
            workspace.FrameRateReadyTimer = new System.Windows.Forms.Timer { Interval = 500 };
            workspace.FrameRateReadyTimer.Tick += delegate
            {
                workspace.FrameRateReadyTimer.Stop();
                workspace.FrameRateReadyTimer.Dispose();
                workspace.FrameRateReadyTimer = null;
                if (workspace.Session.IsDisposed || !workspace.Session.IsReady)
                    return;

                workspace.Session.OptionState.LastAppliedFramesPerSecond = -1;
                ApplyClientFrameRatePolicy(workspace.Session);
                workspace.FrameRatePolicyInitialized = true;
            };
            workspace.FrameRateReadyTimer.Start();
        }

        private void UpdateClientTab(ClientWorkspaceState workspace)
        {
            ClientSession session = workspace.Session;
            bool isActive = ReferenceEquals(clientSessions.ActiveSession, session);
            bool botRunning = session.BotEngine != null && session.BotEngine.IsRunning;
            string status = GetClientStatusText(session, botRunning);
            workspace.TabButton.Text = session.DisplayName + " · " + status;
            workspace.TabButton.AccessibleName = "Show " + session.DisplayName;
            workspace.TabButton.ForeColor = botRunning
                ? Color.LimeGreen
                : session.State == ClientSessionState.Faulted
                    ? Color.IndianRed
                    : isActive
                        ? Color.White
                        : Color.MediumPurple;
            workspace.TabButton.BackColor = isActive
                ? Color.FromArgb(76, 56, 98)
                : Color.FromArgb(36, 40, 50);
            workspace.TabButton.FlatAppearance.BorderColor = isActive
                ? Color.MediumPurple
                : Color.FromArgb(76, 56, 98);
            clientWorkspaceToolTip.SetToolTip(
                workspace.TabButton,
                session.DisplayName + " — " + GetClientStatusDescription(session, botRunning));
        }

        private static string GetClientStatusText(ClientSession session, bool botRunning)
        {
            if (botRunning)
                return "Bot";
            switch (session.State)
            {
                case ClientSessionState.Initializing:
                    return session.LoadProgress + "%";
                case ClientSessionState.Ready:
                    return "Ready";
                case ClientSessionState.Running:
                    return session.GameState.IsLoggedIn ? "Online" : "Open";
                case ClientSessionState.Faulted:
                    return "Error";
                default:
                    return "Starting";
            }
        }

        private static string GetClientStatusDescription(ClientSession session, bool botRunning)
        {
            if (botRunning)
                return "bot running";
            switch (session.State)
            {
                case ClientSessionState.Initializing:
                    return "loading AQW at " + session.LoadProgress + "%";
                case ClientSessionState.Ready:
                    return "AQW is ready";
                case ClientSessionState.Running:
                    return session.GameState.IsLoggedIn ? "logged in" : "AQW is open";
                case ClientSessionState.Faulted:
                    return "client initialization failed";
                default:
                    return session.State.ToString();
            }
        }

        private void ClientTab_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;
            ClientSession session = button?.Tag as ClientSession;
            if (session != null && !session.IsDisposed)
                ShowClientSession(session);
        }

        private void ShowClientSession(ClientSession session)
        {
            if (session == null || session.IsDisposed || !clientWorkspaces.TryGetValue(session, out ClientWorkspaceState workspace))
                return;

            workspace.OpenRequested = true;
            workspace.ShowWhileLoading = true;
            homeWorkspaceVisible = false;
            clientSessions.SetActive(session);
            UpdateClientSurfaceVisibility();
            workspace.Host.BringToFront();
            if (session.IsReady)
                CompleteGameOpen(workspace);
            else
                workspace.Placeholder.BringToFront();
            EnsureClientTabVisible(workspace.TabButton);
            UpdateClientStripButtons();
        }

        private void RequestGameOpen(ClientSession session, AccountCredentials credentials, bool showWhileLoading)
        {
            if (session == null || session.IsDisposed || !clientWorkspaces.TryGetValue(session, out ClientWorkspaceState workspace))
                return;

            StopCredentialFill(session);
            workspace.PendingCredentials = credentials;
            workspace.OpenRequested = true;
            workspace.ShowWhileLoading = showWhileLoading;
            clientSessions.SetActive(session);
            if (session.IsReady)
            {
                CompleteGameOpen(workspace);
            }
            else if (showWhileLoading)
            {
                homeWorkspaceVisible = false;
                UpdateClientSurfaceVisibility();
                workspace.Host.BringToFront();
                workspace.Placeholder.BringToFront();
            }
            RefreshHomeState();
        }

        private void CompleteGameOpen(ClientWorkspaceState workspace)
        {
            if (!workspace.Session.IsReady || workspace.Session.IsDisposed)
                return;

            workspace.OpenRequested = false;
            if (workspace.Session.State != ClientSessionState.Running)
                workspace.Session.MarkRunning();

            if (ReferenceEquals(clientSessions.ActiveSession, workspace.Session)
                && (homeWorkspaceVisible || workspace.ShowWhileLoading))
            {
                homeWorkspaceVisible = false;
                UpdateClientSurfaceVisibility();
                workspace.Host.BringToFront();
                workspace.FlashControl.Focus();
            }

            workspace.ShowWhileLoading = false;
            if (workspace.PendingCredentials != null)
                StartCredentialFill(workspace);
            UpdateClientTab(workspace);
            RefreshHomeState();
        }

        private void StartCredentialFill(ClientWorkspaceState workspace)
        {
            StopCredentialFill(workspace.Session, false);
            workspace.CredentialFillAttempts = 0;
            workspace.CredentialFillTimer = new System.Windows.Forms.Timer { Interval = 250 };
            workspace.CredentialFillTimer.Tick += delegate { TryApplyPendingCredentials(workspace); };
            workspace.CredentialFillTimer.Start();
            TryApplyPendingCredentials(workspace);
        }

        private void TryApplyPendingCredentials(ClientWorkspaceState workspace)
        {
            if (workspace.PendingCredentials == null || workspace.Session.IsDisposed)
            {
                StopCredentialFill(workspace.Session);
                return;
            }

            workspace.CredentialFillAttempts++;
            using (ClientSessionContext.Push(workspace.Session))
            {
                if (AutoRelogin.LoginLabel)
                {
                    Flash.Instance.SetGameObject("mcLogin.ni.text", workspace.PendingCredentials.Username);
                    Flash.Instance.SetGameObject("mcLogin.pi.text", workspace.PendingCredentials.Password);
                    AutoRelogin.LoginExecute();
                    StopCredentialFill(workspace.Session);
                }
                else if (workspace.CredentialFillAttempts >= 60)
                {
                    StopCredentialFill(workspace.Session);
                    workspace.Session.ReleaseAssignedAccount();
                }
            }
        }

        private void StopCredentialFill()
        {
            foreach (ClientSession session in clientWorkspaces.Keys.ToArray())
                StopCredentialFill(session);
            pendingAccount = null;
        }

        private void StopCredentialFill(ClientSession session, bool clearCredentials = true)
        {
            if (session == null || !clientWorkspaces.TryGetValue(session, out ClientWorkspaceState workspace))
                return;

            if (workspace.CredentialFillTimer != null)
            {
                workspace.CredentialFillTimer.Stop();
                workspace.CredentialFillTimer.Dispose();
                workspace.CredentialFillTimer = null;
            }
            if (clearCredentials)
                workspace.PendingCredentials = null;
            workspace.CredentialFillAttempts = 0;
        }

        private void ShowHomeWorkspace()
        {
            homeWorkspaceVisible = true;
            UpdateClientSurfaceVisibility();
            homeBrowser.BringToFront();
            UpdateClientStripButtons();
            RefreshHomeState();
        }

        private void RestoreWorkspaceZOrder()
        {
            UpdateClientSurfaceVisibility();
            if (homeWorkspaceVisible)
            {
                homeBrowser.BringToFront();
                return;
            }

            ClientSession active = clientSessions.ActiveSession;
            if (active != null && clientWorkspaces.TryGetValue(active, out ClientWorkspaceState workspace))
                workspace.Host.BringToFront();
        }

        private void OnActiveClientSessionChanged(ClientSession previous, ClientSession current)
        {
            Game = current?.FlashBridge.Control;
            Flash.flash = Game;
            foreach (ClientWorkspaceState workspace in clientWorkspaces.Values)
                UpdateClientTab(workspace);

            UpdateClientSurfaceVisibility();

            if (!homeWorkspaceVisible
                && current != null
                && clientWorkspaces.TryGetValue(current, out ClientWorkspaceState currentWorkspace))
            {
                currentWorkspace.Host.BringToFront();
                if (current.IsReady)
                    currentWorkspace.FlashControl.Focus();
                else
                    currentWorkspace.Placeholder.BringToFront();
                EnsureClientTabVisible(currentWorkspace.TabButton);
            }
            ApplyClientFrameRatePolicies();
            UpdateClientStripButtons();
            RefreshHomeState();
        }

        private void UpdateClientSurfaceVisibility()
        {
            if (homeBrowser != null)
                homeBrowser.Visible = homeWorkspaceVisible;

            ClientSession activeSession = clientSessions?.ActiveSession;
            foreach (ClientWorkspaceState workspace in clientWorkspaces.Values)
            {
                bool isActive = ReferenceEquals(activeSession, workspace.Session);
                bool botRunning = workspace.Session.BotEngine != null
                    && workspace.Session.BotEngine.IsRunning;
                bool shouldRender = ClientSurfacePolicy.ShouldRenderClientSurface(
                    homeWorkspaceVisible,
                    isActive,
                    botRunning);
                if (workspace.Host.Visible != shouldRender)
                    workspace.Host.Visible = shouldRender;
            }
        }

        private void ApplyClientFrameRatePolicies()
        {
            if (clientSessions == null)
                return;

            foreach (ClientSession session in clientSessions.Sessions)
                ApplyClientFrameRatePolicy(session);
        }

        private void ApplyClientFrameRatePolicy(ClientSession session)
        {
            if (session == null || session.IsDisposed || !session.IsReady)
                return;

            ClientOptionState options = session.OptionState;
            bool botRunning = session.BotEngine != null && session.BotEngine.IsRunning;
            if (botRunning && !options.FrameRateManagedByPerformancePolicy)
                return;

            AppPreferenceSnapshot preferences = GetDesktopPreferences();
            bool isActive = ReferenceEquals(clientSessions.ActiveSession, session);
            bool hasExplicitFrameRate = options.UseCustomFrameRate;
            bool throttle = !isActive
                && !botRunning
                && preferences.ThrottleInactiveClients;
            bool useActiveClientFrameRate = isActive
                && !hasExplicitFrameRate
                && preferences.UseActiveClientFrameRate;
            if (!hasExplicitFrameRate
                && !throttle
                && !useActiveClientFrameRate
                && !options.FrameRateManagedByPerformancePolicy)
            {
                return;
            }

            int desiredFrameRate = hasExplicitFrameRate
                ? options.DesiredFramesPerSecond
                : useActiveClientFrameRate
                    ? ClampFrameRate(preferences.ActiveClientFramesPerSecond, 1, 240)
                    : 24;
            int effectiveFrameRate = throttle
                ? ClampFrameRate(preferences.InactiveClientFramesPerSecond, 1, 60)
                : desiredFrameRate;
            bool managed = throttle || useActiveClientFrameRate;

            if (options.LastAppliedFramesPerSecond == effectiveFrameRate)
            {
                options.FrameRateManagedByPerformancePolicy = managed;
                return;
            }

            session.FlashBridge.Call<object>("SetFPS", effectiveFrameRate);
            options.LastAppliedFramesPerSecond = effectiveFrameRate;
            options.FrameRateManagedByPerformancePolicy = managed;
        }

        private static int ClampFrameRate(int value, int minimum, int maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private void CloseSelectedClient_Click(object sender, EventArgs e)
        {
            CloseClient(CaptureActiveSession(), true);
        }

        public bool CloseClientById(string id, bool confirm)
        {
            ClientSession session = FindClientById(id);
            return CloseClient(session, confirm);
        }

        private bool CloseClient(ClientSession session, bool confirm)
        {
            if (session == null || session.IsDisposed)
                return false;
            if (clientSessions.Sessions.Count <= 1)
            {
                DarkMessageBox.Show(
                    this,
                    "Grimoire keeps one AQW client available. Add another client before closing this one.",
                    "Close client",
                    MessageBoxIcon.Information);
                return false;
            }

            bool botRunning = session.BotEngine != null && session.BotEngine.IsRunning;
            bool loggedIn = session.GameState.IsLoggedIn;
            if (confirm && (botRunning || loggedIn))
            {
                string warning = botRunning
                    ? "A bot is running in " + session.DisplayName + ". Closing this client will stop that bot and end only this AQW session."
                    : session.DisplayName + " is logged in. Closing it will end only this AQW session.";
                DialogResult result = DarkMessageBox.Show(
                    this,
                    warning + "\r\n\r\nClose this client?",
                    "Close " + session.DisplayName + "?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                    return false;
            }

            if (botRunning)
            {
                using (ClientSessionContext.Push(session))
                    session.BotEngine.Stop();
            }
            return clientSessions.Remove(session);
        }

        private void ClientSessions_SessionRemoving(object sender, ClientSessionEventArgs e)
        {
            if (!clientWorkspaces.TryGetValue(e.Session, out ClientWorkspaceState workspace))
                return;

            StopCredentialFill(e.Session);
            DetachClientWorkspace(workspace);
            clientTabsPanel.Controls.Remove(workspace.TabButton);
            GamePanel.Controls.Remove(workspace.Host);
        }

        private void ClientSessions_SessionRemoved(object sender, ClientSessionEventArgs e)
        {
            if (clientWorkspaces.TryGetValue(e.Session, out ClientWorkspaceState workspace))
            {
                clientWorkspaces.Remove(e.Session);
                workspace.TabButton.Dispose();
                workspace.Host.Dispose();
            }

            if (ReferenceEquals(primarySession, e.Session))
                primarySession = clientSessions.Sessions.FirstOrDefault();
            RestoreWorkspaceZOrder();
            UpdateClientStripButtons();
            RefreshHomeState();
        }

        private void DetachClientWorkspace(ClientWorkspaceState workspace)
        {
            if (workspace.FrameRateReadyTimer != null)
            {
                workspace.FrameRateReadyTimer.Stop();
                workspace.FrameRateReadyTimer.Dispose();
                workspace.FrameRateReadyTimer = null;
            }
            if (workspace.StateChanged != null)
                workspace.Session.StateChanged -= workspace.StateChanged;
            if (workspace.LoadProgressChanged != null)
                workspace.Session.LoadProgressChanged -= workspace.LoadProgressChanged;
            if (workspace.IdentityChanged != null)
                workspace.Session.IdentityChanged -= workspace.IdentityChanged;
            if (workspace.BotRunningChanged != null && workspace.Session.BotEngine != null)
                workspace.Session.BotEngine.IsRunningChanged -= workspace.BotRunningChanged;
            workspace.TabButton.Click -= ClientTab_Click;
            workspace.TabButton.MouseWheel -= ClientTabs_MouseWheel;
        }

        private void UpdateClientStripButtons()
        {
            if (btnNewClient == null || btnCloseClient == null || clientSessions == null)
                return;
            btnNewClient.Enabled = clientSessions.Sessions.Count < clientSessions.MaximumSessions;
            btnCloseClient.Enabled = clientSessions.Sessions.Count > 1 && clientSessions.ActiveSession != null;
            clientWorkspaceToolTip.SetToolTip(
                btnNewClient,
                btnNewClient.Enabled ? "Add AQW client" : "Six-client safety limit reached");
            UpdateClientTabNavigation();
        }

        private void InitializeClientProxy(ClientSession session)
        {
            if (session == null || session.IsDisposed || session.PacketDispatcher != null)
                return;
            using (ClientSessionContext.Push(session))
                Proxy.InitializeProxyHandlers();
        }

        private void StartClientProxy(ClientSession session)
        {
            if (session == null || session.IsDisposed || session.PacketDispatcher == null)
                return;
            if (!clientWorkspaces.TryGetValue(session, out ClientWorkspaceState workspace)
                || workspace.ProxyTask != null)
            {
                return;
            }

            Proxy proxy = session.PacketDispatcher;
            workspace.ProxyTask = Task.Factory
                .StartNew(async delegate
                {
                    using (ClientSessionContext.Push(session))
                        await proxy.ClientExecute();
                }, TaskCreationOptions.LongRunning)
                .Unwrap();
            workspace.ProxyTask.ContinueWith(
                task => StartupDiagnostics.RecordException("Proxy client", task.Exception, false),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        private ClientSession FindClientById(string id)
        {
            if (!Guid.TryParse(id, out Guid sessionId) || clientSessions == null)
                return null;
            return clientSessions.Sessions.FirstOrDefault(session => session.Id == sessionId);
        }

        public bool ShowClientById(string id)
        {
            ClientSession session = FindClientById(id);
            if (session == null)
                return false;
            ShowClientSession(session);
            return true;
        }

        public object[] HomeClientSnapshots => clientSessions == null
            ? new object[0]
            : clientSessions.Sessions.Select(session => (object)new
            {
                id = session.Id.ToString("N"),
                name = session.DisplayName,
                state = session.State.ToString(),
                loadProgress = session.LoadProgress,
                selected = ReferenceEquals(clientSessions.ActiveSession, session),
                botRunning = session.BotEngine != null && session.BotEngine.IsRunning,
                running = session.State == ClientSessionState.Running
            }).ToArray();

        public object[] HomeAccountSnapshots => AccountVault.GetAccounts()
            .Select(account => (object)new
            {
                id = account.Id,
                label = account.Label,
                username = account.Username,
                active = clientSessions != null && clientSessions.Sessions.Any(session =>
                    !session.IsDisposed && session.IsAssignedToAccount(account.Id, account.Username))
            })
            .ToArray();

        public bool CanAddClient => clientSessions != null
            && clientSessions.Sessions.Count < clientSessions.MaximumSessions;
    }
}
