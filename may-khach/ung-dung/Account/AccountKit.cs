using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Account
{
    /// <summary>The look of the "Hồ sơ" screens and the admin console: the same captions, hints, buttons and lines as the route form.</summary>
    internal static class AccountKit
    {
        public static Label Caption(string text) =>
            new Label { AutoSize = false, Text = text, Font = ThemeFonts.Bold(15), ForeColor = ThemeColors.Slate800, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Transparent };

        public static Label Hint(string text) =>
            new Label { AutoSize = false, Text = text, Font = ThemeFonts.Regular(12), ForeColor = ThemeColors.Slate500, TextAlign = ContentAlignment.TopLeft, BackColor = Color.Transparent };

        public static Label FieldLabel(string text) =>
            new Label { AutoSize = false, Text = text, Font = ThemeFonts.SemiBold(12), ForeColor = ThemeColors.Slate700, TextAlign = ContentAlignment.BottomLeft, BackColor = Color.Transparent };

        public static Panel Divider() => new Panel { BackColor = ThemeColors.Slate100, Height = 1 };

        public static IconButton Secondary(string text, string icon = "") => Styled(new IconButton(), text, icon, false, false);

        public static IconButton Primary(string text, string icon = "") => Styled(new IconButton(), text, icon, true, false);

        public static IconButton Danger(string text, string icon = "") => Styled(new IconButton(), text, icon, false, true);

        public static IconButton Styled(IconButton b, string text, string icon, bool primary, bool danger)
        {
            b.AutoWidth = false;
            b.Text = text;
            b.IconName = icon;
            b.IconSize = primary ? 18 : 16;
            b.Gap = primary ? 8 : 6;
            b.HorizontalPadding = 8;
            b.Font = primary ? ThemeFonts.SemiBold(14) : ThemeFonts.Medium(13);
            b.Radius = primary ? 12 : 10;
            b.Elevated = primary;
            if (primary)
            {
                b.ForeColor = Color.White;
                b.IconColor = Color.White;
                b.FillColor = ThemeColors.Primary;
                b.HoverFillColor = ThemeColors.PrimaryContainer;
            }
            else
            {
                b.ForeColor = danger ? ThemeColors.Red500 : ThemeColors.Slate700;
                b.IconColor = danger ? ThemeColors.Red500 : ThemeColors.Primary;
                b.FillColor = Color.White;
                b.HoverFillColor = danger ? Color.FromArgb(254, 242, 242) : ThemeColors.Slate100;
                b.BorderColor = danger ? Color.FromArgb(252, 165, 165) : ThemeColors.Slate300;
            }
            return b;
        }

        public static string RoleName(string role) => role == "admin" ? "Quản trị viên" : role == "owner" ? "Chủ quản" : "Người dùng";

        public static string StatusName(string status) => status == "locked" ? "Đã vô hiệu hóa" : status == "must_change_password" ? "Chờ đổi mật khẩu" : "Đang hoạt động";

        public static string GroupName(string group)
        {
            switch (group)
            {
                case "heritage": return "Di tích";
                case "scenery": return "Cảnh quan";
                case "food": return "Ẩm thực";
                case "stay": return "Lưu trú";
                case "service": return "Dịch vụ";
                case "transport": return "Giao thông";
                default: return group ?? "";
            }
        }

        public static string RequestStatusName(string status) =>
            status == "approved" ? "Đã duyệt" : status == "rejected" ? "Bị từ chối" : "Chờ duyệt";

        /// <summary>An ISO time as dd/MM/yyyy HH:mm in local time.</summary>
        public static string Day(string iso)
        {
            DateTime when;
            return DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind, out when) ? when.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : (iso ?? "");
        }
    }

    /// <summary>
    /// A scrolling column: the controls are stacked from the top, each as wide as the column (minus the scroll bar when needed) and as
    /// tall as it asks for, with a gap after it. Built like the route form's body, in code, so a screen is a list of Add calls.
    /// </summary>
    internal sealed class VStack : NoAutoScrollPanel
    {
        private sealed class Row
        {
            public Control Control;
            public Func<int, int> Height;   // height in pixels for a given width
            public int Gap;
            public bool Indented;
        }

        private readonly List<Row> rows = new List<Row>();
        private bool laying;

        public VStack()
        {
            AutoScroll = true;
            BackColor = Color.White;
            Dock = DockStyle.Fill;
            Padding = new Padding(0);
        }

        public int SidePadding { get; set; } = 16;

        /// <summary>Adds a control of a fixed height (design pixels).</summary>
        public void Add(Control control, int designHeight, int designGap = 10, bool indented = true) =>
            Add(control, width => Ui.S(designHeight), designGap, indented);

        /// <summary>Adds a control whose height is worked out from its width (device pixels).</summary>
        public void Add(Control control, Func<int, int> height, int designGap = 10, bool indented = true)
        {
            rows.Add(new Row { Control = control, Height = height, Gap = designGap, Indented = indented });
            Controls.Add(control);
        }

        public void Clear()
        {
            foreach (Row row in rows) { Controls.Remove(row.Control); row.Control.Dispose(); }
            rows.Clear();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Relayout();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) Relayout();
        }

        /// <summary>Lays the rows out again (call after a row's height or visibility changed).</summary>
        public void Relayout()
        {
            if (laying || ClientSize.Width <= 0 || rows.Count == 0) return;
            laying = true;
            try
            {
                int available = ClientSize.Width + (VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0);
                int total = Place(available, apply: false);
                int width = total > ClientSize.Height ? available - SystemInformation.VerticalScrollBarWidth : available;
                total = Place(width, apply: true);
                AutoScrollMinSize = new Size(0, total);
            }
            finally
            {
                laying = false;
            }
        }

        private int Place(int width, bool apply)
        {
            int offset = apply ? AutoScrollPosition.Y : 0;
            int pad = Ui.S(SidePadding);
            int y = Ui.S(16);
            foreach (Row row in rows)
            {
                int w = row.Indented ? Math.Max(10, width - pad * 2) : width;
                int h = row.Control.Visible ? row.Height(w) : 0;
                if (apply) row.Control.SetBounds(row.Indented ? pad : 0, y + offset, w, h);
                if (h > 0) y += h + Ui.S(row.Gap);
            }
            return y + Ui.S(6);
        }
    }

    /// <summary>A row of pill-shaped tabs ("Thông tin", "Ảnh", ...): the chosen one is filled, each can carry a small count.</summary>
    internal sealed class SegmentTabs : Control
    {
        private readonly PressFeedback press;
        private string[] titles = new string[0];
        private int[] counts = new int[0];
        private int selected;
        private int hover = -1;

        public event EventHandler SelectedChanged;

        public SegmentTabs()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            Cursor = Cursors.Hand;
            press = new PressFeedback(this);
        }

        public int SelectedIndex
        {
            get { return selected; }
            set { if (value != selected && value >= 0 && value < titles.Length) { selected = value; Invalidate(); SelectedChanged?.Invoke(this, EventArgs.Empty); } }
        }

        public void SetTabs(params string[] tabTitles)
        {
            titles = tabTitles;
            counts = new int[tabTitles.Length];
            selected = Math.Min(selected, Math.Max(0, tabTitles.Length - 1));
            Invalidate();
        }

        public void SetCount(int index, int count)
        {
            if (index < 0 || index >= counts.Length || counts[index] == count) return;
            counts[index] = count;
            Invalidate();
        }

        private Rectangle TabRect(int i)
        {
            int gap = Ui.S(6);
            int w = (Width - gap * (titles.Length - 1)) / Math.Max(1, titles.Length);
            return new Rectangle(i * (w + gap), 0, w, Height);
        }

        private int IndexAt(Point p)
        {
            for (int i = 0; i < titles.Length; i++) if (TabRect(i).Contains(p)) return i;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int index = IndexAt(e.Location);
            if (index != hover) { hover = index; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = -1;
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
            if (e.Button == MouseButtons.Left && index >= 0) SelectedIndex = index;
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
            for (int i = 0; i < titles.Length; i++)
            {
                Rectangle r = TabRect(i);
                bool on = i == selected;
                Color rest = on ? ThemeColors.Primary : hover == i ? ThemeColors.Slate100 : Color.White;
                Color fill = press.Fill(i, rest, on ? ThemeColors.PrimaryContainer : ThemeColors.Slate200);
                using (GraphicsPath path = Ui.RoundedRect(new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1f, r.Height - 1f), Ui.S(10)))
                using (var brush = new SolidBrush(fill))
                using (var pen = new Pen(on ? ThemeColors.Primary : ThemeColors.Slate300))
                {
                    g.FillPath(brush, path);
                    g.DrawPath(pen, path);
                }
                Color color = on ? Color.White : ThemeColors.Slate700;
                int badgeWidth = 0;
                if (counts[i] > 0)
                {
                    string number = counts[i] > 99 ? "99+" : counts[i].ToString();
                    badgeWidth = Math.Max(Ui.S(18), TextRenderer.MeasureText(number, ThemeFonts.Bold(10), Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width + Ui.S(10));
                    var badge = new Rectangle(r.Right - badgeWidth - Ui.S(6), r.Y + (r.Height - Ui.S(18)) / 2, badgeWidth, Ui.S(18));
                    using (GraphicsPath pill = Ui.RoundedRect(badge, badge.Height / 2f))
                    using (var brush = new SolidBrush(on ? Color.White : ThemeColors.Red500)) g.FillPath(brush, pill);
                    TextRenderer.DrawText(g, number, ThemeFonts.Bold(10), badge, on ? ThemeColors.Primary : Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    badgeWidth += Ui.S(8);
                }
                TextRenderer.DrawText(g, titles[i], on ? ThemeFonts.SemiBold(12) : ThemeFonts.Medium(12), new Rectangle(r.X + Ui.S(4), r.Y, r.Width - Ui.S(8) - badgeWidth, r.Height), color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            }
        }
    }

    /// <summary>Five stars: click one to choose the rating (or only look, when read-only).</summary>
    internal sealed class StarRating : Control
    {
        private int value;
        private int hover;

        public event EventHandler ValueChanged;

        public StarRating()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
        }

        /// <summary>0 for none, 1 to 5.</summary>
        public int Value
        {
            get { return value; }
            set { int v = Math.Max(0, Math.Min(5, value)); if (v != this.value) { this.value = v; Invalidate(); ValueChanged?.Invoke(this, EventArgs.Empty); } }
        }

        public bool ReadOnly { get; set; }

        /// <summary>The size of one star, in design pixels.</summary>
        public int StarSize { get; set; } = 28;

        /// <summary>The width that five stars take.</summary>
        public int WantedWidth => 5 * Ui.S(StarSize) + 4 * Ui.S(4);

        private int StarAt(Point p)
        {
            int size = Ui.S(StarSize), gap = Ui.S(4);
            int i = p.X / (size + gap);
            return p.X >= 0 && i < 5 && p.Y >= 0 && p.Y < size ? i + 1 : 0;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (ReadOnly) return;
            int star = StarAt(e.Location);
            if (star != hover) { hover = star; Cursor = star > 0 ? Cursors.Hand : Cursors.Default; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = 0;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int star = ReadOnly ? 0 : StarAt(e.Location);
            if (e.Button == MouseButtons.Left && star > 0) Value = star;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            int size = Ui.S(StarSize), gap = Ui.S(4);
            int shown = hover > 0 && !ReadOnly ? hover : value;
            for (int i = 0; i < 5; i++)
                g.DrawImage(IconFont.Get("star", size, i < shown ? ThemeColors.Amber500 : ThemeColors.Slate200), i * (size + gap), 0, size, size);
        }
    }
}
