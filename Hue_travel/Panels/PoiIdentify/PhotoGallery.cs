using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Hue_travel.MapEngine;
using Hue_travel.Models;
using Hue_travel.Services.Data;
using Hue_travel.Theme;

namespace Hue_travel.Panels.PoiIdentify
{
    /// <summary>
    /// The photos of a place: one large photo with previous / next buttons and a "2 / 5" counter, and a strip of small photos
    /// under it (click one to show it). Clicking the large photo asks for the full-size viewer. A place without photos shows
    /// a tinted panel with its group's badge. Drawn at the screen's resolution.
    /// </summary>
    internal sealed class PhotoGallery : Control
    {
        private const int HeroDesignHeight = 196;
        private const int StripDesignHeight = 72;
        private const int ArrowDesignSize = 38;
        private const int ThumbMaxDesignWidth = 76;

        private readonly PressFeedback press;
        private IReadOnlyList<string> paths = new string[0];
        private PoiCategory category = PoiCategory.Heritage;
        private int index;
        private int hoverId = -1;   // 0 previous, 1 next, 2 + n a small photo, 100 the large photo

        /// <summary>Raised when the large photo is clicked, with the index of the photo shown.</summary>
        public event EventHandler<int> HeroClicked;

        public PhotoGallery()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            Cursor = Cursors.Hand;
            press = new PressFeedback(this);
        }

        public int PhotoCount => paths.Count;

        public int Index => index;

        public IReadOnlyList<string> Paths => paths;

        /// <summary>The height the gallery needs: the large photo, plus the strip when there is more than one photo.</summary>
        public int WantedHeight => Ui.S(HeroDesignHeight) + (paths.Count > 1 ? Ui.S(StripDesignHeight) : 0);

        public void SetPhotos(IReadOnlyList<string> newPaths, PoiCategory placeCategory)
        {
            paths = newPaths ?? new string[0];
            category = placeCategory;
            index = 0;
            Invalidate();
        }

        public void Select(int newIndex)
        {
            if (paths.Count == 0) return;
            index = (newIndex % paths.Count + paths.Count) % paths.Count;
            Invalidate();
        }

        // ------------------------------------------------------------------ geometry

        private Rectangle HeroRect => new Rectangle(0, 0, Width, Ui.S(HeroDesignHeight));

        private Rectangle ArrowRect(bool next)
        {
            int size = Ui.S(ArrowDesignSize);
            Rectangle hero = HeroRect;
            int x = next ? hero.Right - size - Ui.S(10) : hero.X + Ui.S(10);
            return new Rectangle(x, hero.Y + (hero.Height - size) / 2, size, size);
        }

        private Rectangle ThumbRect(int i)
        {
            int pad = Ui.S(14), gap = Ui.S(8), count = paths.Count;
            int width = Math.Min(Ui.S(ThumbMaxDesignWidth), (Width - pad * 2 - gap * (count - 1)) / count);
            int height = Ui.S(StripDesignHeight) - Ui.S(18);
            return new Rectangle(pad + i * (width + gap), HeroRect.Bottom + Ui.S(9), width, height);
        }

        private int HitId(Point p)
        {
            if (paths.Count > 1)
            {
                if (ArrowRect(false).Contains(p)) return 0;
                if (ArrowRect(true).Contains(p)) return 1;
                for (int i = 0; i < paths.Count; i++) if (ThumbRect(i).Contains(p)) return 2 + i;
            }
            if (paths.Count > 0 && HeroRect.Contains(p)) return 100;
            return -1;
        }

        // ------------------------------------------------------------------ input

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int id = HitId(e.Location);
            if (id != hoverId) { hoverId = id; Cursor = id >= 0 ? Cursors.Hand : Cursors.Default; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hoverId = -1;
            press.Up();
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            int id = HitId(e.Location);
            if (e.Button == MouseButtons.Left && id >= 0 && id < 100) press.Down(id, e.Location);
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            press.Up();
            base.OnMouseUp(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;
            int id = HitId(e.Location);
            if (id == 0) Select(index - 1);
            else if (id == 1) Select(index + 1);
            else if (id >= 2 && id < 100) Select(id - 2);
            else if (id == 100) HeroClicked?.Invoke(this, index);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (paths.Count > 1) Select(index + (e.Delta < 0 ? 1 : -1));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) press.Dispose();
            base.Dispose(disposing);
        }

