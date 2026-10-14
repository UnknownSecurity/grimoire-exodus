using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using AxShockwaveFlashObjects;

internal static class FlashActivationSmoke
{
    [STAThread]
    private static int Main()
    {
        Form host = null;
        AxShockwaveFlash flash = null;
        try
        {
            host = new Form();
            flash = new AxShockwaveFlash();
            ((ISupportInitialize)flash).BeginInit();
            host.Controls.Add(flash);
            ((ISupportInitialize)flash).EndInit();
            host.CreateControl();
            flash.CreateControl();

            string expected = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Flash.ocx"));
            foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
            {
                if (!module.ModuleName.EndsWith(".ocx", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string loaded = Path.GetFullPath(module.FileName);
                Console.WriteLine(loaded);
                return string.Equals(expected, loaded, StringComparison.OrdinalIgnoreCase) ? 0 : 2;
            }

            Console.Error.WriteLine("Flash COM activation succeeded, but no OCX module was found.");
            return 3;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            if (flash != null)
            {
                flash.Dispose();
            }
            if (host != null)
            {
                host.Dispose();
            }
        }
    }
}
