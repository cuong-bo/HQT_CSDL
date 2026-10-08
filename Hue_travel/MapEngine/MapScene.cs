using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using AxInterop.MapWinGIS;
using Hue_travel.Services.Area;
using Hue_travel.Theme;
using Interop.MapWinGIS;

namespace Hue_travel.MapEngine
{
    /// <summary>Colours that change between the light, satellite and night base maps.</summary>
    public sealed class MapPalette
    {
        public Color Background { get; set; }
        public Color River { get; set; }
        public Color RiverEdge { get; set; }
        public Color Island { get; set; }
        public Color IslandEdge { get; set; }
        public Color Citadel { get; set; }
        public Color CitadelEdge { get; set; }
        public Color RoadCasing { get; set; }
        public Color Road { get; set; }
        public Color RiverLabel { get; set; }
        public Color PlaceLabel { get; set; }

        public static MapPalette For(string baseMapId)
        {
            switch (baseMapId)
            {
                case "night":
                    return new MapPalette
                    {
                        Background = ColorTranslator.FromHtml("#0F172A"), River = ColorTranslator.FromHtml("#1E3A8A"), RiverEdge = ColorTranslator.FromHtml("#1D4ED8"),
                        Island = ColorTranslator.FromHtml("#14532D"), IslandEdge = ColorTranslator.FromHtml("#166534"),
                        Citadel = ColorTranslator.FromHtml("#713F12"), CitadelEdge = ColorTranslator.FromHtml("#CA8A04"),
                        RoadCasing = ColorTranslator.FromHtml("#334155"), Road = ColorTranslator.FromHtml("#64748B"),
                        RiverLabel = ColorTranslator.FromHtml("#93C5FD"), PlaceLabel = ColorTranslator.FromHtml("#E2E8F0"),
                    };
                default:
                    return new MapPalette
                    {
                        Background = ColorTranslator.FromHtml("#E8F4FC"), River = ColorTranslator.FromHtml("#7DD3FC"), RiverEdge = ColorTranslator.FromHtml("#BAE6FD"),
                        Island = ColorTranslator.FromHtml("#DCFCE7"), IslandEdge = ColorTranslator.FromHtml("#86EFAC"),
                        Citadel = ColorTranslator.FromHtml("#FEF08A"), CitadelEdge = ColorTranslator.FromHtml("#FACC15"),
                        RoadCasing = Color.White, Road = ColorTranslator.FromHtml("#CBD5E1"),
                        RiverLabel = ColorTranslator.FromHtml("#0369A1"), PlaceLabel = ColorTranslator.FromHtml("#334155"),
                    };
            }
        }
    }

    /// <summary>Where a pin sits in map units (used for click hit-testing and zoom-to-pin).</summary>
    public sealed class PinLocation
    {
        public string Key { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }

    /// <summary>Layer handles per toggle group ("heritage", "food", "roads", "river", ...) and the pin positions of the built scene.</summary>
    public sealed class SceneIndex
    {
        public Dictionary<string, List<int>> Groups { get; } = new Dictionary<string, List<int>>();
        public List<PinLocation> Pins { get; } = new List<PinLocation>();

        public void Add(string group, int handle)
        {
            List<int> handles;
            if (!Groups.TryGetValue(group, out handles)) Groups[group] = handles = new List<int>();
            handles.Add(handle);
        }
    }

    /// <summary>
    /// Builds the Huế sightseeing map shown in GiaoDien/screen.png as in-memory MapWinGIS
    /// shapefiles. Coordinates are the mockup's 1600x900 SVG canvas with Y flipped
    /// (screen Y grows downwards, map Y upwards).
    /// </summary>
    public static class MapScene
    {
        public const double CanvasWidth = 1600;
        public const double CanvasHeight = 900;

        private static readonly PointF[] RiverOutline = BuildRiverOutline();

