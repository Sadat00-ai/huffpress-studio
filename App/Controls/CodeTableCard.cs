using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HuffPressStudio.Controls
{
    // A card holding a dark, owner-drawn list of every byte with its Huffman code.
    internal sealed class CodeTableCard : AnimatedControl
    {
        // ListView with double buffering switched on (removes flicker).
        private sealed class BufferedListView : ListView
        {
            public BufferedListView()
            {
                DoubleBuffered = true;
            }
        }

        private readonly BufferedListView _list = new BufferedListView();

        public CodeTableCard()
        {
            _list.View = View.Details;
            _list.FullRowSelect = true;
            _list.BorderStyle = BorderStyle.None;
            _list.HideSelection = false;
            _list.MultiSelect = false;
            _list.BackColor = Theme.Panel;
            _list.ForeColor = Theme.Text;
            _list.OwnerDraw = true;
            _list.Font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
            _list.Columns.Add("Byte", 80);
            _list.Columns.Add("Count", 80);
            _list.Columns.Add("Bits", 50);
            _list.Columns.Add("Code", 160);

            _list.DrawColumnHeader += OnDrawColumnHeader;
            _list.DrawItem += OnDrawItem;
            _list.DrawSubItem += OnDrawSubItem;
            _list.HandleCreated += OnListHandleCreated;
            Controls.Add(_list);
        }

        public void SetCodes(IList<CodeInfo> codes)
        {
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (CodeInfo c in codes)
            {
                string byteText = Theme.SymbolLabel(c.Symbol) + "  0x" + c.Symbol.ToString("X2");
                ListViewItem item = new ListViewItem(byteText);
                item.SubItems.Add(Theme.FormatCount(c.Count));
                item.SubItems.Add(c.Length.ToString());
                item.SubItems.Add(c.BitString);
                _list.Items.Add(item);
            }
            _list.EndUpdate();
        }

        // layout

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _list.SetBounds(Theme.S(14), Theme.S(44), Math.Max(10, Width - Theme.S(28)), Math.Max(10, Height - Theme.S(56)));
            FitColumns();
        }

        private void FitColumns()
        {
            int usable = _list.ClientSize.Width;
            if (usable <= 0) return;
            _list.Columns[0].Width = (int)(usable * 0.30);
            _list.Columns[1].Width = (int)(usable * 0.24);
            _list.Columns[2].Width = (int)(usable * 0.12);
            _list.Columns[3].Width = usable - _list.Columns[0].Width - _list.Columns[1].Width - _list.Columns[2].Width;
        }

        private void OnListHandleCreated(object sender, EventArgs e)
        {
            try
            {
                NativeMethods.SetWindowTheme(_list.Handle, "DarkMode_Explorer", null);   // dark scrollbars
            }
            catch (Exception)
            {
                // older Windows versions simply keep the normal scrollbar
            }
        }

        // owner drawing

        private void OnDrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (SolidBrush back = new SolidBrush(Theme.PanelLight))
            {
                e.Graphics.FillRectangle(back, e.Bounds);
            }
            Rectangle textBounds = new Rectangle(e.Bounds.X + Theme.S(8), e.Bounds.Y, e.Bounds.Width - Theme.S(8), e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, Theme.UiFont(11f * Theme.Scale, true), textBounds, Theme.TextDim,
                                  TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        private void OnDrawItem(object sender, DrawListViewItemEventArgs e)
        {
            // everything is painted per sub-item
        }

        private void OnDrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            Rectangle bounds = e.Bounds;
            if (e.ColumnIndex == 0) bounds.Width = _list.Columns[0].Width;

            Color back = e.ItemIndex % 2 == 0 ? Theme.Panel : Color.FromArgb(28, 33, 60);
            if (e.Item.Selected) back = Theme.WithAlpha(Theme.Violet, 120);
            using (SolidBrush brush = new SolidBrush(back))
            {
                e.Graphics.FillRectangle(brush, bounds);
            }

            bool codeColumn = e.ColumnIndex == 3;
            Font font = codeColumn ? Theme.MonoFont(13f * Theme.Scale, true) : Theme.UiFont(12f * Theme.Scale, false);
            Color colour = codeColumn ? Theme.Cyan : (e.ColumnIndex == 0 ? Theme.Text : Theme.TextDim);
            Rectangle textBounds = new Rectangle(bounds.X + Theme.S(8), bounds.Y, bounds.Width - Theme.S(8), bounds.Height);
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, font, textBounds, colour,
                                  TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        // AnimatedControl

        protected override bool Step()
        {
            return false;       // static card, nothing to animate
        }

        protected override void PaintScaled(Graphics g, SizeF size)
        {
            string title = "HUFFMAN CODE TABLE";
            if (_list.Items.Count > 0) title += "   " + _list.Items.Count + " codes";
            Theme.DrawCard(g, new RectangleF(0.5f, 0.5f, size.Width - 1f, size.Height - 1f), title);
        }
    }
}
