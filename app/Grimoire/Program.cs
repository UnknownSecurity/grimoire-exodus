using DarkUI.Forms;
using Grimoire.UI;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace Grimoire
{
    internal static class Program
    {
        public static string versionDate = "2026-10-14";

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
            string bundledRuntime = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Flash.ocx");
            if (File.Exists(bundledRuntime))
            {
                try
                {
                    string version = FileVersionInfo.GetVersionInfo(bundledRuntime).FileVersion;
                    return string.IsNullOrWhiteSpace(version) ? "Bundled runtime" : $"Bundled {version.Replace(',', '.')}";
                }
                catch
                {
                    return "Bundled runtime";
                }
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
                DarkMessageBox.Show(new Form { TopMost = true, StartPosition = FormStartPosition.CenterScreen }, "The app-local Flash runtime is missing. Rebuild with '.\\exodus.ps1 build' or install Grimoire Exodus with the v1.1.0 installer. A separate Clean Flash installation is not required.", "Flash runtime missing", MessageBoxIcon.Error);
                return true;
            }
            return false;
        }
    }
}
