using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;

namespace OpenRouterBudget;

/// <summary>
/// Tray icons must match the shell's small-icon pixel size at the current DPI (SM_CXSMICON × DPI).
/// Drawing huge bitmaps and letting Windows scale them down causes blur/smeared text — unlike Explorer's clock,
/// <see cref="NotifyIcon"/> only accepts an <see cref="Icon"/>, not live text.
/// </summary>
internal static class TrayIconRenderer
{
    /// <summary>Short label, no $ — uses Windows regional format (e.g. 0,00 vs 0.00).</summary>
    public static string FormatTrayAmount(double amount)
    {
        CultureInfo c = CultureInfo.CurrentCulture;
        if (amount >= 10_000)
            return $"{(amount / 1000).ToString("F0", c)}k";
        if (amount >= 1000)
            return $"{(amount / 1000).ToString("F1", c)}k";
        if (amount >= 100)
            return amount.ToString("F0", c);
        if (amount >= 10)
            return amount.ToString("F0", c);
        if (amount >= 1)
            return amount.ToString("F1", c);
        return amount.ToString("F2", c);
    }

    /// <summary>Tray shows today’s spend only (larger text); remaining balance stays in tooltip / menu.</summary>
    public static Icon CreateBudgetIcon(double todaySpend)
    {
        string text = FormatTrayAmount(todaySpend);

        int s = TrayShellMetrics.TrayIconSidePixels;
        using var bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb);
        bmp.SetResolution(TrayShellMetrics.EffectiveDpi, TrayShellMetrics.EffectiveDpi);

        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using var back = new SolidBrush(Color.FromArgb(252, 26, 26, 30));
            g.FillRectangle(back, 0, 0, s, s);

            DrawFittedLine(g, text, new Rectangle(0, 0, s, s), FontStyle.Bold, Color.White);
        }

        return CreateIconFromBitmap(bmp, s);
    }

    public static Icon CreateErrorIcon()
    {
        int s = TrayShellMetrics.TrayIconSidePixels;
        using var bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb);
        bmp.SetResolution(TrayShellMetrics.EffectiveDpi, TrayShellMetrics.EffectiveDpi);

        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using var back = new SolidBrush(Color.FromArgb(252, 48, 22, 22));
            g.FillRectangle(back, 0, 0, s, s);
            DrawFittedLine(g, "!", new Rectangle(0, 0, s, s), FontStyle.Bold, Color.FromArgb(255, 255, 200, 200));
        }

        return CreateIconFromBitmap(bmp, s);
    }

    private static Icon CreateIconFromBitmap(Bitmap bmp, int side)
    {
        nint hIcon = bmp.GetHicon();
        using var tmp = Icon.FromHandle(hIcon);
        // Clone so tmp.Dispose can DestroyIcon the bitmap-derived handle without affecting the tray icon.
        return new Icon(tmp, side, side);
    }

    private static void DrawFittedLine(Graphics g, string text, Rectangle bounds, FontStyle style, Color color)
    {
        // Do not use EndEllipsis — it turns values like "0,00" into "0..." in a tight tray rect.
        const TextFormatFlags flags = TextFormatFlags.HorizontalCenter
            | TextFormatFlags.VerticalCenter
            | TextFormatFlags.NoPadding
            | TextFormatFlags.SingleLine
            | TextFormatFlags.NoPrefix;

        float maxPt = Math.Max(5f, bounds.Height * 0.82f);
        for (float px = maxPt; px >= 4f; px -= 0.5f)
        {
            using var font = new Font(TrayShellMetrics.UiFontFamily, px, style, GraphicsUnit.Pixel);
            Size sz = TextRenderer.MeasureText(g, text, font, Size.Empty, flags);
            if (sz.Width <= bounds.Width && sz.Height <= bounds.Height)
            {
                TextRenderer.DrawText(g, text, font, bounds, color, flags);
                return;
            }
        }

        using var tiny = new Font(TrayShellMetrics.UiFontFamily, 4f, style, GraphicsUnit.Pixel);
        TextRenderer.DrawText(g, text, tiny, bounds, color, flags);
    }

    private static class TrayShellMetrics
    {
        private const int SM_CXSMICON = 49;

        private static readonly Lazy<string> _uiFont = new(() =>
        {
            try
            {
                using var _ = new Font("Segoe UI", 12f, FontStyle.Regular, GraphicsUnit.Pixel);
                return "Segoe UI";
            }
            catch
            {
                return "Tahoma";
            }
        });

        public static string UiFontFamily => _uiFont.Value;

        public static int EffectiveDpi
        {
            get
            {
                uint d = GetPrimaryMonitorDpi();
                return d > 0 ? (int)d : 96;
            }
        }

        public static int TrayIconSidePixels
        {
            get
            {
                uint dpi = GetPrimaryMonitorDpi();
                if (dpi == 0) dpi = 96;

                try
                {
                    int px = GetSystemMetricsForDpi(SM_CXSMICON, dpi);
                    if (px > 0)
                        return Math.Clamp(px, 16, 64);
                }
                catch (DllNotFoundException) { }
                catch (EntryPointNotFoundException) { }

                return Math.Clamp((int)Math.Round(16d * dpi / 96d), 16, 64);
            }
        }

        private static uint GetPrimaryMonitorDpi()
        {
            try
            {
                var pt = new POINT { X = 0, Y = 0 };
                nint hMon = MonitorFromPoint(pt, MONITOR_DEFAULTTOPRIMARY);
                if (hMon != 0 && GetDpiForMonitor(hMon, 0, out uint dpx, out _) == 0 && dpx > 0)
                    return dpx;
            }
            catch { }

            try
            {
                using var g = Graphics.FromHwnd(nint.Zero);
                return (uint)Math.Round(g.DpiX);
            }
            catch
            {
                return 96;
            }
        }

        private const uint MONITOR_DEFAULTTOPRIMARY = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern nint MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(nint hmonitor, int dpiType, out uint dpiX, out uint dpiY);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetricsForDpi(int nIndex, uint dpi);
    }
}
