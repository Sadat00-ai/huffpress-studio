using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace HuffPressStudio.Controls
{
    internal enum StatusKind
    {
        Info,
        Success,
        Error
    }

    // Bottom bar with a coloured status dot, a fading message and a sweeping "busy" light.
    internal sealed class StatusFooter : AnimatedControl
    {
        private string _message = "Ready";
        private StatusKind _kind = StatusKind.Info;
        private bool _busy;
        private float _fade = 1f;
        private float _sweep;

        public void SetMessage(string message, StatusKind kind)
        {
            _message = message;
            _kind = kind;
            _fade = 0f;
            Invalidate();
        }

        public void SetBusy(bool busy)
        {
            _busy = busy;
            Invalidate();
        }

        protected override bool Step()
        {
            bool changed = false;
            if (_fade < 1f) { _fade = Math.Min(1f, _fade + 0.08f); changed = true; }
            if (_busy)
            {
                _sweep += 0.02f;
                if (_sweep > 1.3f) _sweep = -0.3f;
                changed = true;
            }
            return changed;
        }

        protected override void PaintScaled(Graphics g, SizeF size)
        {
            RectangleF area = new RectangleF(0f, 0f, size.Width, size.Height);
            using (SolidBrush back = new SolidBrush(Theme.Panel))
            using (Pen line = new Pen(Theme.Border, 1f))
            {
                g.FillRectangle(back, area);
                g.DrawLine(line, 0f, 0.5f, size.Width, 0.5f);
            }

            Color dotColour = _kind == StatusKind.Success ? Theme.Green : (_kind == StatusKind.Error ? Theme.Pink : Theme.Cyan);
            using (SolidBrush dot = new SolidBrush(dotColour))
            using (SolidBrush glow = new SolidBrush(Theme.WithAlpha(dotColour, 60)))
            using (SolidBrush text = new SolidBrush(Theme.WithAlpha(Theme.Text, (int)(255 * _fade))))
            using (StringFormat format = new StringFormat())
            {
                g.FillEllipse(glow, 14f, size.Height / 2f - 8f, 16f, 16f);
                g.FillEllipse(dot, 18f, size.Height / 2f - 4f, 8f, 8f);

                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisCharacter;
                format.FormatFlags = StringFormatFlags.NoWrap;
                g.DrawString(_message, Theme.UiFont(13f, false), text,
                             new RectangleF(40f, 0f, size.Width - 56f, size.Height), format);
            }

            if (_busy)
            {
                float x = _sweep * size.Width;
                RectangleF beam = new RectangleF(x - size.Width * 0.15f, 0f, size.Width * 0.3f, 3f);
                using (LinearGradientBrush brush = new LinearGradientBrush(beam, Color.FromArgb(0, Theme.Cyan), Theme.Cyan, 0f))
                {
                    ColorBlend blend = new ColorBlend(3);
                    blend.Colors = new Color[] { Color.FromArgb(0, Theme.Cyan), Theme.Pink, Color.FromArgb(0, Theme.Cyan) };
                    blend.Positions = new float[] { 0f, 0.5f, 1f };
                    brush.InterpolationColors = blend;
                    g.FillRectangle(brush, beam);
                }
            }
        }
    }
}
