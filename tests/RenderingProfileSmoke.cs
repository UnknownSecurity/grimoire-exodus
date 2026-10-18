using AxShockwaveFlashObjects;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;

internal static class RenderingProfileSmoke
{
    private const int TimeoutMilliseconds = 30000;

    [STAThread]
    private static int Main(string[] args)
    {
        if ((args.Length != 3 && args.Length != 4) || !File.Exists(args[0]))
        {
            Console.Error.WriteLine("Usage: RenderingProfileSmoke <swf> <automatic|direct|gpu> <automatic|high|medium|low> [property-only]");
            return 2;
        }

        string renderingProfile = args[1].ToLowerInvariant();
        string renderQuality = args[2];
        Form host = null;
        AxShockwaveFlash flash = null;
        try
        {
            int progress = 0;
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
            flash = new AxShockwaveFlash { Dock = DockStyle.Fill, Name = "RenderingProfileProbe" };
            ((ISupportInitialize)flash).BeginInit();
            host.Controls.Add(flash);
            ((ISupportInitialize)flash).EndInit();
            flash.FlashCall += delegate(object sender, _IShockwaveFlashEvents_FlashCallEvent e)
            {
                XElement request = XElement.Parse(e.request);
                XAttribute name = request.Attribute("name");
                XElement arguments = request.Element("arguments");
                string function = name == null ? null : name.Value;
                string argument = arguments == null ? string.Empty : arguments.Value;
                if (function == "progress")
                    int.TryParse(argument, out progress);
                else if (function == "loadError")
                    loadError = argument;
            };

            host.Show();
            Application.DoEvents();
            if (renderingProfile != "automatic")
                flash.WMode = renderingProfile;
            if (!renderQuality.Equals("automatic", StringComparison.OrdinalIgnoreCase))
                flash.Quality2 = renderQuality;

            if (renderingProfile != "automatic"
                && !string.Equals(flash.WMode, renderingProfile, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Flash did not retain the requested window mode.");
            }
            if (!renderQuality.Equals("automatic", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(flash.Quality2, renderQuality, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Flash did not retain the requested render quality.");
            }
            if (args.Length == 4 && args[3].Equals("property-only", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(
                    "PASS: " + renderingProfile + " rendering with " + renderQuality
                        + " quality was accepted by the bundled ActiveX runtime.");
                return 0;
            }

            LoadSwf(flash, File.ReadAllBytes(args[0]));
            Stopwatch timeout = Stopwatch.StartNew();
            while (timeout.ElapsedMilliseconds < TimeoutMilliseconds && progress <= 0 && loadError == null)
            {
                Application.DoEvents();
                Thread.Sleep(15);
            }

            if (loadError != null)
                throw new InvalidOperationException("The embedded SWF reported a load failure: " + loadError);
            if (progress <= 0)
                throw new TimeoutException("The rendering profile did not report AQW download progress within 30 seconds.");

            Console.WriteLine(
                "PASS: " + renderingProfile + " rendering with " + renderQuality
                    + " quality initialized and reported AQW load progress.");
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
