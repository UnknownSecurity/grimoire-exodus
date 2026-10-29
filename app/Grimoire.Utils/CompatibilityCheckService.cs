using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Grimoire.Utils
{
    /// <summary>
    /// Runs bounded, privacy-safe compatibility checks for Grimoire Home.
    /// Results never include account data, bot details, logs, or local paths.
    /// </summary>
    internal sealed class CompatibilityCheckService
    {
        private const int MaximumStatuses = 4;
        private const int DotNetFramework48Release = 528040;
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(8);
        private static readonly Uri AqwEndpoint =
            new Uri("https://game.aq.com/game/api/data/gameversion", UriKind.Absolute);
        private static readonly Uri GitHubEndpoint =
            new Uri("https://api.github.com/repos/UnknownSecurity/grimoire-exodus/releases?per_page=1", UriKind.Absolute);
        private static readonly HttpClient Client = CreateClient();
        private static readonly string[] RequiredApplicationFiles =
        {
            "Grimoire.Updater.exe",
            "Flash.ocx",
            "grimoire-exodus.swf",
            "AxInterop.ShockwaveFlashObjects.dll",
            "Interop.ShockwaveFlashObjects.dll",
            "DarkUI.dll"
        };

        private readonly object statusLock = new object();
        private readonly Dictionary<string, CompatibilityCheckOperation> statuses =
            new Dictionary<string, CompatibilityCheckOperation>(StringComparer.Ordinal);
        private readonly Queue<string> statusOrder = new Queue<string>();
        private string activeRequestId;

        public CompatibilityCheckOperation Begin(CompatibilityCheckContext context)
        {
            CompatibilityCheckContext normalized;
            string error;
            if (!TryNormalizeContext(context, out normalized, out error))
                return Failed(error);

            CompatibilityCheckOperation pending;
            lock (statusLock)
            {
                CompatibilityCheckOperation active;
                if (!string.IsNullOrWhiteSpace(activeRequestId)
                    && statuses.TryGetValue(activeRequestId, out active)
                    && !active.Completed)
                {
                    return Clone(active);
                }

                string requestId = Guid.NewGuid().ToString("N");
                pending = new CompatibilityCheckOperation
                {
                    RequestId = requestId,
                    Success = true,
                    Completed = false,
                    Message = "Running compatibility checks...",
                    Checks = CreatePendingChecks()
                };
                activeRequestId = requestId;
                StoreUnsafe(pending);
            }

            _ = Task.Run(() => RunAsync(pending.RequestId, normalized));
            return Clone(pending);
        }

        public CompatibilityCheckOperation GetStatus(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId) || requestId.Length > 64)
                return Failed("The checkup status request was invalid. Run the checkup again.");

            lock (statusLock)
            {
                CompatibilityCheckOperation status;
                return statuses.TryGetValue(requestId, out status)
                    ? Clone(status)
                    : Failed("The checkup result expired. Run the checkup again.");
            }
        }

        internal static List<CompatibilityCheckResult> RunLocalChecks(
            CompatibilityCheckContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            List<CompatibilityCheckResult> checks = new List<CompatibilityCheckResult>
            {
                CheckWindows(),
                CheckDotNetFramework(),
                CheckProcessArchitecture(),
                CheckRequiredFiles(context.ApplicationDirectory),
                CheckFlashPrerequisites(context.ApplicationDirectory),
                CheckWritableDirectories(context),
                CheckResourceHeadroom(context),
                CheckClientInitialization(context),
                CheckExperimentalSettings(context)
            };
            return checks;
        }

        internal static List<CompatibilityCheckResult> CreatePendingChecks()
        {
            return new List<CompatibilityCheckResult>
            {
                Pending("windows", "Windows compatibility"),
                Pending("dotnet", ".NET Framework 4.8"),
                Pending("architecture", "32-bit runtime"),
                Pending("required-files", "Required application files"),
                Pending("flash-runtime", "Bundled Flash runtime"),
                Pending("writable-data", "Writable app data"),
                Pending("headroom", "Storage and memory headroom"),
                Pending("aqw-network", "AQW service reachability"),
                Pending("github-network", "GitHub update reachability"),
                Pending("client-initialization", "AQW client initialization"),
                Pending("experimental-settings", "Experimental settings"),
            };
        }

        internal static string BuildReport(
            string applicationVersion,
            IEnumerable<CompatibilityCheckResult> checks)
        {
            List<CompatibilityCheckResult> results = checks == null
                ? new List<CompatibilityCheckResult>()
                : checks.Where(check => check != null).ToList();
            int passed = results.Count(check => check.Status == CompatibilityCheckStatus.Pass);
            int warnings = results.Count(check => check.Status == CompatibilityCheckStatus.Warning);
            int failed = results.Count(check => check.Status == CompatibilityCheckStatus.Fail);

            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.AppendLine("Grimoire Doctor compatibility report");
            report.AppendLine("Generated (UTC): " + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            report.AppendLine("Application version: " + NormalizeVersion(applicationVersion));
            report.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "Summary: {0} passed, {1} warning(s), {2} issue(s)",
                passed,
                warnings,
                failed));
            report.AppendLine("Privacy note: no accounts, credentials, bot details, logs, packets, room names, or local paths are included.");
            report.AppendLine();

            foreach (CompatibilityCheckResult check in results)
            {
                report.Append('[')
                    .Append((check.Status ?? CompatibilityCheckStatus.Pending).ToUpperInvariant())
                    .Append("] ")
                    .AppendLine(check.Title ?? "Compatibility check");
                report.AppendLine(check.Message ?? "No additional information.");
                report.AppendLine();
            }

            return report.ToString().TrimEnd();
        }

        private async Task RunAsync(string requestId, CompatibilityCheckContext context)
        {
            CompatibilityCheckOperation completed;
            try
            {
                List<CompatibilityCheckResult> localChecks = RunLocalChecks(context);
                Task<CompatibilityCheckResult> aqwCheck = CheckEndpointAsync(
                    AqwEndpoint,
                    "aqw-network",
                    "AQW service reachability",
                    true);
                Task<CompatibilityCheckResult> githubCheck = CheckEndpointAsync(
                    GitHubEndpoint,
                    "github-network",
                    "GitHub update reachability",
                    false);
                CompatibilityCheckResult[] networkChecks = await Task.WhenAll(aqwCheck, githubCheck)
                    .ConfigureAwait(false);

                List<CompatibilityCheckResult> checks = new List<CompatibilityCheckResult>();
                checks.AddRange(localChecks.Take(7));
                checks.AddRange(networkChecks);
                checks.AddRange(localChecks.Skip(7));

                int passed = checks.Count(check => check.Status == CompatibilityCheckStatus.Pass);
                int warnings = checks.Count(check => check.Status == CompatibilityCheckStatus.Warning);
                int failed = checks.Count(check => check.Status == CompatibilityCheckStatus.Fail);
                string message = failed > 0
                    ? "Checkup finished with compatibility issues that need attention."
                    : warnings > 0
                        ? "Checkup finished. Review the warnings below."
                        : "Checkup finished. This device passed every compatibility check.";

                completed = new CompatibilityCheckOperation
                {
                    RequestId = requestId,
                    Success = true,
                    Completed = true,
                    Message = message,
                    Passed = passed,
                    Warnings = warnings,
                    Failed = failed,
                    Checks = checks,
                    Report = BuildReport(context.ApplicationVersion, checks)
                };
            }
            catch
            {
                completed = Failed("The checkup could not finish. Restart Grimoire and try again.");
                completed.RequestId = requestId;
            }

            lock (statusLock)
            {
                statuses[requestId] = completed;
                if (string.Equals(activeRequestId, requestId, StringComparison.Ordinal))
                    activeRequestId = null;
            }
        }

        private static CompatibilityCheckResult CheckWindows()
        {
            OperatingSystem operatingSystem = Environment.OSVersion;
            if (operatingSystem.Platform != PlatformID.Win32NT)
            {
                return Fail(
                    "windows",
                    "Windows compatibility",
                    "Grimoire Exodus requires Windows. Use the official Windows build on Windows 10 or 11.");
            }

            if (operatingSystem.Version.Major >= 10)
            {
                return Pass(
                    "windows",
                    "Windows compatibility",
                    "A supported modern Windows version is available.");
            }

            if (operatingSystem.Version.Major > 6
                || (operatingSystem.Version.Major == 6 && operatingSystem.Version.Minor >= 1))
            {
                return Warning(
                    "windows",
                    "Windows compatibility",
                    "This older Windows version may work, but Windows 10 or 11 is recommended for current security and TLS support.");
            }

            return Fail(
                "windows",
                "Windows compatibility",
                "This Windows version is too old. Upgrade to Windows 10 or 11.");
        }

        private static CompatibilityCheckResult CheckDotNetFramework()
        {
            int release = ReadDotNetFrameworkRelease();
            if (release >= DotNetFramework48Release)
            {
                return Pass(
                    "dotnet",
                    ".NET Framework 4.8",
                    ".NET Framework 4.8 or newer is installed.");
            }

            if (release <= 0)
            {
                return Warning(
                    "dotnet",
                    ".NET Framework 4.8",
                    "The installed .NET Framework version could not be verified. Repair or install .NET Framework 4.8 if Grimoire does not start correctly.");
            }

            return Fail(
                "dotnet",
                ".NET Framework 4.8",
                ".NET Framework 4.8 is missing. Install it from Microsoft, then restart Windows.");
        }

        private static int ReadDotNetFrameworkRelease()
        {
            RegistryView[] views = Environment.Is64BitOperatingSystem
                ? new[] { RegistryView.Registry32, RegistryView.Registry64 }
                : new[] { RegistryView.Registry32 };

            foreach (RegistryView view in views)
            {
                try
                {
                    using (RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (RegistryKey full = localMachine.OpenSubKey(
                        @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full",
                        false))
                    {
                        object value = full == null ? null : full.GetValue("Release");
                        if (value != null && int.TryParse(
                            Convert.ToString(value, CultureInfo.InvariantCulture),
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out int release))
                        {
                            return release;
                        }
                    }
                }
                catch
                {
                    // Try the other registry view before reporting an unknown result.
                }
            }

            return 0;
        }

        private static CompatibilityCheckResult CheckProcessArchitecture()
        {
            return Environment.Is64BitProcess
                ? Fail(
                    "architecture",
                    "32-bit runtime",
                    "Grimoire is running as a 64-bit process, which cannot host the bundled Flash ActiveX control. Reinstall the official x86 build.")
                : Pass(
                    "architecture",
                    "32-bit runtime",
                    "Grimoire is running with the required 32-bit process architecture.");
        }

        private static CompatibilityCheckResult CheckRequiredFiles(string applicationDirectory)
        {
            List<string> missing = RequiredApplicationFiles
                .Where(name => !File.Exists(Path.Combine(applicationDirectory, name)))
                .ToList();
            if (missing.Count == 0)
            {
                return Pass(
                    "required-files",
                    "Required application files",
                    "The updater, Flash runtime, SWF bridge, and required interop files are present.");
            }

            return Fail(
                "required-files",
                "Required application files",
                "Missing " + string.Join(", ", missing) + ". Reinstall from the official installer or portable ZIP without mixing versions.");
        }

        private static CompatibilityCheckResult CheckFlashPrerequisites(string applicationDirectory)
        {
            string[] flashFiles =
            {
                "Flash.ocx",
                "grimoire-exodus.swf",
                "AxInterop.ShockwaveFlashObjects.dll",
                "Interop.ShockwaveFlashObjects.dll"
            };
            bool filesPresent = flashFiles.All(name => File.Exists(Path.Combine(applicationDirectory, name)));
            if (Environment.Is64BitProcess || !filesPresent)
            {
                return Fail(
                    "flash-runtime",
                    "Bundled Flash runtime",
                    "The app-local Flash prerequisites are incomplete. Reinstall the official build; do not register a separate system-wide Flash copy.");
            }

            return Pass(
                "flash-runtime",
                "Bundled Flash runtime",
                "The private app-local Flash prerequisites are ready and do not require Clean Flash.");
        }

        private static CompatibilityCheckResult CheckWritableDirectories(CompatibilityCheckContext context)
        {
            string[] directories =
            {
                context.DataDirectory,
                context.LogsDirectory,
                context.UpdatesDirectory
            };

            int failures = 0;
            foreach (string directory in directories.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                string probePath = null;
                try
                {
                    Directory.CreateDirectory(directory);
                    probePath = Path.Combine(directory, ".grimoire-doctor-" + Guid.NewGuid().ToString("N") + ".tmp");
                    using (FileStream stream = new FileStream(
                        probePath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None,
                        1,
                        FileOptions.WriteThrough))
                    {
                        stream.WriteByte(1);
                    }
                }
                catch
                {
                    failures++;
                }
                finally
                {
                    if (!string.IsNullOrWhiteSpace(probePath))
                    {
                        try
                        {
                            File.Delete(probePath);
                        }
                        catch
                        {
                            failures++;
                        }
                    }
                }
            }

            return failures == 0
                ? Pass(
                    "writable-data",
                    "Writable app data",
                    "Grimoire can write settings, logs, and update data in its private app-data folders.")
                : Fail(
                    "writable-data",
                    "Writable app data",
                    "Grimoire could not write to one or more private data folders. Check security software and Windows folder permissions.");
        }

        private static CompatibilityCheckResult CheckResourceHeadroom(CompatibilityCheckContext context)
        {
            long availableBytes = -1L;
            try
            {
                string root = Path.GetPathRoot(context.DataDirectory);
                if (!string.IsNullOrWhiteSpace(root))
                    availableBytes = new DriveInfo(root).AvailableFreeSpace;
            }
            catch
            {
                // The result below explains that storage could not be verified.
            }

            if (availableBytes >= 0L && availableBytes < 128L * 1024L * 1024L)
            {
                return Fail(
                    "headroom",
                    "Storage and memory headroom",
                    "Very little storage is available for settings and updates. Free at least 512 MB before continuing.");
            }

            if ((availableBytes >= 0L && availableBytes < 512L * 1024L * 1024L)
                || context.WorkingSetBytes >= 1700L * 1024L * 1024L)
            {
                return Warning(
                    "headroom",
                    "Storage and memory headroom",
                    "Resource headroom is low. Close unused clients or free storage before starting a long bot session.");
            }

            if (context.WorkingSetBytes >= 1200L * 1024L * 1024L)
            {
                return Warning(
                    "headroom",
                    "Storage and memory headroom",
                    "The 32-bit process is using substantial memory. Close unused clients to reduce the chance of a stall.");
            }

            if (availableBytes < 0L)
            {
                return Warning(
                    "headroom",
                    "Storage and memory headroom",
                    "Available storage could not be verified. Make sure the system drive has free space for updates and diagnostics.");
            }

            return Pass(
                "headroom",
                "Storage and memory headroom",
                "Storage and 32-bit process memory headroom look healthy.");
        }

        private static CompatibilityCheckResult CheckClientInitialization(CompatibilityCheckContext context)
        {
            if (string.Equals(context.ClientState, "Faulted", StringComparison.OrdinalIgnoreCase))
            {
                return Fail(
                    "client-initialization",
                    "AQW client initialization",
                    "An AQW client failed to initialize. Review the failed checks above, then restart Grimoire after correcting them.");
            }

            if (context.GameReady)
            {
                return Pass(
                    "client-initialization",
                    "AQW client initialization",
                    "The selected AQW client has initialized successfully.");
            }

            if (context.GameLoadProgress > 0)
            {
                return Warning(
                    "client-initialization",
                    "AQW client initialization",
                    "The selected AQW client was still loading at " + context.GameLoadProgress.ToString(CultureInfo.InvariantCulture) + "% when the checkup began. Wait for it to finish before retrying login.");
            }

            if (context.OpenClientCount <= 0)
            {
                return Warning(
                    "client-initialization",
                    "AQW client initialization",
                    "No AQW client is currently open. Start a client after the other compatibility checks pass.");
            }

            return Warning(
                "client-initialization",
                "AQW client initialization",
                "The selected AQW client had not reported load progress when the checkup began. Check AQW reachability and the bundled Flash files above.");
        }

        private static CompatibilityCheckResult CheckExperimentalSettings(CompatibilityCheckContext context)
        {
            List<string> enabled = new List<string>();
            if (context.MultiClientEnabled)
                enabled.Add("multi-client mode");
            if (string.Equals(context.GameEngine, "Ruffle", StringComparison.OrdinalIgnoreCase))
            {
                enabled.Add("Ruffle engine");
                if (!string.IsNullOrWhiteSpace(context.GameEngineAvailabilityError))
                {
                    return Warning(
                        "experimental-settings",
                        "Experimental settings",
                        "Ruffle is selected but unavailable: " + context.GameEngineAvailabilityError + " Grimoire will fall back to Flash.ocx for new clients.");
                }
            }
            if (string.Equals(context.RenderingProfile, "Gpu", StringComparison.OrdinalIgnoreCase))
                enabled.Add("GPU presentation");
            else if (string.Equals(context.RenderingProfile, "Direct", StringComparison.OrdinalIgnoreCase))
                enabled.Add("Direct presentation");

            if (enabled.Count == 0)
            {
                return Pass(
                    "experimental-settings",
                    "Experimental settings",
                    "Compatibility-sensitive experimental display and multi-client options are not enabled.");
            }

            return Warning(
                "experimental-settings",
                "Experimental settings",
                "Enabled: " + string.Join(" and ", enabled) + ". If the client stalls or renders incorrectly, switch to Automatic rendering and test with one client.");
        }

        private static async Task<CompatibilityCheckResult> CheckEndpointAsync(
            Uri endpoint,
            string id,
            string title,
            bool requiredForGame)
        {
            try
            {
                using (CancellationTokenSource timeout = new CancellationTokenSource(RequestTimeout))
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, endpoint))
                {
                    if (string.Equals(endpoint.Host, "api.github.com", StringComparison.OrdinalIgnoreCase))
                    {
                        request.Headers.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                    }

                    using (HttpResponseMessage response = await Client.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        timeout.Token).ConfigureAwait(false))
                    {
                        int statusCode = (int)response.StatusCode;
                        if (statusCode >= 200 && statusCode < 400)
                        {
                            return Pass(
                                id,
                                title,
                                requiredForGame
                                    ? "The official AQW game service is reachable."
                                    : "The official GitHub release service is reachable for update checks.");
                        }
                    }
                }
            }
            catch
            {
                // The normalized result below intentionally omits raw network errors.
            }

            return requiredForGame
                ? Fail(
                    id,
                    title,
                    "The official AQW game service could not be reached. Check the connection, firewall, proxy, DNS, and system clock.")
                : Warning(
                    id,
                    title,
                    "GitHub could not be reached. Automatic updates and in-app issue reporting may be unavailable until the connection is restored.");
        }

        private static bool TryNormalizeContext(
            CompatibilityCheckContext context,
            out CompatibilityCheckContext normalized,
            out string error)
        {
            normalized = null;
            error = null;
            if (context == null)
            {
                error = "The checkup could not read the current application state.";
                return false;
            }

            try
            {
                string applicationDirectory = NormalizeDirectory(context.ApplicationDirectory);
                string dataDirectory = NormalizeDirectory(context.DataDirectory);
                string logsDirectory = NormalizeDirectory(context.LogsDirectory);
                string updatesDirectory = NormalizeDirectory(context.UpdatesDirectory);
                if (string.IsNullOrWhiteSpace(applicationDirectory)
                    || string.IsNullOrWhiteSpace(dataDirectory)
                    || string.IsNullOrWhiteSpace(logsDirectory)
                    || string.IsNullOrWhiteSpace(updatesDirectory))
                {
                    error = "The checkup could not resolve the application data folders.";
                    return false;
                }

                normalized = new CompatibilityCheckContext
                {
                    ApplicationDirectory = applicationDirectory,
                    DataDirectory = dataDirectory,
                    LogsDirectory = logsDirectory,
                    UpdatesDirectory = updatesDirectory,
                    ApplicationVersion = NormalizeVersion(context.ApplicationVersion),
                    ClientState = NormalizeInline(context.ClientState, 32),
                    GameReady = context.GameReady,
                    GameLoadProgress = Math.Max(0, Math.Min(100, context.GameLoadProgress)),
                    OpenClientCount = Math.Max(0, context.OpenClientCount),
                    MultiClientEnabled = context.MultiClientEnabled,
                    GameEngine = NormalizeInline(context.GameEngine, 32),
                    GameEngineAvailabilityError = NormalizeInline(context.GameEngineAvailabilityError, 160),
                    RenderingProfile = NormalizeInline(context.RenderingProfile, 32),
                    WorkingSetBytes = Math.Max(0L, context.WorkingSetBytes)
                };
                return true;
            }
            catch
            {
                error = "The checkup could not resolve the application data folders.";
                return false;
            }
        }

        private static string NormalizeDirectory(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? null
                : Path.GetFullPath(path)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static string NormalizeVersion(string value)
        {
            string normalized = NormalizeInline(value, 40);
            return string.IsNullOrWhiteSpace(normalized) ? "Unknown" : normalized;
        }

        private static string NormalizeInline(string value, int maximumLength)
        {
            string normalized = (value ?? string.Empty)
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();
            return normalized.Length <= maximumLength
                ? normalized
                : normalized.Substring(0, maximumLength);
        }

        private void StoreUnsafe(CompatibilityCheckOperation operation)
        {
            statuses[operation.RequestId] = Clone(operation);
            statusOrder.Enqueue(operation.RequestId);
            while (statusOrder.Count > MaximumStatuses)
            {
                string oldest = statusOrder.Dequeue();
                if (!string.Equals(oldest, activeRequestId, StringComparison.Ordinal))
                    statuses.Remove(oldest);
            }
        }

        private static CompatibilityCheckOperation Clone(CompatibilityCheckOperation operation)
        {
            return new CompatibilityCheckOperation
            {
                RequestId = operation.RequestId,
                Success = operation.Success,
                Completed = operation.Completed,
                Message = operation.Message,
                Passed = operation.Passed,
                Warnings = operation.Warnings,
                Failed = operation.Failed,
                Report = operation.Report,
                Checks = operation.Checks == null
                    ? new List<CompatibilityCheckResult>()
                    : operation.Checks.Select(Clone).ToList()
            };
        }

        private static CompatibilityCheckResult Clone(CompatibilityCheckResult result)
        {
            return new CompatibilityCheckResult
            {
                Id = result.Id,
                Title = result.Title,
                Status = result.Status,
                Message = result.Message
            };
        }

        private static CompatibilityCheckOperation Failed(string message)
        {
            return new CompatibilityCheckOperation
            {
                Success = false,
                Completed = true,
                Message = string.IsNullOrWhiteSpace(message)
                    ? "The compatibility check could not run."
                    : message,
                Checks = new List<CompatibilityCheckResult>()
            };
        }

        private static CompatibilityCheckResult Pending(string id, string title)
        {
            return Result(id, title, CompatibilityCheckStatus.Pending, "Waiting to be checked.");
        }

        private static CompatibilityCheckResult Pass(string id, string title, string message)
        {
            return Result(id, title, CompatibilityCheckStatus.Pass, message);
        }

        private static CompatibilityCheckResult Warning(string id, string title, string message)
        {
            return Result(id, title, CompatibilityCheckStatus.Warning, message);
        }

        private static CompatibilityCheckResult Fail(string id, string title, string message)
        {
            return Result(id, title, CompatibilityCheckStatus.Fail, message);
        }

        private static CompatibilityCheckResult Result(
            string id,
            string title,
            string status,
            string message)
        {
            return new CompatibilityCheckResult
            {
                Id = id,
                Title = title,
                Status = status,
                Message = message
            };
        }

        private static HttpClient CreateClient()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            HttpClient client = new HttpClient
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Grimoire-Exodus-Doctor/" + typeof(CompatibilityCheckService).Assembly.GetName().Version);
            return client;
        }
    }

    internal static class CompatibilityCheckStatus
    {
        public const string Pending = "pending";
        public const string Pass = "pass";
        public const string Warning = "warning";
        public const string Fail = "fail";
    }

    internal sealed class CompatibilityCheckContext
    {
        public string ApplicationDirectory { get; set; }

        public string DataDirectory { get; set; }

        public string LogsDirectory { get; set; }

        public string UpdatesDirectory { get; set; }

        public string ApplicationVersion { get; set; }

        public string ClientState { get; set; }

        public bool GameReady { get; set; }

        public int GameLoadProgress { get; set; }

        public int OpenClientCount { get; set; }

        public bool MultiClientEnabled { get; set; }

        public string GameEngine { get; set; }

        public string GameEngineAvailabilityError { get; set; }

        public string RenderingProfile { get; set; }

        public long WorkingSetBytes { get; set; }
    }

    internal sealed class CompatibilityCheckOperation
    {
        public string RequestId { get; set; }

        public bool Success { get; set; }

        public bool Completed { get; set; }

        public string Message { get; set; }

        public int Passed { get; set; }

        public int Warnings { get; set; }

        public int Failed { get; set; }

        public List<CompatibilityCheckResult> Checks { get; set; }

        public string Report { get; set; }
    }

    internal sealed class CompatibilityCheckResult
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public string Status { get; set; }

        public string Message { get; set; }
    }
}
