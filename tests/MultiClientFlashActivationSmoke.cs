using AxShockwaveFlashObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;

internal static class MultiClientFlashActivationSmoke
{
    private const int TimeoutMilliseconds = 30000;
    private const int ClientCount = 6;

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length != 1 || !File.Exists(args[0]))
        {
            Console.Error.WriteLine("A Grimoire SWF path is required.");
            return 2;
        }

        Form host = null;
        AxShockwaveFlash[] clients = new AxShockwaveFlash[ClientCount];
        try
        {
            int[] progress = new int[ClientCount];
            string loadError = null;
            host = new Form
            {
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Left = -32000,
                Top = -32000,
                Width = 960,
                Height = 620
            };
            for (int index = 0; index < ClientCount; index++)
            {
                int clientIndex = index;
                AxShockwaveFlash client = CreateControl(host, "Client" + (index + 1).ToString("00"));
                clients[index] = client;
                client.Dock = DockStyle.Fill;
                client.FlashCall += delegate(object sender, _IShockwaveFlashEvents_FlashCallEvent e)
                {
                    int reportedProgress;
                    string reportedError;
                    ReadProgress(sender, client, e, out reportedProgress, out reportedError);
                    if (reportedProgress >= 0)
                        progress[clientIndex] = reportedProgress;
                    if (reportedError != null)
                        loadError = reportedError;
                };
            }

            host.Show();
            Application.DoEvents();

            HashSet<IntPtr> handles = new HashSet<IntPtr>();
            for (int index = 0; index < clients.Length; index++)
            {
                IntPtr handle = clients[index].Handle;
                if (handle == IntPtr.Zero || !handles.Add(handle))
                    throw new InvalidOperationException("The six ActiveX clients did not receive independent window handles.");
            }

            byte[] swf = File.ReadAllBytes(args[0]);
            for (int index = 0; index < clients.Length; index++)
                LoadSwf(clients[index], swf);
            Stopwatch timeout = Stopwatch.StartNew();
            while (timeout.ElapsedMilliseconds < TimeoutMilliseconds
                && !AllClientsReportedProgress(progress)
                && loadError == null)
            {
                Application.DoEvents();
                Thread.Sleep(15);
            }

            if (loadError != null)
                throw new InvalidOperationException("An embedded SWF reported a load failure: " + loadError);
            if (!AllClientsReportedProgress(progress))
            {
                throw new TimeoutException(
                    "All six embedded SWFs did not report AQW download progress within 30 seconds. Progress: "
                        + FormatProgress(progress));
            }

            IntPtr[] survivingHandles = new IntPtr[ClientCount - 1];
            for (int index = 1; index < clients.Length; index++)
                survivingHandles[index - 1] = clients[index].Handle;
            clients[0].Dispose();
            clients[0] = null;
            for (int index = 1; index < clients.Length; index++)
            {
                if (clients[index].IsDisposed || clients[index].Handle != survivingHandles[index - 1])
                    throw new InvalidOperationException("Disposing one ActiveX client invalidated another client.");
            }

            Console.WriteLine("PASS: six app-local Flash controls load concurrently, keep distinct callback senders, and dispose independently.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            for (int index = 0; index < clients.Length; index++)
            {
                if (clients[index] != null)
                    clients[index].Dispose();
            }
            if (host != null)
                host.Dispose();
        }
    }

    private static bool AllClientsReportedProgress(int[] progress)
    {
        for (int index = 0; index < progress.Length; index++)
        {
            if (progress[index] <= 0)
                return false;
        }
        return true;
    }

    private static string FormatProgress(int[] progress)
    {
        string[] values = new string[progress.Length];
        for (int index = 0; index < progress.Length; index++)
            values[index] = "Client " + (index + 1).ToString("00") + "=" + progress[index] + "%";
        return string.Join(", ", values);
    }

    private static AxShockwaveFlash CreateControl(Form host, string name)
    {
        AxShockwaveFlash control = new AxShockwaveFlash { Name = name };
        ((ISupportInitialize)control).BeginInit();
        host.Controls.Add(control);
        ((ISupportInitialize)control).EndInit();
        return control;
    }

    private static void ReadProgress(
        object sender,
        AxShockwaveFlash expected,
        _IShockwaveFlashEvents_FlashCallEvent e,
        out int progress,
        out string loadError)
    {
        progress = -1;
        loadError = null;
        if (!ReferenceEquals(sender, expected))
            throw new InvalidOperationException("A Flash callback was raised by the wrong client control.");

        XElement request = XElement.Parse(e.request);
        string function = request.Attribute("name") == null
            ? string.Empty
            : request.Attribute("name").Value;
        string argument = request.Element("arguments") == null
            ? string.Empty
            : request.Element("arguments").Value;
        if (function == "progress")
            int.TryParse(argument, out progress);
        else if (function == "loadError")
            loadError = argument;
    }

    private static void LoadSwf(AxShockwaveFlash flash, byte[] swf)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(8 + swf.Length);
            writer.Write(1432769894);
            writer.Write(swf.Length);
            writer.Write(swf);
            writer.Seek(0, SeekOrigin.Begin);
            flash.OcxState = new AxHost.State(stream, 1, false, null);
        }
    }
}
