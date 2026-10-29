using DarkUI.Controls;
using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

internal static class SettingsScrollbarSmoke
{
    private const int GwlStyle = -16;
    private const int WsVScroll = 0x00200000;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
    private static extern int GetWindowLong(IntPtr window, int index);

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
                int target = Math.Max(scrollBar.Minimum, scrollBar.Maximum - scrollBar.ViewSize);
                scrollBar.Value = target;
                PumpMessages();
                if (target > 0 && content.AutoScrollPosition.Y >= 0)
                    throw new InvalidOperationException("The themed scrollbar did not move overflowing content.");
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
                scrollBar.Value = Math.Min(5, Math.Max(0, scrollBar.Maximum - scrollBar.ViewSize));
                PumpMessages();
                if (scrollBar.Value <= 0)
                    throw new InvalidOperationException("The themed log scrollbar did not move overflowing text.");
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

    private static void PumpMessages()
    {
        for (int index = 0; index < 8; index++)
        {
            Application.DoEvents();
            Thread.Sleep(2);
        }
    }
}
