using System;
using System.Collections.Generic;
using System.Drawing;
using Hue_travel.Models;
using Hue_travel.Theme;

namespace Hue_travel.Panels.PoiIdentify
{
    public partial class PoiIdentifyCard : FloatingPanelForm
    {
        private readonly IconButton btnFavorite = AccountButton("Yêu thích", "star", 110);
        private readonly IconButton btnClaim = AccountButton("Tôi là chủ quản", "verified_user", 140);
        private readonly IconButton btnReport = AccountButton("Báo sai", "campaign", 90);
        private Poi shown;
        private bool signedIn;

        /// <summary>Raised by the buttons that need an account; the host knows who is signed in.</summary>
        public event EventHandler<Poi> FavoriteToggled;
        public event EventHandler<Poi> ClaimRequested;
        public event EventHandler<Poi> ReportRequested;
        public event EventHandler<Poi> AddToRouteRequested;

        /// <summary>Ids of the signed-in user's favourites, so the star shows the right state.</summary>
        public ISet<string> FavoriteIds { get; set; } = new HashSet<string>();

        public PoiIdentifyCard()
        {
            InitializeComponent();
            Height += 34;   // room for the account row under the two main buttons
            btnFavorite.Location = new Point(172, 204);
            btnClaim.Location = new Point(290, 204);
            btnReport.Location = new Point(438, 204);
            foreach (IconButton b in new[] { btnFavorite, btnClaim, btnReport }) { b.Visible = false; Controls.Add(b); }
            btnFavorite.Click += (s, e) => { if (shown != null) FavoriteToggled?.Invoke(this, shown); };
            btnClaim.Click += (s, e) => { if (shown != null) ClaimRequested?.Invoke(this, shown); };
            btnReport.Click += (s, e) => { if (shown != null) ReportRequested?.Invoke(this, shown); };
            btnAddToRoute.Click += (s, e) => { if (shown != null) AddToRouteRequested?.Invoke(this, shown); };
            foreach (System.Windows.Forms.Label value in new[] { lblName, lblAddress, lblHoursValue, lblPriceValue, lblPhoneValue })
                value.AutoEllipsis = true;
        }

        private void btnClose_Click(object sender, EventArgs e) => Hide();

        public void ShowPoi(Poi poi)
        {
            if (poi == null) return;
            shown = poi;
            RefreshAccountButtons();

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

        /// <summary>Shows the favourite / claim / report buttons only to a signed-in user.</summary>
        public bool SignedIn
        {
            get { return signedIn; }
            set { signedIn = value; RefreshAccountButtons(); }
        }

        public void RefreshAccountButtons()
        {
            bool visible = signedIn && shown != null;
            btnFavorite.Visible = btnClaim.Visible = btnReport.Visible = visible;
            if (!visible) return;
            bool favorite = FavoriteIds != null && FavoriteIds.Contains(shown.Id);
            btnFavorite.Text = favorite ? "Bỏ yêu thích" : "Yêu thích";
            btnFavorite.IconColor = favorite ? ThemeColors.Amber600 : ThemeColors.Primary;
        }

        private static IconButton AccountButton(string text, string icon, int width)
        {
            return new IconButton
            {
                AutoWidth = false, Text = text, IconName = icon, IconSize = 14, Font = ThemeFonts.Medium(11), Radius = 4, Gap = 4,
                HorizontalPadding = 8, ForeColor = ThemeColors.Slate700, IconColor = ThemeColors.Primary, FillColor = Color.White,
                HoverFillColor = ThemeColors.Slate100, BorderColor = ThemeColors.Slate200, Size = new Size(width, 28),
            };
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
