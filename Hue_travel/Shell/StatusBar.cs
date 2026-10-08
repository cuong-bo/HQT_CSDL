using System;
using System.Windows.Forms;

namespace Hue_travel.Shell
{
    public partial class AppStatusBar : UserControl
    {
        private const int EdgePadding = 12; // px-3
        private const int GroupGap = 12;    // gap-3 inside the right group

        public AppStatusBar()
        {
            InitializeComponent();
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
        /// Reproduces the mockup's "justify-between" footer: area group pinned left, scale/ready
        /// group pinned right, weather group centred in the space between them.
        /// </summary>
        private void LayoutItems()
        {
            int top = (Height - lblAreaValue.Height) / 2;

            captionArea.Left = Hue_travel.Theme.Ui.S(EdgePadding);
            lblAreaValue.Left = captionArea.Right;
            lblAreaValue.Top = top;
            captionArea.Top = (Height - captionArea.Height) / 2;

            pillReady.Left = Width - Hue_travel.Theme.Ui.S(EdgePadding) - pillReady.Width;
            pillReady.Top = (Height - pillReady.Height) / 2;
            dividerScale.Left = pillReady.Left - Hue_travel.Theme.Ui.S(GroupGap) - dividerScale.Width;
            dividerScale.Top = (Height - dividerScale.Height) / 2;
            lblScale.Left = dividerScale.Left - Hue_travel.Theme.Ui.S(GroupGap) - lblScale.Width;
            lblScale.Top = top;

            // Fixed-width read-out so the digits changing under the pointer do not shift the layout.
            lblCoords.Width = TextRenderer.MeasureText("E 000,000 \u00B7 N 0,000,000", lblCoords.Font, System.Drawing.Size.Empty, TextFormatFlags.NoPadding).Width + Hue_travel.Theme.Ui.S(8);
            dividerCoords.Left = lblScale.Left - Hue_travel.Theme.Ui.S(GroupGap) - dividerCoords.Width;
            dividerCoords.Top = (Height - dividerCoords.Height) / 2;
            lblCoords.Left = dividerCoords.Left - Hue_travel.Theme.Ui.S(GroupGap) - lblCoords.Width;
            lblCoords.Top = (Height - lblCoords.Height) / 2;

            int leftEnd = lblAreaValue.Right;
            int rightStart = lblCoords.Left;
            int weatherWidth = captionWeather.Width + lblWeatherValue.Width;
            int weatherLeft = leftEnd + (rightStart - leftEnd - weatherWidth) / 2;
            captionWeather.Left = weatherLeft;
            captionWeather.Top = (Height - captionWeather.Height) / 2;
            lblWeatherValue.Left = captionWeather.Right;
            lblWeatherValue.Top = top;
        }
    }
}
