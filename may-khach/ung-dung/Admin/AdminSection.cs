using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Account;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Admin
{
    /// <summary>Helpers that fill a <see cref="VStack"/> the way the account screens do (captions, hints, fields, lists, buttons).</summary>
    internal static class StackExtensions
    {
        public static Label Caption(this VStack stack, string text, int gap = 4)
        {
            Label label = AccountKit.Caption(text);
            stack.Add(label, 26, gap);
            return label;
        }

        public static Label Hint(this VStack stack, string text, int gap = 10)
        {
            Label label = AccountKit.Hint(text);
            stack.Add(label, width => Measure(label, width), gap);
            return label;
        }

        public static int Measure(Label label, int width) =>
            string.IsNullOrEmpty(label.Text) ? 0 : TextRenderer.MeasureText(label.Text, label.Font, new Size(width, 0), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Height + Ui.S(4);

        public static IconButton Button(this VStack stack, IconButton button, int height = 44, int gap = 10)
        {
            stack.Add(button, height, gap);
            return button;
        }

        public static ItemList List(this VStack stack, ItemList list, int gap = 10)
        {
            stack.Add(list, width => list.WantedHeight, gap, false);
            return list;
        }

        public static InputField Field(this VStack stack, string label, InputField field, int height = 40, int gap = 12)
        {
            stack.Add(AccountKit.FieldLabel(label), 24, 2);
            stack.Add(field, height, gap);
            return field;
        }

        public static void Divider(this VStack stack, int gap = 12) => stack.Add(AccountKit.Divider(), 1, gap);

        /// <summary>Several buttons side by side in one row, sharing the width equally.</summary>
        public static void Buttons(this VStack stack, params IconButton[] buttons)
        {
            var row = new Panel { BackColor = Color.White };
            foreach (IconButton b in buttons) row.Controls.Add(b);
            row.Resize += (s, e) =>
            {
                int gap = Ui.S(10), width = (row.Width - gap * (buttons.Length - 1)) / buttons.Length;
                for (int i = 0; i < buttons.Length; i++) buttons[i].SetBounds(i * (width + gap), 0, width, row.Height);
            };
            stack.Add(row, 44, 10);
        }
    }

    /// <summary>
    /// One page of the admin console: a list on the left (search, filters, rows) and what is chosen on the right (details and the
    /// actions on it). A page loads its list in <see cref="LoadAsync"/>, shows the chosen row on the right, and tells the console
    /// when it changed something so that the counts on the menu are refreshed.
    /// </summary>
    internal abstract class AdminSection : UserControl
    {
        private const int ListWidth = 440;
        protected readonly ApiClient Api;
        protected readonly VStack ListPane = new VStack { Dock = DockStyle.Left };
        protected readonly VStack DetailPane = new VStack();

        protected AdminSection(ApiClient api)
        {
            Api = api;
            Dock = DockStyle.Fill;
            BackColor = Color.White;
            ListPane.Width = Ui.S(ListWidth);
            var line = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = ThemeColors.Slate200 };
            Controls.Add(DetailPane);
            Controls.Add(line);
            Controls.Add(ListPane);
        }

        public event EventHandler<ToastMessage> MessageRaised;

        /// <summary>Raised after the page changed data on the server (a decision, a deletion).</summary>
        public event EventHandler Changed;

        /// <summary>Raised when the page changed the places (the map's place list must be fetched again).</summary>
        public event EventHandler PlacesChanged;

        /// <summary>Loads (or reloads) the list on the left.</summary>
        public abstract Task LoadAsync();

        protected void Notify(string text, ToastKind kind = ToastKind.Info) => MessageRaised?.Invoke(this, new ToastMessage(text, kind));

        protected void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

        protected void RaisePlacesChanged()
        {
            PlacesChanged?.Invoke(this, EventArgs.Empty);
            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>True when the call worked; otherwise shows the server's message.</summary>
        protected bool Check(ApiResponse response, string successText = null)
        {
            if (!response.IsSuccess)
            {
                Notify(response.Error, ToastKind.Error);
                return false;
            }
            if (successText != null) Notify(successText, ToastKind.Success);
            return true;
        }

        protected static List<IDictionary<string, object>> Rows(ApiResponse r) =>
            (JsonHelper.AsList(r.Json) ?? new List<object>()).OfType<IDictionary<string, object>>().ToList();

        protected static string Str(IDictionary<string, object> d, string key) => JsonHelper.Str(d, key) ?? "";

        /// <summary>Clears the right side and shows a line asking to choose a row.</summary>
        protected void ShowNothing(string text)
        {
            DetailPane.Clear();
            DetailPane.Hint(text);
            DetailPane.Relayout();
        }

        protected Form Window => FindForm();
    }
}
