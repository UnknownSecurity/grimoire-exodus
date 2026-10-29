using Grimoire.Botting;
using Grimoire.Clients;
using Grimoire.Utils;
using System;
using System.Collections.Generic;
using System.IO;

internal static class AppPreferencesSmoke
{
    private static int Main()
    {
        Config original = Config.Instance;
        string directory = Path.Combine(Path.GetTempPath(), "grimoire-preferences-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(directory);
            Config.Instance = new Config
            {
                file = Path.Combine(directory, "config.cfg"),
                Contents = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };

            AppPreferenceSnapshot defaults = AppPreferences.Load();
            AssertEqual(FlashRenderingProfile.Automatic, defaults.RenderingProfile, "Rendering must remain Automatic by default.");
            AssertEqual(FlashRenderQuality.Automatic, defaults.RenderQuality, "Render quality must remain Automatic by default.");
            AssertEqual(false, defaults.UseActiveClientFrameRate, "Active-client FPS must remain opt-in.");
            AssertEqual(60, defaults.ActiveClientFramesPerSecond, "The active-client FPS default changed.");
            AssertEqual(false, defaults.ThrottleInactiveClients, "Inactive-client throttling must remain opt-in.");
            AssertEqual(15, defaults.InactiveClientFramesPerSecond, "The inactive-client FPS default changed.");
            AssertEqual(false, defaults.UsePrereleaseUpdates, "Prerelease updates must remain opt-in.");
            AssertEqual(false, defaults.EnableMultiClient, "Multi-client mode must remain opt-in.");
            AssertEqual(GameEngineKind.FlashActiveX, defaults.GameEngine, "Flash.ocx must remain the default game engine.");

            AppPreferences.Save(new AppPreferenceSnapshot
            {
                ConfirmOnClose = true,
                MinimizeToTray = true,
                EnableMultiClient = true,
                AllowUnlimitedClients = true,
                RenderingProfile = FlashRenderingProfile.Gpu,
                RenderQuality = FlashRenderQuality.Low,
                UseActiveClientFrameRate = true,
                ActiveClientFramesPerSecond = 144,
                ThrottleInactiveClients = true,
                InactiveClientFramesPerSecond = 12,
                UsePrereleaseUpdates = true,
                GameEngine = GameEngineKind.Ruffle
            });
            AppPreferenceSnapshot saved = AppPreferences.Load();
            AssertEqual(FlashRenderingProfile.Gpu, saved.RenderingProfile, "The rendering profile was not persisted.");
            AssertEqual(FlashRenderQuality.Low, saved.RenderQuality, "The render quality was not persisted.");
            AssertEqual(true, saved.UseActiveClientFrameRate, "The active-client FPS switch was not persisted.");
            AssertEqual(144, saved.ActiveClientFramesPerSecond, "The active-client FPS was not persisted.");
            AssertEqual(true, saved.ThrottleInactiveClients, "The inactive-client throttle was not persisted.");
            AssertEqual(12, saved.InactiveClientFramesPerSecond, "The inactive-client FPS was not persisted.");
            AssertEqual(true, saved.UsePrereleaseUpdates, "The prerelease update preference was not persisted.");
            AssertEqual(true, saved.EnableMultiClient, "The multi-client preference was not persisted.");
            AssertEqual(true, saved.AllowUnlimitedClients, "The unlimited-client preference was not persisted.");
            AssertEqual(GameEngineKind.Ruffle, saved.GameEngine, "The Ruffle engine preference was not persisted.");

            Config.Instance.Set("settings.renderingProfile", "unsupported");
            Config.Instance.Set("settings.renderQuality", "unsupported");
            Config.Instance.Set("settings.activeClientFramesPerSecond", "999");
            Config.Instance.Set("settings.inactiveClientFramesPerSecond", "0");
            Config.Instance.Set("settings.gameEngine", "unsupported");
            AppPreferenceSnapshot sanitized = AppPreferences.Load();
            AssertEqual(FlashRenderingProfile.Automatic, sanitized.RenderingProfile, "An unknown rendering profile did not fall back safely.");
            AssertEqual(FlashRenderQuality.Automatic, sanitized.RenderQuality, "An unknown render quality did not fall back safely.");
            AssertEqual(240, sanitized.ActiveClientFramesPerSecond, "The active-client FPS was not clamped.");
            AssertEqual(1, sanitized.InactiveClientFramesPerSecond, "The inactive-client FPS was not clamped.");
            AssertEqual(GameEngineKind.FlashActiveX, sanitized.GameEngine, "An unknown game engine did not fall back to Flash.ocx.");

            Console.WriteLine("PASS: engine, rendering, FPS, client-limit, and update-channel preferences keep compatibility defaults and persist safely.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            Config.Instance = original;
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
    }
}
