using DarkUI.Forms;
using Grimoire.UI;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;

namespace Grimoire
{
    internal static class Program
    {
        public static string versionDate = "2026-10-13";

        public static string flashVersion;

        [STAThread]
        private static void Main()
        {
            flashVersion = FlashVersion();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(defaultValue: false);
            Application.ThreadException += (sender, args) => ReportUnhandledException(args.Exception);
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                ReportUnhandledException(args.Exception);
                args.SetObserved();
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => ReportUnhandledException(args.ExceptionObject as Exception);

            Application.Run(new Root());
        }

        private static void ReportUnhandledException(Exception exception)
        {
            if (exception == null)
                return;

            Debug.WriteLine(exception);
            try
            {
                LogForm.Instance.AppendDebug($"[{DateTime.Now:hh:mm:ss}] Unhandled error: {exception.Message}\r\n");
            }
            catch
            {
            }
        }

        public static string FlashVersion()
        {
            var subKey = Registry.ClassesRoot.OpenSubKey(@"ShockwaveFlash.ShockwaveFlash\CurVer");
            if (subKey != null)
            {
                var value = subKey.GetValue(null) as String;
                return value;
            }
            return string.Empty;
        }

        public static void CheckVersion()
        {
            // The legacy release feed is archived and no longer represents this
            // maintained fork. Update checks are intentionally
            // disabled until Grimoire Exodus has its own release feed.
        }

        public static bool CheckFlashVersion()
        {
            if (flashVersion == string.Empty)
            {
                DarkMessageBox.Show(new Form { TopMost = true, StartPosition = FormStartPosition.CenterScreen }, "You may have to install the Adobe Flash Player plugin (for ActiveX) in order for the bot client to load the game.", $"Flash Player", MessageBoxIcon.Error);
                return true;
            }
            return false;
        }
    }
}
