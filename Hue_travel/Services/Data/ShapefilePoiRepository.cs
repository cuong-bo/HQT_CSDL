using System.Collections.Generic;
using System.IO;
using Hue_travel.Models;

namespace Hue_travel.Services.Data
{
    /// <summary>Builds the point-of-interest list from the four tourism Shapefile layers in a data folder.</summary>
    public static class ShapefilePoiRepository
    {
        private const string NotApplicable = "Không áp dụng";

        public static IReadOnlyList<Poi> Load(string dataDir)
        {
            if (!Directory.Exists(dataDir)) throw new DirectoryNotFoundException("Data folder not found: " + dataDir);

            var pois = new List<Poi>();
            foreach (ShapePoint p in ShapefilePointReader.Read(Path.Combine(dataDir, "diemDuLich.shp")))
                pois.Add(FromSite(p));
            foreach (ShapePoint p in ShapefilePointReader.Read(Path.Combine(dataDir, "diemLuuTru.shp")))
                pois.Add(FromStay(p));
            foreach (ShapePoint p in ShapefilePointReader.Read(Path.Combine(dataDir, "diemDVDuLich.shp")))
                pois.Add(FromService(p));
            foreach (ShapePoint p in ShapefilePointReader.Read(Path.Combine(dataDir, "diemDMGiaoThong.shp")))
                pois.Add(FromTransport(p));
            return pois;
        }

        private static Poi FromSite(ShapePoint p)
        {
            string type = p.Get("LoaiDL");
            return Build(p, p.Get("MaDL"), p.Get("TenDL"), TourismGrouping.GroupOf(type), type, p.Get("DiaChi"),
                p.Get("GioMoCua"), p.Get("GiaVePhi"), p.Get("SDTHoTro"), p.Get("Nguon"), type);
        }

        private static Poi FromStay(ShapePoint p)
        {
            string check = string.Format("Nhận phòng {0} - Trả phòng {1}", p.Get("GioNhan"), p.Get("GioTra"));
            return Build(p, p.Get("MaLT"), p.Get("TenCS"), PoiCategory.Stay, p.Get("LoaiCS"), p.Get("DiaChi"),
                check, p.Get("GiaTK"), p.Get("SDT"), p.Get("Website"), p.Get("MoTa"));
        }

        private static Poi FromService(ShapePoint p)
        {
            return Build(p, p.Get("MaDV"), p.Get("TenDV"), PoiCategory.Service, p.Get("LoaiDV"), p.Get("DiaChi"),
                p.Get("GioHD"), p.Get("GiaDV"), p.Get("SDT"), p.Get("Website"), p.Get("DichVu"));
        }

        private static Poi FromTransport(ShapePoint p)
        {
            return Build(p, p.Get("MaGT"), p.Get("TenGT"), PoiCategory.Transport, p.Get("LoaiGT"), p.Get("DiaChi"),
                p.Get("GioHD"), NotApplicable, p.Get("SDT"), string.Empty, p.Get("DonViQL"));
        }

        private static Poi Build(ShapePoint p, string id, string name, PoiCategory category, string typeLabel, string address,
            string hours, string price, string phone, string website, string summary)
        {
            return new Poi
            {
                Id = id,
                Name = name,
                Category = category,
                TypeLabel = typeLabel,
                Address = address,
                OpeningHours = hours,
                TicketPrice = price,
                Phone = phone,
                Website = website,
                Summary = summary,
                Description = summary,
                Longitude = Number(p.Get("Lon")),
                Latitude = Number(p.Get("Lat")),
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
