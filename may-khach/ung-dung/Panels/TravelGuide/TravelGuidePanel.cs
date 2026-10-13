using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.TravelGuide
{
    public partial class TravelGuidePanel : FloatingPanelForm
    {
        public TravelGuidePanel()
        {
            InitializeComponent();
            btnHotline.Click += (s, e) => CopyHotline();
            btnDownload.Click += (s, e) => Notify("Tính năng tải cẩm nang PDF sẽ được bổ sung", ToastKind.Info);
        }

        private const string Hotline = "0234.3828288";

        private void CopyHotline()
        {
            try
            {
                Clipboard.SetText(Hotline);
                Notify("Đã sao chép số hotline " + Hotline, ToastKind.Success);
            }
            catch (ExternalException)
            {
                Notify("Không sao chép được số hotline " + Hotline, ToastKind.Error);
            }
        }

        private void header_CloseClicked(object sender, EventArgs e) => RaiseCloseRequested();
    }
}
