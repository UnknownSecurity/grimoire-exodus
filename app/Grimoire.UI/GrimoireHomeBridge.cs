using Grimoire.Tools;
using Grimoire.Utils;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Grimoire.UI
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public sealed class GrimoireHomeBridge
    {
        private readonly Root root;
        private readonly IssueReportService issueReports;

        public GrimoireHomeBridge(Root root)
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            issueReports = new IssueReportService();
        }

        public string GetState()
        {
            try
            {
                return CreateResponse(true, null);
            }
            catch (Exception ex)
            {
                return CreateError(ex.Message);
            }
        }

        public string SaveAccount(string label, string username, string password)
        {
            try
            {
                AccountVault.SaveAccount(label, username, password);
                return CreateResponse(true, "Account saved to the encrypted locker.");
            }
            catch (Exception ex)
            {
                return CreateError(ex.Message);
            }
        }

        public string RemoveAccount(string id)
        {
            try
            {
                bool removed = AccountVault.RemoveAccount(id);
                return CreateResponse(
                    removed,
                    removed ? "Account removed from the locker." : "Account was not found.");
            }
            catch (Exception ex)
            {
                return CreateError(ex.Message);
            }
        }

        public string OpenAccount(string id)
        {
            try
            {
                AccountCredentials credentials = AccountVault.GetCredentials(id);
                bool opened = root.TryOpenAccountInNewClient(credentials, out string message);
                return CreateResponse(opened, message);
            }
            catch (Exception ex)
            {
                return CreateError(ex.Message);
            }
        }

        public string OpenGame()
        {
            try
            {
                root.RequestGameOpen(null);
                return CreateResponse(
                    true,
                    root.IsGameReady
                        ? "Opening AQW."
                        : "AQW is still loading. It will open automatically when ready.");
            }
            catch (Exception ex)
            {
                return CreateError(ex.Message);
            }
        }

        public string OpenRunningClient()
        {
            try
            {
                bool opened = root.ShowRunningClient();
                return CreateResponse(
                    opened,
                    opened ? "Opening the selected AQW client." : "No AQW client is currently ready to open.");
            }
            catch (Exception ex)
            {
                return CreateError(ex.Message);
            }
        }

        public string OpenClient(string id)
        {
            try
            {
                bool opened = root.ShowClientById(id);
                return CreateResponse(
                    opened,
                    opened ? null : "That AQW client is no longer available.");
            }
            catch (Exception ex)
            {
                return CreateError(ex.Message);
            }
        }

        public string AddClient()
        {
            try
            {
                bool added = root.TryAddClient(false);
                return CreateResponse(
                    added,
                    added ? "Creating another AQW client." : "The six-client safety limit has been reached.");
            }
            catch (Exception ex)
            {
                return CreateError(ex.Message);
            }
        }

        public string OpenBotFolder()
        {
            try
            {
                string botFolder = AppPaths.ExternalBotsDirectory;
                Directory.CreateDirectory(botFolder);
                Process.Start(new ProcessStartInfo(botFolder) { UseShellExecute = true });
                return CreateResponse(true, null);
            }
            catch (Exception ex)
            {
                return CreateError(ex.Message);
            }
        }

        public string OpenWebsite(string address)
        {
            try
            {
                if (!Uri.TryCreate(address, UriKind.Absolute, out Uri uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    throw new ArgumentException("Only HTTP and HTTPS links can be opened.", nameof(address));
                }

                Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
                return CreateResponse(true, null);
            }
            catch (Exception ex)
            {
                return CreateError(ex.Message);
            }
        }

        public string BeginIssueReport(
            string category,
            string subject,
            string version,
            string details,
            string steps,
            bool includeGbot,
            double startedAt)
        {
            try
            {
                long startedAtMilliseconds = double.IsNaN(startedAt) || double.IsInfinity(startedAt)
                    ? 0L
                    : Convert.ToInt64(Math.Floor(startedAt));
                IssueReportOperation operation = issueReports.Begin(new IssueReportRequest
                {
                    Category = category,
                    Subject = subject,
                    Version = version,
                    Details = details,
                    Steps = steps,
                    IncludeGbot = includeGbot,
                    StartedAtUnixMilliseconds = startedAtMilliseconds
                });

                if (operation.Success && operation.External)
                {
                    Process.Start(new ProcessStartInfo(operation.IssueUrl) { UseShellExecute = true });
                    operation.IssueUrl = null;
                }
                return CreateIssueResponse(operation);
            }
            catch (Exception)
            {
                return CreateIssueError("The report could not be created. Try again later.");
            }
        }

        public string GetIssueReportStatus(string requestId)
        {
            try
            {
                return CreateIssueResponse(issueReports.GetStatus(requestId));
            }
            catch (Exception)
            {
                return CreateIssueError("The report status could not be read. Try again.");
            }
        }

        private string CreateResponse(bool success, string message)
        {
            return JsonConvert.SerializeObject(new
            {
                success,
                message,
                gameReady = root.IsGameReady,
                gameLoadProgress = root.GameLoadProgress,
                clientRunning = root.IsClientRunning,
                clients = root.HomeClientSnapshots,
                canAddClient = root.CanAddClient,
                quickBots = root.QuickBotNames,
                quickBotsLoading = root.IsQuickBotIndexing,
                version = root.AppVersion,
                accounts = root.HomeAccountSnapshots
            });
        }

        private string CreateError(string message)
        {
            return JsonConvert.SerializeObject(new
            {
                success = false,
                message = string.IsNullOrWhiteSpace(message) ? "The request could not be completed." : message,
                gameReady = false,
                clientRunning = root.IsClientRunning,
                clients = root.HomeClientSnapshots,
                canAddClient = root.CanAddClient,
                quickBots = new object[0],
                quickBotsLoading = false,
                version = root.AppVersion,
                accounts = new object[0]
            });
        }

        private static string CreateIssueResponse(IssueReportOperation operation)
        {
            return JsonConvert.SerializeObject(new
            {
                success = operation.Success,
                completed = operation.Completed,
                external = operation.External,
                requestId = operation.RequestId,
                issueUrl = operation.IssueUrl,
                message = operation.Message
            });
        }

        private static string CreateIssueError(string message)
        {
            return JsonConvert.SerializeObject(new
            {
                success = false,
                completed = true,
                external = false,
                requestId = (string)null,
                issueUrl = (string)null,
                message = string.IsNullOrWhiteSpace(message)
                    ? "The report could not be created."
                    : message
            });
        }
    }
}
