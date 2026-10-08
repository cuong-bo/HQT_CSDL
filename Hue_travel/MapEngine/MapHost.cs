using System;
using System.Windows.Forms;
using AxInterop.MapWinGIS;
using Interop.MapWinGIS;

namespace Hue_travel.MapEngine
{
    public partial class MapHost : UserControl
    {
        private const int RightMargin = 16;   // right-4
        private const int ColumnTop = 64;     // top-16
        private const int StackGap = 6;       // gap-1.5
        private const int InfoTop = 12;       // top-3

        private static readonly string[] BaseMapIds = { "standard", "satellite", "night" };

        private string baseMapId = "standard";

        /// <summary>Raised when the on-map quick switch picks a base map ("standard", "satellite" or "night").</summary>
        public event EventHandler<string> BaseMapRequested;

        public MapHost()
        {
            InitializeComponent();

            // Controls added after the ActiveX map must be lifted above it explicitly.
            stackZoom.BringToFront();
            compass.BringToFront();
            stackBaseMaps.BringToFront();
            boxInfo.BringToFront();
            LayoutOverlays();
            Resize += (s, e) => LayoutOverlays();

        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!DesignMode) InitializeMap();
        }

        public AxMap Map => axMap;

        public string BaseMapId => baseMapId;

        public bool IsMeasuring => axMap.CursorMode == tkCursorMode.cmMeasure;

        public void ZoomIn() => axMap.ZoomIn(0.5);

        public void ZoomOut() => axMap.ZoomOut(0.5);

        public void ZoomToFull() => axMap.ZoomToMaxExtents();

        public void PanMode(bool enabled)
        {
            axMap.CursorMode = enabled
                ? tkCursorMode.cmPan
                : tkCursorMode.cmNone;
        }

        /// <summary>Switches between pan and the MapWinGIS measure tool; returns the resulting measuring state.</summary>
        public bool SetMeasureMode(bool enabled)
        {
            axMap.CursorMode = enabled ? tkCursorMode.cmMeasure : tkCursorMode.cmPan;
            stackZoom.ActiveIndex = enabled ? -1 : 2;
            return enabled;
        }

        public void SetBaseMap(string mapId)
        {
            int index = Array.IndexOf(BaseMapIds, mapId);
            if (index < 0) return;
            baseMapId = mapId;
            stackBaseMaps.ActiveIndex = index;

            // Rebuild the layers in the new palette but keep the user's current view.
            Extents view = axMap.Extents;
            MapScene.Build(axMap, MapPalette.For(mapId), zoomToFull: false);
            axMap.Extents = view;
        }

        private void InitializeMap()
        {
            axMap.ShowCoordinates = tkCoordinatesDisplay.cdmNone;
            axMap.ScalebarVisible = false;
            axMap.ShowRedrawTime = false;
            axMap.ShowVersionNumber = false;
            axMap.CursorMode = tkCursorMode.cmPan;
            MapScene.Build(axMap, MapPalette.For(baseMapId));
        }

        // Anchors the overlays like the mockup: info pill centred at the top, controls in the top-right column.
        private void LayoutOverlays()
        {
            boxInfo.Width = Math.Min(680, Math.Max(200, Width - 2 * (RightMargin + 48)));
            tagInfo.Left = boxInfo.Width - tagInfo.Width - 12;
            captionInfo.Width = Math.Max(0, tagInfo.Left - captionInfo.Left - 8);
            boxInfo.Location = new System.Drawing.Point((Width - boxInfo.Width) / 2, InfoTop);

            int x = Width - RightMargin - stackBaseMaps.Width;
            stackBaseMaps.Location = new System.Drawing.Point(x, ColumnTop);
            compass.Location = new System.Drawing.Point(x + (stackBaseMaps.Width - compass.Width) / 2, stackBaseMaps.Bottom + StackGap + 4);
            stackZoom.Location = new System.Drawing.Point(x, compass.Bottom + StackGap);
        }

        private void stackBaseMaps_ItemClicked(object sender, int index)
        {
            SetBaseMap(BaseMapIds[index]);
            BaseMapRequested?.Invoke(this, BaseMapIds[index]);
        }

        private void stackZoom_ItemClicked(object sender, int index)
        {
            if (index == 0) ZoomIn();
            else if (index == 1) ZoomOut();
            else
            {
                PanMode(true);
                stackZoom.ActiveIndex = 2;
            }
        }

        private void compass_Click(object sender, EventArgs e) => ZoomToFull();
    }
}
