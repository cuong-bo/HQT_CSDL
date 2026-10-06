using System.Windows.Forms;
using AxInterop.MapWinGIS;
using Interop.MapWinGIS;

namespace Hue_travel.MapEngine
{
    public partial class MapHost : UserControl
    {
        public MapHost()
        {
            InitializeComponent();
        }

        public AxMap Map => axMap;

        public void ZoomIn() => axMap.ZoomIn(0.5);

        public void ZoomOut() => axMap.ZoomOut(0.5);

        public void PanMode(bool enabled)
        {
            axMap.CursorMode = enabled
                ? tkCursorMode.cmPan
                : tkCursorMode.cmNone;
        }
    }
}
