using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Hue_travel.MapEngine;
using Hue_travel.Models;
using Hue_travel.Theme;

namespace Hue_travel.Panels.TravelGuide
{
    /// <summary>A tour together with the places its ids stand for (the first place is the start).</summary>
    internal sealed class TourEntry
    {
        public GuideTour Tour { get; set; }
        public IReadOnlyList<Poi> Places { get; set; }
    }

    /// <summary>
    /// The suggested tours as cards: a coloured icon, the title, a badge, the length and the places. One card is open at a time;
    /// it lists its stops in order and has the "Dùng tour này" button. Drawn at the screen's resolution; the control grows with
    /// the open card (<see cref="WantedHeight"/>, <see cref="HeightChanged"/>).
    /// </summary>
    internal sealed class TourList : Control
    {
        private const int Margin = 14;
        private const int CardGap = 10;
        private const int ClosedHeight = 112;
        private const int OpenHeaderHeight = 66;
        private const int StopRowHeight = 30;
        private const int UseButtonHeight = 42;
        private const TextFormatFlags MeasureFlags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
        private const TextFormatFlags LineFlags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;

        private enum Part { None, Card, Use }

        private readonly PressFeedback press;
        private List<TourEntry> tours = new List<TourEntry>();
        private int open;
        private int hoverCard = -1;
        private Part hoverPart = Part.None;

        /// <summary>Raised with the tour whose "Dùng tour này" button was pressed.</summary>
        public event EventHandler<TourEntry> UseRequested;

        /// <summary>Raised when a card opens or closes, so the host can make room.</summary>
        public event EventHandler HeightChanged;

        public TourList()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            press = new PressFeedback(this);
        }

        public void SetTours(IEnumerable<TourEntry> value)
        {
            tours = value.ToList();
            open = tours.Count > 0 ? Math.Min(open, tours.Count - 1) : -1;
            hoverCard = -1;
            Invalidate();
            HeightChanged?.Invoke(this, EventArgs.Empty);
        }

        private int CardHeight(int i) => Ui.S(i == open ? OpenHeaderHeight + tours[i].Places.Count * StopRowHeight + 10 + UseButtonHeight + 14 : ClosedHeight);

        /// <summary>The height needed for the cards as they are now.</summary>
        public int WantedHeight
        {
            get
            {
                if (tours.Count == 0) return Ui.S(60);
                int sum = 0;
                for (int i = 0; i < tours.Count; i++) sum += CardHeight(i) + Ui.S(CardGap);
                return sum - Ui.S(CardGap);
            }
        }

        private Rectangle CardRect(int i)
        {
            int top = 0;
            for (int k = 0; k < i; k++) top += CardHeight(k) + Ui.S(CardGap);
            return new Rectangle(Ui.S(Margin), top, Math.Max(10, Width - Ui.S(Margin) * 2), CardHeight(i));
        }

        private Rectangle UseRect(int i)
        {
            Rectangle card = CardRect(i);
            int h = Ui.S(UseButtonHeight);
            return new Rectangle(card.X + Ui.S(12), card.Bottom - Ui.S(14) - h, card.Width - Ui.S(24), h);
        }

        private Part PartAt(Point p, out int index)
        {
            index = -1;
            for (int i = 0; i < tours.Count; i++)
            {
                if (!CardRect(i).Contains(p)) continue;
                index = i;
                return i == open && UseRect(i).Contains(p) ? Part.Use : Part.Card;
            }
            return Part.None;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int card;
            Part part = PartAt(e.Location, out card);
            if (card != hoverCard || part != hoverPart)
            {
                hoverCard = card;
                hoverPart = part;
                Cursor = part == Part.None ? Cursors.Default : Cursors.Hand;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hoverCard = -1;
            hoverPart = Part.None;
            press.Up();
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            int card;
            Part part = PartAt(e.Location, out card);
            if (e.Button == MouseButtons.Left && part == Part.Use) press.Down(card, e.Location);
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
            int card;
            switch (PartAt(e.Location, out card))
            {
                case Part.Use:
                    UseRequested?.Invoke(this, tours[card]);
                    break;
                case Part.Card:
                    open = open == card ? -1 : card;
                    hoverCard = -1;
                    Invalidate();
                    HeightChanged?.Invoke(this, EventArgs.Empty);
                    break;
            }
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
            if (tours.Count == 0)
            {
                TextRenderer.DrawText(g, "Chưa có dữ liệu địa điểm để gợi ý tour.", ThemeFonts.Regular(13), new Rectangle(Ui.S(14), 0, Width - Ui.S(28), Height),
                    ThemeColors.Slate500, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
                return;
            }
            for (int i = 0; i < tours.Count; i++) PaintCard(g, i);
        }

        private void PaintCard(Graphics g, int i)
        {
            TourEntry entry = tours[i];
            bool isOpen = i == open;
            bool hot = hoverCard == i && hoverPart != Part.None;
            Rectangle card = CardRect(i);
            Color accent = RoutePalette.ColorOf(i);

            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(card.X + 0.5f, card.Y + 0.5f, card.Width - 1f, card.Height - 1f), Ui.S(12)))
            using (var fill = new SolidBrush(isOpen ? Color.White : hot ? ThemeColors.Slate50 : Color.White))
            using (var pen = new Pen(isOpen ? ThemeColors.Primary : ThemeColors.Slate200, isOpen ? 1.5f : 1f))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }

