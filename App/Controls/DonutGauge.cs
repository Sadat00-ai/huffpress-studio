using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace HuffPressStudio.Controls
{
    // A ring gauge that sweeps to the "space saved" percentage, with a slowly spinning dotted halo.
    internal sealed class DonutGauge : AnimatedControl
    {
        private float _target;      // 0..1
        private float _shown;
        private float _spin;
        private string _caption = "run Compress";
        private Color _accentA = Theme.Cyan;
        private Color _accentB = Theme.Pink;

        // Sets the fraction (0..1) to display. Negative savings are shown as an empty ring.
        public void SetValue(double fraction, string caption)
        {
            _target = Theme.Clamp01((float)fraction);
            _caption = caption;
            if (fraction > 0.0)
            {
                _accentA = Theme.Cyan;
                _accentB = Theme.Pink;
            }
            else
            {
                _accentA = Theme.Orange;
                _accentB = Theme.Orange;
            }
            Invalidate();
        }

        public void Reset()
        {
            _target = 0f;
            _caption = "run Compress";
            _accentA = Theme.Cyan;
            _accentB = Theme.Pink;
            Invalidate();
        }

        protected override bool Step()
        {
            _spin += 0.4f;
            _shown = Theme.Approach(_shown, _target, 0.07f);
            return true;
        }

        protected override void PaintScaled(Graphics g, SizeF size)
        {
            RectangleF content = Theme.DrawCard(g, new RectangleF(0.5f, 0.5f, size.Width - 1f, size.Height - 1f), "SPACE SAVED");

            float radius = Math.Min(content.Width, content.Height) / 2f - 12f;
            if (radius < 20f) return;
            PointF centre = new PointF(content.X + content.Width / 2f, content.Y + content.Height / 2f);
            RectangleF ring = new RectangleF(centre.X - radius, centre.Y - radius, radius * 2f, radius * 2f);

            DrawHalo(g, centre, radius + 11f);
            DrawRing(g, ring);
            DrawCentreText(g, centre);
        }

        private void DrawHalo(Graphics g, PointF centre, float haloRadius)
        {
            for (int i = 0; i < 48; i++)
            {
                double angle = (i * 7.5 + _spin) * Math.PI / 180.0;
                float x = centre.X + (float)Math.Cos(angle) * haloRadius;
                float y = centre.Y + (float)Math.Sin(angle) * haloRadius;
                int alpha = 40 + (int)(90 * (0.5 + 0.5 * Math.Sin(angle * 2.0 - _spin * 0.05)));
                using (SolidBrush dot = new SolidBrush(Theme.WithAlpha(Theme.Violet, alpha)))
                {
                    g.FillEllipse(dot, x - 1.4f, y - 1.4f, 2.8f, 2.8f);
                }
            }
        }

        private void DrawRing(Graphics g, RectangleF ring)
        {
            using (Pen track = new Pen(Theme.PanelLight, 14f))
            {
                g.DrawEllipse(track, ring);
            }

            float sweep = 360f * _shown;
            if (sweep < 1f) return;

            using (Pen glow = new Pen(Theme.WithAlpha(_accentA, 45), 26f))
            using (LinearGradientBrush brush = new LinearGradientBrush(ring, _accentA, _accentB, 45f))
            using (Pen arc = new Pen(brush, 14f))
            {
                glow.StartCap = LineCap.Round;
                glow.EndCap = LineCap.Round;
                arc.StartCap = LineCap.Round;
                arc.EndCap = LineCap.Round;
                g.DrawArc(glow, ring, -90f, sweep);
                g.DrawArc(arc, ring, -90f, sweep);
            }
        }

        private void DrawCentreText(Graphics g, PointF centre)
        {
            using (StringFormat middle = new StringFormat())
            using (SolidBrush big = new SolidBrush(Theme.Text))
            using (SolidBrush small = new SolidBrush(Theme.TextDim))
            {
                middle.Alignment = StringAlignment.Center;
                middle.LineAlignment = StringAlignment.Center;
                string percent = Math.Round(_shown * 100f).ToString() + "%";
                g.DrawString(percent, Theme.UiFont(34f, true), big,
                             new RectangleF(centre.X - 70f, centre.Y - 26f, 140f, 44f), middle);
                g.DrawString(_caption, Theme.UiFont(12f, false), small,
                             new RectangleF(centre.X - 70f, centre.Y + 14f, 140f, 20f), middle);
            }
        }
    }
}
