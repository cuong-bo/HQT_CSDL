using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using AxInterop.MapWinGIS;
using Interop.MapWinGIS;
using Hue_travel.Services.Data;
using Hue_travel.Theme;

namespace Hue_travel.MapEngine
{
    /// <summary>
    /// The base map drawn from the project's own Shapefiles (downloaded from the server) instead of web tiles. From the bottom: the sea, the land of the
    /// neighbouring countries, the provinces, the communes, the water (lakes, rivers) and the roads (a wider casing under a narrower
    /// filling). The names of the sea, the countries and the provinces show when the map is zoomed out, those of the communes when
    /// it is zoomed in. Layers that are only for calculation (the road network's nodes) are left out.
    /// </summary>
    internal static class LocalBaseMap
    {
        private const double ProvinceNamesFromScale = 120000;    // province names show once the map is at 1:120 000 or smaller
        private const double CountryNamesFromScale = 300000;
        private const double CommuneNamesUntilScale = 150000;
        private const double Unlimited = 1e12;

        // Road classes by the "road_type" field of the road network; any other value (footpaths, steps, ...) is drawn thin.
        private static readonly string[] Major =
        {
            "cao_toc", "nhanh_noi_cao_toc", "duong_truc_chinh", "nhanh_noi_duong_truc_chinh", "duong_chinh_cap_1", "nhanh_noi_duong_chinh_cap_1",
        };

        private static readonly string[] Medium =
        {
            "duong_chinh_cap_2", "nhanh_noi_duong_chinh_cap_2", "duong_chinh_cap_3", "nhanh_noi_duong_chinh_cap_3",
        };

        private static readonly string[] Minor =
        {
            "duong_khu_dan_cu", "duong_noi_bo", "duong_chua_phan_cap", "duong_dan_sinh",
            "duong_chuyen_dung", "duong_chuyen_dung_cap_1", "duong_chuyen_dung_cap_2", "duong_chuyen_dung_cap_3", "duong_chuyen_dung_cap_4", "duong_chuyen_dung_cap_5",
        };

        /// <summary>Where the layers are read from: the data downloaded from the server (see <see cref="BaseMapStore"/>).</summary>
        public static string Folder => BaseMapStore.Folder;



        /// <summary>Adds the layers to the map; returns their handles (empty when the files are missing).</summary>
        public static List<int> Add(AxMap map)
        {
            var handles = new List<int>();
            try
            {
                string folder = Folder;
                if (!Directory.Exists(folder)) return handles;

                Shapefile sea = Open(Path.Combine(folder, "NenBien.shp"));
                if (sea != null)
                {
                    StyleArea(sea, Color.FromArgb(207, 232, 245), Color.FromArgb(207, 232, 245), 0f);
                    NameAt(sea, "BIỂN ĐÔNG", 109.3, 16.2, Color.FromArgb(96, 165, 200), Color.FromArgb(207, 232, 245), true);
                    Register(map, sea, handles);
                }

                Shapefile abroad = Open(Path.Combine(folder, "DL_Nc_Ngoai.shp"));
                if (abroad != null)
                {
                    StyleArea(abroad, Color.FromArgb(232, 230, 224), Color.FromArgb(214, 211, 203), 1f);
                    // The country polygons are huge and ragged: the names sit at fixed places inside each country.
                    NameAt(abroad, "LÀO", 105.6, 17.6, Color.FromArgb(120, 113, 108), Color.FromArgb(232, 230, 224), false);
                    NameAt(abroad, "CAMPUCHIA", 104.9, 12.9, Color.FromArgb(120, 113, 108), Color.FromArgb(232, 230, 224), false);
                    NameAt(abroad, "TRUNG QUỐC", 105.4, 24.2, Color.FromArgb(120, 113, 108), Color.FromArgb(232, 230, 224), false);
                    Register(map, abroad, handles);
                }

                Shapefile provinces = BuildProvinces(folder);
                if (provinces != null)
                {
                    StyleArea(provinces, Color.FromArgb(243, 239, 230), Color.FromArgb(167, 139, 250), 1.8f);
                    provinces.DefaultDrawingOptions.LineStipple = tkDashStyle.dsDash;
                    StyleLabels(provinces.Labels, Color.FromArgb(91, 42, 134), Color.FromArgb(243, 239, 230), 11, false, ProvinceNamesFromScale, Unlimited);
                    Register(map, provinces, handles);
                }

                Shapefile communes = Open(Path.Combine(folder, "rgXa.shp"));
                if (communes != null)
                {
                    StyleArea(communes, Color.FromArgb(243, 239, 230), Color.FromArgb(188, 176, 214), 1.2f);
                    AddLabels(communes, "ten_xa", Color.FromArgb(148, 163, 184), Color.FromArgb(243, 239, 230), 10, true, 0, CommuneNamesUntilScale);
                    Register(map, communes, handles);
                }

                Shapefile lakes = Open(Path.Combine(folder, "song_2n.shp"));
                if (lakes != null)
                {
                    StyleArea(lakes, Color.FromArgb(165, 212, 238), Color.FromArgb(140, 196, 228), 1f);
                    Register(map, lakes, handles);
                }

                Shapefile streams = Open(Path.Combine(folder, "song_1n.shp"));
                if (streams != null)
                {
                    ShapeDrawingOptions style = streams.DefaultDrawingOptions;
                    style.LineVisible = true;
                    style.LineColor = Ole(Color.FromArgb(150, 202, 232));
                    style.LineWidth = Ui.S(1.6f);
                    Register(map, streams, handles);
                }

                AddRoads(map, folder, handles);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LocalBaseMap.Add: " + ex.Message);
            }
            return handles;
        }

