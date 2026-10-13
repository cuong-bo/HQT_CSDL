using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Hue_travel.Theme
{
    /// <summary>
    /// An on / off switch like a web toggle: a pill that is blue with the knob on the right when on and grey with the knob on
    /// the left when off. Drawn at the screen's resolution. <see cref="Draw"/> is shared with owner-drawn lists.
    /// </summary>
    public sealed class ToggleSwitch : Control
    {
        public const int DesignWidth = 42;
        public const int DesignHeight = 24;

        private bool isOn;
        private bool isPartial;

        public event EventHandler CheckedChanged;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(DesignWidth, DesignHeight);
        }

        public bool Checked
        {
            get { return isOn; }
            set { if (isOn == value && !isPartial) return; isOn = value; isPartial = false; Invalidate(); CheckedChanged?.Invoke(this, EventArgs.Empty); }
        }

        /// <summary>Shows the "some are on" state (knob in the middle) without raising <see cref="CheckedChanged"/>.</summary>
        public void SetState(bool on, bool partial)
        {
            isOn = on;
            isPartial = partial;
            Invalidate();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Toggle();
        }

        /// <summary>Flips the switch; from the "some" state it switches everything on.</summary>
        public void Toggle()
        {
            Checked = isPartial ? true : !isOn;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Draw(e.Graphics, new Rectangle(0, 0, Width, Height), isOn, isPartial, Enabled);
        }

        /// <summary>Draws a switch inside <paramref name="bounds"/> (the pill fills the rectangle).</summary>
        public static void Draw(Graphics g, Rectangle bounds, bool on, bool partial, bool enabled)
        {
            SmoothingMode keep = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var pill = new RectangleF(bounds.X + 0.5f, bounds.Y + 0.5f, bounds.Width - 1f, bounds.Height - 1f);
            Color track = !enabled ? ThemeColors.Slate200 : (on || partial) ? ThemeColors.Primary : ThemeColors.Slate300;
            using (GraphicsPath path = Ui.RoundedRect(pill, pill.Height / 2f))
            using (var brush = new SolidBrush(track))
            {
                g.FillPath(brush, path);
            }
            float pad = Math.Max(2f, pill.Height * 0.12f);
            float knob = pill.Height - pad * 2f;
            float x = partial ? pill.X + (pill.Width - knob) / 2f : on ? pill.Right - pad - knob : pill.X + pad;
            var knobRect = new RectangleF(x, pill.Y + pad, knob, knob);
            using (var shadow = new SolidBrush(Color.FromArgb(40, 15, 23, 42))) g.FillEllipse(shadow, knobRect.X, knobRect.Y + 1f, knob, knob);
            g.FillEllipse(Brushes.White, knobRect);
            g.SmoothingMode = keep;
        }
    }
}
