using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Hue_travel.Services.Online;

namespace Hue_travel.Services.Data
{
    /// <summary>What was read from a file of places: the rows (named as the server's import wants them) and what could not be read.</summary>
    public sealed class PlaceFileResult
    {
        public List<Dictionary<string, object>> Rows { get; } = new List<Dictionary<string, object>>();
        public List<string> Problems { get; } = new List<string>();
    }

    /// <summary>
    /// Reads places from a CSV file (first row = column names) or a GeoJSON file (points with properties). Column names may be the
    /// server's (id, group, name, lon, lat, type, address, openingHours, priceText, phone, website, summary) or Vietnamese ones
    /// (mã, nhóm, tên, kinh độ, vĩ độ, loại hình, địa chỉ, giờ mở cửa, giá, điện thoại, giới thiệu). An empty cell means "leave as it is".
    /// </summary>
    public static class PlaceFileReader
    {
        public const int MaxRows = 500;

        private static readonly Dictionary<string, string> Columns = Build(
            "id=id,ma", "group=group,nhom,loai", "name=name,ten,tendiađiem", "lon=lon,lng,longitude,kinhđo", "lat=lat,latitude,vido",
            "type=type,loaihinh", "address=address,diachi", "openingHours=openinghours,giomocua,giolamviec", "priceText=pricetext,price,gia,giave",
            "phone=phone,sdt,dienthoai,sodienthoai", "website=website,web,trangweb", "summary=summary,mota,gioithieu,description");

        private static readonly Dictionary<string, string> Groups = new Dictionary<string, string>
        {
            { "heritage", "heritage" }, { "ditich", "heritage" }, { "scenery", "scenery" }, { "canhquan", "scenery" }, { "food", "food" }, { "amthuc", "food" },
            { "stay", "stay" }, { "luutru", "stay" }, { "service", "service" }, { "dichvu", "service" }, { "transport", "transport" }, { "giaothong", "transport" },
        };

        public static PlaceFileResult Read(string path)
        {
            string text;
            try
            {
                text = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                var failed = new PlaceFileResult();
                failed.Problems.Add("Không đọc được tệp: " + ex.Message);
                return failed;
            }
            string extension = Path.GetExtension(path).ToLowerInvariant();
            return extension == ".geojson" || extension == ".json" || text.TrimStart().StartsWith("{") ? ReadGeoJson(text) : ReadCsv(text);
        }

        // ------------------------------------------------------------------ CSV

        public static PlaceFileResult ReadCsv(string text)
        {
            var result = new PlaceFileResult();
            List<List<string>> table = Split(text.TrimStart('﻿'));
            if (table.Count < 2) { result.Problems.Add("Tệp cần có dòng tên cột và ít nhất một địa điểm."); return result; }

            string[] names = table[0].Select(h => Columns.ContainsKey(Normalize(h)) ? Columns[Normalize(h)] : null).ToArray();
            if (!names.Contains("id")) { result.Problems.Add("Thiếu cột \"id\" (mã địa điểm) ở dòng đầu."); return result; }
            for (int line = 1; line < table.Count; line++)
            {
                List<string> cells = table[line];
                if (cells.All(string.IsNullOrWhiteSpace)) continue;
                var row = new Dictionary<string, object>();
                for (int i = 0; i < names.Length && i < cells.Count; i++)
                    if (names[i] != null && !string.IsNullOrWhiteSpace(cells[i])) Put(row, names[i], cells[i].Trim(), line + 1, result);
                AddRow(result, row, line + 1);
            }
            return result;
        }

