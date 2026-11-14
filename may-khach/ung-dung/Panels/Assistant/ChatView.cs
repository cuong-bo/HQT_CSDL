using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Hue_travel.MapEngine;
using Hue_travel.Models;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Panels.Assistant
{
    /// <summary>
    /// The conversation, drawn at the screen's resolution: the person's questions on the right, the assistant's answers on the left, the
    /// places an answer names as chips under it, and a card with a button for a route the assistant planned. One control draws all the
    /// messages (they differ in height), so only the ones in view cost anything. The text is plain: Markdown is removed by the server.
    /// </summary>
    internal sealed class ChatView : Control
    {
        private sealed class Chip
        {
            public Rectangle Bounds;
            public string Text;
            public ChatPlaceInfo Place;     // a place chip ...
            public string Suggestion;       // ... or a suggestion the person can press to ask it
        }

        private sealed class Item
        {
            public bool IsUser, IsError, Pending;
            public string Text;
            public List<ChatPlaceInfo> Places = new List<ChatPlaceInfo>();
            public ChatRouteInfo Route;
            public List<string> Suggestions = new List<string>();

            // layout, valid for LaidOutWidth
            public int LaidOutWidth, Height;
            public Rectangle Bubble, RouteCard, RouteButton;
            public readonly List<Chip> Chips = new List<Chip>();
        }

        private const TextFormatFlags TextFlags = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
        private const int RouteButtonHover = -2;

        private readonly List<Item> items = new List<Item>();
        private int scroll;
        private bool stickToBottom = true;
        private int hoverChip = -1;      // index into the item's chips, -1 none, RouteButtonHover the route button
        private Item hoverItem;

        /// <summary>Raised when a place chip is pressed.</summary>
        public event EventHandler<ChatPlaceInfo> PlaceClicked;

        /// <summary>Raised when "Xem trên bản đồ" of a planned route is pressed.</summary>
        public event EventHandler<ChatRouteInfo> RouteClicked;

        /// <summary>Raised when a suggestion chip of the greeting is pressed.</summary>
        public event EventHandler<string> SuggestionClicked;

        /// <summary>Raised when the person copies an answer (right click).</summary>
        public event EventHandler TextCopied;

        public ChatView()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            BackColor = Color.White;
        }

        public bool IsEmpty => items.Count == 0;

        // ------------------------------------------------------------------ content

        public void Clear()
        {
            items.Clear();
            scroll = 0;
            stickToBottom = true;
            Invalidate();
        }

        public void AddGreeting(string text, IEnumerable<string> suggestions)
        {
            var item = new Item { Text = text };
            item.Suggestions.AddRange(suggestions);
            Add(item);
        }

        public void AddUser(string text) => Add(new Item { IsUser = true, Text = text });

        public void AddAnswer(ChatAnswer answer)
        {
            var item = new Item { Text = answer.Reply, Route = answer.Route };
            item.Places.AddRange(answer.Places);
            Add(item);
        }

        public void AddError(string text) => Add(new Item { IsError = true, Text = text });

        /// <summary>Shows "Đang trả lời…" under the question until <see cref="RemovePending"/>.</summary>
        public void SetPending(string text)
        {
            Item pending = items.LastOrDefault(i => i.Pending);
            if (pending == null) { Add(new Item { Pending = true, Text = text }); return; }
            pending.Text = text;
            pending.LaidOutWidth = 0;
            Relayout();
        }

        public void RemovePending()
        {
            items.RemoveAll(i => i.Pending);
            Relayout();
        }

        private void Add(Item item)
        {
            items.Add(item);
            Relayout();
        }

        private void Relayout()
        {
            if (stickToBottom) scroll = int.MaxValue;
            Invalidate();
        }

        // ------------------------------------------------------------------ layout

        private int Pad => Ui.S(14);

        private int Gap => Ui.S(10);

        private static Font BodyFont => ThemeFonts.Regular(13);

        private static Font ChipFont => ThemeFonts.Medium(12);

        // Lays one item out for a width (cached until the width or the content changes); returns its height.
        private int Measure(Item item, int width)
        {
            if (item.LaidOutWidth == width) return item.Height;
            item.LaidOutWidth = width;
            item.Chips.Clear();
            int maxBubble = (int)(width * 0.86) - Pad;
            int padX = Ui.S(13), padY = Ui.S(9);
            Size text = TextRenderer.MeasureText(item.Text ?? "", BodyFont, new Size(Math.Max(Ui.S(40), maxBubble - padX * 2), int.MaxValue), TextFlags);
            int bubbleW = Math.Min(maxBubble, text.Width + padX * 2 + 2), bubbleH = text.Height + padY * 2;
            int x = item.IsUser ? width - Pad - bubbleW : Pad;
            item.Bubble = new Rectangle(x, 0, bubbleW, bubbleH);
            int y = bubbleH;

            if (item.Places.Count > 0 || item.Suggestions.Count > 0)
            {
                y += Ui.S(8);
                int chipX = Pad, chipH = Ui.S(30), chipGap = Ui.S(6), limit = width - Pad;
                IEnumerable<Chip> chips = item.Suggestions.Count > 0
                    ? item.Suggestions.Select(s => new Chip { Text = s, Suggestion = s })
                    : item.Places.Select(p => new Chip { Text = p.Name, Place = p });
                foreach (Chip chip in chips)
                {
                    int w = TextRenderer.MeasureText(chip.Text, ChipFont, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width
                            + Ui.S(24) + (chip.Place != null ? Ui.S(24) : 0);
                    w = Math.Min(w, limit - Pad);
                    if (chipX + w > limit && chipX > Pad) { chipX = Pad; y += chipH + chipGap; }
                    chip.Bounds = new Rectangle(chipX, y, w, chipH);
                    item.Chips.Add(chip);
                    chipX += w + chipGap;
                }
                y += chipH;
            }

            if (item.Route != null)
            {
                y += Ui.S(8);
                item.RouteCard = new Rectangle(Pad, y, width - Pad * 2, Ui.S(68));
                int bw = Ui.S(150), bh = Ui.S(34);
                item.RouteButton = new Rectangle(item.RouteCard.Right - bw - Ui.S(12), item.RouteCard.Y + (item.RouteCard.Height - bh) / 2, bw, bh);
                y += item.RouteCard.Height;
            }
            item.Height = y + Gap;
            return item.Height;
        }

        private int TotalHeight()
        {
            int total = Gap;
            foreach (Item item in items) total += Measure(item, Width);
            return total;
        }

        private int MaxScroll => Math.Max(0, TotalHeight() - Height);

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            foreach (Item item in items) item.LaidOutWidth = 0;
            if (stickToBottom) scroll = int.MaxValue;
        }

        // ------------------------------------------------------------------ input

        private bool HitTest(Point p, out Item hit, out Chip chip, out bool routeButton)
        {
            hit = null; chip = null; routeButton = false;
            int current = scroll == int.MaxValue ? MaxScroll : scroll;
            int y = Gap - current;
            foreach (Item item in items)
            {
                int h = Measure(item, Width);
                if (p.Y >= y && p.Y < y + h)
                {
                    hit = item;
                    var local = new Point(p.X, p.Y - y);
                    chip = item.Chips.FirstOrDefault(c => c.Bounds.Contains(local));
                    routeButton = item.Route != null && item.RouteButton.Contains(local);
                    return true;
                }
                y += h;
            }
            return false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            Item item; Chip chip; bool button;
            HitTest(e.Location, out item, out chip, out button);
            Cursor = chip != null || button ? Cursors.Hand : Cursors.Default;
            int chipIndex = chip != null ? item.Chips.IndexOf(chip) : (button ? RouteButtonHover : -1);
            if (hoverItem != item || hoverChip != chipIndex)
            {
                hoverItem = item;
                hoverChip = chipIndex;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hoverItem = null; hoverChip = -1; Cursor = Cursors.Default;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int max = MaxScroll;
            int current = scroll == int.MaxValue ? max : scroll;
            scroll = Math.Max(0, Math.Min(max, current - Math.Sign(e.Delta) * Ui.S(60)));
            stickToBottom = scroll >= max - Ui.S(4);
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            Item item; Chip chip; bool button;
            if (!HitTest(e.Location, out item, out chip, out button)) return;
            if (e.Button == MouseButtons.Right)
            {
                if (!item.IsUser && !item.Pending && !string.IsNullOrEmpty(item.Text))
                {
                    try { Clipboard.SetText(item.Text); TextCopied?.Invoke(this, EventArgs.Empty); }
                    catch (System.Runtime.InteropServices.ExternalException) { /* the clipboard is busy: nothing to do */ }
                }
                return;
            }
            if (e.Button != MouseButtons.Left) return;
            if (chip != null)
            {
                if (chip.Place != null) PlaceClicked?.Invoke(this, chip.Place);
                else SuggestionClicked?.Invoke(this, chip.Suggestion);
            }
            else if (button) RouteClicked?.Invoke(this, item.Route);
        }

        // ------------------------------------------------------------------ painting

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            if (scroll == int.MaxValue) scroll = MaxScroll;
            scroll = Math.Max(0, Math.Min(scroll, MaxScroll));

            int y = Gap - scroll;
            foreach (Item item in items)
            {
                int h = Measure(item, Width);
                if (y + h >= 0 && y <= Height) PaintItem(g, item, y);
                y += h;
            }
            PaintScrollBar(g);
        }

        private void PaintItem(Graphics g, Item item, int top)
        {
            Rectangle bubble = item.Bubble;
            bubble.Offset(0, top);
            Color fill = item.IsUser ? ThemeColors.Primary : item.IsError ? Color.FromArgb(254, 226, 226) : ThemeColors.Slate100;
            Color ink = item.IsUser ? Color.White : item.IsError ? Color.FromArgb(153, 27, 27) : ThemeColors.Slate800;
            if (item.Pending) ink = ThemeColors.Slate500;
            using (GraphicsPath path = Ui.RoundedRect(bubble, Ui.S(ThemeMetrics.RadiusMd)))
            using (var brush = new SolidBrush(fill))
            {
                g.FillPath(brush, path);
            }
            TextRenderer.DrawText(g, item.Text ?? "", BodyFont, new Rectangle(bubble.X + Ui.S(13), bubble.Y + Ui.S(9), bubble.Width - Ui.S(26), bubble.Height - Ui.S(18)), ink, TextFlags);

            for (int i = 0; i < item.Chips.Count; i++)
            {
                Chip chip = item.Chips[i];
                Rectangle box = chip.Bounds;
                box.Offset(0, top);
                bool hot = hoverItem == item && hoverChip == i;
                using (GraphicsPath path = Ui.RoundedRect(new RectangleF(box.X + 0.5f, box.Y + 0.5f, box.Width - 1f, box.Height - 1f), box.Height / 2f))
                using (var brush = new SolidBrush(hot ? ThemeColors.Sky50 : Color.White))
                using (var pen = new Pen(hot ? ThemeColors.Primary : ThemeColors.Slate300))
                {
                    g.FillPath(brush, path);
                    g.DrawPath(pen, path);
                }
                int textLeft = box.X + Ui.S(12);
                if (chip.Place != null)
                {
                    int icon = Ui.S(18);
                    g.DrawImage(PoiMarkerIcons.Badge(CategoryOf(chip.Place.Group), icon), box.X + Ui.S(7), box.Y + (box.Height - icon) / 2, icon, icon);
                    textLeft = box.X + Ui.S(30);
                }
                TextRenderer.DrawText(g, chip.Text, ChipFont, new Rectangle(textLeft, box.Y, Math.Max(1, box.Right - textLeft - Ui.S(10)), box.Height), ThemeColors.Slate700,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            }

            if (item.Route != null) PaintRouteCard(g, item, top);
        }

        private void PaintRouteCard(Graphics g, Item item, int top)
        {
            Rectangle card = item.RouteCard;
            card.Offset(0, top);
            Rectangle button = item.RouteButton;
            button.Offset(0, top);
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(card.X + 0.5f, card.Y + 0.5f, card.Width - 1f, card.Height - 1f), Ui.S(ThemeMetrics.RadiusMd)))
            using (var brush = new SolidBrush(ThemeColors.Sky50))
            using (var pen = new Pen(ThemeColors.Sky200))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }
            int icon = Ui.S(22);
            g.DrawImage(IconFont.Get("route", icon, ThemeColors.Primary), card.X + Ui.S(12), card.Y + Ui.S(12), icon, icon);
            int textLeft = card.X + Ui.S(42), textRight = button.X - Ui.S(8);
            const TextFormatFlags oneLine = TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
            TextRenderer.DrawText(g, "Lộ trình gợi ý", ThemeFonts.SemiBold(13), new Rectangle(textLeft, card.Y + Ui.S(10), Math.Max(1, textRight - textLeft), Ui.S(22)), ThemeColors.Slate800, oneLine);
            TextRenderer.DrawText(g, item.Route.Summary, ThemeFonts.Regular(12), new Rectangle(textLeft, card.Y + Ui.S(34), Math.Max(1, textRight - textLeft), Ui.S(22)), ThemeColors.Slate600, oneLine);

            bool hot = hoverItem == item && hoverChip == RouteButtonHover;
            using (GraphicsPath path = Ui.RoundedRect(button, Ui.S(ThemeMetrics.RadiusSm)))
            using (var brush = new SolidBrush(hot ? ThemeColors.PrimaryContainer : ThemeColors.Primary))
            {
                g.FillPath(brush, path);
            }
            TextRenderer.DrawText(g, "Xem trên bản đồ", ThemeFonts.SemiBold(12), button, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        }

        private void PaintScrollBar(Graphics g)
        {
            int max = MaxScroll;
            if (max <= 0) return;
            int track = Height - Ui.S(8);
            int thumb = Math.Max(Ui.S(28), (int)((long)track * Height / (Height + max)));
            int top = Ui.S(4) + (int)((long)(track - thumb) * Math.Min(scroll, max) / max);
            using (GraphicsPath path = Ui.RoundedRect(new Rectangle(Width - Ui.S(8), top, Ui.S(5), thumb), Ui.S(3)))
            using (var brush = new SolidBrush(ThemeColors.Slate300))
            {
                g.FillPath(brush, path);
            }
        }

        private static PoiCategory CategoryOf(string group)
        {
            switch (group)
            {
                case "scenery": return PoiCategory.Scenery;
                case "food": return PoiCategory.Food;
                case "stay": return PoiCategory.Stay;
                case "service": return PoiCategory.Service;
                case "transport": return PoiCategory.Transport;
                default: return PoiCategory.Heritage;
            }
        }
    }
}
