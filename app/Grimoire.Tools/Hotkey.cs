using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace Grimoire.Tools
{
    public struct HotkeyGesture : IEquatable<HotkeyGesture>
    {
        public HotkeyGesture(Keys key, Keys modifiers)
        {
            Key = key & Keys.KeyCode;
            Modifiers = (key | modifiers) & Keys.Modifiers;
        }

        public Keys Key { get; }

        public Keys Modifiers { get; }

        public bool IsEmpty => Key == Keys.None;

        public static bool TryCreate(Keys key, Keys modifiers, out HotkeyGesture gesture)
        {
            gesture = new HotkeyGesture(key, modifiers);
            if (gesture.IsEmpty || IsModifierKey(gesture.Key))
            {
                gesture = default(HotkeyGesture);
                return false;
            }

            return true;
        }

        public static bool IsModifierKey(Keys key)
        {
            switch (key & Keys.KeyCode)
            {
                case Keys.ControlKey:
                case Keys.LControlKey:
                case Keys.RControlKey:
                case Keys.ShiftKey:
                case Keys.LShiftKey:
                case Keys.RShiftKey:
                case Keys.Menu:
                case Keys.LMenu:
                case Keys.RMenu:
                case Keys.LWin:
                case Keys.RWin:
                    return true;
                default:
                    return false;
            }
        }

        public bool Equals(HotkeyGesture other)
        {
            return Key == other.Key && Modifiers == other.Modifiers;
        }

        public override bool Equals(object obj)
        {
            return obj is HotkeyGesture other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Key * 397) ^ (int)Modifiers;
            }
        }

        public override string ToString()
        {
            if (IsEmpty)
                return "None";

            List<string> parts = new List<string>(4);
            if ((Modifiers & Keys.Control) == Keys.Control)
                parts.Add("Ctrl");
            if ((Modifiers & Keys.Alt) == Keys.Alt)
                parts.Add("Alt");
            if ((Modifiers & Keys.Shift) == Keys.Shift)
                parts.Add("Shift");
            parts.Add(FormatKey(Key));
            return string.Join("+", parts);
        }

        private static string FormatKey(Keys key)
        {
            if (key >= Keys.D0 && key <= Keys.D9)
                return ((int)key - (int)Keys.D0).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
                return "Num " + ((int)key - (int)Keys.NumPad0).ToString(System.Globalization.CultureInfo.InvariantCulture);

            switch (key)
            {
                case Keys.Return:
                    return "Enter";
                case Keys.Escape:
                    return "Esc";
                case Keys.Space:
                    return "Space";
                case Keys.Prior:
                    return "Page Up";
                case Keys.Next:
                    return "Page Down";
                case Keys.Back:
                    return "Backspace";
                default:
                    return key.ToString();
            }
        }
    }

    public class Hotkey
    {
        public string Text
        {
            get;
            set;
        }

        public Keys Key
        {
            get;
            set;
        }

        [DefaultValue(Keys.None)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Keys Modifiers
        {
            get;
            set;
        }

        public int ActionIndex
        {
            get;
            set;
        }

        [JsonIgnore]
        public HotkeyGesture Gesture => new HotkeyGesture(Key, Modifiers);

        public void Install()
        {
            if (Key != Keys.None)
                KeyboardHook.Instance.TargetedKeys.Add(Key & Keys.KeyCode);
        }

        public void Uninstall()
        {
            KeyboardHook.Instance.TargetedKeys.Remove(Key & Keys.KeyCode);
        }
    }
}
