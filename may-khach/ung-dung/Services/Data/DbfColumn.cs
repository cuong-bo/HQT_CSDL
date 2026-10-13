using System;
using System.IO;
using System.Text;

namespace Hue_travel.Services.Data
{
    /// <summary>
    /// Reads one text column of a .dbf table straight from the file, in record order. Much faster than asking the map engine for
    /// every cell of a big table (the road network has over 60,000 records).
    /// </summary>
    public static class DbfColumn
    {
        /// <summary>The values of <paramref name="field"/> (trimmed), one per record; an empty array when the field is not in the table.</summary>
        public static string[] Read(string dbfPath, string field)
        {
            if (!File.Exists(dbfPath)) return new string[0];
            using (var stream = new FileStream(dbfPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16))
            using (var reader = new BinaryReader(stream))
            {
                reader.ReadBytes(4);
                int records = reader.ReadInt32();
                int headerLength = reader.ReadUInt16();
                int recordLength = reader.ReadUInt16();

                int offset = 1, columnOffset = -1, columnLength = 0;
                stream.Position = 32;
                while (stream.Position < headerLength - 1)
                {
                    byte[] descriptor = reader.ReadBytes(32);
                    if (descriptor.Length < 32 || descriptor[0] == 0x0D) break;
                    string name = Encoding.ASCII.GetString(descriptor, 0, 11).TrimEnd('\0');
                    int length = descriptor[16];
                    if (string.Equals(name, field, StringComparison.OrdinalIgnoreCase)) { columnOffset = offset; columnLength = length; }
                    offset += length;
                }
                if (columnOffset < 0) return new string[0];

                var values = new string[records];
                var record = new byte[recordLength];
                stream.Position = headerLength;
                for (int i = 0; i < records; i++)
                {
                    if (reader.Read(record, 0, recordLength) < recordLength) { Array.Resize(ref values, i); break; }
                    values[i] = Encoding.UTF8.GetString(record, columnOffset, columnLength).Trim();
                }
                return values;
            }
        }
    }
}
