using System.Collections.Generic;
using System.IO;
using Hue_travel.Models;

namespace Hue_travel.Services.Data
{
    /// <summary>Builds the point-of-interest list from the tourism point Shapefile layers in a data folder.</summary>
    public static class ShapefilePoiRepository
    {
        private const string NotApplicable = "Không áp dụng";
        private const string NotYet = "Đang cập nhật";

        public static IReadOnlyList<Poi> Load(string dataDir)
        {
            if (!Directory.Exists(dataDir)) throw new DirectoryNotFoundException("Data folder not found: " + dataDir);

            var pois = new List<Poi>();
            foreach (ShapePoint p in ReadLayer(dataDir, "diemDuLich")) pois.Add(FromSite(p));
            foreach (ShapePoint p in ReadLayer(dataDir, "diemAnUong")) pois.Add(FromFood(p));
            foreach (ShapePoint p in ReadLayer(dataDir, "diemLuuTru")) pois.Add(FromStay(p));
            foreach (ShapePoint p in ReadLayer(dataDir, "diemDVDuLich")) pois.Add(FromService(p));
            foreach (ShapePoint p in ReadLayer(dataDir, "diemDMGiaoThong")) pois.Add(FromTransport(p));
            return pois;
        }

        // A layer that is not in the folder is skipped, so the app still starts with what is there.
        private static IEnumerable<ShapePoint> ReadLayer(string dataDir, string name)
        {
            string path = Path.Combine(dataDir, name + ".shp");
            return File.Exists(path) ? ShapefilePointReader.Read(path) : new ShapePoint[0];
        }

        private static Poi FromSite(ShapePoint p)
        {
            string type = p.Get("LoaiDL");
            return Build(p, p.Get("MaDL"), p.Get("TenDL"), TourismGrouping.GroupOf(type), type, p.Get("DiaChi"),
                p.Get("GioMoCua"), p.Get("GiaVePhi"), p.Get("SDTHoTro"), p.Get("Nguon"), type);
        }

        private static Poi FromFood(ShapePoint p)
        {
            string type = p.Get("LoaiAU");
            return Build(p, p.Get("MaAU"), p.Get("TenAU"), PoiCategory.Food, type, p.Get("DiaChi"),
                p.Get("GioMoCua"), p.Get("Gia"), p.Get("SDTHoTro"), p.Get("Nguon"), type);
        }

        private static Poi FromStay(ShapePoint p)
        {
            string check = string.Format("Nhận phòng {0} - Trả phòng {1}", p.Get("GioNhan"), p.Get("GioTra"));
            return Build(p, p.Get("MaLT"), p.Get("TenCS"), PoiCategory.Stay, p.Get("LoaiCS"), p.Get("DiaChi"),
                check, p.Get("GiaPhong"), p.Get("SDT"), p.Get("Website"), p.Get("LuuY"));
        }

        private static Poi FromService(ShapePoint p)
        {
            return Build(p, p.Get("MaCSDV"), p.Get("TenDV"), PoiCategory.Service, p.Get("LoaiDV"), p.Get("DiaChi"),
                OpeningHours(p.Get("GioMo"), p.Get("GioDong")), p.Get("GiaDV"), p.Get("SDT"), p.Get("Website"), p.Get("DichVu"));
        }

        private static Poi FromTransport(ShapePoint p)
        {
            return Build(p, p.Get("MaGT"), p.Get("TenGT"), PoiCategory.Transport, p.Get("LoaiGT"), p.Get("DiaChi"),
                p.Get("LichChay"), p.Get("GiaVe"), p.Get("SDT"), p.Get("Website"), p.Get("Tuyen"));
        }

        // "07:00 - 23:00", or whichever of the two times is known.
        private static string OpeningHours(string open, string close)
        {
            bool hasOpen = !string.IsNullOrWhiteSpace(open) && open != NotYet, hasClose = !string.IsNullOrWhiteSpace(close) && close != NotYet;
            if (hasOpen && hasClose) return open + " - " + close;
            return hasOpen ? "Mở cửa từ " + open : hasClose ? "Đóng cửa lúc " + close : string.Empty;
        }

        private static Poi Build(ShapePoint p, string id, string name, PoiCategory category, string typeLabel, string address,
            string hours, string price, string phone, string website, string summary)
        {
            // Some layers carry only the point geometry: the position in degrees is then worked out from the map metres.
            double longitude = Number(p.Get("Lon")), latitude = Number(p.Get("Lat"));
            if (longitude == 0 && latitude == 0 && (p.X != 0 || p.Y != 0)) VnUtm.ToDegrees(p.X, p.Y, out longitude, out latitude);
            return new Poi
            {
                Id = id,
                Name = name,
                Category = category,
                TypeLabel = typeLabel,
                Address = address,
                OpeningHours = hours,
                TicketPrice = string.IsNullOrWhiteSpace(price) ? NotApplicable : price,
                Phone = phone,
                Website = website,
                Summary = summary,
                Description = summary,
                Longitude = longitude,
                Latitude = latitude,
                X = p.X,
                Y = p.Y
            };
        }

        private static double Number(string text)
        {
            double value = ShapefilePointReader.ParseNumber(text);
            return double.IsNaN(value) ? 0 : value;
        }
    }
}
