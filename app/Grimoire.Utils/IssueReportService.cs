using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Grimoire.Utils
{
    internal sealed class IssueReportService : IDisposable
    {
        internal const string RelayAddress = "https://grimoire-exodus-issue-relay.0110harold.workers.dev/issues";
        internal const string OfficialWebsiteOrigin = "https://unknownsecurity.github.io";
        internal const string OfficialIssuePrefix = "https://github.com/UnknownSecurity/grimoire-exodus/issues/";
        private const string ComposerAddress = "https://github.com/UnknownSecurity/grimoire-exodus/issues/new";
        private const int MaximumResponseBytes = 64 * 1024;
        private const int DefaultMaximumStatuses = 24;
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(20);

        private readonly HttpClient client;
        private readonly int maximumStatuses;
        private readonly object statusLock = new object();
        private readonly Dictionary<string, IssueReportOperation> statuses =
            new Dictionary<string, IssueReportOperation>(StringComparer.Ordinal);
        private readonly Queue<string> statusOrder = new Queue<string>();
        private bool disposed;

        public IssueReportService()
            : this(CreateHandler(), DefaultMaximumStatuses)
        {
        }

        internal IssueReportService(HttpMessageHandler handler, int maximumStatuses)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (maximumStatuses < 1 || maximumStatuses > 100)
                throw new ArgumentOutOfRangeException(nameof(maximumStatuses));

            this.maximumStatuses = maximumStatuses;
            client = new HttpClient(handler, true)
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Grimoire-Exodus-Home/" + typeof(IssueReportService).Assembly.GetName().Version);
        }

        internal int StatusCount
        {
            get
            {
                lock (statusLock)
                    return statuses.Count;
            }
        }

        public IssueReportOperation Begin(IssueReportRequest request)
        {
            if (disposed)
                return Failed("Issue reporting is no longer available in this Home session.");

            IssueReportRequest normalized;
            string validationError;
            if (!TryNormalize(request, out normalized, out validationError))
                return Failed(validationError);

            if (normalized.IncludeGbot)
            {
                return new IssueReportOperation
                {
                    Completed = true,
                    Success = true,
                    External = true,
                    IssueUrl = BuildComposerUrl(normalized),
                    Message = "GitHub is opening. Attach the public-safe GBot there before submitting."
                };
            }

            string requestId = Guid.NewGuid().ToString("N");
            IssueReportOperation pending = new IssueReportOperation
            {
                RequestId = requestId,
                Completed = false,
                Success = true,
                Message = "Sending the report securely..."
            };
            Store(pending);
            _ = SubmitAsync(requestId, normalized);
            return Clone(pending);
        }

        public IssueReportOperation GetStatus(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId) || requestId.Length > 64)
                return Failed("The report status request was invalid. Try sending the report again.");

            lock (statusLock)
            {
                IssueReportOperation status;
                return statuses.TryGetValue(requestId, out status)
                    ? Clone(status)
                    : Failed("The report status expired. Try sending the report again.");
            }
        }

        internal static bool TryNormalize(
            IssueReportRequest request,
            out IssueReportRequest normalized,
            out string error)
        {
            normalized = null;
            error = null;
            if (request == null)
            {
                error = "Enter the report details again.";
                return false;
            }

            string category = (request.Category ?? string.Empty).Trim();
            if (category != "Bug" && category != "Feature" && category != "Support")
            {
                error = "Choose a valid report type.";
                return false;
            }

            string subject = CleanInline(request.Subject);
            string details = CleanMultiline(request.Details);
            string steps = CleanMultiline(request.Steps);
            string version = CleanInline(request.Version);
            if (subject.Length < 5 || subject.Length > 100)
            {
                error = "Use 5 to 100 characters for the subject.";
                return false;
            }
            if (details.Length < 20 || details.Length > 4000)
            {
                error = "Use 20 to 4000 characters for the details.";
                return false;
            }
            if (steps.Length > 2000)
            {
                error = "Additional context must be 2000 characters or fewer.";
                return false;
            }
            if (version.Length > 40)
            {
                error = "The version must be 40 characters or fewer.";
                return false;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long age = now - request.StartedAtUnixMilliseconds;
            if (request.StartedAtUnixMilliseconds <= 0L || age < 0L || age > TimeSpan.FromHours(24).TotalMilliseconds)
            {
                error = "Refresh Grimoire Home and try again.";
                return false;
            }
            if (age < 1500L)
            {
                error = "Wait a moment, review the report, and try again.";
                return false;
            }

            normalized = new IssueReportRequest
            {
                Category = category,
                Subject = subject,
                Version = version,
                Details = details,
                Steps = steps,
                IncludeGbot = request.IncludeGbot,
                StartedAtUnixMilliseconds = request.StartedAtUnixMilliseconds
            };
            return true;
        }

        internal static bool IsTrustedIssueUrl(string address)
        {
            Uri uri;
            if (string.IsNullOrWhiteSpace(address)
                || !address.StartsWith(OfficialIssuePrefix, StringComparison.Ordinal)
                || !Uri.TryCreate(address, UriKind.Absolute, out uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase)
                || !uri.AbsolutePath.StartsWith(
                    "/UnknownSecurity/grimoire-exodus/issues/",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string issueNumber = uri.AbsolutePath.Substring(
                "/UnknownSecurity/grimoire-exodus/issues/".Length).Trim('/');
            return issueNumber.Length > 0 && issueNumber.All(char.IsDigit);
        }

        internal static string BuildComposerUrl(IssueReportRequest request)
        {
            string gbot = request.IncludeGbot
                ? "No local file was read or uploaded by Grimoire. Attach a public-safe .gbot or .zip file in GitHub before submitting."
                : "No GBot is included with this report.";
            string body = string.Join("\n", new[]
            {
                "Submitted from Grimoire Exodus Home.",
                string.Empty,
                "## Report type",
                request.Category,
                string.Empty,
                "## Grimoire version",
                string.IsNullOrWhiteSpace(request.Version) ? "Not specified" : request.Version,
                string.Empty,
                "## Details",
                request.Details,
                string.Empty,
                "## Steps or additional context",
                string.IsNullOrWhiteSpace(request.Steps) ? "Not provided" : request.Steps,
                string.Empty,
                "## GBot",
                gbot,
                string.Empty,
                "## Privacy checklist",
                "- [ ] I removed passwords, account-vault data, tokens, private room names, and other sensitive information.",
                "- [ ] I downloaded Grimoire Exodus from the official repository or built it from source."
            });
            string query = "template=" + HttpUtility.UrlEncode("website_contact.md")
                + "&title=" + HttpUtility.UrlEncode("[" + request.Category + "] " + request.Subject)
                + "&body=" + HttpUtility.UrlEncode(body);
            return ComposerAddress + "?" + query;
        }

        private async Task SubmitAsync(string requestId, IssueReportRequest request)
        {
            try
            {
                using (CancellationTokenSource timeout = new CancellationTokenSource(RequestTimeout))
                using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, RelayAddress))
                {
                    message.Headers.TryAddWithoutValidation("Origin", OfficialWebsiteOrigin);
                    message.Content = new StringContent(
                        JsonConvert.SerializeObject(new
                        {
                            category = request.Category,
                            subject = request.Subject,
                            version = request.Version,
                            details = request.Details,
                            steps = request.Steps,
                            includeGbot = false,
                            website = string.Empty,
                            startedAt = request.StartedAtUnixMilliseconds
                        }),
                        Encoding.UTF8,
                        "application/json");

                    using (HttpResponseMessage response = await client.SendAsync(
                        message,
                        HttpCompletionOption.ResponseHeadersRead,
                        timeout.Token).ConfigureAwait(false))
                    {
                        string content = await ReadBoundedContentAsync(response, timeout.Token).ConfigureAwait(false);
                        JObject result = string.IsNullOrWhiteSpace(content) ? new JObject() : JObject.Parse(content);
                        if (!response.IsSuccessStatusCode || result.Value<bool?>("ok") != true)
                        {
                            string relayError = result.Value<string>("error");
                            throw new IssueReportException(CleanRelayMessage(relayError));
                        }

                        string issueUrl = result.Value<string>("issueUrl");
                        if (!IsTrustedIssueUrl(issueUrl))
                            throw new IssueReportException("The issue service returned an unexpected response.");

                        Store(new IssueReportOperation
                        {
                            RequestId = requestId,
                            Completed = true,
                            Success = true,
                            IssueUrl = issueUrl,
                            Message = "Issue created successfully."
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Store(Failed(requestId, "The report request timed out. Try again."));
            }
            catch (IssueReportException ex)
            {
                Store(Failed(requestId, ex.Message));
            }
            catch (Exception)
            {
                Store(Failed(requestId, "The report could not be created. Try again later."));
            }
        }

        private static async Task<string> ReadBoundedContentAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            if (response.Content == null)
                return string.Empty;

            long? declaredLength = response.Content.Headers.ContentLength;
            if (declaredLength.HasValue && declaredLength.Value > MaximumResponseBytes)
                throw new IssueReportException("The issue service returned an unexpected response.");

            using (Stream input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            using (MemoryStream output = new MemoryStream())
            {
                byte[] buffer = new byte[4096];
                int read;
                while ((read = await input.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    if (output.Length + read > MaximumResponseBytes)
                        throw new IssueReportException("The issue service returned an unexpected response.");
                    output.Write(buffer, 0, read);
                }
                return Encoding.UTF8.GetString(output.ToArray());
            }
        }

        private void Store(IssueReportOperation status)
        {
            lock (statusLock)
            {
                if (!statuses.ContainsKey(status.RequestId))
                    statusOrder.Enqueue(status.RequestId);
                statuses[status.RequestId] = Clone(status);
                while (statuses.Count > maximumStatuses && statusOrder.Count > 0)
                    statuses.Remove(statusOrder.Dequeue());
            }
        }

        private static IssueReportOperation Failed(string message)
        {
            return Failed(null, message);
        }

        private static IssueReportOperation Failed(string requestId, string message)
        {
            return new IssueReportOperation
            {
                RequestId = requestId,
                Completed = true,
                Success = false,
                Message = string.IsNullOrWhiteSpace(message)
                    ? "The report could not be created."
                    : message
            };
        }

        private static IssueReportOperation Clone(IssueReportOperation status)
        {
            return new IssueReportOperation
            {
                RequestId = status.RequestId,
                Completed = status.Completed,
                Success = status.Success,
                External = status.External,
                Message = status.Message,
                IssueUrl = status.IssueUrl
            };
        }

        private static string CleanInline(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            StringBuilder output = new StringBuilder(value.Length);
            bool previousWhitespace = false;
            foreach (char character in value)
            {
                char normalized = character == '\r' || character == '\n'
                    || character == '`' || character == '<' || character == '>'
                    ? ' '
                    : character;
                if (char.IsWhiteSpace(normalized))
                {
                    if (!previousWhitespace)
                        output.Append(' ');
                    previousWhitespace = true;
                }
                else
                {
                    output.Append(normalized);
                    previousWhitespace = false;
                }
            }
            return output.ToString().Trim();
        }

        private static string CleanMultiline(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace("\0", string.Empty).Trim();
        }

        private static string CleanRelayMessage(string message)
        {
            string cleaned = CleanInline(message);
            if (cleaned.Length == 0)
                return "The report could not be created. Try again later.";
            return cleaned.Length <= 240 ? cleaned : cleaned.Substring(0, 240);
        }

        private static HttpMessageHandler CreateHandler()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            return new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = false
            };
        }

        public void Dispose()
        {
            disposed = true;
            client.Dispose();
        }

        private sealed class IssueReportException : Exception
        {
            public IssueReportException(string message)
                : base(message)
            {
            }
        }
    }

    internal sealed class IssueReportRequest
    {
        public string Category { get; set; }

        public string Subject { get; set; }

        public string Version { get; set; }

        public string Details { get; set; }

        public string Steps { get; set; }

        public bool IncludeGbot { get; set; }

        public long StartedAtUnixMilliseconds { get; set; }
    }

    internal sealed class IssueReportOperation
    {
        public string RequestId { get; set; }

        public bool Completed { get; set; }

        public bool Success { get; set; }

        public bool External { get; set; }

        public string Message { get; set; }

        public string IssueUrl { get; set; }
    }
}
