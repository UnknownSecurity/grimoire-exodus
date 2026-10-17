using Grimoire.Utils;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

internal static class SingleInstanceBrokerSmoke
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 1 && args[0] == "--secondary")
            {
                using (SingleInstanceBroker secondary = SingleInstanceBroker.CreateForCurrentUser())
                {
                    if (secondary.IsPrimaryInstance)
                        throw new InvalidOperationException("The second process unexpectedly became the primary instance.");
                    if (!secondary.ForwardLaunchRequest(TimeSpan.FromSeconds(5)))
                        throw new InvalidOperationException("The second process could not forward its launch request.");
                }
                return 0;
            }

            ManualResetEventSlim requestReceived = new ManualResetEventSlim(false);
            using (SingleInstanceBroker primary = SingleInstanceBroker.CreateForCurrentUser())
            {
                if (!primary.IsPrimaryInstance)
                    throw new InvalidOperationException("The first process did not acquire the instance lease.");
                primary.StartListening(requestReceived.Set);

                ProcessStartInfo startInfo = new ProcessStartInfo(
                    Application.ExecutablePath,
                    "--secondary")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (Process secondaryProcess = Process.Start(startInfo))
                {
                    if (secondaryProcess == null || !secondaryProcess.WaitForExit(10000))
                        throw new TimeoutException("The second process did not finish in time.");
                    if (secondaryProcess.ExitCode != 0)
                        throw new InvalidOperationException("The second process failed with exit code " + secondaryProcess.ExitCode + ".");
                }

                if (!requestReceived.Wait(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("The primary process did not receive the forwarded launch request.");
            }

            using (SingleInstanceBroker reacquired = SingleInstanceBroker.CreateForCurrentUser())
            {
                if (!reacquired.IsPrimaryInstance)
                    throw new InvalidOperationException("The instance lease was not released during shutdown.");
            }

            Console.WriteLine("PASS: a second process forwards one bounded launch request and the instance lease is released cleanly.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
