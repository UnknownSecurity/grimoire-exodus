using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Grimoire.Clients
{
    /// <summary>
    /// Per-client, loopback-only origin for Ruffle, AQW assets, and raw-socket
    /// WebSocket tunnels. It intentionally exposes no general-purpose proxy.
    /// </summary>
    internal sealed class RuffleLocalHost : IDisposable
    {
        private const int MaximumHeaderBytes = 32768;
        private const int MaximumRequestBodyBytes = 8 * 1024 * 1024;
        private const int MaximumUpstreamResponseBytes = 64 * 1024 * 1024;
        private const int MaximumWebSocketMessageBytes = 4 * 1024 * 1024;
        private static readonly Uri OfficialGameBase = new Uri("https://game.aq.com/game/");

        private readonly TcpListener listener;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly HashSet<TcpClient> clients = new HashSet<TcpClient>();
        private readonly object clientsSync = new object();
        private readonly SemaphoreSlim connectionLimit = new SemaphoreSlim(24, 24);
        private readonly CookieContainer cookies = new CookieContainer();
        private readonly string runtimeDirectory;
        private readonly byte[] swf;
        private readonly byte[] index;
        private readonly RuffleServerMap serverMap;
        private bool disposed;

        public RuffleLocalHost(string runtimeDirectory, byte[] swf, string indexHtml, RuffleServerMap serverMap)
        {
            if (string.IsNullOrWhiteSpace(runtimeDirectory))
                throw new ArgumentException("A Ruffle runtime directory is required.", nameof(runtimeDirectory));
            if (swf == null || swf.Length == 0)
                throw new ArgumentException("The AQW client SWF is empty.", nameof(swf));
            if (string.IsNullOrWhiteSpace(indexHtml))
                throw new ArgumentException("The Ruffle host page is empty.", nameof(indexHtml));

            this.runtimeDirectory = Path.GetFullPath(runtimeDirectory);
            this.swf = (byte[])swf.Clone();
            this.serverMap = serverMap ?? throw new ArgumentNullException(nameof(serverMap));
            Token = CreateToken();
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start(32);
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            index = Encoding.UTF8.GetBytes(indexHtml.Replace("__BASE_URL__", BaseUrl));
            _ = Task.Run((Func<Task>)AcceptLoopAsync);
        }

        public int Port { get; }

        public string Token { get; }

        public string BaseUrl => "http://127.0.0.1:" + Port.ToString(CultureInfo.InvariantCulture)
            + "/" + Token + "/";

        private async Task AcceptLoopAsync()
        {
            while (!cancellation.IsCancellationRequested)
            {
                TcpClient client = null;
                try
                {
                    client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    await connectionLimit.WaitAsync(cancellation.Token).ConfigureAwait(false);
                    lock (clientsSync)
                        clients.Add(client);
                    _ = HandleClientAndReleaseAsync(client);
                }
                catch (ObjectDisposedException) when (cancellation.IsCancellationRequested)
                {
                    client?.Dispose();
                    return;
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
                {
                    client?.Dispose();
                    return;
                }
                catch
                {
                    client?.Dispose();
                    if (!cancellation.IsCancellationRequested)
                        await Task.Delay(50).ConfigureAwait(false);
                }
            }
        }

        private async Task HandleClientAndReleaseAsync(TcpClient client)
        {
            try
            {
                await HandleClientAsync(client).ConfigureAwait(false);
            }
            catch
            {
                // The adapter is fail-closed. Bridge-level failures are
                // surfaced by Ruffle/AQW without recording private traffic.
            }
            finally
            {
                lock (clientsSync)
                    clients.Remove(client);
                client.Dispose();
                connectionLimit.Release();
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            client.NoDelay = true;
            NetworkStream stream = client.GetStream();
            HttpRequest request = await ReadRequestAsync(stream, cancellation.Token).ConfigureAwait(false);
            if (request == null)
                return;

            string routePrefix = "/" + Token + "/";
            if (!request.PathAndQuery.StartsWith(routePrefix, StringComparison.Ordinal))
            {
                await WriteResponseAsync(stream, 404, "Not Found", "text/plain", Array.Empty<byte>(), null).ConfigureAwait(false);
                return;
            }

            string route = request.PathAndQuery.Substring(routePrefix.Length);
            int queryIndex = route.IndexOf('?');
            string routePath = queryIndex < 0 ? route : route.Substring(0, queryIndex);
            if (string.Equals(routePath, "socket", StringComparison.OrdinalIgnoreCase)
                || routePath.StartsWith("socket/", StringComparison.OrdinalIgnoreCase))
            {
                await HandleWebSocketAsync(stream, request, routePath).ConfigureAwait(false);
                return;
            }

            if (!string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(request.Method, "POST", StringComparison.OrdinalIgnoreCase))
            {
                await WriteResponseAsync(stream, 405, "Method Not Allowed", "text/plain", Array.Empty<byte>(), null).ConfigureAwait(false);
                return;
            }

            if (string.Equals(routePath, string.Empty, StringComparison.Ordinal)
                || string.Equals(routePath, "index.html", StringComparison.OrdinalIgnoreCase))
            {
                await WriteResponseAsync(stream, 200, "OK", "text/html; charset=utf-8", index, null).ConfigureAwait(false);
                return;
            }
            if (string.Equals(routePath, "client.swf", StringComparison.OrdinalIgnoreCase))
            {
                await WriteResponseAsync(stream, 200, "OK", "application/x-shockwave-flash", swf, null).ConfigureAwait(false);
                return;
            }
            if (routePath.StartsWith("ruffle/", StringComparison.OrdinalIgnoreCase))
            {
                await ServeRuffleFileAsync(stream, routePath.Substring("ruffle/".Length)).ConfigureAwait(false);
                return;
            }
            if (route.StartsWith("game/", StringComparison.OrdinalIgnoreCase))
            {
                await ProxyOfficialGameRequestAsync(stream, request, route.Substring("game/".Length)).ConfigureAwait(false);
                return;
            }

            await WriteResponseAsync(stream, 404, "Not Found", "text/plain", Array.Empty<byte>(), null).ConfigureAwait(false);
        }

        private async Task ServeRuffleFileAsync(NetworkStream stream, string name)
        {
            if (string.IsNullOrWhiteSpace(name)
                || name.IndexOf('/') >= 0
                || name.IndexOf('\\') >= 0
                || name.Contains("..")
                || !RuffleEngineAvailability.IsRequiredRuntimeFile(name))
            {
                await WriteResponseAsync(stream, 404, "Not Found", "text/plain", Array.Empty<byte>(), null).ConfigureAwait(false);
                return;
            }

            string path = Path.GetFullPath(Path.Combine(runtimeDirectory, name));
            if (!string.Equals(Path.GetDirectoryName(path), runtimeDirectory, StringComparison.OrdinalIgnoreCase)
                || !File.Exists(path))
            {
                await WriteResponseAsync(stream, 404, "Not Found", "text/plain", Array.Empty<byte>(), null).ConfigureAwait(false);
                return;
            }

            string contentType = name.EndsWith(".wasm", StringComparison.OrdinalIgnoreCase)
                ? "application/wasm"
                : name.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
                    ? "application/javascript; charset=utf-8"
                    : "text/plain; charset=utf-8";
            await WriteResponseAsync(stream, 200, "OK", contentType, File.ReadAllBytes(path), null).ConfigureAwait(false);
        }

        private async Task ProxyOfficialGameRequestAsync(NetworkStream clientStream, HttpRequest request, string relative)
        {
            if (relative.IndexOf("..", StringComparison.Ordinal) >= 0
                || relative.IndexOf('\\') >= 0)
            {
                await WriteResponseAsync(clientStream, 400, "Bad Request", "text/plain", Array.Empty<byte>(), null).ConfigureAwait(false);
                return;
            }

            Uri upstream = new Uri(OfficialGameBase, relative);
            if (!string.Equals(upstream.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(upstream.Host, OfficialGameBase.Host, StringComparison.OrdinalIgnoreCase)
                || !upstream.AbsolutePath.StartsWith(OfficialGameBase.AbsolutePath, StringComparison.Ordinal))
            {
                await WriteResponseAsync(clientStream, 403, "Forbidden", "text/plain", Array.Empty<byte>(), null).ConfigureAwait(false);
                return;
            }

            HttpWebRequest proxy = (HttpWebRequest)WebRequest.Create(upstream);
            proxy.Method = request.Method;
            proxy.AllowAutoRedirect = false;
            proxy.AutomaticDecompression = DecompressionMethods.None;
            proxy.Timeout = 30000;
            proxy.ReadWriteTimeout = 30000;
            proxy.UserAgent = "Grimoire-Exodus-Ruffle/1.0";
            proxy.CookieContainer = cookies;
            if (request.Headers.TryGetValue("Content-Type", out string contentType))
                proxy.ContentType = contentType;
            if (request.Headers.TryGetValue("Range", out string range))
                proxy.Headers[HttpRequestHeader.Range] = range;
            if (request.Body.Length > 0)
            {
                proxy.ContentLength = request.Body.Length;
                using (Stream body = await proxy.GetRequestStreamAsync().ConfigureAwait(false))
                    await body.WriteAsync(request.Body, 0, request.Body.Length).ConfigureAwait(false);
            }

            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)await proxy.GetResponseAsync().ConfigureAwait(false);
            }
            catch (WebException exception)
            {
                response = exception.Response as HttpWebResponse;
                if (response == null)
                {
                    await WriteResponseAsync(clientStream, 502, "Bad Gateway", "text/plain", Array.Empty<byte>(), null).ConfigureAwait(false);
                    return;
                }
            }

            using (response)
            using (Stream responseStream = response.GetResponseStream())
            using (MemoryStream buffer = new MemoryStream())
            {
                try
                {
                    await CopyToLimitedAsync(
                        responseStream,
                        buffer,
                        MaximumUpstreamResponseBytes,
                        cancellation.Token).ConfigureAwait(false);
                }
                catch (InvalidDataException)
                {
                    await WriteResponseAsync(clientStream, 502, "Bad Gateway", "text/plain", Array.Empty<byte>(), null).ConfigureAwait(false);
                    return;
                }
                Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(response.Headers[HttpResponseHeader.Location]))
                {
                    Uri location = new Uri(upstream, response.Headers[HttpResponseHeader.Location]);
                    if (string.Equals(location.Host, OfficialGameBase.Host, StringComparison.OrdinalIgnoreCase)
                        && location.AbsolutePath.StartsWith(OfficialGameBase.AbsolutePath, StringComparison.Ordinal))
                    {
                        string local = BaseUrl + "game/" + location.PathAndQuery.Substring(OfficialGameBase.AbsolutePath.Length);
                        headers["Location"] = local;
                    }
                }
                CopyResponseHeader(response, headers, "Accept-Ranges");
                CopyResponseHeader(response, headers, "Content-Range");
                CopyResponseHeader(response, headers, "ETag");
                CopyResponseHeader(response, headers, "Last-Modified");
                await WriteResponseAsync(
                    clientStream,
                    (int)response.StatusCode,
                    response.StatusDescription,
                    response.ContentType,
                    buffer.ToArray(),
                    headers).ConfigureAwait(false);
            }
        }

        private async Task HandleWebSocketAsync(NetworkStream browser, HttpRequest request, string routePath)
        {
            string[] segments = routePath.Split('/');
            if (segments.Length != 2
                || !int.TryParse(segments[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int slot)
                || !serverMap.TryGetEndpoint(slot, out RuffleServerEndpoint endpoint)
                || !string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase)
                || !request.Headers.TryGetValue("Upgrade", out string upgrade)
                || !string.Equals(upgrade, "websocket", StringComparison.OrdinalIgnoreCase)
                || !request.Headers.TryGetValue("Connection", out string connection)
                || !HeaderContainsToken(connection, "Upgrade")
                || !request.Headers.TryGetValue("Sec-WebSocket-Key", out string key)
                || !IsValidWebSocketKey(key)
                || !request.Headers.TryGetValue("Sec-WebSocket-Version", out string version)
                || !string.Equals(version.Trim(), "13", StringComparison.Ordinal)
                || !request.Headers.TryGetValue("Origin", out string origin)
                || !string.Equals(
                    origin,
                    "http://127.0.0.1:" + Port.ToString(CultureInfo.InvariantCulture),
                    StringComparison.OrdinalIgnoreCase))
            {
                await WriteResponseAsync(browser, 404, "Not Found", "text/plain", Array.Empty<byte>(), null).ConfigureAwait(false);
                return;
            }

            TcpClient game = new TcpClient();
            try
            {
                Task connect = game.ConnectAsync(endpoint.Host, endpoint.Port);
                if (await Task.WhenAny(connect, Task.Delay(10000, cancellation.Token)).ConfigureAwait(false) != connect)
                    throw new TimeoutException();
                await connect.ConfigureAwait(false);
                game.NoDelay = true;

                string accept;
                using (SHA1 sha1 = SHA1.Create())
                {
                    byte[] hash = sha1.ComputeHash(Encoding.ASCII.GetBytes(key.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
                    accept = Convert.ToBase64String(hash);
                }
                byte[] handshake = Encoding.ASCII.GetBytes(
                    "HTTP/1.1 101 Switching Protocols\r\n"
                    + "Upgrade: websocket\r\nConnection: Upgrade\r\n"
                    + "Sec-WebSocket-Accept: " + accept + "\r\n\r\n");
                await browser.WriteAsync(handshake, 0, handshake.Length).ConfigureAwait(false);

                using (NetworkStream gameStream = game.GetStream())
                using (CancellationTokenSource relayCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token))
                using (SemaphoreSlim browserWrite = new SemaphoreSlim(1, 1))
                {
                    Task inbound = RelayWebSocketToTcpAsync(browser, gameStream, browserWrite, relayCancellation.Token);
                    Task outbound = RelayTcpToWebSocketAsync(gameStream, browser, browserWrite, relayCancellation.Token);
                    await Task.WhenAny(inbound, outbound).ConfigureAwait(false);
                    relayCancellation.Cancel();
                    game.Close();
                    browser.Close();
                    await ObserveRelayCompletionAsync(inbound, outbound).ConfigureAwait(false);
                }
            }
            finally
            {
                game.Dispose();
            }
        }

        private static async Task RelayWebSocketToTcpAsync(
            NetworkStream browser,
            NetworkStream game,
            SemaphoreSlim browserWrite,
            CancellationToken cancellationToken)
        {
            using (MemoryStream fragmented = new MemoryStream())
            {
                bool fragmentActive = false;
                while (!cancellationToken.IsCancellationRequested)
                {
                    WebSocketFrame frame = await ReadWebSocketFrameAsync(browser, cancellationToken).ConfigureAwait(false);
                    if (frame.Opcode == 8)
                        return;
                    if (frame.Opcode == 9)
                    {
                        await WriteWebSocketFrameAsync(browser, browserWrite, 10, frame.Payload, cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    if (frame.Opcode == 10)
                        continue;
                    if (frame.Opcode == 1)
                        return;
                    if (frame.Opcode == 2)
                    {
                        if (fragmentActive)
                            return;
                        fragmented.SetLength(0);
                        fragmentActive = !frame.Final;
                        if (frame.Payload.Length > MaximumWebSocketMessageBytes)
                            return;
                        fragmented.Write(frame.Payload, 0, frame.Payload.Length);
                    }
                    else if (frame.Opcode == 0 && fragmentActive)
                    {
                        if (frame.Payload.Length > MaximumWebSocketMessageBytes - fragmented.Length)
                            return;
                        fragmented.Write(frame.Payload, 0, frame.Payload.Length);
                        fragmentActive = !frame.Final;
                    }
                    else
                    {
                        return;
                    }
                    if (!fragmentActive)
                    {
                        byte[] message = fragmented.ToArray();
                        await game.WriteAsync(message, 0, message.Length, cancellationToken).ConfigureAwait(false);
                        fragmented.SetLength(0);
                    }
                }
            }
        }

        private static async Task RelayTcpToWebSocketAsync(
            NetworkStream game,
            NetworkStream browser,
            SemaphoreSlim browserWrite,
            CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[16384];
            while (!cancellationToken.IsCancellationRequested)
            {
                int read = await game.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    return;
                byte[] payload = new byte[read];
                Buffer.BlockCopy(buffer, 0, payload, 0, read);
                await WriteWebSocketFrameAsync(browser, browserWrite, 2, payload, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<WebSocketFrame> ReadWebSocketFrameAsync(Stream stream, CancellationToken cancellationToken)
        {
            byte[] header = new byte[2];
            await ReadExactlyAsync(stream, header, 0, 2, cancellationToken).ConfigureAwait(false);
            bool final = (header[0] & 0x80) != 0;
            bool hasReservedBits = (header[0] & 0x70) != 0;
            int opcode = header[0] & 0x0F;
            bool masked = (header[1] & 0x80) != 0;
            ulong length = (ulong)(header[1] & 0x7F);
            if (hasReservedBits
                || (opcode != 0 && opcode != 1 && opcode != 2 && opcode != 8 && opcode != 9 && opcode != 10))
            {
                throw new IOException("Invalid WebSocket frame.");
            }
            if (!masked)
                throw new IOException("Unmasked WebSocket client frame.");
            if (length == 126)
            {
                byte[] extended = new byte[2];
                await ReadExactlyAsync(stream, extended, 0, 2, cancellationToken).ConfigureAwait(false);
                length = (ulong)((extended[0] << 8) | extended[1]);
            }
            else if (length == 127)
            {
                byte[] extended = new byte[8];
                await ReadExactlyAsync(stream, extended, 0, 8, cancellationToken).ConfigureAwait(false);
                length = 0;
                for (int index = 0; index < extended.Length; index++)
                    length = (length << 8) | extended[index];
            }
            if (length > MaximumWebSocketMessageBytes)
                throw new IOException("WebSocket frame exceeded the safety limit.");
            if (opcode >= 8 && (!final || length > 125))
                throw new IOException("Invalid WebSocket control frame.");

            byte[] mask = new byte[4];
            await ReadExactlyAsync(stream, mask, 0, 4, cancellationToken).ConfigureAwait(false);
            byte[] payload = new byte[(int)length];
            await ReadExactlyAsync(stream, payload, 0, payload.Length, cancellationToken).ConfigureAwait(false);
            for (int index = 0; index < payload.Length; index++)
                payload[index] ^= mask[index % 4];
            return new WebSocketFrame(final, opcode, payload);
        }

        private static async Task ObserveRelayCompletionAsync(params Task[] tasks)
        {
            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception exception) when (
                exception is IOException
                || exception is ObjectDisposedException
                || exception is OperationCanceledException
                || exception is SocketException)
            {
            }
        }

        private static bool HeaderContainsToken(string value, string token)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            string[] tokens = value.Split(',');
            foreach (string candidate in tokens)
            {
                if (string.Equals(candidate.Trim(), token, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool IsValidWebSocketKey(string value)
        {
            try
            {
                return Convert.FromBase64String(value.Trim()).Length == 16;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static void CopyResponseHeader(
            HttpWebResponse response,
            IDictionary<string, string> headers,
            string name)
        {
            string value = response.Headers[name];
            if (!string.IsNullOrWhiteSpace(value)
                && value.IndexOf('\r') < 0
                && value.IndexOf('\n') < 0)
            {
                headers[name] = value;
            }
        }

        private static async Task CopyToLimitedAsync(
            Stream source,
            Stream destination,
            int maximumBytes,
            CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[81920];
            int total = 0;
            while (true)
            {
                int read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    return;
                if (read > maximumBytes - total)
                    throw new InvalidDataException("The upstream response exceeded the safety limit.");
                await destination.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                total += read;
            }
        }

        private static async Task WriteWebSocketFrameAsync(
            Stream stream,
            SemaphoreSlim writeLock,
            int opcode,
            byte[] payload,
            CancellationToken cancellationToken)
        {
            await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using (MemoryStream frame = new MemoryStream())
                {
                    frame.WriteByte((byte)(0x80 | opcode));
                    if (payload.Length < 126)
                    {
                        frame.WriteByte((byte)payload.Length);
                    }
                    else if (payload.Length <= ushort.MaxValue)
                    {
                        frame.WriteByte(126);
                        frame.WriteByte((byte)(payload.Length >> 8));
                        frame.WriteByte((byte)payload.Length);
                    }
                    else
                    {
                        frame.WriteByte(127);
                        for (int shift = 56; shift >= 0; shift -= 8)
                            frame.WriteByte((byte)((ulong)payload.Length >> shift));
                    }
                    frame.Write(payload, 0, payload.Length);
                    byte[] bytes = frame.ToArray();
                    await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                writeLock.Release();
            }
        }

        private static async Task<HttpRequest> ReadRequestAsync(Stream stream, CancellationToken cancellationToken)
        {
            using (MemoryStream requestBuffer = new MemoryStream())
            {
                byte[] chunk = new byte[4096];
                int headerLength = -1;
                int scanFrom = 0;
                while (headerLength < 0)
                {
                    int read = await stream.ReadAsync(chunk, 0, chunk.Length, cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                        return null;
                    int previousLength = (int)requestBuffer.Length;
                    requestBuffer.Write(chunk, 0, read);
                    byte[] buffered = requestBuffer.GetBuffer();
                    headerLength = FindHeaderTerminator(
                        buffered,
                        (int)requestBuffer.Length,
                        Math.Max(0, scanFrom - 3));
                    scanFrom = previousLength + read;
                    if (headerLength < 0 && requestBuffer.Length > MaximumHeaderBytes)
                        throw new IOException("HTTP header exceeded the safety limit.");
                }
                if (headerLength > MaximumHeaderBytes)
                    throw new IOException("HTTP header exceeded the safety limit.");

                byte[] requestBytes = requestBuffer.ToArray();
                string headerText = Encoding.ASCII.GetString(requestBytes, 0, headerLength);
                string[] lines = headerText.Split(new[] { "\r\n" }, StringSplitOptions.None);
                string[] requestLine = lines[0].Split(' ');
                if (requestLine.Length != 3 || requestLine[1].Length > 8192)
                    throw new IOException("Invalid HTTP request line.");
                Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int index = 1; index < lines.Length && lines[index].Length > 0; index++)
                {
                    int separator = lines[index].IndexOf(':');
                    if (separator <= 0)
                        throw new IOException("Invalid HTTP header.");
                    headers[lines[index].Substring(0, separator).Trim()] = lines[index].Substring(separator + 1).Trim();
                }

                int contentLength = 0;
                if (headers.TryGetValue("Content-Length", out string lengthText)
                    && (!int.TryParse(lengthText, NumberStyles.None, CultureInfo.InvariantCulture, out contentLength)
                        || contentLength < 0
                        || contentLength > MaximumRequestBodyBytes))
                {
                    throw new IOException("HTTP request body exceeded the safety limit.");
                }
                byte[] body = new byte[contentLength];
                int bufferedBodyBytes = Math.Min(contentLength, requestBytes.Length - headerLength);
                if (bufferedBodyBytes > 0)
                    Buffer.BlockCopy(requestBytes, headerLength, body, 0, bufferedBodyBytes);
                await ReadExactlyAsync(
                    stream,
                    body,
                    bufferedBodyBytes,
                    body.Length - bufferedBodyBytes,
                    cancellationToken).ConfigureAwait(false);
                return new HttpRequest(requestLine[0], requestLine[1], headers, body);
            }
        }

        private static int FindHeaderTerminator(byte[] buffer, int count, int start)
        {
            for (int index = Math.Max(0, start); index <= count - 4; index++)
            {
                if (buffer[index] == 13
                    && buffer[index + 1] == 10
                    && buffer[index + 2] == 13
                    && buffer[index + 3] == 10)
                {
                    return index + 4;
                }
            }
            return -1;
        }

        private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken token)
        {
            while (count > 0)
            {
                int read = await stream.ReadAsync(buffer, offset, count, token).ConfigureAwait(false);
                if (read == 0)
                    throw new EndOfStreamException();
                offset += read;
                count -= read;
            }
        }

        private static async Task WriteResponseAsync(
            Stream stream,
            int status,
            string reason,
            string contentType,
            byte[] body,
            IDictionary<string, string> extraHeaders)
        {
            StringBuilder header = new StringBuilder()
                .Append("HTTP/1.1 ").Append(status).Append(' ').Append(reason ?? string.Empty).Append("\r\n")
                .Append("Connection: close\r\n")
                .Append("Cache-Control: no-store\r\n")
                .Append("X-Content-Type-Options: nosniff\r\n")
                .Append("Content-Type: ").Append(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType).Append("\r\n")
                .Append("Content-Length: ").Append(body.Length.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            if (extraHeaders != null)
            {
                foreach (KeyValuePair<string, string> pair in extraHeaders)
                    header.Append(pair.Key).Append(": ").Append(pair.Value).Append("\r\n");
            }
            header.Append("\r\n");
            byte[] headerBytes = Encoding.ASCII.GetBytes(header.ToString());
            await stream.WriteAsync(headerBytes, 0, headerBytes.Length).ConfigureAwait(false);
            if (body.Length > 0)
                await stream.WriteAsync(body, 0, body.Length).ConfigureAwait(false);
        }

        private static string CreateToken()
        {
            byte[] bytes = new byte[24];
            using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
                generator.GetBytes(bytes);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            cancellation.Cancel();
            listener.Stop();
            lock (clientsSync)
            {
                foreach (TcpClient client in clients)
                    client.Dispose();
                clients.Clear();
            }
        }

        private sealed class HttpRequest
        {
            public HttpRequest(string method, string pathAndQuery, Dictionary<string, string> headers, byte[] body)
            {
                Method = method;
                PathAndQuery = pathAndQuery;
                Headers = headers;
                Body = body;
            }

            public string Method { get; }
            public string PathAndQuery { get; }
            public Dictionary<string, string> Headers { get; }
            public byte[] Body { get; }
        }

        private sealed class WebSocketFrame
        {
            public WebSocketFrame(bool final, int opcode, byte[] payload)
            {
                Final = final;
                Opcode = opcode;
                Payload = payload;
            }

            public bool Final { get; }
            public int Opcode { get; }
            public byte[] Payload { get; }
        }
    }
}
