using System;
using System.Drawing;
using System.Windows.Forms;
using Hue_travel.Theme;

namespace Hue_travel.Shell
{
    public partial class AppStatusBar : UserControl
    {
        private const int EdgePadding = 14; // px-3.5
        private const int GroupGap = 12;    // gap-3 inside the right group

        public AppStatusBar()
        {
            InitializeComponent();
            // The pill is compact: the captions ("Khu vực đang xem:", "Thời tiết hôm nay:") only add noise, the icons say enough.
            captionArea.Visible = false;
            captionWeather.Visible = false;
            lblAreaValue.AutoSize = false;
            lblAreaValue.AutoEllipsis = true;
            lblAreaValue.TextAlign = ContentAlignment.MiddleLeft;
            lblWeatherValue.AutoSize = false;
            lblWeatherValue.AutoEllipsis = true;
            lblWeatherValue.TextAlign = ContentAlignment.MiddleCenter;
            lblCoords.TextAlign = ContentAlignment.MiddleLeft;
            lblScale.TextAlign = ContentAlignment.MiddleRight;
            LayoutItems();
            Resize += (s, e) => LayoutItems();
        }

        public void SetArea(string area)
        {
            lblAreaValue.Text = area;
            LayoutItems();
        }

        /// <summary>Raised when the coordinate read-out is clicked (the host switches between UTM and degrees).</summary>
        public event EventHandler CoordinatesClicked;

        public void SetCoordinates(string text)
        {
            lblCoords.Text = text;
        }

        private void lblCoords_Click(object sender, EventArgs e) => CoordinatesClicked?.Invoke(this, EventArgs.Empty);

        public void SetWeather(string text)
        {
            lblWeatherValue.Text = text;
            LayoutItems();
        }

        public void SetScale(string text)
        {
            lblScale.Text = $"Tỉ lệ: {text}";
            LayoutItems();
        }

        /// <summary>
        /// A compact single-row pill: area on the left, weather in the middle, coordinates and scale on the right.
        /// Everything is clipped with an ellipsis, so a narrow window never overlaps the groups.
        /// </summary>
        private void LayoutItems()
        {
            int pad = Ui.S(EdgePadding);
            int height = Height;
            if (height <= 0) return;

            lblScale.AutoSize = false;
            lblCoords.AutoSize = false;
            int lineHeight = Math.Min(height, lblAreaValue.Font.Height + Ui.S(4));
            int top = (height - lineHeight) / 2;
            lblScale.SetBounds(Width - pad - Ui.S(92), top, Ui.S(92), lineHeight);
            lblCoords.SetBounds(lblScale.Left - Ui.S(GroupGap) - Ui.S(150), top, Ui.S(150), lineHeight);
            dividerCoords.Visible = false;

            int left = pad;
            int right = lblCoords.Left - Ui.S(GroupGap);
            int areaWidth = Math.Max(Ui.S(80), (right - left) * 45 / 100);
            lblAreaValue.SetBounds(left, top, areaWidth, lineHeight);

            int weatherLeft = left + areaWidth + Ui.S(GroupGap);
            lblWeatherValue.SetBounds(weatherLeft, top, Math.Max(0, right - weatherLeft), lineHeight);
        }
    }
}
