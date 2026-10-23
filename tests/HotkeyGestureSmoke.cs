using Grimoire.Tools;
using Newtonsoft.Json;
using System;
using System.Windows.Forms;

internal static class HotkeyGestureSmoke
{
    private static int Main()
    {
        try
        {
            HotkeyGesture single = new HotkeyGesture(Keys.F5, Keys.None);
            AssertEqual(Keys.F5, single.Key, "The single-key binding changed its base key.");
            AssertEqual(Keys.None, single.Modifiers, "A single-key binding gained modifiers.");
            AssertEqual("F5", single.ToString(), "The single-key display text changed.");

            HotkeyGesture combination = new HotkeyGesture(Keys.S, Keys.Control | Keys.Shift);
            AssertEqual(Keys.S, combination.Key, "The combination changed its base key.");
            AssertEqual(Keys.Control | Keys.Shift, combination.Modifiers, "The combination lost its modifiers.");
            AssertEqual("Ctrl+Shift+S", combination.ToString(), "The combination display text is incorrect.");

            HotkeyGesture normalized = new HotkeyGesture(Keys.Control | Keys.Alt | Keys.D7, Keys.None);
            AssertEqual(Keys.D7, normalized.Key, "Combined Keys values were not normalized.");
            AssertEqual(Keys.Control | Keys.Alt, normalized.Modifiers, "Combined Keys modifiers were not normalized.");

            AssertTrue(!single.Equals(combination), "Distinct shortcuts compared as equal.");
            HotkeyGesture captured;
            AssertTrue(
                HotkeyGesture.TryCreate(Keys.S, Keys.Control | Keys.Shift, out captured)
                    && captured.Equals(combination),
                "A valid captured combination was rejected.");
            AssertTrue(
                !HotkeyGesture.TryCreate(Keys.ControlKey, Keys.Control, out captured),
                "A modifier-only shortcut was accepted.");

            const string legacyJson = "[{\"Text\":\"F5: Start bot\",\"Key\":116,\"ActionIndex\":20}]";
            Hotkey[] legacy = JsonConvert.DeserializeObject<Hotkey[]>(legacyJson);
            AssertTrue(legacy != null && legacy.Length == 1, "The legacy shortcut JSON did not load.");
            AssertEqual(Keys.F5, legacy[0].Key, "The legacy base key changed while loading.");
            AssertEqual(Keys.None, legacy[0].Modifiers, "A legacy shortcut gained modifiers while loading.");

            string singleJson = JsonConvert.SerializeObject(new Hotkey
            {
                ActionIndex = 20,
                Key = Keys.F5,
                Modifiers = Keys.None,
                Text = "F5: Start bot"
            });
            AssertTrue(
                singleJson.IndexOf("Modifiers", StringComparison.Ordinal) < 0,
                "Single-key JSON should retain the legacy-compatible shape.");

            string combinationJson = JsonConvert.SerializeObject(new Hotkey
            {
                ActionIndex = 20,
                Key = Keys.S,
                Modifiers = Keys.Control | Keys.Shift,
                Text = "Ctrl+Shift+S: Start bot"
            });
            Hotkey restored = JsonConvert.DeserializeObject<Hotkey>(combinationJson);
            AssertTrue(restored != null && restored.Gesture.Equals(combination), "The key combination did not round-trip.");

            Console.WriteLine("PASS: legacy single-key shortcuts and captured key combinations serialize safely.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!object.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }
}
