using DarkUI.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

internal static class QuickBotSelectorSmoke
{
    private const int GwlStyle = -16;
    private const int WsVScroll = 0x00200000;
    private static Assembly clientAssembly;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
    private static extern int GetWindowLong(IntPtr window, int index);

    [STAThread]
    private static int Main()
    {
        try
        {
            string clientPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Grimoire Exodus.exe");
            clientAssembly = Assembly.LoadFrom(clientPath);
            AppDomain.CurrentDomain.AssemblyResolve += ResolveClientAssembly;
            Type selectorType = clientAssembly.GetType("Grimoire.UI.QuickBotSelectorForm", true);
            Type rootType = clientAssembly.GetType("Grimoire.UI.Root", true);
            FieldInfo quickBotButton = rootType.GetField("quickBotsButton", BindingFlags.Instance | BindingFlags.NonPublic);
            Require(quickBotButton != null, "Quick Bot toolbar button field was not found.");
            Require(typeof(ToolStripButton).IsAssignableFrom(quickBotButton.FieldType), "Quick Bot toolbar control is still a drop-down.");
            Require(rootType.GetField("quickBotsMenuItem", BindingFlags.Instance | BindingFlags.NonPublic) == null, "Legacy Quick Bot drop-down field remains.");
            MethodInfo buttonFactory = rootType.GetMethod(
                "CreateQuickBotsButton",
                BindingFlags.Static | BindingFlags.NonPublic);
            Require(buttonFactory != null, "Quick Bot toolbar button factory was not found.");
            using (Bitmap icon = new Bitmap(16, 16))
            using (ToolStripButton button = (ToolStripButton)buttonFactory.Invoke(null, new object[] { icon }))
            using (MenuStrip toolbar = new MenuStrip())
            {
                toolbar.Size = new Size(960, 24);
                toolbar.Items.Add(new ToolStripMenuItem("Home"));
                toolbar.Items.Add(button);
                toolbar.PerformLayout();

                Require(button.Alignment == ToolStripItemAlignment.Right, "Quick Bot toolbar button is not right-aligned.");
                Require(button.AutoSize, "Quick Bot toolbar button should size to its compact content.");
                Require(button.DisplayStyle == ToolStripItemDisplayStyle.ImageAndText, "Quick Bot toolbar button does not show an icon and label.");
                Require(button.Image != null, "Quick Bot toolbar button icon is missing.");
                Require(button.Text == "Quick Load", "Quick Bot toolbar button label is not stable.");
                Require(button.Bounds.Right == toolbar.ClientSize.Width, "Quick Bot toolbar button is not flush with the right edge.");
            }
            ConstructorInfo constructor = selectorType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(IEnumerable<string>), typeof(string) },
                null);
            Require(constructor != null, "Quick Bot selector constructor was not found.");

            VerifyThemedScrollbar(constructor);

            string basePath = Path.Combine(Path.GetTempPath(), "grimoire-quick-bot-selector-smoke");
            string firstPath = Path.Combine(basePath, "Alpha.gbot");
            string secondPath = Path.Combine(basePath, "Beta.gbot");
            using (Form selector = (Form)constructor.Invoke(new object[]
            {
                new[] { firstPath, secondPath },
                secondPath
            }))
            {
                Require(selector.MinimumSize.Width >= 380, "Quick Bot selector minimum width is too small.");
                Require(selector.MinimumSize.Height >= 320, "Quick Bot selector minimum height is too small.");
                Require(selector.StartPosition == FormStartPosition.CenterParent, "Quick Bot selector is not centered on its owner.");
                Require(!selector.ShowInTaskbar, "Quick Bot selector should remain owned by the main window.");

                ListBox list = FindControl<ListBox>(selector, "Available GBots");
                TextBox filter = FindControl<TextBox>(selector, "Filter GBots");
                Button load = FindButton(selector, "Load");
                Require(list != null, "Scrollable GBot list was not found.");
                Require(filter != null, "GBot filter field was not found.");
                Require(load != null, "GBot Load button was not found.");
                Require(!list.IntegralHeight, "GBot list should resize continuously with the window.");
                Require(list.SelectionMode == SelectionMode.One, "GBot list should allow one selection.");
                Require(list.Items.Count == 2, "GBot list did not receive the supplied entries.");
                Require(list.SelectedItem != null && list.SelectedItem.ToString() == "Beta", "Previously selected GBot was not restored.");

                filter.Text = "Alpha";
                Require(list.Items.Count == 1 && list.Items[0].ToString() == "Alpha", "GBot search did not filter the list.");
                list.SelectedIndex = 0;
                Require(load.Enabled, "Load button did not enable after selecting a GBot.");
                selector.StartPosition = FormStartPosition.Manual;
                selector.Location = new Point(-32000, -32000);
                selector.Opacity = 0.01d;
                selector.Show();
                Application.DoEvents();
                load.PerformClick();
                Application.DoEvents();

                PropertyInfo selectedPath = selectorType.GetProperty("SelectedBotPath");
                Require(selectedPath != null, "SelectedBotPath is missing.");
                Require(string.Equals(selectedPath.GetValue(selector) as string, firstPath, StringComparison.OrdinalIgnoreCase), "Selected GBot path was not returned.");
                Require(selector.DialogResult == DialogResult.OK, "Selecting Load did not accept the dialog.");
            }

