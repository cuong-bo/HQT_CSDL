using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Account
{
    internal enum PillTone { Neutral, Info, Good, Warn, Bad }

    /// <summary>One row of an <see cref="ItemList"/>: a title, a few lines under it, an optional status pill and a round icon or initials.</summary>
    internal sealed class ListItem
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }

        /// <summary>A short status word on the right of the title ("Chờ duyệt").</summary>
        public string Pill { get; set; }

        public PillTone Tone { get; set; }

        /// <summary>Name of a glyph for the round tile on the left; when empty the initials of the title are shown.</summary>
        public string Icon { get; set; }

        /// <summary>Whatever the screen needs back when the row is chosen (an id, the row's JSON).</summary>
        public object Data { get; set; }
    }

    /// <summary>
    /// The rows of a list on a screen: a round tile, a bold title, a status pill and wrapped text underneath, with an optional small
    /// button on the right (remove, open). Drawn at the screen's resolution; the control is as tall as its rows (<see cref="WantedHeight"/>)
    /// and the screen around it scrolls. A row darkens and sinks when pressed.
    /// </summary>
    internal sealed class ItemList : Control
    {
        private const int BaseRowHeight = 62;
        private const int LineHeight = 17;
        private const TextFormatFlags LineFlags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;

        private readonly PressFeedback press;
        private List<ListItem> items = new List<ListItem>();
        private int hover = -1;
        private bool hoverTrailing;
        private int selected = -1;

        /// <summary>Raised with the row that was pressed (not on its trailing button).</summary>
        public event EventHandler<ListItem> ItemClicked;

        /// <summary>Raised with the row whose trailing button was pressed.</summary>
        public event EventHandler<ListItem> TrailingClicked;

        public ItemList()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            press = new PressFeedback(this);
        }

        /// <summary>How many lines of the text under the title are shown (1 to 4).</summary>
        public int SubtitleLines { get; set; } = 1;

        public string EmptyText { get; set; } = "Chưa có gì để hiển thị.";

        /// <summary>Glyph of the small button on the right of every row ("close", "chevron_right"); empty for none.</summary>
        public string TrailingIcon { get; set; } = "";

        /// <summary>Colour of the trailing glyph when the pointer is over it.</summary>
        public Color TrailingHotColor { get; set; } = ThemeColors.Red500;

        public bool Selectable { get; set; }

        public IReadOnlyList<ListItem> Items => items;

        public ListItem SelectedItem => selected >= 0 && selected < items.Count ? items[selected] : null;

        public int SelectedIndex
        {
            get { return selected; }
            set { selected = value >= 0 && value < items.Count ? value : -1; Invalidate(); }
        }

        private int RowHeight => Ui.S(BaseRowHeight + (Math.Max(1, Math.Min(4, SubtitleLines)) - 1) * LineHeight);

        /// <summary>The height needed for all the rows (or for the empty message).</summary>
        public int WantedHeight => items.Count == 0 ? Ui.S(64) : items.Count * RowHeight;

        public void SetItems(IEnumerable<ListItem> value, bool keepSelection = false)
        {
            object keep = keepSelection ? SelectedItem?.Data : null;
            items = new List<ListItem>(value);
            hover = -1;
            selected = -1;
            if (keep != null) selected = items.FindIndex(i => Equals(i.Data, keep));
            Invalidate();
        }

        private bool HasTrailing => !string.IsNullOrEmpty(TrailingIcon);

        private Rectangle TrailingRect(int row)
        {
            int size = Ui.S(32);
            return new Rectangle(Width - Ui.S(12) - size, row * RowHeight + (RowHeight - size) / 2, size, size);
        }

        private int RowAt(Point p) => p.Y >= 0 && RowHeight > 0 && p.Y / RowHeight < items.Count ? p.Y / RowHeight : -1;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int row = RowAt(e.Location);
            bool trailing = row >= 0 && HasTrailing && TrailingRect(row).Contains(e.Location);
            if (row != hover || trailing != hoverTrailing)
            {
                hover = row;
                hoverTrailing = trailing;
                Cursor = row >= 0 && (Selectable || ItemClicked != null || trailing) ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = -1;
            hoverTrailing = false;
            press.Up();
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            int row = RowAt(e.Location);
            if (e.Button == MouseButtons.Left && row >= 0) press.Down(row, e.Location);
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
            int row = RowAt(e.Location);
            if (e.Button != MouseButtons.Left || row < 0) return;
            if (HasTrailing && TrailingRect(row).Contains(e.Location)) { TrailingClicked?.Invoke(this, items[row]); return; }
            if (Selectable) { selected = row; Invalidate(); }
            ItemClicked?.Invoke(this, items[row]);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) press.Dispose();
            base.Dispose(disposing);
        }

        private static void Tone(PillTone tone, out Color fill, out Color text)
        {
            switch (tone)
            {
                case PillTone.Info: fill = ThemeColors.Sky100; text = ThemeColors.Primary; break;
                case PillTone.Good: fill = Color.FromArgb(209, 250, 229); text = ThemeColors.Emerald700; break;
                case PillTone.Warn: fill = ThemeColors.Amber100; text = ThemeColors.Amber700; break;
                case PillTone.Bad: fill = Color.FromArgb(254, 226, 226); text = Color.FromArgb(185, 28, 28); break;
                default: fill = ThemeColors.Slate100; text = ThemeColors.Slate600; break;
            }
        }

        private static string Initials(string name)
        {
            string[] words = (name ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return "?";
            if (words.Length == 1) return words[0].Substring(0, 1).ToUpperInvariant();
            return (words[0].Substring(0, 1) + words[words.Length - 1].Substring(0, 1)).ToUpperInvariant();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            if (items.Count == 0)
            {
                TextRenderer.DrawText(g, EmptyText, ThemeFonts.Regular(13), new Rectangle(Ui.S(16), 0, Width - Ui.S(32), Height), ThemeColors.Slate500,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
                return;
            }

            int lines = Math.Max(1, Math.Min(4, SubtitleLines));
            for (int i = 0; i < items.Count; i++)
            {
                ListItem item = items[i];
                int top = i * RowHeight;
                var rowRect = new Rectangle(0, top, Width, RowHeight);
                int sink = press.SinkFor(i);

                Color rest = i == selected ? ThemeColors.Sky50 : i == hover ? ThemeColors.Slate50 : Color.White;
                Color fill = press.Fill(i, rest, ThemeColors.Slate200);
                if (fill != Color.White) using (var brush = new SolidBrush(fill)) g.FillRectangle(brush, rowRect);
                if (i == selected) using (var bar = new SolidBrush(ThemeColors.Primary)) g.FillRectangle(bar, 0, top, Ui.S(3), RowHeight);

                int tile = Ui.S(38);
                var tileRect = new Rectangle(Ui.S(16), top + Ui.S(12) + sink, tile, tile);
                if (!string.IsNullOrEmpty(item.Icon))
                {
                    using (var brush = new SolidBrush(ThemeColors.Sky100)) g.FillEllipse(brush, tileRect);
                    int glyph = (int)(tile * 0.56);
                    g.DrawImage(IconFont.Get(item.Icon, glyph, ThemeColors.Primary), tileRect.X + (tile - glyph) / 2, tileRect.Y + (tile - glyph) / 2, glyph, glyph);
                }
                else
                {
                    using (var brush = new LinearGradientBrush(tileRect, ThemeColors.Sky500, ThemeColors.Sky700, LinearGradientMode.ForwardDiagonal)) g.FillEllipse(brush, tileRect);
                    TextRenderer.DrawText(g, Initials(item.Title), ThemeFonts.Bold(13), tileRect, Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                }

                int textLeft = tileRect.Right + Ui.S(12);
                int right = Width - Ui.S(16) - (HasTrailing ? Ui.S(32) + Ui.S(4) : 0);

                int pillWidth = 0;
                if (!string.IsNullOrEmpty(item.Pill))
                {
                    Font pillFont = ThemeFonts.SemiBold(10);
                    pillWidth = TextRenderer.MeasureText(item.Pill, pillFont, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width + Ui.S(16);
                    var pill = new Rectangle(right - pillWidth, top + Ui.S(12) + sink, pillWidth, Ui.S(20));
                    Color pillFill, pillText;
                    Tone(item.Tone, out pillFill, out pillText);
                    using (GraphicsPath path = Ui.RoundedRect(pill, pill.Height / 2f))
                    using (var brush = new SolidBrush(pillFill)) g.FillPath(brush, path);
                    TextRenderer.DrawText(g, item.Pill, pillFont, pill, pillText,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
                    pillWidth += Ui.S(8);
                }
                TextRenderer.DrawText(g, item.Title ?? "", ThemeFonts.SemiBold(14), new Rectangle(textLeft, top + Ui.S(10) + sink, Math.Max(Ui.S(40), right - textLeft - pillWidth), Ui.S(22)), ThemeColors.Slate800, LineFlags);
                if (!string.IsNullOrEmpty(item.Subtitle))
                {
                    TextRenderer.DrawText(g, item.Subtitle, ThemeFonts.Regular(12), new Rectangle(textLeft, top + Ui.S(34) + sink, Math.Max(Ui.S(40), right - textLeft), Ui.S(LineHeight) * lines), ThemeColors.Slate500,
                        lines == 1 ? LineFlags : TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                }

                if (HasTrailing)
                {
                    Rectangle t = TrailingRect(i);
                    bool hot = i == hover && hoverTrailing;
                    if (hot) using (var brush = new SolidBrush(ThemeColors.Slate100)) g.FillEllipse(brush, t);
                    int px = (int)(t.Width * 0.62);
                    g.DrawImage(IconFont.Get(TrailingIcon, px, hot ? TrailingHotColor : ThemeColors.Slate400), t.X + (t.Width - px) / 2, t.Y + (t.Height - px) / 2, px, px);
                }

                if (i < items.Count - 1)
                {
                    using (var pen = new Pen(ThemeColors.Slate100)) g.DrawLine(pen, textLeft, rowRect.Bottom - 1, Width - Ui.S(16), rowRect.Bottom - 1);
                }
            }
        }
    }
}
