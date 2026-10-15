using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Grimoire.Networking
{
    internal static class HandlerTask
    {
        public static void Observe(Task task, string handlerName)
        {
            task.ContinueWith(
                failed => Debug.WriteLine(
                    "Packet handler " + handlerName + " failed: " + failed.Exception?.GetBaseException()),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }
    }
}
