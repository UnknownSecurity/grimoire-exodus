using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

internal static class QuickBotSelectorSmoke
{
    private static Assembly clientAssembly;

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
}
