using Grimoire.Utils;
using System;
using System.IO;

namespace Grimoire.Utils
{
    internal static class AppPaths
    {
        private static readonly string Root = Environment.GetEnvironmentVariable("GRIMOIRE_DIAGNOSTICS_TEST_ROOT");

        public static string LogsDirectory { get { return GetDirectory("Logs"); } }

        public static string CrashReportsDirectory { get { return GetDirectory("Crash Reports"); } }

        public static string RecoveryDirectory { get { return GetDirectory("Recovery"); } }

        private static string GetDirectory(string name)
        {
            if (string.IsNullOrWhiteSpace(Root))
                throw new InvalidOperationException("The diagnostics test root was not provided.");

            string directory = Path.Combine(Root, name);
            Directory.CreateDirectory(directory);
            return directory;
        }
    }
}

internal static class StartupDiagnosticsSmoke
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 1)
                return Fail("Expected a test mode.");

            if (args[0] == "leave-marker")
            {
                StartupDiagnostics.StartSession();
                StartupDiagnostics.SetStage("Simulated crash");
                StartupDiagnostics.UpdateActivity(Path.Combine(AppPaths.RecoveryDirectory, "legacy.gbot"), true, true);
                return CountSessionMarkers() == 1 ? 0 : Fail("The active session marker was not created.");
            }

            if (args[0] == "recover")
            {
                File.WriteAllText(Path.Combine(AppPaths.RecoveryDirectory, "session-corrupt.json"), "not json");
                StartupDiagnostics.StartSession();
                RecoveryInfo recovery = StartupDiagnostics.PreviousSession;
                if (recovery == null)
                    return Fail("The unclean session was not detected.");
                if (recovery.LastStage != "Simulated crash" || !recovery.BotWasRunning || !recovery.WasHiddenToTray)
                    return Fail("The recovery state did not preserve the last activity.");
                if (CountSessionMarkers() != 1)
                    return Fail("Orphan or corrupt session markers were not cleaned conservatively.");

                StartupDiagnostics.MarkCleanShutdown();
                if (CountSessionMarkers() != 0)
                    return Fail("The clean shutdown marker was not removed.");
                if (Directory.GetFiles(AppPaths.CrashReportsDirectory, "unclean-session-*.txt").Length == 0)
                    return Fail("The unclean-session report was not created.");

                Console.WriteLine("Startup diagnostics and recovery passed.");
                return 0;
            }

            return Fail("Unknown test mode: " + args[0]);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static int CountSessionMarkers()
    {
        return Directory.GetFiles(AppPaths.RecoveryDirectory, "session-*.json").Length;
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 2;
    }
}
