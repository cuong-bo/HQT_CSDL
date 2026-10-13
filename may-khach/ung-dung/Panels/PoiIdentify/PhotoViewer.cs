using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Hue_travel.Services.Data;
using Hue_travel.Theme;

namespace Hue_travel.Panels.PoiIdentify
{
    /// <summary>
    /// Full-size photo viewer: the photo on a dark backdrop over the main window, previous / next buttons, a counter and a close
    /// button. Left / right keys change photo, Escape or a click outside the photo closes it.
    /// </summary>
    internal sealed class PhotoViewer : Form
    {
        private const int ButtonDesignSize = 48;

        private readonly IReadOnlyList<string> paths;
        private readonly string title;
        private int index;
        private int hover = -1;   // 0 previous, 1 next, 2 close

        private PhotoViewer(IReadOnlyList<string> paths, int index, string title)
        {
            this.paths = paths;
            this.index = Math.Max(0, Math.Min(paths.Count - 1, index));
            this.title = title ?? "";
            Icon = AppImages.AppIcon();
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            KeyPreview = true;
            BackColor = Color.FromArgb(15, 23, 42);
            Opacity = 0.97;
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        /// <summary>Opens the viewer over <paramref name="owner"/> (modal) at photo <paramref name="index"/>.</summary>
        public static void ShowOver(Form owner, IReadOnlyList<string> paths, int index, string title)
        {
            if (paths == null || paths.Count == 0) return;
            using (var viewer = new PhotoViewer(paths, index, title))
            {
                viewer.Bounds = owner.Bounds;
                viewer.ShowDialog(owner);
            }
        }

        private Rectangle ButtonRect(int id)
        {
            int size = Ui.S(ButtonDesignSize);
            if (id == 2) return new Rectangle(Width - size - Ui.S(24), Ui.S(24), size, size);
            int x = id == 0 ? Ui.S(24) : Width - size - Ui.S(24);
            return new Rectangle(x, (Height - size) / 2, size, size);
        }

        private int HitButton(Point p)
        {
            if (ButtonRect(2).Contains(p)) return 2;
            if (paths.Count > 1 && ButtonRect(0).Contains(p)) return 0;
            if (paths.Count > 1 && ButtonRect(1).Contains(p)) return 1;
            return -1;
        }

        private Rectangle ImageArea => new Rectangle(Ui.S(96), Ui.S(72), Width - Ui.S(192), Height - Ui.S(72) - Ui.S(80));

        private void Step(int delta)
        {
            if (paths.Count < 2) return;
            index = (index + delta + paths.Count) % paths.Count;
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Close();
            else if (e.KeyCode == Keys.Left) Step(-1);
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Space) Step(1);
            base.OnKeyDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int id = HitButton(e.Location);
            if (id != hover) { hover = id; Cursor = id >= 0 ? Cursors.Hand : Cursors.Default; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;
            int id = HitButton(e.Location);
            if (id == 0) Step(-1);
            else if (id == 1) Step(1);
            else if (id == 2) Close();
            else if (!ImageArea.Contains(e.Location)) Close();   // a click on the backdrop closes
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            Step(e.Delta < 0 ? 1 : -1);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            Rectangle area = ImageArea;
            Rectangle screen = Screen.FromControl(this).Bounds;
            Bitmap photo = PoiPhotos.Load(paths[index], Math.Max(screen.Width, screen.Height));
            if (photo != null)
            {
                float scale = Math.Min(area.Width / (float)photo.Width, area.Height / (float)photo.Height);
                float w = photo.Width * scale, h = photo.Height * scale;
                var target = new RectangleF(area.X + (area.Width - w) / 2f, area.Y + (area.Height - h) / 2f, w, h);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                using (GraphicsPath path = Ui.RoundedRect(target, Ui.S(10)))
                {
                    GraphicsState state = g.Save();
                    g.SetClip(path, CombineMode.Intersect);
                    g.DrawImage(photo, target);
                    g.Restore(state);
                }
            }
            else
            {
                TextRenderer.DrawText(g, "Không mở được ảnh này", ThemeFonts.Medium(15), area, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }

            TextRenderer.DrawText(g, title, ThemeFonts.Bold(17), new Rectangle(Ui.S(32), Ui.S(24), Width - Ui.S(160), Ui.S(40)), Color.White,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            string counter = (index + 1) + " / " + paths.Count + (paths.Count > 1 ? "   ·   ← → để chuyển ảnh   ·   Esc để đóng" : "   ·   Esc để đóng");
            TextRenderer.DrawText(g, counter, ThemeFonts.Medium(13), new Rectangle(0, Height - Ui.S(64), Width, Ui.S(40)), Color.FromArgb(200, 255, 255, 255),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

            PaintButton(g, 2, "close", false);
            if (paths.Count > 1)
            {
                PaintButton(g, 0, "chevron_right", true);
                PaintButton(g, 1, "chevron_right", false);
            }
        }

        private void PaintButton(Graphics g, int id, string icon, bool flip)
        {
            Rectangle r = ButtonRect(id);
            using (var brush = new SolidBrush(Color.FromArgb(hover == id ? 90 : 55, 255, 255, 255))) g.FillEllipse(brush, r);
            int px = (int)(r.Width * 0.5);
            Bitmap glyph = IconFont.Get(icon, px, Color.White);
            if (flip)
            {
                using (var flipped = (Bitmap)glyph.Clone())
                {
                    flipped.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    g.DrawImage(flipped, r.X + (r.Width - px) / 2, r.Y + (r.Height - px) / 2, px, px);
                }
            }
            else
            {
                g.DrawImage(glyph, r.X + (r.Width - px) / 2, r.Y + (r.Height - px) / 2, px, px);
            }
        }
    }
}
