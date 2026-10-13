using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AxInterop.MapWinGIS;
using Hue_travel.Models;
using Hue_travel.Services;
using Hue_travel.Services.Area;
using Hue_travel.Services.Online;
using Hue_travel.Services.Routing;
using Hue_travel.Theme;
using Interop.MapWinGIS;

namespace Hue_travel.MapEngine
{
    /// <summary>The map: MapWinGIS does the tiles, zoom, pan, measuring and coordinates; overlay controls sit on top.</summary>
    public partial class MapHost : UserControl
    {
        private const int RightMargin = 16;   // right-4
        private const int ColumnTop = 16;     // top-4 of the map, under the filter bar
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
        private RouteDrawing routeDrawing;
        private bool pickingPoint;
        private int placeholderHandle = -1;
        private HighlightRing highlightRing;
        private Poi highlighted;
        private const int HighlightDesignPixels = 54;
        private IReadOnlyList<Poi> lastPois = new List<Poi>();
        private readonly HashSet<string> hiddenPoiIds = new HashSet<string>();
        private const int MarkerDesignPixels = 34;               // the round badge on the map
        private const double LabelMaxScaleDenominator = 45000;   // names appear once zoomed in to about 1:45 000
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
            stackTools.BringToFront();
            stackZoom.BringToFront();
            compass.BringToFront();
            stackBaseMaps.BringToFront();
            attribution.BringToFront();
            InitTripChip();
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
            lastPois = pois;
            Extents keptView = CopyOfView();   // MapWinGIS zooms to the new layer when the map was empty
            // New layers go in before the old ones come out: a map with no layers loses its tiles.
            List<int> oldLayers = poiLayers.Handles.ToList();
            poiLayers.Clear();
            markers.Clear();

            foreach (IGrouping<PoiCategory, Poi> group in pois.Where(p => (p.X != 0 || p.Y != 0) && !hiddenPoiIds.Contains(p.Id)).GroupBy(p => p.Category))
            {
                int handle = AddMarkerLayer(group.Key, group.ToList());
                if (handle < 0) continue;
                markers.AddRange(group.Select(p => new PoiMarker(p, handle)));
                AddPoiLayer(group.Key, handle);
            }
            // With every place switched off no layer is left, and a map without layers loses its tiles: keep one empty layer.
            int oldPlaceholder = placeholderHandle;
            placeholderHandle = poiLayers.Handles.Count == 0 ? AddEmptyLayer() : -1;
            if (oldPlaceholder >= 0) axMap.RemoveLayer(oldPlaceholder);

            foreach (int handle in oldLayers) axMap.RemoveLayer(handle);
            DrawHighlight();
            if (keptView != null) axMap.Extents = keptView;
            axMap.Redraw();
        }

        private int AddEmptyLayer()
        {
            var layer = new Shapefile();
            return layer.CreateNew(string.Empty, ShpfileType.SHP_POINT) ? axMap.AddLayer(layer, false) : -1;
        }

        /// <summary>Rings the place the map was zoomed to or the card shows, until <see cref="ClearHighlight"/>.</summary>
        public void HighlightPoi(Poi poi)
        {
            highlighted = poi;
            DrawHighlight();
            axMap.Redraw();
        }

        public void ClearHighlight()
        {
            if (highlighted == null) return;
            highlighted = null;
            DrawHighlight();
            axMap.Redraw();
        }

        // The pulse is a click-through window above the map (a map layer with a single point is not drawn by the engine); it follows the place.
        private void DrawHighlight()
        {
            bool show = highlighted != null && (highlighted.X != 0 || highlighted.Y != 0) && !hiddenPoiIds.Contains(highlighted.Id);
            if (show && highlightRing == null)
            {
                highlightRing = new HighlightRing(Ui.S(HighlightDesignPixels * 2), Ui.S(MarkerDesignPixels), HighlightCenterOnScreen);
                Disposed += (s, e) => highlightRing.Dispose();
            }
            if (highlightRing != null) highlightRing.Run(show, FindForm());
        }

        // Screen position of the highlighted marker's centre; null when it is outside the map window.
        private System.Drawing.Point? HighlightCenterOnScreen()
        {
            if (highlighted == null || !IsHandleCreated) return null;
            double px = 0, py = 0;
            axMap.ProjToPixel(highlighted.X, highlighted.Y, ref px, ref py);
            // The marker picture stands on its point (its bottom centre is the place), so its centre is half a marker above it.
            var local = new System.Drawing.Point((int)Math.Round(px), (int)Math.Round(py) - Ui.S(MarkerDesignPixels) / 2 - 2);
            return ClientRectangle.Contains(local) ? PointToScreen(local) : (System.Drawing.Point?)null;
        }

