using System.Drawing;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    partial class AppStatusBar
    {
        private System.ComponentModel.IContainer components = null;
        private Panel panelTopBorder;
        private IconButton captionArea;
        private Label lblAreaValue;
        private IconButton captionWeather;
        private Label lblWeatherValue;
        private Label lblScale;
        private Label lblCoords;
        private Panel dividerCoords;
        private Panel dividerScale;
        private IconButton pillReady;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        // Every property is assigned inline (no helper calls) so the WinForms Designer can render it.
        // Final x-positions are set by AppStatusBar.LayoutItems() (flex "justify-between" behaviour).
        private void InitializeComponent()
        {
            this.panelTopBorder = new Panel();
            this.captionArea = new IconButton();
            this.lblAreaValue = new Label();
            this.captionWeather = new IconButton();
            this.lblWeatherValue = new Label();
            this.lblScale = new Label();
            this.lblCoords = new Label();
            this.dividerCoords = new Panel();
            this.dividerScale = new Panel();
            this.pillReady = new IconButton();
            this.SuspendLayout();

            // h-7 white footer, 1px slate-200 top border, 12px slate-600 text.
            this.BackColor = ThemeColors.SurfaceContainerLowest;

            this.panelTopBorder.Dock = DockStyle.Top;
            this.panelTopBorder.Height = 1;
            this.panelTopBorder.BackColor = ThemeColors.Slate200;

            this.captionArea.Text = "Khu vực đang xem:";
            this.captionArea.IconName = "explore";
            this.captionArea.IconSize = 15;
            this.captionArea.IconColor = ThemeColors.Primary;
            this.captionArea.Font = ThemeFonts.Regular(12);
            this.captionArea.ForeColor = ThemeColors.Slate600;
            this.captionArea.FillColor = Color.Transparent;
            this.captionArea.HoverFillColor = Color.Transparent;
            this.captionArea.HorizontalPadding = 0;
            this.captionArea.Gap = 8;
            this.captionArea.Cursor = Cursors.Default;
            this.captionArea.Size = new Size(150, 22);
            this.captionArea.Location = new Point(12, 4);

            this.lblAreaValue.Text = "Trung tâm Cố đô Huế & Bờ sông Hương";
            this.lblAreaValue.Font = ThemeFonts.Bold(12);
            this.lblAreaValue.ForeColor = ThemeColors.Slate600;
            this.lblAreaValue.BackColor = Color.Transparent;
            this.lblAreaValue.AutoSize = true;
            this.lblAreaValue.UseMnemonic = false;
            this.lblAreaValue.Location = new Point(166, 5);

            this.captionWeather.Text = "Thời tiết hôm nay:";
            this.captionWeather.IconName = "wb_sunny";
            this.captionWeather.IconSize = 16;
            this.captionWeather.IconColor = ThemeColors.Amber500;
            this.captionWeather.Font = ThemeFonts.Regular(12);
            this.captionWeather.ForeColor = ThemeColors.Slate600;
            this.captionWeather.FillColor = Color.Transparent;
            this.captionWeather.HoverFillColor = Color.Transparent;
            this.captionWeather.HorizontalPadding = 0;
            this.captionWeather.Gap = 8;
            this.captionWeather.Cursor = Cursors.Default;
            this.captionWeather.Size = new Size(150, 22);
            this.captionWeather.Location = new Point(520, 4);

            this.lblWeatherValue.Text = "Nắng đẹp, 26°C - Rất thuận tiện tham quan";
            this.lblWeatherValue.Font = ThemeFonts.Bold(12);
            this.lblWeatherValue.ForeColor = ThemeColors.Slate600;
            this.lblWeatherValue.BackColor = Color.Transparent;
            this.lblWeatherValue.AutoSize = true;
            this.lblWeatherValue.UseMnemonic = false;
            this.lblWeatherValue.Location = new Point(676, 5);

            this.lblScale.Text = "Tỉ lệ: 1 : 15,000";
            this.lblScale.Font = ThemeFonts.Regular(12);
            this.lblScale.ForeColor = ThemeColors.Slate600;
            this.lblScale.BackColor = Color.Transparent;
            this.lblScale.AutoSize = true;
            this.lblScale.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.lblScale.Location = new Point(1030, 5);

            this.lblCoords.Text = "--°N, --°E";
            this.lblCoords.Font = ThemeFonts.Regular(12);
            this.lblCoords.ForeColor = ThemeColors.Slate600;
            this.lblCoords.BackColor = Color.Transparent;
            this.lblCoords.AutoSize = false;
            this.lblCoords.TextAlign = ContentAlignment.MiddleLeft;
            this.lblCoords.UseMnemonic = false;
            this.lblCoords.Cursor = Cursors.Hand;
            this.lblCoords.Size = new Size(190, 18);
            this.lblCoords.Location = new Point(840, 5);
            this.lblCoords.Click += new System.EventHandler(this.lblCoords_Click);

            this.dividerCoords.BackColor = ThemeColors.Slate300;
            this.dividerCoords.Size = new Size(1, 12);
            this.dividerCoords.Location = new Point(1020, 9);

            this.dividerScale.BackColor = ThemeColors.Slate300;
            this.dividerScale.Size = new Size(1, 12);
            this.dividerScale.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.dividerScale.Location = new Point(1130, 9);

            this.pillReady.Text = "Bản đồ du lịch sẵn sàng";
            this.pillReady.Font = ThemeFonts.SemiBold(12);
            this.pillReady.ForeColor = ThemeColors.Emerald700;
            this.pillReady.DotColor = ThemeColors.Emerald500;
            this.pillReady.FillColor = Color.Transparent;
            this.pillReady.HoverFillColor = Color.Transparent;
            this.pillReady.HorizontalPadding = 0;
            this.pillReady.Gap = 6;
            this.pillReady.Cursor = Cursors.Default;
            this.pillReady.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.pillReady.Size = new Size(160, 22);
            this.pillReady.Location = new Point(1108, 4);

            this.Controls.Add(this.pillReady);
            this.Controls.Add(this.dividerCoords);
            this.Controls.Add(this.lblCoords);
            this.Controls.Add(this.dividerScale);
            this.Controls.Add(this.lblScale);
            this.Controls.Add(this.lblWeatherValue);
            this.Controls.Add(this.captionWeather);
            this.Controls.Add(this.lblAreaValue);
            this.Controls.Add(this.captionArea);
            this.Controls.Add(this.panelTopBorder);
            this.Name = "AppStatusBar";
            this.Size = new Size(1280, 28);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
