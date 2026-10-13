using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Hue_travel.Theme
{
    /// <summary>
    /// The "it was pressed" look for owner-drawn controls that hold several clickable items (navigation rail, map tool
    /// stack, filter chips): the pressed item darkens and sinks one pixel, and a ripple spreads from the click point.
    /// The pressed look stays on screen for at least <see cref="MinPressedMs"/> so a quick tap is still seen.
    /// </summary>
    public sealed class PressFeedback : IDisposable
    {
        public const int MinPressedMs = 110;
        private const float InStep = 0.5f;
        private const float OutStep = 0.12f;
        private const float RippleStep = 0.055f;

        private readonly Control owner;
        private readonly Timer timer = new Timer { Interval = 15 };
        private bool isDown;
        private int downAtTick;
        private float amount;
        private float ripple = -1f;
        private Point rippleOrigin;

        public PressFeedback(Control owner)
        {
            this.owner = owner;
            timer.Tick += (s, e) => Step();
        }

        /// <summary>Index of the item that is (or was just) pressed, -1 when nothing is.</summary>
        public int Index { get; private set; } = -1;

        /// <summary>How pressed the item is, 0 (rest) to 1 (fully pressed).</summary>
        public float AmountFor(int index) => index == Index ? amount : 0f;

        /// <summary>One pixel (scaled) while pressed, so the content visibly sinks.</summary>
        public int SinkFor(int index) => (int)Math.Round(Ui.S(1) * AmountFor(index));

        public void Down(int index, Point location)
        {
            Index = index;
            isDown = true;
            downAtTick = Environment.TickCount;
            rippleOrigin = location;
            ripple = 0f;
            timer.Start();
            owner.Invalidate();
        }

        public void Up()
        {
            isDown = false;
            timer.Start();
        }

        private void Step()
        {
            bool hold = isDown || unchecked(Environment.TickCount - downAtTick) < MinPressedMs;
            amount = hold ? Math.Min(1f, amount + InStep) : Math.Max(0f, amount - OutStep);
            if (ripple >= 0f)
            {
                ripple += RippleStep;
                if (ripple >= 1f) ripple = -1f;
            }
            if (!hold && amount <= 0f && ripple < 0f)
            {
                Index = -1;
                timer.Stop();
            }
            owner.Invalidate();
        }

        /// <summary>Darkens a fill by the press amount of <paramref name="index"/> (a transparent fill gets a visible tint).</summary>
        public Color Fill(int index, Color rest, Color pressedTint)
        {
            float t = AmountFor(index);
            if (t <= 0f) return rest;
            Color from = rest.A == 0 ? Color.FromArgb(0, pressedTint) : rest;
            return Ui.Lerp(from, pressedTint, t);
        }

        /// <summary>Draws the ripple of <paramref name="index"/> inside <paramref name="clip"/> (the item's shape, in control coordinates).</summary>
        public void PaintRipple(Graphics g, int index, GraphicsPath clip, Color tint, int peakAlpha)
        {
            if (index != Index || ripple < 0f) return;
            float ease = 1f - (1f - ripple) * (1f - ripple);
            float radius = Math.Max(owner.Width, owner.Height) * ease;
            int alpha = Math.Max(0, (int)(peakAlpha * (1f - ripple)));
            GraphicsState state = g.Save();
            g.SetClip(clip, CombineMode.Intersect);
            using (var brush = new SolidBrush(Color.FromArgb(alpha, tint)))
            {
                g.FillEllipse(brush, rippleOrigin.X - radius, rippleOrigin.Y - radius, radius * 2, radius * 2);
            }
            g.Restore(state);
        }

        public void Dispose() => timer.Dispose();
    }
}
