using System;
using System.IO;

namespace Grimoire.Utils
{
    /// <summary>
    /// Separates read-only application files from per-user writable data.
    /// Installed builds live under Program Files, so configuration and user
    /// content must not be written beside the executable.
    /// </summary>
    public static class AppPaths
    {
        public static string ApplicationDirectory { get; } =
            Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);

        public static string DataDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Grimoire Exodus");

        public static string BotClientConfigFile => GetUserFile("BotClientConfig.cfg");

        public static string ConfigFile => GetUserFile("config.cfg");

        public static string HotkeysFile => GetUserFile("hotkeys.json");

        /// <summary>
        /// User-managed GBots. Kept under Documents so they remain easy to
        /// find and survive application updates or reinstalls.
        /// </summary>
        public static string BotsDirectory => ExternalBotsDirectory;

        public static string ExternalBotsDirectory => GetExternalBotsDirectory();

        /// <summary>
        /// Read-only GBots shipped with the official application package.
        /// </summary>
        public static string CommunityBotsDirectory => Path.Combine(ApplicationDirectory, "Bot");

        public static string PluginsDirectory => GetUserDirectory("Plugins");

        public static string SetsDirectory => GetUserDirectory("Sets");

        public static string LogsDirectory => GetUserDirectory("Logs");

        public static string CrashReportsDirectory => GetUserDirectory("Crash Reports");

        public static string RecoveryDirectory => GetUserDirectory("Recovery");

        public static string UpdatesDirectory => GetUserDirectory("Updates");

        private static string GetExternalBotsDirectory()
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (string.IsNullOrWhiteSpace(documents))
            {
                documents = DataDirectory;
            }

            string destination = Path.Combine(documents, "Grimoire", "Bot");
            if (Directory.Exists(destination))
            {
                return destination;
            }

            Directory.CreateDirectory(destination);

            // Migrate the two locations used by older installed and portable
            // builds. Existing files in Documents always win.
            string[] legacyDirectories =
            {
                Path.Combine(DataDirectory, "Bots"),
                Path.Combine(ApplicationDirectory, "Bots")
            };
            foreach (string legacyDirectory in legacyDirectories)
            {
                if (Directory.Exists(legacyDirectory)
                    && !PathsEqual(legacyDirectory, destination))
                {
                    CopyDirectory(legacyDirectory, destination);
                }
            }

            return destination;
        }

        private static string GetUserFile(string name)
        {
            Directory.CreateDirectory(DataDirectory);
            string destination = Path.Combine(DataDirectory, name);
            string legacyPath = Path.Combine(ApplicationDirectory, name);

            if (!File.Exists(destination) && File.Exists(legacyPath))
            {
                TryCopyFile(legacyPath, destination);
            }

            return destination;
        }

        private static string GetUserDirectory(string name)
        {
            string destination = Path.Combine(DataDirectory, name);
            if (Directory.Exists(destination))
            {
                return destination;
            }

            Directory.CreateDirectory(destination);
            string legacyPath = Path.Combine(ApplicationDirectory, name);
            if (Directory.Exists(legacyPath)
                && !PathsEqual(legacyPath, destination))
            {
                CopyDirectory(legacyPath, destination);
            }

            return destination;
        }

        private static void CopyDirectory(string source, string destination)
        {
            foreach (string directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                string relativePath = directory.Substring(source.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                Directory.CreateDirectory(Path.Combine(destination, relativePath));
            }

            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string relativePath = file.Substring(source.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string target = Path.Combine(destination, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                TryCopyFile(file, target);
            }
        }

        private static void TryCopyFile(string source, string destination)
        {
            try
            {
                File.Copy(source, destination, false);
            }
            catch (IOException)
            {
                // A second instance may complete the same one-time migration.
                if (!File.Exists(destination))
                {
                    throw;
                }
            }
        }

        private static bool PathsEqual(string first, string second)
        {
            return string.Equals(
                Path.GetFullPath(first).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Path.GetFullPath(second).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