        /// <summary>Places that are switched off in the layer list: they are left out of the map until switched on again.</summary>
        public void SetHiddenPois(IEnumerable<string> ids)
        {
            hiddenPoiIds.Clear();
            foreach (string id in ids) hiddenPoiIds.Add(id);
            LoadPois(lastPois);
        }

        /// <summary>The map window in map metres; false before the map has a view.</summary>
        public bool TryGetViewBounds(out double xMin, out double yMin, out double xMax, out double yMax)
        {
            Extents view = axMap.Extents;
            xMin = yMin = xMax = yMax = 0;
            if (view == null || view.xMax <= view.xMin) return false;
            xMin = view.xMin; yMin = view.yMin; xMax = view.xMax; yMax = view.yMax;
            return true;
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
            int nameField = layer.EditAddField("name", FieldType.STRING_FIELD, 0, 100);
            foreach (Poi poi in pois)
            {
                var point = new Shape();
                if (!point.Create(ShpfileType.SHP_POINT)) continue;
                point.AddPoint(poi.X, poi.Y);
                int index = layer.EditAddShape(point);
                if (index >= 0 && nameField >= 0) layer.EditCellValue(nameField, index, poi.Name ?? string.Empty);
            }

            ShapeDrawingOptions style = layer.DefaultDrawingOptions;
            if (!UsePictureMarker(style, category))
            {
                // Fallback when the picture cannot be loaded: a plain coloured disc.
                style.PointType = tkPointSymbolType.ptSymbolStandard;
                style.PointShape = tkPointShapeType.ptShapeCircle;
                style.PointSize = Ui.S(PoiMarkerStyle.DiameterPixels);
                style.FillColor = (uint)System.Drawing.ColorTranslator.ToOle(PoiMarkerStyle.ColorOf(category));
                style.LineColor = (uint)System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.White);
                style.LineVisible = true;
                style.LineWidth = 1.5f;
            }
            if (nameField >= 0) StyleNameLabels(layer, nameField);

