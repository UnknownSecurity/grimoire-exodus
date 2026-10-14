using DarkUI.Forms;
using Grimoire.UI;
using Grimoire.Utils;
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
            try
            {
                StartupDiagnostics.StartSession();
                StartupDiagnostics.SetStage("Runtime");
                flashVersion = FlashVersion();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(defaultValue: false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (sender, args) => ReportUnhandledException(args.Exception, "Windows Forms", false);
                TaskScheduler.UnobservedTaskException += (sender, args) =>
                {
                    ReportUnhandledException(args.Exception, "Background task", false);
                    args.SetObserved();
                };
                AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                    ReportUnhandledException(args.ExceptionObject as Exception, "Application domain", args.IsTerminating);

                StartupDiagnostics.SetStage("UI construction");
                Application.Run(new Root());
                StartupDiagnostics.MarkCleanShutdown();
            }
            catch (Exception exception)
            {
                ReportUnhandledException(exception, "Main", true);
                try
                {
                    DarkMessageBox.Show(
                        new Form { TopMost = true, StartPosition = FormStartPosition.CenterScreen },
                        "Grimoire Exodus could not finish starting. A diagnostic report was saved to:\r\n\r\n"
                            + AppPaths.CrashReportsDirectory,
                        "Grimoire Exodus startup error",
                        MessageBoxIcon.Error);
                }
                catch
                {
                }
            }
        }

        private static void ReportUnhandledException(Exception exception, string context, bool isTerminating)
        {
            if (exception == null)
                return;

            Debug.WriteLine(exception);
            StartupDiagnostics.RecordException(context, exception, isTerminating);
            try
            {
                if (!isTerminating)
                {
                    LogForm.Instance.AppendDebug($"[{DateTime.Now:hh:mm:ss}] Unhandled error: {exception.Message}\r\n");
                }
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
