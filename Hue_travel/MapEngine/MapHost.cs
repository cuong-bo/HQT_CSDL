using System;
using System.Collections.Generic;
using System.Windows.Forms;
using AxInterop.MapWinGIS;
using Hue_travel.Services;
using Hue_travel.Services.Area;
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

        private static readonly string[] BaseMapIds = { "roadmap", "satellite", "hybrid", "terrain" };

        private string baseMapId = BaseMapCatalog.DefaultId;
        private SceneIndex scene = new SceneIndex();
        private readonly Dictionary<string, bool> groupVisibility = new Dictionary<string, bool>();
        private int mouseDownX = int.MinValue, mouseDownY = int.MinValue;
        private bool wasButtonDown;
        private readonly Timer pressTracker = new Timer { Interval = 10 };
        private readonly Timer viewTracker = new Timer { Interval = 150 };
        private double lastXMin, lastXMax, lastYMin, lastYMax;
        private int lastPixelWidth = -1;

        /// <summary>Raised when the on-map quick switch picks a base map ("roadmap", "satellite", "hybrid" or "terrain").</summary>
        public event EventHandler<string> BaseMapRequested;

        /// <summary>Raised when a pin is clicked; the argument is the pin key (the sample POI id, e.g. "dai-noi").</summary>
        public event EventHandler<string> PinClicked;

        /// <summary>Raised after the visible extent (or the control width) changed: new centre and scale.</summary>
        public event EventHandler<MapViewChangedEventArgs> ViewChanged;

        /// <summary>Raised while the pointer moves over the map, with its coordinates.</summary>
        public event EventHandler<MapCursorEventArgs> CursorMoved;

        public MapHost()
        {
            InitializeComponent();

            // Controls added after the ActiveX map must be lifted above it explicitly.
            stackZoom.BringToFront();
            compass.BringToFront();
            stackBaseMaps.BringToFront();
            boxInfo.BringToFront();
            attribution.BringToFront();
            LayoutOverlays();
            Resize += (s, e) => LayoutOverlays();
            new ToolTip().SetToolTip(compass, "Khôi phục hướng Bắc");
            Disposed += (s, e) => { pressTracker.Dispose(); viewTracker.Dispose(); };

        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!DesignMode) InitializeMap();
        }

        public AxMap Map => axMap;

        public string BaseMapId => baseMapId;

        public bool IsMeasuring => axMap.CursorMode == tkCursorMode.cmMeasure;

        public MeasureKind ActiveMeasure
        {
            get
            {
                if (!IsMeasuring) return MeasureKind.None;
                return axMap.Measuring.MeasuringType == tkMeasuringType.MeasureArea ? MeasureKind.Area : MeasureKind.Distance;
            }
        }

        public void ZoomIn() => axMap.ZoomIn(0.5);

        public void ZoomOut() => axMap.ZoomOut(0.5);

        public void ZoomToFull() => axMap.Extents = MapScene.FullExtents();

        public void PanMode(bool enabled)
        {
            axMap.CursorMode = enabled
                ? tkCursorMode.cmPan
                : tkCursorMode.cmNone;
        }

        /// <summary>
        /// Starts the given MapWinGIS measure tool, or goes back to panning when that tool is already active.
        /// Returns the tool that is active afterwards.
        /// </summary>
        public MeasureKind ToggleMeasure(MeasureKind kind)
        {
            bool stop = kind == MeasureKind.None || ActiveMeasure == kind;
            axMap.Measuring.Clear();
            if (!stop)
            {
                axMap.Measuring.MeasuringType = kind == MeasureKind.Area ? tkMeasuringType.MeasureArea : tkMeasuringType.MeasureDistance;
                axMap.Measuring.LengthUnits = tkLengthDisplayMode.ldmMetric;
                axMap.Measuring.AreaUnits = tkAreaDisplayMode.admMetric;
            }
            axMap.CursorMode = stop ? tkCursorMode.cmPan : tkCursorMode.cmMeasure;
            stackZoom.ActiveIndex = stop ? 2 : -1;
            return stop ? MeasureKind.None : kind;
        }

        public void SetBaseMap(string mapId)
        {
            BaseMapInfo info = BaseMapCatalog.Find(mapId);
            if (info == null) return;
            baseMapId = mapId;
            stackBaseMaps.ActiveIndex = Array.IndexOf(BaseMapIds, mapId);
            ApplyTiles(info);
        }

        // The base map is a web tile layer behind the points of interest; MapWinGIS still does zoom, pan, measuring and hit-testing.
        private void ApplyTiles(BaseMapInfo info)
        {
            TileProviders providers = axMap.Tiles.Providers;
            if (providers.get_IndexByProviderId(info.ProviderId) < 0
                && !providers.Add(info.ProviderId, info.Name, info.TileUrl, tkTileProjection.SphericalMercator, BaseMapCatalog.MinZoom, BaseMapCatalog.MaxZoom, string.Empty))
            {
                System.Diagnostics.Debug.WriteLine("MapHost: tile provider rejected: " + providers.get_ErrorMsg(providers.LastErrorCode));
                attribution.Visible = false;
                return;
            }
            axMap.Tiles.ProviderId = info.ProviderId;
            axMap.Tiles.Visible = true;
            attribution.Text = info.Attribution;
            attribution.Visible = true;
            LayoutOverlays();
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
            double halfWidth = 260 * SceneGeoreference.MetresPerCanvasUnit, halfHeight = 146 * SceneGeoreference.MetresPerCanvasUnit;
            view.SetBounds(pin.X - halfWidth, pin.Y - halfHeight, 0, pin.X + halfWidth, pin.Y + halfHeight, 0);
            axMap.Extents = view;
            return true;
        }

        private void PublishCursor(int pixelX, int pixelY)
        {
            double easting = 0, northing = 0, longitude = 0, latitude = 0;
            axMap.PixelToProj(pixelX, pixelY, ref easting, ref northing);
            if (!axMap.ProjToDegrees(easting, northing, ref longitude, ref latitude)) return;
            CursorMoved?.Invoke(this, new MapCursorEventArgs(easting, northing, longitude, latitude));
        }

        // The ActiveX map has no throttled "view changed" notification, so poll the extent (also catches inertia and resize).
        private void PublishViewIfChanged()
        {
            Extents view = axMap.Extents;
            if (view == null || axMap.Width <= 0) return;
            double xMin = view.xMin, xMax = view.xMax, yMin = view.yMin, yMax = view.yMax;
            if (xMin == lastXMin && xMax == lastXMax && yMin == lastYMin && yMax == lastYMax && axMap.Width == lastPixelWidth) return;
            lastXMin = xMin; lastXMax = xMax; lastYMin = yMin; lastYMax = yMax; lastPixelWidth = axMap.Width;

            // Map units are metres (the target CRS, VN-2000 UTM zone 48N, is metric); 96 DPI scaled to the screen.
            double denominator = MapScale.Denominator(xMax - xMin, axMap.Width, 96.0 * Ui.Scale);
            ViewChanged?.Invoke(this, new MapViewChangedEventArgs((xMin + xMax) / 2, (yMin + yMax) / 2, denominator));
        }

        private void BuildScene(bool zoomToFull)
        {
            scene = MapScene.Build(axMap, zoomToFull);
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
            axMap.SendMouseMove = true;
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
            viewTracker.Tick += (s, e) => PublishViewIfChanged();
            viewTracker.Start();
            axMap.MouseUpEvent += (s, e) => axMap_MouseUp(e.x, e.y);
            axMap.MouseMoveEvent += (s, e) => PublishCursor(e.x, e.y);
            BuildScene(zoomToFull: true);
            ApplyTiles(BaseMapCatalog.Find(baseMapId));
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
            attribution.Location = new System.Drawing.Point(Width - attribution.Width - Ui.S(8), Height - attribution.Height - Ui.S(8));
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
