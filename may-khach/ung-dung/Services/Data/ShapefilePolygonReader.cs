using System.Collections.Generic;
using System.IO;

namespace Hue_travel.Services.Data
{
    /// <summary>
    /// Minimal reader for the geometry of polygon Shapefiles: every record becomes a list of rings, each ring a flat array
    /// x0, y0, x1, y1, ... (in the file's own units). Records without a shape give an empty list.
    /// </summary>
    public static class ShapefilePolygonReader
    {
        private const int PolygonType = 5;

        public static List<List<double[]>> Read(string shpPath)
        {
            var records = new List<List<double[]>>();
            using (var stream = new FileStream(shpPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16))
            using (var reader = new BinaryReader(stream))
            {
                stream.Position = 100;
                while (stream.Position + 8 <= stream.Length)
                {
                    reader.ReadBytes(4);   // record number (big endian)
                    int contentWords = ReadBigEndian(reader);
                    long end = stream.Position + contentWords * 2L;
                    var rings = new List<double[]>();
                    if (end > stream.Length) break;

                    int type = reader.ReadInt32();
                    if (type == PolygonType)
                    {
                        reader.ReadBytes(32);   // bounding box
                        int parts = reader.ReadInt32(), points = reader.ReadInt32();
                        var starts = new int[parts + 1];
                        for (int i = 0; i < parts; i++) starts[i] = reader.ReadInt32();
                        starts[parts] = points;
                        var coordinates = new double[points * 2];
                        for (int i = 0; i < coordinates.Length; i++) coordinates[i] = reader.ReadDouble();
                        for (int i = 0; i < parts; i++)
                        {
                            var ring = new double[(starts[i + 1] - starts[i]) * 2];
                            System.Array.Copy(coordinates, starts[i] * 2, ring, 0, ring.Length);
                            rings.Add(ring);
                        }
                    }
                    records.Add(rings);
                    stream.Position = end;
                }
            }
            return records;
        }

        private static int ReadBigEndian(BinaryReader reader)
        {
            byte[] b = reader.ReadBytes(4);
            return (b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3];
        }
    }
}
