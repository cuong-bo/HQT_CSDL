using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.MapEngine.Overlays
{
    /// <summary>
    /// A vertical stack of 36px icon buttons in one rounded white box with hairline dividers:
    /// the base-map quick switch and the zoom/pan column on the map. Fills are opaque because
    /// translucency cannot composite over the MapWinGIS ActiveX window.
    /// </summary>
    public class MapToolStack : Control
    {
        private const int ItemSize = 36;

        private string[] icons = new string[0];
        private int activeIndex = -1;
        private int hoverIndex = -1;
        private string[] tips = new string[0];
        private readonly ToolTip tooltip = new ToolTip();
        private readonly PressFeedback press;

        public event EventHandler<int> ItemClicked;

        public MapToolStack()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            ActiveFill = ThemeColors.Sky100;
            ActiveIconColor = ThemeColors.Primary;
            press = new PressFeedback(this);
        }

        /// <summary>Comma-separated icon names, top to bottom (e.g. "add,remove,pan_tool").</summary>
        [Category("Appearance")]
        public string IconList
        {
            get { return string.Join(",", icons); }
            set
            {
                icons = string.IsNullOrWhiteSpace(value) ? new string[0] : value.Split(',');
                Size = new Size(ItemSize + 2, icons.Length * ItemSize + Math.Max(0, icons.Length - 1) + 2);
                Invalidate();
            }
        }

        /// <summary>Tooltip texts separated by "|", in the same order as <see cref="IconList"/>.</summary>
        [Category("Appearance")]
        public string ToolTipList
        {
            get { return string.Join("|", tips); }
            set { tips = string.IsNullOrEmpty(value) ? new string[0] : value.Split('|'); }
        }

        [Category("Appearance")] public Color ActiveFill { get; set; }
        [Category("Appearance")] public Color ActiveIconColor { get; set; }

        [Category("Appearance")]
        public int ActiveIndex { get { return activeIndex; } set { activeIndex = value; Invalidate(); } }

        // Item height follows the real height of the control, so it fits at any screen scale (the design size is 36).
        private int ItemPx => icons.Length == 0 ? ItemSize : Math.Max(8, (Height - 2 - (icons.Length - 1)) / icons.Length);

        private int IndexAt(Point p) => p.Y < 1 ? -1 : Math.Min(icons.Length - 1, (p.Y - 1) / (ItemPx + 1));

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int index = IndexAt(e.Location);
            if (index != hoverIndex)
            {
                hoverIndex = index;
                tooltip.SetToolTip(this, index >= 0 && index < tips.Length ? tips[index] : "");
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                tooltip.Dispose();
                press.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnMouseLeave(EventArgs e) { hoverIndex = -1; press.Up(); Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            int index = IndexAt(e.Location);
            if (e.Button == MouseButtons.Left && index >= 0 && index < icons.Length) press.Down(index, e.Location);
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
            if (index >= 0 && index < icons.Length) ItemClicked?.Invoke(this, index);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var box = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            int itemPx = ItemPx;
            int iconPx = itemPx / 2;
            using (GraphicsPath path = Ui.RoundedRect(box, Ui.S(8)))
            {
                using (var fill = new SolidBrush(Color.White)) g.FillPath(fill, path);

                var state = g.Save();
                g.SetClip(path, CombineMode.Intersect);
                for (int i = 0; i < icons.Length; i++)
                {
                    int top = 1 + i * (itemPx + 1);
                    Color rest = i == activeIndex ? ActiveFill : (i == hoverIndex ? ThemeColors.Slate100 : Color.White);
                    Color back = press.Fill(i, rest, i == activeIndex ? ThemeColors.Sky200 : ThemeColors.Slate200);
                    using (var brush = new SolidBrush(back)) g.FillRectangle(brush, 1, top, Width - 2, itemPx);
                    var itemBox = new Rectangle(1, top, Width - 2, itemPx);
                    using (var itemPath = new GraphicsPath())
                    {
                        itemPath.AddRectangle(itemBox);
                        press.PaintRipple(g, i, itemPath, ThemeColors.Sky700, 70);
                    }
                    Color iconColor = i == activeIndex ? ActiveIconColor : ThemeColors.Slate700;
                    g.DrawImage(IconFont.Get(icons[i], iconPx, iconColor), (Width - iconPx) / 2, top + (itemPx - iconPx) / 2 + press.SinkFor(i), iconPx, iconPx);
                    if (i < icons.Length - 1)
                    {
                        using (var pen = new Pen(ThemeColors.Slate200)) g.DrawLine(pen, 1, top + itemPx, Width - 2, top + itemPx);
                    }
                }
                g.Restore(state);

                using (var pen = new Pen(ThemeColors.Slate200, 1f)) g.DrawPath(pen, path);
            }
        }
    }
}
