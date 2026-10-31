using DarkUI.Controls;
using DarkUI.Forms;
using Grimoire.Clients;
using Grimoire.Tools;
using Grimoire.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Grimoire.UI
{
    internal sealed class SettingsForm : GrimoireForm
    {
        private static readonly Color BackgroundColor = Color.FromArgb(28, 32, 40);
        private static readonly Color CardColor = Color.FromArgb(36, 40, 50);
        private static readonly Color SecondaryTextColor = Color.FromArgb(170, 174, 188);

        private readonly Root root;
        private readonly DarkCheckBox chkSkipHome = new DarkCheckBox();
        private readonly DarkCheckBox chkHideDeviceStats = new DarkCheckBox();
        private readonly DarkCheckBox chkConfirmOnClose = new DarkCheckBox();
        private readonly DarkCheckBox chkMinimizeToTray = new DarkCheckBox();
        private readonly DarkCheckBox chkEnableMultiClient = new DarkCheckBox();
        private readonly DarkCheckBox chkUnlimitedClients = new DarkCheckBox();
        private readonly DarkCheckBox chkPrereleaseUpdates = new DarkCheckBox();
        private readonly DarkComboBox cmbGameEngine = new DarkComboBox();
        private readonly DarkComboBox cmbRenderingProfile = new DarkComboBox();
        private readonly DarkComboBox cmbRenderQuality = new DarkComboBox();
        private readonly DarkCheckBox chkUseActiveClientFrameRate = new DarkCheckBox();
        private readonly DarkNumericUpDown numActiveClientFrameRate = new DarkNumericUpDown();
        private readonly DarkCheckBox chkThrottleInactiveClients = new DarkCheckBox();
        private readonly DarkNumericUpDown numInactiveClientFrameRate = new DarkNumericUpDown();
        private readonly DarkTextBox txtSupportReport = new DarkTextBox();
        private readonly DarkButton btnRefreshLogs = new DarkButton();
        private readonly DarkButton btnCopySupportReport = new DarkButton();
        private readonly DarkButton btnOpenDiagnosticsFolder = new DarkButton();
        private readonly DarkComboBox cmbVersions = new DarkComboBox();
        private readonly DarkButton btnRefreshVersions = new DarkButton();
        private readonly DarkButton btnApplyVersion = new DarkButton();
        private readonly Label lblCurrentVersion = new Label();
        private readonly Label lblVersionStatus = new Label();
        private readonly Dictionary<int, DarkTextBox> bindingEditors = new Dictionary<int, DarkTextBox>();
        private readonly Dictionary<int, HotkeyGesture?> bindingValues = new Dictionary<int, HotkeyGesture?>();
        private readonly DarkButton btnSave = new DarkButton();
        private readonly DarkButton btnClose = new DarkButton();
        private TabPage versionTab;
        private string supportReport;
        private bool supportReportLoaded;
        private bool versionsLoaded;
        private bool versionsLoading;
        private readonly CancellationTokenSource versionCancellation = new CancellationTokenSource();

        public SettingsForm(Root owner)
        {
            root = owner ?? throw new ArgumentNullException(nameof(owner));
            InitializeSettingsForm();
            LoadValues();
        }

        private void InitializeSettingsForm()
        {
            Text = "Grimoire Exodus Settings";
            Icon = (Icon)global::Properties.Resources.GrimoireIcon.Clone();
            BackColor = BackgroundColor;
            ForeColor = Color.Gainsboro;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            MinimumSize = new Size(620, 470);
            ClientSize = new Size(660, 500);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;

            TableLayoutPanel layout = new TableLayoutPanel
            {
                BackColor = BackgroundColor,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                RowCount = 2
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

            NonClosableVSTabControl tabs = new NonClosableVSTabControl
            {
                Dock = DockStyle.Fill,
                HotTrack = true,
                ItemSize = new Size(100, 24),
                SizeMode = TabSizeMode.Fixed
            };
            tabs.TabPages.Add(CreateGeneralTab());
            tabs.TabPages.Add(CreatePerformanceTab());
            tabs.TabPages.Add(CreateKeyBindingsTab());
            tabs.TabPages.Add(CreateAdvancedTab());
            versionTab = CreateVersionTab();
            tabs.TabPages.Add(versionTab);
            tabs.TabPages.Add(CreateLogsTab());
            tabs.SelectedIndexChanged += SettingsTabs_SelectedIndexChanged;

            FlowLayoutPanel footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 9, 0, 0),
                WrapContents = false
            };
            ConfigureButton(btnSave, "Save settings", 118);
            ConfigureButton(btnClose, "Close", 90);
            btnSave.Click += Save_Click;
            btnClose.Click += delegate { Close(); };
            footer.Controls.Add(btnSave);
            footer.Controls.Add(btnClose);

            layout.Controls.Add(tabs, 0, 0);
            layout.Controls.Add(footer, 0, 1);
            Controls.Add(layout);
            AcceptButton = btnSave;
            CancelButton = btnClose;
            Shown += SettingsForm_Shown;
            FormClosed += SettingsForm_FormClosed;
        }

        private TabPage CreateGeneralTab()
        {
            TabPage page = CreateTabPage("General");
            FlowLayoutPanel content = CreateVerticalContent();
            content.Controls.Add(CreateHeading("Startup"));
            ConfigurePreferenceCheckBox(
                chkSkipHome,
                "Open AQW login on startup",
                "Skip the Home launcher and show the AQW login screen as soon as the client is ready. Takes effect next launch.");
            content.Controls.Add(chkSkipHome);

            content.Controls.Add(CreateHeading("Status panel"));
            ConfigurePreferenceCheckBox(
                chkHideDeviceStats,
                "Hide device stats",
                "Hide CPU, uptime, and network activity from the lower panel.");
            content.Controls.Add(chkHideDeviceStats);

            content.Controls.Add(CreateHeading("Window behavior"));
            ConfigurePreferenceCheckBox(
                chkConfirmOnClose,
                "Confirm before closing",
                "Ask for confirmation before ending Grimoire and the current game or bot session.");
            ConfigurePreferenceCheckBox(
                chkMinimizeToTray,
                "Send Grimoire to the tray when minimized",
                "Keep the client running in the notification area instead of the taskbar.");
            content.Controls.Add(chkConfirmOnClose);
            content.Controls.Add(chkMinimizeToTray);

            content.Controls.Add(CreateHeading("Experimental"));
            ConfigurePreferenceCheckBox(
                chkEnableMultiClient,
                "Enable multi-client mode (experimental)",
                "Shows the client switcher and allows up to six independent AQW clients. While enabled, later launches join the first window; while disabled, every launch opens separately. Restart after changing this mode.");
            chkEnableMultiClient.CheckedChanged += delegate
            {
                chkUnlimitedClients.Enabled = chkEnableMultiClient.Checked;
            };
            content.Controls.Add(chkEnableMultiClient);
            page.Controls.Add(CreateThemedScrollHost(content));
            return page;
        }

        private TabPage CreatePerformanceTab()
        {
            TabPage page = CreateTabPage("Performance");
            FlowLayoutPanel content = CreateVerticalContent();

            content.Controls.Add(CreateHeading("Game engine"));
            ConfigureDropDown(cmbGameEngine, "Flash.ocx (recommended)", "Ruffle (experimental)");
            content.Controls.Add(CreatePreferenceRow(
                "AQW engine",
                "Flash.ocx is the compatibility default. Ruffle uses WebView2 and an isolated local bridge; changes apply only to newly created clients.",
                cmbGameEngine));

            content.Controls.Add(CreateHeading("Rendering"));
            Label rendererNote = CreateBodyLabel(
                "These profiles change how Flash presents the game. Direct and GPU are experimental requests; game logic, bots, and networking still run on the CPU. Changes apply to newly created clients.");
            rendererNote.Height = 48;
            rendererNote.Margin = new Padding(0, 2, 0, 6);
            rendererNote.Width = 570;
            content.Controls.Add(rendererNote);

            ConfigureDropDown(
                cmbRenderingProfile,
                "Automatic (recommended)",
                "Direct presentation (experimental)",
                "GPU (single-client experimental)");
            content.Controls.Add(CreatePreferenceRow(
                "Rendering profile",
                "Automatic is recommended for multiple clients. Legacy GPU presentation can stall on some drivers and is best limited to one client.",
                cmbRenderingProfile));

            ConfigureDropDown(cmbRenderQuality, "Automatic", "High", "Medium", "Low");
            content.Controls.Add(CreatePreferenceRow(
                "Render quality",
                "Lower quality can reduce vector-rendering work on slower systems. Automatic leaves the Flash default unchanged.",
                cmbRenderQuality));

            content.Controls.Add(CreateHeading("Frame rate"));
            ConfigurePreferenceCheckBox(
                chkUseActiveClientFrameRate,
                "Use a custom FPS for the active client",
                "Applies the target below only while a client is selected. A GBot Set FPS command remains authoritative for that client.");
            chkUseActiveClientFrameRate.CheckedChanged += delegate
            {
                numActiveClientFrameRate.Enabled = chkUseActiveClientFrameRate.Checked;
            };
            content.Controls.Add(chkUseActiveClientFrameRate);

            ConfigureFrameRateInput(numActiveClientFrameRate, 1, 240);
            content.Controls.Add(CreatePreferenceRow(
                "Selected client FPS",
                "Higher values can look smoother but increase CPU use. The safe range is 1–240 FPS.",
                numActiveClientFrameRate));

            ConfigurePreferenceCheckBox(
                chkThrottleInactiveClients,
                "Throttle inactive clients",
                "Reduces only background clients that have no running bot. Running bots are never throttled.");
            chkThrottleInactiveClients.CheckedChanged += delegate
            {
                numInactiveClientFrameRate.Enabled = chkThrottleInactiveClients.Checked;
            };
            content.Controls.Add(chkThrottleInactiveClients);

            ConfigureFrameRateInput(numInactiveClientFrameRate, 1, 60);
            content.Controls.Add(CreatePreferenceRow(
                "Inactive client FPS",
                "A conservative 15 FPS target reduces background rendering while keeping the session connected.",
                numInactiveClientFrameRate));

            page.Controls.Add(CreateThemedScrollHost(content));
            return page;
        }

        private TabPage CreateKeyBindingsTab()
        {
            TabPage page = CreateTabPage("Key Bindings");
            TableLayoutPanel content = new TableLayoutPanel
            {
                AutoScroll = true,
                BackColor = BackgroundColor,
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Padding = new Padding(18),
                RowCount = Hotkeys.SettingsBindingDefinitions.Length + 3
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F));

            Label introduction = CreateBodyLabel(
                "Click a field, then press one key or a combination such as Ctrl+Shift+S. Press Backspace or Delete to clear it. Shortcuts work while Grimoire is focused.");
            introduction.Dock = DockStyle.Fill;
            content.Controls.Add(introduction, 0, 0);
            content.SetColumnSpan(introduction, 2);

            int row = 1;
            foreach (HotkeyBindingDefinition definition in Hotkeys.SettingsBindingDefinitions)
            {
                content.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
                Label label = CreateBodyLabel(definition.DisplayName);
                label.Dock = DockStyle.Fill;
                label.TextAlign = ContentAlignment.MiddleLeft;

                DarkTextBox editor = new DarkTextBox
                {
                    AccessibleName = definition.DisplayName + " shortcut",
                    AccessibleDescription = "Focus this field and press the shortcut keys together. Press Backspace or Delete to clear it.",
                    BackColor = CardColor,
                    Dock = DockStyle.Fill,
                    ForeColor = Color.Gainsboro,
                    Margin = new Padding(8, 10, 8, 10)
                };
                editor.ReadOnly = true;
                editor.ShortcutsEnabled = false;
                editor.Tag = definition.ActionIndex;
                editor.TextAlign = HorizontalAlignment.Center;
                editor.Enter += ShortcutEditor_Enter;
                editor.Leave += ShortcutEditor_Leave;
                editor.PreviewKeyDown += ShortcutEditor_PreviewKeyDown;
                editor.KeyDown += ShortcutEditor_KeyDown;
                bindingEditors[definition.ActionIndex] = editor;
                bindingValues[definition.ActionIndex] = null;

                content.Controls.Add(label, 0, row);
                content.Controls.Add(editor, 1, row);
                row++;
            }

            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            DarkButton defaults = new DarkButton();
            ConfigureButton(defaults, "Restore defaults", 132);
            defaults.Margin = new Padding(8, 8, 8, 8);
            defaults.Click += delegate { ApplyDefaultBindings(); };
            content.Controls.Add(defaults, 1, row);

            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            page.Controls.Add(CreateThemedScrollHost(content));
            return page;
        }

        private TabPage CreateAdvancedTab()
        {
            TabPage page = CreateTabPage("Advanced");
            TableLayoutPanel content = new TableLayoutPanel
            {
                AutoScroll = true,
                BackColor = BackgroundColor,
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Padding = new Padding(18),
                RowCount = 6
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            ConfigurePreferenceCheckBox(
                chkPrereleaseUpdates,
                "Use testing / experimental updates",
                "Includes GitHub prereleases in update checks. These builds may be less stable than the latest public release.");
            chkPrereleaseUpdates.Dock = DockStyle.Fill;
            chkPrereleaseUpdates.Margin = new Padding(0, 4, 0, 10);
            content.Controls.Add(chkPrereleaseUpdates, 0, 0);
            content.SetColumnSpan(chkPrereleaseUpdates, 2);

            ConfigurePreferenceCheckBox(
                chkUnlimitedClients,
                "Remove the six-client safety limit",
                "Allows more than six live clients. Each extra Flash client increases CPU, memory, and handle use in the 32-bit process.");
            chkUnlimitedClients.Dock = DockStyle.Fill;
            chkUnlimitedClients.Margin = new Padding(0, 4, 0, 10);
            content.Controls.Add(chkUnlimitedClients, 0, 1);
            content.SetColumnSpan(chkUnlimitedClients, 2);

            AddAdvancedAction(
                content,
                2,
                "Delete character information",
                "Permanently removes every saved account and encrypted password from the Account Locker.",
                "Delete credentials",
                DeleteCredentials_Click);
            AddAdvancedAction(
                content,
                3,
                "Full reset",
                "Removes Grimoire settings, saved characters, logs, recovery data, cache, and downloaded updates. Bot folders are preserved.",
                "Full reset",
                FullReset_Click);
            AddAdvancedAction(
                content,
                4,
                "Check for an update",
                "Checks the selected stable or experimental GitHub release channel and shows the update icon when a newer version is available.",
                "Check now",
                CheckForUpdate_Click);

            page.Controls.Add(CreateThemedScrollHost(content));
            return page;
        }

        private TabPage CreateLogsTab()
        {
            TabPage page = CreateTabPage("Logs");
            TableLayoutPanel content = new TableLayoutPanel
            {
                BackColor = BackgroundColor,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 12, 18, 12),
                RowCount = 3
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

            Label note = CreateBodyLabel(
                "Review recent startup and crash diagnostics here. The copied report removes common private fields and paths automatically; review free-form errors before sharing.");
            note.AutoEllipsis = false;
            note.Dock = DockStyle.Fill;
            note.Padding = new Padding(0, 2, 0, 7);

            txtSupportReport.AccessibleName = "Privacy-filtered support report";
            txtSupportReport.BackColor = CardColor;
            txtSupportReport.BorderStyle = BorderStyle.FixedSingle;
            txtSupportReport.Dock = DockStyle.Fill;
            txtSupportReport.Font = new Font("Consolas", 8.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtSupportReport.ForeColor = Color.Gainsboro;
            txtSupportReport.Multiline = true;
            txtSupportReport.ReadOnly = true;
            txtSupportReport.ScrollBars = ScrollBars.None;
            txtSupportReport.Text = "Collecting diagnostics...";
            txtSupportReport.WordWrap = true;

            FlowLayoutPanel actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 8, 0, 0),
                WrapContents = false
            };
            ConfigureButton(btnRefreshLogs, "Refresh", 88);
            ConfigureButton(btnCopySupportReport, "Copy device info + logs", 174);
            ConfigureButton(btnOpenDiagnosticsFolder, "Open diagnostics folder", 160);
            btnRefreshLogs.Click += RefreshLogs_Click;
            btnCopySupportReport.Click += CopySupportReport_Click;
            btnOpenDiagnosticsFolder.Click += OpenDiagnosticsFolder_Click;
            actions.Controls.Add(btnRefreshLogs);
            actions.Controls.Add(btnCopySupportReport);
            actions.Controls.Add(btnOpenDiagnosticsFolder);

            content.Controls.Add(note, 0, 0);
            content.Controls.Add(CreateThemedLogHost(txtSupportReport), 0, 1);
            content.Controls.Add(actions, 0, 2);
            page.Controls.Add(content);
            return page;
        }

        private TabPage CreateVersionTab()
        {
            TabPage page = CreateTabPage("Version");
            TableLayoutPanel content = new TableLayoutPanel
            {
                BackColor = BackgroundColor,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 14, 18, 14),
                RowCount = 6
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

            lblCurrentVersion.AccessibleName = "Installed Grimoire Exodus version";
            lblCurrentVersion.Dock = DockStyle.Fill;
            lblCurrentVersion.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblCurrentVersion.ForeColor = Color.MediumPurple;
            lblCurrentVersion.Text = "Installed version: " + UpdateService.CurrentVersion.ToString(3);
            lblCurrentVersion.TextAlign = ContentAlignment.MiddleLeft;

            Label introduction = CreateBodyLabel(
                "Choose a verified official GitHub release. Newer versions update Grimoire; older versions perform a downgrade while preserving writable settings, saved accounts, and GBot folders.");
            introduction.AutoEllipsis = false;
            introduction.Dock = DockStyle.Fill;

            TableLayoutPanel selector = new TableLayoutPanel
            {
                BackColor = CardColor,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 8, 12, 8),
                RowCount = 2
            };
            selector.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            selector.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            selector.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            Label selectorLabel = CreateBodyLabel("Official release");
            selectorLabel.Dock = DockStyle.Fill;
            selectorLabel.ForeColor = Color.Gainsboro;
            cmbVersions.AccessibleName = "Grimoire Exodus version";
            cmbVersions.Dock = DockStyle.Fill;
            cmbVersions.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbVersions.SelectedIndexChanged += VersionSelectionChanged;
            selector.Controls.Add(selectorLabel, 0, 0);
            selector.Controls.Add(cmbVersions, 0, 1);

            lblVersionStatus.AccessibleName = "Selected version details";
            lblVersionStatus.BackColor = CardColor;
            lblVersionStatus.Dock = DockStyle.Fill;
            lblVersionStatus.ForeColor = SecondaryTextColor;
            lblVersionStatus.Padding = new Padding(12, 8, 12, 8);
            lblVersionStatus.Text = "Loading official releases...";
            lblVersionStatus.TextAlign = ContentAlignment.MiddleLeft;

            Label safety = CreateBodyLabel(
                "Only release assets that pass trusted-host, file-name, payload, size, and SHA-256 checks can be installed. Grimoire always asks before closing a running client or bot.");
            safety.AutoEllipsis = false;
            safety.Dock = DockStyle.Fill;
            safety.Padding = new Padding(0, 8, 0, 0);

            FlowLayoutPanel actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 8, 0, 0),
                WrapContents = false
            };
            ConfigureButton(btnRefreshVersions, "Refresh releases", 120);
            ConfigureButton(btnApplyVersion, "Use selected version", 150);
            btnRefreshVersions.Click += RefreshVersions_Click;
            btnApplyVersion.Click += ApplyVersion_Click;
            btnApplyVersion.Enabled = false;
            actions.Controls.Add(btnApplyVersion);
            actions.Controls.Add(btnRefreshVersions);

            content.Controls.Add(lblCurrentVersion, 0, 0);
            content.Controls.Add(introduction, 0, 1);
            content.Controls.Add(selector, 0, 2);
            content.Controls.Add(lblVersionStatus, 0, 3);
            content.Controls.Add(safety, 0, 4);
            content.Controls.Add(actions, 0, 5);
            page.Controls.Add(content);
            return page;
        }

        private async void SettingsForm_Shown(object sender, EventArgs e)
        {
            if (!supportReportLoaded)
                await RefreshSupportReportAsync();
        }

        private async void SettingsTabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            TabControl tabs = sender as TabControl;
            if (tabs != null && ReferenceEquals(tabs.SelectedTab, versionTab) && !versionsLoaded)
                await RefreshVersionsAsync();
        }

        private void SettingsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            KeyboardHook.Instance.SuppressHotkeyEvents = false;
            versionCancellation.Cancel();
            versionCancellation.Dispose();
        }

        private async void RefreshVersions_Click(object sender, EventArgs e)
        {
            await RefreshVersionsAsync();
        }

        private async Task RefreshVersionsAsync()
        {
            if (versionsLoading || IsDisposed)
                return;

            versionsLoading = true;
            btnRefreshVersions.Enabled = false;
            btnApplyVersion.Enabled = false;
            lblVersionStatus.ForeColor = SecondaryTextColor;
            lblVersionStatus.Text = "Loading verified official releases...";
            try
            {
                IList<UpdateRelease> releases = await UpdateService.GetAvailableReleasesAsync(
                    chkPrereleaseUpdates.Checked,
                    versionCancellation.Token);
                if (IsDisposed || Disposing)
                    return;

                cmbVersions.BeginUpdate();
                try
                {
                    cmbVersions.Items.Clear();
                    foreach (UpdateRelease release in releases)
                        cmbVersions.Items.Add(new VersionReleaseItem(release, UpdateService.CurrentVersion));
                }
                finally
                {
                    cmbVersions.EndUpdate();
                }

                versionsLoaded = true;
                if (cmbVersions.Items.Count == 0)
                {
                    lblVersionStatus.Text = "No verified release packages are available on the selected channel.";
                    return;
                }

                int currentIndex = -1;
                for (int index = 0; index < cmbVersions.Items.Count; index++)
                {
                    VersionReleaseItem item = (VersionReleaseItem)cmbVersions.Items[index];
                    if (item.Release.Version == UpdateService.CurrentVersion)
                    {
                        currentIndex = index;
                        break;
                    }
                }
                cmbVersions.SelectedIndex = currentIndex >= 0 ? currentIndex : 0;
                UpdateVersionSelectionStatus();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                versionsLoaded = false;
                if (!IsDisposed)
                {
                    lblVersionStatus.ForeColor = Color.LightSalmon;
                    lblVersionStatus.Text = "Official releases could not be loaded. " + exception.Message;
                }
            }
            finally
            {
                versionsLoading = false;
                if (!IsDisposed)
                    btnRefreshVersions.Enabled = true;
            }
        }

        private void VersionSelectionChanged(object sender, EventArgs e)
        {
            UpdateVersionSelectionStatus();
        }

        private void UpdateVersionSelectionStatus()
        {
            VersionReleaseItem item = cmbVersions.SelectedItem as VersionReleaseItem;
            if (item == null)
            {
                btnApplyVersion.Enabled = false;
                return;
            }

            int comparison = item.Release.Version.CompareTo(UpdateService.CurrentVersion);
            string relation = comparison < 0
                ? "Downgrade"
                : comparison > 0
                    ? "Update"
                    : "Installed";
            string channel = item.Release.IsPrerelease ? "Experimental prerelease" : "Stable release";
            string packages = item.Release.PortableAsset != null && item.Release.InstallerAsset != null
                ? "portable and installer packages"
                : item.Release.PortableAsset != null
                    ? "portable package"
                    : "installer package";
            bool downgradeSupported = comparison >= 0 || item.Release.PortableAsset != null;
            lblVersionStatus.ForeColor = comparison < 0 ? Color.Khaki : SecondaryTextColor;
            lblVersionStatus.Text = relation + " · " + channel + " · verified " + packages + "."
                + (comparison < 0
                    ? downgradeSupported
                        ? " Newer application features will be unavailable until you update again."
                        : " This release cannot be downgraded safely because it has no portable package."
                    : string.Empty);
            btnApplyVersion.Enabled = comparison != 0 && downgradeSupported && !versionsLoading;
        }

        private async void ApplyVersion_Click(object sender, EventArgs e)
        {
            VersionReleaseItem item = cmbVersions.SelectedItem as VersionReleaseItem;
            if (item == null || item.Release.Version == UpdateService.CurrentVersion)
                return;

            btnApplyVersion.Enabled = false;
            btnRefreshVersions.Enabled = false;
            cmbVersions.Enabled = false;
            lblVersionStatus.ForeColor = SecondaryTextColor;
            lblVersionStatus.Text = "Preparing Grimoire Exodus " + item.Release.TagName + "...";
            try
            {
                Progress<int> progress = new Progress<int>(percent =>
                {
                    if (!IsDisposed)
                        lblVersionStatus.Text = "Downloading and verifying " + item.Release.TagName + " · " + percent + "%";
                });
                bool applied = await root.InstallApplicationReleaseAsync(item.Release, progress);
                if (!applied && !IsDisposed)
                    UpdateVersionSelectionStatus();
            }
            finally
            {
                if (!IsDisposed)
                {
                    btnRefreshVersions.Enabled = true;
                    cmbVersions.Enabled = true;
                    UpdateVersionSelectionStatus();
                }
            }
        }

        private async void RefreshLogs_Click(object sender, EventArgs e)
        {
            await RefreshSupportReportAsync();
        }

        private async void CopySupportReport_Click(object sender, EventArgs e)
        {
            if (!supportReportLoaded)
                await RefreshSupportReportAsync();
            if (string.IsNullOrWhiteSpace(supportReport) || IsDisposed)
                return;

            try
            {
                Clipboard.SetText(supportReport);
                DarkMessageBox.Show(
                    this,
                    "The privacy-filtered device information and logs were copied.",
                    "Support report copied",
                    MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                DarkMessageBox.Show(this, exception.Message, "Unable to copy support report", MessageBoxIcon.Error);
            }
        }

        private void OpenDiagnosticsFolder_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(AppPaths.DataDirectory) { UseShellExecute = true });
            }
            catch (Exception exception)
            {
                DarkMessageBox.Show(this, exception.Message, "Unable to open diagnostics folder", MessageBoxIcon.Error);
            }
        }

        private async Task RefreshSupportReportAsync()
        {
            btnRefreshLogs.Enabled = false;
            btnCopySupportReport.Enabled = false;
            txtSupportReport.Text = "Collecting diagnostics...";
            try
            {
                string debugLog = LogForm.Instance.GetDebugLogSnapshot(64000);
                string report = await Task.Run(() => SupportDiagnostics.BuildReport(debugLog));
                if (IsDisposed)
                    return;

                supportReport = report;
                supportReportLoaded = true;
                txtSupportReport.Text = report;
                txtSupportReport.SelectionStart = 0;
                txtSupportReport.SelectionLength = 0;
                txtSupportReport.ScrollToCaret();
            }
            catch (Exception exception)
            {
                supportReport = null;
                supportReportLoaded = false;
                if (!IsDisposed)
                {
                    txtSupportReport.Text = "The diagnostics report could not be created.\r\n\r\n" + exception.Message;
                }
            }
            finally
            {
                if (!IsDisposed)
                {
                    btnRefreshLogs.Enabled = true;
                    btnCopySupportReport.Enabled = supportReportLoaded;
                }
            }
        }

        private void AddAdvancedAction(
            TableLayoutPanel layout,
            int row,
            string title,
            string description,
            string buttonText,
            EventHandler click)
        {
            Panel copy = new Panel { BackColor = BackgroundColor, Dock = DockStyle.Fill };
            Label heading = new Label
            {
                AutoEllipsis = true,
                Dock = DockStyle.Top,
                Font = new Font(Font, FontStyle.Bold),
                ForeColor = Color.Gainsboro,
                Height = 26,
                Text = title,
                TextAlign = ContentAlignment.BottomLeft
            };
            Label body = CreateBodyLabel(description);
            body.Dock = DockStyle.Fill;
            copy.Controls.Add(body);
            copy.Controls.Add(heading);

            DarkButton button = new DarkButton();
            ConfigureButton(button, buttonText, 128);
            button.Anchor = AnchorStyles.Right;
            button.Margin = new Padding(8, 27, 8, 27);
            button.Click += click;

            layout.Controls.Add(copy, 0, row);
            layout.Controls.Add(button, 1, row);
        }

        private void LoadValues()
        {
            AppPreferenceSnapshot preferences = AppPreferences.Load();
            chkSkipHome.Checked = preferences.SkipHomePage;
            chkHideDeviceStats.Checked = preferences.HideDeviceStats;
            chkConfirmOnClose.Checked = preferences.ConfirmOnClose;
            chkMinimizeToTray.Checked = preferences.MinimizeToTray;
            chkEnableMultiClient.Checked = preferences.EnableMultiClient;
            chkUnlimitedClients.Checked = preferences.AllowUnlimitedClients;
            chkUnlimitedClients.Enabled = preferences.EnableMultiClient;
            chkPrereleaseUpdates.Checked = preferences.UsePrereleaseUpdates;
            cmbGameEngine.SelectedIndex = (int)preferences.GameEngine;
            cmbRenderingProfile.SelectedIndex = (int)preferences.RenderingProfile;
            cmbRenderQuality.SelectedIndex = (int)preferences.RenderQuality;
            chkUseActiveClientFrameRate.Checked = preferences.UseActiveClientFrameRate;
            numActiveClientFrameRate.Value = preferences.ActiveClientFramesPerSecond;
            numActiveClientFrameRate.Enabled = preferences.UseActiveClientFrameRate;
            chkThrottleInactiveClients.Checked = preferences.ThrottleInactiveClients;
            numInactiveClientFrameRate.Value = preferences.InactiveClientFramesPerSecond;
            numInactiveClientFrameRate.Enabled = preferences.ThrottleInactiveClients;

            foreach (HotkeyBindingDefinition definition in Hotkeys.SettingsBindingDefinitions)
            {
                SetBinding(
                    definition.ActionIndex,
                    Hotkeys.GetBindingGesture(definition.ActionIndex));
            }
        }

        private void Save_Click(object sender, EventArgs e)
        {
            try
            {
                AppPreferenceSnapshot previousPreferences = AppPreferences.Load();
                if (!chkEnableMultiClient.Checked
                    && previousPreferences.EnableMultiClient
                    && root.OpenClientCount > 1)
                {
                    DarkMessageBox.Show(
                        this,
                        "Close the additional AQW clients before turning off multi-client mode. Grimoire will not close a live client automatically.",
                        "Multi-client is still in use",
                        MessageBoxIcon.Information);
                    return;
                }

                if (chkUnlimitedClients.Checked && !previousPreferences.AllowUnlimitedClients)
                {
                    DialogResult confirmation = DarkMessageBox.Show(
                        this,
                        "Remove the six-client safety limit?\r\n\r\nEvery AQW client runs another Flash control inside the same 32-bit process. Creating too many clients can cause severe slowdown or make Grimoire run out of memory and close.\r\n\r\nOnly enable this if you understand the resource risk.",
                        "Enable unlimited clients?",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2);
                    if (confirmation != DialogResult.Yes)
                        return;
                }

                if (chkPrereleaseUpdates.Checked && !previousPreferences.UsePrereleaseUpdates)
                {
                    DialogResult confirmation = DarkMessageBox.Show(
                        this,
                        "Enable testing / experimental updates?\r\n\r\nGrimoire will include GitHub prereleases when checking for updates. Prereleases may contain unfinished behavior or regressions. You can return to stable updates at any time.",
                        "Enable experimental updates?",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2);
                    if (confirmation != DialogResult.Yes)
                        return;
                }

                GameEngineKind selectedEngine = (GameEngineKind)Math.Max(0, cmbGameEngine.SelectedIndex);
                if (selectedEngine == GameEngineKind.Ruffle
                    && previousPreferences.GameEngine != GameEngineKind.Ruffle)
                {
                    DialogResult confirmation = DarkMessageBox.Show(
                        this,
                        "Enable the experimental Ruffle engine?\r\n\r\nRuffle can improve rendering performance, but AQW and bot compatibility may differ from Flash.ocx. The change applies only to clients created after saving; no running client or bot will be restarted. If Ruffle is unavailable, Grimoire safely falls back to Flash.ocx.",
                        "Enable experimental Ruffle?",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2);
                    if (confirmation != DialogResult.Yes)
                        return;
                }

                Hotkeys.SaveSettingsBindings(
                    new Dictionary<int, HotkeyGesture?>(bindingValues));
                AppPreferenceSnapshot preferences = new AppPreferenceSnapshot
                {
                    SkipHomePage = chkSkipHome.Checked,
                    HideDeviceStats = chkHideDeviceStats.Checked,
                    ConfirmOnClose = chkConfirmOnClose.Checked,
                    MinimizeToTray = chkMinimizeToTray.Checked,
                    EnableMultiClient = chkEnableMultiClient.Checked,
                    AllowUnlimitedClients = chkEnableMultiClient.Checked && chkUnlimitedClients.Checked,
                    UsePrereleaseUpdates = chkPrereleaseUpdates.Checked,
                    GameEngine = selectedEngine,
                    RenderingProfile = (FlashRenderingProfile)Math.Max(0, cmbRenderingProfile.SelectedIndex),
                    RenderQuality = (FlashRenderQuality)Math.Max(0, cmbRenderQuality.SelectedIndex),
                    UseActiveClientFrameRate = chkUseActiveClientFrameRate.Checked,
                    ActiveClientFramesPerSecond = Decimal.ToInt32(numActiveClientFrameRate.Value),
                    ThrottleInactiveClients = chkThrottleInactiveClients.Checked,
                    InactiveClientFramesPerSecond = Decimal.ToInt32(numInactiveClientFrameRate.Value)
                };
                bool engineChanged = previousPreferences.GameEngine != preferences.GameEngine;
                bool multiClientChanged = previousPreferences.EnableMultiClient != preferences.EnableMultiClient;
                bool renderingChanged = previousPreferences.RenderingProfile != preferences.RenderingProfile
                    || previousPreferences.RenderQuality != preferences.RenderQuality;
                AppPreferences.Save(preferences);
                root.ApplyPreferences();
                bool updateChannelChanged = previousPreferences.UsePrereleaseUpdates != preferences.UsePrereleaseUpdates;
                string message = renderingChanged || engineChanged
                    ? "Your settings were saved. Rendering changes apply to new AQW clients. Restart Grimoire to apply them to every client. Current clients were not interrupted."
                    : "Your settings were saved.";
                if (multiClientChanged)
                    message += " Restart Grimoire once so future launches use the new multi-client window behavior.";
                if (updateChannelChanged)
                    message += " The selected release channel will be used on the next update check.";
                DarkMessageBox.Show(this, message, "Settings", MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                DarkMessageBox.Show(this, exception.Message, "Unable to save settings", MessageBoxIcon.Error);
            }
        }

        private void DeleteCredentials_Click(object sender, EventArgs e)
        {
            DialogResult confirmation = DarkMessageBox.Show(
                this,
                "Delete every saved character and encrypted password from the Account Locker?\r\n\r\nThis cannot be undone.",
                "Delete character information?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirmation != DialogResult.Yes)
                return;

            try
            {
                AccountVault.DeleteAllAccounts();
                root.RefreshHomeAccounts();
                DarkMessageBox.Show(this, "Saved character credentials were deleted.", "Account Locker", MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                DarkMessageBox.Show(this, exception.Message, "Unable to delete credentials", MessageBoxIcon.Error);
            }
        }

        private void FullReset_Click(object sender, EventArgs e)
        {
            DialogResult confirmation = DarkMessageBox.Show(
                this,
                "Reset all Grimoire settings and app data?\r\n\r\nSaved characters, settings, logs, recovery data, cache, and downloaded updates will be removed. Bot folders are preserved. Grimoire will restart.\r\n\r\nThis cannot be undone.",
                "Full reset Grimoire Exodus?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirmation != DialogResult.Yes)
                return;

            try
            {
                AccountVault.DeleteAllAccounts();
                AppDataReset.ResetAll();
                StartupDiagnostics.PrepareForFullReset();
                root.RestartAfterFullReset();
            }
            catch (Exception exception)
            {
                DarkMessageBox.Show(this, exception.Message, "Unable to reset Grimoire", MessageBoxIcon.Error);
            }
        }

        private async void CheckForUpdate_Click(object sender, EventArgs e)
        {
            DarkButton button = sender as DarkButton;
            if (button == null)
                return;

            string originalText = button.Text;
            button.Enabled = false;
            button.Text = "Checking...";
            try
            {
                UpdateRelease release = await root.CheckForApplicationUpdateAsync(false);
                string message = release == null
                    ? "You already have the latest release on your selected update channel."
                    : "Grimoire Exodus " + release.Version.ToString(3)
                        + (release.IsPrerelease ? " prerelease" : string.Empty)
                        + " is available. Use the update icon in the upper toolbar to install it.";
                DarkMessageBox.Show(this, message, "Update check", MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                DarkMessageBox.Show(
                    this,
                    "The update check could not be completed.\r\n\r\n" + exception.Message,
                    "Update check failed",
                    MessageBoxIcon.Error);
            }
            finally
            {
                if (!button.IsDisposed)
                {
                    button.Text = originalText;
                    button.Enabled = true;
                }
            }
        }

        private void ApplyDefaultBindings()
        {
            foreach (HotkeyBindingDefinition definition in Hotkeys.SettingsBindingDefinitions)
            {
                SetBinding(
                    definition.ActionIndex,
                    new HotkeyGesture(definition.DefaultKey, Keys.None));
            }
        }

        private void ShortcutEditor_Enter(object sender, EventArgs e)
        {
            KeyboardHook.Instance.SuppressHotkeyEvents = true;
        }

        private void ShortcutEditor_Leave(object sender, EventArgs e)
        {
            KeyboardHook.Instance.SuppressHotkeyEvents = false;
        }

        private static void ShortcutEditor_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode != Keys.Tab)
                e.IsInputKey = true;
        }

        private void ShortcutEditor_KeyDown(object sender, KeyEventArgs e)
        {
            DarkTextBox editor = sender as DarkTextBox;
            if (editor == null || !(editor.Tag is int actionIndex))
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;

            if ((e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
                && e.Modifiers == Keys.None)
            {
                SetBinding(actionIndex, null);
                return;
            }

            if (HotkeyGesture.TryCreate(e.KeyCode, e.Modifiers, out HotkeyGesture gesture))
                SetBinding(actionIndex, gesture);
        }

        private void SetBinding(int actionIndex, HotkeyGesture? gesture)
        {
            bindingValues[actionIndex] = gesture;
            if (!bindingEditors.TryGetValue(actionIndex, out DarkTextBox editor))
                return;

            editor.Text = gesture.HasValue ? gesture.Value.ToString() : "None";
        }

        private static TabPage CreateTabPage(string title)
        {
            return new TabPage
            {
                BackColor = BackgroundColor,
                ForeColor = Color.Gainsboro,
                Padding = new Padding(0),
                Text = title
            };
        }

        private Panel CreatePreferenceRow(string title, string description, Control input)
        {
            TableLayoutPanel card = new TableLayoutPanel
            {
                BackColor = CardColor,
                ColumnCount = 2,
                Height = 66,
                Margin = new Padding(0, 5, 0, 5),
                Padding = new Padding(12, 6, 12, 6),
                RowCount = 1,
                Width = 570
            };
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Panel copy = new Panel { BackColor = CardColor, Dock = DockStyle.Fill };
            Label heading = new Label
            {
                AutoEllipsis = true,
                Dock = DockStyle.Top,
                Font = new Font(Font, FontStyle.Bold),
                ForeColor = Color.Gainsboro,
                Height = 23,
                Text = title,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Label body = CreateBodyLabel(description);
            body.Dock = DockStyle.Fill;
            copy.Controls.Add(body);
            copy.Controls.Add(heading);

            input.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            input.Margin = new Padding(10, 12, 0, 12);
            card.Controls.Add(copy, 0, 0);
            card.Controls.Add(input, 1, 0);
            return card;
        }

        private static void ConfigureDropDown(DarkComboBox comboBox, params string[] items)
        {
            comboBox.AccessibleName = "Performance setting";
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox.Items.AddRange(items.Cast<object>().ToArray());
            comboBox.SelectedIndex = 0;
        }

        private static void ConfigureFrameRateInput(DarkNumericUpDown input, int minimum, int maximum)
        {
            input.AccessibleName = "Frame rate";
            input.Minimum = minimum;
            input.Maximum = maximum;
            input.TextAlign = HorizontalAlignment.Right;
        }

        private static FlowLayoutPanel CreateVerticalContent()
        {
            return new FlowLayoutPanel
            {
                AutoScroll = true,
                BackColor = BackgroundColor,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(18, 12, 18, 18),
                WrapContents = false
            };
        }

        private static Control CreateThemedScrollHost(ScrollableControl content)
        {
            Panel host = new Panel
            {
                BackColor = BackgroundColor,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            DarkScrollBar scrollBar = CreateVerticalScrollBar();
            content.Dock = DockStyle.Fill;
            host.Controls.Add(content);
            host.Controls.Add(scrollBar);
            scrollBar.SendToBack();
            new ScrollableControlThemeAdapter(host, content, scrollBar);
            return host;
        }

        private static Control CreateThemedLogHost(DarkTextBox textBox)
        {
            Panel host = new Panel
            {
                BackColor = CardColor,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            DarkScrollBar scrollBar = CreateVerticalScrollBar();
            textBox.Dock = DockStyle.Fill;
            host.Controls.Add(textBox);
            host.Controls.Add(scrollBar);
            scrollBar.SendToBack();
            new TextBoxThemeAdapter(host, textBox, scrollBar);
            return host;
        }

        private static DarkScrollBar CreateVerticalScrollBar()
        {
            return new DarkScrollBar
            {
                Dock = DockStyle.Right,
                Margin = Padding.Empty,
                Minimum = 0,
                ScrollOrientation = DarkScrollOrientation.Vertical,
                TabStop = false,
                Visible = false,
                ViewSize = 1,
                Width = 11
            };
        }

        private static Label CreateHeading(string text)
        {
            return new Label
            {
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.MediumPurple,
                Height = 31,
                Margin = new Padding(0, 6, 0, 0),
                Text = text,
                TextAlign = ContentAlignment.BottomLeft,
                Width = 570
            };
        }

        private static Label CreateBodyLabel(string text)
        {
            return new Label
            {
                AutoEllipsis = true,
                ForeColor = SecondaryTextColor,
                Padding = new Padding(0, 3, 0, 0),
                Text = text,
                TextAlign = ContentAlignment.TopLeft
            };
        }

        private static void ConfigurePreferenceCheckBox(
            DarkCheckBox checkBox,
            string title,
            string description)
        {
            checkBox.AutoSize = false;
            checkBox.BackColor = CardColor;
            checkBox.ForeColor = Color.Gainsboro;
            checkBox.Height = 54;
            checkBox.Margin = new Padding(0, 5, 0, 5);
            checkBox.Padding = new Padding(12, 0, 8, 0);
            checkBox.Text = title + Environment.NewLine + description;
            checkBox.Width = 570;
        }

        private static void ConfigureButton(DarkButton button, string text, int width)
        {
            button.BackColor = Color.FromArgb(45, 51, 66);
            button.BackColorUseGeneric = false;
            button.ForeColor = Color.Gainsboro;
            button.Height = 27;
            button.Text = text;
            button.Width = width;
        }

        private sealed class VersionReleaseItem
        {
            private readonly Version currentVersion;

            public VersionReleaseItem(UpdateRelease release, Version currentVersion)
            {
                Release = release ?? throw new ArgumentNullException(nameof(release));
                this.currentVersion = currentVersion ?? throw new ArgumentNullException(nameof(currentVersion));
            }

            public UpdateRelease Release { get; }

            public override string ToString()
            {
                int comparison = Release.Version.CompareTo(currentVersion);
                string relation = comparison < 0
                    ? "downgrade"
                    : comparison > 0
                        ? "update"
                        : "installed";
                return Release.TagName
                    + (Release.IsPrerelease ? " · prerelease" : " · stable")
                    + " · "
                    + relation;
            }
        }

        private static class NativeScrollBars
        {
            private const int SbBoth = 3;
            private const int SbVertical = 1;
            private const uint EmGetFirstVisibleLine = 0x00CE;
            private const uint EmGetLineCount = 0x00BA;
            private const uint EmLineScroll = 0x00B6;

            [DllImport("user32.dll", ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool ShowScrollBar(
                IntPtr window,
                int bar,
                [MarshalAs(UnmanagedType.Bool)] bool show);

            [DllImport(
                "user32.dll",
                EntryPoint = "SendMessageW",
                ExactSpelling = true,
                CallingConvention = CallingConvention.Winapi)]
            private static extern IntPtr SendMessage(
                IntPtr window,
                uint message,
                IntPtr wParam,
                IntPtr lParam);

            public static void HideBoth(Control control)
            {
                if (control != null && control.IsHandleCreated)
                    ShowScrollBar(control.Handle, SbBoth, false);
            }

            public static void HideVertical(Control control)
            {
                if (control != null && control.IsHandleCreated)
                    ShowScrollBar(control.Handle, SbVertical, false);
            }

            public static int GetFirstVisibleLine(TextBoxBase textBox)
            {
                return textBox == null || !textBox.IsHandleCreated
                    ? 0
                    : SendMessage(textBox.Handle, EmGetFirstVisibleLine, IntPtr.Zero, IntPtr.Zero).ToInt32();
            }

            public static int GetLineCount(TextBoxBase textBox)
            {
                return textBox == null || !textBox.IsHandleCreated
                    ? 1
                    : Math.Max(1, SendMessage(textBox.Handle, EmGetLineCount, IntPtr.Zero, IntPtr.Zero).ToInt32());
            }

            public static void ScrollLines(TextBoxBase textBox, int lines)
            {
                if (textBox != null && textBox.IsHandleCreated && lines != 0)
                    SendMessage(textBox.Handle, EmLineScroll, IntPtr.Zero, new IntPtr(lines));
            }
        }

        private sealed class ScrollableControlThemeAdapter
        {
            private readonly Control host;
            private readonly ScrollableControl content;
            private readonly DarkScrollBar scrollBar;
            private bool syncScheduled;
            private bool synchronizing;

            public ScrollableControlThemeAdapter(
                Control host,
                ScrollableControl content,
                DarkScrollBar scrollBar)
            {
                this.host = host;
                this.content = content;
                this.scrollBar = scrollBar;
                content.HandleCreated += delegate { ScheduleSync(); };
                content.Layout += delegate { ScheduleSync(); };
                content.Resize += delegate { ScheduleSync(); };
                content.Scroll += delegate { SyncFromContent(); };
                scrollBar.ValueChanged += ScrollBar_ValueChanged;
                scrollBar.MouseWheel += ScrollBar_MouseWheel;
                host.Resize += delegate { ScheduleSync(); };
                ScheduleSync();
            }

            private void ScrollBar_ValueChanged(object sender, ScrollValueEventArgs e)
            {
                if (synchronizing || !content.IsHandleCreated)
                    return;

                ScrollTo(e.Value);
            }

            private void ScrollBar_MouseWheel(object sender, MouseEventArgs e)
            {
                if (!content.IsHandleCreated || e.Delta == 0)
                    return;

                int lines = SystemInformation.MouseWheelScrollLines;
                int step = lines < 0
                    ? Math.Max(1, content.ClientSize.Height)
                    : Math.Max(1, lines) * Math.Max(16, content.Font.Height);
                int notches = e.Delta / SystemInformation.MouseWheelScrollDelta;
                int current = Math.Abs(content.AutoScrollPosition.Y);
                ScrollTo(current - (notches * step));

                HandledMouseEventArgs handled = e as HandledMouseEventArgs;
                if (handled != null)
                    handled.Handled = true;
            }

            private void ScrollTo(int requestedValue)
            {
                int maximum = GetMaximumScrollValue();
                int value = Math.Max(0, Math.Min(requestedValue, maximum));
                try
                {
                    content.AutoScrollPosition = new Point(
                        Math.Abs(content.AutoScrollPosition.X),
                        value);
                }
                catch (ArgumentOutOfRangeException)
                {
                }
                NativeScrollBars.HideBoth(content);
                ScheduleSync();
            }

            private void ScheduleSync()
            {
                if (!content.IsHandleCreated || content.IsDisposed)
                    return;
                NativeScrollBars.HideBoth(content);
                if (syncScheduled)
                    return;
                syncScheduled = true;
                try
                {
                    content.BeginInvoke(new Action(delegate
                    {
                        syncScheduled = false;
                        SyncFromContent();
                    }));
                }
                catch (InvalidOperationException)
                {
                    syncScheduled = false;
                }
            }

            private void SyncFromContent()
            {
                if (content.IsDisposed || !content.IsHandleCreated)
                    return;

                synchronizing = true;
                try
                {
                    int viewportHeight = Math.Max(1, content.ClientSize.Height);
                    int contentHeight = Math.Max(viewportHeight, content.DisplayRectangle.Height);
                    int maximumValue = Math.Max(0, contentHeight - viewportHeight);
                    scrollBar.Minimum = 0;
                    scrollBar.Maximum = Math.Max(1, contentHeight);
                    scrollBar.ViewSize = Math.Min(contentHeight, viewportHeight);
                    scrollBar.Value = Math.Min(
                        Math.Abs(content.AutoScrollPosition.Y),
                        maximumValue);
                    bool wasVisible = scrollBar.Visible;
                    scrollBar.Visible = maximumValue > 0;
                    if (wasVisible != scrollBar.Visible)
                        host.PerformLayout();
                    NativeScrollBars.HideBoth(content);
                }
                finally
                {
                    synchronizing = false;
                }
            }

            private int GetMaximumScrollValue()
            {
                return Math.Max(
                    0,
                    content.DisplayRectangle.Height - Math.Max(1, content.ClientSize.Height));
            }
        }

        private sealed class TextBoxThemeAdapter
        {
            private readonly Control host;
            private readonly DarkTextBox textBox;
            private readonly DarkScrollBar scrollBar;
            private bool syncScheduled;
            private bool synchronizing;

            public TextBoxThemeAdapter(Control host, DarkTextBox textBox, DarkScrollBar scrollBar)
            {
                this.host = host;
                this.textBox = textBox;
                this.scrollBar = scrollBar;
                textBox.HandleCreated += delegate { ScheduleSync(); };
                textBox.TextChanged += delegate { ScheduleSync(); };
                textBox.Resize += delegate { ScheduleSync(); };
                textBox.MouseWheel += delegate { ScheduleSync(); };
                textBox.KeyUp += delegate { ScheduleSync(); };
                scrollBar.ValueChanged += ScrollBar_ValueChanged;
                scrollBar.MouseWheel += ScrollBar_MouseWheel;
                host.Resize += delegate { ScheduleSync(); };
                ScheduleSync();
            }

            private void ScrollBar_ValueChanged(object sender, ScrollValueEventArgs e)
            {
                if (synchronizing || !textBox.IsHandleCreated)
                    return;
                int current = NativeScrollBars.GetFirstVisibleLine(textBox);
                NativeScrollBars.ScrollLines(textBox, e.Value - current);
                NativeScrollBars.HideVertical(textBox);
                ScheduleSync();
            }

            private void ScrollBar_MouseWheel(object sender, MouseEventArgs e)
            {
                if (!textBox.IsHandleCreated || e.Delta == 0)
                    return;

                int lines = SystemInformation.MouseWheelScrollLines;
                int step = lines < 0 ? GetVisibleLineCount() : Math.Max(1, lines);
                int notches = e.Delta / SystemInformation.MouseWheelScrollDelta;
                int current = NativeScrollBars.GetFirstVisibleLine(textBox);
                int maximum = GetMaximumScrollValue();
                int target = Math.Max(0, Math.Min(current - (notches * step), maximum));
                NativeScrollBars.ScrollLines(textBox, target - current);
                NativeScrollBars.HideVertical(textBox);
                ScheduleSync();

                HandledMouseEventArgs handled = e as HandledMouseEventArgs;
                if (handled != null)
                    handled.Handled = true;
            }

            private void ScheduleSync()
            {
                if (!textBox.IsHandleCreated || textBox.IsDisposed)
                    return;
                NativeScrollBars.HideVertical(textBox);
                if (syncScheduled)
                    return;
                syncScheduled = true;
                try
                {
                    textBox.BeginInvoke(new Action(delegate
                    {
                        syncScheduled = false;
                        SyncFromTextBox();
                    }));
                }
                catch (InvalidOperationException)
                {
                    syncScheduled = false;
                }
            }

            private void SyncFromTextBox()
            {
                if (textBox.IsDisposed || !textBox.IsHandleCreated)
                    return;

                synchronizing = true;
                try
                {
                    int lineCount = NativeScrollBars.GetLineCount(textBox);
                    int visibleLines = GetVisibleLineCount();
                    int maximumValue = Math.Max(0, lineCount - visibleLines);
                    scrollBar.Minimum = 0;
                    scrollBar.Maximum = Math.Max(1, lineCount);
                    scrollBar.ViewSize = Math.Min(lineCount, visibleLines);
                    scrollBar.Value = Math.Min(NativeScrollBars.GetFirstVisibleLine(textBox), maximumValue);
                    bool wasVisible = scrollBar.Visible;
                    scrollBar.Visible = maximumValue > 0;
                    if (wasVisible != scrollBar.Visible)
                        host.PerformLayout();
                    NativeScrollBars.HideVertical(textBox);
                }
                finally
                {
                    synchronizing = false;
                }
            }

            private int GetMaximumScrollValue()
            {
                return Math.Max(0, NativeScrollBars.GetLineCount(textBox) - GetVisibleLineCount());
            }

            private int GetVisibleLineCount()
            {
                return Math.Max(1, textBox.ClientSize.Height / Math.Max(1, textBox.Font.Height));
            }
        }
    }
}
