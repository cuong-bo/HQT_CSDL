using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Account
{
    /// <summary>Base of the screens inside the account panel: has the API, reports messages and loads its data.</summary>
    internal abstract class AccountTab : UserControl
    {
        protected AccountTab(ApiClient api)
        {
            Api = api;
            Dock = DockStyle.Fill;
            BackColor = Color.White;
            Padding = new Padding(12);
        }

        protected ApiClient Api { get; }

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

        /// <summary>A row of buttons docked at the bottom of the tab.</summary>
        protected FlowLayoutPanel ButtonRow(params IconButton[] buttons)
        {
            var row = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(0, 8, 0, 0), WrapContents = true };
            foreach (IconButton b in buttons)
            {
                b.Margin = new Padding(0, 0, 8, 4);
                row.Controls.Add(b);
            }
            return row;
        }

        protected string SelectedTag(ListView list) => list.SelectedItems.Count == 0 ? null : list.SelectedItems[0].Tag as string;
    }
}