        // Splits CSV text into rows of cells; the separator is the most common of , ; tab in the first line; quoted cells may hold separators and line breaks.
        private static List<List<string>> Split(string text)
        {
            string firstLine = text.Split('\n')[0];
            char separator = new[] { ',', ';', '\t' }.OrderByDescending(c => firstLine.Count(x => x == c)).First();
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                    else if (c == '"') quoted = false;
                    else cell.Append(c);
                }
                else if (c == '"') quoted = true;
                else if (c == separator) { row.Add(cell.ToString()); cell.Clear(); }
                else if (c == '\n' || c == '\r')
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    row.Add(cell.ToString());
                    cell.Clear();
                    rows.Add(row);
                    row = new List<string>();
                }
                else cell.Append(c);
            }
            if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row); }
            return rows;
        }

        // ------------------------------------------------------------------ GeoJSON

        public static PlaceFileResult ReadGeoJson(string text)
        {
            var result = new PlaceFileResult();
            var root = JsonHelper.Parse(text) as IDictionary<string, object>;
            IList<object> features = JsonHelper.List(root, "features");
            if (features == null)
            {
                IDictionary<string, object> single = root != null && JsonHelper.Str(root, "type") == "Feature" ? root : null;
                features = single == null ? null : new List<object> { single };
            }
            if (features == null) { result.Problems.Add("Tệp không phải GeoJSON (cần FeatureCollection hoặc Feature)."); return result; }

            int index = 0;
            foreach (IDictionary<string, object> feature in features.OfType<IDictionary<string, object>>())
            {
                index++;
                var row = new Dictionary<string, object>();
                IDictionary<string, object> properties = JsonHelper.Obj(feature, "properties");
                if (properties != null)
                    foreach (KeyValuePair<string, object> p in properties)
                        if (Columns.ContainsKey(Normalize(p.Key)) && p.Value != null && !string.IsNullOrWhiteSpace(Convert.ToString(p.Value, CultureInfo.InvariantCulture)))
                            Put(row, Columns[Normalize(p.Key)], Convert.ToString(p.Value, CultureInfo.InvariantCulture).Trim(), index, result);
                if (!row.ContainsKey("id") && feature.ContainsKey("id") && feature["id"] != null) row["id"] = Convert.ToString(feature["id"], CultureInfo.InvariantCulture);
                IDictionary<string, object> geometry = JsonHelper.Obj(feature, "geometry");
                IList<object> coordinates = geometry != null && JsonHelper.Str(geometry, "type") == "Point" ? JsonHelper.List(geometry, "coordinates") : null;
                if (coordinates != null && coordinates.Count >= 2)
                {
                    row["lon"] = Convert.ToDouble(coordinates[0], CultureInfo.InvariantCulture);
                    row["lat"] = Convert.ToDouble(coordinates[1], CultureInfo.InvariantCulture);
                }
                AddRow(result, row, index);
            }
            return result;
        }

        // ------------------------------------------------------------------ shared

        private static void AddRow(PlaceFileResult result, Dictionary<string, object> row, int line)
        {
            if (!row.ContainsKey("id")) { result.Problems.Add("Dòng " + line + ": thiếu mã địa điểm."); return; }
            if (result.Rows.Count >= MaxRows) { result.Problems.Add("Chỉ đọc tối đa " + MaxRows + " địa điểm mỗi lần; phần còn lại bị bỏ qua."); return; }
            result.Rows.Add(row);
        }

        private static void Put(Dictionary<string, object> row, string key, string value, int line, PlaceFileResult result)
        {
            if (key == "lon" || key == "lat")
            {
                double number;
                if (!double.TryParse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out number)) { result.Problems.Add("Dòng " + line + ": \"" + value + "\" không phải số."); return; }
                row[key] = number;
            }
            else if (key == "group")
            {
                string group;
                if (!Groups.TryGetValue(Normalize(value), out group)) { result.Problems.Add("Dòng " + line + ": nhóm \"" + value + "\" không đúng."); return; }
                row[key] = group;
            }
            else row[key] = value;
        }

        // lower case, no accents, no spaces or punctuation: "Kinh độ" -> "kinhđo" (đ has no accent to drop, so it stays)
        private static string Normalize(string text)
        {
            string decomposed = (text ?? "").Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(c)) sb.Append(c);
            }
            return sb.ToString();
        }

        private static Dictionary<string, string> Build(params string[] definitions)
        {
            var map = new Dictionary<string, string>();
            foreach (string definition in definitions)
            {
                string[] parts = definition.Split('=');
                foreach (string alias in parts[1].Split(',')) map[alias] = parts[0];
            }
            return map;
        }
    }
}
