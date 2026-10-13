using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Hue_travel.Models;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    /// <summary>
    /// The row of category chips under the header (the filter bar of a web map): one chip per kind of place, the chosen one
    /// solid blue. Choosing a chip shows only that kind on the map. Chips darken, sink and ripple when pressed.
    /// </summary>
    public sealed class FilterBar : Control
    {
        private sealed class Chip
        {
            public string Label;
            public string Icon;
            public Color IconColor;
            public PoiCategory? Category;
            public Rectangle Bounds;
        }

        private const int ChipHeight = 30;
        private const int ChipGap = 8;
        private const int SidePadding = 16;
        private const int IconSize = 16;
        private const int InnerPadding = 12;
        private const int InnerGap = 6;

        private readonly Chip[] chips =
        {
            new Chip { Label = "Tất cả", Icon = "apps", IconColor = ThemeColors.Slate600, Category = null },
            new Chip { Label = "Di tích", Icon = "account_balance", IconColor = ThemeColors.CategoryHeritage, Category = PoiCategory.Heritage },
            new Chip { Label = "Cảnh quan", Icon = "landscape", IconColor = ThemeColors.CategoryScenery, Category = PoiCategory.Scenery },
            new Chip { Label = "Ẩm thực", Icon = "restaurant", IconColor = ThemeColors.CategoryFood, Category = PoiCategory.Food },
            new Chip { Label = "Lưu trú", Icon = "hotel", IconColor = ThemeColors.CategoryStay, Category = PoiCategory.Stay },
            new Chip { Label = "Dịch vụ", Icon = "support_agent", IconColor = ThemeColors.Emerald700, Category = PoiCategory.Service },
            new Chip { Label = "Giao thông", Icon = "train", IconColor = ThemeColors.Slate600, Category = PoiCategory.Transport },
        };

        private readonly PressFeedback press;
        private int selectedIndex;
        private int hoverIndex = -1;

        /// <summary>The picked kind of place; null means "Tất cả" (everything).</summary>
        public event EventHandler<PoiCategory?> CategorySelected;

        public FilterBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = ThemeColors.SurfaceContainerLowest;
            Cursor = Cursors.Hand;
            Height = 48;
            press = new PressFeedback(this);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            LayoutChips();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutChips();
        }

        private static Font ChipFont(bool selected) => selected ? ThemeFonts.SemiBold(12) : ThemeFonts.Medium(12);

        private void LayoutChips()
        {
            int x = Ui.S(SidePadding);
            int height = Ui.S(ChipHeight);
            int y = (Height - height) / 2;
            for (int i = 0; i < chips.Length; i++)
            {
                int text = TextRenderer.MeasureText(chips[i].Label, ChipFont(true), Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width;
                int width = Ui.S(InnerPadding) * 2 + Ui.S(IconSize) + Ui.S(InnerGap) + text;
                chips[i].Bounds = new Rectangle(x, y, width, height);
                x += width + Ui.S(ChipGap);
            }
            Invalidate();
        }

        private int IndexAt(Point p)
        {
            for (int i = 0; i < chips.Length; i++)
            {
                if (chips[i].Bounds.Contains(p)) return i;
            }
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int index = IndexAt(e.Location);
            if (index != hoverIndex) { hoverIndex = index; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hoverIndex = -1;
            press.Up();
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            int index = IndexAt(e.Location);
            if (e.Button == MouseButtons.Left && index >= 0) press.Down(index, e.Location);
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
            int index = IndexAt(e.Location);
            if (e.Button != MouseButtons.Left || index < 0) return;
            selectedIndex = index;
            Invalidate();
            CategorySelected?.Invoke(this, chips[index].Category);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) press.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            using (var pen = new Pen(ThemeColors.Slate200)) g.DrawLine(pen, 0, Height - 1, Width, Height - 1);

            for (int i = 0; i < chips.Length; i++)
            {
                Chip chip = chips[i];
                bool selected = i == selectedIndex;
                Rectangle box = chip.Bounds;
                var bounds = new RectangleF(box.X + 0.5f, box.Y + 0.5f, box.Width - 1f, box.Height - 1f);

                Color rest = selected ? ThemeColors.Primary : (i == hoverIndex ? ThemeColors.Slate100 : ThemeColors.SurfaceContainerLowest);
                Color tint = selected ? ThemeColors.Sky800 : ThemeColors.Slate200;
                Color fill = press.Fill(i, rest, tint);
                Color border = selected ? ThemeColors.Primary : (i == hoverIndex ? ThemeColors.Slate400 : ThemeColors.Slate300);

                using (GraphicsPath path = Ui.RoundedRect(bounds, bounds.Height / 2f))
                {
                    using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);
                    press.PaintRipple(g, i, path, selected ? Color.White : ThemeColors.Sky700, selected ? 90 : 60);
                    using (var pen = new Pen(border, 1f)) g.DrawPath(pen, path);
                }

                int sink = press.SinkFor(i);
                int iconPx = Ui.S(IconSize);
                int x = box.X + Ui.S(InnerPadding);
                g.DrawImage(IconFont.Get(chip.Icon, iconPx, selected ? Color.White : chip.IconColor), x, box.Y + (box.Height - iconPx) / 2 + sink, iconPx, iconPx);
                var textBounds = new Rectangle(x + iconPx + Ui.S(InnerGap), box.Y + sink, box.Width, box.Height);
                TextRenderer.DrawText(g, chip.Label, ChipFont(selected), textBounds, selected ? Color.White : ThemeColors.Slate700,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            }
        }
    }
}
