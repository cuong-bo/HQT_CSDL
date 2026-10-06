using System;
using System.Windows.Forms;
using Hue_travel.SampleData;
using Hue_travel.Theme;

namespace Hue_travel.Panels.TravelGuide
{
    public partial class TravelGuidePanel : UserControl
    {
        public event EventHandler CloseRequested;

        public TravelGuidePanel()
        {
            InitializeComponent();
            PopulateTours();
        }

        private void btnClose_Click(object sender, EventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

        private void PopulateTours()
        {
            foreach (var tour in SampleTours.All)
            {
                var card = new Panel
                {
                    Width = 352,
                    Height = 60,
                    Margin = new Padding(0, 0, 0, 8),
                    BackColor = ThemeColors.SurfaceContainerLow
                };

                var title = new Label
                {
                    Text = $"{tour.Title}  [{tour.Badge}]",
                    Font = ThemeFonts.LabelMd,
                    ForeColor = ThemeColors.OnSurface,
                    AutoSize = false,
                    Width = 340,
                    Location = new System.Drawing.Point(8, 6)
                };

                var desc = new Label
                {
                    Text = tour.Description,
                    Font = ThemeFonts.BodySm,
                    ForeColor = ThemeColors.OnSurfaceVariant,
                    AutoSize = false,
                    Width = 340,
                    Height = 32,
                    Location = new System.Drawing.Point(8, 26)
                };

                card.Controls.Add(title);
                card.Controls.Add(desc);
                flowTours.Controls.Add(card);
            }
        }
    }
}
