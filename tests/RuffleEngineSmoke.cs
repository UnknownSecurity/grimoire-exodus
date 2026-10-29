using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

internal static class RuffleEngineSmoke
{
    [STAThread]
    private static int Main()
    {
        try
        {
            Assembly assembly = Assembly.LoadFrom("Grimoire Exodus.exe");
            Type mapType = RequireType(assembly, "Grimoire.Clients.RuffleServerMap");
            object map = Activator.CreateInstance(mapType, true);
            MethodInfo rewrite = mapType.GetMethod("RewriteModified", BindingFlags.Instance | BindingFlags.NonPublic);
            string input = "{\"login\":{},\"servers\":[{\"sIP\":\"twilly.aq.com\",\"iPort\":5588}]}";
            string rewritten = (string)rewrite.Invoke(map, new object[] { input });
            AssertContains(rewritten, "\"sIP\":\"127.0.0.1\"");
            AssertContains(rewritten, "\"iPort\":41000");
            AssertContains(rewritten, "\"RealAddress\":\"twilly.aq.com\"");
            AssertContains(rewritten, "\"RealPort\":5588");

            MethodInfo tryGetEndpoint = mapType.GetMethod("TryGetEndpoint", BindingFlags.Instance | BindingFlags.Public);
            object[] endpointArguments = { 0, null };
            if (!(bool)tryGetEndpoint.Invoke(map, endpointArguments))
                throw new InvalidOperationException("The first socket map did not retain its endpoint.");
            object firstEndpoint = endpointArguments[1];
            AssertEqual("twilly.aq.com", GetProperty<string>(firstEndpoint, "Host"), "original endpoint host");
            AssertEqual(5588, GetProperty<int>(firstEndpoint, "Port"), "original endpoint port");

            object otherMap = Activator.CreateInstance(mapType, true);
            rewrite.Invoke(otherMap, new object[]
            {
                "{\"login\":{},\"servers\":[{\"sIP\":\"safiria.aq.com\",\"iPort\":5589}]}"
            });
            endpointArguments = new object[] { 0, null };
            tryGetEndpoint.Invoke(map, endpointArguments);
            AssertEqual("twilly.aq.com", GetProperty<string>(endpointArguments[1], "Host"), "per-client endpoint isolation");

            bool rejectedPrivate = false;
            try
            {
                rewrite.Invoke(map, new object[]
                {
                    "{\"login\":{},\"servers\":[{\"sIP\":\"127.0.0.1\",\"iPort\":5588}]}"
                });
            }
            catch (TargetInvocationException exception)
            {
                if (exception.InnerException is InvalidOperationException)
                    rejectedPrivate = true;
                else
                    throw;
            }
            if (!rejectedPrivate)
                throw new InvalidOperationException("The socket map accepted a private destination.");

            AssertRejectedEndpoint(rewrite, map, "::ffff:127.0.0.1");
            AssertRejectedEndpoint(rewrite, map, "fe80::1");
            AssertRejectedEndpoint(rewrite, map, "ff02::1");

            Type hostType = RequireType(assembly, "Grimoire.Clients.RuffleLocalHost");
            TestBridgeResultConversion(assembly);
            TestSessionProgressIsolation(assembly);
            Type availabilityType = RequireType(assembly, "Grimoire.Clients.RuffleEngineAvailability");
            MethodInfo isRequiredRuntimeFile = availabilityType.GetMethod(
                "IsRequiredRuntimeFile",
                BindingFlags.Static | BindingFlags.NonPublic);
            AssertEqual(true, (bool)isRequiredRuntimeFile.Invoke(null, new object[] { "ruffle.js" }), "runtime allowlist entry");
            AssertEqual(false, (bool)isRequiredRuntimeFile.Invoke(null, new object[] { "README.md" }), "runtime allowlist rejection");

            MethodInfo headerContainsToken = hostType.GetMethod("HeaderContainsToken", BindingFlags.Static | BindingFlags.NonPublic);
            AssertEqual(true, (bool)headerContainsToken.Invoke(null, new object[] { "keep-alive, Upgrade", "upgrade" }), "WebSocket connection token");
            AssertEqual(false, (bool)headerContainsToken.Invoke(null, new object[] { "keep-alive", "upgrade" }), "missing WebSocket connection token");

            MethodInfo validKey = hostType.GetMethod("IsValidWebSocketKey", BindingFlags.Static | BindingFlags.NonPublic);
            AssertEqual(true, (bool)validKey.Invoke(null, new object[] { Convert.ToBase64String(new byte[16]) }), "valid WebSocket key");
            AssertEqual(false, (bool)validKey.Invoke(null, new object[] { "invalid" }), "invalid WebSocket key");
            TestWebSocketFrames(hostType);
            TestHttpRequestParsing(hostType);
            TestProcessFailureClassification(assembly);
            TestHostCallbackDispatchPolicy(assembly);

            string runtime = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Ruffle");
            object host = Activator.CreateInstance(
                hostType,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                new object[] { runtime, new byte[] { 1, 2, 3 }, "<html>__BASE_URL__</html>", map },
                null);
            try
            {
                string baseUrl = (string)hostType.GetProperty("BaseUrl").GetValue(host, null);
                using (WebClient client = new WebClient())
                {
                    string page = client.DownloadString(baseUrl + "index.html");
                    AssertContains(page, baseUrl);
                    byte[] swf = client.DownloadData(baseUrl + "client.swf");
                    if (swf.Length != 3)
                        throw new InvalidOperationException("The local host changed the bundled SWF payload.");

                    byte[] runtimeScript = client.DownloadData(baseUrl + "ruffle/ruffle.js");
                    if (runtimeScript.Length == 0)
                        throw new InvalidOperationException("The allowlisted Ruffle runtime was not served.");

                    ExpectWebStatus(
                        delegate { client.DownloadData(baseUrl + "ruffle/README.md"); },
                        HttpStatusCode.NotFound,
                        "The local host served a file outside the Ruffle allowlist.");

                    ExpectWebStatus(
                        delegate { client.DownloadData(baseUrl + "../invalid/index.html"); },
                        HttpStatusCode.NotFound,
                        "The local host accepted an invalid token path.");
                }
            }
            finally
            {
                ((IDisposable)host).Dispose();
            }

            Console.WriteLine("PASS: Ruffle bridge values, per-client callbacks, server mapping, WebSocket rules, and loopback origin are isolated and bounded.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static Type RequireType(Assembly assembly, string name)
    {
        return assembly.GetType(name, true);
    }

    private static void AssertContains(string value, string expected)
    {
        if (value == null || value.IndexOf(expected, StringComparison.Ordinal) < 0)
            throw new InvalidOperationException("Expected output was not produced: " + expected);
    }

    private static void AssertRejectedEndpoint(MethodInfo rewrite, object map, string host)
    {
        try
        {
            rewrite.Invoke(map, new object[]
            {
                "{\"login\":{},\"servers\":[{\"sIP\":\"" + host + "\",\"iPort\":5588}]}"
            });
        }
        catch (TargetInvocationException exception)
        {
            if (exception.InnerException is InvalidOperationException)
                return;
            throw;
        }
        throw new InvalidOperationException("The socket map accepted a non-public endpoint.");
    }

    private static T GetProperty<T>(object value, string name)
    {
        return (T)value.GetType().GetProperty(name).GetValue(value, null);
    }

    private static void AssertEqual<T>(T expected, T actual, string scenario)
    {
        if (!object.Equals(expected, actual))
            throw new InvalidOperationException("Unexpected result for " + scenario + ".");
    }

    private static void ExpectWebStatus(Action action, HttpStatusCode expected, string message)
    {
        try
        {
            action();
        }
        catch (WebException exception)
        {
            HttpWebResponse response = exception.Response as HttpWebResponse;
            if (response != null && response.StatusCode == expected)
                return;
            throw;
        }
        throw new InvalidOperationException(message);
    }

    private static void TestWebSocketFrames(Type hostType)
    {
        MethodInfo readFrame = hostType.GetMethod("ReadWebSocketFrameAsync", BindingFlags.Static | BindingFlags.NonPublic);
        object frame = ReadFrame(readFrame, BuildMaskedFrame(2, true, new byte[] { 1, 2, 3 }, false));
        AssertEqual(true, GetProperty<bool>(frame, "Final"), "binary frame final flag");
        AssertEqual(2, GetProperty<int>(frame, "Opcode"), "binary frame opcode");
        byte[] payload = GetProperty<byte[]>(frame, "Payload");
        AssertEqual(3, payload.Length, "binary frame payload size");
        AssertEqual((byte)2, payload[1], "binary frame unmasking");

        ExpectFrameFailure(readFrame, new byte[] { 0x82, 0x01, 0x00 }, "unmasked frame");
        ExpectFrameFailure(readFrame, BuildMaskedFrame(2, true, new byte[0], true), "reserved-bit frame");
        ExpectFrameFailure(readFrame, BuildMaskedFrame(9, false, new byte[0], false), "fragmented control frame");
    }

    private static void TestHttpRequestParsing(Type hostType)
    {
        MethodInfo readRequest = hostType.GetMethod("ReadRequestAsync", BindingFlags.Static | BindingFlags.NonPublic);
        byte[] body = Encoding.UTF8.GetBytes("login=test");
        byte[] header = Encoding.ASCII.GetBytes(
            "POST /token/game/login HTTP/1.1\r\n"
            + "Host: 127.0.0.1\r\n"
            + "Content-Type: application/x-www-form-urlencoded\r\n"
            + "Content-Length: " + body.Length + "\r\n\r\n");
        byte[] requestBytes = new byte[header.Length + body.Length];
        Buffer.BlockCopy(header, 0, requestBytes, 0, header.Length);
        Buffer.BlockCopy(body, 0, requestBytes, header.Length, body.Length);

        object request;
        using (MemoryStream stream = new MemoryStream(requestBytes))
        {
            Task task = (Task)readRequest.Invoke(null, new object[] { stream, CancellationToken.None });
            task.GetAwaiter().GetResult();
            request = task.GetType().GetProperty("Result").GetValue(task, null);
        }
        AssertEqual("POST", GetProperty<string>(request, "Method"), "buffered HTTP method");
        AssertEqual("/token/game/login", GetProperty<string>(request, "PathAndQuery"), "buffered HTTP path");
        AssertEqual("login=test", Encoding.UTF8.GetString(GetProperty<byte[]>(request, "Body")), "buffered HTTP body");

        byte[] oversized = Encoding.ASCII.GetBytes(
            "POST / HTTP/1.1\r\nContent-Length: 9000000\r\n\r\n");
        try
        {
            using (MemoryStream stream = new MemoryStream(oversized))
            {
                Task task = (Task)readRequest.Invoke(null, new object[] { stream, CancellationToken.None });
                task.GetAwaiter().GetResult();
            }
        }
        catch (IOException)
        {
            return;
        }
        throw new InvalidOperationException("The local host accepted an oversized HTTP request body.");
    }

    private static void TestProcessFailureClassification(Assembly assembly)
    {
        Type bridgeType = RequireType(assembly, "Grimoire.Clients.RuffleFlashBridge");
        MethodInfo isFatal = bridgeType.GetMethod(
            "IsFatalProcessFailure",
            BindingFlags.Static | BindingFlags.NonPublic);
        AssertEqual(
            false,
            (bool)isFatal.Invoke(null, new object[] { CoreWebView2ProcessFailedKind.GpuProcessExited }),
            "recoverable GPU process restart");
        AssertEqual(
            false,
            (bool)isFatal.Invoke(null, new object[] { CoreWebView2ProcessFailedKind.UtilityProcessExited }),
            "recoverable utility process restart");
        AssertEqual(
            true,
            (bool)isFatal.Invoke(null, new object[] { CoreWebView2ProcessFailedKind.BrowserProcessExited }),
            "fatal browser process exit");
        AssertEqual(
            true,
            (bool)isFatal.Invoke(null, new object[] { CoreWebView2ProcessFailedKind.RenderProcessExited }),
            "fatal renderer process exit");
    }

    private static void TestHostCallbackDispatchPolicy(Assembly assembly)
    {
        Type bridgeType = RequireType(assembly, "Grimoire.Clients.RuffleFlashBridge");
        MethodInfo requiresDeferred = bridgeType.GetMethod(
            "RequiresDeferredHostCallback",
            BindingFlags.Static | BindingFlags.NonPublic);
        AssertEqual(
            false,
            (bool)requiresDeferred.Invoke(null, new object[] { "modifyServers" }),
            "synchronous server-list rewrite");
        AssertEqual(
            false,
            (bool)requiresDeferred.Invoke(null, new object[] { "progress" }),
            "synchronous load progress");
        AssertEqual(
            true,
            (bool)requiresDeferred.Invoke(null, new object[] { "packetFromServer" }),
            "deferred server packet callback");
        AssertEqual(
            true,
            (bool)requiresDeferred.Invoke(null, new object[] { "packetFromClient" }),
            "deferred client packet callback");
    }

    private static void TestBridgeResultConversion(Assembly assembly)
    {
        Type bridgeType = RequireType(assembly, "Grimoire.Clients.RuffleFlashBridge");
        MethodInfo direct = bridgeType.GetMethod("ConvertDirectResult", BindingFlags.Static | BindingFlags.NonPublic);
        MethodInfo serialized = bridgeType.GetMethod("ConvertSerializedResult", BindingFlags.Static | BindingFlags.NonPublic);
        Assembly jsonAssembly = Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Newtonsoft.Json.dll"));
        Type tokenType = jsonAssembly.GetType("Newtonsoft.Json.Linq.JToken", true);
        MethodInfo parse = tokenType.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string) }, null);

        object nullToken = parse.Invoke(null, new object[] { "null" });
        AssertEqual<object>(null, direct.Invoke(null, new object[] { nullToken, typeof(string) }), "null bridge result");
        AssertEqual(true, (bool)direct.Invoke(null, new object[] { parse.Invoke(null, new object[] { "true" }), typeof(bool) }), "Boolean bridge result");
        AssertEqual(42, (int)direct.Invoke(null, new object[] { parse.Invoke(null, new object[] { "42" }), typeof(int) }), "number bridge result");
        AssertEqual("hello", (string)direct.Invoke(null, new object[] { parse.Invoke(null, new object[] { "\"hello\"" }), typeof(string) }), "string bridge result");

        string[] array = (string[])direct.Invoke(null, new object[]
        {
            parse.Invoke(null, new object[] { "[\"one\",\"two\"]" }),
            typeof(string[])
        });
        AssertEqual(2, array.Length, "array bridge result");
        AssertEqual("two", array[1], "array bridge value");

        Type dictionaryType = typeof(System.Collections.Generic.Dictionary<string, int>);
        object dictionary = direct.Invoke(null, new object[]
        {
            parse.Invoke(null, new object[] { "{\"value\":7}" }),
            dictionaryType
        });
        AssertEqual(7, ((System.Collections.Generic.IDictionary<string, int>)dictionary)["value"], "object bridge result");

        object jsonString = parse.Invoke(null, new object[] { "\"{\\\"value\\\":9}\"" });
        dictionary = serialized.Invoke(null, new object[] { jsonString, dictionaryType });
        AssertEqual(9, ((System.Collections.Generic.IDictionary<string, int>)dictionary)["value"], "JSON-string bridge result");
    }

