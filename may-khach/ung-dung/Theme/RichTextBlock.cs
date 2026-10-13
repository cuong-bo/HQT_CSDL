using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Hue_travel.Theme
{
    /// <summary>
    /// Word-wrapped text with inline emphasis. Text between ** markers is drawn bold (and in
    /// <see cref="BoldColor"/> when set), e.g. "**Chặng 1:** Ga Huế → Đại Nội". Height can be
    /// fitted to the width with <see cref="FitHeight"/>.
    /// </summary>
    public class RichTextBlock : Control
    {
        private const TextFormatFlags Flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.Left | TextFormatFlags.Top;

        private struct Run
        {
            public string Text;
            public bool IsBold;
        }

        private float lineSpacing = 1.5f;

        public RichTextBlock()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = ThemeFonts.Regular(12);
            BoldFont = ThemeFonts.Bold(12);
            Size = new Size(200, 40);
        }

        [Category("Appearance")] public Font BoldFont { get; set; }
        /// <summary>Colour for ** emphasised runs; Color.Empty keeps ForeColor.</summary>
        [Category("Appearance")] public Color BoldColor { get; set; }
        /// <summary>Line height as a multiple of the font height (Tailwind leading-relaxed is about 1.6).</summary>
        [Category("Appearance")] public float LineSpacing { get { return lineSpacing; } set { lineSpacing = value; Invalidate(); } }

        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        public override string Text { get { return base.Text; } set { base.Text = value; Invalidate(); } }

        /// <summary>Sets the height needed to show all text at the current width.</summary>
        public void FitHeight()
        {
            Height = LayoutText(null, Width);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            LayoutText(e.Graphics, Width);
        }

        private static List<Run> Parse(string text)
        {
            var runs = new List<Run>();
            bool bold = false;
            foreach (string part in (text ?? "").Split(new[] { "**" }, StringSplitOptions.None))
            {
                if (part.Length > 0) runs.Add(new Run { Text = part, IsBold = bold });
                bold = !bold;
            }
            return runs;
        }

        private static void DrawSegment(Graphics g, string text, Font font, Color color, int x, int y, int lineHeight)
        {
            if (g == null || text.Length == 0) return;
            TextRenderer.DrawText(g, text, font, new Point(x, y + (int)((lineHeight - font.GetHeight()) / 2)), color, Flags);
        }

        // Greedy word wrap; draws when g is not null. Returns the total height.
        private int LayoutText(Graphics g, int width)
        {
            Font bold = BoldFont ?? Font;
            int lineHeight = (int)Math.Ceiling(Font.GetHeight() * lineSpacing);
            int x = 0, y = 0;

            foreach (Run run in Parse(Text))
            {
                Font font = run.IsBold ? bold : Font;
                Color color = run.IsBold && !BoldColor.IsEmpty ? BoldColor : ForeColor;
                int space = TextRenderer.MeasureText("a a", font, Size.Empty, Flags).Width - TextRenderer.MeasureText("aa", font, Size.Empty, Flags).Width;
                if (run.Text.StartsWith(" ")) x += space;

                // Measure whole segments (not single words) so GDI's own spacing is kept; draw one segment per line.
                string segment = "";
                foreach (string word in run.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string candidate = segment.Length == 0 ? word : segment + " " + word;
                    int candidateWidth = TextRenderer.MeasureText(candidate, font, Size.Empty, Flags).Width;
                    if (x + candidateWidth > width && (x > 0 || segment.Length > 0))
                    {
                        DrawSegment(g, segment, font, color, x, y, lineHeight);
                        segment = word;
                        x = 0;
                        y += lineHeight;
                    }
                    else
                    {
                        segment = candidate;
                    }
                }
                DrawSegment(g, segment, font, color, x, y, lineHeight);
                if (segment.Length > 0) x += TextRenderer.MeasureText(segment, font, Size.Empty, Flags).Width;
                if (run.Text.EndsWith(" ")) x += space;
            }
            return y + lineHeight;
        }
    }
}
