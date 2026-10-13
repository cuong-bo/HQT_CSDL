using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Hue_travel.MapEngine;
using Hue_travel.Models;
using Hue_travel.Panels.SearchPoi;
using Hue_travel.Panels.Shared;
using Hue_travel.Services.Data;
using Hue_travel.Theme;

namespace Hue_travel.Account
{
    internal sealed class DialogField
    {
        public DialogField(string label, string initial = "", int lines = 1, bool password = false, string hint = "")
        {
            Label = label;
            Initial = initial ?? "";
            Lines = lines;
            Password = password;
            Hint = hint ?? "";
        }

        public string Label { get; }
        public string Initial { get; }
        public int Lines { get; }
        public bool Password { get; }

        /// <summary>Grey text inside the box while it is empty.</summary>
        public string Hint { get; }
    }

    /// <summary>
    /// A small window over the main one in the project's own style: a white rounded card with a title, the content, and two buttons
    /// (cancel, OK). Enter confirms and Esc cancels. The content is added by the dialogs below.
    /// </summary>
    internal class ModalCard : Form
    {
        protected const int Side = 20;
        private readonly Label title;
        protected readonly Panel Content = new Panel { BackColor = Color.White };
        protected readonly IconButton Ok;
        protected readonly IconButton Cancel;
        private readonly int designWidth;

        public ModalCard(string titleText, int designWidth, string okText, bool danger = false, bool showCancel = true)
        {
            this.designWidth = designWidth;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            KeyPreview = true;
            BackColor = Color.White;
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;

            title = new Label { Text = titleText, AutoSize = false, Font = ThemeFonts.Bold(16), ForeColor = ThemeColors.Slate800, TextAlign = ContentAlignment.MiddleLeft };
            Ok = AccountKit.Primary(okText);
            if (danger) { Ok.FillColor = ThemeColors.Red500; Ok.HoverFillColor = Color.FromArgb(185, 28, 28); }
            Cancel = AccountKit.Secondary("Huỷ");
            Cancel.Visible = showCancel;
            Ok.Click += (s, e) => { if (CanAccept()) DialogResult = DialogResult.OK; };
            Cancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.Add(title);
            Controls.Add(Content);
            Controls.Add(Ok);
            Controls.Add(Cancel);
        }

