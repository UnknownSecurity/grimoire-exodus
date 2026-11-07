using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Grimoire.Clients
{
    /// <summary>
    /// Lets Ruffle reuse an existing WebView2 surface. The modern shell
    /// implements this boundary so a Ruffle client does not start a second
    /// Chromium browser inside the 32-bit desktop process.
    /// </summary>
    public interface IRuffleFrameHost
    {
        Control InvocationControl { get; }

        Task<IRuffleFrame> AttachRuffleFrameAsync(
            string frameName,
            string sessionId,
            Uri source,
            object hostObject);

        void DetachRuffleFrame(string frameName);
    }

    public interface IRuffleFrame : IDisposable
    {
        event Action<Exception> Failed;

        Task<string> ExecuteScriptAsync(string script);
    }
}
