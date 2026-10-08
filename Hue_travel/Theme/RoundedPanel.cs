using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Hue_travel.Theme
{
    /// <summary>
    /// A container with a rounded fill (optionally a left-to-right gradient) and a 1px
    /// border: the "rounded-md border bg-sky-50" boxes of the mockup.
    /// </summary>
    public class RoundedPanel : Panel
    {
        public RoundedPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            FillColor = Color.White;
            Radius = 6;
        }

        [Category("Appearance")] public Color FillColor { get; set; }
        /// <summary>When set, the fill is a horizontal gradient from FillColor to this colour.</summary>
        [Category("Appearance")] public Color FillColor2 { get; set; }
        [Category("Appearance")] public Color BorderColor { get; set; }
        [Category("Appearance")] public int Radius { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (GraphicsPath path = Ui.RoundedRect(bounds, Radius))
            {
                if (!FillColor2.IsEmpty && Width > 0 && Height > 0)
                {
                    using (var brush = new LinearGradientBrush(new Rectangle(0, 0, Width, Height), FillColor, FillColor2, LinearGradientMode.Horizontal))
                    {
                        g.FillPath(brush, path);
                    }
                }
                else if (FillColor.A > 0)
                {
                    using (var brush = new SolidBrush(FillColor)) g.FillPath(brush, path);
                }

                if (!BorderColor.IsEmpty && BorderColor.A > 0)
                {
                    using (var pen = new Pen(BorderColor, 1f)) g.DrawPath(pen, path);
                }
            }
        }
    }
}
