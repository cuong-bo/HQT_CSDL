using System;
using Hue_travel.Models;
using Hue_travel.Theme;

namespace Hue_travel.Panels.PoiIdentify
{
    public partial class PoiIdentifyCard : FloatingPanelForm
    {
        public PoiIdentifyCard()
        {
            InitializeComponent();
            foreach (System.Windows.Forms.Label value in new[] { lblName, lblAddress, lblHoursValue, lblPriceValue, lblPhoneValue })
                value.AutoEllipsis = true;
        }

        private void btnClose_Click(object sender, EventArgs e) => Hide();

        public void ShowPoi(Poi poi)
        {
            if (poi == null) return;

            btnBadge.Text = !string.IsNullOrEmpty(poi.TypeLabel) ? poi.TypeLabel : CategoryName(poi.Category);
            lblHeaderTitle.Left = btnBadge.Right + 10; // the title follows the badge, whatever its width
            lblName.Text = poi.Name;
            lblAddress.Text = poi.Address;
            lblHoursCaption.Text = poi.Category == PoiCategory.Stay ? "Giờ phòng:" : poi.Category == PoiCategory.Transport || poi.Category == PoiCategory.Service ? "Giờ hoạt động:" : "Giờ mở cửa:";
            lblPriceCaption.Text = PriceCaption(poi.Category);
            lblPhoneCaption.Text = "Điện thoại:";
            lblHoursValue.Text = poi.OpeningHours;
            lblPriceValue.Text = poi.TicketPrice;
            lblPhoneValue.Text = string.IsNullOrEmpty(poi.Phone) ? "Chưa có thông tin" : poi.Phone;
            Visible = true;
            BringToFront();
        }

        private static string PriceCaption(PoiCategory category)
        {
            switch (category)
            {
                case PoiCategory.Stay: return "Giá phòng:";
                case PoiCategory.Service: return "Giá dịch vụ:";
                case PoiCategory.Transport: return "Giá:";
                default: return "Giá vé:";
            }
        }

        private static string CategoryName(PoiCategory category)
        {
            switch (category)
            {
                case PoiCategory.Food: return "Ẩm thực";
                case PoiCategory.Stay: return "Lưu trú";
                case PoiCategory.Scenery: return "Cảnh quan";
                case PoiCategory.Service: return "Dịch vụ";
                case PoiCategory.Transport: return "Giao thông";
                default: return "Địa điểm";
            }
        }
    }
}
