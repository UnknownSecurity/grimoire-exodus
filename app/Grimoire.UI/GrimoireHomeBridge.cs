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

        public GrimoireHomeBridge(Root root)
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
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
                root.RequestGameOpen(credentials);
                return CreateResponse(
                    true,
                    root.IsGameReady
                        ? "Opening AQW and filling the selected account."
                        : "AQW is still loading. It will open automatically when ready.");
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
                    opened ? "Opening Client 01." : "Client 01 is not currently running.");
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

        private string CreateResponse(bool success, string message)
        {
            return JsonConvert.SerializeObject(new
            {
                success,
                message,
                gameReady = root.IsGameReady,
                gameLoadProgress = root.GameLoadProgress,
                clientRunning = root.IsClientRunning,
                quickBots = root.QuickBotNames,
                quickBotsLoading = root.IsQuickBotIndexing,
                version = root.AppVersion,
                accounts = AccountVault.GetAccounts()
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
                quickBots = new object[0],
                quickBotsLoading = false,
                version = root.AppVersion,
                accounts = new object[0]
            });
        }
    }
}
