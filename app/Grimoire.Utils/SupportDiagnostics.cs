using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Grimoire.Utils
{
    /// <summary>
    /// Builds a bounded, privacy-filtered report from application-owned diagnostics.
    /// The report intentionally excludes chat, packets, credentials, and account data.
    /// </summary>
    internal static class SupportDiagnostics
    {
        internal const int MaximumReportCharacters = 160000;

        private const int MaximumDiagnosticFiles = 6;
        private const int MaximumCharactersPerFile = 32000;
        private const int MaximumDebugCharacters = 64000;
        private const int MaximumDebugLines = 120;
        private const int MaximumTailBytes = 65536;

        private static readonly Regex AbsoluteWindowsPath = new Regex(
            @"(?i)\b[A-Z]:\\[^\r\n]*",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex UncPath = new Regex(
            @"\\\\[^\r\n]+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex StandaloneToken = new Regex(
            @"(?i)\b(?:github_pat_[A-Za-z0-9_]{12,}|gh[opsu]_[A-Za-z0-9]{12,})\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex SecretField = new Regex(
            @"(?im)\b(password|passwd|pwd|token|authorization|cookie|session(?:id)?)(\s*[=:]\s*|\s+)(?:""[^""]*""|'[^']*'|[^\s,;]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex PersonalField = new Regex(
            @"(?im)\b(username|account|character|player|room(?:id|name)?)(\s*[=:]\s*|\s+)(?:""[^""]*""|'[^']*'|[^\s,;]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex LoggedInAs = new Regex(
            @"(?im)\b(logged\s+in\s+as\s+)([^\s,;]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex EmailAddress = new Regex(
            @"(?i)\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex BotPathLine = new Regex(
            @"(?im)^(Last bot path:\s*).*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex BotFile = new Regex(
            @"(?i)(?:[A-Z]:\\[^\r\n]*?\.gbot|['""][^'""\r\n]*\.gbot['""]|(?<!\w)[^\s'""<>|]+\.gbot)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly string[] ErrorTerms =
        {
            "error",
            "exception",
            "failed",
            "failure",
            "unable",
            "warning"
        };

        public static string BuildReport(string currentDebugLog)
        {
            return BuildReport(
                AppPaths.LogsDirectory,
                AppPaths.CrashReportsDirectory,
                currentDebugLog);
        }

        internal static string BuildReport(
            string logsDirectory,
            string crashReportsDirectory,
            string currentDebugLog)
        {
            StringBuilder report = new StringBuilder();
            AppendDeviceInformation(report);
            report.AppendLine();
            report.AppendLine("Privacy note");
            report.AppendLine("Account fields, credentials, bot filenames, room identifiers, tokens, and machine-specific paths are removed automatically.");
            report.AppendLine("Review the report before sharing it because free-form error messages can contain unexpected content.");

            string likelyErrors = ExtractLikelyErrors(currentDebugLog);
            report.AppendLine();
            report.AppendLine("Current-session likely errors");
            report.AppendLine(string.IsNullOrWhiteSpace(likelyErrors)
                ? "No likely error messages were detected in the current debug view."
                : Sanitize(likelyErrors));

            report.AppendLine();
            report.AppendLine("Recent app diagnostics");
            List<DiagnosticFile> files = FindDiagnosticFiles(logsDirectory, crashReportsDirectory);
            if (files.Count == 0)
            {
                report.AppendLine("No startup or crash diagnostic files are available.");
            }
            else
            {
                foreach (DiagnosticFile file in files)
                {
                    report.AppendLine();
                    report.AppendLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "--- {0} · {1:yyyy-MM-dd HH:mm:ss} UTC ---",
                        file.DisplayName,
                        file.LastWriteUtc));

                    string contents = ReadTail(file.Path);
                    report.AppendLine(string.IsNullOrWhiteSpace(contents)
                        ? "[Diagnostic could not be read.]"
                        : Sanitize(contents));

                    if (report.Length >= MaximumReportCharacters)
                        break;
                }
            }

            return BoundReport(report.ToString());
        }

        private static void AppendDeviceInformation(StringBuilder report)
        {
            report.AppendLine("Grimoire Exodus support report");
            report.AppendLine("Generated (UTC): " + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            report.AppendLine("Application version: " + GetApplicationVersion());
            report.AppendLine("Windows: " + Environment.OSVersion.VersionString);
            report.AppendLine(".NET runtime: " + Environment.Version);
            report.AppendLine("Process architecture: " + (Environment.Is64BitProcess ? "64-bit" : "32-bit"));
            report.AppendLine("OS architecture: " + (Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit"));
            report.AppendLine("Logical processors: " + Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("Selected game engine: " + GetSelectedGameEngine());
            report.AppendLine("WebView2 Runtime: " + GetWebView2Version());

            try
            {
                using (Process process = Process.GetCurrentProcess())
                {
                    long workingSetMb = process.WorkingSet64 / (1024L * 1024L);
                    report.AppendLine("Grimoire working set: " + workingSetMb.ToString(CultureInfo.InvariantCulture) + " MB");
                }
            }
            catch
            {
                report.AppendLine("Grimoire working set: unavailable");
            }
        }

        private static string GetApplicationVersion()
        {
            Version version = typeof(SupportDiagnostics).Assembly.GetName().Version;
            return version == null ? "Unknown" : version.ToString();
        }

        private static string GetSelectedGameEngine()
        {
            try
            {
                Type preferencesType = Type.GetType("Grimoire.Utils.AppPreferences, Grimoire", false);
                if (preferencesType == null)
                    return "Unknown";
                MethodInfo load = preferencesType.GetMethod("Load", BindingFlags.Static | BindingFlags.Public);
                if (load == null)
                    return "Unknown";
                object preferences = load.Invoke(null, null);
                if (preferences == null)
                    return "Unknown";
                PropertyInfo engine = preferences.GetType().GetProperty("GameEngine", BindingFlags.Instance | BindingFlags.Public);
                if (engine == null)
                    return "Unknown";
                object value = engine.GetValue(preferences, null);
                return value == null ? "Unknown" : value.ToString();
            }
            catch
            {
                return "Unknown";
            }
        }

        private static string GetWebView2Version()
        {
            try
            {
                Type environmentType = Type.GetType(
                    "Microsoft.Web.WebView2.Core.CoreWebView2Environment, Microsoft.Web.WebView2.Core",
                    false);
                if (environmentType == null)
                    return "unavailable";
                MethodInfo getVersion = environmentType.GetMethod(
                    "GetAvailableBrowserVersionString",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    Type.EmptyTypes,
                    null);
                if (getVersion == null)
                    return "unavailable";
                string version = getVersion.Invoke(null, null) as string;
                return string.IsNullOrWhiteSpace(version) ? "unavailable" : version;
            }
            catch
            {
                return "unavailable";
            }
        }

        private static List<DiagnosticFile> FindDiagnosticFiles(
            string logsDirectory,
            string crashReportsDirectory)
        {
            List<DiagnosticFile> files = new List<DiagnosticFile>();
            AddFiles(files, logsDirectory, "startup-*.log", "Startup diagnostic", TimeSpan.FromDays(30));
            AddFiles(files, crashReportsDirectory, "*.txt", "Crash diagnostic", TimeSpan.FromDays(90));
            return files
                .OrderByDescending(file => file.LastWriteUtc)
                .Take(MaximumDiagnosticFiles)
                .ToList();
        }

        private static void AddFiles(
            ICollection<DiagnosticFile> destination,
            string directory,
            string pattern,
            string displayName,
            TimeSpan maximumAge)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return;

            string root;
            try
            {
                root = Path.GetFullPath(directory)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
            }
            catch
            {
                return;
            }

            string[] paths;
            try
            {
                paths = Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly);
            }
            catch
            {
                return;
            }

            foreach (string path in paths)
            {
                try
                {
                    string fullPath = Path.GetFullPath(path);
                    if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                        continue;

                    FileInfo file = new FileInfo(fullPath);
                    if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
                        continue;
                    if (DateTime.UtcNow - file.LastWriteTimeUtc > maximumAge)
                        continue;

                    destination.Add(new DiagnosticFile
                    {
                        DisplayName = displayName,
                        LastWriteUtc = file.LastWriteTimeUtc,
                        Path = fullPath
                    });
                }
                catch
                {
                    // A diagnostic may disappear while the report is being built.
                }
            }
        }

        private static string ReadTail(string path)
        {
            try
            {
                using (FileStream stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete))
                {
                    long offset = Math.Max(0L, stream.Length - MaximumTailBytes);
                    stream.Seek(offset, SeekOrigin.Begin);
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true))
                    {
                        if (offset > 0)
                            reader.ReadLine();

                        string contents = reader.ReadToEnd();
                        if (contents.Length > MaximumCharactersPerFile)
                        {
                            contents = contents.Substring(contents.Length - MaximumCharactersPerFile);
                        }
                        return contents.Trim();
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private static string ExtractLikelyErrors(string debugLog)
        {
            if (string.IsNullOrWhiteSpace(debugLog))
                return null;

            if (debugLog.Length > MaximumDebugCharacters)
            {
                debugLog = debugLog.Substring(debugLog.Length - MaximumDebugCharacters);
            }

            Queue<string> matches = new Queue<string>();
            using (StringReader reader = new StringReader(debugLog))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!ContainsErrorTerm(line))
                        continue;

                    matches.Enqueue(line);
                    while (matches.Count > MaximumDebugLines)
                    {
                        matches.Dequeue();
                    }
                }
            }

            return matches.Count == 0 ? null : string.Join(Environment.NewLine, matches);
        }

        private static bool ContainsErrorTerm(string line)
        {
            foreach (string term in ErrorTerms)
            {
                if (line.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string sanitized = BotPathLine.Replace(text, "$1[redacted]");
            sanitized = SecretField.Replace(sanitized, "$1$2[redacted]");
            sanitized = StandaloneToken.Replace(sanitized, "[redacted token]");
            sanitized = PersonalField.Replace(sanitized, "$1$2[redacted]");
            sanitized = LoggedInAs.Replace(sanitized, "$1[redacted]");
            sanitized = EmailAddress.Replace(sanitized, "[redacted email]");
            sanitized = BotFile.Replace(sanitized, "[redacted bot file]");
            sanitized = AbsoluteWindowsPath.Replace(sanitized, "[redacted path]");
            sanitized = UncPath.Replace(sanitized, "[redacted path]");
            sanitized = ReplaceMachineValue(sanitized, Environment.UserName, "[redacted user]");
            sanitized = ReplaceMachineValue(sanitized, Environment.MachineName, "[redacted device]");
            return sanitized.Trim();
        }

        private static string ReplaceMachineValue(string text, string value, string replacement)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 3)
                return text;

            return Regex.Replace(
                text,
                Regex.Escape(value),
                replacement,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static string BoundReport(string report)
        {
            if (report.Length <= MaximumReportCharacters)
                return report;

            const string marker = "\r\n[Report shortened to protect clipboard and UI performance.]";
            return report.Substring(0, MaximumReportCharacters - marker.Length) + marker;
        }

        private sealed class DiagnosticFile
        {
            public string DisplayName { get; set; }

            public DateTime LastWriteUtc { get; set; }

            public string Path { get; set; }
        }
    }
}
