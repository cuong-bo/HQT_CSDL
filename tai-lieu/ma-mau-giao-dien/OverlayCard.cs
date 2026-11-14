using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Hue_travel.Theme
{
    /// <summary>
    /// Turns a control that sits ON the map (the search box, the chip bar, the tool rail, the status pill) into a floating card: its
    /// corners are rounded with a Region, and the host's <see cref="ChromeLayer"/> adds the soft shadow and a smooth 1 px outline over
    /// the Region's jagged edge. The control must be opaque (it cannot be translucent over the ActiveX map).
    /// </summary>
    public static class OverlayCard
    {
        /// <param name="radius">Design pixels; a value larger than half the shorter side makes a pill.</param>
        public static ChromeCard Attach(Form host, Control control, int radius, Elevation level = Elevation.Chip)
        {
            Action round = () =>
            {
                if (control.Width <= 0 || control.Height <= 0) return;
                using (GraphicsPath path = Ui.RoundedRect(new RectangleF(0, 0, control.Width, control.Height), Ui.S(radius)))
                {
                    control.Region = new Region(path);
                }
            };
            control.SizeChanged += (s, e) => round();
            round();

            ChromeLayer layer = ChromeLayer.For(host);
            var card = new ChromeCard
            {
                Radius = radius,
                Level = level,
                Ring = ThemeColors.Line,
                ScreenBounds = () => control.IsHandleCreated && control.Visible ? control.RectangleToScreen(control.ClientRectangle) : Rectangle.Empty,
            };
            layer.Add(card);
            control.LocationChanged += (s, e) => layer.RequestRender();
            control.SizeChanged += (s, e) => layer.RequestRender();
            control.VisibleChanged += (s, e) => layer.RequestRender();
            control.Disposed += (s, e) => layer.Remove(card);
            return card;
        }
    }
}