        private static Shapefile Open(string path)
        {
            if (!File.Exists(path)) return null;
            var shapefile = new Shapefile();
            if (shapefile.Open(path, null)) return shapefile;
            System.Diagnostics.Debug.WriteLine("LocalBaseMap: cannot open " + path);
            return null;
        }

        private static void Register(AxMap map, Shapefile shapefile, List<int> handles)
        {
            int handle = map.AddLayer(shapefile, true);
            if (handle >= 0) handles.Add(handle);
        }

        // ------------------------------------------------------------------ provinces

        // The province file covers the whole country in degrees with a very detailed outline (17 MB). It is read directly, moved to
        // the map's metres and thinned to one point per few hundred metres, then built in memory: this takes a fraction of a second,
        // where letting the map engine reproject the file took about nine seconds. Province outlines are only shown zoomed out, where
        // such a thin-out is not visible; Huế itself is drawn by the commune layer, so its province polygon is left invisible (it
        // is only there for its name).
        private static Shapefile BuildProvinces(string folder)
        {
            string path = Path.Combine(folder, "DiaPhan_Tinh_2025.shp");
            if (!File.Exists(path)) return null;
            List<List<double[]>> records = ShapefilePolygonReader.Read(path);
            string[] names = DbfColumn.Read(Path.ChangeExtension(path, ".dbf"), "tenTinh");

            var layer = new Shapefile();
            if (!layer.CreateNew(string.Empty, ShpfileType.SHP_POLYGON)) return null;
            int nameField = layer.EditAddField("tenTinh", FieldType.STRING_FIELD, 0, 60);
            ShapefileCategory home = layer.Categories.Add("hue");
            home.DrawingOptions.FillVisible = true;
            home.DrawingOptions.FillColor = Ole(Color.FromArgb(243, 239, 230));
            home.DrawingOptions.LineVisible = false;

            for (int i = 0; i < records.Count; i++)
            {
                var shape = new Shape();
                if (!shape.Create(ShpfileType.SHP_POLYGON)) continue;
                double labelX = 0, labelY = 0, largest = 0;
                foreach (double[] ring in records[i]) AddRing(shape, ring, ref labelX, ref labelY, ref largest);
                if (shape.numPoints == 0) continue;
                int index = layer.EditAddShape(shape);
                if (index < 0) continue;
                string name = i < names.Length ? names[i] : string.Empty;
                layer.EditCellValue(nameField, index, name);
                if (name == HomeProvince) layer.set_ShapeCategory(index, 0);
                if (largest > 0) layer.Labels.AddLabel(name, labelX, labelY, 0, -1);
            }
            return layer;
        }

