using System;
using System.Collections.Generic;
using System.Windows.Forms;
using AxInterop.MapWinGIS;
using Hue_travel.Theme;
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
        private SceneIndex scene = new SceneIndex();
        private readonly Dictionary<string, bool> groupVisibility = new Dictionary<string, bool>();
        private int mouseDownX = int.MinValue, mouseDownY = int.MinValue;
        private bool wasButtonDown;
        private readonly Timer pressTracker = new Timer { Interval = 10 };

        /// <summary>Raised when the on-map quick switch picks a base map ("standard", "satellite" or "night").</summary>
        public event EventHandler<string> BaseMapRequested;

        /// <summary>Raised when a pin is clicked; the argument is the pin key (the sample POI id, e.g. "dai-noi").</summary>
        public event EventHandler<string> PinClicked;

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
            new ToolTip().SetToolTip(compass, "Khôi phục hướng Bắc");
            Disposed += (s, e) => pressTracker.Dispose();

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
            BuildScene(zoomToFull: false);
            axMap.Extents = view;
        }

        /// <summary>Shows or hides one toggle group of map layers ("heritage", "food", "stay", "roads", "river", "piers").</summary>
        public void SetGroupVisible(string group, bool visible)
        {
            groupVisibility[group] = visible;
            ApplyGroupVisibility(group);
        }

        /// <summary>Centres the map on a pin and zooms in to it. Returns false when the key has no pin on the map.</summary>
        public bool ZoomToPin(string key)
        {
            PinLocation pin = scene.Pins.Find(p => p.Key == key);
            if (pin == null) return false;

            var view = new Extents();
            view.SetBounds(pin.X - 260, pin.Y - 146, 0, pin.X + 260, pin.Y + 146, 0);
            axMap.Extents = view;
            return true;
        }

        private void BuildScene(bool zoomToFull)
        {
            scene = MapScene.Build(axMap, MapPalette.For(baseMapId), zoomToFull);
            foreach (string group in groupVisibility.Keys) ApplyGroupVisibility(group);
        }

        private void ApplyGroupVisibility(string group)
        {
            List<int> handles;
            if (!scene.Groups.TryGetValue(group, out handles)) return;
            foreach (int handle in handles) axMap.set_LayerVisible(handle, groupVisibility[group]);
        }

        // A click is a press and release in (almost) the same spot, so panning is not mistaken for a pin click.
        private void axMap_MouseUp(int x, int y)
        {
            if (IsMeasuring || Math.Abs(x - mouseDownX) > 3 || Math.Abs(y - mouseDownY) > 3) return;

            string hit = null;
            double best = double.MaxValue;
            foreach (PinLocation pin in scene.Pins)
            {
                double px = 0, py = 0;
                axMap.ProjToPixel(pin.X, pin.Y, ref px, ref py);
                // The pin picture sits above its anchor point: ~110 px wide, ~80 px tall.
                if (Math.Abs(x - px) > Ui.S(55) || y < py - Ui.S(80) || y > py + Ui.S(8)) continue;
                double distance = Math.Abs(x - px) + Math.Abs(y - py + Ui.S(30));
                if (distance < best) { best = distance; hit = pin.Key; }
            }
            if (hit != null) PinClicked?.Invoke(this, hit);
        }

        private void InitializeMap()
        {
            axMap.ShowCoordinates = tkCoordinatesDisplay.cdmNone;
            axMap.ScalebarVisible = false;
            axMap.ShowRedrawTime = false;
            axMap.ShowVersionNumber = false;
            axMap.CursorMode = tkCursorMode.cmPan;
            axMap.InertiaOnPanning = tkCustomState.csTrue;   // the map glides after a drag
            axMap.SendMouseDown = true;
            axMap.SendMouseUp = true;
            // The ActiveX map does not raise MouseDown while panning, so note where the left button went down by polling.
            pressTracker.Tick += (s, e) =>
            {
                bool isDown = (Control.MouseButtons & MouseButtons.Left) != 0;
                if (isDown && !wasButtonDown)
                {
                    System.Drawing.Point p = axMap.PointToClient(Cursor.Position);
                    mouseDownX = p.X;
                    mouseDownY = p.Y;
                }
                wasButtonDown = isDown;
            };
            pressTracker.Start();
            axMap.MouseUpEvent += (s, e) => axMap_MouseUp(e.x, e.y);
            BuildScene(zoomToFull: true);
        }

        // Anchors the overlays like the mockup: info pill centred at the top, controls in the top-right column.
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            LayoutOverlays(); // Control.Scale grows children after the Resize handler ran
        }

        private void LayoutOverlays()
        {
            boxInfo.Width = Math.Min(Ui.S(680), Math.Max(Ui.S(200), Width - 2 * Ui.S(RightMargin + 48)));
            tagInfo.Left = boxInfo.Width - tagInfo.Width - Ui.S(12);
            captionInfo.Width = Math.Max(0, tagInfo.Left - captionInfo.Left - Ui.S(8));
            boxInfo.Location = new System.Drawing.Point((Width - boxInfo.Width) / 2, Ui.S(InfoTop));

            int x = Width - Ui.S(RightMargin) - stackBaseMaps.Width;
            stackBaseMaps.Location = new System.Drawing.Point(x, Ui.S(ColumnTop));
            compass.Location = new System.Drawing.Point(x + (stackBaseMaps.Width - compass.Width) / 2, stackBaseMaps.Bottom + Ui.S(StackGap + 4));
            stackZoom.Location = new System.Drawing.Point(x, compass.Bottom + Ui.S(StackGap));
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
