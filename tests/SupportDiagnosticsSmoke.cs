using Grimoire.Utils;
using System;
using System.IO;

namespace Grimoire.Utils
{
    internal static class AppPaths
    {
        public static string LogsDirectory { get { return SupportDiagnosticsSmoke.LogsDirectory; } }

        public static string CrashReportsDirectory { get { return SupportDiagnosticsSmoke.CrashReportsDirectory; } }
    }
}

internal static class SupportDiagnosticsSmoke
{
    private static readonly string Root = Environment.GetEnvironmentVariable("GRIMOIRE_SUPPORT_TEST_ROOT");

    public static string LogsDirectory { get { return Path.Combine(Root, "Logs"); } }

    public static string CrashReportsDirectory { get { return Path.Combine(Root, "Crash Reports"); } }

    private static int Main()
    {
        try
        {
            Directory.CreateDirectory(LogsDirectory);
            Directory.CreateDirectory(CrashReportsDirectory);

            string startup = Path.Combine(LogsDirectory, "startup-current.log");
            File.WriteAllText(
                startup,
                "Stage: Ready\r\n"
                + "Handled warning username=private-user room=private-room password=hunter2 "
                + "token=github_" + "pat_FAKESECRET123456789 C:\\Users\\PrivateUser\\Bots\\private.gbot\r\n");

            string crash = Path.Combine(CrashReportsDirectory, "crash-current.txt");
            File.WriteAllText(
                crash,
                "InvalidOperationException: failed to load secret-bot.gbot\r\n"
                + "Last bot path: C:\\Users\\PrivateUser\\Documents\\Grimoire\\Bot\\secret-bot.gbot\r\n");

            string old = Path.Combine(LogsDirectory, "startup-old.log");
            File.WriteAllText(old, "OLD-DIAGNOSTIC-MUST-NOT-APPEAR");
            File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddDays(-31));
            File.WriteAllText(Path.Combine(LogsDirectory, "unrelated.txt"), "UNRELATED-MUST-NOT-APPEAR");

            for (int index = 0; index < 4; index++)
            {
                string filler = Path.Combine(CrashReportsDirectory, "crash-filler-" + index + ".txt");
                File.WriteAllText(filler, "Exception: " + new string('x', 40000));
                File.SetLastWriteTimeUtc(filler, DateTime.UtcNow.AddMinutes(-10 - index));
            }

            string debug = "packet body should stay private\r\n"
                + "ERROR logged in as private-character authorization=secret-value contact private@example.com\r\n";
            string report = SupportDiagnostics.BuildReport(debug);

            Require(report.Contains("Grimoire Exodus support report"), "The device section is missing.");
            Require(report.Contains("Current-session likely errors"), "The current error section is missing.");
            Require(report.Contains("Handled warning"), "Recent startup diagnostics were not included.");
            Require(report.Contains("InvalidOperationException"), "Recent crash diagnostics were not included.");
            Require(!report.Contains("packet body"), "Non-error debug traffic leaked into the report.");
            Require(!report.Contains("private-user"), "An account field was not redacted.");
            Require(!report.Contains("private-character"), "A character name was not redacted.");
            Require(!report.Contains("private-room"), "A room identifier was not redacted.");
            Require(!report.Contains("hunter2"), "A password was not redacted.");
            Require(!report.Contains("FAKESECRET"), "A token was not redacted.");
            Require(!report.Contains("private@example.com"), "An email address was not redacted.");
            Require(!report.Contains("PrivateUser"), "A user-profile path was not redacted.");
            Require(!report.Contains("secret-bot.gbot"), "A bot filename was not redacted.");
            Require(!report.Contains("OLD-DIAGNOSTIC"), "An expired diagnostic was included.");
            Require(!report.Contains("UNRELATED"), "An unrelated file was included.");
            Require(report.Length <= SupportDiagnostics.MaximumReportCharacters, "The report exceeded its bound.");

            Console.WriteLine("Support diagnostics privacy and bounds passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
