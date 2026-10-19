using Grimoire.Utils;
using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

internal static class IssueReportServiceSmoke
{
    private static readonly Assembly GrimoireAssembly = typeof(UpdateService).Assembly;
    private static readonly Type ServiceType = RequireType("Grimoire.Utils.IssueReportService");
    private static readonly Type RequestType = RequireType("Grimoire.Utils.IssueReportRequest");

    private static int Main()
    {
        try
        {
            ValidateInputAndComposer();
            ValidateSuccessfulSubmission();
            ValidateUntrustedResponse();
            ValidateBoundedStatusStore();
            Console.WriteLine("Embedded Home issue reporting validation passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void ValidateInputAndComposer()
    {
        using (IDisposable service = CreateService(new FakeHandler(201, SuccessResponse(15)), 4))
        {
            object invalid = Begin(service, CreateRequest("No", false));
            Require(Get<bool>(invalid, "Completed"), "Invalid input did not complete immediately.");
            Require(!Get<bool>(invalid, "Success"), "Invalid input was accepted.");

            object external = Begin(service, CreateRequest("A useful report subject", true));
            Require(Get<bool>(external, "Success") && Get<bool>(external, "External"),
                "A GBot report did not use the external GitHub composer.");
            string composerUrl = Get<string>(external, "IssueUrl");
            Require(composerUrl.StartsWith(
                "https://github.com/UnknownSecurity/grimoire-exodus/issues/new?",
                StringComparison.Ordinal), "The composer URL is not trusted.");
            Require(composerUrl.IndexOf("C%3a%5c", StringComparison.OrdinalIgnoreCase) < 0,
                "A local path leaked into the composer URL.");

            object oversized = CreateRequest("A useful report subject", false);
            Set(oversized, "Details", new string('x', 4001));
            object rejected = Begin(service, oversized);
            Require(!Get<bool>(rejected, "Success"), "Oversized report details were accepted.");
        }
    }

    private static void ValidateSuccessfulSubmission()
    {
        FakeHandler handler = new FakeHandler(201, SuccessResponse(42));
        using (IDisposable service = CreateService(handler, 4))
        {
            object pending = Begin(service, CreateRequest("Home report submission works", false));
            Require(Get<bool>(pending, "Success") && !Get<bool>(pending, "Completed"),
                "A valid text report did not start asynchronously.");

            object completed = WaitForCompletion(service, Get<string>(pending, "RequestId"));
            Require(Get<bool>(completed, "Success"), "The accepted report did not complete successfully.");
            Require(Get<string>(completed, "IssueUrl").EndsWith("/42", StringComparison.Ordinal),
                "The trusted issue URL was not retained.");
            Require(string.Equals(handler.Origin, "https://unknownsecurity.github.io", StringComparison.Ordinal),
                "The official website origin was not sent to the relay.");
            Require(handler.Body.IndexOf("\"includeGbot\":false", StringComparison.Ordinal) >= 0,
                "The relay payload did not explicitly reject GBot uploads.");
            Require(handler.Body.IndexOf("password", StringComparison.OrdinalIgnoreCase) < 0,
                "Unexpected credential data appeared in the relay payload.");
        }
    }

    private static void ValidateUntrustedResponse()
    {
        FakeHandler handler = new FakeHandler(
            201,
            "{\"ok\":true,\"issueUrl\":\"https://example.com/UnknownSecurity/grimoire-exodus/issues/9\"}");
        using (IDisposable service = CreateService(handler, 4))
        {
            object pending = Begin(service, CreateRequest("Reject an untrusted issue URL", false));
            object completed = WaitForCompletion(service, Get<string>(pending, "RequestId"));
            Require(!Get<bool>(completed, "Success"), "An untrusted issue URL was accepted.");
        }
    }

    private static void ValidateBoundedStatusStore()
    {
        using (IDisposable service = CreateService(new FakeHandler(201, SuccessResponse(77)), 2))
        {
            for (int index = 0; index < 5; index++)
            {
                object request = Begin(service, CreateRequest("Bounded report status " + index, false));
                WaitForCompletion(service, Get<string>(request, "RequestId"));
            }

            int count = GetProperty<int>(service, "StatusCount");
            Require(count <= 2, "The issue-report status store exceeded its configured bound.");
        }
    }

    private static IDisposable CreateService(HttpMessageHandler handler, int maximumStatuses)
    {
        ConstructorInfo constructor = ServiceType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(HttpMessageHandler), typeof(int) },
            null);
        Require(constructor != null, "The testable issue-report service constructor is missing.");
        return (IDisposable)constructor.Invoke(new object[] { handler, maximumStatuses });
    }

    private static object CreateRequest(string subject, bool includeGbot)
    {
        object request = Activator.CreateInstance(RequestType, true);
        Set(request, "Category", "Bug");
        Set(request, "Subject", subject);
        Set(request, "Version", "1.4.0.0");
        Set(request, "Details", "The embedded Home report form should submit this safely.");
        Set(request, "Steps", "Open Home, expand Contact, and submit the report.");
        Set(request, "IncludeGbot", includeGbot);
        Set(request, "StartedAtUnixMilliseconds", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 5000L);
        return request;
    }

    private static object Begin(object service, object request)
    {
        return ServiceType.GetMethod("Begin", BindingFlags.Instance | BindingFlags.Public)
            .Invoke(service, new[] { request });
    }

    private static object WaitForCompletion(object service, string requestId)
    {
        MethodInfo getStatus = ServiceType.GetMethod("GetStatus", BindingFlags.Instance | BindingFlags.Public);
        for (int attempt = 0; attempt < 100; attempt++)
        {
            object status = getStatus.Invoke(service, new object[] { requestId });
            if (Get<bool>(status, "Completed"))
                return status;
            Thread.Sleep(20);
        }
        throw new InvalidOperationException("The issue report did not complete in time.");
    }

    private static string SuccessResponse(int issueNumber)
    {
        return "{\"ok\":true,\"issueUrl\":\"https://github.com/UnknownSecurity/grimoire-exodus/issues/"
            + issueNumber + "\"}";
    }

    private static Type RequireType(string name)
    {
        Type type = GrimoireAssembly.GetType(name, false);
        if (type == null)
            throw new InvalidOperationException("Required type was not found: " + name);
        return type;
    }

    private static void Set(object target, string propertyName, object value)
    {
        target.GetType().GetProperty(propertyName).SetValue(target, value, null);
    }

    private static T Get<T>(object target, string propertyName)
    {
        return (T)target.GetType().GetProperty(propertyName).GetValue(target, null);
    }

    private static T GetProperty<T>(object target, string propertyName)
    {
        return (T)target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(target, null);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly int statusCode;
        private readonly string responseBody;

        public FakeHandler(int statusCode, string responseBody)
        {
            this.statusCode = statusCode;
            this.responseBody = responseBody;
        }

        public string Origin { get; private set; }

        public string Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Origin = string.Join(",", request.Headers.GetValues("Origin"));
            Body = await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage((HttpStatusCode)statusCode)
            {
                Content = new StringContent(responseBody)
            };
        }
    }
}
