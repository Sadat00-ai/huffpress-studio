using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace HuffPressStudio.Controls
{
    // Small card with one number. It counts up or down to the new value and flashes.
    internal sealed class StatCard : AnimatedControl
    {
        private readonly string _title;
        private readonly Color _accent;
        private double _target;
        private double _current;
        private bool _hasValue;
        private float _flash;
        private Func<double, string> _format;

        public StatCard(string title, Color accent)
        {
            _title = title;
            _accent = accent;
            _format = DefaultFormat;
        }

        private static string DefaultFormat(double value)
        {
            return Math.Round(value).ToString();
        }

        // Shows a new value; the number animates from the old value to the new one.
        public void SetValue(double value, Func<double, string> format)
        {
            _format = format;
            _target = value;
            _hasValue = true;
            _flash = 1f;
            Invalidate();
        }

        // Goes back to the empty "-" state.
        public void Reset()
        {
            _hasValue = false;
            _current = 0.0;
            _flash = 0f;
            Invalidate();
        }

        protected override bool Step()
        {
            bool changed = false;
            if (_hasValue && _current != _target)
            {
                double diff = _target - _current;
                double limit = 0.0005 * Math.Max(1.0, Math.Abs(_target));
                _current = Math.Abs(diff) <= limit ? _target : _current + diff * 0.16;
                changed = true;
            }
            if (_flash > 0f)
            {
                _flash = Math.Max(0f, _flash - 0.035f);
                changed = true;
            }
            return changed;
        }

        protected override void PaintScaled(Graphics g, SizeF size)
        {
            RectangleF rect = new RectangleF(0.5f, 0.5f, size.Width - 1f, size.Height - 1f);
            Theme.DrawCard(g, rect, null);

            if (_flash > 0.01f)
            {
                using (GraphicsPath path = Theme.RoundedRect(rect, 14f))
                using (Pen flashPen = new Pen(Theme.WithAlpha(_accent, (int)(255 * _flash)), 2f))
                {
                    g.DrawPath(flashPen, path);
                }
            }

            RectangleF bar = new RectangleF(14f, 18f, 4f, size.Height - 36f);
            using (GraphicsPath barPath = Theme.RoundedRect(bar, 2f))
            using (LinearGradientBrush barBrush = new LinearGradientBrush(bar, _accent, Theme.Mix(_accent, Color.White, 0.5f), 90f))
            {
                g.FillPath(barBrush, barPath);
            }

            using (SolidBrush titleBrush = new SolidBrush(Theme.TextDim))
            using (SolidBrush valueBrush = new SolidBrush(Theme.Text))
            {
                g.DrawString(_title, Theme.UiFont(11f, true), titleBrush, 28f, 14f);
                string value = _hasValue ? _format(_current) : "-";
                g.DrawString(value, Theme.UiFont(26f, true), valueBrush, 26f, 34f);
            }
        }
    }
}
