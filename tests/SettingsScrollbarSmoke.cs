using DarkUI.Controls;
using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Forms;

internal static class SettingsScrollbarSmoke
{
    private const int GwlStyle = -16;
    private const uint EmGetFirstVisibleLine = 0x00CE;
    private const int WsVScroll = 0x00200000;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
    private static extern int GetWindowLong(IntPtr window, int index);

    [DllImport("user32.dll", EntryPoint = "SendMessageW", ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
    private static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    [STAThread]
    private static int Main()
    {
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Type settingsType = typeof(Grimoire.UI.Root).Assembly.GetType(
                "Grimoire.UI.SettingsForm",
                true);
            MethodInfo createHost = settingsType.GetMethod(
                "CreateThemedScrollHost",
                BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo createLogHost = settingsType.GetMethod(
                "CreateThemedLogHost",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (createHost == null || createLogHost == null)
                throw new InvalidOperationException("A Settings scroll host factory was not found.");

            VerifyTabOrder(settingsType);
            VerifyScenario(createHost, 2, false, "fitting content");
            VerifyScenario(createHost, 18, true, "overflowing content");
            VerifyLogScenario(createLogHost, 1, false, "fitting log text");
            VerifyLogScenario(createLogHost, 80, true, "overflowing log text");

            Console.WriteLine("PASS: Settings scrollbars appear only for real overflow and remain themed during repeated layout.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void VerifyTabOrder(Type settingsType)
    {
        Type rootType = typeof(Grimoire.UI.Root);
        ConstructorInfo constructor = settingsType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { rootType },
            null);
        if (constructor == null)
            throw new InvalidOperationException("The Settings form constructor was not found.");

        object root = FormatterServices.GetUninitializedObject(rootType);
        using (Form settings = (Form)constructor.Invoke(new[] { root }))
        {
            TabControl tabs = FindControl<TabControl>(settings);
            if (tabs == null)
                throw new InvalidOperationException("The Settings tabs were not created.");

            string[] expected = { "General", "Performance", "Key Bindings", "Advanced", "Version", "Logs" };
            string[] actual = tabs.TabPages.Cast<TabPage>().Select(page => page.Text).ToArray();
            if (!actual.SequenceEqual(expected))
                throw new InvalidOperationException("Unexpected Settings tab order: " + string.Join(", ", actual));

            TabPage version = tabs.TabPages.Cast<TabPage>().Single(page => page.Text == "Version");
            if (FindControl<ComboBox>(version, "Grimoire Exodus version") == null)
                throw new InvalidOperationException("The Version tab does not expose its release selector.");
        }
    }

    private static void VerifyScenario(
        MethodInfo createHost,
        int itemCount,
        bool expectScrollBar,
        string scenario)
    {
        using (Form form = new Form
        {
            ClientSize = new Size(640, 420),
            FormBorderStyle = FormBorderStyle.FixedToolWindow,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Left = -32000,
            Top = -32000
        })
        using (FlowLayoutPanel content = CreateContent(itemCount))
        {
            Control host = (Control)createHost.Invoke(null, new object[] { content });
            host.Dock = DockStyle.Fill;
            form.Controls.Add(host);
            form.Show();
            PumpMessages();

            DarkScrollBar scrollBar = FindScrollBar(host);
            if (scrollBar == null)
                throw new InvalidOperationException("The themed Settings scrollbar was not created.");
            if (scrollBar.Visible != expectScrollBar)
            {
                throw new InvalidOperationException(
                    "Unexpected scrollbar visibility for " + scenario
                    + ". Expected " + expectScrollBar + " but got " + scrollBar.Visible
                    + ". Display=" + content.DisplayRectangle.Height
                    + ", client=" + content.ClientSize.Height
                    + ", nativeVisible=" + content.VerticalScroll.Visible
                    + ", maximum=" + content.VerticalScroll.Maximum
                    + ", largeChange=" + content.VerticalScroll.LargeChange
                    + ", contentBounds=" + content.Bounds
                    + ", scrollBounds=" + scrollBar.Bounds + ".");
            }
            VerifyBarGeometry(content, scrollBar, expectScrollBar, scenario);

            for (int index = 0; index < 12; index++)
            {
                form.ClientSize = new Size(640 + (index % 2), 420);
                form.PerformLayout();
                content.PerformLayout();
                if (HasNativeVerticalScrollStyle(content))
                    throw new InvalidOperationException("A native white scrollbar resurfaced during " + scenario + ".");
                PumpMessages();
                if (scrollBar.Visible != expectScrollBar)
                    throw new InvalidOperationException("Scrollbar visibility flickered for " + scenario + ".");
                VerifyBarGeometry(content, scrollBar, expectScrollBar, scenario);
            }

            if (expectScrollBar)
            {
                VerifyPointerTarget(scrollBar, scenario);
                DragThumbToBottom(scrollBar);
                PumpMessages();
                if (content.AutoScrollPosition.Y >= 0)
                    throw new InvalidOperationException("Dragging the themed scrollbar did not move overflowing content.");

                content.AutoScrollPosition = Point.Empty;
                scrollBar.Value = 0;
                InvokeMouse(scrollBar, "OnMouseWheel", new MouseEventArgs(MouseButtons.None, 0, 0, 0, -120));
                PumpMessages();
                if (content.AutoScrollPosition.Y >= 0)
                    throw new InvalidOperationException("The mouse wheel did not move overflowing Settings content.");
            }

            form.Controls.Remove(host);
            host.Dispose();
        }
    }

    private static bool HasNativeVerticalScrollStyle(Control control)
    {
        return control.IsHandleCreated
            && (GetWindowLong(control.Handle, GwlStyle) & WsVScroll) != 0;
    }

    private static void VerifyLogScenario(
        MethodInfo createHost,
        int lineCount,
        bool expectScrollBar,
        string scenario)
    {
        using (Form form = new Form
        {
            ClientSize = new Size(640, 420),
            FormBorderStyle = FormBorderStyle.FixedToolWindow,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Left = -32000,
            Top = -32000
        })
        using (DarkTextBox textBox = new DarkTextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.None,
            Text = string.Join(Environment.NewLine, Enumerable.Range(0, lineCount).Select(index => "Log line " + index)),
            WordWrap = true
        })
        {
            Control host = (Control)createHost.Invoke(null, new object[] { textBox });
            host.Dock = DockStyle.Fill;
            form.Controls.Add(host);
            form.Show();
            PumpMessages();

            DarkScrollBar scrollBar = FindScrollBar(host);
            if (scrollBar == null || scrollBar.Visible != expectScrollBar)
                throw new InvalidOperationException("Unexpected themed scrollbar visibility for " + scenario + ".");
            VerifyBarGeometry(textBox, scrollBar, expectScrollBar, scenario);

            for (int index = 0; index < 12; index++)
            {
                form.ClientSize = new Size(640 + (index % 2), 420);
                form.PerformLayout();
                PumpMessages();
                if (scrollBar.Visible != expectScrollBar)
                    throw new InvalidOperationException("Scrollbar visibility flickered for " + scenario + ".");
                VerifyBarGeometry(textBox, scrollBar, expectScrollBar, scenario);
            }

            if (expectScrollBar)
            {
                VerifyPointerTarget(scrollBar, scenario);
                DragThumbToBottom(scrollBar);
                PumpMessages();
                int firstVisibleLine = SendMessage(
                    textBox.Handle,
                    EmGetFirstVisibleLine,
                    IntPtr.Zero,
                    IntPtr.Zero).ToInt32();
                if (firstVisibleLine <= 0)
                    throw new InvalidOperationException("Dragging the themed log scrollbar did not move the visible log text.");

                scrollBar.Value = 0;
                PumpMessages();
                InvokeMouse(scrollBar, "OnMouseWheel", new MouseEventArgs(MouseButtons.None, 0, 0, 0, -120));
                PumpMessages();
                firstVisibleLine = SendMessage(
                    textBox.Handle,
                    EmGetFirstVisibleLine,
                    IntPtr.Zero,
                    IntPtr.Zero).ToInt32();
                if (firstVisibleLine <= 0)
                    throw new InvalidOperationException("The mouse wheel did not move the visible Settings log text.");
            }

            form.Controls.Remove(host);
            host.Dispose();
        }
    }

    private static void VerifyBarGeometry(
        Control content,
        DarkScrollBar scrollBar,
        bool expectScrollBar,
        string scenario)
    {
        if (expectScrollBar && content.Right > scrollBar.Left)
        {
            throw new InvalidOperationException(
                "The themed scrollbar overlaps the content for " + scenario
                + ", allowing a native scrollbar or border to flicker beside it.");
        }
        if (!expectScrollBar
            && scrollBar.Parent != null
            && content.Right != scrollBar.Parent.ClientRectangle.Right)
        {
            throw new InvalidOperationException(
                "A hidden scrollbar still reserves empty space for " + scenario + ".");
        }
    }

    private static FlowLayoutPanel CreateContent(int itemCount)
    {
        FlowLayoutPanel content = new FlowLayoutPanel
        {
            AutoScroll = true,
            BackColor = Color.FromArgb(28, 32, 40),
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(14),
            WrapContents = false
        };
        for (int index = 0; index < itemCount; index++)
        {
            content.Controls.Add(new Label
            {
                AutoSize = false,
                Height = 30,
                Margin = new Padding(0, 0, 0, 8),
                Text = "Setting " + index,
                Width = 480
            });
        }
        return content;
    }

    private static DarkScrollBar FindScrollBar(Control root)
    {
        foreach (Control control in root.Controls)
        {
            DarkScrollBar scrollBar = control as DarkScrollBar;
            if (scrollBar != null)
                return scrollBar;
            scrollBar = FindScrollBar(control);
            if (scrollBar != null)
                return scrollBar;
        }
        return null;
    }

    private static T FindControl<T>(Control root, string accessibleName = null)
        where T : Control
    {
        foreach (Control control in root.Controls)
        {
            T match = control as T;
            if (match != null
                && (accessibleName == null
                    || string.Equals(match.AccessibleName, accessibleName, StringComparison.Ordinal)))
            {
                return match;
            }

            match = FindControl<T>(control, accessibleName);
            if (match != null)
                return match;
        }
        return null;
    }

    private static void VerifyPointerTarget(DarkScrollBar scrollBar, string scenario)
    {
        Control hit = scrollBar.Parent.GetChildAtPoint(new Point(
            scrollBar.Left + (scrollBar.Width / 2),
            scrollBar.Top + (scrollBar.Height / 2)));
        if (!ReferenceEquals(hit, scrollBar))
            throw new InvalidOperationException("The themed scrollbar cannot receive pointer input for " + scenario + ".");
    }

    private static void DragThumbToBottom(DarkScrollBar scrollBar)
    {
        FieldInfo thumbField = typeof(DarkScrollBar).GetField(
            "_thumbArea",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (thumbField == null)
            throw new InvalidOperationException("Dark scrollbar thumb geometry is unavailable.");
        Rectangle thumb = (Rectangle)thumbField.GetValue(scrollBar);
        if (thumb.IsEmpty)
            throw new InvalidOperationException("Dark scrollbar thumb geometry is empty.");

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
        if (method == null)
            throw new InvalidOperationException("Dark scrollbar mouse handler is unavailable: " + methodName);
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
