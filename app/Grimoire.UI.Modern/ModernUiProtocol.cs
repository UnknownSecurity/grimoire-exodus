using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Grimoire.UI.Modern
{
    public sealed class ModernUiRequest
    {
        public string Id { get; internal set; }

        public string Method { get; internal set; }

        public JObject Parameters { get; internal set; }
    }

    public sealed class ModernGameViewport
    {
        public double X { get; internal set; }

        public double Y { get; internal set; }

        public double Width { get; internal set; }

        public double Height { get; internal set; }

        public double ViewportWidth { get; internal set; }

        public double ViewportHeight { get; internal set; }
    }

    public static class ModernUiProtocol
    {
        public const string Version = "1.0";
        public const int MaximumMessageCharacters = 64 * 1024;
        private const int MaximumIdentifierCharacters = 128;
        private const int MaximumMethodCharacters = 96;

        private static readonly HashSet<string> AllowedMethods = new HashSet<string>(
            new[]
            {
                "app.getBootstrap",
                "ui.ready",
                "ui.setGameViewport",
                "navigation.home",
                "navigation.client",
                "sessions.activate",
                "sessions.create",
                "sessions.close",
                "bots.start",
                "bots.stop",
                "location.refresh",
                "location.pads",
                "location.move",
                "updates.install",
                "options.toggle",
                "tools.open",
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
            },
            StringComparer.Ordinal);

        public static bool TryParseRequest(string json, out ModernUiRequest request, out JObject error)
        {
            request = null;
            error = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = CreateError(null, "INVALID_REQUEST", "The request body is required.");
                return false;
            }
            if (json.Length > MaximumMessageCharacters)
            {
                error = CreateError(null, "REQUEST_TOO_LARGE", "The request is too large.");
                return false;
            }

            JObject envelope;
            try
            {
                envelope = JObject.Parse(json);
            }
            catch (JsonException)
            {
                error = CreateError(null, "INVALID_JSON", "The request is not valid JSON.");
                return false;
            }

            string id = envelope.Value<string>("id");
            if (string.IsNullOrWhiteSpace(id) || id.Length > MaximumIdentifierCharacters)
            {
                error = CreateError(null, "INVALID_ID", "The request identifier is invalid.");
                return false;
            }
            if (!string.Equals(envelope.Value<string>("protocol"), Version, StringComparison.Ordinal))
            {
                error = CreateError(id, "UNSUPPORTED_PROTOCOL", "The UI protocol version is not supported.");
                return false;
            }

            string method = envelope.Value<string>("method");
            if (string.IsNullOrWhiteSpace(method)
                || method.Length > MaximumMethodCharacters
                || !AllowedMethods.Contains(method))
            {
                error = CreateError(id, "UNKNOWN_METHOD", "The requested UI action is not available.");
                return false;
            }

            JToken parameterToken = envelope["params"];
            if (parameterToken != null && parameterToken.Type != JTokenType.Object)
            {
                error = CreateError(id, "INVALID_PARAMS", "The request parameters are invalid.");
                return false;
            }
            JObject parameters = parameterToken as JObject ?? new JObject();
            if (RequiresSession(method))
            {
                string sessionId = parameters.Value<string>("sessionId");
                if (!Guid.TryParse(sessionId, out Guid ignored))
                {
                    error = CreateError(id, "INVALID_SESSION", "A valid client session is required.");
                    return false;
                }
            }

            request = new ModernUiRequest
            {
                Id = id,
                Method = method,
                Parameters = parameters
            };
            return true;
        }

        public static bool TryParseViewport(JObject parameters, out ModernGameViewport viewport)
        {
            viewport = null;
            if (parameters == null)
                return false;

            if (!TryReadFinite(parameters, "x", out double x)
                || !TryReadFinite(parameters, "y", out double y)
                || !TryReadFinite(parameters, "width", out double width)
                || !TryReadFinite(parameters, "height", out double height)
                || !TryReadFinite(parameters, "viewportWidth", out double viewportWidth)
                || !TryReadFinite(parameters, "viewportHeight", out double viewportHeight)
                || width < 1d
                || height < 1d
                || viewportWidth < 1d
                || viewportHeight < 1d
                || x < 0d
                || y < 0d
                || x > viewportWidth
                || y > viewportHeight)
            {
                return false;
            }

            viewport = new ModernGameViewport
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                ViewportWidth = viewportWidth,
                ViewportHeight = viewportHeight
            };
            return true;
        }

        public static Rectangle MapViewport(ModernGameViewport viewport, Size hostSize)
        {
            if (viewport == null || hostSize.Width < 1 || hostSize.Height < 1)
                return Rectangle.Empty;

            double scaleX = hostSize.Width / viewport.ViewportWidth;
            double scaleY = hostSize.Height / viewport.ViewportHeight;
            Rectangle proposed = Rectangle.FromLTRB(
                (int)Math.Round(viewport.X * scaleX),
                (int)Math.Round(viewport.Y * scaleY),
                (int)Math.Round((viewport.X + viewport.Width) * scaleX),
                (int)Math.Round((viewport.Y + viewport.Height) * scaleY));
            Rectangle bounds = new Rectangle(Point.Empty, hostSize);
            Rectangle clamped = Rectangle.Intersect(bounds, proposed);
            return clamped.Width < 1 || clamped.Height < 1 ? Rectangle.Empty : clamped;
        }

        public static bool ShouldApplyViewport(Rectangle current, Rectangle next)
        {
            return current != next;
        }

        public static JObject CreateSuccess(string id, JToken result)
        {
            return new JObject
            {
                ["protocol"] = Version,
                ["id"] = id,
                ["ok"] = true,
                ["result"] = result ?? new JObject()
            };
        }

        public static JObject CreateError(string id, string code, string message, bool retryable = false)
        {
            JObject response = new JObject
            {
                ["protocol"] = Version,
                ["ok"] = false,
                ["error"] = new JObject
                {
                    ["code"] = code,
                    ["message"] = message,
                    ["retryable"] = retryable
                }
            };
            if (!string.IsNullOrWhiteSpace(id))
                response["id"] = id;
            return response;
        }

        public static JObject CreateEvent(string eventName, long sequence, JToken payload)
        {
            return new JObject
            {
                ["protocol"] = Version,
                ["event"] = eventName,
                ["sequence"] = sequence,
                ["payload"] = payload ?? new JObject()
            };
        }

        private static bool RequiresSession(string method)
        {
            return string.Equals(method, "sessions.activate", StringComparison.Ordinal)
                || string.Equals(method, "sessions.close", StringComparison.Ordinal)
                || string.Equals(method, "bots.start", StringComparison.Ordinal)
                || string.Equals(method, "bots.stop", StringComparison.Ordinal)
                || string.Equals(method, "location.refresh", StringComparison.Ordinal)
                || string.Equals(method, "location.pads", StringComparison.Ordinal)
                || string.Equals(method, "location.move", StringComparison.Ordinal)
                || string.Equals(method, "options.toggle", StringComparison.Ordinal);
        }

        private static bool TryReadFinite(JObject value, string name, out double number)
        {
            number = 0d;
            JToken token = value[name];
            if (token == null || (token.Type != JTokenType.Float && token.Type != JTokenType.Integer))
                return false;
            number = token.Value<double>();
            return !double.IsNaN(number) && !double.IsInfinity(number);
        }
    }
}