        private const string HomeProvince = "Thành phố Huế";
        private const double ProvinceToleranceMetres = 300;

        // Adds a ring thinned out; the centre of the largest ring of the shape is kept for the name.
        private static void AddRing(Shape shape, double[] degrees, ref double labelX, ref double labelY, ref double largest)
        {
            var kept = new List<double>();
            double lastX = 0, lastY = 0;
            for (int i = 0; i + 1 < degrees.Length; i += 2)
            {
                double x, y;
                VnUtm.ToMetres(degrees[i], degrees[i + 1], out x, out y);
                bool first = kept.Count == 0;
                if (!first && Math.Abs(x - lastX) < ProvinceToleranceMetres && Math.Abs(y - lastY) < ProvinceToleranceMetres) continue;
                kept.Add(x);
                kept.Add(y);
                lastX = x;
                lastY = y;
            }
            if (kept.Count < 6) return;   // fewer than three corners: a speck smaller than the thinning distance
            if (kept[0] != kept[kept.Count - 2] || kept[1] != kept[kept.Count - 1]) { kept.Add(kept[0]); kept.Add(kept[1]); }

            int part = shape.NumParts;
            shape.InsertPart(shape.numPoints, ref part);
            for (int i = 0; i < kept.Count; i += 2) shape.AddPoint(kept[i], kept[i + 1]);

            double area = 0, cx = 0, cy = 0;
            for (int i = 0; i + 3 < kept.Count; i += 2)
            {
                double cross = kept[i] * kept[i + 3] - kept[i + 2] * kept[i + 1];
                area += cross;
                cx += (kept[i] + kept[i + 2]) * cross;
                cy += (kept[i + 1] + kept[i + 3]) * cross;
            }
            if (Math.Abs(area) > largest && area != 0)
            {
                largest = Math.Abs(area);
                labelX = cx / (3 * area);
                labelY = cy / (3 * area);
            }
        }

        // ------------------------------------------------------------------ roads

        // Roads in two layers over each other: the wider "casing" below and the narrower filling above. The class of every road
        // comes from reading the table's "road_type" column once, then each shape gets its category directly (evaluating
        // category expressions over the 60,000 roads took several seconds per layer).
        private static void AddRoads(AxMap map, string folder, List<int> handles)
        {
            string path = Path.Combine(folder, "MangGiaoThong.shp");
            if (!File.Exists(path)) return;
            string[] types = DbfColumn.Read(Path.ChangeExtension(path, ".dbf"), "road_type");
            foreach (bool casing in new[] { true, false })
            {
                Shapefile roads = Open(path);
                if (roads == null) continue;
                StyleRoads(roads, casing, types);
                Register(map, roads, handles);
            }
        }

        // The road layers, so the small roads can be hidden when the map is zoomed out.
        private static readonly List<Shapefile> RoadLayers = new List<Shapefile>();
        private static int roadDetail = 0;   // 0 every road, 1 no small roads, 2 only the main roads

        private const double AllRoadsUntilScale = 80000;      // 1 : 80 000 and larger: every road
        private const double MediumRoadsUntilScale = 250000;  // up to 1 : 250 000: main and medium roads

        /// <summary>
        /// Hides the small roads (and then the medium ones) as the map is zoomed out: the small roads are only clutter at that
        /// scale. Changing the visibility of a road class is instant, the shapes keep their class.
        /// </summary>
        public static bool ApplyScale(double scaleDenominator)
        {
            int detail = scaleDenominator <= AllRoadsUntilScale ? 0 : scaleDenominator <= MediumRoadsUntilScale ? 1 : 2;
            if (detail == roadDetail || RoadLayers.Count == 0) return false;
            roadDetail = detail;
            for (int i = 0; i < RoadLayers.Count; i++)
            {
                Shapefile roads = RoadLayers[i];
                bool fill = i % 2 == 1;   // the layers come in pairs: casing, then fill
                roads.Categories.get_Item(1).DrawingOptions.Visible = detail <= 1;   // medium
                roads.Categories.get_Item(2).DrawingOptions.Visible = detail == 0;   // small
                if (fill) roads.DefaultDrawingOptions.Visible = detail == 0;         // footpaths and the like
            }
            return true;
        }

