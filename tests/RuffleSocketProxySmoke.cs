using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

internal static class RuffleSocketProxySmoke
{
    private static readonly byte[] ProbeRequest = Encoding.ASCII.GetBytes("grimoire-socket-probe");
    private static readonly byte[] ProbeReply = Encoding.ASCII.GetBytes("grimoire-socket-reply");

    private static int Main()
    {
        TcpListener upstream = null;
        IDisposable localHost = null;
        try
        {
            Assembly assembly = Assembly.LoadFrom("Grimoire Exodus.exe");
            upstream = new TcpListener(IPAddress.Loopback, 0);
            upstream.Start(1);
            int upstreamPort = ((IPEndPoint)upstream.LocalEndpoint).Port;

            object serverMap = CreateSocketMap(assembly, upstreamPort);
            Type hostType = assembly.GetType("Grimoire.Clients.RuffleLocalHost", true);
            localHost = (IDisposable)Activator.CreateInstance(
                hostType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new object[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Ruffle"),
                    new byte[] { 1 },
                    "<html>__BASE_URL__</html>",
                    serverMap
                },
                null);

            int port = (int)hostType.GetProperty("Port").GetValue(localHost, null);
            string token = (string)hostType.GetProperty("Token").GetValue(localHost, null);
            Task<TcpClient> upstreamAccept = upstream.AcceptTcpClientAsync();

            using (TcpClient browser = new TcpClient())
            {
                browser.ReceiveTimeout = 5000;
                browser.SendTimeout = 5000;
                browser.Connect(IPAddress.Loopback, port);
                using (NetworkStream browserStream = browser.GetStream())
                {
                    string key = Convert.ToBase64String(Encoding.ASCII.GetBytes("0123456789ABCDEF"));
                    string request =
                        "GET /" + token + "/socket/0 HTTP/1.1\r\n"
                        + "Host: 127.0.0.1:" + port + "\r\n"
                        + "Upgrade: websocket\r\n"
                        + "Connection: keep-alive, Upgrade\r\n"
                        + "Sec-WebSocket-Key: " + key + "\r\n"
                        + "Sec-WebSocket-Version: 13\r\n"
                        + "Origin: http://127.0.0.1:" + port + "\r\n\r\n";
                    byte[] requestBytes = Encoding.ASCII.GetBytes(request);
                    browserStream.Write(requestBytes, 0, requestBytes.Length);
                    string response = ReadHttpHeaders(browserStream);
                    AssertContains(response, "HTTP/1.1 101 Switching Protocols");
                    AssertContains(response, "Sec-WebSocket-Accept: " + CalculateAccept(key));

                    using (TcpClient game = Await(upstreamAccept, "upstream TCP connection"))
                    using (NetworkStream gameStream = game.GetStream())
                    {
                        game.ReceiveTimeout = 5000;
                        game.SendTimeout = 5000;
                        byte[] browserFrame = BuildMaskedBinaryFrame(ProbeRequest);
                        browserStream.Write(browserFrame, 0, browserFrame.Length);

                        byte[] received = ReadExactly(gameStream, ProbeRequest.Length);
                        AssertEqual(ProbeRequest, received, "browser-to-game relay");

                        gameStream.Write(ProbeReply, 0, ProbeReply.Length);
                        byte[] relayedReply = ReadServerBinaryFrame(browserStream);
                        AssertEqual(ProbeReply, relayedReply, "game-to-browser relay");
                    }
                }
            }

            Console.WriteLine("PASS: the tokenized Ruffle WebSocket handshake and bidirectional TCP relay completed without exposing packet data.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            if (localHost != null)
                localHost.Dispose();
            if (upstream != null)
                upstream.Stop();
        }
    }

    private static object CreateSocketMap(Assembly assembly, int port)
    {
        Type mapType = assembly.GetType("Grimoire.Clients.RuffleServerMap", true);
        object serverMap = Activator.CreateInstance(mapType, true);
        FieldInfo endpointsField = mapType.GetField("endpoints", BindingFlags.Instance | BindingFlags.NonPublic);
        IDictionary endpoints = (IDictionary)endpointsField.GetValue(serverMap);
        Type endpointType = assembly.GetType("Grimoire.Clients.RuffleServerEndpoint", true);
        object endpoint = Activator.CreateInstance(
            endpointType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new object[] { "127.0.0.1", port },
            null);
        endpoints.Add(0, endpoint);
        return serverMap;
    }

    private static string ReadHttpHeaders(Stream stream)
    {
        using (MemoryStream buffer = new MemoryStream())
        {
            int matched = 0;
            byte[] terminator = { 13, 10, 13, 10 };
            while (matched < terminator.Length)
            {
                int value = stream.ReadByte();
                if (value < 0)
                    throw new EndOfStreamException("The WebSocket handshake ended early.");
                buffer.WriteByte((byte)value);
                matched = value == terminator[matched]
                    ? matched + 1
                    : value == terminator[0] ? 1 : 0;
                if (buffer.Length > 32768)
                    throw new InvalidDataException("The WebSocket handshake exceeded the header limit.");
            }
            return Encoding.ASCII.GetString(buffer.ToArray());
        }
    }

    private static string CalculateAccept(string key)
    {
        using (SHA1 sha1 = SHA1.Create())
        {
            byte[] value = Encoding.ASCII.GetBytes(
                key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
            return Convert.ToBase64String(sha1.ComputeHash(value));
        }
    }

    private static byte[] BuildMaskedBinaryFrame(byte[] payload)
    {
        if (payload.Length >= 126)
            throw new ArgumentOutOfRangeException("payload");

        byte[] mask = Encoding.ASCII.GetBytes("mask");
        byte[] frame = new byte[2 + mask.Length + payload.Length];
        frame[0] = 0x82;
        frame[1] = (byte)(0x80 | payload.Length);
        Buffer.BlockCopy(mask, 0, frame, 2, mask.Length);
        for (int index = 0; index < payload.Length; index++)
            frame[6 + index] = (byte)(payload[index] ^ mask[index % mask.Length]);
        return frame;
    }

    private static byte[] ReadServerBinaryFrame(Stream stream)
    {
        int first = stream.ReadByte();
        int second = stream.ReadByte();
        if (first != 0x82 || second < 0 || (second & 0x80) != 0)
            throw new InvalidDataException("The relay returned an invalid WebSocket binary frame.");

        int length = second & 0x7F;
        if (length == 126)
        {
            byte[] extended = ReadExactly(stream, 2);
            length = (extended[0] << 8) | extended[1];
        }
        else if (length == 127)
        {
            throw new InvalidDataException("The smoke-test reply unexpectedly used a 64-bit frame length.");
        }
        return ReadExactly(stream, length);
    }

    private static byte[] ReadExactly(Stream stream, int length)
    {
        byte[] buffer = new byte[length];
        int offset = 0;
        while (offset < length)
        {
            int read = stream.Read(buffer, offset, length - offset);
            if (read == 0)
                throw new EndOfStreamException("The socket relay ended before the expected bytes arrived.");
            offset += read;
        }
        return buffer;
    }

    private static T Await<T>(Task<T> task, string stage)
    {
        if (!task.Wait(5000))
            throw new TimeoutException("Timed out waiting for " + stage + ".");
        return task.GetAwaiter().GetResult();
    }

    private static void AssertContains(string value, string expected)
    {
        if (value == null || value.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
            throw new InvalidOperationException("The WebSocket handshake was missing a required response header.");
    }

    private static void AssertEqual(byte[] expected, byte[] actual, string stage)
    {
        if (expected.Length != actual.Length)
            throw new InvalidOperationException("Unexpected byte count during " + stage + ".");
        for (int index = 0; index < expected.Length; index++)
        {
            if (expected[index] != actual[index])
                throw new InvalidOperationException("Unexpected bytes during " + stage + ".");
        }
    }
}
