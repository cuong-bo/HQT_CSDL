using System.Windows.Forms;
using Hue_travel.Models;

namespace Hue_travel.Panels.PoiIdentify
{
    public partial class PoiIdentifyCard : UserControl
    {
        public PoiIdentifyCard()
        {
            InitializeComponent();
        }

        public void ShowPoi(Poi poi)
        {
            lblBadge.Text = poi.Category == PoiCategory.Heritage ? "Di tích cấp Quốc gia đặc biệt" : poi.Category.ToString();
            lblName.Text = poi.Name;
            lblAddress.Text = poi.Address;
            lblHours.Text = $"Giờ mở cửa: {poi.OpeningHours}";
            lblPrice.Text = $"Giá vé: {poi.TicketPrice}";
            Visible = true;
            BringToFront();
        }
    }
}
