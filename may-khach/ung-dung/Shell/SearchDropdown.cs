using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Hue_travel.MapEngine;
using Hue_travel.Models;
using Hue_travel.Panels.Shared;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    /// <summary>
    /// The results of the search box, shown right under it (no new window): a rounded white list of the best matches, a line
    /// saying how many more there are, or "no place found". It never takes the keyboard from the search box; the up / down
    /// keys and Enter are passed in by the host.
    /// </summary>
    public sealed class SearchDropdown : Control
    {
        private const int MaxRows = 7;
        private const int NoteHeight = 34;
        private const int EmptyHeight = 64;

        private readonly PoiRowList list = new PoiRowList { StealFocus = false, Dock = DockStyle.None };
        private string note = "";
        private bool isEmpty;

        /// <summary>Raised when a result is clicked or chosen with Enter.</summary>
        public event EventHandler<Poi> PoiChosen;

        public SearchDropdown()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            TabStop = false;
            Visible = false;
            list.RowClicked += (s, row) => PoiChosen?.Invoke(this, row.Poi);
            Controls.Add(list);
        }

        /// <summary>Shows up to seven matches under <paramref name="box"/> (the search box, in the host's coordinates).</summary>
        public void ShowResults(IReadOnlyList<Poi> matches, Rectangle box)
        {
            isEmpty = matches.Count == 0;
            var rows = matches.Take(MaxRows).Select(p => new PoiRow(p) { Subtitle = SubtitleOf(p) }).ToList();
            list.SetRows(rows);
            list.SelectedIndex = rows.Count > 0 ? 0 : -1;
            note = matches.Count > MaxRows ? "Còn " + (matches.Count - MaxRows) + " kết quả khác — gõ cụ thể hơn để thu hẹp" : "";

            int listHeight = isEmpty ? 0 : list.ContentHeight;
            int noteHeight = note.Length > 0 ? Ui.S(NoteHeight) : 0;
            int height = (isEmpty ? Ui.S(EmptyHeight) : listHeight) + noteHeight + Ui.S(2);
            SetBounds(box.X, box.Bottom + Ui.S(8), box.Width, height);
            list.SetBounds(Ui.S(1), Ui.S(1), Width - Ui.S(2), isEmpty ? 0 : listHeight);
            list.Visible = !isEmpty;
            UpdateRegion();
            Visible = true;
            BringToFront();
            Invalidate();
        }

        public void Dismiss()
        {
            Visible = false;
        }

        /// <summary>Moves the highlighted result up (-1) or down (+1).</summary>
        public void MoveSelection(int delta)
        {
            if (!Visible || list.RowCount == 0) return;
            list.SelectedIndex = Math.Max(0, Math.Min(list.RowCount - 1, list.SelectedIndex + delta));
        }

        /// <summary>Chooses the highlighted result; false when there is none.</summary>
        public bool ChooseSelected()
        {
            PoiRow row = list.RowAt(list.SelectedIndex);
            if (!Visible || row == null) return false;
            PoiChosen?.Invoke(this, row.Poi);
            return true;
        }

        private static string SubtitleOf(Poi poi)
        {
            string place = !string.IsNullOrEmpty(poi.Address) ? poi.Address : poi.TypeLabel;
            return string.IsNullOrEmpty(place) ? PoiMarkerIcons.NameOf(poi.Category) : PoiMarkerIcons.NameOf(poi.Category) + " · " + place;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(0, 0, Width, Height), Ui.S(14)))
            {
                Region = new Region(path);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);
            if (isEmpty)
            {
                TextRenderer.DrawText(g, "Không tìm thấy địa điểm nào khớp với từ khoá", ThemeFonts.Regular(13), new Rectangle(Ui.S(16), 0, Width - Ui.S(32), Ui.S(EmptyHeight)),
                    ThemeColors.Slate500, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            }
            if (note.Length > 0)
            {
                var strip = new Rectangle(0, Height - Ui.S(NoteHeight) - Ui.S(1), Width, Ui.S(NoteHeight));
                using (var brush = new SolidBrush(ThemeColors.Slate50)) g.FillRectangle(brush, strip);
                using (var pen = new Pen(ThemeColors.Slate200)) g.DrawLine(pen, 0, strip.Y, Width, strip.Y);
                TextRenderer.DrawText(g, note, ThemeFonts.Regular(12), strip, ThemeColors.Slate500,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            }
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), Ui.S(14)))
            using (var pen = new Pen(ThemeColors.Slate300, 1.2f))
            {
                g.DrawPath(pen, path);
            }
        }
    }
}