            int icon = Ui.S(44);
            var iconRect = new Rectangle(card.X + Ui.S(12), card.Y + Ui.S(14), icon, icon);
            using (var brush = new SolidBrush(accent)) g.FillEllipse(brush, iconRect);
            int glyph = (int)(icon * 0.56);
            g.DrawImage(IconFont.Get(entry.Tour.Icon, glyph, Color.White), iconRect.X + (icon - glyph) / 2, iconRect.Y + (icon - glyph) / 2, glyph, glyph);

            int chevron = Ui.S(22);
            g.DrawImage(IconFont.Get(isOpen ? "expand_less" : "expand_more", chevron, ThemeColors.Slate500), card.Right - Ui.S(12) - chevron, card.Y + Ui.S(14), chevron, chevron);

            int textLeft = iconRect.Right + Ui.S(12);
            int textRight = card.Right - Ui.S(12) - chevron - Ui.S(6);
            TextRenderer.DrawText(g, entry.Tour.Title, ThemeFonts.Bold(14), new Rectangle(textLeft, card.Y + Ui.S(12), Math.Max(Ui.S(40), textRight - textLeft), Ui.S(22)), ThemeColors.Slate800, LineFlags);

            // the badge pill, then "5 điểm · Khoảng 1 ngày"
            Font badgeFont = ThemeFonts.SemiBold(10);
            int badgeWidth = TextRenderer.MeasureText(entry.Tour.Badge, badgeFont, Size.Empty, MeasureFlags).Width + Ui.S(16);
            var badge = new Rectangle(textLeft, card.Y + Ui.S(38), badgeWidth, Ui.S(20));
            using (GraphicsPath pill = Ui.RoundedRect(badge, badge.Height / 2f))
            using (var brush = new SolidBrush(ThemeColors.Sky100))
                g.FillPath(brush, pill);
            TextRenderer.DrawText(g, entry.Tour.Badge, badgeFont, badge, ThemeColors.Primary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            string meta = entry.Places.Count + " điểm · " + entry.Tour.Duration;
            TextRenderer.DrawText(g, meta, ThemeFonts.Regular(12), new Rectangle(badge.Right + Ui.S(8), badge.Y, Math.Max(Ui.S(30), textRight - badge.Right - Ui.S(8)), badge.Height), ThemeColors.Slate500, LineFlags);

            if (!isOpen)
            {
                string chain = string.Join(" → ", entry.Places.Select(p => p.Name));
                TextRenderer.DrawText(g, chain, ThemeFonts.Regular(12), new Rectangle(card.X + Ui.S(12), card.Y + Ui.S(66), card.Width - Ui.S(24), Ui.S(38)), ThemeColors.Slate600,
                    TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                return;
            }

            // the open card: the stops in order, then the button
            int top = card.Y + Ui.S(OpenHeaderHeight);
            using (var pen = new Pen(ThemeColors.Slate100)) g.DrawLine(pen, card.X + Ui.S(12), top - Ui.S(4), card.Right - Ui.S(12), top - Ui.S(4));
            for (int k = 0; k < entry.Places.Count; k++)
            {
                int rowTop = top + k * Ui.S(StopRowHeight);
                int dot = Ui.S(22);
                var dotRect = new Rectangle(card.X + Ui.S(14), rowTop + (Ui.S(StopRowHeight) - dot) / 2, dot, dot);
                using (var brush = new SolidBrush(RoutePalette.ColorOf(k))) g.FillEllipse(brush, dotRect);
                TextRenderer.DrawText(g, k == 0 ? "A" : k.ToString(), ThemeFonts.Bold(11), dotRect, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                string name = entry.Places[k].Name + (k == 0 ? "  (điểm bắt đầu)" : "");
                TextRenderer.DrawText(g, name, ThemeFonts.Medium(13), new Rectangle(dotRect.Right + Ui.S(10), rowTop, card.Right - dotRect.Right - Ui.S(22), Ui.S(StopRowHeight)), ThemeColors.Slate800, LineFlags);
            }

            Rectangle use = UseRect(i);
            int sink = press.SinkFor(i);
            Color rest = hoverCard == i && hoverPart == Part.Use ? ThemeColors.PrimaryContainer : ThemeColors.Primary;
            Color buttonFill = press.Fill(i, rest, ThemeColors.PrimaryContainer);
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(use.X, use.Y + sink, use.Width, use.Height), Ui.S(10)))
            using (var brush = new SolidBrush(buttonFill))
                g.FillPath(brush, path);
            const string label = "Dùng tour này";
            Font labelFont = ThemeFonts.SemiBold(14);
            int labelWidth = TextRenderer.MeasureText(label, labelFont, Size.Empty, MeasureFlags).Width;
            int glyphSize = Ui.S(18), gap = Ui.S(8);
            int startX = use.X + (use.Width - (glyphSize + gap + labelWidth)) / 2;
            g.DrawImage(IconFont.Get("directions", glyphSize, Color.White), startX, use.Y + sink + (use.Height - glyphSize) / 2, glyphSize, glyphSize);
            TextRenderer.DrawText(g, label, labelFont, new Rectangle(startX + glyphSize + gap, use.Y + sink, labelWidth + Ui.S(4), use.Height), Color.White, LineFlags);
        }
    }
}
