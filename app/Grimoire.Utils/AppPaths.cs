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

        public static string BotsDirectory => GetUserDirectory("Bots");

        public static string PluginsDirectory => GetUserDirectory("Plugins");

        public static string SetsDirectory => GetUserDirectory("Sets");

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
