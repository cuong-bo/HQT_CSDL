using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.TravelGuide
{
    /// <summary>
    /// A column of notes: a round icon, a bold title and wrapped text, with an optional "Sao chép" button on the right.
    /// Drawn at the screen's resolution; <see cref="HeightFor"/> says how tall it needs to be at a given width.
    /// </summary>
    internal sealed class InfoRows : Control
    {
        private const string CopyLabel = "Sao chép";
        private const TextFormatFlags WrapFlags = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
        private const TextFormatFlags MeasureFlags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
        private const TextFormatFlags LineFlags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;

        private readonly PressFeedback press;
        private IReadOnlyList<GuideNote> notes = new GuideNote[0];
        private int hoverAction = -1;

        /// <summary>Raised with the note whose copy button was pressed.</summary>
        public event EventHandler<GuideNote> CopyClicked;

        public InfoRows()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            press = new PressFeedback(this);
        }

        public void SetNotes(IReadOnlyList<GuideNote> value)
        {
            notes = value ?? new GuideNote[0];
            hoverAction = -1;
            Invalidate();
        }

        private int IconSize => Ui.S(36);
        private int TextLeft => Ui.S(14) + IconSize + Ui.S(12);

        private bool HasAction(int i) => !string.IsNullOrEmpty(notes[i].CopyText);

        private int ActionWidth() => TextRenderer.MeasureText(CopyLabel, ThemeFonts.Medium(12), Size.Empty, MeasureFlags).Width + Ui.S(20);

        private int TextWidth(int width, int i) => Math.Max(Ui.S(60), width - TextLeft - Ui.S(14) - (HasAction(i) ? ActionWidth() + Ui.S(10) : 0));

        private int RowHeight(int width, int i)
        {
            int text = TextRenderer.MeasureText(notes[i].Text ?? "", ThemeFonts.Regular(12), new Size(TextWidth(width, i), 0), WrapFlags).Height;
            return Math.Max(Ui.S(56), Ui.S(12) + Ui.S(22) + text + Ui.S(12));
        }

        /// <summary>The height needed at a given width.</summary>
        public int HeightFor(int width)
        {
            int sum = 0;
            for (int i = 0; i < notes.Count; i++) sum += RowHeight(width, i);
            return Math.Max(1, sum);
        }

        private Rectangle ActionRect(int top, int rowHeight)
        {
            int w = ActionWidth();
            int h = Ui.S(30);
            return new Rectangle(Width - Ui.S(14) - w, top + (rowHeight - h) / 2, w, h);
        }

        private int ActionAt(Point p)
        {
            int top = 0;
            for (int i = 0; i < notes.Count; i++)
            {
                int h = RowHeight(Width, i);
                if (HasAction(i) && ActionRect(top, h).Contains(p)) return i;
                top += h;
            }
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int index = ActionAt(e.Location);
            if (index != hoverAction)
            {
                hoverAction = index;
                Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hoverAction = -1;
            press.Up();
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            int index = ActionAt(e.Location);
            if (e.Button == MouseButtons.Left && index >= 0) press.Down(index, e.Location);
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            press.Up();
            base.OnMouseUp(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int index = ActionAt(e.Location);
            if (e.Button == MouseButtons.Left && index >= 0) CopyClicked?.Invoke(this, notes[index]);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) press.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            int top = 0;
            for (int i = 0; i < notes.Count; i++)
            {
                GuideNote note = notes[i];
                int h = RowHeight(Width, i);
                var iconRect = new Rectangle(Ui.S(14), top + Ui.S(12), IconSize, IconSize);
                using (var brush = new SolidBrush(ThemeColors.Sky100)) g.FillEllipse(brush, iconRect);
                int glyph = (int)(IconSize * 0.56);
                g.DrawImage(IconFont.Get(note.Icon, glyph, ThemeColors.Primary), iconRect.X + (IconSize - glyph) / 2, iconRect.Y + (IconSize - glyph) / 2, glyph, glyph);

                int textWidth = TextWidth(Width, i);
                TextRenderer.DrawText(g, note.Title ?? "", ThemeFonts.SemiBold(13), new Rectangle(TextLeft, top + Ui.S(12), textWidth, Ui.S(20)), ThemeColors.Slate800, LineFlags);
                TextRenderer.DrawText(g, note.Text ?? "", ThemeFonts.Regular(12), new Rectangle(TextLeft, top + Ui.S(34), textWidth, h - Ui.S(34)), ThemeColors.Slate600, WrapFlags);

                if (HasAction(i))
                {
                    Rectangle r = ActionRect(top, h);
                    int sink = press.SinkFor(i);
                    Color fill = press.Fill(i, hoverAction == i ? ThemeColors.Slate100 : Color.White, ThemeColors.Slate200);
                    using (GraphicsPath path = Ui.RoundedRect(new RectangleF(r.X + 0.5f, r.Y + 0.5f + sink, r.Width - 1f, r.Height - 1f), Ui.S(8)))
                    using (var brush = new SolidBrush(fill))
                    using (var pen = new Pen(ThemeColors.Slate300))
                    {
                        g.FillPath(brush, path);
                        g.DrawPath(pen, path);
                    }
                    TextRenderer.DrawText(g, CopyLabel, ThemeFonts.Medium(12), new Rectangle(r.X, r.Y + sink, r.Width, r.Height), ThemeColors.Slate700,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
                }

                if (i < notes.Count - 1)
                {
                    using (var pen = new Pen(ThemeColors.Slate100)) g.DrawLine(pen, TextLeft, top + h - 1, Width - Ui.S(14), top + h - 1);
                }
                top += h;
            }
        }
    }
}
