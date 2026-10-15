using Grimoire.Networking;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Grimoire.Tools
{
    public class Spammer
    {
        public List<string> _packets;

        private int _delay;

        private CancellationTokenSource _cancellationTokenSource;

        private Task _spamTask;

        public static Spammer Instance
        {
            get;
        } = new Spammer();

        public event Action<int> IndexChanged;

        public Spammer()
        {
        }

        public void Start(List<string> packets, int delay)
        {
            Stop();
            _packets = packets == null ? new List<string>() : new List<string>(packets);
            _delay = delay;
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;
            List<string> packetSnapshot = _packets;
            int delaySnapshot = Math.Max(1, _delay);
            _spamTask = Task.Run(() => Spam(packetSnapshot, delaySnapshot, token), token);
            _spamTask.ContinueWith(
                task => System.Diagnostics.Debug.WriteLine("Packet spammer failed: " + task.Exception?.GetBaseException()),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel(throwOnFirstException: false);
        }

        private async Task Spam(IReadOnlyList<string> packets, int delay, CancellationToken token)
        {
            if (packets == null || packets.Count == 0)
            {
                return;
            }

            int index = 0;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (index >= packets.Count)
                    {
                        index = 0;
                    }
                    this.IndexChanged?.Invoke(index);
                    await Proxy.Instance.SendToServer(packets[index++]);
                    await Task.Delay(delay, token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
        }
    }
}
