using AxShockwaveFlashObjects;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;

internal static class FlashLoadProgressSmoke
{
    private const int TimeoutMilliseconds = 30000;

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length < 1 || args.Length > 2 || !File.Exists(args[0]))
        {
            Console.Error.WriteLine("A Grimoire SWF path is required.");
            return 2;
        }

        Form host = null;
        AxShockwaveFlash flash = null;
        try
        {
            string loadError = null;
            int progress = 0;
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
            bool showFlash = args.Length == 2 && string.Equals(args[1], "--visible", StringComparison.OrdinalIgnoreCase);
            flash = new AxShockwaveFlash { Dock = DockStyle.Fill, Visible = showFlash };
            ((ISupportInitialize)flash).BeginInit();
            host.Controls.Add(flash);
            ((ISupportInitialize)flash).EndInit();
            flash.FlashCall += delegate(object sender, _IShockwaveFlashEvents_FlashCallEvent e)
            {
                XElement request = XElement.Parse(e.request);
                XAttribute nameAttribute = request.Attribute("name");
                XElement argumentsElement = request.Element("arguments");
                string function = nameAttribute == null ? string.Empty : nameAttribute.Value;
                string argument = argumentsElement == null ? string.Empty : argumentsElement.Value;
                Console.WriteLine(function + ": " + argument);
                if (function == "progress")
                    int.TryParse(argument, out progress);
                else if (function == "loadError")
                    loadError = argument;
            };

            host.Show();
            Application.DoEvents();
            LoadSwf(flash, args[0]);
            Console.WriteLine("ActiveX ready state after load: " + flash.ReadyState + "; playing: " + flash.Playing);

            Stopwatch timeout = Stopwatch.StartNew();
            while (timeout.ElapsedMilliseconds < TimeoutMilliseconds && progress <= 0 && loadError == null)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(15);
            }

            if (loadError != null)
                throw new InvalidOperationException("The embedded SWF reported a load failure: " + loadError);
            if (progress <= 0)
                throw new TimeoutException("The embedded SWF remained at 0% for 30 seconds.");

            Console.WriteLine("PASS: embedded SWF reported AQW download progress above 0%.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            if (flash != null)
                flash.Dispose();
            if (host != null)
                host.Dispose();
        }
    }

    private static void LoadSwf(AxShockwaveFlash flash, string swfPath)
    {
        byte[] swf = File.ReadAllBytes(swfPath);
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
