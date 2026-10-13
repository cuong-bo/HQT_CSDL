using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Account
{
    /// <summary>One photo of a <see cref="PhotoGrid"/>: its id on the server, its status and the picture once it has been fetched.</summary>
    internal sealed class PhotoCell
    {
        public long Id { get; set; }

        /// <summary>pending, approved or rejected.</summary>
        public string Status { get; set; }

        public string Caption { get; set; }
        public Bitmap Image { get; set; }
    }

    /// <summary>
    /// Photos as square thumbnails, three to a row, each with a status pill; a small cross removes one. Drawn at the screen's
    /// resolution and as tall as its rows, so the screen around it scrolls.
    /// </summary>
    internal sealed class PhotoGrid : Control
    {
        private const int Columns = 3;
        private const int Gap = 8;

        private List<PhotoCell> cells = new List<PhotoCell>();
        private int hover = -1;
        private bool hoverDelete;
        private int selected = -1;

        public event EventHandler<PhotoCell> CellClicked;
        public event EventHandler<PhotoCell> DeleteClicked;

        public PhotoGrid()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
        }

        public string EmptyText { get; set; } = "Chưa có ảnh nào.";

        /// <summary>When false the cross is not shown (the screen only looks at the photos).</summary>
        public bool CanDelete { get; set; } = true;

        public IReadOnlyList<PhotoCell> Cells => cells;

        public PhotoCell Selected => selected >= 0 && selected < cells.Count ? cells[selected] : null;

        public void SetCells(IEnumerable<PhotoCell> value)
        {
            List<PhotoCell> next = new List<PhotoCell>(value);
            foreach (PhotoCell old in cells) if (old.Image != null && !next.Contains(old)) old.Image.Dispose();
            cells = next;
            hover = selected = -1;
            Invalidate();
        }

        public void SetImage(long id, Bitmap image)
        {
            PhotoCell cell = cells.FirstOrDefault(c => c.Id == id);
            if (cell == null) { image?.Dispose(); return; }
            cell.Image?.Dispose();
            cell.Image = image;
            Invalidate();
        }

        private int CellSize(int width) => (width - Ui.S(Gap) * (Columns - 1)) / Columns;

        /// <summary>The height needed for the rows at a given width.</summary>
        public int HeightFor(int width)
        {
            if (cells.Count == 0) return Ui.S(64);
            int rows = (cells.Count + Columns - 1) / Columns;
            return rows * CellSize(width) + (rows - 1) * Ui.S(Gap);
        }

        private Rectangle CellRect(int i)
        {
            int size = CellSize(Width), gap = Ui.S(Gap);
            return new Rectangle((i % Columns) * (size + gap), (i / Columns) * (size + gap), size, size);
        }

        private Rectangle DeleteRect(int i)
        {
            Rectangle r = CellRect(i);
            int size = Ui.S(26);
            return new Rectangle(r.Right - size - Ui.S(6), r.Y + Ui.S(6), size, size);
        }

        private int CellAt(Point p)
        {
            for (int i = 0; i < cells.Count; i++) if (CellRect(i).Contains(p)) return i;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int index = CellAt(e.Location);
            bool del = CanDelete && index >= 0 && DeleteRect(index).Contains(e.Location);
            if (index != hover || del != hoverDelete)
            {
                hover = index;
                hoverDelete = del;
                Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = -1;
            hoverDelete = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int index = CellAt(e.Location);
            if (e.Button != MouseButtons.Left || index < 0) return;
            if (CanDelete && DeleteRect(index).Contains(e.Location)) { DeleteClicked?.Invoke(this, cells[index]); return; }
            selected = index;
            Invalidate();
            CellClicked?.Invoke(this, cells[index]);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) foreach (PhotoCell cell in cells) cell.Image?.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.Clear(BackColor);
            if (cells.Count == 0)
            {
                TextRenderer.DrawText(g, EmptyText, ThemeFonts.Regular(13), new Rectangle(Ui.S(16), 0, Width - Ui.S(32), Height), ThemeColors.Slate500,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
                return;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                PhotoCell cell = cells[i];
                Rectangle r = CellRect(i);
                using (GraphicsPath path = Ui.RoundedRect(r, Ui.S(10)))
                {
                    GraphicsState state = g.Save();
                    g.SetClip(path, CombineMode.Intersect);
                    if (cell.Image != null)
                    {
                        // fill the square, cutting off whatever sticks out (like a cover photo)
                        float scale = Math.Max(r.Width / (float)cell.Image.Width, r.Height / (float)cell.Image.Height);
                        float w = cell.Image.Width * scale, h = cell.Image.Height * scale;
                        g.DrawImage(cell.Image, r.X + (r.Width - w) / 2f, r.Y + (r.Height - h) / 2f, w, h);
                    }
                    else
                    {
                        using (var brush = new SolidBrush(ThemeColors.Slate100)) g.FillRectangle(brush, r);
                        int glyph = (int)(r.Width * 0.3);
                        g.DrawImage(IconFont.Get("visibility", glyph, ThemeColors.Slate300), r.X + (r.Width - glyph) / 2, r.Y + (r.Height - glyph) / 2, glyph, glyph);
                    }
                    g.Restore(state);
                    using (var pen = new Pen(i == selected ? ThemeColors.Primary : ThemeColors.Slate200, i == selected ? 2f : 1f)) g.DrawPath(pen, path);
                }

                Color fill, text;
                string label = Status(cell.Status, out fill, out text);
                if (label != null)
                {
                    Font font = ThemeFonts.SemiBold(10);
                    int width = TextRenderer.MeasureText(label, font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width + Ui.S(14);
                    var pill = new Rectangle(r.X + Ui.S(6), r.Bottom - Ui.S(26), width, Ui.S(20));
                    using (GraphicsPath path = Ui.RoundedRect(pill, pill.Height / 2f))
                    using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);
                    TextRenderer.DrawText(g, label, font, pill, text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
                }

                if (CanDelete && i == hover)
                {
                    Rectangle d = DeleteRect(i);
                    using (var brush = new SolidBrush(hoverDelete ? ThemeColors.Red500 : Color.FromArgb(200, 255, 255, 255))) g.FillEllipse(brush, d);
                    int px = (int)(d.Width * 0.62);
                    g.DrawImage(IconFont.Get("close", px, hoverDelete ? Color.White : ThemeColors.Slate700), d.X + (d.Width - px) / 2, d.Y + (d.Height - px) / 2, px, px);
                }
            }
        }

        private static string Status(string status, out Color fill, out Color text)
        {
            switch (status)
            {
                case "pending": fill = ThemeColors.Amber100; text = ThemeColors.Amber700; return "Chờ duyệt";
                case "rejected": fill = Color.FromArgb(254, 226, 226); text = Color.FromArgb(185, 28, 28); return "Bị từ chối";
                case "approved": fill = Color.FromArgb(209, 250, 229); text = ThemeColors.Emerald700; return "Đang hiển thị";
                default: fill = Color.White; text = ThemeColors.Slate700; return null;
            }
        }
    }
}
