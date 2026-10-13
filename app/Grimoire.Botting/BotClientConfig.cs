using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using System.Windows.Forms;

namespace Grimoire.Botting
{
    public class BotClientConfig
    {
        public string file { get; set; }

        public Dictionary<string, string> Contents { get; set; } = new Dictionary<string, string>();

        public BotClientConfig() { }

        public static BotClientConfig Instance
        {
            get;
            set;
        } = BotClientConfig.Load(Application.StartupPath + "\\BotClientConfig.cfg");

        public string Get(string key)
        {
            return Contents.TryGetValue(key, out string s) ? s : null;
        }

        public void Set(string key, string value)
        {
            Contents[key] = value;
        }

        public void Save()
        {
            File.WriteAllLines(file, Contents.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
                .Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }

        public static BotClientConfig Load(string path)
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, string.Empty);
            }

            Dictionary<string, string> contents = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    continue;

                int separator = line.IndexOf('=');
                if (separator <= 0)
                    continue;

                contents[line.Substring(0, separator).Trim()] = line.Substring(separator + 1);
            }

            return new BotClientConfig()
            {
                file = path,
                Contents = contents
            };
        }

        public T GetValue<T>(string key)
        {
            object value;
            try
            {
                value = BotClientConfig.Instance.Get(key);
            }
            catch
            {
                value = null;
            }
            if (typeof(T) == typeof(int))
            {
                value = value != null && int.TryParse(value.ToString(), out int intValue) ? intValue : -1;
            }
            else if (typeof(T) == typeof(bool))
            {
                value = value != null && bool.TryParse(value.ToString(), out bool boolValue) && boolValue;
            }
            else if (typeof(T) == typeof(string))
            {
                value = (value == null) ? null : value.ToString();
            }
            return (T)value;
        }

        public void SetValue(string key, string value)
        {
            BotClientConfig.Instance.Set(key, value);
            BotClientConfig.Instance.Save();
        }
    }
}
