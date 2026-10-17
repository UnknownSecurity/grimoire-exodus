using Grimoire.Botting;
using System;
using System.Collections.Generic;
using System.IO;

namespace Grimoire.Utils
{
    public sealed class AppPreferenceSnapshot
    {
        public bool SkipHomePage { get; set; }

        public bool HideDeviceStats { get; set; }

        public bool ConfirmOnClose { get; set; }

        public bool MinimizeToTray { get; set; }

        public bool AllowUnlimitedClients { get; set; }
    }

    /// <summary>
    /// Typed access to global UI preferences stored in the existing app-owned
    /// configuration file. Defaults intentionally preserve the legacy startup
    /// experience while protecting active sessions from accidental closure.
    /// </summary>
    public static class AppPreferences
    {
        private const string SkipHomePageKey = "settings.skipHomePage";
        private const string HideDeviceStatsKey = "settings.hideDeviceStats";
        private const string ConfirmOnCloseKey = "settings.confirmOnClose";
        private const string MinimizeToTrayKey = "settings.minimizeToTray";
        private const string AllowUnlimitedClientsKey = "settings.allowUnlimitedClients";

        public static AppPreferenceSnapshot Load()
        {
            return new AppPreferenceSnapshot
            {
                SkipHomePage = GetBoolean(SkipHomePageKey, false),
                HideDeviceStats = GetBoolean(HideDeviceStatsKey, false),
                ConfirmOnClose = GetBoolean(ConfirmOnCloseKey, true),
                MinimizeToTray = GetBoolean(MinimizeToTrayKey, true),
                AllowUnlimitedClients = GetBoolean(AllowUnlimitedClientsKey, false)
            };
        }

        public static void Save(AppPreferenceSnapshot preferences)
        {
            if (preferences == null)
                throw new ArgumentNullException(nameof(preferences));

            Config.Instance.Set(SkipHomePageKey, preferences.SkipHomePage.ToString());
            Config.Instance.Set(HideDeviceStatsKey, preferences.HideDeviceStats.ToString());
            Config.Instance.Set(ConfirmOnCloseKey, preferences.ConfirmOnClose.ToString());
            Config.Instance.Set(MinimizeToTrayKey, preferences.MinimizeToTray.ToString());
            Config.Instance.Set(AllowUnlimitedClientsKey, preferences.AllowUnlimitedClients.ToString());
            Config.Instance.Save();
        }

        private static bool GetBoolean(string key, bool defaultValue)
        {
            string value = Config.Instance.Get(key);
            return bool.TryParse(value, out bool parsed) ? parsed : defaultValue;
        }
    }

    /// <summary>
    /// Removes only Grimoire-owned writable data. User-managed GBots under
    /// Documents and packaged community bots are deliberately outside scope.
    /// </summary>
    public static class AppDataReset
    {
        public static void ResetAll()
        {
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localAppDataPath))
                throw new InvalidOperationException("The Windows app-data directory could not be resolved.");

            string localAppData = NormalizeDirectory(localAppDataPath);
            string dataDirectory = NormalizeDirectory(AppPaths.DataDirectory);

            if (!string.Equals(Path.GetDirectoryName(dataDirectory), localAppData, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Path.GetFileName(dataDirectory), "Grimoire Exodus", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The Grimoire data directory failed its safety check.");
            }

            if (!Directory.Exists(dataDirectory))
                return;

            string externalBots = NormalizeDirectory(AppPaths.ExternalBotsDirectory);
            List<Exception> failures = new List<Exception>();

            foreach (string file in Directory.GetFiles(dataDirectory))
            {
                TryDeleteFile(file, failures);
            }

            foreach (string directory in Directory.GetDirectories(dataDirectory))
            {
                string normalized = NormalizeDirectory(directory);
                if (IsSameOrParent(normalized, externalBots))
                    continue;

                TryDeleteDirectory(directory, failures);
            }

            if (failures.Count > 0)
            {
                throw new IOException("Some Grimoire data could not be reset. Close other Grimoire windows and try again.");
            }
        }

        private static void TryDeleteFile(string path, ICollection<Exception> failures)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                failures.Add(exception);
            }
        }

        private static void TryDeleteDirectory(string path, ICollection<Exception> failures)
        {
            try
            {
                Directory.Delete(path, true);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                failures.Add(exception);
            }
        }

        private static string NormalizeDirectory(string path)
        {
            return Path.GetFullPath(path ?? string.Empty)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static bool IsSameOrParent(string candidate, string path)
        {
            if (string.Equals(candidate, path, StringComparison.OrdinalIgnoreCase))
                return true;

            return path.StartsWith(candidate + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
    }
}
