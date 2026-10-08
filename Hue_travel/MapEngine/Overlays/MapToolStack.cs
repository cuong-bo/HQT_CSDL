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

        public event EventHandler<int> ItemClicked;

        public MapToolStack()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            ActiveFill = ThemeColors.Sky100;
            ActiveIconColor = ThemeColors.Primary;
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

        [Category("Appearance")] public Color ActiveFill { get; set; }
        [Category("Appearance")] public Color ActiveIconColor { get; set; }

        [Category("Appearance")]
        public int ActiveIndex { get { return activeIndex; } set { activeIndex = value; Invalidate(); } }

        private int IndexAt(Point p) => p.Y < 1 ? -1 : Math.Min(icons.Length - 1, (p.Y - 1) / (ItemSize + 1));

        protected override void OnMouseMove(MouseEventArgs e) { hoverIndex = IndexAt(e.Location); Invalidate(); base.OnMouseMove(e); }
        protected override void OnMouseLeave(EventArgs e) { hoverIndex = -1; Invalidate(); base.OnMouseLeave(e); }

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
            using (GraphicsPath path = Ui.RoundedRect(box, 8))
            {
                using (var fill = new SolidBrush(Color.White)) g.FillPath(fill, path);

                var state = g.Save();
                g.SetClip(path, CombineMode.Intersect);
                for (int i = 0; i < icons.Length; i++)
                {
                    int top = 1 + i * (ItemSize + 1);
                    Color back = i == activeIndex ? ActiveFill : (i == hoverIndex ? ThemeColors.Slate100 : Color.White);
                    using (var brush = new SolidBrush(back)) g.FillRectangle(brush, 1, top, Width - 2, ItemSize);
                    Color iconColor = i == activeIndex ? ActiveIconColor : ThemeColors.Slate700;
                    g.DrawImage(IconFont.Get(icons[i], 18, iconColor), (Width - 18) / 2, top + (ItemSize - 18) / 2, 18, 18);
                    if (i < icons.Length - 1)
                    {
                        using (var pen = new Pen(ThemeColors.Slate200)) g.DrawLine(pen, 1, top + ItemSize, Width - 2, top + ItemSize);
                    }
                }
                g.Restore(state);

                using (var pen = new Pen(ThemeColors.Slate200, 1f)) g.DrawPath(pen, path);
            }
        }
    }
}
