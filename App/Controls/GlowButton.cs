using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HuffPressStudio.Controls
{
    // Gradient button with an icon, a glow on hover and a press effect.
    internal sealed class GlowButton : AnimatedControl
    {
        private float _hover;
        private bool _hovering;
        private bool _pressed;

        public Image Icon { get; set; }
        public Color Accent1 { get; set; }
        public Color Accent2 { get; set; }

        public GlowButton()
        {
            Accent1 = Theme.Cyan;
            Accent2 = Theme.Violet;
            Cursor = Cursors.Hand;
            TabStop = true;
            SetStyle(ControlStyles.Selectable, true);
        }

        protected override bool Step()
        {
            float target = (_hovering && Enabled) ? 1f : 0f;
            if (Math.Abs(_hover - target) < 0.001f) return false;
            _hover = Theme.Approach(_hover, target, 0.25f);
            return true;
        }

        protected override void PaintScaled(Graphics g, SizeF size)
        {
            RectangleF rect = new RectangleF(4f, 4f, size.Width - 8f, size.Height - 8f);
            if (_pressed && Enabled) rect.Offset(0f, 1.5f);

            // outer glow that grows on hover
            if (Enabled && _hover > 0.01f)
            {
                for (int i = 3; i >= 1; i--)
                {
                    RectangleF halo = RectangleF.Inflate(rect, i * 2.5f, i * 2.5f);
                    using (GraphicsPath haloPath = Theme.RoundedRect(halo, 14f + i * 2f))
                    using (SolidBrush haloBrush = new SolidBrush(Theme.WithAlpha(Accent1, (int)(30 * _hover))))
                    {
                        g.FillPath(haloBrush, haloPath);
                    }
                }
            }

            Color c1 = Theme.Mix(Accent1, Color.White, 0.18f * _hover);
            Color c2 = Theme.Mix(Accent2, Color.White, 0.18f * _hover);
            using (GraphicsPath path = Theme.RoundedRect(rect, 14f))
            using (LinearGradientBrush fill = new LinearGradientBrush(rect, c1, c2, 0f))
            {
                g.FillPath(fill, path);
            }

            // glossy highlight on the top half
            RectangleF top = new RectangleF(rect.X + 2f, rect.Y + 1f, rect.Width - 4f, rect.Height / 2f);
            using (GraphicsPath topPath = Theme.RoundedRect(top, 12f))
            using (SolidBrush gloss = new SolidBrush(Color.FromArgb(28, 255, 255, 255)))
            {
                g.FillPath(gloss, topPath);
            }

            if (Icon != null)
            {
                g.DrawImage(Icon, new RectangleF(rect.X + 14f, rect.Y + (rect.Height - 30f) / 2f, 30f, 30f));
            }

            using (StringFormat format = new StringFormat())
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisCharacter;
                format.FormatFlags = StringFormatFlags.NoWrap;
                RectangleF textRect = new RectangleF(rect.X + 56f, rect.Y, rect.Width - 64f, rect.Height);
                g.DrawString(Text, Theme.UiFont(15f, true), textBrush, textRect, format);
            }

            // dim everything when disabled
            if (!Enabled)
            {
                using (GraphicsPath dimPath = Theme.RoundedRect(rect, 14f))
                using (SolidBrush dim = new SolidBrush(Color.FromArgb(185, Theme.Panel)))
                {
                    g.FillPath(dim, dimPath);
                }
            }

            if (Focused)
            {
                using (GraphicsPath focusPath = Theme.RoundedRect(RectangleF.Inflate(rect, 2f, 2f), 16f))
                using (Pen focusPen = new Pen(Color.FromArgb(200, 255, 255, 255), 1.5f))
                {
                    g.DrawPath(focusPen, focusPath);
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovering = true; }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovering = false;
            _pressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _pressed = false;
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) OnClick(EventArgs.Empty);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    }
}
