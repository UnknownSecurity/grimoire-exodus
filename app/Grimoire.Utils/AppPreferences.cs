using Grimoire.Botting;
using System;
using System.Collections.Generic;
using System.IO;

namespace Grimoire.Utils
{
    public enum FlashRenderingProfile
    {
        Automatic,
        Direct,
        Gpu
    }

    public enum FlashRenderQuality
    {
        Automatic,
        High,
        Medium,
        Low
    }

    public sealed class AppPreferenceSnapshot
    {
        public bool SkipHomePage { get; set; }

        public bool HideDeviceStats { get; set; }

        public bool ConfirmOnClose { get; set; }

        public bool MinimizeToTray { get; set; }

        public bool EnableMultiClient { get; set; }

        public bool AllowUnlimitedClients { get; set; }

        public bool UsePrereleaseUpdates { get; set; }

        public FlashRenderingProfile RenderingProfile { get; set; }

        public FlashRenderQuality RenderQuality { get; set; }

        public bool UseActiveClientFrameRate { get; set; }

        public int ActiveClientFramesPerSecond { get; set; }

        public bool ThrottleInactiveClients { get; set; }

        public int InactiveClientFramesPerSecond { get; set; }
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
        private const string EnableMultiClientKey = "settings.enableMultiClient";
        private const string AllowUnlimitedClientsKey = "settings.allowUnlimitedClients";
        private const string UsePrereleaseUpdatesKey = "settings.usePrereleaseUpdates";
        private const string RenderingProfileKey = "settings.renderingProfile";
        private const string RenderQualityKey = "settings.renderQuality";
        private const string UseActiveClientFrameRateKey = "settings.useActiveClientFrameRate";
        private const string ActiveClientFramesPerSecondKey = "settings.activeClientFramesPerSecond";
        private const string ThrottleInactiveClientsKey = "settings.throttleInactiveClients";
        private const string InactiveClientFramesPerSecondKey = "settings.inactiveClientFramesPerSecond";

        public static AppPreferenceSnapshot Load()
        {
            return new AppPreferenceSnapshot
            {
                SkipHomePage = GetBoolean(SkipHomePageKey, false),
                HideDeviceStats = GetBoolean(HideDeviceStatsKey, false),
                ConfirmOnClose = GetBoolean(ConfirmOnCloseKey, true),
                MinimizeToTray = GetBoolean(MinimizeToTrayKey, true),
                EnableMultiClient = GetBoolean(EnableMultiClientKey, false),
                AllowUnlimitedClients = GetBoolean(AllowUnlimitedClientsKey, false),
                UsePrereleaseUpdates = GetBoolean(UsePrereleaseUpdatesKey, false),
                RenderingProfile = GetEnum(RenderingProfileKey, FlashRenderingProfile.Automatic),
                RenderQuality = GetEnum(RenderQualityKey, FlashRenderQuality.Automatic),
                UseActiveClientFrameRate = GetBoolean(UseActiveClientFrameRateKey, false),
                ActiveClientFramesPerSecond = GetInteger(ActiveClientFramesPerSecondKey, 60, 1, 240),
                ThrottleInactiveClients = GetBoolean(ThrottleInactiveClientsKey, false),
                InactiveClientFramesPerSecond = GetInteger(InactiveClientFramesPerSecondKey, 15, 1, 60)
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
            Config.Instance.Set(EnableMultiClientKey, preferences.EnableMultiClient.ToString());
            Config.Instance.Set(AllowUnlimitedClientsKey, preferences.AllowUnlimitedClients.ToString());
            Config.Instance.Set(UsePrereleaseUpdatesKey, preferences.UsePrereleaseUpdates.ToString());
            Config.Instance.Set(RenderingProfileKey, preferences.RenderingProfile.ToString());
            Config.Instance.Set(RenderQualityKey, preferences.RenderQuality.ToString());
            Config.Instance.Set(UseActiveClientFrameRateKey, preferences.UseActiveClientFrameRate.ToString());
            Config.Instance.Set(
                ActiveClientFramesPerSecondKey,
                Clamp(preferences.ActiveClientFramesPerSecond, 1, 240).ToString(System.Globalization.CultureInfo.InvariantCulture));
            Config.Instance.Set(ThrottleInactiveClientsKey, preferences.ThrottleInactiveClients.ToString());
            Config.Instance.Set(
                InactiveClientFramesPerSecondKey,
                Clamp(preferences.InactiveClientFramesPerSecond, 1, 60).ToString(System.Globalization.CultureInfo.InvariantCulture));
            Config.Instance.Save();
        }

        private static bool GetBoolean(string key, bool defaultValue)
        {
            string value = Config.Instance.Get(key);
            return bool.TryParse(value, out bool parsed) ? parsed : defaultValue;
        }

        private static int GetInteger(string key, int defaultValue, int minimum, int maximum)
        {
            string value = Config.Instance.Get(key);
            if (!int.TryParse(
                value,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out int parsed))
            {
                return defaultValue;
            }

            return Clamp(parsed, minimum, maximum);
        }

        private static T GetEnum<T>(string key, T defaultValue)
            where T : struct
        {
            string value = Config.Instance.Get(key);
            return Enum.TryParse(value, true, out T parsed) && Enum.IsDefined(typeof(T), parsed)
                ? parsed
                : defaultValue;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
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