        /// <summary>Replaces every layer of <paramref name="map"/> with the scene drawn in <paramref name="palette"/>.</summary>
        public static SceneIndex Build(AxMap map, MapPalette palette, bool zoomToFull = true, bool includeDecor = true)
        {
            map.RemoveAllLayers();
            MapCrs.Apply(map);
            map.BackColor = palette.Background;
            var index = new SceneIndex();

            if (includeDecor) AddDecor(map, palette, index);

            Color bg = palette.Background;
            Color sky = ColorTranslator.FromHtml("#0284C7");
            Color slate = ColorTranslator.FromHtml("#1E293B");

            AddPin(map, index, null, "ga-hue", 430, 670, bg, new PinSpec { Caption = "Ga Huế (Điểm xuất phát)", Icon = "train", Marker = slate, MarkerSize = 32, IconSize = 18, LabelFill = slate, LabelText = Color.White, LabelRadius = 12, StatusDot = true, CaptionPx = 11 });
            AddPin(map, index, "heritage", "dai-noi", 620, 320, bg, new PinSpec { Caption = "Đại Nội - Hoàng Thành Huế", Icon = "account_balance", Order = 1, Marker = sky, MarkerSize = 36, IconSize = 20, LabelFill = sky, LabelText = Color.White, BadgeFill = ThemeColors.Amber400, BadgeText = slate, LabelRadius = 8, CaptionPx = 12, CaptionBold = true });
            AddPin(map, index, "heritage", "thien-mu", 300, 480, bg, new PinSpec { Caption = "Chùa Thiên Mụ", Icon = "temple_buddhist", Order = 2, Marker = sky, MarkerSize = 32, IconSize = 18, LabelFill = Color.White, LabelBorder = ThemeColors.Sky100, LabelText = slate, BadgeFill = sky, BadgeText = Color.White, LabelRadius = 4, CaptionPx = 12 });
            AddPin(map, index, "food", "bun-bo-mu-roi", 890, 560, bg, new PinSpec { Caption = "Quán Bún Bò Mụ Rơi", Icon = "restaurant", Order = 3, Marker = ThemeColors.Amber600, MarkerSize = 32, IconSize = 18, LabelFill = Color.White, LabelBorder = ThemeColors.Amber100, LabelText = slate, BadgeFill = ThemeColors.Amber600, BadgeText = Color.White, LabelRadius = 4, CaptionPx = 12 });

            if (zoomToFull) map.ZoomToMaxExtents();
            return index;
        }

        // The drawn base map: river, citadel, streets, route and their labels (hidden on real tile maps).
        private static void AddDecor(AxMap map, MapPalette palette, SceneIndex index)
        {
            index.Add("river", AddPolygons(map, new[] { RiverOutline }, palette.River, palette.RiverEdge, 1.5f, false));
            index.Add("river", AddPolygons(map, new[] { Poly(1150, 420, 1220, 390, 1290, 392, 1340, 430, 1300, 452, 1230, 455) }, palette.Island, palette.IslandEdge, 1.5f, false));
            index.Add("heritage", AddPolygons(map, new[] { Poly(460, 210, 690, 160, 760, 380, 520, 430) }, palette.Citadel, palette.CitadelEdge, 2.5f, false));
            index.Add("heritage", AddPolygons(map, new[] { Poly(530, 245, 660, 220, 695, 340, 560, 370) }, palette.Citadel, ColorTranslator.FromHtml("#CA8A04"), 2f, true));

            PointF[][] roads =
            {
                Line(280, 200, 960, 90), Line(420, 720, 1180, 580), Line(740, 450, 1150, 260), Line(300, 480, 460, 760),
            };
            index.Add("roads", AddLines(map, roads, palette.RoadCasing, 5f, false));
            index.Add("roads", AddLines(map, roads, palette.Road, 2f, false));

            index.Add("roads", AddLines(map, new[] { Line(500, 580, 550, 690) }, ColorTranslator.FromHtml("#64748B"), 6f, false));
            index.Add("roads", AddLines(map, new[] { Line(720, 465, 770, 585) }, ColorTranslator.FromHtml("#94A3B8"), 6f, false));
            index.Add("roads", AddLines(map, new[] { Line(900, 440, 940, 560) }, ColorTranslator.FromHtml("#0284C7"), 6f, false));

            PointF[][] route =
            {
                Line(430, 670, 525, 640, 730, 460, 620, 320),
                Line(620, 320, 590, 280, 310, 390, 300, 480),
                Line(300, 480, 430, 670, 890, 560),
            };
            index.Add("roads", AddLines(map, route, ColorTranslator.FromHtml("#0284C7"), 5f, false));
            index.Add("roads", AddLines(map, route, Color.White, 2.5f, true));

            index.Add("river", AddLabels(map, palette.RiverLabel, 12, new[] { Tuple.Create("SÔNG HƯƠNG", 680.0, 560.0, -15.0) }));
            index.Add("river", AddLabels(map, ColorTranslator.FromHtml("#15803D"), 9, new[] { Tuple.Create("Cồn Hến", 1235.0, 432.0, 0.0) }));
            index.Add("heritage", AddLabels(map, ColorTranslator.FromHtml("#854D0E"), 10, new[] { Tuple.Create("HOÀNG THÀNH HUẾ", 610.0, 302.0, 0.0) }));
            index.Add("roads", AddLabels(map, palette.PlaceLabel, 8, new[]
            {
                Tuple.Create("Cầu Dã Viên", 470.0, 650.0, -65.0),
                Tuple.Create("Cầu Phú Xuân", 690.0, 535.0, -65.0),
                Tuple.Create("Cầu Trường Tiền", 965.0, 505.0, 70.0),
            }));

        }

