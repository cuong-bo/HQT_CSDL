using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hue_travel.Services.Data;

namespace Hue_travel.Services.Online
{
    /// <summary>Photos of places between the server and this computer: download what a place has, show a photo, upload a new one.</summary>
    public static class PhotoSync
    {
        private const long MaxUploadBytes = 6L * 1024 * 1024;

        /// <summary>
        /// Brings the stored photos of a place up to date with the server's approved list. Returns true when something changed
        /// (so the card can show them); false when nothing changed or the server could not be asked.
        /// </summary>
        public static async Task<bool> SyncAsync(ApiClient api, string placeId, CancellationToken ct)
        {
            ApiResponse list = await api.GetAsync("/v1/pois/" + Uri.EscapeDataString(placeId) + "/photos", ct).ConfigureAwait(true);
            if (!list.IsSuccess) return false;
            var ids = (JsonHelper.AsList(list.Json) ?? new List<object>()).OfType<IDictionary<string, object>>().Select(d => JsonHelper.Long(d, "id")).Where(i => i > 0).ToList();

            bool changed = false;
            foreach (long id in ids)
            {
                if (PhotoStore.Has(placeId, id)) continue;
                byte[] jpeg = await DownloadAsync(api, id, ct).ConfigureAwait(true);
                if (jpeg == null) return changed;   // keep what was fetched; the rest comes next time
                PhotoStore.Save(placeId, id, jpeg);
                changed = true;
            }
            return PhotoStore.SetOrder(placeId, ids) || changed;
        }

        private static async Task<byte[]> DownloadAsync(ApiClient api, long photoId, CancellationToken ct)
        {
            ApiResponse r = await api.GetAsync("/v1/photos/" + photoId, ct).ConfigureAwait(true);
            if (!r.IsSuccess) return null;
            string data = JsonHelper.Str(r.Object, "data");
            if (string.IsNullOrEmpty(data)) return null;
            try
            {
                return Convert.FromBase64String(data);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        /// <summary>A photo as a bitmap (a copy the caller owns), or null when it cannot be fetched. Used for waiting photos, which are not stored.</summary>
        public static async Task<Bitmap> LoadAsync(ApiClient api, long photoId, CancellationToken ct)
        {
            byte[] jpeg = await DownloadAsync(api, photoId, ct).ConfigureAwait(true);
            if (jpeg == null) return null;
            try
            {
                using (var stream = new MemoryStream(jpeg))
                using (Image image = Image.FromStream(stream))
                    return new Bitmap(image);   // a copy, so the stream can go
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>Sends an image file. An owner's photo waits for approval; an admin's is shown at once. Returns the server's answer.</summary>
        public static async Task<ApiResponse> UploadAsync(ApiClient api, string placeId, string file, bool asAdmin, CancellationToken ct)
        {
            FileInfo info = new FileInfo(file);
            if (!info.Exists) return new ApiResponse { StatusCode = 0, Error = "Không tìm thấy tệp ảnh." };
            if (info.Length > MaxUploadBytes) return new ApiResponse { StatusCode = 0, Error = "Ảnh quá lớn (tối đa 6 MB). Hãy chọn ảnh nhỏ hơn." };
            string type = string.Equals(info.Extension, ".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
            string base64 = Convert.ToBase64String(File.ReadAllBytes(file));
            string path = (asAdmin ? "/v1/admin/pois/" : "/v1/owner/places/") + Uri.EscapeDataString(placeId) + "/photos";
            return await api.PostAsync(path, new Dictionary<string, object> { { "contentType", type }, { "data", base64 } }, ct).ConfigureAwait(true);
        }
    }
}
