using Grimoire.UI.Modern;
using Grimoire.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;

internal static class ModernUiProtocolSmoke
{
    private static int Main()
    {
        try
        {
            TestFeatureFlag();
            TestRequestValidation();
            TestViewportMapping();
            TestViewportUpdateGate();
            TestEnvelopes();
            Console.WriteLine("PASS: modern UI feature flags, protocol allowlist, envelopes, and viewport bounds are validated.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void TestFeatureFlag()
    {
        AssertEqual(false, ModernShellFeature.IsEnabled(new string[0], null), "modern shell default");
        AssertEqual(true, ModernShellFeature.IsEnabled(new[] { "Grimoire.exe", "--modern-ui" }, null), "command flag");
        AssertEqual(true, ModernShellFeature.IsEnabled(new string[0], "yes"), "environment flag");
        AssertEqual(
            true,
            ModernShellFeature.IsEnabled(new string[0], null, UserInterfaceMode.Modern),
            "persisted modern preference");
        AssertEqual(
            false,
            ModernShellFeature.IsEnabled(new[] { "Grimoire.exe", "--legacy-ui" }, "1", UserInterfaceMode.Modern),
            "legacy recovery override over persisted preference");
        AssertEqual(
            false,
            ModernShellFeature.IsEnabled(new[] { "Grimoire.exe", "--modern-ui", "--legacy-ui" }, "1"),
            "legacy recovery override");
    }

    private static void TestRequestValidation()
    {
        Guid id = Guid.NewGuid();
        JObject parameters = new JObject();
        parameters["sessionId"] = id.ToString("D");
        JObject envelope = new JObject();
        envelope["protocol"] = ModernUiProtocol.Version;
        envelope["id"] = "request-1";
        envelope["method"] = "sessions.activate";
        envelope["params"] = parameters;
        string valid = envelope.ToString();
        ModernUiRequest request;
        JObject error;
        AssertEqual(true, ModernUiProtocol.TryParseRequest(valid, out request, out error), "valid request");
        AssertEqual("sessions.activate", request.Method, "parsed method");
        AssertEqual(null, error, "valid request error");

        AssertRejected(valid.Replace("1.0", "2.0"), "UNSUPPORTED_PROTOCOL");
        AssertRejected(valid.Replace("sessions.activate", "runtime.invokeAnything"), "UNKNOWN_METHOD");
        AssertRejected(valid.Replace(id.ToString("D"), "not-a-guid"), "INVALID_SESSION");
        AssertRejected(new string('x', ModernUiProtocol.MaximumMessageCharacters + 1), "REQUEST_TOO_LARGE");

        envelope["method"] = "options.toggle";
        parameters["option"] = "lag-killer";
        AssertEqual(
            true,
            ModernUiProtocol.TryParseRequest(envelope.ToString(), out request, out error),
            "client option request");
        AssertEqual("options.toggle", request.Method, "client option method");

        parameters["sessionId"] = "not-a-guid";
        AssertEqual(
            false,
            ModernUiProtocol.TryParseRequest(envelope.ToString(), out request, out error),
            "client option session validation");
        AssertEqual("INVALID_SESSION", error["error"].Value<string>("code"), "client option rejection code");

        foreach (string method in new[]
        {
            "navigation.client",
            "home.getState",
            "home.accounts.save",
            "home.accounts.remove",
            "home.accounts.open",
            "home.botFolder",
            "home.doctor.begin",
            "home.doctor.status",
            "home.doctor.copy",
            "home.report.begin",
            "home.report.status",
            "home.link.open",
            "window.beginDrag",
            "window.minimize",
            "window.toggleMaximize",
            "window.close"
        })
        {
            envelope["method"] = method;
            envelope["params"] = new JObject();
            AssertEqual(
                true,
                ModernUiProtocol.TryParseRequest(envelope.ToString(), out request, out error),
                method + " request");
        }

        foreach (string method in new[] { "location.refresh", "location.move" })
        {
            envelope["method"] = method;
            JObject locationParameters = new JObject();
            locationParameters["sessionId"] = id.ToString("D");
            envelope["params"] = locationParameters;
            AssertEqual(
                true,
                ModernUiProtocol.TryParseRequest(envelope.ToString(), out request, out error),
                method + " request");
        }
    }

    private static void TestViewportMapping()
    {
        JObject value = new JObject();
        value["x"] = 100;
        value["y"] = 50;
        value["width"] = 500;
        value["height"] = 300;
        value["viewportWidth"] = 1000;
        value["viewportHeight"] = 500;
        ModernGameViewport viewport;
        AssertEqual(true, ModernUiProtocol.TryParseViewport(value, out viewport), "valid viewport");
        Rectangle mapped = ModernUiProtocol.MapViewport(viewport, new Size(2000, 1000));
        AssertEqual(new Rectangle(200, 100, 1000, 600), mapped, "scaled viewport");

        value["x"] = 900;
        value["width"] = 500;
        AssertEqual(true, ModernUiProtocol.TryParseViewport(value, out viewport), "clamped viewport input");
        mapped = ModernUiProtocol.MapViewport(viewport, new Size(1000, 500));
        AssertEqual(new Rectangle(900, 50, 100, 300), mapped, "clamped viewport bounds");

        value["width"] = 0;
        AssertEqual(false, ModernUiProtocol.TryParseViewport(value, out viewport), "zero-width viewport rejection");
    }

    private static void TestViewportUpdateGate()
    {
        Rectangle current = new Rectangle(12, 20, 800, 450);
        AssertEqual(
            false,
            ModernUiProtocol.ShouldApplyViewport(current, current),
            "identical viewport update");
        AssertEqual(
            true,
            ModernUiProtocol.ShouldApplyViewport(current, new Rectangle(12, 20, 801, 450)),
            "changed viewport update");
        AssertEqual(
            true,
            ModernUiProtocol.ShouldApplyViewport(Rectangle.Empty, current),
            "initial viewport update");
    }

    private static void TestEnvelopes()
    {
        JObject result = new JObject();
        result["ready"] = true;
        JObject success = ModernUiProtocol.CreateSuccess("42", result);
        AssertEqual("1.0", success.Value<string>("protocol"), "success protocol");
        AssertEqual(true, success.Value<bool>("ok"), "success flag");

        JObject failure = ModernUiProtocol.CreateError("42", "FAILED", "Failed safely.");
        AssertEqual(false, failure.Value<bool>("ok"), "failure flag");
        AssertEqual("FAILED", failure["error"].Value<string>("code"), "failure code");

        JObject eventValue = ModernUiProtocol.CreateEvent("app.stateChanged", 7, new JObject());
        AssertEqual(7L, eventValue.Value<long>("sequence"), "event sequence");
    }

    private static void AssertRejected(string json, string expectedCode)
    {
        ModernUiRequest ignored;
        JObject error;
        if (ModernUiProtocol.TryParseRequest(json, out ignored, out error))
            throw new InvalidOperationException("The invalid modern UI request was accepted.");
        AssertEqual(expectedCode, error["error"].Value<string>("code"), "request rejection code");
    }

    private static void AssertEqual<T>(T expected, T actual, string scenario)
    {
        if (!object.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                "Unexpected value for " + scenario + ". Expected: " + expected + "; actual: " + actual + ".");
        }
    }
}
