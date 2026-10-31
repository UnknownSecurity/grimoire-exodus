using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Grimoire.UI
{
    /// <summary>
    /// Resolves class names that AQW does not include in its map-wide roster.
    /// Live packet and bridge data always take priority; this bounded cache only
    /// queries the official public character page for unresolved players.
    /// </summary>
    internal sealed class UltraCharacterClassLookup : IDisposable
    {
        private const int MaximumCacheEntries = 128;
        private const int MaximumPendingRequests = 4;
        private static readonly TimeSpan SuccessLifetime = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan FailureLifetime = TimeSpan.FromSeconds(45);
        private static readonly Regex CharacterClassPattern = new Regex(
            @"(?:&amp;|&)strClassName=(?<class>[^&""<>]*)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly SemaphoreSlim RequestSlots = new SemaphoreSlim(2, 2);
        private static readonly HttpClient Client = CreateClient();

        private readonly object syncRoot = new object();
        private readonly Dictionary<string, CacheEntry> cache =
            new Dictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> pending =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool disposed;

        public string ResolveOrQueue(string username)
        {
            string normalized = NormalizeUsername(username);
            if (normalized == null)
                return null;

            CancellationToken cancellationToken;
            lock (syncRoot)
            {
                if (disposed)
                    return null;

                CacheEntry cached;
                if (cache.TryGetValue(normalized, out cached))
                {
                    if (cached.ExpiresUtc > DateTime.UtcNow)
                        return cached.ClassName;
                    cache.Remove(normalized);
                }

                if (pending.Contains(normalized) || pending.Count >= MaximumPendingRequests)
                    return null;
                pending.Add(normalized);
                cancellationToken = lifetime.Token;
            }

            Observe(FetchAndCacheAsync(normalized, cancellationToken));
            return null;
        }

        public void Remember(string username, string className)
        {
            string normalized = NormalizeUsername(username);
            string resolvedClass = NormalizeClassName(className);
            if (normalized == null || resolvedClass == null)
                return;

            lock (syncRoot)
            {
                if (!disposed)
                    Store(normalized, resolvedClass, SuccessLifetime, true);
            }
        }

        internal static string ParseClassFromCharacterPage(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return null;

            Match match = CharacterClassPattern.Match(html);
            if (!match.Success)
                return null;

            string encoded = HttpUtility.HtmlDecode(match.Groups["class"].Value);
            string decoded;
            try
            {
                decoded = Uri.UnescapeDataString(encoded.Replace("+", " "));
            }
            catch (UriFormatException)
            {
                return null;
            }
            return NormalizeClassName(decoded);
        }

        public void Dispose()
        {
            lock (syncRoot)
            {
                if (disposed)
                    return;
                disposed = true;
                pending.Clear();
                cache.Clear();
            }
            lifetime.Cancel();
            lifetime.Dispose();
        }

        private async Task FetchAndCacheAsync(string username, CancellationToken cancellationToken)
        {
            string className = null;
            bool acquired = false;
            try
            {
                await RequestSlots.WaitAsync(cancellationToken).ConfigureAwait(false);
                acquired = true;
                Uri uri = new Uri(
                    "https://account.aq.com/CharPage?id=" + Uri.EscapeDataString(username),
                    UriKind.Absolute);
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri))
                using (HttpResponseMessage response = await Client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    cancellationToken).ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode
                        && response.RequestMessage?.RequestUri != null
                        && response.RequestMessage.RequestUri.Scheme == Uri.UriSchemeHttps
                        && string.Equals(
                            response.RequestMessage.RequestUri.Host,
                            "account.aq.com",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        string html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        className = ParseClassFromCharacterPage(html);
                    }
                }
            }
            catch (Exception exception) when (
                exception is HttpRequestException
                || exception is IOException
                || exception is OperationCanceledException
                || exception is UriFormatException)
            {
                className = null;
            }
            finally
            {
                if (acquired)
                    RequestSlots.Release();

                lock (syncRoot)
                {
                    pending.Remove(username);
                    if (!disposed && !cancellationToken.IsCancellationRequested)
                    {
                        CacheEntry current;
                        if (!cache.TryGetValue(username, out current) || !current.FromLiveData)
                        {
                            Store(
                                username,
                                className,
                                className == null ? FailureLifetime : SuccessLifetime,
                                false);
                        }
                    }
                }
            }
        }

        private void Store(
            string username,
            string className,
            TimeSpan lifetimeValue,
            bool fromLiveData)
        {
            if (cache.Count >= MaximumCacheEntries && !cache.ContainsKey(username))
            {
                string oldest = cache
                    .OrderBy(item => item.Value.ExpiresUtc)
                    .Select(item => item.Key)
                    .FirstOrDefault();
                if (oldest != null)
                    cache.Remove(oldest);
            }

            cache[username] = new CacheEntry
            {
                ClassName = className,
                ExpiresUtc = DateTime.UtcNow.Add(lifetimeValue),
                FromLiveData = fromLiveData
            };
        }

        private static string NormalizeUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            string normalized = username.Trim();
            if (normalized.Length > 25
                || string.Equals(normalized, "Unknown player", StringComparison.OrdinalIgnoreCase)
                || normalized.Any(char.IsControl))
            {
                return null;
            }
            return normalized;
        }

        private static string NormalizeClassName(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
                return null;

            string normalized = className.Trim();
            if (normalized.Length > 100
                || string.Equals(normalized, "Unknown", StringComparison.OrdinalIgnoreCase)
                || normalized.IndexOfAny(new[] { '<', '>', '\r', '\n' }) >= 0)
            {
                return null;
            }
            return normalized;
        }

        private static void Observe(Task task)
        {
            task.ContinueWith(
                completed => { GC.KeepAlive(completed.Exception); },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        private static HttpClient CreateClient()
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            HttpClient client = new HttpClient(handler)
            {
                MaxResponseContentBufferSize = 256 * 1024,
                Timeout = TimeSpan.FromSeconds(10)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Grimoire-Exodus-Ultra-Kit/1.0");
            return client;
        }

        private sealed class CacheEntry
        {
            public string ClassName { get; set; }
            public DateTime ExpiresUtc { get; set; }
            public bool FromLiveData { get; set; }
        }
    }
}
