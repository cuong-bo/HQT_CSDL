using System.Drawing;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.PoiIdentify
{
    partial class PoiIdentifyCard
    {
        private System.ComponentModel.IContainer components = null;
        private Panel panelHeader;
        private IconButton btnBadge;
        private Label lblHeaderTitle;
        private IconButton btnClose;
        private PictureBox picPhoto;
        private Label lblName;
        private IconButton iconAddress;
        private Label lblAddress;
        private Label lblHoursCaption;
        private Label lblHoursValue;
        private Label lblPriceCaption;
        private Label lblPriceValue;
        private Label lblPhoneCaption;
        private Label lblPhoneValue;
        private IconButton btnAddToRoute;
        private IconButton btnShowOnMap;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        // Every property is assigned inline (no helper calls) so the WinForms Designer can render it.
        private void InitializeComponent()
        {
            this.panelHeader = new Panel();
            this.btnBadge = new IconButton();
            this.lblHeaderTitle = new Label();
            this.btnClose = new IconButton();
            this.picPhoto = new PictureBox();
            this.lblName = new Label();
            this.iconAddress = new IconButton();
            this.lblAddress = new Label();
            this.lblHoursCaption = new Label();
            this.lblHoursValue = new Label();
            this.lblPriceCaption = new Label();
            this.lblPriceValue = new Label();
            this.lblPhoneCaption = new Label();
            this.lblPhoneValue = new Label();
            this.btnAddToRoute = new IconButton();
            this.btnShowOnMap = new IconButton();
            ((System.ComponentModel.ISupportInitialize)(this.picPhoto)).BeginInit();
            this.SuspendLayout();
            this.panelHeader.SuspendLayout();

            // w-[540px] card on white.
            this.BackColor = ThemeColors.SurfaceContainerLowest;
            this.Size = new Size(540, 214);

            // Header strip: sky-50.
            this.panelHeader.Dock = DockStyle.Top;
            this.panelHeader.Height = 38;
            this.panelHeader.BackColor = ThemeColors.Sky50;

            this.btnBadge.Text = "Di tích cấp Quốc gia đặc biệt";
            this.btnBadge.Font = ThemeFonts.Bold(11);
            this.btnBadge.ForeColor = Color.White;
            this.btnBadge.FillColor = ThemeColors.Amber500;
            this.btnBadge.HoverFillColor = ThemeColors.Amber500;
            this.btnBadge.Radius = 12;
            this.btnBadge.HorizontalPadding = 8;
            this.btnBadge.Cursor = Cursors.Default;
            this.btnBadge.Height = 20;
            this.btnBadge.Location = new Point(14, 9);

            this.lblHeaderTitle.Text = "Thông Tin Chi Tiết Địa Điểm";
            this.lblHeaderTitle.Font = ThemeFonts.SemiBold(12);
            this.lblHeaderTitle.ForeColor = ThemeColors.Slate700;
            this.lblHeaderTitle.AutoSize = false;
            this.lblHeaderTitle.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHeaderTitle.Size = new Size(220, 38);
            this.lblHeaderTitle.Location = new Point(190, 0);

            this.btnClose.AutoWidth = false;
            this.btnClose.IconName = "close";
            this.btnClose.IconSize = 14;
            this.btnClose.IconColor = ThemeColors.Slate500;
            this.btnClose.FillColor = Color.Transparent;
            this.btnClose.HoverFillColor = ThemeColors.Slate200;
            this.btnClose.Radius = 4;
            this.btnClose.Size = new Size(20, 20);
            this.btnClose.Location = new Point(506, 9);
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);

            this.panelHeader.Controls.Add(this.btnBadge);
            this.panelHeader.Controls.Add(this.lblHeaderTitle);
            this.panelHeader.Controls.Add(this.btnClose);

            // 144 x 112 photo, rounded.
            this.picPhoto.Image = AppImages.PoiPhoto(144, 112, 6);
            this.picPhoto.Size = new Size(144, 112);
            this.picPhoto.Location = new Point(14, 52);

            // Details column.
            this.lblName.Text = "Đại Nội - Hoàng Thành Huế";
            this.lblName.Font = ThemeFonts.Bold(14);
            this.lblName.ForeColor = ThemeColors.Slate900;
            this.lblName.AutoSize = false;
            this.lblName.Size = new Size(354, 22);
            this.lblName.Location = new Point(172, 50);

            this.iconAddress.AutoWidth = false;
            this.iconAddress.IconName = "place";
            this.iconAddress.IconSize = 14;
            this.iconAddress.IconColor = ThemeColors.Primary;
            this.iconAddress.FillColor = Color.Transparent;
            this.iconAddress.HoverFillColor = Color.Transparent;
            this.iconAddress.Cursor = Cursors.Default;
            this.iconAddress.Size = new Size(14, 14);
            this.iconAddress.Location = new Point(172, 76);
            this.lblAddress.Text = "Đường 23 Tháng 8, Phường Thuận Hòa, TP. Huế";
            this.lblAddress.Font = ThemeFonts.Regular(12);
            this.lblAddress.ForeColor = ThemeColors.Slate600;
            this.lblAddress.AutoSize = false;
            this.lblAddress.Size = new Size(334, 18);
            this.lblAddress.Location = new Point(190, 74);

            this.lblHoursCaption.Text = "Giờ mở cửa:";
            this.lblHoursCaption.Font = ThemeFonts.Regular(12);
            this.lblHoursCaption.ForeColor = ThemeColors.Slate500;
            this.lblHoursCaption.AutoSize = false;
            this.lblHoursCaption.Size = new Size(110, 18);
            this.lblHoursCaption.Location = new Point(172, 98);
            this.lblHoursValue.Text = "07:00 - 17:30 (Mở cửa tất cả các ngày trong tuần)";
            this.lblHoursValue.Font = ThemeFonts.SemiBold(12);
            this.lblHoursValue.ForeColor = ThemeColors.Slate800;
            this.lblHoursValue.AutoSize = false;
            this.lblHoursValue.Size = new Size(244, 18);
            this.lblHoursValue.Location = new Point(282, 98);

            this.lblPriceCaption.Text = "Giá vé tham quan:";
            this.lblPriceCaption.Font = ThemeFonts.Regular(12);
            this.lblPriceCaption.ForeColor = ThemeColors.Slate500;
            this.lblPriceCaption.AutoSize = false;
            this.lblPriceCaption.Size = new Size(110, 18);
            this.lblPriceCaption.Location = new Point(172, 118);
            this.lblPriceValue.Text = "200.000 VNĐ / người lớn (Miễn phí trẻ dưới 1m)";
            this.lblPriceValue.Font = ThemeFonts.Bold(12);
            this.lblPriceValue.ForeColor = ThemeColors.Sky700;
            this.lblPriceValue.AutoSize = false;
            this.lblPriceValue.Size = new Size(244, 18);
            this.lblPriceValue.Location = new Point(282, 118);

            this.lblPhoneCaption.Text = "Số điện thoại hỗ trợ:";
            this.lblPhoneCaption.Font = ThemeFonts.Regular(12);
            this.lblPhoneCaption.ForeColor = ThemeColors.Slate500;
            this.lblPhoneCaption.AutoSize = false;
            this.lblPhoneCaption.Size = new Size(110, 18);
            this.lblPhoneCaption.Location = new Point(172, 138);
            this.lblPhoneValue.Text = "0234.3523237";
            this.lblPhoneValue.Font = ThemeFonts.SemiBold(12);
            this.lblPhoneValue.ForeColor = ThemeColors.Slate800;
            this.lblPhoneValue.AutoSize = false;
            this.lblPhoneValue.Size = new Size(244, 18);
            this.lblPhoneValue.Location = new Point(282, 138);

            // Actions.
            this.btnAddToRoute.Text = "Thêm vào lộ trình tham quan";
            this.btnAddToRoute.IconName = "add";
            this.btnAddToRoute.IconSize = 15;
            this.btnAddToRoute.Font = ThemeFonts.Medium(12);
            this.btnAddToRoute.ForeColor = Color.White;
            this.btnAddToRoute.IconColor = Color.White;
            this.btnAddToRoute.FillColor = ThemeColors.Primary;
            this.btnAddToRoute.HoverFillColor = ThemeColors.PrimaryContainer;
            this.btnAddToRoute.Radius = 4;
            this.btnAddToRoute.HorizontalPadding = 10;
            this.btnAddToRoute.Gap = 4;
            this.btnAddToRoute.Height = 28;
            this.btnAddToRoute.Location = new Point(172, 166);

            this.btnShowOnMap.Text = "Xem vị trí trên bản đồ";
            this.btnShowOnMap.IconName = "center_focus_strong";
            this.btnShowOnMap.IconSize = 15;
            this.btnShowOnMap.Font = ThemeFonts.Medium(12);
            this.btnShowOnMap.ForeColor = ThemeColors.Slate700;
            this.btnShowOnMap.IconColor = ThemeColors.Slate600;
            this.btnShowOnMap.FillColor = ThemeColors.Slate50;
            this.btnShowOnMap.HoverFillColor = ThemeColors.Slate100;
            this.btnShowOnMap.BorderColor = ThemeColors.Slate200;
            this.btnShowOnMap.Radius = 4;
            this.btnShowOnMap.HorizontalPadding = 10;
            this.btnShowOnMap.Gap = 4;
            this.btnShowOnMap.Height = 28;
            this.btnShowOnMap.Location = new Point(366, 166);

            this.Controls.Add(this.btnShowOnMap);
            this.Controls.Add(this.btnAddToRoute);
            this.Controls.Add(this.lblPhoneValue);
            this.Controls.Add(this.lblPhoneCaption);
            this.Controls.Add(this.lblPriceValue);
            this.Controls.Add(this.lblPriceCaption);
            this.Controls.Add(this.lblHoursValue);
            this.Controls.Add(this.lblHoursCaption);
            this.Controls.Add(this.lblAddress);
            this.Controls.Add(this.iconAddress);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.picPhoto);
            this.Controls.Add(this.panelHeader);
            this.Name = "PoiIdentifyCard";
            ((System.ComponentModel.ISupportInitialize)(this.picPhoto)).EndInit();
            this.panelHeader.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
