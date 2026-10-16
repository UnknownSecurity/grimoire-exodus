using DarkUI.Controls;
using DarkUI.Forms;
using Grimoire.Tools;
using Grimoire.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
        private readonly Dictionary<int, DarkComboBox> bindingSelectors = new Dictionary<int, DarkComboBox>();
        private readonly DarkButton btnSave = new DarkButton();
        private readonly DarkButton btnClose = new DarkButton();

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
                ItemSize = new Size(110, 24),
                SizeMode = TabSizeMode.Fixed
            };
            tabs.TabPages.Add(CreateGeneralTab());
            tabs.TabPages.Add(CreateKeyBindingsTab());
            tabs.TabPages.Add(CreateAdvancedTab());

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
            page.Controls.Add(content);
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
                "Shortcuts work while Grimoire is focused. Existing legacy shortcuts remain available in Tools > Hotkeys.");
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

                DarkComboBox selector = new DarkComboBox
                {
                    AccessibleName = definition.DisplayName + " shortcut",
                    Dock = DockStyle.Fill,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Margin = new Padding(8, 10, 8, 10)
                };
                selector.Items.Add("None");
                selector.Items.AddRange(GetKeyChoices().Cast<object>().ToArray());
                bindingSelectors[definition.ActionIndex] = selector;

                content.Controls.Add(label, 0, row);
                content.Controls.Add(selector, 1, row);
                row++;
            }

            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            DarkButton defaults = new DarkButton();
            ConfigureButton(defaults, "Restore defaults", 132);
            defaults.Margin = new Padding(8, 8, 8, 8);
            defaults.Click += delegate { ApplyDefaultBindings(); };
            content.Controls.Add(defaults, 1, row);

            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            page.Controls.Add(content);
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
                RowCount = 4
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            AddAdvancedAction(
                content,
                0,
                "Delete character information",
                "Permanently removes every saved account and encrypted password from the Account Locker.",
                "Delete credentials",
                DeleteCredentials_Click);
            AddAdvancedAction(
                content,
                1,
                "Full reset",
                "Removes Grimoire settings, saved characters, logs, recovery data, cache, and downloaded updates. Bot folders are preserved.",
                "Full reset",
                FullReset_Click);
            AddAdvancedAction(
                content,
                2,
                "Check for an update",
                "Checks the latest public GitHub release and shows the update icon when a newer version is available.",
                "Check now",
                CheckForUpdate_Click);

            page.Controls.Add(content);
            return page;
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

            foreach (HotkeyBindingDefinition definition in Hotkeys.SettingsBindingDefinitions)
            {
                SetSelectedKey(
                    bindingSelectors[definition.ActionIndex],
                    Hotkeys.GetBindingKey(definition.ActionIndex));
            }
        }

        private void Save_Click(object sender, EventArgs e)
        {
            try
            {
                Dictionary<int, Keys?> bindings = bindingSelectors.ToDictionary(
                    item => item.Key,
                    item => ReadSelectedKey(item.Value));
                Hotkeys.SaveSettingsBindings(bindings);
                AppPreferences.Save(new AppPreferenceSnapshot
                {
                    SkipHomePage = chkSkipHome.Checked,
                    HideDeviceStats = chkHideDeviceStats.Checked,
                    ConfirmOnClose = chkConfirmOnClose.Checked,
                    MinimizeToTray = chkMinimizeToTray.Checked
                });
                root.ApplyPreferences();
                DarkMessageBox.Show(this, "Your settings were saved.", "Settings", MessageBoxIcon.Information);
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
                    ? "You already have the latest public release."
                    : "Grimoire Exodus " + release.Version.ToString(3)
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
                SetSelectedKey(bindingSelectors[definition.ActionIndex], definition.DefaultKey);
            }
        }

        private static IEnumerable<Keys> GetKeyChoices()
        {
            for (Keys key = Keys.F1; key <= Keys.F12; key++)
                yield return key;
            for (Keys key = Keys.A; key <= Keys.Z; key++)
                yield return key;
            for (Keys key = Keys.D0; key <= Keys.D9; key++)
                yield return key;
        }

        private static Keys? ReadSelectedKey(DarkComboBox selector)
        {
            return selector.SelectedItem is Keys key ? key : (Keys?)null;
        }

        private static void SetSelectedKey(DarkComboBox selector, Keys? key)
        {
            if (!key.HasValue)
            {
                selector.SelectedIndex = 0;
                return;
            }

            for (int index = 0; index < selector.Items.Count; index++)
            {
                if (selector.Items[index] is Keys item && item == key.Value)
                {
                    selector.SelectedIndex = index;
                    return;
                }
            }

            selector.Items.Add(key.Value);
            selector.SelectedItem = key.Value;
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
    }
}
