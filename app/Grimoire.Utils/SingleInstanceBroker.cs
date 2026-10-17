using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Grimoire.Utils
{
    /// <summary>
    /// Keeps one shell per Windows user and installation. Later launches send
    /// one bounded request to the existing shell; no account or bot data is
    /// transferred through the pipe.
    /// </summary>
    public sealed class SingleInstanceBroker : IDisposable
    {
        private const byte AddClientRequest = 1;
        private readonly object serverSync = new object();
        private readonly Mutex instanceMutex;
        private readonly string pipeName;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool ownsMutex;
        private bool disposed;
        private NamedPipeServerStream activeServer;
        private Task listenerTask;
        private Action launchRequest;

        private SingleInstanceBroker(string mutexName, string pipeName)
        {
            this.pipeName = pipeName;
            bool createdNew;
            instanceMutex = new Mutex(true, mutexName, out createdNew);
            ownsMutex = createdNew;
            if (!ownsMutex)
            {
                try
                {
                    ownsMutex = instanceMutex.WaitOne(0, false);
                }
                catch (AbandonedMutexException)
                {
                    ownsMutex = true;
                }
            }
        }

        public bool IsPrimaryInstance => ownsMutex;

        public static SingleInstanceBroker CreateForCurrentUser()
        {
            string userIdentity = WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName;
            string executablePath = Path.GetFullPath(Application.ExecutablePath)
                .ToUpperInvariant();
            string digest;
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(userIdentity + "|" + executablePath));
                digest = BitConverter.ToString(hash, 0, 12).Replace("-", string.Empty);
            }

            return new SingleInstanceBroker(
                "Local\\GrimoireExodus-" + digest,
                "GrimoireExodus-" + digest);
        }

        public void StartListening(Action onLaunchRequest)
        {
            if (!ownsMutex)
                throw new InvalidOperationException("Only the primary instance can listen for launch requests.");
            if (onLaunchRequest == null)
                throw new ArgumentNullException(nameof(onLaunchRequest));
            if (listenerTask != null)
                return;

            launchRequest = onLaunchRequest;
            listenerTask = Task.Factory.StartNew(
                ListenLoop,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public bool ForwardLaunchRequest(TimeSpan timeout)
        {
            if (ownsMutex || timeout <= TimeSpan.Zero)
                return false;

            Stopwatch timer = Stopwatch.StartNew();
            while (timer.Elapsed < timeout)
            {
                int remaining = (int)Math.Max(1, (timeout - timer.Elapsed).TotalMilliseconds);
                try
                {
                    using (NamedPipeClientStream client = new NamedPipeClientStream(
                        ".",
                        pipeName,
                        PipeDirection.Out,
                        PipeOptions.None,
                        TokenImpersonationLevel.Identification))
                    {
                        client.Connect(Math.Min(remaining, 300));
                        client.WriteByte(AddClientRequest);
                        client.Flush();
                        return true;
                    }
                }
                catch (TimeoutException)
                {
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }

                if (timer.Elapsed < timeout)
                    Thread.Sleep(Math.Min(100, remaining));
            }
            return false;
        }

        private void ListenLoop()
        {
            while (!cancellation.IsCancellationRequested)
            {
                NamedPipeServerStream server = null;
                try
                {
                    server = CreateServer();
                    lock (serverSync)
                    {
                        if (cancellation.IsCancellationRequested)
                        {
                            server.Dispose();
                            return;
                        }
                        activeServer = server;
                    }

                    server.WaitForConnection();
                    int request = server.ReadByte();
                    if (request == AddClientRequest && !cancellation.IsCancellationRequested)
                        launchRequest?.Invoke();
                }
                catch (IOException) when (cancellation.IsCancellationRequested)
                {
                    return;
                }
                catch (ObjectDisposedException) when (cancellation.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    Debug.WriteLine("Single-instance broker request failed: " + exception);
                    if (!cancellation.IsCancellationRequested)
                        Thread.Sleep(100);
                }
                finally
                {
                    lock (serverSync)
                    {
                        if (ReferenceEquals(activeServer, server))
                            activeServer = null;
                    }
                    server?.Dispose();
                }
            }
        }

        private NamedPipeServerStream CreateServer()
        {
            SecurityIdentifier currentUser = WindowsIdentity.GetCurrent().User;
            PipeSecurity security = new PipeSecurity();
            security.SetAccessRuleProtection(true, false);
            if (currentUser != null)
            {
                security.SetOwner(currentUser);
                security.AddAccessRule(new PipeAccessRule(
                    currentUser,
                    PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance,
                    AccessControlType.Allow));
            }

            return new NamedPipeServerStream(
                pipeName,
                PipeDirection.In,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.None,
                0,
                0,
                security);
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            cancellation.Cancel();
            lock (serverSync)
            {
                activeServer?.Dispose();
                activeServer = null;
            }

            try
            {
                listenerTask?.Wait(1000);
            }
            catch (AggregateException)
            {
            }

            cancellation.Dispose();
            if (ownsMutex)
            {
                try
                {
                    instanceMutex.ReleaseMutex();
                }
                catch (ApplicationException)
                {
                }
                ownsMutex = false;
            }
            instanceMutex.Dispose();
        }
    }
}
