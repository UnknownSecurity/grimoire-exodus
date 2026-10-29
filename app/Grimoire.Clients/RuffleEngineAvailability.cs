using Grimoire.Utils;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Grimoire.Clients
{
    internal static class RuffleEngineAvailability
    {
        private static readonly IDictionary<string, string> RequiredFiles =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ruffle.js", "26036A94567088DFB3DE88A759E8ED10A4CEA07B29500AF36F345EEF42420098" },
            { "core.ruffle.16d61072ac7fdc4d5013.js", "D27C6BDCE2A34AFB32E8DFA40970BC7E86CBCB912F92DB7EB3E1AC70E943BD82" },
            { "core.ruffle.994eeef0a471a88cd0e3.js", "D89E507EE60D550629E9229AA1E4DD4F9E75EC7DB271E0135CBC87DAB042EA51" },
            { "172de42e6619eac8f371.wasm", "1ED33B9B71A6B01B9AAFAFED660BE343FFEB2D246C5D79E6425A32BF93E38207" },
            { "878d548199cddef07035.wasm", "AF9B6A13A8F5E8D305BC3CE28A4EDF3F6242DF1183E8576AD4CA086E9F21EAEA" },
            { "LICENSE_APACHE", "62C7A1E35F56406896D7AA7CA52D0CC0D272AC022B5D2796E7D6905DB8A3636A" },
            { "LICENSE_MIT", "4DE9338A7879C68E911742A7D691F0797FF1EF8D8A6FB978B0C711E258FE959C" }
        };

        internal static bool IsRequiredRuntimeFile(string name)
        {
            return !string.IsNullOrWhiteSpace(name)
                && name.IndexOf('/') < 0
                && name.IndexOf('\\') < 0
                && !name.Contains("..")
                && RequiredFiles.ContainsKey(name);
        }

        public static bool TryGetFailure(out string message)
        {
            string runtimeDirectory = Path.Combine(AppPaths.ApplicationDirectory, "Ruffle");
            foreach (KeyValuePair<string, string> file in RequiredFiles)
            {
                string path = Path.Combine(runtimeDirectory, file.Key);
                if (!File.Exists(path) || !HasExpectedHash(path, file.Value))
                {
                    message = "The verified Ruffle runtime is missing or invalid in this installation.";
                    return true;
                }
            }

            try
            {
                string version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                if (string.IsNullOrWhiteSpace(version))
                {
                    message = "Microsoft Edge WebView2 Runtime is not installed.";
                    return true;
                }
            }
            catch
            {
                message = "Microsoft Edge WebView2 Runtime is not installed or is unavailable.";
                return true;
            }

            message = null;
            return false;
        }

        private static bool HasExpectedHash(string path, string expected)
        {
            try
            {
                using (FileStream stream = File.OpenRead(path))
                using (SHA256 algorithm = SHA256.Create())
                    return string.Equals(BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty), expected, StringComparison.OrdinalIgnoreCase);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