            Console.WriteLine("Quick Bot selector validation passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void VerifyThemedScrollbar(ConstructorInfo constructor)
    {
        string[] paths = new string[40];
        for (int index = 0; index < paths.Length; index++)
            paths[index] = Path.Combine(Path.GetTempPath(), "Bot " + index.ToString("00") + ".gbot");

        using (Form selector = (Form)constructor.Invoke(new object[] { paths, null }))
        {
            selector.StartPosition = FormStartPosition.Manual;
            selector.Location = new Point(-32000, -32000);
            selector.Opacity = 0.01d;
            selector.Show();
            PumpMessages();

            ListBox list = FindControl<ListBox>(selector, "Available GBots");
            DarkScrollBar scrollBar = FindControl<DarkScrollBar>(selector, "GBot list scrollbar");
            Require(list != null, "Scrollable GBot list was not found for the overflow scenario.");
            Require(scrollBar != null, "The themed GBot list scrollbar was not created.");
            Require(scrollBar.Visible, "The themed GBot list scrollbar is hidden while the list overflows.");
            Require(scrollBar.Width <= 12, "The themed GBot list scrollbar is too bulky.");
            Require(list.Right <= scrollBar.Left, "The themed GBot scrollbar overlaps the GBot list.");
            Require((GetWindowLong(list.Handle, GwlStyle) & WsVScroll) == 0,
                "The native white ListBox scrollbar is still visible.");

            Control hit = scrollBar.Parent.GetChildAtPoint(new Point(
                scrollBar.Left + (scrollBar.Width / 2),
                scrollBar.Top + (scrollBar.Height / 2)));
            Require(ReferenceEquals(hit, scrollBar), "The themed GBot scrollbar cannot receive pointer input.");

            DragThumbToBottom(scrollBar);
            PumpMessages();
            Require(list.TopIndex > 0, "Dragging the themed GBot scrollbar does not navigate the list.");

            list.TopIndex = 0;
            scrollBar.Value = 0;
            InvokeMouse(scrollBar, "OnMouseWheel", new MouseEventArgs(MouseButtons.None, 0, 0, 0, -120));
            PumpMessages();
            Require(list.TopIndex > 0, "The mouse wheel does not navigate the themed GBot scrollbar.");
        }

        using (Form selector = (Form)constructor.Invoke(new object[]
        {
            new[] { Path.Combine(Path.GetTempPath(), "Only.gbot") },
            null
        }))
        {
            selector.StartPosition = FormStartPosition.Manual;
            selector.Location = new Point(-32000, -32000);
            selector.Opacity = 0.01d;
            selector.Show();
            PumpMessages();

            DarkScrollBar scrollBar = FindControl<DarkScrollBar>(selector, "GBot list scrollbar");
            Require(scrollBar != null, "The themed GBot list scrollbar was not created for fitting content.");
            Require(!scrollBar.Visible, "The themed GBot list scrollbar reserves space when content fits.");
        }
    }

    private static Assembly ResolveClientAssembly(object sender, ResolveEventArgs args)
    {
        AssemblyName requested = new AssemblyName(args.Name);
        return string.Equals(requested.Name, "Grimoire", StringComparison.OrdinalIgnoreCase)
            ? clientAssembly
            : null;
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

    private static Button FindButton(Control parent, string text)
    {
        foreach (Control child in parent.Controls)
        {
            Button match = child as Button;
            if (match != null && string.Equals(match.Text, text, StringComparison.Ordinal))
                return match;

            Button nested = FindButton(child, text);
            if (nested != null)
                return nested;
        }
        return null;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void DragThumbToBottom(DarkScrollBar scrollBar)
    {
        FieldInfo thumbField = typeof(DarkScrollBar).GetField(
            "_thumbArea",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(thumbField != null, "Dark scrollbar thumb geometry is unavailable.");
        Rectangle thumb = (Rectangle)thumbField.GetValue(scrollBar);
        Require(!thumb.IsEmpty, "Dark scrollbar thumb geometry is empty.");

        int startX = thumb.Left + Math.Max(1, thumb.Width / 2);
        int startY = thumb.Top + Math.Max(1, thumb.Height / 2);
        int endY = Math.Max(startY + 1, scrollBar.ClientSize.Height - 20);
        InvokeMouse(scrollBar, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, startX, startY, 0));
        InvokeMouse(scrollBar, "OnMouseMove", new MouseEventArgs(MouseButtons.Left, 0, startX, endY, 0));
        InvokeMouse(scrollBar, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, startX, endY, 0));
    }

    private static void InvokeMouse(DarkScrollBar scrollBar, string methodName, MouseEventArgs eventArgs)
    {
        MethodInfo method = typeof(DarkScrollBar).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(method != null, "Dark scrollbar mouse handler is unavailable: " + methodName);
        method.Invoke(scrollBar, new object[] { eventArgs });
    }

    private static void PumpMessages()
    {
        for (int index = 0; index < 8; index++)
        {
            Application.DoEvents();
            Thread.Sleep(2);
        }
    }
}
