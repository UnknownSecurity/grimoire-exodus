using System;
using System.Linq;
using Grimoire.Utils;

namespace Grimoire.UI.Modern
{
    /// <summary>
    /// Resolves the selected desktop shell while retaining command-line and
    /// environment overrides for recovery and development validation.
    /// </summary>
    public static class ModernShellFeature
    {
        public const string EnvironmentVariable = "GRIMOIRE_EXODUS_MODERN_UI";

        public static bool IsEnabledForCurrentProcess()
        {
            return IsEnabled(
                Environment.GetCommandLineArgs(),
                Environment.GetEnvironmentVariable(EnvironmentVariable),
                AppPreferences.Load().InterfaceMode);
        }

        public static bool IsEnabled(string[] arguments, string environmentValue)
        {
            return IsEnabled(arguments, environmentValue, UserInterfaceMode.Legacy);
        }

        public static bool IsEnabled(
            string[] arguments,
            string environmentValue,
            UserInterfaceMode interfaceMode)
        {
            string[] values = arguments ?? new string[0];
            if (values.Any(value => string.Equals(value, "--legacy-ui", StringComparison.OrdinalIgnoreCase)))
                return false;
            if (values.Any(value => string.Equals(value, "--modern-ui", StringComparison.OrdinalIgnoreCase)))
                return true;

            string normalized = (environmentValue ?? string.Empty).Trim();
            bool environmentEnabled = string.Equals(normalized, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "yes", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "on", StringComparison.OrdinalIgnoreCase);
            return environmentEnabled || interfaceMode == UserInterfaceMode.Modern;
        }
    }
}
