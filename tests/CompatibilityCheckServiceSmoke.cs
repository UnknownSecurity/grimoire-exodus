using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Grimoire.Utils
{
    internal static class CompatibilityCheckServiceSmoke
    {
        private static readonly string[] RequiredFiles =
        {
            "Grimoire.Updater.exe",
            "Flash.ocx",
            "grimoire-exodus.swf",
            "AxInterop.ShockwaveFlashObjects.dll",
            "Interop.ShockwaveFlashObjects.dll",
            "DarkUI.dll"
        };

        public static int Main()
        {
            string root = Path.Combine(
                Path.GetTempPath(),
                "grimoire-compatibility-test-" + Guid.NewGuid().ToString("N"));
            string applicationDirectory = Path.Combine(root, "app");
            string dataDirectory = Path.Combine(root, "data");
            string logsDirectory = Path.Combine(dataDirectory, "Logs");
            string updatesDirectory = Path.Combine(dataDirectory, "Updates");

            try
            {
                Directory.CreateDirectory(applicationDirectory);
                Directory.CreateDirectory(dataDirectory);

                List<CompatibilityCheckResult> pending =
                    CompatibilityCheckService.CreatePendingChecks();
                Assert(pending.Count == 11, "The Doctor must expose every planned check while pending.");
                Assert(
                    pending.Select(check => check.Id).Distinct(StringComparer.Ordinal).Count() == pending.Count,
                    "Check identifiers must be unique.");

                CompatibilityCheckContext context = new CompatibilityCheckContext
                {
                    ApplicationDirectory = applicationDirectory,
                    DataDirectory = dataDirectory,
                    LogsDirectory = logsDirectory,
                    UpdatesDirectory = updatesDirectory,
                    ApplicationVersion = "1.4.0.0",
                    ClientState = "Faulted",
                    GameReady = false,
                    GameLoadProgress = 0,
                    OpenClientCount = 1,
                    MultiClientEnabled = true,
                    RenderingProfile = "Gpu",
                    WorkingSetBytes = 128L * 1024L * 1024L
                };

                List<CompatibilityCheckResult> failed =
                    CompatibilityCheckService.RunLocalChecks(context);
                AssertStatus(failed, "required-files", CompatibilityCheckStatus.Fail);
                AssertStatus(failed, "flash-runtime", CompatibilityCheckStatus.Fail);
                AssertStatus(failed, "writable-data", CompatibilityCheckStatus.Pass);
                AssertStatus(failed, "client-initialization", CompatibilityCheckStatus.Fail);
                AssertStatus(failed, "experimental-settings", CompatibilityCheckStatus.Warning);
                AssertNoProbeFiles(dataDirectory);

                string failedReport = CompatibilityCheckService.BuildReport(
                    context.ApplicationVersion,
                    failed);
                AssertPrivacySafe(failedReport, root);
                Assert(
                    failedReport.IndexOf("[FAIL] Required application files", StringComparison.Ordinal) >= 0,
                    "The copyable report must include failed checks.");

                foreach (string fileName in RequiredFiles)
                    File.WriteAllBytes(Path.Combine(applicationDirectory, fileName), new byte[] { 1 });

                context.ClientState = "Ready";
                context.GameReady = true;
                context.GameLoadProgress = 100;
                context.MultiClientEnabled = false;
                context.RenderingProfile = "Automatic";

                List<CompatibilityCheckResult> passing =
                    CompatibilityCheckService.RunLocalChecks(context);
                AssertStatus(passing, "required-files", CompatibilityCheckStatus.Pass);
                AssertStatus(passing, "flash-runtime", CompatibilityCheckStatus.Pass);
                AssertStatus(passing, "client-initialization", CompatibilityCheckStatus.Pass);
                AssertStatus(passing, "experimental-settings", CompatibilityCheckStatus.Pass);
                AssertNoProbeFiles(dataDirectory);
                AssertPrivacySafe(
                    CompatibilityCheckService.BuildReport(context.ApplicationVersion, passing),
                    root);

                Console.WriteLine("Compatibility check service validation passed.");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }

        private static void AssertStatus(
            IEnumerable<CompatibilityCheckResult> checks,
            string id,
            string expectedStatus)
        {
            CompatibilityCheckResult result = checks.FirstOrDefault(check => check.Id == id);
            Assert(result != null, "Missing check: " + id);
            Assert(
                string.Equals(result.Status, expectedStatus, StringComparison.Ordinal),
                id + " returned " + result.Status + " instead of " + expectedStatus + ".");
        }

        private static void AssertNoProbeFiles(string dataDirectory)
        {
            string[] probes = Directory.Exists(dataDirectory)
                ? Directory.GetFiles(dataDirectory, ".grimoire-doctor-*.tmp", SearchOption.AllDirectories)
                : new string[0];
            Assert(probes.Length == 0, "Writable-folder probes must always be removed.");
        }

        private static void AssertPrivacySafe(string report, string privateRoot)
        {
            Assert(!string.IsNullOrWhiteSpace(report), "The copyable report must not be empty.");
            Assert(
                report.IndexOf(privateRoot, StringComparison.OrdinalIgnoreCase) < 0,
                "The report exposed a local path.");
            Assert(
                string.IsNullOrWhiteSpace(Environment.UserName)
                    || report.IndexOf(Environment.UserName, StringComparison.OrdinalIgnoreCase) < 0,
                "The report exposed the local username.");
            Assert(
                string.IsNullOrWhiteSpace(Environment.MachineName)
                    || report.IndexOf(Environment.MachineName, StringComparison.OrdinalIgnoreCase) < 0,
                "The report exposed the machine name.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
