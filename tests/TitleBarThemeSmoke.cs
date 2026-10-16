using DarkUI.Forms;
using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class TitleBarThemeSmoke
{
    private const int DwmUseImmersiveDarkMode = 20;
    private const int DwmCaptionColor = 35;
    private const int ExpectedDarkCaptionColor = 0x28201C;
    private const uint WindowMessageClose = 0x0010;
    private static Assembly clientAssembly;

    [DllImport("dwmapi.dll", CallingConvention = CallingConvention.Winapi, PreserveSig = true)]
    private static extern int DwmGetWindowAttribute(
        IntPtr windowHandle,
        int attribute,
        out int attributeValue,
        int attributeSize);

    [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi)]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr windowHandle, uint message, IntPtr wordParameter, IntPtr longParameter);

    [STAThread]
    private static int Main()
    {
        try
        {
            string clientPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Grimoire Exodus.exe");
            clientAssembly = Assembly.LoadFrom(clientPath);
            AppDomain.CurrentDomain.AssemblyResolve += ResolveClientAssembly;
            Type themedForm = clientAssembly.GetType("Grimoire.UI.GrimoireForm", true);
            MethodInfo initialize = themedForm.GetMethod(
                "InitializeNativeTitleBarTheming",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (initialize != null)
            {
                initialize.Invoke(null, null);
            }

            int result = TestDarkUiAlert();
            if (result != 0)
            {
                return result;
            }

            return TestNativeAlert();
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

    private static int TestDarkUiAlert()
    {
        int result = 1;
        using (Form owner = CreateOffScreenOwner())
        using (Timer inspectionTimer = new Timer())
        {
            owner.Show();

            inspectionTimer.Interval = 250;
            inspectionTimer.Tick += delegate
            {
                Form dialog = null;
                foreach (Form openForm in Application.OpenForms)
                {
                    if (openForm.GetType().Assembly == typeof(DarkMessageBox).Assembly)
                    {
                        dialog = openForm;
                        break;
                    }
                }

                if (dialog == null)
                {
                    return;
                }

                inspectionTimer.Stop();
                if (HasDarkTitleBar(dialog.Handle))
                {
                    Console.WriteLine("PASS: DarkUI alert title bar follows the dark Grimoire theme.");
                    result = 0;
                }
                else
                {
                    Console.Error.WriteLine("DarkUI alert retained the system light title bar.");
                    result = 3;
                }

                dialog.Close();
            };
            inspectionTimer.Start();
            DarkMessageBox.Show(
                owner,
                "This alert is opened only by the automated title-bar smoke test.",
                "Grimoire Exodus title-bar test",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            owner.Close();
        }

        return result;
    }

    private static int TestNativeAlert()
    {
        const string title = "Grimoire Exodus native title-bar test";
        int result = 1;
        using (Form owner = CreateOffScreenOwner())
        using (Timer inspectionTimer = new Timer())
        {
            owner.Show();

            inspectionTimer.Interval = 250;
            inspectionTimer.Tick += delegate
            {
                IntPtr dialogHandle = GetActiveWindow();
                if (dialogHandle == IntPtr.Zero || dialogHandle == owner.Handle)
                {
                    return;
                }

                inspectionTimer.Stop();
                if (HasDarkTitleBar(dialogHandle))
                {
                    Console.WriteLine("PASS: native alert title bar follows the dark Grimoire theme.");
                    result = 0;
                }
                else
                {
                    Console.Error.WriteLine("Native alert retained the system light title bar.");
                    result = 4;
                }

                PostMessage(dialogHandle, WindowMessageClose, IntPtr.Zero, IntPtr.Zero);
            };
            inspectionTimer.Start();
            MessageBox.Show(
                owner,
                "This alert is opened only by the automated title-bar smoke test.",
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            owner.Close();
        }

        return result;
    }

    private static Form CreateOffScreenOwner()
    {
        return new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-32000, -32000),
            Opacity = 0.01d,
            ShowInTaskbar = false
        };
    }

    private static bool HasDarkTitleBar(IntPtr windowHandle)
    {
        int captionColor;
        int captionResult = DwmGetWindowAttribute(
            windowHandle,
            DwmCaptionColor,
            out captionColor,
            sizeof(int));
        int immersiveDark;
        int darkModeResult = DwmGetWindowAttribute(
            windowHandle,
            DwmUseImmersiveDarkMode,
            out immersiveDark,
            sizeof(int));

        return (captionResult == 0 && captionColor == ExpectedDarkCaptionColor)
            || (darkModeResult == 0 && immersiveDark != 0);
    }
}
