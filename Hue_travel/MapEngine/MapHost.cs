using System;
using System.Windows.Forms;
using AxInterop.MapWinGIS;
using Hue_travel.Services;
using Hue_travel.Services.Area;
using Hue_travel.Theme;
using Interop.MapWinGIS;

namespace Hue_travel.MapEngine
{
    /// <summary>The map: MapWinGIS does the tiles, zoom, pan, measuring and coordinates; overlay controls sit on top.</summary>
    public partial class MapHost : UserControl
    {
        private const int RightMargin = 16;   // right-4
        private const int ColumnTop = 64;     // top-16
        private const int StackGap = 6;       // gap-1.5

        // Order of the icons in the vertical base map switch.
        private static readonly string[] BaseMapIds = { "roadmap", "satellite", "hybrid", "terrain" };

        private string baseMapId = BaseMapCatalog.DefaultId;
        private readonly Timer viewTracker = new Timer { Interval = 150 };
        private double lastXMin, lastXMax, lastYMin, lastYMax;
        private int lastPixelWidth = -1;

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
            attribution.BringToFront();
            LayoutOverlays();
            Resize += (s, e) => LayoutOverlays();
            new ToolTip().SetToolTip(compass, "Khôi phục hướng Bắc");
            Disposed += (s, e) => viewTracker.Dispose();
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

        /// <summary>Shows the whole demo area (about 3.2 x 1.8 km around the Imperial City).</summary>
        public void ZoomToFull()
        {
            var view = new Extents();
            view.SetBounds(
                SceneGeoreference.X(0), SceneGeoreference.Y(SceneGeoreference.CanvasHeight), 0,
                SceneGeoreference.X(SceneGeoreference.CanvasWidth), SceneGeoreference.Y(0), 0);
            axMap.Extents = view;
        }

        /// <summary>Centres the map on a position (VN-2000 UTM metres) at street level.</summary>
        public void ZoomTo(double easting, double northing)
        {
            double halfWidth = 260 * SceneGeoreference.MetresPerCanvasUnit, halfHeight = 146 * SceneGeoreference.MetresPerCanvasUnit;
            var view = new Extents();
            view.SetBounds(easting - halfWidth, northing - halfHeight, 0, easting + halfWidth, northing + halfHeight, 0);
            axMap.Extents = view;
        }

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

        // The base map is a web tile layer; MapWinGIS still does zoom, pan, measuring and the coordinate read-out.
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
            axMap.Redraw();
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

        private void InitializeMap()
        {
            axMap.ShowCoordinates = tkCoordinatesDisplay.cdmNone;
            axMap.ScalebarVisible = false;
            axMap.ShowRedrawTime = false;
            axMap.ShowVersionNumber = false;
            axMap.CursorMode = tkCursorMode.cmPan;
            axMap.InertiaOnPanning = tkCustomState.csTrue;   // the map glides after a drag
            axMap.SendMouseMove = true;
            axMap.BackColor = System.Drawing.ColorTranslator.FromHtml("#E2E8F0");
            MapCrs.Apply(axMap);
            viewTracker.Tick += (s, e) => PublishViewIfChanged();
            viewTracker.Start();
            axMap.MouseMoveEvent += (s, e) => PublishCursor(e.x, e.y);
            ApplyTiles(BaseMapCatalog.Find(baseMapId));
            ZoomToFull();
        }

        // Anchors the overlays like the mockup: controls in the top-right column, credit line bottom-right.
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            LayoutOverlays(); // Control.Scale grows children after the Resize handler ran
        }

        private void LayoutOverlays()
        {
            int x = Width - Ui.S(RightMargin) - stackBaseMaps.Width;
            stackBaseMaps.Location = new System.Drawing.Point(x, Ui.S(ColumnTop));
            compass.Location = new System.Drawing.Point(x + (stackBaseMaps.Width - compass.Width) / 2, stackBaseMaps.Bottom + Ui.S(StackGap + 4));
            stackZoom.Location = new System.Drawing.Point(x, compass.Bottom + Ui.S(StackGap));
            attribution.Location = new System.Drawing.Point(Width - attribution.Width - Ui.S(8), Height - attribution.Height - Ui.S(8));
        }

        private void stackBaseMaps_ItemClicked(object sender, int index) => SetBaseMap(BaseMapIds[index]);

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
