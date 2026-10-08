using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Hue_travel.Theme
{
    /// <summary>
    /// Base for the floating panels (LayerControl, RouteOptimizer, ...): a borderless white
    /// Form with a 1px slate-200 border and rounded corners (rounded-lg), embedded in
    /// MainForm as a child control. Shadows cannot composite over the MapWinGIS ActiveX
    /// window, so the border stands in for the mockup's drop shadow.
    /// </summary>
    public class FloatingPanelForm : Form
    {
        private const int CornerRadius = 8;

        public event EventHandler CloseRequested;

        public FloatingPanelForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            BackColor = ThemeColors.SurfaceContainerLowest;
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        private int expandedHeight;
        private bool isCollapsed;

        /// <summary>Collapses the panel to its header strip, or restores it. Bottom-anchored panels keep their bottom edge.</summary>
        public void ToggleCollapsed(int collapsedHeight)
        {
            bool keepBottom = (Anchor & AnchorStyles.Bottom) != 0 && (Anchor & AnchorStyles.Top) == 0;
            int bottom = Bottom;
            if (!isCollapsed)
            {
                expandedHeight = Height;
                Height = collapsedHeight;
            }
            else
            {
                Height = expandedHeight;
            }
            isCollapsed = !isCollapsed;

            // Only the header strip stays; docked body/footer would otherwise overlap it.
            foreach (Control child in Controls)
            {
                if (!(child is PanelHeader)) child.Visible = !isCollapsed;
            }
            if (keepBottom) Top = bottom - Height;
        }

        /// <summary>Raised when the panel wants the host window to show a toast message.</summary>
        public event EventHandler<ToastMessage> MessageRaised;

        protected void Notify(string text, ToastKind kind = ToastKind.Info) => MessageRaised?.Invoke(this, new ToastMessage(text, kind));

        protected void RaiseCloseRequested() => CloseRequested?.Invoke(this, EventArgs.Empty);

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width <= 0 || Height <= 0) return;
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(0, 0, Width, Height), CornerRadius))
            {
                Region = new Region(path);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), CornerRadius))
            using (var pen = new Pen(ThemeColors.Slate200, 1f))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }
    }
}
