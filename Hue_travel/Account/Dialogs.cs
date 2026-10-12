using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Account
{
    internal sealed class DialogField
    {
        public DialogField(string label, string initial = "", int lines = 1, bool password = false)
        {
            Label = label;
            Initial = initial ?? "";
            Lines = lines;
            Password = password;
        }

        public string Label { get; }
        public string Initial { get; }
        public int Lines { get; }
        public bool Password { get; }
    }

    /// <summary>Simple modal forms (a few labelled text boxes, or a pick-from-a-list), laid out in code.</summary>
    internal static class Dialogs
    {
        /// <summary>Returns the entered values in order, or null when cancelled.</summary>
        public static string[] Ask(IWin32Window owner, string title, params DialogField[] fields)
        {
            using (var form = NewForm(title, 440))
            {
                var boxes = new List<TextBox>();
                int y = Ui.S(12);
                foreach (DialogField field in fields)
                {
                    var label = new Label { Text = field.Label, AutoSize = true, Font = ThemeFonts.Medium(11), ForeColor = ThemeColors.Slate700, Location = new Point(Ui.S(14), y) };
                    y += Ui.S(20);
                    var box = new TextBox
                    {
                        Text = field.Initial, Multiline = field.Lines > 1, ScrollBars = field.Lines > 1 ? ScrollBars.Vertical : ScrollBars.None,
                        UseSystemPasswordChar = field.Password, Font = ThemeFonts.Regular(12),
                        Location = new Point(Ui.S(14), y), Size = new Size(form.ClientSize.Width - Ui.S(28), Ui.S(field.Lines > 1 ? 24 * field.Lines : 26)),
                    };
                    y += box.Height + Ui.S(10);
                    form.Controls.Add(label);
                    form.Controls.Add(box);
                    boxes.Add(box);
                }
                AddButtons(form, y);
                if (form.ShowDialog(owner) != DialogResult.OK) return null;
                var values = new string[boxes.Count];
                for (int i = 0; i < boxes.Count; i++) values[i] = boxes[i].Text;
                return values;
            }
        }

        /// <summary>Shows rows in a list and returns the index of the chosen one, or -1.</summary>
        public static int Pick(IWin32Window owner, string title, string[] columns, IList<string[]> rows)
        {
            using (var form = NewForm(title, 560))
            {
                var list = new ListView
                {
                    View = View.Details, FullRowSelect = true, HideSelection = false, MultiSelect = false, Font = ThemeFonts.Regular(11),
                    Location = new Point(Ui.S(14), Ui.S(12)), Size = new Size(form.ClientSize.Width - Ui.S(28), Ui.S(240)),
                };
                foreach (string c in columns) list.Columns.Add(c, (list.Width - Ui.S(24)) / columns.Length);
                foreach (string[] row in rows) list.Items.Add(new ListViewItem(row));
                if (list.Items.Count > 0) list.Items[0].Selected = true;
                form.Controls.Add(list);
                AddButtons(form, list.Bottom + Ui.S(10));
                if (form.ShowDialog(owner) != DialogResult.OK || list.SelectedIndices.Count == 0) return -1;
                return list.SelectedIndices[0];
            }
        }

        private static Form NewForm(string title, int designWidth)
        {
            return new Form
            {
                Text = title, FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, MaximizeBox = false,
                MinimizeBox = false, ShowInTaskbar = false, BackColor = Color.White, Font = ThemeFonts.Regular(11), ClientSize = new Size(Ui.S(designWidth), Ui.S(100)),
            };
        }

        private static void AddButtons(Form form, int y)
        {
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new Size(Ui.S(90), Ui.S(30)), Font = ThemeFonts.Medium(11) };
            var cancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Size = new Size(Ui.S(90), Ui.S(30)), Font = ThemeFonts.Medium(11) };
            cancel.Location = new Point(form.ClientSize.Width - cancel.Width - Ui.S(14), y);
            ok.Location = new Point(cancel.Left - ok.Width - Ui.S(8), y);
            form.Controls.Add(ok);
            form.Controls.Add(cancel);
            form.AcceptButton = ok;
            form.CancelButton = cancel;
            form.ClientSize = new Size(form.ClientSize.Width, y + ok.Height + Ui.S(14));
        }
    }
}
