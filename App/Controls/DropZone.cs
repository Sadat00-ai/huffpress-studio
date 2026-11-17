using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HuffPressStudio.Controls
{
    // Drag-and-drop target with "marching ants" dashed border and a pulsing glow.
    // Click it to browse, or drop any file on it.
    internal sealed class DropZone : AnimatedControl
    {
        private readonly Image _picture = ProjectAssets.Load("dropzone.png");
        private float _time;
        private float _dashOffset;
        private float _dragAmount;      // 0..1, eased
        private bool _dragging;
        private string _fileName;
        private string _fileDetail;

        public event EventHandler<string> FileDropped;
        public event EventHandler BrowseRequested;

        public DropZone()
        {
            AllowDrop = true;
            Cursor = Cursors.Hand;
        }

        public void ShowFile(string name, string detail)
        {
            _fileName = name;
            _fileDetail = detail;
            Invalidate();
        }

        protected override bool Step()
        {
            _time += 0.033f;
            _dashOffset -= 0.18f;
            _dragAmount = Theme.Approach(_dragAmount, _dragging ? 1f : 0f, 0.2f);
            return true;
        }

        protected override void PaintScaled(Graphics g, SizeF size)
        {
            RectangleF rect = new RectangleF(1f, 1f, size.Width - 2f, size.Height - 2f);

            using (GraphicsPath path = Theme.RoundedRect(rect, 16f))
            {
                using (SolidBrush fill = new SolidBrush(Theme.Panel))
                {
                    g.FillPath(fill, path);
                }
                if (_dragAmount > 0.01f)
                {
                    using (SolidBrush glow = new SolidBrush(Theme.WithAlpha(Theme.Cyan, (int)(55 * _dragAmount))))
                    {
                        g.FillPath(glow, path);
                    }
                }

                float pulse = 0.5f + 0.5f * (float)Math.Sin(_time * 2.4);
                Color borderColor = Theme.Mix(Theme.Violet, Theme.Cyan, Math.Max(pulse * 0.6f, _dragAmount));
                using (Pen dashed = new Pen(borderColor, 2f))
                {
                    dashed.DashPattern = new float[] { 4f, 3f };
                    dashed.DashOffset = _dashOffset;
                    g.DrawPath(dashed, path);
                }
            }

            // keep the content vertically centred when the zone is taller than needed
            float offset = Math.Max(0f, (size.Height - 160f) / 2f);
            g.TranslateTransform(0f, offset);
            DrawPicture(g, size);
            DrawLabels(g, size);
            g.TranslateTransform(0f, -offset);
        }

        private void DrawPicture(Graphics g, SizeF size)
        {
            float bob = (float)Math.Sin(_time * 2.2) * 4f - _dragAmount * 6f;
            RectangleF pic = new RectangleF((size.Width - 110f) / 2f, 14f + bob, 110f, 80f);
            if (_picture != null)
            {
                g.DrawImage(_picture, pic);
            }
            else
            {
                using (SolidBrush brush = new SolidBrush(Theme.Violet))
                {
                    g.FillEllipse(brush, pic);
                }
            }
        }

        private void DrawLabels(Graphics g, SizeF size)
        {
            using (StringFormat centre = new StringFormat())
            using (SolidBrush main = new SolidBrush(Theme.Text))
            using (SolidBrush dim = new SolidBrush(Theme.TextDim))
            {
                centre.Alignment = StringAlignment.Center;
                centre.Trimming = StringTrimming.EllipsisCharacter;
                centre.FormatFlags = StringFormatFlags.NoWrap;

                bool hasFile = !string.IsNullOrEmpty(_fileName);
                string headline = _dragging ? "Release to load" : (hasFile ? _fileName : "Drop a file here");
                string subline = hasFile && !_dragging ? _fileDetail : "or click to browse";

                RectangleF line1 = new RectangleF(12f, 106f, size.Width - 24f, 24f);
                RectangleF line2 = new RectangleF(12f, 134f, size.Width - 24f, 20f);
                g.DrawString(headline, Theme.UiFont(16f, true), main, line1, centre);
                g.DrawString(subline, Theme.UiFont(12f, false), dim, line2, centre);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            EventHandler handler = BrowseRequested;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                _dragging = true;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        protected override void OnDragLeave(EventArgs e)
        {
            base.OnDragLeave(e);
            _dragging = false;
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            base.OnDragDrop(e);
            _dragging = false;
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null && files.Length > 0)
            {
                EventHandler<string> handler = FileDropped;
                if (handler != null) handler(this, files[0]);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _picture != null) _picture.Dispose();
            base.Dispose(disposing);
        }
    }
}