        private static void StyleRoads(Shapefile roads, bool casing, string[] types)
        {
            RoadLayers.Add(roads);
            ShapeDrawingOptions other = roads.DefaultDrawingOptions;   // footpaths, steps, unknown
            other.LineVisible = true;
            other.LineColor = Ole(casing ? Color.FromArgb(243, 239, 230) : Color.FromArgb(214, 217, 224));
            other.LineWidth = casing ? 0f : Ui.S(1f);
            other.Visible = !casing;

            AddClass(roads, "major", casing ? Color.FromArgb(214, 160, 58) : Color.FromArgb(252, 205, 120), Ui.S(casing ? 7.5f : 5.5f));
            AddClass(roads, "medium", casing ? Color.FromArgb(185, 192, 203) : Color.White, Ui.S(casing ? 5.5f : 3.8f));
            AddClass(roads, "minor", casing ? Color.FromArgb(206, 211, 219) : Color.White, Ui.S(casing ? 3.6f : 2.2f));

            int count = Math.Min(types.Length, roads.NumShapes);
            for (int i = 0; i < count; i++)
            {
                int category = ClassOf(types[i]);
                if (category >= 0) roads.set_ShapeCategory(i, category);
            }
        }

        private static int ClassOf(string type)
        {
            if (Array.IndexOf(Major, type) >= 0) return 0;
            if (Array.IndexOf(Medium, type) >= 0) return 1;
            return Array.IndexOf(Minor, type) >= 0 ? 2 : -1;
        }

        private static void AddClass(Shapefile roads, string name, Color color, float width)
        {
            ShapefileCategory category = roads.Categories.Add(name);
            ShapeDrawingOptions options = category.DrawingOptions;
            options.LineVisible = true;
            options.LineColor = Ole(color);
            options.LineWidth = width;
        }

        // ------------------------------------------------------------------ styling

        private static void StyleArea(Shapefile shapefile, Color fill, Color line, float lineWidth)
        {
            ShapeDrawingOptions style = shapefile.DefaultDrawingOptions;
            style.FillVisible = true;
            style.FillColor = Ole(fill);
            style.LineVisible = lineWidth > 0;
            style.LineColor = Ole(line);
            style.LineWidth = lineWidth;
        }

        private static uint Ole(Color color) => (uint)ColorTranslator.ToOle(color);

        // One name at a position in degrees, shown when the map is zoomed out to 1:300 000 or smaller.
        private static void NameAt(Shapefile shapefile, string text, double longitude, double latitude, Color color, Color halo, bool italic)
        {
            double x, y;
            VnUtm.ToMetres(longitude, latitude, out x, out y);
            shapefile.Labels.AddLabel(text, x, y, 0, -1);
            StyleLabels(shapefile.Labels, color, halo, 14, italic, CountryNamesFromScale, Unlimited);
        }

        // Names from a text field; the names are visible between the two map scales (1 : value).
        private static void AddLabels(Shapefile shapefile, string field, Color color, Color halo, int size, bool italic, double minScale, double maxScale)
        {
            int index = shapefile.Table.get_FieldIndexByName(field);
            if (index < 0) return;
            shapefile.GenerateLabels(index, tkLabelPositioning.lpCentroid, false);
            StyleLabels(shapefile.Labels, color, halo, size, italic, minScale, maxScale);
        }

        private static void StyleLabels(Labels labels, Color color, Color halo, int size, bool italic, double minScale, double maxScale)
        {
            labels.FontName = "Segoe UI";
            labels.FontSize = size;
            labels.FontBold = !italic;
            labels.FontItalic = italic;
            labels.FontColor = Ole(color);
            labels.HaloVisible = true;
            labels.HaloColor = Ole(halo);
            labels.HaloSize = 2;
            labels.FrameVisible = false;
            labels.AvoidCollisions = true;
            labels.DynamicVisibility = true;
            labels.MinVisibleScale = minScale;
            labels.MaxVisibleScale = maxScale;
        }
    }
}
