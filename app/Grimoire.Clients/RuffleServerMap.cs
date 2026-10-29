using Grimoire.Tools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;

namespace Grimoire.Clients
{
    /// <summary>
    /// Maps AQW's TCP server list onto stable synthetic ports understood by
    /// Ruffle's WebSocket socket proxy. The real destinations never leave the
    /// owning client session.
    /// </summary>
    internal sealed class RuffleServerMap
    {
        internal const int FirstSyntheticPort = 41000;
        internal const int MaximumSlots = 64;

        private readonly object sync = new object();
        private readonly Dictionary<int, RuffleServerEndpoint> endpoints =
            new Dictionary<int, RuffleServerEndpoint>();

        public string Rewrite(string response)
        {
            string modified = Flash.ModifyServerList(response ?? string.Empty);
            return RewriteModified(modified);
        }

        internal string RewriteModified(string modified)
        {
            if (string.IsNullOrWhiteSpace(modified)
                || !modified.StartsWith("{", StringComparison.Ordinal))
            {
                return modified;
            }

            JObject packet = JObject.Parse(modified);
            JArray servers = packet["servers"] as JArray;
            if (servers == null)
                return modified;
            if (servers.Count > MaximumSlots)
                throw new InvalidOperationException("The AQW server list exceeded the Ruffle safety limit.");

            Dictionary<int, RuffleServerEndpoint> next = new Dictionary<int, RuffleServerEndpoint>();
            for (int index = 0; index < servers.Count; index++)
            {
                JObject server = servers[index] as JObject;
                string host = server?["sIP"]?.Value<string>();
                int port = server?["iPort"]?.Value<int>() ?? 0;
                ValidateEndpoint(host, port);

                int syntheticPort = FirstSyntheticPort + index;
                next.Add(index, new RuffleServerEndpoint(host, port));
                server["RealAddress"] = host;
                server["RealPort"] = port;
                server["sIP"] = "127.0.0.1";
                server["iPort"] = syntheticPort;
            }

            lock (sync)
            {
                endpoints.Clear();
                foreach (KeyValuePair<int, RuffleServerEndpoint> pair in next)
                    endpoints.Add(pair.Key, pair.Value);
            }
            return packet.ToString(Newtonsoft.Json.Formatting.None);
        }

        public bool TryGetEndpoint(int slot, out RuffleServerEndpoint endpoint)
        {
            lock (sync)
                return endpoints.TryGetValue(slot, out endpoint);
        }

        private static void ValidateEndpoint(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host) || host.Length > 255)
                throw new InvalidOperationException("AQW returned an invalid game-server address.");
            if (port < 1 || port > ushort.MaxValue)
                throw new InvalidOperationException("AQW returned an invalid game-server port.");

            string normalized = host.Trim().TrimEnd('.');
            if (IPAddress.TryParse(normalized, out IPAddress address))
            {
                byte[] bytes = address.GetAddressBytes();
                bool privateAddress = IPAddress.IsLoopback(address)
                    || address.Equals(IPAddress.Any)
                    || address.Equals(IPAddress.IPv6Any)
                    || IsNonPublicAddress(bytes, address);
                if (privateAddress)
                    throw new InvalidOperationException("AQW returned a non-public game-server address.");
                return;
            }

            if (!IsAllowedHost(normalized))
                throw new InvalidOperationException("AQW returned an untrusted game-server host.");
        }

        private static bool IsAllowedHost(string host)
        {
            return host.EndsWith(".aq.com", StringComparison.OrdinalIgnoreCase)
                || host.EndsWith(".battleon.com", StringComparison.OrdinalIgnoreCase)
                || host.EndsWith(".artix.com", StringComparison.OrdinalIgnoreCase)
                || string.Equals(host, "aq.com", StringComparison.OrdinalIgnoreCase)
                || string.Equals(host, "battleon.com", StringComparison.OrdinalIgnoreCase)
                || string.Equals(host, "artix.com", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNonPublicAddress(byte[] bytes, IPAddress address)
        {
            if (bytes.Length == 4)
            {
                return bytes[0] == 0
                    || bytes[0] == 10
                    || bytes[0] == 127
                    || bytes[0] >= 224
                    || (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127)
                    || (bytes[0] == 169 && bytes[1] == 254)
                    || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                    || (bytes[0] == 192 && bytes[1] == 168);
            }

            if (bytes.Length != 16)
                return true;
            if (address.IsIPv6LinkLocal
                || address.IsIPv6SiteLocal
                || bytes[0] == 0xFF
                || (bytes[0] & 0xFE) == 0xFC)
            {
                return true;
            }

            bool mappedIpv4 = true;
            for (int index = 0; index < 10; index++)
                mappedIpv4 &= bytes[index] == 0;
            mappedIpv4 &= bytes[10] == 0xFF && bytes[11] == 0xFF;
            if (mappedIpv4)
            {
                return IsNonPublicAddress(
                    new[] { bytes[12], bytes[13], bytes[14], bytes[15] },
                    new IPAddress(new[] { bytes[12], bytes[13], bytes[14], bytes[15] }));
            }

            // Only globally routable unicast IPv6 is accepted for a literal
            // endpoint. Official DNS hostnames remain the normal AQW path.
            return (bytes[0] & 0xE0) != 0x20;
        }
    }

    internal sealed class RuffleServerEndpoint
    {
        public RuffleServerEndpoint(string host, int port)
        {
            Host = host;
            Port = port;
        }

        public string Host { get; }

        public int Port { get; }
    }
}
