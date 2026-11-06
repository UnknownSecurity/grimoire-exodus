using Grimoire.Clients;
using Grimoire.Game;
using Grimoire.UI.Modern;
using Grimoire.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Grimoire.UI
{
    public partial class Root
    {
        private const int ModernResizeBorderWidth = 6;
        private const int ModernMinimumWidthLogicalPixels = 960;
        private const int ModernMinimumHeightLogicalPixels = 620;
        private const int DwmNcRenderingPolicy = 2;
        private const int DwmNcRenderingEnabled = 2;
        private const int WmNcCalcSize = 0x0083;
        private const int WmNcHitTest = 0x0084;
        private const int HtClient = 1;
        private const int HtLeft = 10;
        private const int HtRight = 11;
        private const int HtTop = 12;
        private const int HtTopLeft = 13;
        private const int HtTopRight = 14;
        private const int HtBottom = 15;
        private const int HtBottomLeft = 16;
        private const int HtBottomRight = 17;
        private const int ModernMaximizeEdgeCompensationLogicalPixels = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeNcCalcSizeParams
        {
            public NativeRect ProposedClient;
            public NativeRect PreviousWindow;
            public NativeRect PreviousClient;
            public IntPtr WindowPosition;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeDwmMargins
        {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }

        [DllImport("dwmapi.dll", CallingConvention = CallingConvention.Winapi, PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(
            IntPtr windowHandle,
            int attribute,
            ref int attributeValue,
            int attributeSize);

        [DllImport("dwmapi.dll", CallingConvention = CallingConvention.Winapi, PreserveSig = true)]
        private static extern int DwmExtendFrameIntoClientArea(
            IntPtr windowHandle,
            ref NativeDwmMargins margins);

        private bool modernShellRequested;
        private bool modernShellActive;
        private bool modernWindowFramePrepared;
        private bool modernStartupPresentationPending;
        private bool legacyHeaderWasVisible;
        private bool legacyMetricsWereVisible;
        private bool legacyClientStripWasVisible;
        private double legacyWindowOpacity = 1d;
        private Rectangle modernGameViewportBounds = Rectangle.Empty;
        private Rectangle legacyGamePanelBounds;
        private Size legacyMinimumSize;
        private Padding legacyRootPadding;
        private DockStyle legacyGamePanelDock;
        private FormBorderStyle legacyFormBorderStyle;
        private ModernShellHost modernShellHost;

        internal bool IsModernShellActive => modernShellActive;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyModernWindowShadow();
        }

        private void InitializeModernShellFeature()
        {
            modernShellRequested = ModernShellFeature.IsEnabledForCurrentProcess();
            if (modernShellRequested)
            {
                legacyWindowOpacity = Opacity;
                modernStartupPresentationPending = true;
                Opacity = 0d;
                PrepareModernWindowFrame();
            }
        }

        private async Task TryInitializeModernShellAsync()
        {
            if (!modernShellRequested || modernShellActive || modernShellHost != null || IsDisposed)
                return;

            ModernShellHost host = new ModernShellHost(new RootModernUiApplication(this));
            host.Visible = true;
            host.GameViewportChanged += ModernShellHost_GameViewportChanged;
            host.ShellFailed += ModernShellHost_ShellFailed;
            modernShellHost = host;
            GamePanel.Controls.Add(host);
            host.SendToBack();

            try
            {
                await host.InitializeAsync();
                if (IsDisposed || Disposing || !ReferenceEquals(modernShellHost, host))
                    return;
                ActivateModernShell();
            }
            catch (Exception exception)
            {
                if (IsDisposed || Disposing || host.IsDisposed)
                    return;

                StartupDiagnostics.RecordException("Modern UI startup", exception, false);
                FallbackToLegacyShell(host);
            }
        }

        private void ActivateModernShell()
        {
            legacyHeaderWasVisible = panel1.Visible;
            legacyMetricsWereVisible = metricsPanel.Visible;
            legacyClientStripWasVisible = clientStripPanel != null && clientStripPanel.Visible;
            modernShellActive = true;
            panel1.Visible = false;
            metricsPanel.Visible = false;
            if (clientStripPanel != null)
                clientStripPanel.Visible = false;
            homeBrowser.Visible = false;
            modernShellHost.Visible = true;
            ApplyModernWindowFrameLayout();
            LayoutClientWorkspace();
            UpdateClientSurfaceVisibility();
            RestoreWorkspaceZOrder();
            modernShellHost.RequestStatePublication();
            CompleteModernStartupPresentation();
        }

        private void ModernShellHost_GameViewportChanged(ModernGameViewport viewport)
        {
            if (modernShellHost == null)
                return;

            Rectangle nextBounds = ModernUiProtocol.MapViewport(viewport, modernShellHost.ClientSize);
            if (!ModernUiProtocol.ShouldApplyViewport(modernGameViewportBounds, nextBounds))
                return;

            modernGameViewportBounds = nextBounds;
            if (!modernShellActive)
                return;
            ApplyModernClientSurfaceLayout();
            RestoreWorkspaceZOrder();
        }

        private void ModernShellHost_ShellFailed(Exception exception)
        {
            StartupDiagnostics.RecordException("Modern UI runtime", exception, false);
            FallbackToLegacyShell(modernShellHost);
        }

        private void FallbackToLegacyShell(ModernShellHost failedHost)
        {
            if (failedHost != null)
            {
                failedHost.GameViewportChanged -= ModernShellHost_GameViewportChanged;
                failedHost.ShellFailed -= ModernShellHost_ShellFailed;
                GamePanel.Controls.Remove(failedHost);
                failedHost.Dispose();
            }
            if (ReferenceEquals(modernShellHost, failedHost))
                modernShellHost = null;

            bool wasActive = modernShellActive;
            modernShellActive = false;
            modernGameViewportBounds = Rectangle.Empty;
            RestoreLegacyWindowFrame();
            if (wasActive)
            {
                panel1.Visible = legacyHeaderWasVisible;
                metricsPanel.Visible = legacyMetricsWereVisible;
                if (clientStripPanel != null)
                    clientStripPanel.Visible = legacyClientStripWasVisible;
            }
            foreach (ClientWorkspaceState workspace in clientWorkspaces.Values)
            {
                workspace.Host.Dock = DockStyle.Fill;
                workspace.Host.Bounds = GamePanel.ClientRectangle;
            }
            LayoutClientWorkspace();
            UpdateClientSurfaceVisibility();
            RestoreWorkspaceZOrder();
            CompleteModernStartupPresentation();
        }

        private void CompleteModernStartupPresentation()
        {
            if (!modernStartupPresentationPending || IsDisposed || Disposing)
                return;

            modernStartupPresentationPending = false;
            Opacity = legacyWindowOpacity;
        }

        private void ApplyModernClientSurfaceLayout()
        {
            if (!modernShellActive)
                return;

            Rectangle bounds = modernGameViewportBounds;
            foreach (ClientWorkspaceState workspace in clientWorkspaces.Values)
            {
                workspace.Host.Dock = DockStyle.None;
                bool requiresVisibleInitialization = workspace.Session.FlashBridge.Engine == GameEngineKind.Ruffle
                    && workspace.Session.State == ClientSessionState.Initializing;
                workspace.Host.Bounds = bounds.IsEmpty && requiresVisibleInitialization
                    ? GamePanel.ClientRectangle
                    : bounds;
            }
        }

        private void ApplyModernWorkspaceZOrder()
        {
            if (!modernShellActive || modernShellHost == null)
                return;

            modernShellHost.BringToFront();
            if (homeWorkspaceVisible || modernGameViewportBounds.IsEmpty)
                return;

            ClientSession active = clientSessions?.ActiveSession;
            if (active != null && clientWorkspaces.TryGetValue(active, out ClientWorkspaceState workspace))
                workspace.Host.BringToFront();
        }

        private void RequestModernUiStateRefresh()
        {
            modernShellHost?.RequestStatePublication();
        }

        internal JObject BuildModernUiBootstrap()
        {
            ClientSession active = CaptureActiveSession();
            JArray sessions = new JArray();
            if (clientSessions != null)
            {
                foreach (ClientSession session in clientSessions.Sessions)
                {
                    bool botRunning = session.BotEngine != null && session.BotEngine.IsRunning;
                    int commandCount = session.BotEngine?.Configuration?.Commands?.Count
                        ?? session.BotDocument.Configuration?.Commands?.Count
                        ?? 0;
                    sessions.Add(new JObject
                    {
                        ["id"] = session.Id.ToString("D"),
                        ["title"] = session.DisplayName,
                        ["state"] = session.State.ToString(),
                        ["engine"] = session.FlashBridge.Engine.ToString(),
                        ["isActive"] = ReferenceEquals(active, session),
                        ["isLoggedIn"] = session.GameState.IsLoggedIn,
                        ["loadProgress"] = session.LoadProgress,
                        ["ping"] = session.LatestPingLatency.HasValue
                            ? new JValue(session.LatestPingLatency.Value)
                            : JValue.CreateNull(),
                        ["botRunning"] = botRunning,
                        ["botName"] = session.BotDocument.DisplayName,
                        ["botIndex"] = session.BotEngine?.Index ?? 0,
                        ["commandCount"] = commandCount
                    });
                }
            }

            bool activeBotRunning = active?.BotEngine != null && active.BotEngine.IsRunning;
            int activeCommandCount = active?.BotEngine?.Configuration?.Commands?.Count
                ?? active?.BotDocument.Configuration?.Commands?.Count
                ?? 0;
            int activeIndex = active?.BotEngine?.Index ?? 0;
            string activeAction = null;
            if (active != null
                && active.BotEngine?.Configuration?.Commands != null
                && activeIndex >= 0
                && activeIndex < active.BotEngine.Configuration.Commands.Count)
            {
                activeAction = active.BotEngine.Configuration.Commands[activeIndex]?.GetType().Name;
            }

            return new JObject
            {
                ["version"] = AppVersion,
                ["homeVisible"] = homeWorkspaceVisible,
                ["hideDeviceStats"] = GetDesktopPreferences().HideDeviceStats,
                ["windowState"] = WindowState.ToString(),
                ["activeSessionId"] = active?.Id.ToString("D"),
                ["multiClientEnabled"] = IsMultiClientEnabled,
                ["canCreateClient"] = IsMultiClientEnabled
                    && clientSessions != null
                    && clientSessions.Sessions.Count < clientSessions.MaximumSessions,
                ["sessions"] = sessions,
                ["activeBot"] = new JObject
                {
                    ["running"] = activeBotRunning,
                    ["name"] = active?.BotDocument.DisplayName ?? "Untitled Bot",
                    ["index"] = activeIndex,
                    ["commandCount"] = activeCommandCount,
                    ["activeAction"] = activeAction,
                    ["runtime"] = FormatModernRuntime()
                },
                ["location"] = BuildModernLocationState(active),
                ["options"] = BuildModernOptionsState(active)
            };
        }

        internal JToken InvokeModernUiAction(string method, JObject parameters)
        {
            switch (method)
            {
                case "app.getBootstrap":
                    return BuildModernUiBootstrap();
                case "navigation.home":
                    ShowHome();
                    return Accepted();
                case "navigation.client":
                    ClientSession activeSession = CaptureActiveSession();
                    if (activeSession == null || activeSession.IsDisposed)
                    {
                        throw new ModernUiActionException(
                            "SESSION_NOT_FOUND",
                            "Create an AQW client before opening this workspace.");
                    }
                    ShowModernSession(activeSession);
                    return Accepted();
                case "sessions.activate":
                    ShowModernSession(RequireModernSession(parameters));
                    return Accepted();
                case "sessions.create":
                    if (!TryAddClient(false))
                        throw new ModernUiActionException(
                            "CLIENT_CREATE_UNAVAILABLE",
                            IsMultiClientEnabled
                                ? "Another AQW client could not be created."
                                : "Enable experimental multi-client mode in Settings > General first.");
                    return Accepted();
                case "sessions.close":
                    if (!CloseClient(RequireModernSession(parameters), true))
                        throw new ModernUiActionException("CLIENT_NOT_CLOSED", "The client was not closed.");
                    return Accepted();
                case "bots.start":
                    ShowModernSession(RequireModernSession(parameters));
                    StartBotFromShortcut();
                    return Accepted();
                case "bots.stop":
                    ShowModernSession(RequireModernSession(parameters));
                    StopBotFromShortcut();
                    return Accepted();
                case "location.refresh":
                    RefreshModernLocation(RequireActiveModernSession(parameters));
                    return Accepted();
                case "location.move":
                    MoveModernLocation(
                        RequireActiveModernSession(parameters),
                        parameters.Value<string>("cell"),
                        parameters.Value<string>("pad"));
                    return Accepted();
                case "options.toggle":
                    ToggleModernOption(RequireModernSession(parameters), parameters.Value<string>("option"));
                    return Accepted();
                case "tools.open":
                    OpenModernTool(parameters.Value<string>("tool"));
                    return Accepted();
                case "home.getState":
                    return ParseHomeBridgeResponse(homeBridge.GetState());
                case "home.accounts.save":
                    return ParseHomeBridgeResponse(homeBridge.SaveAccount(
                        parameters.Value<string>("label"),
                        parameters.Value<string>("username"),
                        parameters.Value<string>("password")));
                case "home.accounts.remove":
                    return ParseHomeBridgeResponse(homeBridge.RemoveAccount(parameters.Value<string>("id")));
                case "home.accounts.open":
                    return ParseHomeBridgeResponse(homeBridge.OpenAccount(parameters.Value<string>("id")));
                case "home.botFolder":
                    return ParseHomeBridgeResponse(homeBridge.OpenBotFolder());
                case "home.doctor.begin":
                    return ParseHomeBridgeResponse(homeBridge.BeginCompatibilityCheck());
                case "home.doctor.status":
                    return ParseHomeBridgeResponse(homeBridge.GetCompatibilityCheckStatus(
                        parameters.Value<string>("requestId")));
                case "home.doctor.copy":
                    return ParseHomeBridgeResponse(homeBridge.CopyCompatibilityCheckResults(
                        parameters.Value<string>("requestId")));
                case "home.report.begin":
                    return ParseHomeBridgeResponse(homeBridge.BeginIssueReport(
                        parameters.Value<string>("category"),
                        parameters.Value<string>("subject"),
                        parameters.Value<string>("version"),
                        parameters.Value<string>("details"),
                        parameters.Value<string>("steps"),
                        false,
                        parameters.Value<double?>("startedAt") ?? 0d));
                case "home.report.status":
                    return ParseHomeBridgeResponse(homeBridge.GetIssueReportStatus(
                        parameters.Value<string>("requestId")));
                case "home.link.open":
                    return OpenModernHomeLink(parameters.Value<string>("address"));
                case "window.beginDrag":
                    QueueModernWindowDrag();
                    return Accepted();
                case "window.minimize":
                    WindowState = FormWindowState.Minimized;
                    return Accepted();
                case "window.toggleMaximize":
                    ToggleModernWindowState();
                    return Accepted();
                case "window.close":
                    BeginInvoke(new Action(delegate
                    {
                        if (!IsDisposed && !Disposing)
                            Close();
                    }));
                    return Accepted();
                default:
                    throw new ModernUiActionException("UNKNOWN_METHOD", "The requested UI action is not available.");
            }
        }

        private static JObject Accepted()
        {
            return new JObject { ["accepted"] = true };
        }

        private ClientSession RequireModernSession(JObject parameters)
        {
            string id = parameters.Value<string>("sessionId");
            ClientSession session = FindClientById(id);
            if (session == null || session.IsDisposed)
                throw new ModernUiActionException("SESSION_NOT_FOUND", "That AQW client is no longer available.");
            return session;
        }

        private ClientSession RequireActiveModernSession(JObject parameters)
        {
            ClientSession session = RequireModernSession(parameters);
            if (!ReferenceEquals(CaptureActiveSession(), session))
            {
                throw new ModernUiActionException(
                    "SESSION_NOT_ACTIVE",
                    "Select that AQW client before changing its location.");
            }
            return session;
        }

        private JObject BuildModernLocationState(ClientSession session)
        {
            return new JObject
            {
                ["available"] = session != null && session.GameState.IsLoggedIn,
                ["cell"] = cbCells.Text ?? string.Empty,
                ["pad"] = cbPads.Text ?? string.Empty,
                ["cells"] = new JArray(cbCells.Items.Cast<object>()
                    .Select(item => item == null ? string.Empty : item.ToString())
                    .Where(value => !string.IsNullOrWhiteSpace(value))),
                ["pads"] = new JArray(cbPads.Items.Cast<object>()
                    .Select(item => item == null ? string.Empty : item.ToString())
                    .Where(value => !string.IsNullOrWhiteSpace(value)))
            };
        }

        private void RefreshModernLocation(ClientSession session)
        {
            using (ClientSessionContext.Push(session))
            {
                if (!Player.IsLoggedIn)
                    throw new ModernUiActionException("PLAYER_NOT_READY", "Log in before reading the current location.");

                string[] cells = World.Cells ?? new string[0];
                List<string> pads = World.Pads ?? new List<string>();
                cbCells.Items.Clear();
                cbCells.Items.AddRange(cells);
                cbCells.Text = Player.Cell ?? string.Empty;

                cbPads.Items.Clear();
                cbPads.Items.AddRange(pads.ToArray());
                foreach (string preset in presetPads)
                {
                    if (!pads.Any(value => string.Equals(value, preset, StringComparison.OrdinalIgnoreCase)))
                        cbPads.Items.Add(preset);
                }
                cbPads.Text = Player.Pad ?? string.Empty;
            }
            RequestModernUiStateRefresh();
        }

        private void MoveModernLocation(ClientSession session, string cell, string pad)
        {
            string requestedCell = (cell ?? string.Empty).Trim();
            string requestedPad = (pad ?? string.Empty).Trim();
            if (requestedCell.Length < 1 || requestedCell.Length > 80
                || requestedPad.Length < 1 || requestedPad.Length > 80)
            {
                throw new ModernUiActionException("INVALID_LOCATION", "Choose a valid cell and pad.");
            }

            using (ClientSessionContext.Push(session))
            {
                if (!Player.IsLoggedIn || !Player.IsAlive)
                    throw new ModernUiActionException("PLAYER_NOT_READY", "The character must be logged in and alive before moving.");

                string[] cells = World.Cells ?? new string[0];
                if (!cells.Any(value => string.Equals(value, requestedCell, StringComparison.OrdinalIgnoreCase)))
                    throw new ModernUiActionException("INVALID_CELL", "That cell is not available in the current map.");

                Player.MoveToCell(requestedCell, requestedPad);
                cbCells.Text = requestedCell;
                cbPads.Text = requestedPad;
            }
            RequestModernUiStateRefresh();
        }

        private static JObject ParseHomeBridgeResponse(string json)
        {
            try
            {
                return JObject.Parse(json ?? string.Empty);
            }
            catch (JsonException)
            {
                throw new ModernUiActionException(
                    "HOME_RESPONSE_INVALID",
                    "The Grimoire Home service returned an invalid response.");
            }
        }

        private JObject OpenModernHomeLink(string address)
        {
            string normalized = (address ?? string.Empty).Trim();
            bool allowed = string.Equals(
                    normalized,
                    "https://unknownsecurity.github.io/grimoire-exodus/#contact",
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    normalized,
                    "https://github.com/UnknownSecurity/grimoire-exodus/security/advisories/new",
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    normalized,
                    "https://github.com/UnknownSecurity/grimoire-mobile/releases/latest",
                    StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith(
                    "https://github.com/UnknownSecurity/grimoire-exodus/issues/",
                    StringComparison.OrdinalIgnoreCase);
            if (!allowed)
                throw new ModernUiActionException("LINK_NOT_ALLOWED", "That link is not available from Grimoire Home.");
            return ParseHomeBridgeResponse(homeBridge.OpenWebsite(normalized));
        }

        private void ShowModernSession(ClientSession session)
        {
            ShowClientSession(session);
            RequestModernUiStateRefresh();
        }

        private void PrepareModernWindowFrame()
        {
            if (modernWindowFramePrepared)
                return;

            legacyFormBorderStyle = FormBorderStyle;
            legacyMinimumSize = MinimumSize;
            legacyRootPadding = Padding;
            legacyGamePanelDock = GamePanel.Dock;
            legacyGamePanelBounds = GamePanel.Bounds;
            modernWindowFramePrepared = true;
            MinimumSize = GetModernMinimumSize();
            FormBorderStyle = FormBorderStyle.None;
            ApplyModernWindowShadow();
        }

        private void ApplyModernWindowFrameLayout()
        {
            if (!modernWindowFramePrepared)
                return;

            SuspendLayout();
            try
            {
                Padding = GetModernWindowPadding();
                MinimumSize = GetModernMinimumSize();
                GamePanel.Dock = DockStyle.Fill;
                ApplyModernWindowShadow();
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private void RestoreLegacyWindowFrame()
        {
            if (!modernWindowFramePrepared)
                return;

            SuspendLayout();
            try
            {
                Padding = legacyRootPadding;
                GamePanel.Dock = legacyGamePanelDock;
                GamePanel.Bounds = legacyGamePanelBounds;
                MinimumSize = legacyMinimumSize;
                modernWindowFramePrepared = false;
                FormBorderStyle = legacyFormBorderStyle;
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private void UpdateModernWindowFrame()
        {
            if (!modernShellActive)
                return;

            Padding desiredPadding = GetModernWindowPadding();
            if (Padding != desiredPadding)
                Padding = desiredPadding;
            RequestModernUiStateRefresh();
        }

        private Padding GetModernWindowPadding()
        {
            return WindowState == FormWindowState.Maximized
                ? Padding.Empty
                : new Padding(
                    ModernResizeBorderWidth,
                    0,
                    ModernResizeBorderWidth,
                    ModernResizeBorderWidth);
        }

        private Size GetModernMinimumSize()
        {
            double scale = Math.Max(1d, DeviceDpi / 96d);
            return new Size(
                (int)Math.Round(ModernMinimumWidthLogicalPixels * scale, MidpointRounding.AwayFromZero),
                (int)Math.Round(ModernMinimumHeightLogicalPixels * scale, MidpointRounding.AwayFromZero));
        }

        private void ApplyModernWindowShadow()
        {
            if (!modernWindowFramePrepared
                || !IsHandleCreated
                || Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return;
            }

            try
            {
                int policy = DwmNcRenderingEnabled;
                DwmSetWindowAttribute(
                    Handle,
                    DwmNcRenderingPolicy,
                    ref policy,
                    sizeof(int));

                NativeDwmMargins margins = new NativeDwmMargins
                {
                    Left = 1,
                    Right = 1,
                    Top = 1,
                    Bottom = 1
                };
                DwmExtendFrameIntoClientArea(Handle, ref margins);
            }
            catch (DllNotFoundException)
            {
                // Pre-DWM Windows keeps the custom outline without a native shadow.
            }
            catch (EntryPointNotFoundException)
            {
                // Older DWM versions may not expose frame extension.
            }
        }

        private void QueueModernWindowDrag()
        {
            if (!modernShellActive || IsDisposed || Disposing)
                return;

            BeginInvoke(new Action(delegate
            {
                if (!modernShellActive || IsDisposed || Disposing)
                    return;
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }));
        }

        private void ToggleModernWindowState()
        {
            if (!modernShellActive)
                return;

            if (WindowState == FormWindowState.Maximized)
            {
                WindowState = FormWindowState.Normal;
            }
            else
            {
                MaximizedBounds = GetModernMaximizedBounds();
                WindowState = FormWindowState.Maximized;
            }
        }

        private Rectangle GetModernMaximizedBounds()
        {
            Rectangle bounds = Screen.FromHandle(Handle).WorkingArea;
            int edgeCompensation = Math.Max(
                1,
                (int)Math.Round(
                    ModernMaximizeEdgeCompensationLogicalPixels * DeviceDpi / 96d,
                    MidpointRounding.AwayFromZero));
            bounds.Inflate(edgeCompensation, edgeCompensation);
            return bounds;
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WmNcCalcSize
                && modernWindowFramePrepared
                && FormBorderStyle == FormBorderStyle.None)
            {
                AdjustModernClientRect(ref message);
                return;
            }

            base.WndProc(ref message);
            if (message.Msg != WmNcHitTest
                || !modernShellActive
                || FormBorderStyle != FormBorderStyle.None
                || WindowState != FormWindowState.Normal)
            {
                return;
            }

            int packedPoint = message.LParam.ToInt32();
            Point cursor = PointToClient(new Point(
                (short)(packedPoint & 0xffff),
                (short)((packedPoint >> 16) & 0xffff)));
            bool left = cursor.X < ModernResizeBorderWidth;
            bool right = cursor.X >= ClientSize.Width - ModernResizeBorderWidth;
            bool top = cursor.Y < ModernResizeBorderWidth;
            bool bottom = cursor.Y >= ClientSize.Height - ModernResizeBorderWidth;

            if (left && top)
                message.Result = (IntPtr)HtTopLeft;
            else if (right && top)
                message.Result = (IntPtr)HtTopRight;
            else if (left && bottom)
                message.Result = (IntPtr)HtBottomLeft;
            else if (right && bottom)
                message.Result = (IntPtr)HtBottomRight;
            else if (left)
                message.Result = (IntPtr)HtLeft;
            else if (right)
                message.Result = (IntPtr)HtRight;
            else if (top)
                message.Result = (IntPtr)HtTop;
            else if (bottom)
                message.Result = (IntPtr)HtBottom;
        }

        private void AdjustModernClientRect(ref Message message)
        {
            if (message.WParam == IntPtr.Zero)
            {
                NativeRect proposedWindow = (NativeRect)Marshal.PtrToStructure(
                    message.LParam,
                    typeof(NativeRect));
                int proposedTop = proposedWindow.Top;
                base.WndProc(ref message);
                NativeRect client = (NativeRect)Marshal.PtrToStructure(
                    message.LParam,
                    typeof(NativeRect));
                client.Top = proposedTop;
                Marshal.StructureToPtr(client, message.LParam, false);
                return;
            }

            NativeNcCalcSizeParams proposed = (NativeNcCalcSizeParams)Marshal.PtrToStructure(
                message.LParam,
                typeof(NativeNcCalcSizeParams));
            int top = proposed.ProposedClient.Top;
            base.WndProc(ref message);
            NativeNcCalcSizeParams adjusted = (NativeNcCalcSizeParams)Marshal.PtrToStructure(
                message.LParam,
                typeof(NativeNcCalcSizeParams));
            adjusted.ProposedClient.Top = top;
            Marshal.StructureToPtr(adjusted, message.LParam, false);
        }

        private static JObject BuildModernOptionsState(ClientSession session)
        {
            Grimoire.Botting.ClientOptionState options = session?.OptionState;
            return new JObject
            {
                ["infiniteRange"] = options != null && options.InfiniteRange,
                ["provokeCell"] = options != null && options.ProvokeMonsters,
                ["provokeMap"] = options != null && options.ProvokeAllMonsters,
                ["targetMagnet"] = options != null && options.EnemyMagnet,
                ["lagKiller"] = options != null && options.LagKiller,
                ["hidePlayers"] = options != null && options.HidePlayers,
                ["skipCutscenes"] = options != null && options.SkipCutscenes,
                ["disableAnimations"] = options != null && options.DisableAnimations,
                ["untargetSelf"] = options != null && options.Untarget,
                ["autoSaveState"] = options != null && options.SaveStateEnabled,
                ["fastWalk"] = options != null && options.WalkSpeed > 8
            };
        }

        private void ToggleModernOption(ClientSession session, string option)
        {
            if (!ReferenceEquals(CaptureActiveSession(), session))
            {
                throw new ModernUiActionException(
                    "SESSION_NOT_ACTIVE",
                    "Select that AQW client before changing its options.");
            }

            ToolStripMenuItem item;
            switch ((option ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "infinite-range":
                    item = infRangeToolStripMenuItem;
                    break;
                case "provoke-cell":
                    item = provokeToolStripMenuItem1;
                    break;
                case "provoke-map":
                    item = provokeAllMonsterInMapToolStripMenuItem;
                    break;
                case "target-magnet":
                    item = enemyMagnetToolStripMenuItem;
                    break;
                case "lag-killer":
                    item = lagKillerToolStripMenuItem;
                    break;
                case "hide-players":
                    item = hidePlayersToolStripMenuItem;
                    break;
                case "skip-cutscenes":
                    item = skipCutscenesToolStripMenuItem;
                    break;
                case "disable-animations":
                    item = disableAnimationsToolStripMenuItem;
                    break;
                case "untarget-self":
                    item = untargetSelfToolStripMenuItem;
                    break;
                case "auto-save-state":
                    item = autosaveStateToolStripMenuItem;
                    break;
                case "fast-walk":
                    item = walkSpeedToolStripMenuItem;
                    break;
                default:
                    throw new ModernUiActionException(
                        "OPTION_NOT_ALLOWED",
                        "That client option is not available in the modern shell.");
            }

            item.PerformClick();
            RequestModernUiStateRefresh();
        }

        private void OpenModernTool(string tool)
        {
            switch ((tool ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "bot-manager":
                    managerToolStripMenuItem_Click(this, EventArgs.Empty);
                    break;
                case "settings":
                    btnSettings_Click(this, EventArgs.Empty);
                    break;
                case "quick-load":
                    quickBotsButton_Click(this, EventArgs.Empty);
                    break;
                case "load-bot":
                    loadBotToolStripMenuItem_Click(this, EventArgs.Empty);
                    break;
                case "travel":
                    fastTravelsToolStripMenuItem_Click(this, EventArgs.Empty);
                    break;
                case "loaders":
                    loadersgrabbersToolStripMenuItem_Click(this, EventArgs.Empty);
                    break;
                case "hotkeys":
                    hotkeysToolStripMenuItem_Click(this, EventArgs.Empty);
                    break;
                case "packet-logger":
                    snifferToolStripMenuItem_Click(this, EventArgs.Empty);
                    break;
                case "packet-spammer":
                    spammerToolStripMenuItem_Click(this, EventArgs.Empty);
                    break;
                case "packet-tamperer":
                    tampererToolStripMenuItem_Click(this, EventArgs.Empty);
                    break;
                case "plugin-manager":
                    pluginManagerToolStripMenuItem_Click(this, EventArgs.Empty);
                    break;
                case "bank":
                    bankToolStripMenuItem1_Click(this, EventArgs.Empty);
                    break;
                case "bank-items":
                    bankToolStripMenuItem_Click(this, EventArgs.Empty);
                    break;
                case "bank-reload":
                    reloadToolStripMenuItem_Click(this, EventArgs.Empty);
                    break;
                case "cosmetics":
                    cosmeticsToolStripMenuItem_Click(this, EventArgs.Empty);
                    break;
                case "eye-dropper":
                    eyeDropperToolStripMenuItem_Click_1(this, EventArgs.Empty);
                    break;
                case "logs":
                    logsToolStripMenuItem1_Click(this, EventArgs.Empty);
                    break;
                case "notepad":
                    notepadToolStripMenuItem1_Click(this, EventArgs.Empty);
                    break;
                case "ping-monitor":
                    pingMonitorToolStripMenuItem_Click(this, EventArgs.Empty);
                    break;
                case "immediate-login":
                    loginToolStripMenuItem_Click(this, EventArgs.Empty);
                    break;
                case "ultra-kit":
                    UltraKitToolStripMenuItem_Click(this, EventArgs.Empty);
                    break;
                default:
                    throw new ModernUiActionException("TOOL_NOT_ALLOWED", "That tool is not available in the modern shell yet.");
            }
        }

        private string FormatModernRuntime()
        {
            TimeSpan elapsed = DateTime.UtcNow - metricsStartedUtc;
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0:00}:{1:00}:{2:00}",
                Math.Floor(elapsed.TotalHours),
                elapsed.Minutes,
                elapsed.Seconds);
        }

        private void DisposeModernShell()
        {
            ModernShellHost host = modernShellHost;
            modernShellHost = null;
            modernShellActive = false;
            if (host == null)
                return;
            host.GameViewportChanged -= ModernShellHost_GameViewportChanged;
            host.ShellFailed -= ModernShellHost_ShellFailed;
            host.Dispose();
        }
    }
}
