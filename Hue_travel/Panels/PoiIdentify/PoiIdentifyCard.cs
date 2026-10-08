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
        }

        private void btnClose_Click(object sender, EventArgs e) => Hide();

        public void ShowPoi(Poi poi)
        {
            if (poi == null) return;

            btnBadge.Text = poi.Category == PoiCategory.Heritage ? "Di tích cấp Quốc gia đặc biệt" : CategoryName(poi.Category);
            lblName.Text = poi.Name;
            lblAddress.Text = poi.Address;
            lblHoursValue.Text = poi.OpeningHours;
            lblPriceValue.Text = poi.TicketPrice;
            Visible = true;
            BringToFront();
        }

        private static string CategoryName(PoiCategory category)
        {
            switch (category)
            {
                case PoiCategory.Food: return "Ẩm thực";
                case PoiCategory.Stay: return "Lưu trú";
                case PoiCategory.River: return "Sông Hương";
                case PoiCategory.Transport: return "Giao thông";
                default: return "Địa điểm";
            }
        }
    }
}
