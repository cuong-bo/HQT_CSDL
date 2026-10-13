using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.MapEngine
{
    /// <summary>
    /// A bold ring drawn around the place the map was zoomed to. It sits above the map as a control with a
    /// round shape and lets mouse clicks through to the map and its markers.
    /// </summary>
    internal sealed class HighlightRing : Control
    {
        private const int WmNcHitTest = 0x0084;
        private const int HtTransparent = -1;

        public HighlightRing(int diameter)
        {
            Size = new Size(diameter, diameter);
            BackColor = ThemeColors.Primary;
            TabStop = false;
            Visible = false;
        }

        // The form scales its controls once it shows, so the round shape is cut again for whatever size the ring ends up with.
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            int diameter = Math.Min(Width, Height);
            if (diameter <= 0) return;
            int thickness = Math.Max(3, (int)Math.Round(diameter * 0.08));
            using (var shape = new GraphicsPath())
            {
                shape.AddEllipse(0, 0, diameter, diameter);
                shape.AddEllipse(thickness, thickness, diameter - thickness * 2, diameter - thickness * 2);   // alternate fill: a ring, the marker shows through the middle
                Region = new Region(shape);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmNcHitTest) { m.Result = (IntPtr)HtTransparent; return; }
            base.WndProc(ref m);
        }
    }
}
