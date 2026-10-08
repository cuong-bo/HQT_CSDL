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
            this.SuspendLayout();

            // w-76 (304px) panel on white.
            this.BackColor = ThemeColors.SurfaceContainerLowest;
            this.Size = new Size(328, 272);

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
