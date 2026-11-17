using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace HuffPressStudio.Controls
{
    // Base class for all the custom controls. It gives them:
    //  - flicker-free double buffered painting,
    //  - a ~30 fps animation timer (Step), and
    //  - drawing in "design pixels" so the UI looks the same at any Windows DPI scale.
    internal abstract class AnimatedControl : Control
    {
        private readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer();

        protected AnimatedControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Background;
            _timer.Interval = 33;
            _timer.Tick += OnTimerTick;
        }

        // Width in design pixels.
        protected float LogicalWidth { get { return Width / Theme.Scale; } }

        // Height in design pixels.
        protected float LogicalHeight { get { return Height / Theme.Scale; } }

        // Advance the animation by one frame. Return true if the control must be repainted.
        protected abstract bool Step();

        // Draw the control. Coordinates are in design pixels.
        protected abstract void PaintScaled(Graphics g, SizeF size);

        protected PointF ToLogical(Point p)
        {
            return new PointF(p.X / Theme.Scale, p.Y / Theme.Scale);
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (Visible && Step()) Invalidate();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!DesignMode) _timer.Start();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _timer.Stop();
            base.OnHandleDestroyed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.ScaleTransform(Theme.Scale, Theme.Scale);
            PaintScaled(g, new SizeF(LogicalWidth, LogicalHeight));
        }
    }
}