            int handle = axMap.AddLayer(layer, true);
            if (handle >= 0) axMap.set_LayerName(handle, category.ToString());
            return handle;
        }

        // The badge of the group as the point symbol (a picture drawn at the screen's own resolution, so it stays sharp).
        private static bool UsePictureMarker(ShapeDrawingOptions style, PoiCategory category) =>
            UsePicture(style, PoiMarkerIcons.FilePath(category, Ui.S(MarkerDesignPixels)));

        // A picture file as the point symbol.
        private static bool UsePicture(ShapeDrawingOptions style, string path)
        {
            if (path == null) return false;
            try
            {
                var picture = new Interop.MapWinGIS.Image();
                if (!picture.Open(path, ImageType.PNG_FILE, true, null)) return false;
                style.PointType = tkPointSymbolType.ptSymbolPicture;
                style.Picture = picture;
                style.PictureScaleX = 1.0;
                style.PictureScaleY = 1.0;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("MapHost.UsePictureMarker: " + ex.Message);
                return false;
            }
        }

        // Small name under each marker: dark text with a white halo, kept apart from each other, shown once zoomed in.
        private static void StyleNameLabels(Shapefile layer, int nameField)
        {
            layer.GenerateLabels(nameField, tkLabelPositioning.lpCenter, false);
            Labels labels = layer.Labels;
            labels.FontName = "Segoe UI";
            labels.FontSize = 9;
            labels.FontBold = false;
            labels.FrameVisible = false;   // dark text with a white halo, no box
            labels.FontColor = (uint)System.Drawing.ColorTranslator.ToOle(ThemeColors.Slate800);
            labels.HaloVisible = true;
            labels.HaloColor = (uint)System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.White);
            labels.HaloSize = 3;
            labels.Alignment = tkLabelAlignment.laTopCenter;
            labels.OffsetY = Ui.S(MarkerDesignPixels) / 2 + Ui.S(1);
            labels.AvoidCollisions = true;
            labels.DynamicVisibility = true;
            labels.MaxVisibleScale = LabelMaxScaleDenominator;
        }

        private int mapCursorState = -1;   // 0 arrow, 2 over a place, 3 the tool's own cursor

        // The pointers of the system: the arrow over the map, the pointing hand over a place you can click.
        private void UpdateMapCursor(int pixelX, int pixelY)
        {
            int state = pickingPoint ? 4 : axMap.CursorMode != tkCursorMode.cmPan ? 3 : FindPoiAt(pixelX, pixelY) != null ? 2 : 0;
            if (state == mapCursorState) return;
            mapCursorState = state;
            axMap.MapCursor = state == 3 ? tkCursor.crsrMapDefault : state == 2 ? tkCursor.crsrHand : state == 4 ? tkCursor.crsrCross : tkCursor.crsrArrow;
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
            if (button != LeftButton || axMap.CursorMode != tkCursorMode.cmPan) { UpdateMapCursor(pixelX, pixelY); return; }
            if (downX == int.MinValue) return;   // no press was seen: do not guess
            if (Math.Abs(pixelX - downX) > ClickSlopPixels || Math.Abs(pixelY - downY) > ClickSlopPixels) return;
            if (pickingPoint)
            {
                double x = 0, y = 0;
                axMap.PixelToProj(pixelX, pixelY, ref x, ref y);
                CancelPickPoint();
                PointPicked?.Invoke(this, new MapXY(x, y));
                return;
            }
            UpdateMapCursor(pixelX, pixelY);
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

        /// <summary>A position in degrees as map metres, or null when the map cannot convert it.</summary>
        public MapXY? TryProject(double longitude, double latitude)
        {
            double x = 0, y = 0;
            return axMap.DegreesToProj(longitude, latitude, ref x, ref y) ? new MapXY(x, y) : (MapXY?)null;
        }

        /// <summary>A map position as degrees.</summary>
        public bool TryToDegrees(MapXY point, out double longitude, out double latitude)
        {
            longitude = latitude = 0;
            return axMap.ProjToDegrees(point.X, point.Y, ref longitude, ref latitude);
        }

        /// <summary>Raised when the car enters another leg of the trip (the index of the leg).</summary>
        public event EventHandler<int> TripLegChanged;

        /// <summary>Raised when the car starts or stops moving.</summary>
        public event EventHandler TripPlayStateChanged;

        /// <summary>Raised when the car reaches the end of the trip.</summary>
        public event EventHandler TripFinished;

        /// <summary>Raised with the clicked position (map metres) while <see cref="BeginPickPoint"/> is waiting for one.</summary>
        public event EventHandler<MapXY> PointPicked;

        public bool IsPickingPoint => pickingPoint;

        public bool TripPlaying => routeDrawing != null && routeDrawing.IsPlaying;

        /// <summary>Waits for the next click on the map and raises <see cref="PointPicked"/> with it (a crosshair shows meanwhile).</summary>
        public void BeginPickPoint()
        {
            pickingPoint = true;
            mapCursorState = -1;
            UpdateMapCursor(0, 0);
        }

        public void CancelPickPoint()
        {
            pickingPoint = false;
            mapCursorState = -1;
            UpdateMapCursor(0, 0);
        }

        /// <summary>Draws the trip (a coloured line per leg, numbered stops) with a car that drives along it, and zooms to it.</summary>
        public void ShowTrip(TripPlan plan, bool play)
        {
            if (plan == null || plan.Legs.Count == 0) return;
            EnsureRouteDrawing().Show(plan, play);
        }

        public void ClearRoute()
        {
            if (routeDrawing != null) routeDrawing.Clear();
        }

        public void PlayTrip() { if (routeDrawing != null) routeDrawing.Play(); }

        public void PauseTrip() { if (routeDrawing != null) routeDrawing.Pause(); }

        public void ReplayTrip() { if (routeDrawing != null) routeDrawing.Replay(); }

        public void ZoomToTripLeg(int leg) { if (routeDrawing != null) routeDrawing.ZoomToLeg(leg); }

        public void ZoomToTrip() { if (routeDrawing != null) routeDrawing.ZoomToTrip(); }

        private RouteDrawing EnsureRouteDrawing()
        {
            if (routeDrawing != null) return routeDrawing;
            routeDrawing = new RouteDrawing(axMap, LowestPlacePosition);
            routeDrawing.LegChanged += (s, leg) => TripLegChanged?.Invoke(this, leg);
            routeDrawing.PlayStateChanged += (s, e) => TripPlayStateChanged?.Invoke(this, EventArgs.Empty);
            routeDrawing.Finished += (s, e) => TripFinished?.Invoke(this, EventArgs.Empty);
            Disposed += (s, e) => routeDrawing.Dispose();
            return routeDrawing;
        }

        // The layer position of the lowest place layer: the route line goes in just below it, so markers stay on top.
        private int LowestPlacePosition()
        {
            int lowest = axMap.NumLayers;
            foreach (int handle in poiLayers.Handles) lowest = Math.Min(lowest, axMap.get_LayerPosition(handle));
            return lowest;
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
            stackTools.ActiveIndex = stop ? -1 : (kind == MeasureKind.Area ? 2 : 1);
            return stop ? MeasureKind.None : kind;
        }

        public void SetBaseMap(string mapId)
        {
            if (BaseMapCatalog.UseLocalData) return;
            BaseMapInfo info = BaseMapCatalog.Find(mapId);
            if (info == null) return;
            baseMapId = mapId;
            stackBaseMaps.ActiveIndex = Array.IndexOf(BaseMapIds, mapId);
            ApplyTiles(info);
        }

        /// <summary>Applies the current base map again, after the catalog got new tile addresses.</summary>
        public void ReloadBaseMap()
        {
            if (BaseMapCatalog.UseLocalData) return;
            BaseMapInfo info = BaseMapCatalog.Find(baseMapId);
            if (info != null) ApplyTiles(info);
        }

        // The base map is drawn from the project's own Shapefiles (the web tiles are switched off); the place layers are added above it.
        private void ApplyLocalBase()
        {
            axMap.Tiles.Visible = false;
            axMap.TileProvider = tkTileProvider.ProviderNone;
            LocalBaseMap.Add(axMap);
            stackBaseMaps.Visible = false;
            attribution.Visible = false;
            LayoutOverlays();
            axMap.Redraw();
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
            if (BaseMapCatalog.UseLocalData && LocalBaseMap.ApplyScale(denominator)) axMap.Redraw();   // small roads leave the map as it is zoomed out
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
            axMap.MouseMoveEvent += (s, e) => { PublishCursor(e.x, e.y); UpdateMapCursor(e.x, e.y); };
            pressFilter.Pressed += (s, screen) => NotePress(screen);
            Application.AddMessageFilter(pressFilter);
            axMap.MouseUpEvent += (s, e) => OnMapMouseUp(e.button, e.x, e.y);
            if (BaseMapCatalog.UseLocalData) ApplyLocalBase();
            else ApplyTiles(BaseMapCatalog.Find(baseMapId));
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
            stackTools.Location = new System.Drawing.Point(x, stackZoom.Bottom + Ui.S(StackGap + 4));
            attribution.Location = new System.Drawing.Point(Width - attribution.Width - Ui.S(8), Height - attribution.Height - Ui.S(8));
            int chipWidth = Ui.S(206), chipHeight = Ui.S(46);
            // Centred on the window (like the result panel it stands for), not on the map, which starts to the right of the rail.
            Form window = FindForm();
            int centre = window != null && window.IsHandleCreated ? PointToClient(window.PointToScreen(new System.Drawing.Point(window.ClientSize.Width / 2, 0))).X : Width / 2;
            tripChip.SetBounds(centre - chipWidth / 2, Height - chipHeight - Ui.S(22), chipWidth, chipHeight);
        }

        // ------------------------------------------------------------------ the button the folded route result leaves on the map

        private readonly IconButton tripChip = new IconButton();

        /// <summary>Raised when the folded route result's button is pressed.</summary>
        public event EventHandler TripChipClicked;

        private void InitTripChip()
        {
            tripChip.AutoWidth = false;
            tripChip.Text = "Kết quả tìm đường";
            tripChip.IconName = "route";
            tripChip.IconSize = 18;
            tripChip.TrailingIconName = "expand_less";
            tripChip.TrailingIconSize = 18;
            tripChip.Gap = 8;
            tripChip.Font = ThemeFonts.SemiBold(13);
            tripChip.Radius = 23;
            tripChip.ForeColor = System.Drawing.Color.White;
            tripChip.IconColor = ThemeColors.Sky300;
            tripChip.TrailingIconColor = ThemeColors.Slate300;
            tripChip.FillColor = ThemeColors.Slate800;
            tripChip.HoverFillColor = ThemeColors.Slate700;
            tripChip.Elevated = true;
            tripChip.Visible = false;
            tripChip.Click += (s, e) => TripChipClicked?.Invoke(this, EventArgs.Empty);
            Controls.Add(tripChip);
            tripChip.BringToFront();
        }

        /// <summary>Shows or hides the button that reopens the folded route result.</summary>
        public void ShowTripChip(bool visible)
        {
            tripChip.Visible = visible;
            if (visible) tripChip.BringToFront();
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
                stackTools.ActiveIndex = -1;
            }
        }

        // Whole-city view and the two measuring tools (a second click on a measuring tool goes back to panning).
        private void stackTools_ItemClicked(object sender, int index)
        {
            if (index == 0) ZoomToFull();
            else ToggleMeasure(index == 2 ? MeasureKind.Area : MeasureKind.Distance);
        }

        private void compass_Click(object sender, EventArgs e) => ZoomToFull();
    }
}
