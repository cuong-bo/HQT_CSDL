using AxInterop.MapWinGIS;

namespace Hue_travel.MapEngine
{
    partial class MapHost
    {
        private System.ComponentModel.IContainer components = null;
        private AxMap axMap;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.axMap = new AxMap();
            ((System.ComponentModel.ISupportInitialize)(this.axMap)).BeginInit();
            this.SuspendLayout();

            this.axMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axMap.Name = "axMap";
            this.axMap.TabIndex = 0;

            this.Controls.Add(this.axMap);
            this.Name = "MapHost";
            this.Size = new System.Drawing.Size(900, 700);

            ((System.ComponentModel.ISupportInitialize)(this.axMap)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
