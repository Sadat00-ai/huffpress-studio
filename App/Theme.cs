using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace HuffPressStudio
{
    // Colours, fonts, DPI scaling and small drawing helpers shared by every control.
    internal static class Theme
    {
        public static readonly Color Background = Color.FromArgb(14, 17, 32);
        public static readonly Color Panel      = Color.FromArgb(24, 28, 52);
        public static readonly Color PanelLight = Color.FromArgb(34, 39, 72);
        public static readonly Color Border     = Color.FromArgb(52, 58, 100);
        public static readonly Color Text       = Color.FromArgb(235, 238, 255);
        public static readonly Color TextDim    = Color.FromArgb(150, 158, 200);
        public static readonly Color Cyan       = Color.FromArgb(0, 220, 255);
        public static readonly Color Blue       = Color.FromArgb(0, 150, 255);
        public static readonly Color Violet     = Color.FromArgb(150, 90, 255);
        public static readonly Color Pink       = Color.FromArgb(255, 90, 190);
        public static readonly Color Green      = Color.FromArgb(60, 220, 140);
        public static readonly Color Orange     = Color.FromArgb(255, 150, 60);

        private static readonly Dictionary<string, Font> FontCache = new Dictionary<string, Font>();

        // Screen scale: 1.0 at 96 dpi, 1.25 at 120 dpi, 1.5 at 144 dpi ...
        public static float Scale { get; private set; }

        static Theme()
        {
            Scale = 1f;
        }

        public static void Initialize()
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                Scale = g.DpiX / 96f;
            }
        }

        // Scales a design-time pixel value to the real screen.
        public static int S(int value)
        {
            return (int)Math.Round(value * Scale);
        }

        // fonts

        // Segoe UI font sized in design pixels (the controls apply the DPI scale themselves).
        public static Font UiFont(float pixels, bool bold)
        {
            return GetFont("Segoe UI", pixels, bold);
        }

        public static Font MonoFont(float pixels, bool bold)
        {
            return GetFont("Consolas", pixels, bold);
        }

        private static Font GetFont(string family, float pixels, bool bold)
        {
            string key = family + "|" + pixels.ToString(CultureInfo.InvariantCulture) + "|" + bold;
            Font font;
            if (!FontCache.TryGetValue(key, out font))
            {
                font = new Font(family, pixels, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
                FontCache[key] = font;
            }
            return font;
        }

        // colours and maths

        public static Color WithAlpha(Color c, int alpha)
        {
            if (alpha < 0) alpha = 0;
            if (alpha > 255) alpha = 255;
            return Color.FromArgb(alpha, c.R, c.G, c.B);
        }

        public static Color Mix(Color a, Color b, float t)
        {
            t = Clamp01(t);
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        public static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }

        // Moves a value a fraction of the way towards its target (smooth ease-out).
        public static float Approach(float current, float target, float rate)
        {
            float next = current + (target - current) * rate;
            return Math.Abs(target - next) < 0.002f ? target : next;
        }

        // shapes

        public static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            if (d <= 0f)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // Draws a rounded dark card with an optional small title. Returns the free area inside.
        public static RectangleF DrawCard(Graphics g, RectangleF rect, string title)
        {
            using (GraphicsPath path = RoundedRect(rect, 14f))
            using (SolidBrush fill = new SolidBrush(Panel))
            using (Pen border = new Pen(Border, 1f))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }

            if (string.IsNullOrEmpty(title))
            {
                return RectangleF.Inflate(rect, -14f, -14f);
            }

            using (SolidBrush dot = new SolidBrush(Cyan))
            using (SolidBrush text = new SolidBrush(TextDim))
            {
                g.FillEllipse(dot, rect.X + 16f, rect.Y + 19f, 7f, 7f);
                g.DrawString(title, UiFont(12f, true), text, rect.X + 30f, rect.Y + 14f);
            }
            return new RectangleF(rect.X + 14f, rect.Y + 44f, rect.Width - 28f, rect.Height - 56f);
        }

        // formatting

        public static string FormatBytes(double bytes)
        {
            string[] units = { "B", "KB", "MB", "GB" };
            int unit = 0;
            while (bytes >= 1024.0 && unit < units.Length - 1)
            {
                bytes /= 1024.0;
                unit++;
            }
            string format = unit == 0 ? "0" : "0.0";
            return bytes.ToString(format, CultureInfo.InvariantCulture) + " " + units[unit];
        }

        public static string FormatCount(long value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        // A short readable label for a byte value (a letter, or SP / LF / hex).
        public static string SymbolLabel(int value)
        {
            if (value == 32) return "SP";
            if (value == 10) return "LF";
            if (value == 13) return "CR";
            if (value == 9) return "TB";
            if (value > 32 && value < 127) return ((char)value).ToString();
            return value.ToString("X2", CultureInfo.InvariantCulture);
        }
    }
}