        // ------------------------------------------------------------------ painting

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);
            Rectangle hero = HeroRect;

            Bitmap photo = paths.Count > 0 ? PoiPhotos.Load(paths[index], Math.Max(hero.Width, hero.Height) * 2) : null;
            if (photo != null) AppImages.DrawCover(g, photo, hero);
            else PaintPlaceholder(g, hero);

            if (paths.Count == 0) return;

            // Soft shade at the bottom of the large photo so the counter stays readable.
            using (var shade = new LinearGradientBrush(new Rectangle(hero.X, hero.Bottom - Ui.S(54), hero.Width, Ui.S(54) + 1), Color.FromArgb(0, 0, 0, 0), Color.FromArgb(110, 0, 0, 0), LinearGradientMode.Vertical))
            {
                g.FillRectangle(shade, hero.X, hero.Bottom - Ui.S(54), hero.Width, Ui.S(54));
            }

            if (paths.Count > 1)
            {
                PaintArrow(g, false);
                PaintArrow(g, true);
                PaintCounter(g, hero);
                PaintStrip(g);
            }
        }

        private void PaintPlaceholder(Graphics g, Rectangle hero)
        {
            Color color = PoiMarkerStyle.ColorOf(category);
            using (var brush = new LinearGradientBrush(hero, Ui.Lerp(color, Color.Black, 0.35f), Ui.Lerp(color, Color.White, 0.25f), LinearGradientMode.ForwardDiagonal))
            {
                g.FillRectangle(brush, hero);
            }
            int badge = Ui.S(76);
            g.DrawImage(PoiMarkerIcons.Badge(category, badge), hero.X + (hero.Width - badge) / 2, hero.Y + hero.Height / 2 - badge + Ui.S(8), badge, badge);
            TextRenderer.DrawText(g, "Chưa có ảnh cho địa điểm này", ThemeFonts.Medium(13), new Rectangle(hero.X, hero.Y + hero.Height / 2 + Ui.S(14), hero.Width, Ui.S(24)), Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }

        private void PaintArrow(Graphics g, bool next)
        {
            Rectangle r = ArrowRect(next);
            int id = next ? 1 : 0;
            bool hot = hoverId == id;
            Color fill = press.Fill(id, Color.FromArgb(hot ? 250 : 215, 255, 255, 255), Color.FromArgb(255, 226, 232, 240));
            int sink = press.SinkFor(id);
            using (var shadow = new SolidBrush(Color.FromArgb(40, 15, 23, 42))) g.FillEllipse(shadow, r.X, r.Y + 1, r.Width, r.Height);
            using (var brush = new SolidBrush(fill)) g.FillEllipse(brush, r);
            int px = (int)(r.Width * 0.55);
            Bitmap chevron = IconFont.Get("chevron_right", px, ThemeColors.Slate700);
            if (!next)
            {
                using (var flipped = (Bitmap)chevron.Clone())
                {
                    flipped.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    g.DrawImage(flipped, r.X + (r.Width - px) / 2, r.Y + (r.Height - px) / 2 + sink, px, px);
                }
            }
            else
            {
                g.DrawImage(chevron, r.X + (r.Width - px) / 2, r.Y + (r.Height - px) / 2 + sink, px, px);
            }
        }

        private void PaintCounter(Graphics g, Rectangle hero)
        {
            string text = (index + 1) + " / " + paths.Count;
            Font font = ThemeFonts.SemiBold(12);
            Size size = TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            var pill = new Rectangle(hero.Right - size.Width - Ui.S(34), hero.Bottom - Ui.S(34), size.Width + Ui.S(22), Ui.S(24));
            using (GraphicsPath path = Ui.RoundedRect(pill, pill.Height / 2f))
            using (var brush = new SolidBrush(Color.FromArgb(150, 15, 23, 42))) g.FillPath(brush, path);
            TextRenderer.DrawText(g, text, font, pill, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }

        private void PaintStrip(Graphics g)
        {
            for (int i = 0; i < paths.Count; i++)
            {
                Rectangle r = ThumbRect(i);
                int id = 2 + i;
                bool selected = i == index;
                int sink = press.SinkFor(id);
                var box = new Rectangle(r.X, r.Y + sink, r.Width, r.Height);
                using (GraphicsPath clip = Ui.RoundedRect(box, Ui.S(8)))
                {
                    GraphicsState state = g.Save();
                    g.SetClip(clip, CombineMode.Intersect);
                    Bitmap thumb = PoiPhotos.Load(paths[i], Math.Max(r.Width, r.Height) * 2);
                    if (thumb != null) AppImages.DrawCover(g, thumb, box);
                    else using (var brush = new SolidBrush(ThemeColors.Slate200)) g.FillRectangle(brush, box);
                    if (!selected && hoverId != id)
                    {
                        using (var dim = new SolidBrush(Color.FromArgb(70, 255, 255, 255))) g.FillRectangle(dim, box);
                    }
                    g.Restore(state);
                    using (var pen = new Pen(selected ? ThemeColors.Primary : ThemeColors.Slate200, selected ? 2.4f : 1f)) g.DrawPath(pen, clip);
                }
            }
        }
    }
}
