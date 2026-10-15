using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Grimoire.Utils
{
    /// <summary>
    /// Batches text produced by worker threads and keeps WinForms text controls bounded.
    /// </summary>
    internal sealed class BoundedTextBuffer
    {
        private readonly TextBoxBase control;
        private readonly int maximumCharacters;
        private readonly ConcurrentQueue<string> pending = new ConcurrentQueue<string>();
        private int drainScheduled;

        public BoundedTextBuffer(TextBoxBase control, int maximumCharacters)
        {
            this.control = control ?? throw new ArgumentNullException(nameof(control));
            this.maximumCharacters = Math.Max(4096, maximumCharacters);
            this.control.HandleCreated += (sender, args) => ScheduleDrain();
        }

        public void Append(string text)
        {
            if (string.IsNullOrEmpty(text) || control.IsDisposed)
            {
                return;
            }

            pending.Enqueue(text);
            ScheduleDrain();
        }

        public void Clear()
        {
            while (pending.TryDequeue(out _))
            {
            }

            RunOnUiThread(() => control.Clear());
        }

        private void ScheduleDrain()
        {
            if (Interlocked.CompareExchange(ref drainScheduled, 1, 0) != 0)
            {
                return;
            }

            RunOnUiThread(Drain);
        }

        private void RunOnUiThread(Action action)
        {
            if (control.IsDisposed || !control.IsHandleCreated)
            {
                Interlocked.Exchange(ref drainScheduled, 0);
                return;
            }

            try
            {
                if (control.InvokeRequired)
                {
                    control.BeginInvoke(action);
                }
                else
                {
                    action();
                }
            }
            catch (InvalidOperationException)
            {
                Interlocked.Exchange(ref drainScheduled, 0);
            }
        }

        private void Drain()
        {
            if (control.IsDisposed)
            {
                Interlocked.Exchange(ref drainScheduled, 0);
                return;
            }

            StringBuilder batch = new StringBuilder();
            while (batch.Length < 262144 && pending.TryDequeue(out string text))
            {
                batch.Append(text);
            }

            if (batch.Length > 0)
            {
                control.AppendText(batch.ToString());
                TrimToLimit();
            }

            Interlocked.Exchange(ref drainScheduled, 0);
            if (!pending.IsEmpty)
            {
                ScheduleDrain();
            }
        }

        private void TrimToLimit()
        {
            int excess = control.TextLength - maximumCharacters;
            if (excess <= 0)
            {
                return;
            }

            int removeCount = excess;
            string leadingText = control.Text.Substring(0, Math.Min(control.TextLength, excess + 2048));
            int newline = leadingText.IndexOf('\n', excess);
            if (newline >= 0)
            {
                removeCount = newline + 1;
            }

            control.Select(0, Math.Min(removeCount, control.TextLength));
            control.SelectedText = string.Empty;
            control.SelectionStart = control.TextLength;
        }
    }
}