    private static void TestSessionProgressIsolation(Assembly assembly)
    {
        Type bridgeType = RequireType(assembly, "Grimoire.Clients.RuffleFlashBridge");
        Type sessionType = RequireType(assembly, "Grimoire.Clients.ClientSession");
        object firstBridge = Activator.CreateInstance(bridgeType);
        object secondBridge = Activator.CreateInstance(bridgeType);
        object firstSession = null;
        object secondSession = null;
        try
        {
            ConstructorInfo constructor = null;
            foreach (ConstructorInfo candidate in sessionType.GetConstructors())
            {
                if (candidate.GetParameters().Length == 3)
                {
                    constructor = candidate;
                    break;
                }
            }
            if (constructor == null)
                throw new InvalidOperationException("The client-session constructor was not found.");

            firstSession = constructor.Invoke(new object[] { "Ruffle test A", firstBridge, null });
            secondSession = constructor.Invoke(new object[] { "Ruffle test B", secondBridge, null });
            MethodInfo dispatch = bridgeType.GetMethod("Dispatch", BindingFlags.Instance | BindingFlags.NonPublic);
            dispatch.Invoke(firstBridge, new object[] { "progress", "[37]" });
            AssertEqual(37, GetProperty<int>(firstSession, "LoadProgress"), "first Ruffle session progress");
            AssertEqual(0, GetProperty<int>(secondSession, "LoadProgress"), "second Ruffle session isolation");

            object oversizedFunctionResult = dispatch.Invoke(
                firstBridge,
                new object[] { new string('x', 129), "[]" });
            AssertEqual<object>(null, oversizedFunctionResult, "oversized bridge function rejection");
            object oversizedMessageResult = dispatch.Invoke(
                firstBridge,
                new object[] { "progress", new string('0', (4 * 1024 * 1024) + 1) });
            AssertEqual<object>(null, oversizedMessageResult, "oversized bridge message rejection");
            AssertEqual(37, GetProperty<int>(firstSession, "LoadProgress"), "bounded bridge request state");
        }
        finally
        {
            if (firstSession != null)
                ((IDisposable)firstSession).Dispose();
            else
                ((IDisposable)firstBridge).Dispose();
            if (secondSession != null)
                ((IDisposable)secondSession).Dispose();
            else
                ((IDisposable)secondBridge).Dispose();
        }
    }

