using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AxInterop.MapWinGIS;
using Hue_travel.Models;
using Hue_travel.Services;
using Hue_travel.Services.Area;
using Hue_travel.Services.Online;
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
        private const double CityHalfWidthMetres = 5000;
        private const int ClickSlopPixels = 4;    // a press and release this close together is a click, not a drag
        private const int LeftButton = 1;
        private const int HitRadius = 10;         // design pixels around a marker that count as hitting it

        // Order of the icons in the vertical base map switch.
        private static readonly string[] BaseMapIds = { "roadmap", "satellite", "hybrid", "terrain" };

        private string baseMapId = BaseMapCatalog.DefaultId;
        private readonly Timer viewTracker = new Timer { Interval = 150 };
        private readonly PoiLayerFilter poiLayers = new PoiLayerFilter();
        private double lastXMin, lastXMax, lastYMin, lastYMax;
        private int lastPixelWidth = -1;
        private readonly List<PoiMarker> markers = new List<PoiMarker>();
        private readonly LeftPressFilter pressFilter = new LeftPressFilter();
        private int routeHandle = -1;
        private int pressX = int.MinValue, pressY = int.MinValue;   // MinValue = no press seen since the last release

        /// <summary>Raised after the visible extent (or the control width) changed: new centre and scale.</summary>
        public event EventHandler<MapViewChangedEventArgs> ViewChanged;

        /// <summary>Raised while the pointer moves over the map, with its coordinates.</summary>
        public event EventHandler<MapCursorEventArgs> CursorMoved;

        /// <summary>Raised when the user clicks (not drags) a point of interest on the map.</summary>
        public event EventHandler<Poi> PoiClicked;

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
            Disposed += (s, e) =>
            {
                viewTracker.Dispose();
                Application.RemoveMessageFilter(pressFilter);
            };
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

        /// <summary>Shows the city (about 10 x 5.6 km around the Imperial City); before data is loaded, the 3.2 x 1.8 km demo scene.</summary>
        public void ZoomToFull()
        {
            double halfWidth = markers.Count > 0 ? CityHalfWidthMetres : SceneGeoreference.CanvasWidth * SceneGeoreference.MetresPerCanvasUnit / 2;
            double halfHeight = halfWidth * SceneGeoreference.CanvasHeight / SceneGeoreference.CanvasWidth;
            double centerX = SceneGeoreference.X(SceneGeoreference.CanvasWidth / 2), centerY = SceneGeoreference.Y(SceneGeoreference.CanvasHeight / 2);
            var view = new Extents();
            view.SetBounds(centerX - halfWidth, centerY - halfHeight, 0, centerX + halfWidth, centerY + halfHeight, 0);
            axMap.Extents = view;
        }

        /// <summary>Centres the map on a position (degrees) at street level.</summary>
        public void ZoomTo(double longitude, double latitude)
        {
            double easting = 0, northing = 0;
            if (!axMap.DegreesToProj(longitude, latitude, ref easting, ref northing)) return;
            ZoomToProjected(easting, northing);
        }

        /// <summary>Centres the map on a position in the map's own units (metres) at street level.</summary>
        public void ZoomToProjected(double easting, double northing)
        {
            double halfWidth = 260 * SceneGeoreference.MetresPerCanvasUnit, halfHeight = 146 * SceneGeoreference.MetresPerCanvasUnit;
            var view = new Extents();
            view.SetBounds(easting - halfWidth, northing - halfHeight, 0, easting + halfWidth, northing + halfHeight, 0);
            axMap.Extents = view;
        }

        /// <summary>Registers a points-of-interest data layer (already added to the map) so the left rail can filter it.</summary>
        public void AddPoiLayer(PoiCategory category, int layerHandle)
        {
            poiLayers.Add(category, layerHandle);
            axMap.set_LayerVisible(layerHandle, poiLayers.IsVisible(layerHandle));
        }

        /// <summary>Shows only the POI layers of one category (null = all of them).</summary>
        public void ShowOnlyCategory(PoiCategory? category)
        {
            poiLayers.ShowOnly(category);
            foreach (int handle in poiLayers.Handles) axMap.set_LayerVisible(handle, poiLayers.IsVisible(handle));
            axMap.Redraw();
        }

        /// <summary>
        /// Draws the points of interest: one in-memory MapWinGIS point layer per group (so the rail can show or
        /// hide a whole group), coloured like the rail icons. Points without a projected position are skipped.
        /// </summary>
        public void LoadPois(IReadOnlyList<Poi> pois)
        {
            Extents keptView = CopyOfView();   // MapWinGIS zooms to the new layer when the map was empty
            // New layers go in before the old ones come out: a map with no layers loses its tiles.
            List<int> oldLayers = poiLayers.Handles.ToList();
            poiLayers.Clear();
            markers.Clear();

            foreach (IGrouping<PoiCategory, Poi> group in pois.Where(p => p.X != 0 || p.Y != 0).GroupBy(p => p.Category))
            {
                int handle = AddMarkerLayer(group.Key, group.ToList());
                if (handle < 0) continue;
                markers.AddRange(group.Select(p => new PoiMarker(p, handle)));
                AddPoiLayer(group.Key, handle);
            }
            foreach (int handle in oldLayers) axMap.RemoveLayer(handle);
            if (keptView != null) axMap.Extents = keptView;
            axMap.Redraw();
        }

        private Extents CopyOfView()
        {
            Extents view = axMap.Extents;
            if (view == null || view.xMax <= view.xMin) return null;
            var copy = new Extents();
            copy.SetBounds(view.xMin, view.yMin, 0, view.xMax, view.yMax, 0);
            return copy;
        }

        private int AddMarkerLayer(PoiCategory category, IReadOnlyList<Poi> pois)
        {
            var layer = new Shapefile();
            if (!layer.CreateNew(string.Empty, ShpfileType.SHP_POINT)) return -1;
            foreach (Poi poi in pois)
            {
                var point = new Shape();
                if (!point.Create(ShpfileType.SHP_POINT)) continue;
                point.AddPoint(poi.X, poi.Y);
                layer.EditAddShape(point);
            }

            ShapeDrawingOptions style = layer.DefaultDrawingOptions;
            style.PointShape = tkPointShapeType.ptShapeCircle;
            style.PointSize = Ui.S(PoiMarkerStyle.DiameterPixels);
            style.FillColor = (uint)System.Drawing.ColorTranslator.ToOle(PoiMarkerStyle.ColorOf(category));
            style.LineColor = (uint)System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.White);
            style.LineVisible = true;
            style.LineWidth = 1.5f;

            int handle = axMap.AddLayer(layer, true);
            if (handle >= 0) axMap.set_LayerName(handle, category.ToString());
            return handle;
        }

        // The nearest visible marker within HitRadius of a pixel, or null.
        private Poi FindPoiAt(int pixelX, int pixelY)
        {
            Poi best = null;
            double bestSquared = Math.Pow(Ui.S(HitRadius), 2);
            foreach (PoiMarker marker in markers)
            {
                if (!poiLayers.IsVisible(marker.LayerHandle)) continue;
                double px = 0, py = 0;
                axMap.ProjToPixel(marker.Poi.X, marker.Poi.Y, ref px, ref py);
                double squared = (px - pixelX) * (px - pixelX) + (py - pixelY) * (py - pixelY);
                if (squared > bestSquared) continue;
                bestSquared = squared;
                best = marker.Poi;
            }
            return best;
        }

        // MapWinGIS sends no mouse-down event while panning, so a message filter notes where the left button went down.
        private void NotePress(System.Drawing.Point screen)
        {
            System.Drawing.Point press = axMap.PointToClient(screen);
            pressX = press.X;
            pressY = press.Y;
        }

        private void OnMapMouseUp(int button, int pixelX, int pixelY)
        {
            int downX = pressX, downY = pressY;
            pressX = pressY = int.MinValue;
            if (button != LeftButton || axMap.CursorMode != tkCursorMode.cmPan) return;
            if (downX == int.MinValue) return;   // no press was seen: do not guess
            if (Math.Abs(pixelX - downX) > ClickSlopPixels || Math.Abs(pixelY - downY) > ClickSlopPixels) return;
            Poi hit = FindPoiAt(pixelX, pixelY);
            if (hit != null) PoiClicked?.Invoke(this, hit);
        }

        // Sees every left-button press of the application (the ActiveX map swallows its own mouse-down).
        private sealed class LeftPressFilter : IMessageFilter
        {
            private const int WmLButtonDown = 0x0201;

            public event EventHandler<System.Drawing.Point> Pressed;

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == WmLButtonDown) Pressed?.Invoke(this, Control.MousePosition);
                return false;
            }
        }

        private sealed class PoiMarker
        {
            public PoiMarker(Poi poi, int layerHandle)
            {
                Poi = poi;
                LayerHandle = layerHandle;
            }

            public Poi Poi { get; }
            public int LayerHandle { get; }
        }

        /// <summary>Draws the route (one line per leg, in map metres) above the base map and below the markers, and zooms to it.</summary>
        public void ShowRoute(IReadOnlyList<IReadOnlyList<MapXY>> lines)
        {
            ClearRoute();
            var layer = new Shapefile();
            if (!layer.CreateNew(string.Empty, ShpfileType.SHP_POLYLINE)) return;

            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (IReadOnlyList<MapXY> line in lines)
            {
                if (line.Count < 2) continue;
                var shape = new Shape();
                int part = 0;
                if (!shape.Create(ShpfileType.SHP_POLYLINE) || !shape.InsertPart(0, ref part)) continue;
                foreach (MapXY p in line)
                {
                    shape.AddPoint(p.X, p.Y);
                    minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
                    minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
                }
                layer.EditAddShape(shape);
            }
            if (layer.NumShapes == 0) return;

            ShapeDrawingOptions style = layer.DefaultDrawingOptions;
            style.LineColor = (uint)System.Drawing.ColorTranslator.ToOle(ThemeColors.Primary);
            style.LineWidth = Ui.S(5);
            style.LineVisible = true;
            style.FillVisible = false;
            routeHandle = axMap.AddLayer(layer, true);
            foreach (int handle in poiLayers.Handles) axMap.MoveLayerTop(handle);   // markers stay on top of the line

            double padX = Math.Max((maxX - minX) * 0.15, 300), padY = Math.Max((maxY - minY) * 0.15, 300);
            var view = new Extents();
            view.SetBounds(minX - padX, minY - padY, 0, maxX + padX, maxY + padY, 0);
            axMap.Extents = view;
        }

        public void ClearRoute()
        {
            if (routeHandle < 0) return;
            axMap.RemoveLayer(routeHandle);
            routeHandle = -1;
            axMap.Redraw();
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

        /// <summary>Applies the current base map again, after the catalog got new tile addresses.</summary>
        public void ReloadBaseMap()
        {
            BaseMapInfo info = BaseMapCatalog.Find(baseMapId);
            if (info != null) ApplyTiles(info);
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
            attribution.Visible = !string.IsNullOrEmpty(info.Attribution);
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
            double centerX = (xMin + xMax) / 2, centerY = (yMin + yMax) / 2, longitude = 0, latitude = 0;
            axMap.ProjToDegrees(centerX, centerY, ref longitude, ref latitude);
            ViewChanged?.Invoke(this, new MapViewChangedEventArgs(centerX, centerY, longitude, latitude, denominator));
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
            axMap.SendMouseUp = true;
            axMap.BackColor = System.Drawing.ColorTranslator.FromHtml("#E2E8F0");
            MapCrs.Apply(axMap);
            viewTracker.Tick += (s, e) => PublishViewIfChanged();
            viewTracker.Start();
            axMap.MouseMoveEvent += (s, e) => PublishCursor(e.x, e.y);
            pressFilter.Pressed += (s, screen) => NotePress(screen);
            Application.AddMessageFilter(pressFilter);
            axMap.MouseUpEvent += (s, e) => OnMapMouseUp(e.button, e.x, e.y);
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
