using System;
using System.Linq;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Hue_travel.Theme
{
    /// <summary>
    /// The mockup's select box: h-8 slate-50 field, slate-200 border, 12px text and a chevron.
    /// Clicking opens a menu of <see cref="Items"/>; the choice becomes <see cref="Text"/>.
    /// </summary>
    public class SelectField : Control
    {
        private readonly ContextMenuStrip menu = new ContextMenuStrip();
        private bool isHovered;

        public event EventHandler SelectedChanged;

        public SelectField()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Font = ThemeFonts.Regular(12);
            ForeColor = ThemeColors.Slate800;
            Size = new Size(200, 32);
            Items = new System.Collections.Generic.List<string>();
            menu.Font = Font;
        }

        [Browsable(false)] public System.Collections.Generic.List<string> Items { get; private set; }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        public override string Text { get { return base.Text; } set { base.Text = value; Invalidate(); } }

        protected override void OnMouseEnter(EventArgs e) { isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { isHovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (Items.Count == 0) return;
            DisposeMenuItems();
            menu.Width = Width;
            foreach (string item in Items)
            {
                string captured = item;
                menu.Items.Add(captured, null, (s, a) =>
                {
                    Text = captured;
                    SelectedChanged?.Invoke(this, EventArgs.Empty);
                });
            }
            menu.Show(this, new Point(0, Height));
        }

        private void DisposeMenuItems()
        {
            foreach (ToolStripItem item in new System.Collections.Generic.List<ToolStripItem>(menu.Items.Cast<ToolStripItem>())) item.Dispose();
            menu.Items.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeMenuItems();
                menu.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (GraphicsPath path = Ui.RoundedRect(bounds, Ui.S(6)))
            using (var fill = new SolidBrush(isHovered ? Color.White : ThemeColors.Slate50))
            using (var pen = new Pen(isHovered ? ThemeColors.Sky200 : ThemeColors.Slate200, 1f))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }
            TextRenderer.DrawText(g, Text, Font, new Rectangle(Ui.S(10), 0, Width - Ui.S(40), Height), ForeColor,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            int chevron = Ui.S(18);
            g.DrawImage(IconFont.Get("expand_more", chevron, ThemeColors.Slate500), Width - Ui.S(26), (Height - chevron) / 2, chevron, chevron);
        }
    }
}