    private static object ReadFrame(MethodInfo method, byte[] bytes)
    {
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            Task task = (Task)method.Invoke(null, new object[] { stream, CancellationToken.None });
            task.GetAwaiter().GetResult();
            return task.GetType().GetProperty("Result").GetValue(task, null);
        }
    }

    private static void ExpectFrameFailure(MethodInfo method, byte[] bytes, string scenario)
    {
        try
        {
            ReadFrame(method, bytes);
        }
        catch (IOException)
        {
            return;
        }
        throw new InvalidOperationException("The local host accepted an invalid " + scenario + ".");
    }

    private static byte[] BuildMaskedFrame(int opcode, bool final, byte[] payload, bool reserved)
    {
        if (payload.Length >= 126)
            throw new ArgumentOutOfRangeException("payload");

        byte[] mask = Encoding.ASCII.GetBytes("mask");
        byte[] frame = new byte[2 + mask.Length + payload.Length];
        frame[0] = (byte)((final ? 0x80 : 0) | (reserved ? 0x40 : 0) | opcode);
        frame[1] = (byte)(0x80 | payload.Length);
        Buffer.BlockCopy(mask, 0, frame, 2, mask.Length);
        for (int index = 0; index < payload.Length; index++)
            frame[6 + index] = (byte)(payload[index] ^ mask[index % mask.Length]);
        return frame;
    }
}
