using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace Grimoire.Utils
{
    /// <summary>
    /// Keeps startup/crash information outside bot files. Session files are
    /// intentionally per-process so multiple Grimoire instances cannot erase
    /// each other's recovery state.
    /// </summary>
    public static class StartupDiagnostics
    {
        private static readonly object SyncRoot = new object();
        private static SessionState currentSession;
        private static string currentSessionFile;
        private static string startupLogFile;
        private static bool sessionStarted;
        private static bool cleanShutdown;

        public static RecoveryInfo PreviousSession { get; private set; }

        public static string StartupLogFile
        {
            get
            {
                lock (SyncRoot)
                {
                    return startupLogFile;
                }
            }
        }

        public static void StartSession()
        {
            lock (SyncRoot)
            {
                if (sessionStarted)
                    return;

                try
                {
                    Directory.CreateDirectory(AppPaths.LogsDirectory);
                    Directory.CreateDirectory(AppPaths.CrashReportsDirectory);
                    Directory.CreateDirectory(AppPaths.RecoveryDirectory);

                    using (Process process = Process.GetCurrentProcess())
                    {
                        DateTime processStartedUtc = SafeGetProcessStartTimeUtc(process) ?? DateTime.UtcNow;
                        string sessionId = Guid.NewGuid().ToString("N");
                        startupLogFile = Path.Combine(
                            AppPaths.LogsDirectory,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "startup-{0:yyyyMMdd-HHmmss}-{1}.log",
                                DateTime.UtcNow,
                                process.Id));

                        AppendLogUnsafe("Starting Grimoire Exodus diagnostics.");
                        PreviousSession = FindAndCleanOrphanedSessionsUnsafe();
                        CleanupOldOwnedFilesUnsafe();

                        currentSession = new SessionState
                        {
                            SessionId = sessionId,
                            ProcessId = process.Id,
                            ExecutablePath = SafeGetExecutablePath(process),
                            ProcessStartedUtc = processStartedUtc,
                            StartedUtc = DateTime.UtcNow,
                            LastUpdatedUtc = DateTime.UtcNow,
                            LastStage = "Diagnostics",
                            LastBotPath = null,
                            BotWasRunning = false,
                            WasHiddenToTray = false
                        };
                        currentSessionFile = Path.Combine(
                            AppPaths.RecoveryDirectory,
                            "session-" + process.Id.ToString(CultureInfo.InvariantCulture) + "-" + sessionId + ".json");
                    }

                    sessionStarted = true;
                    WriteCurrentSessionUnsafe();
                    AppendLogUnsafe("Session marker created. Orphan cleanup does not terminate processes.");
                }
                catch (Exception exception)
                {
                    // Diagnostics must never prevent the client from starting.
                    sessionStarted = true;
                    currentSession = null;
                    Debug.WriteLine("Unable to start diagnostics: " + exception);
                }
            }
        }

        public static void SetStage(string stage)
        {
            if (string.IsNullOrWhiteSpace(stage))
                return;

            lock (SyncRoot)
            {
                EnsureStartedUnsafe();
                if (currentSession == null)
                    return;

                currentSession.LastStage = stage.Trim();
                currentSession.LastUpdatedUtc = DateTime.UtcNow;
                WriteCurrentSessionUnsafe();
                AppendLogUnsafe("Stage: " + currentSession.LastStage);
            }
        }

        public static void UpdateActivity(string botPath, bool botIsRunning, bool hiddenToTray)
        {
            lock (SyncRoot)
            {
                EnsureStartedUnsafe();
                if (currentSession == null)
                    return;

                if (!string.IsNullOrWhiteSpace(botPath))
                {
                    currentSession.LastBotPath = SafeFullPath(botPath);
                }
                currentSession.BotWasRunning = botIsRunning;
                currentSession.WasHiddenToTray = hiddenToTray;
                currentSession.LastUpdatedUtc = DateTime.UtcNow;
                WriteCurrentSessionUnsafe();
            }
        }

        public static void RecordException(string context, Exception exception, bool isTerminating)
        {
            if (exception == null)
                return;

            lock (SyncRoot)
            {
                try
                {
                    EnsureStartedUnsafe();
                    if (currentSession == null)
                    {
                        Debug.WriteLine(exception);
                        return;
                    }

                    string description = string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} exception in {1}:{2}{3}",
                        isTerminating ? "Terminating" : "Handled",
                        string.IsNullOrWhiteSpace(context) ? "Unknown" : context,
                        Environment.NewLine,
                        exception);
                    AppendLogUnsafe(description);

                    string reportPath = Path.Combine(
                        AppPaths.CrashReportsDirectory,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "crash-{0:yyyyMMdd-HHmmss-fff}-{1}.txt",
                            DateTime.UtcNow,
                            currentSession.ProcessId));
                    File.WriteAllText(reportPath, BuildCrashReport(context, exception, isTerminating), Encoding.UTF8);
                }
                catch (Exception diagnosticsException)
                {
                    Debug.WriteLine("Unable to record diagnostics: " + diagnosticsException);
                }
            }
        }

        public static void MarkCleanShutdown()
        {
            lock (SyncRoot)
            {
                if (!sessionStarted || currentSession == null || cleanShutdown)
                    return;

                cleanShutdown = true;
                AppendLogUnsafe("Clean shutdown completed.");
                TryDelete(currentSessionFile);
            }
        }

        public static void PrepareForFullReset()
        {
            lock (SyncRoot)
            {
                cleanShutdown = true;
                TryDelete(currentSessionFile);
                currentSession = null;
                currentSessionFile = null;
                startupLogFile = null;
                sessionStarted = true;
            }
        }

        private static void EnsureStartedUnsafe()
        {
            if (!sessionStarted)
            {
                StartSession();
            }
        }

        private static RecoveryInfo FindAndCleanOrphanedSessionsUnsafe()
        {
            RecoveryInfo newestRecovery = null;
            string[] sessionFiles;
            try
            {
                sessionFiles = Directory.GetFiles(AppPaths.RecoveryDirectory, "session-*.json");
            }
            catch (Exception exception)
            {
                AppendLogUnsafe("Unable to enumerate recovery markers: " + exception.Message);
                return null;
            }

            foreach (string sessionFile in sessionFiles)
            {
                SessionState state;
                try
                {
                    state = JsonConvert.DeserializeObject<SessionState>(File.ReadAllText(sessionFile));
                    if (state == null || state.ProcessId <= 0 || string.IsNullOrWhiteSpace(state.SessionId))
                    {
                        throw new InvalidDataException("The session marker is incomplete.");
                    }
                }
                catch (Exception exception)
                {
                    AppendLogUnsafe("Removed corrupt recovery marker '" + Path.GetFileName(sessionFile) + "': " + exception.Message);
                    TryDelete(sessionFile);
                    continue;
                }

                if (SessionStillRunning(state))
                {
                    AppendLogUnsafe("Kept live session marker for PID " + state.ProcessId.ToString(CultureInfo.InvariantCulture) + ".");
                    continue;
                }

                RecoveryInfo recovery = new RecoveryInfo
                {
                    StartedUtc = state.StartedUtc,
                    LastUpdatedUtc = state.LastUpdatedUtc,
                    LastStage = state.LastStage,
                    LastBotPath = state.LastBotPath,
                    BotWasRunning = state.BotWasRunning,
                    WasHiddenToTray = state.WasHiddenToTray
                };
                if (newestRecovery == null || recovery.LastUpdatedUtc > newestRecovery.LastUpdatedUtc)
                {
                    newestRecovery = recovery;
                }

                WriteRecoverySummaryUnsafe(state);
                TryDelete(sessionFile);
                AppendLogUnsafe("Cleaned stale session marker for PID " + state.ProcessId.ToString(CultureInfo.InvariantCulture) + ".");
            }

            return newestRecovery;
        }

        private static bool SessionStillRunning(SessionState state)
        {
            try
            {
                using (Process process = Process.GetProcessById(state.ProcessId))
                {
                    if (process.HasExited)
                        return false;

                    string executablePath = SafeGetExecutablePath(process);
                    DateTime? processStartedUtc = SafeGetProcessStartTimeUtc(process);

                    // If Windows does not permit inspecting a live process, keep
                    // its marker. Cleanup must prefer a harmless false positive.
                    if (string.IsNullOrWhiteSpace(executablePath) || !processStartedUtc.HasValue)
                        return true;

                    bool pathMatches = string.Equals(
                        SafeFullPath(executablePath),
                        SafeFullPath(state.ExecutablePath),
                        StringComparison.OrdinalIgnoreCase);
                    bool startMatches = Math.Abs((processStartedUtc.Value - state.ProcessStartedUtc).TotalSeconds) < 2d;
                    return pathMatches && startMatches;
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (Exception exception)
            {
                AppendLogUnsafe("Kept an unverifiable session marker: " + exception.Message);
                return true;
            }
        }

        private static void CleanupOldOwnedFilesUnsafe()
        {
            CleanupFilesUnsafe(AppPaths.RecoveryDirectory, "*.tmp", TimeSpan.FromDays(1));
            CleanupFilesUnsafe(AppPaths.LogsDirectory, "startup-*.log", TimeSpan.FromDays(30));
            CleanupFilesUnsafe(AppPaths.CrashReportsDirectory, "*.txt", TimeSpan.FromDays(90));
        }

        private static void CleanupFilesUnsafe(string directory, string pattern, TimeSpan maximumAge)
        {
            try
            {
                foreach (string file in Directory.GetFiles(directory, pattern))
                {
                    if (DateTime.UtcNow - File.GetLastWriteTimeUtc(file) <= maximumAge)
                        continue;

                    TryDelete(file);
                    AppendLogUnsafe("Removed old app-owned file '" + Path.GetFileName(file) + "'.");
                }
            }
            catch (Exception exception)
            {
                AppendLogUnsafe("Unable to clean old files in '" + directory + "': " + exception.Message);
            }
        }

        private static void WriteRecoverySummaryUnsafe(SessionState state)
        {
            try
            {
                string reportPath = Path.Combine(
                    AppPaths.CrashReportsDirectory,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "unclean-session-{0:yyyyMMdd-HHmmss}-{1}.txt",
                        state.LastUpdatedUtc == default(DateTime) ? DateTime.UtcNow : state.LastUpdatedUtc,
                        state.ProcessId));
                File.WriteAllText(
                    reportPath,
                    "Grimoire Exodus detected an uncleanly ended session." + Environment.NewLine
                    + "Started (UTC): " + state.StartedUtc.ToString("O", CultureInfo.InvariantCulture) + Environment.NewLine
                    + "Last update (UTC): " + state.LastUpdatedUtc.ToString("O", CultureInfo.InvariantCulture) + Environment.NewLine
                    + "Last startup stage: " + (state.LastStage ?? "Unknown") + Environment.NewLine
                    + "Bot was running: " + state.BotWasRunning + Environment.NewLine
                    + "Hidden to tray: " + state.WasHiddenToTray + Environment.NewLine
                    + "Last bot path: " + (state.LastBotPath ?? "None") + Environment.NewLine,
                    Encoding.UTF8);
            }
            catch (Exception exception)
            {
                AppendLogUnsafe("Unable to write an unclean-session report: " + exception.Message);
            }
        }

        private static string BuildCrashReport(string context, Exception exception, bool isTerminating)
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("Grimoire Exodus crash diagnostic");
            report.AppendLine("Timestamp (UTC): " + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            report.AppendLine("Context: " + (context ?? "Unknown"));
            report.AppendLine("Terminating: " + isTerminating);
            report.AppendLine("Version: " + typeof(StartupDiagnostics).Assembly.GetName().Version);
            report.AppendLine("OS: " + Environment.OSVersion);
            report.AppendLine("Runtime: " + Environment.Version);
            report.AppendLine("32-bit process: " + (!Environment.Is64BitProcess));
            report.AppendLine("Startup stage: " + (currentSession.LastStage ?? "Unknown"));
            report.AppendLine("Last bot path: " + (currentSession.LastBotPath ?? "None"));
            report.AppendLine();
            report.AppendLine(exception.ToString());
            return report.ToString();
        }

        private static void WriteCurrentSessionUnsafe()
        {
            try
            {
                string json = JsonConvert.SerializeObject(currentSession, Formatting.Indented);
                AtomicWrite(currentSessionFile, json);
            }
            catch (Exception exception)
            {
                AppendLogUnsafe("Unable to update session marker: " + exception.Message);
            }
        }

        private static void AtomicWrite(string destination, string contents)
        {
            string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, contents, Encoding.UTF8);
                if (File.Exists(destination))
                {
                    File.Replace(temporary, destination, null, true);
                }
                else
                {
                    File.Move(temporary, destination);
                }
            }
            finally
            {
                TryDelete(temporary);
            }
        }

        private static void AppendLogUnsafe(string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(startupLogFile))
                    return;

                File.AppendAllText(
                    startupLogFile,
                    "[" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) + "] " + message + Environment.NewLine,
                    Encoding.UTF8);
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Unable to append startup diagnostics: " + exception.Message);
            }
        }

        private static DateTime? SafeGetProcessStartTimeUtc(Process process)
        {
            try
            {
                return process.StartTime.ToUniversalTime();
            }
            catch
            {
                return null;
            }
        }

        private static string SafeGetExecutablePath(Process process)
        {
            try
            {
                return process.MainModule.FileName;
            }
            catch
            {
                return null;
            }
        }

        private static string SafeFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return path;
            }
        }

        private static void TryDelete(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Unable to delete app-owned diagnostic file '" + path + "': " + exception.Message);
            }
        }

        private sealed class SessionState
        {
            public string SessionId { get; set; }

            public int ProcessId { get; set; }

            public string ExecutablePath { get; set; }

            public DateTime ProcessStartedUtc { get; set; }

            public DateTime StartedUtc { get; set; }

            public DateTime LastUpdatedUtc { get; set; }

            public string LastStage { get; set; }

            public string LastBotPath { get; set; }

            public bool BotWasRunning { get; set; }

            public bool WasHiddenToTray { get; set; }
        }
    }

    public sealed class RecoveryInfo
    {
        public DateTime StartedUtc { get; internal set; }

        public DateTime LastUpdatedUtc { get; internal set; }

        public string LastStage { get; internal set; }

        public string LastBotPath { get; internal set; }

        public bool BotWasRunning { get; internal set; }

        public bool WasHiddenToTray { get; internal set; }
    }
}
