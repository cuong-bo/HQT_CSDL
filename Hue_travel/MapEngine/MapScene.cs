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
    /// The points of interest of the demo as in-memory MapWinGIS point layers, placed on the real map in
    /// VN-2000 UTM metres (see <see cref="SceneGeoreference"/>). Positions are given on the mockup's
    /// 1600x900 canvas with Y flipped (canvas Y grows downwards, map Y upwards).
    /// </summary>
    public static class MapScene
    {
        public const double CanvasWidth = 1600;
        public const double CanvasHeight = 900;

        // Transparent key colour of the pin pictures (never used inside the pin art).
        private static readonly Color PinKey = ColorTranslator.FromHtml("#E8F4FC");

        /// <summary>Replaces every layer of <paramref name="map"/> with the points of interest; the base map itself comes from web tiles.</summary>
        public static SceneIndex Build(AxMap map, bool zoomToFull = true)
        {
            map.RemoveAllLayers();
            MapCrs.Apply(map);
            map.BackColor = ColorTranslator.FromHtml("#E2E8F0");
            var index = new SceneIndex();

            Color sky = ColorTranslator.FromHtml("#0284C7");
            Color slate = ColorTranslator.FromHtml("#1E293B");

            AddPin(map, index, null, "ga-hue", 430, 670, PinKey, new PinSpec { Caption = "Ga Huế (Điểm xuất phát)", Icon = "train", Marker = slate, MarkerSize = 32, IconSize = 18, LabelFill = slate, LabelText = Color.White, LabelRadius = 12, StatusDot = true, CaptionPx = 11 });
            AddPin(map, index, "heritage", "dai-noi", 620, 320, PinKey, new PinSpec { Caption = "Đại Nội - Hoàng Thành Huế", Icon = "account_balance", Order = 1, Marker = sky, MarkerSize = 36, IconSize = 20, LabelFill = sky, LabelText = Color.White, BadgeFill = ThemeColors.Amber400, BadgeText = slate, LabelRadius = 8, CaptionPx = 12, CaptionBold = true });
            AddPin(map, index, "heritage", "thien-mu", 300, 480, PinKey, new PinSpec { Caption = "Chùa Thiên Mụ", Icon = "temple_buddhist", Order = 2, Marker = sky, MarkerSize = 32, IconSize = 18, LabelFill = Color.White, LabelBorder = ThemeColors.Sky100, LabelText = slate, BadgeFill = sky, BadgeText = Color.White, LabelRadius = 4, CaptionPx = 12 });
            AddPin(map, index, "food", "bun-bo-mu-roi", 890, 560, PinKey, new PinSpec { Caption = "Quán Bún Bò Mụ Rơi", Icon = "restaurant", Order = 3, Marker = ThemeColors.Amber600, MarkerSize = 32, IconSize = 18, LabelFill = Color.White, LabelBorder = ThemeColors.Amber100, LabelText = slate, BadgeFill = ThemeColors.Amber600, BadgeText = Color.White, LabelRadius = 4, CaptionPx = 12 });

            if (zoomToFull) map.Extents = FullExtents();
            return index;
        }

        /// <summary>The whole demo area: about 3.2 x 1.8 km around the Imperial City.</summary>
        public static Extents FullExtents()
        {
            var extents = new Extents();
            extents.SetBounds(SceneGeoreference.X(0), SceneGeoreference.Y(CanvasHeight), 0, SceneGeoreference.X(CanvasWidth), SceneGeoreference.Y(0), 0);
            return extents;
        }

        // Canvas points are Y-flipped (see Flip); layers are stored in VN-2000 / UTM metres.
        private static double MapX(PointF p) => SceneGeoreference.X(p.X);

        private static double MapY(PointF p) => SceneGeoreference.Y(CanvasHeight - p.Y);

        private static uint Ole(Color color) => (uint)ColorTranslator.ToOle(color);

        // The mockup's Y axis points down; MapWinGIS's points up.
        private static PointF Flip(double x, double y) => new PointF((float)x, (float)(CanvasHeight - y));

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
