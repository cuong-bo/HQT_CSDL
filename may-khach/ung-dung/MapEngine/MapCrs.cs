using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AxInterop.MapWinGIS;
using Interop.MapWinGIS;

namespace Hue_travel.MapEngine
{
    /// <summary>The coordinate system the whole project uses: VN-2000 / UTM zone 48N (EPSG:3405), units in metres.</summary>
    public static class MapCrs
    {
        public const int Epsg = 3405;
        public const string Name = "VN-2000 / UTM zone 48N";

        /// <summary>Declares the map's coordinate system. Returns false (and the map keeps plain coordinates) if MapWinGIS cannot load it.</summary>
        public static bool Apply(AxMap map)
        {
            GeoProjection projection = Create();
            if (projection == null) return false;
            map.GeoProjection = projection;
            // Once the map has a coordinate system MapWinGIS switches on its OpenStreetMap tiles and its zoom bar;
            // the demo scene draws its own base and the app has its own zoom controls.
            map.TileProvider = tkTileProvider.ProviderNone;
            map.ShowZoomBar = false;
            return true;
        }

        private static GeoProjection Create()
        {
            try
            {
                var projection = new GeoProjection();
                if (projection.ImportFromEPSG(Epsg)) return projection;
                Debug.WriteLine("MapCrs: MapWinGIS could not import EPSG:" + Epsg);
            }
            catch (COMException ex)
            {
                Debug.WriteLine("MapCrs: " + ex.Message);
            }
            return null;
        }
    }
}
