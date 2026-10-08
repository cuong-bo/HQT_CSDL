using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Hue_travel.Theme
{
    public enum ToastKind
    {
        Info,
        Success,
        Error
    }

    /// <summary>A short message a panel wants the host window to show as a toast.</summary>
    public sealed class ToastMessage : EventArgs
    {
        public ToastMessage(string text, ToastKind kind)
        {
            Text = text;
            Kind = kind;
        }

        public string Text { get; }
        public ToastKind Kind { get; }
    }

    /// <summary>
    /// A small dark pill centred near the top of its parent that disappears after a moment
    /// ("Đã sao chép số hotline", validation errors, ...). Opaque so it stays above the map.
    /// </summary>
    public class Toast : Control
    {
        private const int PillHeight = 36;
        private const int VisibleMilliseconds = 3000;

        private readonly Timer hideTimer = new Timer { Interval = VisibleMilliseconds };
        private ToastKind kind;

        public Toast()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = ThemeFonts.SemiBold(12);
            Visible = false;
            Height = PillHeight;
            TopOffset = 104;
            hideTimer.Tick += (s, e) => { hideTimer.Stop(); Visible = false; };
        }

        /// <summary>Distance from the top of the parent (below the ribbon).</summary>
        public int TopOffset { get; set; }

        public void ShowMessage(string text, ToastKind messageKind)
        {
            kind = messageKind;
            Text = text;
            int textWidth = TextRenderer.MeasureText(text, Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
            Width = textWidth + 52;
            if (Parent != null) Left = (Parent.ClientSize.Width - Width) / 2;
            Top = TopOffset;
            Visible = true;
            BringToFront();
            hideTimer.Stop();
            hideTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) hideTimer.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(0, 0, Width, Height), Height / 2f))
            using (var fill = new SolidBrush(ThemeColors.Slate800))
            {
                g.FillPath(fill, path);
            }

            Color dot = kind == ToastKind.Success ? ThemeColors.Emerald500 : kind == ToastKind.Error ? ThemeColors.Red500 : ThemeColors.Sky400;
            using (var brush = new SolidBrush(dot)) g.FillEllipse(brush, 16, (Height - 9) / 2f, 9, 9);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(34, 0, Width - 44, Height), Color.White,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine);
        }
    }
}
