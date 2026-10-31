using DarkUI.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Grimoire.UI
{
    [DesignerCategory("Code")]
    internal sealed class QuickBotSelectorForm : GrimoireForm
    {
        private static readonly Color BackgroundColor = Color.FromArgb(28, 32, 40);
        private static readonly Color CardColor = Color.FromArgb(36, 40, 50);
        private static readonly Color SecondaryTextColor = Color.FromArgb(170, 174, 188);

        private readonly IContainer components = new Container();
        private readonly List<BotListItem> allBots;
        private readonly DarkTextBox filterBox = new DarkTextBox();
        private readonly DarkListBox botList;
        private readonly DarkScrollBar botListScrollBar = new DarkScrollBar();
        private readonly DarkButton loadButton = new DarkButton();
        private readonly DarkButton cancelButton = new DarkButton();
        private ListBoxThemeAdapter botListThemeAdapter;

        public QuickBotSelectorForm(IEnumerable<string> botPaths, string selectedBotPath)
        {
            if (botPaths == null)
                throw new ArgumentNullException(nameof(botPaths));

            allBots = botPaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => new BotListItem(path))
                .ToList();
            botList = new DarkListBox(components);
            InitializeSelector(selectedBotPath);
        }

        public string SelectedBotPath { get; private set; }

        private void InitializeSelector(string selectedBotPath)
        {
            Text = "Quick Load GBot";
            Icon = (Icon)global::Properties.Resources.GrimoireIcon.Clone();
            BackColor = BackgroundColor;
            ForeColor = Color.Gainsboro;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            MinimumSize = new Size(380, 320);
            ClientSize = new Size(500, 440);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;

            TableLayoutPanel layout = new TableLayoutPanel
            {
                BackColor = BackgroundColor,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Padding = new Padding(14),
                RowCount = 4
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            Panel introduction = new Panel { BackColor = BackgroundColor, Dock = DockStyle.Fill };
            Label heading = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Font = new Font(Font, FontStyle.Bold),
                ForeColor = Color.Gainsboro,
                Height = 25,
                Text = "Choose a GBot"
            };
            Label description = new Label
            {
                AutoEllipsis = true,
                Dock = DockStyle.Fill,
                ForeColor = SecondaryTextColor,
                Text = "Select a bot to load into the active client."
            };
            introduction.Controls.Add(description);
            introduction.Controls.Add(heading);

            TableLayoutPanel filterRow = new TableLayoutPanel
            {
                BackColor = BackgroundColor,
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8),
                RowCount = 1
            };
            filterRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56F));
            filterRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            Label filterLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = SecondaryTextColor,
                Text = "Search",
                TextAlign = ContentAlignment.MiddleLeft
            };
            filterBox.AccessibleName = "Filter GBots";
            filterBox.Dock = DockStyle.Fill;
            filterBox.Margin = new Padding(0, 2, 0, 5);
            filterBox.TabIndex = 0;
            filterBox.TextChanged += FilterBox_TextChanged;
            filterRow.Controls.Add(filterLabel, 0, 0);
            filterRow.Controls.Add(filterBox, 1, 0);

            botList.AccessibleDescription = "Scrollable list of GBots available for quick loading.";
            botList.AccessibleName = "Available GBots";
            botList.BackColor = CardColor;
            botList.BorderStyle = BorderStyle.FixedSingle;
            botList.Dock = DockStyle.Fill;
            botList.DrawMode = DrawMode.OwnerDrawFixed;
            botList.ForeColor = Color.Gainsboro;
            botList.FormattingEnabled = true;
            botList.IntegralHeight = false;
            botList.ItemHeight = 24;
            botList.Margin = new Padding(0);
            botList.SelectionMode = SelectionMode.One;
            botList.TabIndex = 1;
            botList.SelectedIndexChanged += BotList_SelectedIndexChanged;
            botList.DoubleClick += BotList_DoubleClick;

            Panel botListHost = new Panel
            {
                BackColor = CardColor,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            botListScrollBar.AccessibleName = "GBot list scrollbar";
            botListScrollBar.Dock = DockStyle.Right;
            botListScrollBar.Margin = Padding.Empty;
            botListScrollBar.Minimum = 0;
            botListScrollBar.ScrollOrientation = DarkScrollOrientation.Vertical;
            botListScrollBar.TabStop = false;
            botListScrollBar.Visible = false;
            botListScrollBar.ViewSize = 1;
            botListScrollBar.Width = 11;
            botListHost.Controls.Add(botList);
            botListHost.Controls.Add(botListScrollBar);
            botListScrollBar.SendToBack();
            botListThemeAdapter = new ListBoxThemeAdapter(botListHost, botList, botListScrollBar);

            TableLayoutPanel footer = new TableLayoutPanel
            {
                BackColor = BackgroundColor,
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                RowCount = 1
            };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            Label hint = new Label
            {
                AutoEllipsis = true,
                Dock = DockStyle.Fill,
                ForeColor = SecondaryTextColor,
                Text = "Double-click a bot or select Load.",
                TextAlign = ContentAlignment.MiddleLeft
            };
            FlowLayoutPanel actions = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 9, 0, 0),
                WrapContents = false
            };
            ConfigureButton(loadButton, "Load", 88);
            ConfigureButton(cancelButton, "Cancel", 88);
            loadButton.Enabled = false;
            loadButton.TabIndex = 2;
            loadButton.Click += LoadButton_Click;
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.TabIndex = 3;
            actions.Controls.Add(loadButton);
            actions.Controls.Add(cancelButton);
            footer.Controls.Add(hint, 0, 0);
            footer.Controls.Add(actions, 1, 0);

            layout.Controls.Add(introduction, 0, 0);
            layout.Controls.Add(filterRow, 0, 1);
            layout.Controls.Add(botListHost, 0, 2);
            layout.Controls.Add(footer, 0, 3);
            Controls.Add(layout);

            AcceptButton = loadButton;
            CancelButton = cancelButton;
            PopulateBots(selectedBotPath);
        }

        private static void ConfigureButton(DarkButton button, string text, int width)
        {
            button.Height = 29;
            button.Margin = new Padding(8, 0, 0, 0);
            button.Text = text;
            button.Width = width;
        }

        private void FilterBox_TextChanged(object sender, EventArgs e)
        {
            BotListItem selected = botList.SelectedItem as BotListItem;
            PopulateBots(selected?.Path);
        }

        private void PopulateBots(string selectedBotPath)
        {
            string filter = filterBox.Text == null ? string.Empty : filterBox.Text.Trim();
            botList.BeginUpdate();
            try
            {
                botList.Items.Clear();
                foreach (BotListItem bot in allBots)
                {
                    if (filter.Length == 0
                        || bot.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        int index = botList.Items.Add(bot);
                        if (!string.IsNullOrWhiteSpace(selectedBotPath)
                            && string.Equals(bot.Path, selectedBotPath, StringComparison.OrdinalIgnoreCase))
                        {
                            botList.SelectedIndex = index;
                        }
                    }
                }
                if (botList.Items.Count == 0)
                {
                    botList.Items.Add(filter.Length == 0
                        ? "No GBots found. Add files to Documents\\Grimoire\\Bot."
                        : "No matching GBots found.");
                }
            }
            finally
            {
                botList.EndUpdate();
            }
            loadButton.Enabled = botList.SelectedItem is BotListItem;
            botListThemeAdapter?.Refresh();
        }

        private void BotList_SelectedIndexChanged(object sender, EventArgs e)
        {
            loadButton.Enabled = botList.SelectedItem is BotListItem;
        }

        private void BotList_DoubleClick(object sender, EventArgs e)
        {
            AcceptSelection();
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            AcceptSelection();
        }

        private void AcceptSelection()
        {
            BotListItem selected = botList.SelectedItem as BotListItem;
            if (selected == null)
                return;

            SelectedBotPath = selected.Path;
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private static class NativeScrollBars
        {
            private const int SbVertical = 1;

            [DllImport("user32.dll", ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool ShowScrollBar(
                IntPtr window,
                int bar,
                [MarshalAs(UnmanagedType.Bool)] bool show);

            public static void HideVertical(Control control)
            {
                if (control != null && control.IsHandleCreated)
                    ShowScrollBar(control.Handle, SbVertical, false);
            }
        }

        private sealed class ListBoxThemeAdapter
        {
            private readonly Control host;
            private readonly ListBox listBox;
            private readonly DarkScrollBar scrollBar;
            private bool syncScheduled;
            private bool synchronizing;

            public ListBoxThemeAdapter(Control host, ListBox listBox, DarkScrollBar scrollBar)
            {
                this.host = host;
                this.listBox = listBox;
                this.scrollBar = scrollBar;
                listBox.HandleCreated += delegate { ScheduleSync(); };
                listBox.Resize += delegate { ScheduleSync(); };
                listBox.MouseWheel += delegate { ScheduleSync(); };
                listBox.KeyUp += delegate { ScheduleSync(); };
                listBox.SelectedIndexChanged += delegate { ScheduleSync(); };
                scrollBar.ValueChanged += ScrollBar_ValueChanged;
                scrollBar.MouseWheel += ScrollBar_MouseWheel;
                host.Resize += delegate { ScheduleSync(); };
                ScheduleSync();
            }

            public void Refresh()
            {
                ScheduleSync();
            }

            private void ScrollBar_MouseWheel(object sender, MouseEventArgs e)
            {
                if (!listBox.IsHandleCreated || e.Delta == 0)
                    return;

                int visibleItems = GetVisibleItemCount();
                int lines = SystemInformation.MouseWheelScrollLines;
                int step = lines < 0 ? visibleItems : Math.Max(1, lines);
                int notches = e.Delta / SystemInformation.MouseWheelScrollDelta;
                int value = listBox.TopIndex - (notches * step);
                listBox.TopIndex = Math.Max(0, Math.Min(value, GetMaximumTopIndex()));
                NativeScrollBars.HideVertical(listBox);
                ScheduleSync();

                HandledMouseEventArgs handled = e as HandledMouseEventArgs;
                if (handled != null)
                    handled.Handled = true;
            }

            private void ScrollBar_ValueChanged(object sender, ScrollValueEventArgs e)
            {
                if (synchronizing || !listBox.IsHandleCreated)
                    return;

                int maximum = GetMaximumTopIndex();
                listBox.TopIndex = Math.Max(0, Math.Min(e.Value, maximum));
                NativeScrollBars.HideVertical(listBox);
                ScheduleSync();
            }

            private void ScheduleSync()
            {
                if (!listBox.IsHandleCreated || listBox.IsDisposed)
                    return;

                NativeScrollBars.HideVertical(listBox);
                if (syncScheduled)
                    return;
                syncScheduled = true;
                try
                {
                    listBox.BeginInvoke(new Action(delegate
                    {
                        syncScheduled = false;
                        SyncFromListBox();
                    }));
                }
                catch (InvalidOperationException)
                {
                    syncScheduled = false;
                }
            }

            private void SyncFromListBox()
            {
                if (listBox.IsDisposed || !listBox.IsHandleCreated)
                    return;

                synchronizing = true;
                try
                {
                    int itemCount = listBox.Items.Count;
                    int visibleItems = GetVisibleItemCount();
                    int maximum = Math.Max(0, itemCount - visibleItems);
                    scrollBar.Minimum = 0;
                    scrollBar.Maximum = Math.Max(1, itemCount);
                    scrollBar.ViewSize = Math.Min(Math.Max(1, itemCount), visibleItems);
                    scrollBar.Value = Math.Min(listBox.TopIndex, maximum);
                    bool wasVisible = scrollBar.Visible;
                    scrollBar.Visible = maximum > 0;
                    if (wasVisible != scrollBar.Visible)
                        host.PerformLayout();
                    NativeScrollBars.HideVertical(listBox);
                }
                finally
                {
                    synchronizing = false;
                }
            }

            private int GetMaximumTopIndex()
            {
                return Math.Max(0, listBox.Items.Count - GetVisibleItemCount());
            }

            private int GetVisibleItemCount()
            {
                return Math.Max(
                    1,
                    listBox.ClientSize.Height / Math.Max(1, listBox.ItemHeight));
            }
        }

        private sealed class BotListItem
        {
            public BotListItem(string path)
            {
                Path = path;
                Name = System.IO.Path.GetFileNameWithoutExtension(path);
            }

            public string Name { get; }

            public string Path { get; }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}
