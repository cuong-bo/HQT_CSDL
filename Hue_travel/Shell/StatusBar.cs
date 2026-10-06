using System.Windows.Forms;

namespace Hue_travel.Shell
{
    public partial class AppStatusBar : UserControl
    {
        public AppStatusBar()
        {
            InitializeComponent();
        }

        public void SetCoordinates(double lat, double lon)
        {
            lblCoordinates.Text = $"Tọa độ: {lat:F5}, {lon:F5}";
        }

        public void SetScale(string text)
        {
            lblScale.Text = $"Tỉ lệ: {text}";
        }
    }
}
