using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Grimoire.UI
{
    internal enum AccountProfileCaptureDisposition
    {
        Retry,
        Pause,
        Complete
    }

    internal static class AccountProfileCapturePolicy
    {
        public const int MaximumAttempts = 20;

        public static AccountProfileCaptureDisposition Evaluate(
            bool hasExistingPortrait,
            bool capturedPortrait,
            int attempts)
        {
            if (hasExistingPortrait || capturedPortrait)
                return AccountProfileCaptureDisposition.Complete;
            return attempts >= MaximumAttempts
                ? AccountProfileCaptureDisposition.Pause
                : AccountProfileCaptureDisposition.Retry;
        }
    }

    internal static class AccountCharacterVisual
    {
        private const uint PrintClientOnly = 1;
        private const int OutputSize = 96;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PrintWindow(IntPtr window, IntPtr deviceContext, uint flags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public static byte[] TryCapture(Control clientControl)
        {
            if (clientControl == null
                || clientControl.IsDisposed
                || !clientControl.IsHandleCreated
                || !clientControl.Visible
                || clientControl.ClientSize.Width < 160
                || clientControl.ClientSize.Height < 120)
            {
                return null;
            }

            try
            {
                using (Bitmap surface = CaptureSurface(clientControl))
                {
                    if (surface == null)
                        return null;
                    int portraitSize = Math.Max(
                        48,
                        Math.Min(
                            (int)Math.Round(surface.Width * 0.14d),
                            (int)Math.Round(surface.Height * 0.24d)));
                    portraitSize = Math.Min(portraitSize, Math.Min(surface.Width, surface.Height));
                    Rectangle source = new Rectangle(0, 0, portraitSize, portraitSize);
                    using (Bitmap portrait = new Bitmap(OutputSize, OutputSize, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics graphics = Graphics.FromImage(portrait))
                        {
                            graphics.Clear(Color.FromArgb(28, 32, 40));
                            graphics.CompositingQuality = CompositingQuality.HighQuality;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            graphics.DrawImage(
                                surface,
                                new Rectangle(0, 0, OutputSize, OutputSize),
                                source,
                                GraphicsUnit.Pixel);
                        }
                        if (!HasMeaningfulContent(portrait))
                            return null;
                        using (MemoryStream stream = new MemoryStream())
                        {
                            portrait.Save(stream, ImageFormat.Png);
                            return stream.ToArray();
                        }
                    }
                }
            }
            catch (Exception exception) when (
                exception is ArgumentException
                || exception is ExternalException
                || exception is InvalidOperationException)
            {
                return null;
            }
        }

        private static Bitmap CaptureSurface(Control control)
        {
            Bitmap bitmap = new Bitmap(
                control.ClientSize.Width,
                control.ClientSize.Height,
                PixelFormat.Format32bppArgb);
            bool captured = false;
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                IntPtr deviceContext = graphics.GetHdc();
                try
                {
                    captured = PrintWindow(control.Handle, deviceContext, PrintClientOnly);
                }
                finally
                {
                    graphics.ReleaseHdc(deviceContext);
                }
            }
            if (captured && HasMeaningfulContent(bitmap))
                return bitmap;

            try
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                    graphics.Clear(Color.Black);
                control.DrawToBitmap(bitmap, control.ClientRectangle);
                captured = HasMeaningfulContent(bitmap);
            }
            catch (Exception exception) when (
                exception is ArgumentException
                || exception is ExternalException
                || exception is InvalidOperationException)
            {
                captured = false;
            }
            if (captured)
                return bitmap;

            if (TryCaptureFromScreen(control, bitmap))
                return bitmap;
            bitmap.Dispose();
            return null;
        }

        private static bool TryCaptureFromScreen(Control control, Bitmap bitmap)
        {
            Form form = control.FindForm();
            if (form == null
                || !form.Visible
                || !form.IsHandleCreated
                || form.WindowState == FormWindowState.Minimized
                || GetForegroundWindow() != form.Handle)
            {
                return false;
            }

            Rectangle screenBounds;
            try
            {
                screenBounds = control.RectangleToScreen(control.ClientRectangle);
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            bool intersectsScreen = false;
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.Bounds.IntersectsWith(screenBounds))
                {
                    intersectsScreen = true;
                    break;
                }
            }
            if (!intersectsScreen)
                return false;

            try
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(Color.Black);
                    graphics.CopyFromScreen(
                        screenBounds.Location,
                        Point.Empty,
                        bitmap.Size,
                        CopyPixelOperation.SourceCopy);
                }
                return HasMeaningfulContent(bitmap);
            }
            catch (Exception exception) when (
                exception is ArgumentException
                || exception is ExternalException
                || exception is InvalidOperationException)
            {
                return false;
            }
        }

        private static bool HasMeaningfulContent(Bitmap bitmap)
        {
            HashSet<int> colors = new HashSet<int>();
            int minimumBrightness = 255;
            int maximumBrightness = 0;
            for (int y = 0; y < bitmap.Height; y += 6)
            {
                for (int x = 0; x < bitmap.Width; x += 6)
                {
                    Color color = bitmap.GetPixel(x, y);
                    int brightness = (color.R * 299 + color.G * 587 + color.B * 114) / 1000;
                    minimumBrightness = Math.Min(minimumBrightness, brightness);
                    maximumBrightness = Math.Max(maximumBrightness, brightness);
                    colors.Add((color.R >> 4) << 8 | (color.G >> 4) << 4 | (color.B >> 4));
                }
            }
            return colors.Count >= 12 && maximumBrightness - minimumBrightness >= 35;
        }
    }
}
