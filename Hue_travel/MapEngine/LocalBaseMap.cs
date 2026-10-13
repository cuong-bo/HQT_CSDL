using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using AxInterop.MapWinGIS;
using Interop.MapWinGIS;
using Hue_travel.Theme;

namespace Hue_travel.MapEngine
{
    /// <summary>
    /// The base map drawn from the project's own Shapefiles (Data\shp) instead of web tiles. From the bottom: the sea, the land of the
    /// neighbouring countries, the communes, the water (lakes, rivers) and the roads (a wider casing under a narrower filling).
    /// Layers that are only for calculation (the road network's nodes) or not needed yet are left out.
    /// </summary>
    internal static class LocalBaseMap
    {
        public const string FolderName = "shp";

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

        public static string Folder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", FolderName);

        /// <summary>Adds the layers to the map; returns their handles (empty when the files are missing).</summary>
        public static List<int> Add(AxMap map)
        {
            var handles = new List<int>();
            try
            {
                string folder = Folder;
                if (!Directory.Exists(folder)) return handles;

                Shapefile sea = Open(folder, "NenBien");
                if (sea != null)
                {
                    StyleArea(sea, Color.FromArgb(207, 232, 245), Color.FromArgb(207, 232, 245), 0f);
                    Register(map, sea, handles);
                }

                Shapefile abroad = Open(folder, "DL_Nc_Ngoai");
                if (abroad != null)
                {
                    StyleArea(abroad, Color.FromArgb(232, 230, 224), Color.FromArgb(214, 211, 203), 1f);
                    Register(map, abroad, handles);
                }

                Shapefile communes = Open(folder, "rgXa");
                if (communes != null)
                {
                    StyleArea(communes, Color.FromArgb(243, 239, 230), Color.FromArgb(188, 176, 214), 1.2f);
                    AddLabels(communes, "ten_xa", 150000);
                    Register(map, communes, handles);
                }

                Shapefile lakes = Open(folder, "song_2n");
                if (lakes != null)
                {
                    StyleArea(lakes, Color.FromArgb(165, 212, 238), Color.FromArgb(140, 196, 228), 1f);
                    Register(map, lakes, handles);
                }

                Shapefile streams = Open(folder, "song_1n");
                if (streams != null)
                {
                    ShapeDrawingOptions style = streams.DefaultDrawingOptions;
                    style.LineVisible = true;
                    style.LineColor = Ole(Color.FromArgb(150, 202, 232));
                    style.LineWidth = Ui.S(1.6f);
                    Register(map, streams, handles);
                }

                Shapefile casing = Open(folder, "MangGiaoThong");
                if (casing != null)
                {
                    StyleRoads(casing, true);
                    Register(map, casing, handles);
                }
                Shapefile roads = Open(folder, "MangGiaoThong");
                if (roads != null)
                {
                    StyleRoads(roads, false);
                    Register(map, roads, handles);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LocalBaseMap.Add: " + ex.Message);
            }
            return handles;
        }

        private static Shapefile Open(string folder, string name)
        {
            string path = Path.Combine(folder, name + ".shp");
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

        private static void AddLabels(Shapefile shapefile, string field, double maxScale)
        {
            int index = shapefile.Table.get_FieldIndexByName(field);
            if (index < 0) return;
            shapefile.GenerateLabels(index, tkLabelPositioning.lpCentroid, false);
            Labels labels = shapefile.Labels;
            labels.FontName = "Segoe UI";
            labels.FontSize = 10;
            labels.FontItalic = true;
            labels.FontColor = Ole(Color.FromArgb(148, 163, 184));
            labels.HaloVisible = true;
            labels.HaloColor = Ole(Color.FromArgb(243, 239, 230));
            labels.HaloSize = 2;
            labels.FrameVisible = false;
            labels.AvoidCollisions = true;
            labels.DynamicVisibility = true;
            labels.MaxVisibleScale = maxScale;
        }

        // Roads in two layers over each other: the wider "casing" below and the narrower filling above, per road class.
        private static void StyleRoads(Shapefile roads, bool casing)
        {
            ShapeDrawingOptions other = roads.DefaultDrawingOptions;   // footpaths, steps, unknown
            other.LineVisible = true;
            other.LineColor = Ole(casing ? Color.FromArgb(243, 239, 230) : Color.FromArgb(214, 217, 224));
            other.LineWidth = casing ? 0f : Ui.S(1f);
            other.Visible = !casing;

            AddClass(roads, "major", Major, casing ? Color.FromArgb(214, 160, 58) : Color.FromArgb(252, 205, 120), Ui.S(casing ? 7.5f : 5.5f));
            AddClass(roads, "medium", Medium, casing ? Color.FromArgb(185, 192, 203) : Color.White, Ui.S(casing ? 5.5f : 3.8f));
            AddClass(roads, "minor", Minor, casing ? Color.FromArgb(206, 211, 219) : Color.White, Ui.S(casing ? 3.6f : 2.2f));
            roads.Categories.ApplyExpressions();
        }

        private static void AddClass(Shapefile roads, string name, string[] values, Color color, float width)
        {
            ShapefileCategory category = roads.Categories.Add(name);
            category.Expression = "[road_type] = \"" + string.Join("\" OR [road_type] = \"", values) + "\"";
            ShapeDrawingOptions options = category.DrawingOptions;
            options.LineVisible = true;
            options.LineColor = Ole(color);
            options.LineWidth = width;
        }
    }
}
