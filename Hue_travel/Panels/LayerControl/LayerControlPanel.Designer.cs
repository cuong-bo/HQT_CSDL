using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using Hue_travel.Theme;

namespace Hue_travel.Panels.LayerControl
{
    partial class LayerControlPanel
    {
        private System.ComponentModel.IContainer components = null;
        private PanelHeader header;
        private Label lblHeading;
        private LayerRow rowHeritage;
        private LayerRow rowFood;
        private LayerRow rowStay;
        private LayerRow rowRoads;
        private LayerRow rowRiver;
        private LayerRow rowPiers;
        private Panel dividerBaseMap;
        private IconButton captionBaseMap;
        private IconButton badgeBaseMap;
        private Panel dividerBrightness;
        private Label lblBrightnessCaption;
        private Label lblBrightnessValue;
        private Guna2TrackBar sliderBrightness;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        // Every property is assigned inline (no helper calls) so the WinForms Designer can render it.
        private void InitializeComponent()
        {
            this.header = new PanelHeader();
            this.lblHeading = new Label();
            this.rowHeritage = new LayerRow();
            this.rowFood = new LayerRow();
            this.rowStay = new LayerRow();
            this.rowRoads = new LayerRow();
            this.rowRiver = new LayerRow();
            this.rowPiers = new LayerRow();
            this.dividerBaseMap = new Panel();
            this.captionBaseMap = new IconButton();
            this.badgeBaseMap = new IconButton();
            this.dividerBrightness = new Panel();
            this.lblBrightnessCaption = new Label();
            this.lblBrightnessValue = new Label();
            this.sliderBrightness = new Guna2TrackBar();
            this.SuspendLayout();

            // w-76 (304px) panel on white.
            this.BackColor = ThemeColors.SurfaceContainerLowest;
            this.Size = new Size(328, 372);

            this.header.IconName = "category";
            this.header.Text = "Danh Mục Địa Điểm";
            this.header.Prominent = false;
            this.header.CloseClicked += new System.EventHandler(this.header_CloseClicked);

            // text-[11px] font-bold uppercase text-slate-500
            this.lblHeading.Text = "CÁC ĐIỂM THAM QUAN TRÊN BẢN ĐỒ";
            this.lblHeading.Font = ThemeFonts.Bold(11);
            this.lblHeading.ForeColor = ThemeColors.Slate500;
            this.lblHeading.AutoSize = true;
            this.lblHeading.UseMnemonic = false;
            this.lblHeading.Location = new Point(12, 44);

            // Six layer rows (28px tall, 4px apart).
            this.rowHeritage.Text = "Di tích lịch sử & Điểm tham quan";
            this.rowHeritage.CountText = "38 điểm";
            this.rowHeritage.SwatchColor = ThemeColors.Sky600;
            this.rowHeritage.SwatchShape = SwatchShape.Circle;
            this.rowHeritage.Checked = true;
            this.rowHeritage.Location = new Point(12, 66);

            this.rowFood.Text = "Món ngon & Quán đặc sản Huế";
            this.rowFood.CountText = "64 quán";
            this.rowFood.SwatchColor = ThemeColors.Amber600;
            this.rowFood.SwatchShape = SwatchShape.Circle;
            this.rowFood.Checked = true;
            this.rowFood.Location = new Point(12, 98);

            this.rowStay.Text = "Khách sạn & Nơi lưu trú";
            this.rowStay.CountText = "52 nơi";
            this.rowStay.SwatchColor = ThemeColors.Blue600;
            this.rowStay.SwatchShape = SwatchShape.RoundedSquare;
            this.rowStay.Checked = true;
            this.rowStay.Location = new Point(12, 130);

            this.rowRoads.Text = "Tuyến đường chính & Cầu qua sông";
            this.rowRoads.CountText = "Đầy đủ";
            this.rowRoads.SwatchColor = ThemeColors.Slate500;
            this.rowRoads.SwatchShape = SwatchShape.Line;
            this.rowRoads.Checked = true;
            this.rowRoads.Location = new Point(12, 162);

            this.rowRiver.Text = "Sông Hương & Các hồ cảnh quan";
            this.rowRiver.CountText = "Cảnh sắc";
            this.rowRiver.SwatchColor = ThemeColors.Cyan400;
            this.rowRiver.SwatchShape = SwatchShape.Circle;
            this.rowRiver.Checked = true;
            this.rowRiver.Location = new Point(12, 194);

            this.rowPiers.Text = "Bến thuyền rồng ca Huế & Điểm xe buýt";
            this.rowPiers.CountText = "Tắt";
            this.rowPiers.SwatchColor = ThemeColors.Slate300;
            this.rowPiers.SwatchShape = SwatchShape.Circle;
            this.rowPiers.Checked = false;
            this.rowPiers.Location = new Point(12, 226);

            // border-t slate-100, "Lớp nền hiện tại" + current base map badge.
            this.dividerBaseMap.BackColor = ThemeColors.Slate100;
            this.dividerBaseMap.Size = new Size(304, 1);
            this.dividerBaseMap.Location = new Point(12, 262);

            this.captionBaseMap.AutoWidth = false;
            this.captionBaseMap.Text = "Lớp nền hiện tại";
            this.captionBaseMap.IconName = "layers";
            this.captionBaseMap.IconSize = 14;
            this.captionBaseMap.IconColor = ThemeColors.Primary;
            this.captionBaseMap.Font = ThemeFonts.Medium(11);
            this.captionBaseMap.ForeColor = ThemeColors.Slate600;
            this.captionBaseMap.FillColor = Color.Transparent;
            this.captionBaseMap.HoverFillColor = Color.Transparent;
            this.captionBaseMap.HorizontalPadding = 0;
            this.captionBaseMap.Gap = 4;
            this.captionBaseMap.Cursor = Cursors.Default;
            this.captionBaseMap.Size = new Size(140, 24);
            this.captionBaseMap.Location = new Point(12, 271);

            this.badgeBaseMap.AutoWidth = false;
            this.badgeBaseMap.Text = "Chuẩn sáng";
            this.badgeBaseMap.Font = ThemeFonts.SemiBold(11);
            this.badgeBaseMap.ForeColor = ThemeColors.Primary;
            this.badgeBaseMap.FillColor = ThemeColors.Sky50;
            this.badgeBaseMap.HoverFillColor = ThemeColors.Sky50;
            this.badgeBaseMap.BorderColor = ThemeColors.Sky200;
            this.badgeBaseMap.Radius = 4;
            this.badgeBaseMap.Cursor = Cursors.Default;
            this.badgeBaseMap.Size = new Size(84, 22);
            this.badgeBaseMap.Location = new Point(232, 272);

            // border-t, caption + percentage, slider.
            this.dividerBrightness.BackColor = ThemeColors.Slate100;
            this.dividerBrightness.Size = new Size(304, 1);
            this.dividerBrightness.Location = new Point(12, 306);

            this.lblBrightnessCaption.Text = "Độ sáng nền bản đồ";
            this.lblBrightnessCaption.Font = ThemeFonts.Medium(12);
            this.lblBrightnessCaption.ForeColor = ThemeColors.Slate600;
            this.lblBrightnessCaption.AutoSize = true;
            this.lblBrightnessCaption.Location = new Point(12, 316);

            this.lblBrightnessValue.Text = "85%";
            this.lblBrightnessValue.Font = ThemeFonts.Bold(12);
            this.lblBrightnessValue.ForeColor = ThemeColors.Primary;
            this.lblBrightnessValue.AutoSize = false;
            this.lblBrightnessValue.TextAlign = ContentAlignment.MiddleRight;
            this.lblBrightnessValue.Size = new Size(60, 18);
            this.lblBrightnessValue.Location = new Point(256, 316);

            this.sliderBrightness.Minimum = 20;
            this.sliderBrightness.Maximum = 100;
            this.sliderBrightness.Value = 85;
            this.sliderBrightness.ThumbColor = ThemeColors.Primary;
            this.sliderBrightness.FillColor = ThemeColors.Slate200;
            this.sliderBrightness.Size = new Size(304, 20);
            this.sliderBrightness.Location = new Point(12, 340);
            this.sliderBrightness.ValueChanged += new System.EventHandler(this.sliderBrightness_ValueChanged);

            this.Controls.Add(this.sliderBrightness);
            this.Controls.Add(this.lblBrightnessValue);
            this.Controls.Add(this.lblBrightnessCaption);
            this.Controls.Add(this.dividerBrightness);
            this.Controls.Add(this.badgeBaseMap);
            this.Controls.Add(this.captionBaseMap);
            this.Controls.Add(this.dividerBaseMap);
            this.Controls.Add(this.rowPiers);
            this.Controls.Add(this.rowRiver);
            this.Controls.Add(this.rowRoads);
            this.Controls.Add(this.rowStay);
            this.Controls.Add(this.rowFood);
            this.Controls.Add(this.rowHeritage);
            this.Controls.Add(this.lblHeading);
            this.Controls.Add(this.header);
            this.Name = "LayerControlPanel";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
