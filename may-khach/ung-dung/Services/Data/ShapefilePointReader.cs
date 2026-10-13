using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Hue_travel.Services.Data
{
    /// <summary>One point of a point Shapefile: its geometry (in the layer's own CRS) and its DBF attributes.</summary>
    public sealed class ShapePoint
    {
        public ShapePoint(double x, double y, IReadOnlyDictionary<string, string> attributes)
        {
            X = x;
            Y = y;
            Attributes = attributes;
        }

        public double X { get; }
        public double Y { get; }
        public IReadOnlyDictionary<string, string> Attributes { get; }

        public string Get(string field)
        {
            string value;
            return Attributes.TryGetValue(field, out value) ? value : string.Empty;
        }
    }

    /// <summary>
    /// Minimal managed reader for point Shapefiles (.shp + .dbf, encoding from .cpg). It is used for the
    /// attribute tables and the offline catalogue; MapWinGIS still draws and hit-tests the map itself.
    /// </summary>
    public static class ShapefilePointReader
    {
        private const int ShpHeaderBytes = 100;
        private const int ShpRecordHeaderBytes = 8;
        private const int NullShape = 0;
        private const int PointType = 1;
        private const int PointMType = 21;
        private const int PointZType = 11;
        private const int MinPointContentBytes = 20; // shape type + X + Y
        private const byte DbfDeletedMark = (byte)'*';
        private const byte DbfFieldTerminator = 0x0D;

        public static IReadOnlyList<ShapePoint> Read(string shpPath)
        {
            if (string.IsNullOrWhiteSpace(shpPath)) throw new ArgumentException("Shapefile path is empty.", nameof(shpPath));
            if (!File.Exists(shpPath)) throw new FileNotFoundException("Shapefile not found.", shpPath);

            string stem = Path.Combine(Path.GetDirectoryName(shpPath) ?? string.Empty, Path.GetFileNameWithoutExtension(shpPath));
            byte[] shp = File.ReadAllBytes(shpPath);
            DbfTable table = DbfTable.Load(stem + ".dbf", DetectEncoding(stem));

            var points = new List<ShapePoint>(table.Rows.Count);
            int offset = ShpHeaderBytes;
            int row = 0;
            while (offset + ShpRecordHeaderBytes <= shp.Length && row < table.Rows.Count)
            {
                int contentBytes = ReadInt32BigEndian(shp, offset + 4) * 2;
                int content = offset + ShpRecordHeaderBytes;
                if (content + contentBytes > shp.Length) throw new InvalidDataException("Truncated record in " + shpPath);

                DbfRow attributes = table.Rows[row++];
                offset = content + contentBytes;
                if (attributes.IsDeleted) continue;

                if (contentBytes < sizeof(int)) throw new InvalidDataException("Empty record in " + shpPath);
                int shapeType = BitConverter.ToInt32(shp, content);
                if (shapeType == NullShape) continue;
                bool isPoint = shapeType == PointType || shapeType == PointZType || shapeType == PointMType;
                if (!isPoint || contentBytes < MinPointContentBytes) throw new InvalidDataException("Not a point layer: " + shpPath);

                points.Add(new ShapePoint(BitConverter.ToDouble(shp, content + 4), BitConverter.ToDouble(shp, content + 12), attributes.Values));
            }
            return points;
        }

        /// <summary>Parses a number from a DBF text cell using the invariant culture; NaN when empty or invalid.</summary>
        public static double ParseNumber(string text)
        {
            double value;
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : double.NaN;
        }

        private static Encoding DetectEncoding(string stem)
        {
            string cpg = stem + ".cpg";
            if (!File.Exists(cpg)) return Encoding.UTF8;
            string name = File.ReadAllText(cpg).Trim();
            try
            {
                return string.IsNullOrEmpty(name) ? Encoding.UTF8 : Encoding.GetEncoding(name);
            }
            catch (ArgumentException)
            {
                return Encoding.UTF8; // an unknown code page name: UTF-8 is what the data set uses
            }
        }

        private static int ReadInt32BigEndian(byte[] data, int offset)
        {
            return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
        }

        private sealed class DbfRow
        {
            public DbfRow(bool isDeleted, IReadOnlyDictionary<string, string> values)
            {
                IsDeleted = isDeleted;
                Values = values;
            }

            public bool IsDeleted { get; }
            public IReadOnlyDictionary<string, string> Values { get; }
        }

        private sealed class DbfTable
        {
            private DbfTable(List<DbfRow> rows)
            {
                Rows = rows;
            }

            public List<DbfRow> Rows { get; }

            public static DbfTable Load(string path, Encoding encoding)
            {
                if (!File.Exists(path)) throw new FileNotFoundException("Attribute table not found.", path);
                byte[] data = File.ReadAllBytes(path);
                if (data.Length < 32) throw new InvalidDataException("Attribute table too short: " + path);

                int recordCount = BitConverter.ToInt32(data, 4);
                int headerBytes = BitConverter.ToUInt16(data, 8);
                int recordBytes = BitConverter.ToUInt16(data, 10);
                if (recordCount < 0 || headerBytes < 33 || recordBytes < 1) throw new InvalidDataException("Bad attribute table header: " + path);

                var names = new List<string>();
                var lengths = new List<int>();
                for (int at = 32; at < data.Length && data[at] != DbfFieldTerminator; at += 32)
                {
                    int end = Array.IndexOf(data, (byte)0, at, 11);
                    if (end < 0) end = at + 11;
                    names.Add(Encoding.ASCII.GetString(data, at, end - at));
                    lengths.Add(data[at + 16]);
                }

                if (lengths.Sum() + 1 > recordBytes) throw new InvalidDataException("Field widths exceed the record size: " + path);
                var rows = new List<DbfRow>(recordCount);
                for (int i = 0; i < recordCount; i++)
                {
                    int start = headerBytes + i * recordBytes;
                    if (start + recordBytes > data.Length) throw new InvalidDataException("Truncated attribute table: " + path);

                    var values = new Dictionary<string, string>(names.Count, StringComparer.OrdinalIgnoreCase);
                    int cursor = start + 1;
                    for (int f = 0; f < names.Count; f++)
                    {
                        values[names[f]] = encoding.GetString(data, cursor, lengths[f]).Trim().Normalize(NormalizationForm.FormC);
                        cursor += lengths[f];
                    }
                    rows.Add(new DbfRow(data[start] == DbfDeletedMark, values));
                }
                return new DbfTable(rows);
            }
        }
    }
}
