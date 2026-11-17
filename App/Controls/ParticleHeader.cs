using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace HuffPressStudio.Controls
{
    // The banner at the top: artwork, floating 0/1 particles, a bobbing logo,
    // a gradient title and an animated glow line along the bottom edge.
    internal sealed class ParticleHeader : AnimatedControl
    {
        private sealed class Particle
        {
            public float X, Y, Speed, Size;
            public int Alpha;
            public char Bit;
        }

        private readonly List<Particle> _particles = new List<Particle>();
        private readonly Random _rng = new Random();
        private readonly Image _banner = ProjectAssets.Load("banner.jpg");
        private readonly Image _logo = ProjectAssets.Load("logo.png");
        private float _time;

        public string EngineInfo { get; set; }

        public ParticleHeader()
        {
            EngineInfo = "";
        }

        protected override bool Step()
        {
            _time += 0.033f;
            float w = LogicalWidth, h = LogicalHeight;
            if (_particles.Count == 0) CreateParticles(w, h);

            foreach (Particle p in _particles)
            {
                p.Y -= p.Speed;
                if (p.Y < -20f) Respawn(p, w, h);
                if (_rng.Next(150) == 0) p.Bit = p.Bit == '0' ? '1' : '0';
            }
            return true;
        }

        private void CreateParticles(float w, float h)
        {
            for (int i = 0; i < 60; i++)
            {
                Particle p = new Particle();
                Respawn(p, w, h);
                p.Y = (float)(_rng.NextDouble() * h);     // spread them over the whole height at start
                _particles.Add(p);
            }
        }

        private void Respawn(Particle p, float w, float h)
        {
            p.X = (float)(_rng.NextDouble() * w);
            p.Y = h + (float)(_rng.NextDouble() * 20.0);
            p.Speed = 0.25f + (float)(_rng.NextDouble() * 0.9);
            p.Size = 10f + (float)(_rng.NextDouble() * 12.0);
            p.Alpha = 40 + _rng.Next(110);
            p.Bit = _rng.Next(2) == 0 ? '0' : '1';
        }

        protected override void PaintScaled(Graphics g, SizeF size)
        {
            RectangleF area = new RectangleF(0f, 0f, size.Width, size.Height);
            DrawBackdrop(g, area);
            DrawParticles(g);
            DrawLogo(g, size);
            DrawTitleBlock(g, size);
            DrawEngineInfo(g, size);
            DrawGlowLine(g, size);
        }

        private void DrawBackdrop(Graphics g, RectangleF area)
        {
            if (_banner != null)
            {
                g.DrawImage(_banner, area);
            }
            else
            {
                using (LinearGradientBrush back = new LinearGradientBrush(area, Color.FromArgb(20, 24, 60), Color.FromArgb(60, 22, 110), 0f))
                {
                    g.FillRectangle(back, area);
                }
            }

            // Darken the left side so the title stays readable.
            using (LinearGradientBrush shade = new LinearGradientBrush(area,
                   Color.FromArgb(215, 14, 17, 32), Color.FromArgb(0, 14, 17, 32), 0f))
            {
                g.FillRectangle(shade, area);
            }
        }

        private void DrawParticles(Graphics g)
        {
            Font font = Theme.MonoFont(14f, true);
            foreach (Particle p in _particles)
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(p.Alpha, Theme.Cyan)))
                {
                    g.DrawString(p.Bit.ToString(), font, brush, p.X, p.Y);
                }
            }
        }

        private void DrawLogo(Graphics g, SizeF size)
        {
            float logoSize = 80f;
            float bob = (float)Math.Sin(_time * 2.0) * 3f;
            RectangleF rect = new RectangleF(26f, (size.Height - logoSize) / 2f + bob, logoSize, logoSize);

            // soft glow behind the logo
            for (int i = 4; i >= 1; i--)
            {
                RectangleF halo = RectangleF.Inflate(rect, i * 5f, i * 5f);
                using (SolidBrush glow = new SolidBrush(Color.FromArgb(10, Theme.Cyan)))
                {
                    g.FillEllipse(glow, halo);
                }
            }

            if (_logo != null)
            {
                g.DrawImage(_logo, rect);
            }
            else
            {
                using (LinearGradientBrush tile = new LinearGradientBrush(rect, Theme.Violet, Theme.Cyan, 45f))
                using (GraphicsPath path = Theme.RoundedRect(rect, 22f))
                {
                    g.FillPath(tile, path);
                }
            }
        }

        private void DrawTitleBlock(Graphics g, SizeF size)
        {
            float x = 128f;
            string title = "HuffPress Studio";
            Font titleFont = Theme.UiFont(38f, true);
            SizeF titleSize = g.MeasureString(title, titleFont);
            RectangleF titleRect = new RectangleF(x, 12f, titleSize.Width, titleSize.Height);

            using (LinearGradientBrush titleBrush = new LinearGradientBrush(titleRect, Color.White, Theme.Cyan, 0f))
            {
                g.DrawString(title, titleFont, titleBrush, x, 12f);
            }

            using (SolidBrush sub = new SolidBrush(Color.FromArgb(210, 215, 245)))
            {
                g.DrawString("Huffman compression lab - see how every byte gets its own shortest code",
                             Theme.UiFont(14f, false), sub, x + 2f, 66f);
            }

            float chipX = x + 2f;
            chipX += DrawChip(g, "C++ native engine", chipX, 94f, Theme.Cyan) + 8f;
            chipX += DrawChip(g, "C# WinForms UI", chipX, 94f, Theme.Violet) + 8f;
            DrawChip(g, "P/Invoke bridge", chipX, 94f, Theme.Pink);
        }

        // Draws a small rounded label and returns its width.
        private float DrawChip(Graphics g, string text, float x, float y, Color color)
        {
            Font font = Theme.UiFont(12f, true);
            SizeF textSize = g.MeasureString(text, font);
            RectangleF rect = new RectangleF(x, y, textSize.Width + 16f, 24f);
            using (GraphicsPath path = Theme.RoundedRect(rect, 12f))
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(55, color)))
            using (Pen border = new Pen(Color.FromArgb(170, color), 1f))
            using (SolidBrush label = new SolidBrush(Color.White))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
                g.DrawString(text, font, label, x + 8f, y + 3f);
            }
            return rect.Width;
        }

        private void DrawEngineInfo(Graphics g, SizeF size)
        {
            if (string.IsNullOrEmpty(EngineInfo)) return;
            string text = "Engine: " + EngineInfo;
            Font font = Theme.UiFont(12f, false);
            SizeF s = g.MeasureString(text, font);
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(200, Theme.TextDim)))
            {
                g.DrawString(text, font, brush, size.Width - s.Width - 18f, 14f);
            }
        }

        private void DrawGlowLine(Graphics g, SizeF size)
        {
            RectangleF line = new RectangleF(0f, size.Height - 3f, size.Width, 3f);
            using (LinearGradientBrush brush = new LinearGradientBrush(line, Theme.Cyan, Theme.Pink, 0f))
            {
                ColorBlend blend = new ColorBlend(3);
                blend.Colors = new Color[] { Theme.Cyan, Theme.Violet, Theme.Pink };
                blend.Positions = new float[] { 0f, 0.5f, 1f };
                brush.InterpolationColors = blend;
                g.FillRectangle(brush, line);
            }

            // a bright spot that travels along the line
            float t = (_time * 0.25f) % 1f;
            float x = t * (size.Width + 120f) - 60f;
            using (SolidBrush spot = new SolidBrush(Color.FromArgb(150, 255, 255, 255)))
            {
                g.FillEllipse(spot, x - 40f, size.Height - 6f, 80f, 6f);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_banner != null) _banner.Dispose();
                if (_logo != null) _logo.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