        // Canvas points are Y-flipped (see Flip); layers are stored in VN-2000 / UTM metres.
        private static double MapX(PointF p) => SceneGeoreference.X(p.X);

        private static double MapY(PointF p) => SceneGeoreference.Y(CanvasHeight - p.Y);

        private static uint Ole(Color color) => (uint)ColorTranslator.ToOle(color);

        // The mockup's Y axis points down; MapWinGIS's points up.
        private static PointF Flip(double x, double y) => new PointF((float)x, (float)(CanvasHeight - y));

        private static PointF[] Line(params double[] xy)
        {
            var points = new PointF[xy.Length / 2];
            for (int i = 0; i < points.Length; i++) points[i] = Flip(xy[2 * i], xy[2 * i + 1]);
            return points;
        }

        private static PointF[] Poly(params double[] xy) => Line(xy);

        private static PointF[] Cubic(PointF a, PointF b, PointF c, PointF d, int steps)
        {
            var result = new PointF[steps];
            for (int i = 1; i <= steps; i++)
            {
                double t = i / (double)steps, u = 1 - t;
                result[i - 1] = new PointF(
                    (float)(u * u * u * a.X + 3 * u * u * t * b.X + 3 * u * t * t * c.X + t * t * t * d.X),
                    (float)(u * u * u * a.Y + 3 * u * u * t * b.Y + 3 * u * t * t * c.Y + t * t * t * d.Y));
            }
            return result;
        }

        // The river path from the mockup's SVG: two banks of cubic Béziers joined at the right edge.
        private static PointF[] BuildRiverOutline()
        {
            var pts = new List<PointF>();
            PointF cur = Flip(-50, 780);
            pts.Add(cur);
            double[][] top =
            {
                new double[] { 220, 740, 360, 620, 520, 540 },
                new double[] { 720, 440, 890, 490, 1080, 390 },
                new double[] { 1260, 300, 1420, 320, 1650, 210 },
            };
            foreach (double[] s in top)
            {
                pts.AddRange(Cubic(cur, Flip(s[0], s[1]), Flip(s[2], s[3]), Flip(s[4], s[5]), 16));
                cur = Flip(s[4], s[5]);
            }
            cur = Flip(1650, 310);
            pts.Add(cur);
            double[][] bottom =
            {
                new double[] { 1440, 410, 1280, 390, 1100, 480 },
                new double[] { 910, 580, 740, 530, 540, 640 },
                new double[] { 370, 730, 230, 830, -50, 880 },
            };
            foreach (double[] s in bottom)
            {
                pts.AddRange(Cubic(cur, Flip(s[0], s[1]), Flip(s[2], s[3]), Flip(s[4], s[5]), 16));
                cur = Flip(s[4], s[5]);
            }
            return pts.ToArray();
        }

        private static double SignedArea(PointF[] ring)
        {
            double sum = 0;
            for (int i = 0; i < ring.Length; i++)
            {
                PointF a = ring[i], b = ring[(i + 1) % ring.Length];
                sum += (double)a.X * b.Y - (double)b.X * a.Y;
            }
            return sum / 2;
        }

        private static Shape CreatePolygon(PointF[] ring)
        {
            // Shapefile outer rings run clockwise (negative signed area) and must be closed.
            PointF[] ordered = (PointF[])ring.Clone();
            if (SignedArea(ordered) > 0) Array.Reverse(ordered);

            var shape = new Shape();
            shape.Create(ShpfileType.SHP_POLYGON);
            foreach (PointF p in ordered) shape.AddPoint(MapX(p), MapY(p));
            shape.AddPoint(MapX(ordered[0]), MapY(ordered[0]));
            return shape;
        }

        private static Shape CreatePolyline(PointF[] points)
        {
            var shape = new Shape();
            shape.Create(ShpfileType.SHP_POLYLINE);
            foreach (PointF p in points) shape.AddPoint(MapX(p), MapY(p));
            return shape;
        }

        private static Shape CreatePoint(PointF p)
        {
            var shape = new Shape();
            shape.Create(ShpfileType.SHP_POINT);
            shape.AddPoint(MapX(p), MapY(p));
            return shape;
        }

        private static Shapefile NewShapefile(ShpfileType type)
        {
            var sf = new Shapefile();
            sf.CreateNew("", type);
            return sf;
        }

        private static void Insert(Shapefile sf, Shape shape)
        {
            int index = sf.NumShapes;
            sf.EditInsertShape(shape, ref index);
        }

