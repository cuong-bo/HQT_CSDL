using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Account
{
    /// <summary>
    /// Base of the screens inside the account panel: has the API, reports messages and loads its data. A screen is a
    /// <see cref="VStack"/> (a scrolling column) filled with captions, hints, fields, lists and buttons through the Add helpers.
    /// </summary>
    internal abstract class AccountTab : UserControl
    {
        protected AccountTab(ApiClient api)
        {
            Api = api;
            Dock = DockStyle.Fill;
            BackColor = Color.White;
            Stack = new VStack();
            Controls.Add(Stack);
        }

        protected ApiClient Api { get; }

        protected VStack Stack { get; }

        public event EventHandler<ToastMessage> MessageRaised;

        public abstract Task LoadAsync();

        protected void Notify(string text, ToastKind kind = ToastKind.Info) => MessageRaised?.Invoke(this, new ToastMessage(text, kind));

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

        // ------------------------------------------------------------------ building a screen

        protected Label AddCaption(string text, int gap = 4)
        {
            Label label = AccountKit.Caption(text);
            Stack.Add(label, 26, gap);
            return label;
        }

        /// <summary>A grey explanation; its height follows the text and the width of the screen.</summary>
        protected Label AddHint(string text, int gap = 10)
        {
            Label label = AccountKit.Hint(text);
            Stack.Add(label, width => HintHeight(label, width), gap);
            return label;
        }

        protected static int HintHeight(Label label, int width) =>
            string.IsNullOrEmpty(label.Text) ? 0 : TextRenderer.MeasureText(label.Text, label.Font, new Size(width, 0), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Height + Ui.S(4);

        protected IconButton AddButton(IconButton button, int height = 44, int gap = 10)
        {
            Stack.Add(button, height, gap);
            return button;
        }

        protected ItemList AddList(ItemList list, int gap = 10)
        {
            Stack.Add(list, width => list.WantedHeight, gap, false);
            return list;
        }

        protected InputField AddField(string label, InputField field, int height = 40, int gap = 12)
        {
            Stack.Add(AccountKit.FieldLabel(label), 24, 2);
            Stack.Add(field, height, gap);
            return field;
        }

        protected void AddDivider(int gap = 12) => Stack.Add(AccountKit.Divider(), 1, gap);

        /// <summary>Two buttons side by side in one row.</summary>
        protected void AddButtonPair(IconButton left, IconButton right, int height = 44, int gap = 10)
        {
            var row = new Panel { BackColor = Color.White };
            row.Controls.Add(left);
            row.Controls.Add(right);
            row.Resize += (s, e) =>
            {
                int half = (row.Width - Ui.S(10)) / 2;
                left.SetBounds(0, 0, half, row.Height);
                right.SetBounds(half + Ui.S(10), 0, row.Width - half - Ui.S(10), row.Height);
            };
            Stack.Add(row, height, gap);
        }
    }
}
