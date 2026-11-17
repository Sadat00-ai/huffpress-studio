using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HuffPressStudio.Controls
{
    // Bar chart of the most frequent byte values. Bars grow one after another
    // when new data arrives, and hovering a bar shows its exact numbers.
    internal sealed class FrequencyChart : AnimatedControl
    {
        private const int MaxBars = 32;

        private int[] _symbols = new int[0];
        private uint[] _counts = new uint[0];
        private float[] _target = new float[0];     // final bar heights, 0..1
        private float[] _shown = new float[0];      // animated bar heights, 0..1
        private int[] _delay = new int[0];          // frames to wait before a bar starts growing
        private long _total;
        private int _distinct;
        private int _hover = -1;

        public void SetData(uint[] freq)
        {
            List<int> used = new List<int>();
            _total = 0;
            for (int i = 0; i < freq.Length; i++)
            {
                _total += freq[i];
                if (freq[i] > 0) used.Add(i);
            }
            _distinct = used.Count;
            used.Sort(delegate (int a, int b)
            {
                int c = freq[b].CompareTo(freq[a]);
                return c != 0 ? c : a.CompareTo(b);
            });

            int n = Math.Min(MaxBars, used.Count);
            _symbols = new int[n];
            _counts = new uint[n];
            _target = new float[n];
            _shown = new float[n];
            _delay = new int[n];

            uint max = n > 0 ? freq[used[0]] : 1u;
            for (int i = 0; i < n; i++)
            {
                _symbols[i] = used[i];
                _counts[i] = freq[used[i]];
                _target[i] = (float)_counts[i] / max;
                _delay[i] = i * 2;                  // stagger the growth
            }
            _hover = -1;
            Invalidate();
        }

        public void Clear()
        {
            SetData(new uint[256]);
        }

        protected override bool Step()
        {
            bool changed = false;
            for (int i = 0; i < _shown.Length; i++)
            {
                if (_delay[i] > 0) { _delay[i]--; changed = true; continue; }
                float next = Theme.Approach(_shown[i], _target[i], 0.14f);
                if (next != _shown[i]) { _shown[i] = next; changed = true; }
            }
            return changed;
        }

        // The rectangle (design pixels) where the bars are drawn.
        private RectangleF PlotArea()
        {
            return new RectangleF(18f, 54f, LogicalWidth - 36f, LogicalHeight - 54f - 32f);
        }

        protected override void PaintScaled(Graphics g, SizeF size)
        {
            string title = "BYTE FREQUENCY";
            if (_symbols.Length > 0)
            {
                title += "   top " + _symbols.Length + " of " + _distinct + " byte values";
            }
            Theme.DrawCard(g, new RectangleF(0.5f, 0.5f, size.Width - 1f, size.Height - 1f), title);

            if (_symbols.Length == 0)
            {
                DrawEmptyMessage(g, size);
                return;
            }

            RectangleF plot = PlotArea();
            DrawGridLines(g, plot);
            DrawBars(g, plot);
            if (_hover >= 0) DrawTooltip(g, size);
        }

        private void DrawEmptyMessage(Graphics g, SizeF size)
        {
            using (StringFormat centre = new StringFormat())
            using (SolidBrush brush = new SolidBrush(Theme.TextDim))
            {
                centre.Alignment = StringAlignment.Center;
                centre.LineAlignment = StringAlignment.Center;
                g.DrawString("Load a file to see which bytes appear most often",
                             Theme.UiFont(14f, false), brush,
                             new RectangleF(0f, 30f, size.Width, size.Height - 30f), centre);
            }
        }

        private void DrawGridLines(Graphics g, RectangleF plot)
        {
            using (Pen grid = new Pen(Color.FromArgb(40, 255, 255, 255), 1f))
            {
                for (int i = 0; i <= 4; i++)
                {
                    float y = plot.Bottom - plot.Height * i / 4f;
                    g.DrawLine(grid, plot.Left, y, plot.Right, y);
                }
            }
        }

        private void DrawBars(Graphics g, RectangleF plot)
        {
            float slot = plot.Width / Math.Max(_symbols.Length, 10);
            float barWidth = slot * 0.68f;
            Font labelFont = Theme.UiFont(10f, false);

            using (StringFormat centre = new StringFormat())
            {
                centre.Alignment = StringAlignment.Center;

                for (int i = 0; i < _symbols.Length; i++)
                {
                    float x = plot.Left + i * slot + (slot - barWidth) / 2f;
                    float height = _shown[i] * plot.Height;
                    bool hot = i == _hover;

                    if (height >= 1.5f)
                    {
                        RectangleF bar = new RectangleF(x, plot.Bottom - height, barWidth, height);
                        Color top = hot ? Color.White : Theme.Cyan;
                        Color bottom = hot ? Theme.Cyan : Theme.Violet;
                        using (GraphicsPath path = Theme.RoundedRect(bar, Math.Min(4f, barWidth / 2f)))
                        using (LinearGradientBrush brush = new LinearGradientBrush(bar, top, bottom, 90f))
                        {
                            g.FillPath(brush, path);
                        }
                    }

                    using (SolidBrush text = new SolidBrush(hot ? Theme.Text : Theme.TextDim))
                    {
                        g.DrawString(Theme.SymbolLabel(_symbols[i]), labelFont, text,
                                     new RectangleF(x - 4f, plot.Bottom + 5f, barWidth + 8f, 16f), centre);
                    }
                }
            }
        }

        private void DrawTooltip(Graphics g, SizeF size)
        {
            double percent = _total > 0 ? _counts[_hover] * 100.0 / _total : 0.0;
            string text = "'" + Theme.SymbolLabel(_symbols[_hover]) + "'  0x" + _symbols[_hover].ToString("X2")
                        + "   " + Theme.FormatCount(_counts[_hover]) + " times   "
                        + percent.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "%";
            Font font = Theme.UiFont(12f, true);
            SizeF textSize = g.MeasureString(text, font);
            RectangleF pill = new RectangleF(size.Width - textSize.Width - 36f, 11f, textSize.Width + 20f, 26f);

            using (GraphicsPath path = Theme.RoundedRect(pill, 13f))
            using (SolidBrush fill = new SolidBrush(Theme.PanelLight))
            using (Pen border = new Pen(Theme.Cyan, 1f))
            using (SolidBrush label = new SolidBrush(Theme.Text))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
                g.DrawString(text, font, label, pill.X + 10f, pill.Y + 4f);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int found = -1;
            if (_symbols.Length > 0)
            {
                PointF p = ToLogical(e.Location);
                RectangleF plot = PlotArea();
                float slot = plot.Width / Math.Max(_symbols.Length, 10);
                if (p.X >= plot.Left && p.Y >= plot.Top && p.Y <= plot.Bottom + 24f)
                {
                    int index = (int)((p.X - plot.Left) / slot);
                    if (index >= 0 && index < _symbols.Length) found = index;
                }
            }
            if (found != _hover)
            {
                _hover = found;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hover != -1)
            {
                _hover = -1;
                Invalidate();
            }
        }
    }
}