        private static int AddPolygons(AxMap map, IEnumerable<PointF[]> rings, Color fill, Color edge, float edgeWidth, bool dashed)
        {
            Shapefile sf = NewShapefile(ShpfileType.SHP_POLYGON);
            foreach (PointF[] ring in rings) Insert(sf, CreatePolygon(ring));
            sf.DefaultDrawingOptions.FillColor = Ole(fill);
            sf.DefaultDrawingOptions.LineColor = Ole(edge);
            sf.DefaultDrawingOptions.LineWidth = edgeWidth;
            if (dashed) sf.DefaultDrawingOptions.LineStipple = tkDashStyle.dsDash;
            return map.AddLayer(sf, true);
        }

        private static int AddLines(AxMap map, IEnumerable<PointF[]> lines, Color color, float width, bool dashed)
        {
            Shapefile sf = NewShapefile(ShpfileType.SHP_POLYLINE);
            foreach (PointF[] line in lines) Insert(sf, CreatePolyline(line));
            sf.DefaultDrawingOptions.LineColor = Ole(color);
            sf.DefaultDrawingOptions.LineWidth = width;
            if (dashed) sf.DefaultDrawingOptions.LineStipple = tkDashStyle.dsDash;
            return map.AddLayer(sf, true);
        }

        // Text labels hang off an invisible point layer so each style (colour/size) gets its own Labels object.
        private static int AddLabels(AxMap map, Color color, int fontSize, IEnumerable<Tuple<string, double, double, double>> items)
        {
            Shapefile sf = NewShapefile(ShpfileType.SHP_POINT);
            sf.DefaultDrawingOptions.PointSize = 1f;
            sf.DefaultDrawingOptions.FillVisible = false;
            sf.DefaultDrawingOptions.LineVisible = false;
            foreach (var item in items)
            {
                PointF p = Flip(item.Item2, item.Item3);
                Insert(sf, CreatePoint(p));
                sf.Labels.AddLabel(item.Item1, MapX(p), MapY(p), item.Item4, -1, 0, 0);
            }
            sf.Labels.FontName = "Plus Jakarta Sans";
            sf.Labels.FontSize = fontSize;
            sf.Labels.FontBold = true;
            sf.Labels.FontColor = Ole(color);
            sf.Labels.Alignment = tkLabelAlignment.laCenter;
            sf.Labels.AvoidCollisions = false;
            sf.Labels.Visible = true;
            return map.AddLayer(sf, true);
        }

        // A pin is a picture drawn with GDI+ (caption pill, icon marker, stem) so it matches the mockup.
        private static void AddPin(AxMap map, SceneIndex index, string group, string key, double x, double y, Color background, PinSpec spec)
        {
            PointF p = Flip(x, y);
            Shapefile sf = NewShapefile(ShpfileType.SHP_POINT);
            Insert(sf, CreatePoint(p));
            try
            {
                using (Bitmap bitmap = PinRenderer.Render(spec, background))
                {
                    // MapWinGIS reads the GDI picture bottom-up, so flip it to keep text upright.
                    bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    var picture = new Interop.MapWinGIS.Image();
                    picture.GetType().InvokeMember("Picture", BindingFlags.SetProperty, null, picture, new[] { PinRenderer.ToPictureDisp(bitmap) });
                    picture.UseTransparencyColor = true;
                    picture.TransparencyColor = Ole(background);
                    picture.TransparencyColor2 = Ole(background); // colour range is [TransparencyColor, TransparencyColor2]

                    ShapeDrawingOptions options = sf.DefaultDrawingOptions;
                    options.PointType = tkPointSymbolType.ptSymbolPicture;
                    options.Picture = picture;
                    options.AlignPictureByBottom = true;
                    options.PictureScaleX = 1;
                    options.PictureScaleY = 1;
                }
            }
            catch (Exception ex)
            {
                // If the picture cannot be built, fall back to a plain marker with a text caption.
                System.Diagnostics.Debug.WriteLine("MapScene.AddPin: " + ex.Message);
                sf.DefaultDrawingOptions.PointType = tkPointSymbolType.ptSymbolStandard;
                sf.DefaultDrawingOptions.PointShape = tkPointShapeType.ptShapeCircle;
                sf.DefaultDrawingOptions.PointSize = 18f;
                sf.DefaultDrawingOptions.FillColor = Ole(spec.Marker);
                sf.Labels.AddLabel(spec.Caption, MapX(p), MapY(p), 0, -1, 0, 0);
                sf.Labels.Alignment = tkLabelAlignment.laTopCenter;
                sf.Labels.Visible = true;
            }
            int handle = map.AddLayer(sf, true);
            if (group != null) index.Add(group, handle);
            index.Pins.Add(new PinLocation { Key = key, X = MapX(p), Y = MapY(p) });
        }
    }
}
