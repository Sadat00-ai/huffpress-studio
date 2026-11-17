using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HuffPressStudio.Controls
{
    // Draws the top levels of the Huffman tree. Edges are labelled 0 (left) and 1 (right);
    // leaves show their byte, and a "..." node stands for a deeper branch that is not drawn.
    // The tree "grows" level by level whenever new codes are set.
    internal sealed class HuffmanTreeControl : AnimatedControl
    {
        private const int MaxDepth = 4;         // levels drawn below the root

        private sealed class TrieNode
        {
            public int Symbol = -1;             // >= 0 for leaves
            public TrieNode Zero;
            public TrieNode One;
            public int Depth;
            public float X;                     // leaf slot index; inner nodes use the average of their children
            public bool Terminal;               // a leaf, or a branch that is cut off at MaxDepth
            public int Hidden;                  // leaves below a cut-off branch
            public int Length;
            public ulong Bits;
            public uint Count;
        }

        private readonly List<TrieNode> _drawn = new List<TrieNode>();
        private TrieNode _root;
        private int _leafSlots;
        private float _reveal;
        private TrieNode _hover;

        // geometry shared by painting and mouse hit-testing
        private float _left, _spacing, _top, _stepY, _radius;

        public void SetCodes(IList<CodeInfo> codes)
        {
            _root = null;
            _drawn.Clear();
            _hover = null;
            _leafSlots = 0;
            _reveal = 0f;

            if (codes != null && codes.Count > 0)
            {
                _root = BuildTrie(codes);
                AssignSlots(_root);
            }
            Invalidate();
        }

        // building

        private static TrieNode BuildTrie(IList<CodeInfo> codes)
        {
            TrieNode root = new TrieNode();
            foreach (CodeInfo code in codes)
            {
                TrieNode node = root;
                for (int i = code.Length - 1; i >= 0; i--)
                {
                    bool one = ((code.Bits >> i) & 1UL) == 1UL;
                    TrieNode child = one ? node.One : node.Zero;
                    if (child == null)
                    {
                        child = new TrieNode();
                        child.Depth = node.Depth + 1;
                        if (one) node.One = child; else node.Zero = child;
                    }
                    node = child;
                }
                node.Symbol = code.Symbol;
                node.Length = code.Length;
                node.Bits = code.Bits;
                node.Count = code.Count;
            }
            return root;
        }

        private static int CountLeaves(TrieNode node)
        {
            if (node == null) return 0;
            if (node.Symbol >= 0) return 1;
            return CountLeaves(node.Zero) + CountLeaves(node.One);
        }

        // Gives every visible leaf a horizontal slot (left to right) and lists the visible nodes.
        private void AssignSlots(TrieNode node)
        {
            _drawn.Add(node);
            node.Terminal = node.Symbol >= 0 || node.Depth >= MaxDepth;
            if (node.Terminal)
            {
                node.X = _leafSlots++;
                if (node.Symbol < 0) node.Hidden = CountLeaves(node);
                return;
            }

            float sum = 0f;
            int children = 0;
            if (node.Zero != null) { AssignSlots(node.Zero); sum += node.Zero.X; children++; }
            if (node.One != null) { AssignSlots(node.One); sum += node.One.X; children++; }
            node.X = children > 0 ? sum / children : _leafSlots++;
        }

        // animation

        protected override bool Step()
        {
            if (_reveal >= MaxDepth + 2) return false;
            _reveal += 0.07f;
            return true;
        }

        private float Visibility(int depth)
        {
            return Theme.Clamp01(_reveal - depth);
        }

        // painting

        private void ComputeGeometry()
        {
            float w = LogicalWidth, h = LogicalHeight;
            float right = w - 30f;
            _left = 30f;
            _spacing = _leafSlots > 1 ? (right - _left) / (_leafSlots - 1) : 0f;
            _top = 66f;
            _stepY = (h - 36f - _top) / MaxDepth;
            _radius = _spacing > 0f ? Math.Max(6f, Math.Min(11f, _spacing / 2f - 1.5f)) : 11f;
            _radius = Math.Max(6f, Math.Min(_radius, _stepY / 2f - 2f));
        }

        private PointF Position(TrieNode node)
        {
            float x = _leafSlots > 1 ? _left + node.X * _spacing : LogicalWidth / 2f;
            return new PointF(x, _top + node.Depth * _stepY);
        }

        protected override void PaintScaled(Graphics g, SizeF size)
        {
            Theme.DrawCard(g, new RectangleF(0.5f, 0.5f, size.Width - 1f, size.Height - 1f), "HUFFMAN TREE   top levels");

            if (_root == null)
            {
                using (StringFormat centre = new StringFormat())
                using (SolidBrush brush = new SolidBrush(Theme.TextDim))
                {
                    centre.Alignment = StringAlignment.Center;
                    centre.LineAlignment = StringAlignment.Center;
                    g.DrawString("The code tree appears here", Theme.UiFont(14f, false), brush,
                                 new RectangleF(0f, 30f, size.Width, size.Height - 30f), centre);
                }
                return;
            }

            ComputeGeometry();
            DrawEdges(g);
            DrawNodes(g);
            DrawHint(g, size);
        }

        private void DrawEdges(Graphics g)
        {
            Font bitFont = Theme.MonoFont(11f, true);
            foreach (TrieNode node in _drawn)
            {
                if (node.Terminal) continue;
                DrawEdge(g, node, node.Zero, "0", bitFont);
                DrawEdge(g, node, node.One, "1", bitFont);
            }
        }

        private void DrawEdge(Graphics g, TrieNode parent, TrieNode child, string bit, Font bitFont)
        {
            if (child == null) return;
            float visible = Visibility(child.Depth);
            if (visible <= 0f) return;

            PointF a = Position(parent);
            PointF b = Position(child);
            using (Pen pen = new Pen(Theme.WithAlpha(Theme.Violet, (int)(200 * visible)), 1.6f))
            {
                g.DrawLine(pen, a, b);
            }

            float mx = (a.X + b.X) / 2f + (bit == "0" ? -8f : 3f);
            float my = (a.Y + b.Y) / 2f - 7f;
            using (SolidBrush text = new SolidBrush(Theme.WithAlpha(Theme.TextDim, (int)(255 * visible))))
            {
                g.DrawString(bit, bitFont, text, mx, my);
            }
        }

        private void DrawNodes(Graphics g)
        {
            foreach (TrieNode node in _drawn)
            {
                float visible = Visibility(node.Depth);
                if (visible <= 0f) continue;

                PointF p = Position(node);
                int alpha = (int)(255 * visible);

                if (!node.Terminal)
                {
                    using (SolidBrush dot = new SolidBrush(Theme.WithAlpha(Theme.Violet, alpha)))
                    {
                        g.FillEllipse(dot, p.X - 4f, p.Y - 4f, 8f, 8f);
                    }
                }
                else if (node.Symbol >= 0)
                {
                    DrawLeaf(g, node, p, alpha);
                }
                else
                {
                    DrawCollapsed(g, p, alpha);
                }
            }
        }

        private void DrawLeaf(Graphics g, TrieNode node, PointF p, int alpha)
        {
            bool hot = node == _hover;
            RectangleF circle = new RectangleF(p.X - _radius, p.Y - _radius, _radius * 2f, _radius * 2f);
            using (SolidBrush fill = new SolidBrush(hot ? Theme.WithAlpha(Theme.Cyan, alpha) : Theme.WithAlpha(Theme.PanelLight, alpha)))
            using (Pen ring = new Pen(Theme.WithAlpha(Theme.Cyan, alpha), 1.6f))
            using (SolidBrush label = new SolidBrush(hot ? Theme.WithAlpha(Theme.Background, alpha) : Theme.WithAlpha(Theme.Text, alpha)))
            using (StringFormat centre = new StringFormat())
            {
                centre.Alignment = StringAlignment.Center;
                centre.LineAlignment = StringAlignment.Center;
                g.FillEllipse(fill, circle);
                g.DrawEllipse(ring, circle);
                g.DrawString(Theme.SymbolLabel(node.Symbol), Theme.UiFont(10f, true), label,
                             new RectangleF(circle.X - 4f, circle.Y, circle.Width + 8f, circle.Height), centre);
            }
        }

        private void DrawCollapsed(Graphics g, PointF p, int alpha)
        {
            RectangleF circle = new RectangleF(p.X - _radius, p.Y - _radius, _radius * 2f, _radius * 2f);
            using (Pen dashed = new Pen(Theme.WithAlpha(Theme.Pink, alpha), 1.6f))
            using (SolidBrush label = new SolidBrush(Theme.WithAlpha(Theme.Pink, alpha)))
            using (StringFormat centre = new StringFormat())
            {
                dashed.DashStyle = DashStyle.Dot;
                centre.Alignment = StringAlignment.Center;
                centre.LineAlignment = StringAlignment.Center;
                g.DrawEllipse(dashed, circle);
                g.DrawString("...", Theme.UiFont(10f, true), label,
                             new RectangleF(circle.X - 4f, circle.Y - 3f, circle.Width + 8f, circle.Height), centre);
            }
        }

        private void DrawHint(Graphics g, SizeF size)
        {
            string text = "Hover a node to see its code";
            Color colour = Theme.TextDim;
            if (_hover != null)
            {
                colour = Theme.Cyan;
                if (_hover.Symbol >= 0)
                {
                    CodeInfo info = new CodeInfo();
                    info.Bits = _hover.Bits;
                    info.Length = _hover.Length;
                    text = "'" + Theme.SymbolLabel(_hover.Symbol) + "'  becomes  " + info.BitString
                         + "   (" + _hover.Length + " bits instead of 8)";
                }
                else
                {
                    text = _hover.Hidden + " more byte values live below this branch";
                }
            }

            using (StringFormat centre = new StringFormat())
            using (SolidBrush brush = new SolidBrush(colour))
            {
                centre.Alignment = StringAlignment.Center;
                g.DrawString(text, Theme.UiFont(12f, true), brush,
                             new RectangleF(0f, size.Height - 28f, size.Width, 20f), centre);
            }
        }

        // mouse

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_root == null) return;

            ComputeGeometry();
            PointF mouse = ToLogical(e.Location);
            TrieNode found = null;
            foreach (TrieNode node in _drawn)
            {
                if (!node.Terminal || Visibility(node.Depth) < 1f) continue;
                PointF p = Position(node);
                float dx = mouse.X - p.X, dy = mouse.Y - p.Y;
                if (dx * dx + dy * dy <= (_radius + 3f) * (_radius + 3f)) { found = node; break; }
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
            if (_hover != null)
            {
                _hover = null;
                Invalidate();
            }
        }
    }
}
