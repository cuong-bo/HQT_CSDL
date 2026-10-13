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
    /// The base map drawn from the project's own Shapefiles (province and commune boundaries, rivers, roads) instead of web tiles.
    /// The layers go in below the places, in the order land, boundary, water, road casing, roads.
    /// </summary>
    internal static class LocalBaseMap
    {
        public const string FolderName = "NenBanDo";

        // Road classes by the OpenStreetMap highway value kept in the "loai_doi_t" field.
        private static readonly string[] Major = { "motorway", "motorway_link", "trunk", "trunk_link", "primary", "primary_link" };
        private static readonly string[] Medium = { "secondary", "secondary_link", "tertiary", "tertiary_link" };
        private static readonly string[] Minor = { "residential", "unclassified", "service", "living_street", "pedestrian" };

        public static string Folder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", FolderName);

        /// <summary>Adds the layers to the map; returns their handles (empty when the files are missing).</summary>
        public static List<int> Add(AxMap map)
        {
            var handles = new List<int>();
            try
            {
                string folder = Folder;
                if (!Directory.Exists(folder)) return handles;

                Shapefile communes = Open(folder, "rgXa");
                if (communes != null)
                {
                    ShapeDrawingOptions style = communes.DefaultDrawingOptions;
                    style.FillVisible = true;
                    style.FillColor = Ole(Color.FromArgb(243, 239, 230));
                    style.LineVisible = true;
                    style.LineColor = Ole(Color.FromArgb(214, 207, 190));
                    style.LineWidth = 1f;
                    AddLabels(communes, "tenXa", 150000);
                    Register(map, communes, handles);
                }

                Shapefile province = Open(folder, "rgTinh");
                if (province != null)
                {
                    ShapeDrawingOptions style = province.DefaultDrawingOptions;
                    style.FillVisible = false;
                    style.LineVisible = true;
                    style.LineColor = Ole(Color.FromArgb(167, 139, 250));
                    style.LineWidth = Ui.S(2.2f);
                    style.LineStipple = tkDashStyle.dsDash;
                    Register(map, province, handles);
                }

                Shapefile lakes = Open(folder, "song_2n");
                if (lakes != null)
                {
                    ShapeDrawingOptions style = lakes.DefaultDrawingOptions;
                    style.FillVisible = true;
                    style.FillColor = Ole(Color.FromArgb(165, 212, 238));
                    style.LineVisible = true;
                    style.LineColor = Ole(Color.FromArgb(140, 196, 228));
                    style.LineWidth = 1f;
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

                Shapefile casing = Open(folder, "duongGiaothong");
                if (casing != null)
                {
                    StyleRoads(casing, true);
                    Register(map, casing, handles);
                }
                Shapefile roads = Open(folder, "duongGiaothong");
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
            ShapeDrawingOptions other = roads.DefaultDrawingOptions;   // tracks, paths, footways, steps
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
            category.Expression = "[loai_doi_t] = \"" + string.Join("\" OR [loai_doi_t] = \"", values) + "\"";
            ShapeDrawingOptions options = category.DrawingOptions;
            options.LineVisible = true;
            options.LineColor = Ole(color);
            options.LineWidth = width;
        }
    }
}