        /// <summary>Checked when OK is pressed; a dialog that finds a problem shows it and returns false.</summary>
        protected virtual bool CanAccept() => true;

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape) { DialogResult = DialogResult.Cancel; return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>Sizes the window for content of the given design height and puts the buttons under it.</summary>
        protected void Arrange(int contentDesignHeight)
        {
            int width = Ui.S(designWidth), side = Ui.S(Side);
            int y = Ui.S(18);
            title.SetBounds(side, y, width - side * 2, Ui.S(30));
            y += Ui.S(30) + Ui.S(8);
            Content.SetBounds(side, y, width - side * 2, Ui.S(contentDesignHeight));
            y = Content.Bottom + Ui.S(16);
            int buttonHeight = Ui.S(44), buttonWidth = Ui.S(Cancel.Visible ? 124 : 160);
            Ok.SetBounds(width - side - buttonWidth, y, buttonWidth, buttonHeight);
            Cancel.SetBounds(Ok.Left - Ui.S(10) - buttonWidth, y, buttonWidth, buttonHeight);
            ClientSize = new Size(width, y + buttonHeight + Ui.S(18));
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(0, 0, Width, Height), Ui.S(14)))
                Region = new Region(path);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), Ui.S(14)))
            using (var pen = new Pen(ThemeColors.Slate300)) e.Graphics.DrawPath(pen, path);
        }
    }

    /// <summary>The questions the account screens ask: a few text boxes, a yes or no, a choice from a list, a set of places.</summary>
    internal static class Dialogs
    {
        /// <summary>Returns the entered values in order, or null when cancelled.</summary>
        public static string[] Ask(IWin32Window owner, string title, params DialogField[] fields) => Ask(owner, title, null, "Đồng ý", fields);

        public static string[] Ask(IWin32Window owner, string title, string message, string okText, params DialogField[] fields)
        {
            using (var card = new AskCard(title, message, okText, fields)) return card.ShowDialog(owner) == DialogResult.OK ? card.Values : null;
        }

        /// <summary>A question with only OK and Huỷ; the OK button is red when the action cannot be undone.</summary>
        public static bool Confirm(IWin32Window owner, string title, string message, string okText = "Đồng ý", bool danger = false)
        {
            using (var card = new MessageCard(title, message, okText, danger, true)) return card.ShowDialog(owner) == DialogResult.OK;
        }

        public static void Message(IWin32Window owner, string title, string message)
        {
            using (var card = new MessageCard(title, message, "Đóng", false, false)) card.ShowDialog(owner);
        }

        /// <summary>Shows rows and returns the index of the chosen one, or -1.</summary>
        public static int Pick(IWin32Window owner, string title, IList<ListItem> rows, string okText = "Chọn")
        {
            using (var card = new PickCard(title, rows, okText)) return card.ShowDialog(owner) == DialogResult.OK ? card.Chosen : -1;
        }

        /// <summary>Lets the person tick up to <paramref name="max"/> places; returns them, or null when cancelled.</summary>
        public static IReadOnlyList<Poi> PickPlaces(IWin32Window owner, string title, int max, IEnumerable<string> chosenIds, string okText = "Xong")
        {
            using (var card = new PlacesCard(title, max, chosenIds, okText)) return card.ShowDialog(owner) == DialogResult.OK ? card.Chosen : null;
        }

        // ------------------------------------------------------------------ the cards

        private sealed class AskCard : ModalCard
        {
            private readonly List<InputField> boxes = new List<InputField>();

            public AskCard(string title, string message, string okText, DialogField[] fields) : base(title, 460, okText)
            {
                int inner = 460 - Side * 2;
                int y = 0;
                if (!string.IsNullOrEmpty(message))
                {
                    int lines = Math.Max(1, (int)Math.Ceiling(message.Length / 56.0));
                    var hint = AccountKit.Hint(message);
                    hint.SetBounds(0, 0, Ui.S(inner), Ui.S(lines * 17 + 4));
                    Content.Controls.Add(hint);
                    y = lines * 17 + 10;
                }
                foreach (DialogField field in fields)
                {
                    var label = AccountKit.FieldLabel(field.Label);
                    label.SetBounds(0, Ui.S(y), Ui.S(inner), Ui.S(22));
                    y += 24;
                    int height = field.Lines > 1 ? 24 * field.Lines + 16 : 40;
                    var box = new InputField { IsPassword = field.Password, PlaceholderText = field.Hint, Text = field.Initial };
                    if (field.Lines > 1) box.IsMultiline = true;
                    if (field.Password) box.Revealable = true;
                    box.SetBounds(0, Ui.S(y), Ui.S(inner), Ui.S(height));
                    y += height + 12;
                    Content.Controls.Add(label);
                    Content.Controls.Add(box);
                    boxes.Add(box);
                }
                Arrange(y);
                Shown += (s, e) => { if (boxes.Count > 0) boxes[0].FocusText(); };
            }

            public string[] Values => boxes.Select(b => b.Text).ToArray();
        }

        private sealed class MessageCard : ModalCard
        {
            public MessageCard(string title, string message, string okText, bool danger, bool showCancel) : base(title, 460, okText, danger, showCancel)
            {
                var text = new Label { AutoSize = false, Text = message, Font = ThemeFonts.Regular(13), ForeColor = ThemeColors.Slate700, TextAlign = ContentAlignment.TopLeft };
                int lines = message.Split('\n').Sum(line => Math.Max(1, (int)Math.Ceiling(line.Length / 52.0)));
                int height = Math.Max(24, lines * 20 + 6);
                text.SetBounds(0, 0, Ui.S(460 - Side * 2), Ui.S(height));
                Content.Controls.Add(text);
                Arrange(height);
            }
        }

        private sealed class PickCard : ModalCard
        {
            private readonly ItemList list = new ItemList { Selectable = true, SubtitleLines = 1 };

            public PickCard(string title, IList<ListItem> rows, string okText) : base(title, 460, okText)
            {
                list.SetItems(rows);
                if (rows.Count > 0) list.SelectedIndex = 0;
                list.DoubleClick += (s, e) => { if (list.SelectedIndex >= 0) DialogResult = DialogResult.OK; };
                int visible = Math.Min(Math.Max(rows.Count, 1), 6) * 62 + 2;
                var scroll = new NoAutoScrollPanel { AutoScroll = true, BackColor = Color.White };
                scroll.SetBounds(0, 0, Ui.S(460 - Side * 2), Ui.S(visible));
                list.SetBounds(0, 0, scroll.ClientSize.Width - (rows.Count > 6 ? SystemInformation.VerticalScrollBarWidth : 0), list.WantedHeight);
                scroll.Controls.Add(list);
                scroll.AutoScrollMinSize = new Size(0, list.WantedHeight);
                Content.Controls.Add(scroll);
                Arrange(visible);
            }

            public int Chosen => list.SelectedIndex;

            protected override bool CanAccept() => list.SelectedIndex >= 0;
        }

        private sealed class PlacesCard : ModalCard
        {
            private readonly int max;
            private readonly InputField search = new InputField { IconName = "search", PlaceholderText = "Tìm địa điểm theo tên…" };
            private readonly PoiRowList list = new PoiRowList { ShowAddButton = true, StealFocus = false, EmptyText = "Không có địa điểm nào khớp." };
            private readonly Label count = AccountKit.Hint("");
            private readonly List<Poi> chosen = new List<Poi>();

            public PlacesCard(string title, int max, IEnumerable<string> chosenIds, string okText) : base(title, 480, okText)
            {
                this.max = max;
                var ids = new HashSet<string>(chosenIds ?? new string[0]);
                chosen.AddRange(PoiCatalog.All.Where(p => ids.Contains(p.Id)));
                int inner = 480 - Side * 2;
                search.SetBounds(0, 0, Ui.S(inner), Ui.S(40));
                count.SetBounds(0, Ui.S(46), Ui.S(inner), Ui.S(34));
                list.SetBounds(0, Ui.S(84), Ui.S(inner), Ui.S(5 * 60));
                Content.Controls.Add(search);
                Content.Controls.Add(count);
                Content.Controls.Add(list);
                search.TextChanged += (s, e) => Reload();
                list.AddClicked += (s, row) => Toggle(row);
                list.RowClicked += (s, row) => Toggle(row);
                Arrange(84 + 5 * 60 + 2);
                Reload();
                Shown += (s, e) => search.FocusText();
            }

            public IReadOnlyList<Poi> Chosen => chosen;

            private void Reload()
            {
                IEnumerable<Poi> source = PoiCatalog.All.Where(p => p.X != 0 || p.Y != 0);
                IReadOnlyList<Poi> found = PoiSearchFilter.Apply(source, search.Text, null);
                list.SetRows(found.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).Take(300)
                    .Select(p => new PoiRow(p) { Subtitle = PoiMarkerIcons.Describe(p), Added = chosen.Any(c => c.Id == p.Id) }).ToList());
                Counter();
            }

            private void Toggle(PoiRow row)
            {
                Poi existing = chosen.FirstOrDefault(p => p.Id == row.Poi.Id);
                if (existing != null) chosen.Remove(existing);
                else if (chosen.Count >= max) { count.Text = "Chọn tối đa " + max + " địa điểm."; return; }
                else chosen.Add(row.Poi);
                row.Added = chosen.Any(p => p.Id == row.Poi.Id);
                list.Repaint();
                Counter();
            }

            private void Counter() => count.Text = "Đã chọn " + chosen.Count + " / " + max + (chosen.Count > 0 ? ": " + string.Join(", ", chosen.Select(p => p.Name)) : "");

            protected override bool CanAccept() => chosen.Count > 0;
        }
    }
}
