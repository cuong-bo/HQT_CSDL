using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Account;
using Hue_travel.Models;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Panels.Feed
{
    /// <summary>
    /// The window for a new post: text, up to a few pictures, a place, and (when a route is open) that route. The pictures are shrunk here
    /// before they go (the server shrinks them again and removes their metadata). A person who has not accepted the community rules does so here.
    /// </summary>
    internal sealed class ComposerDialog : ModalCard
    {
        private const int DialogWidth = 540;
        private const int MaxSide = 1600;
        private readonly ApiFeedService feed;
        private readonly FeedMe me;
        private readonly IReadOnlyList<string> routeIds;
        private readonly CancellationTokenSource closing = new CancellationTokenSource();
        private readonly InputField body = new InputField { IsMultiline = true, PlaceholderText = "Bạn đang thấy gì ở Huế? (tối đa 2000 ký tự)" };
        private readonly Label placeLabel = new Label { AutoSize = false, Font = ThemeFonts.Medium(12), ForeColor = ThemeColors.Slate600, TextAlign = ContentAlignment.MiddleLeft };
        private readonly Label photoLabel = new Label { AutoSize = false, Font = ThemeFonts.Medium(12), ForeColor = ThemeColors.Slate600, TextAlign = ContentAlignment.MiddleLeft };
        private readonly IconButton pickPlace = AccountKit.Secondary("Gắn địa điểm", "place");
        private readonly IconButton addPhoto = AccountKit.Secondary("Thêm ảnh", "add");
        private readonly IconButton removePhoto = AccountKit.Secondary("Bỏ ảnh cuối");
        private readonly CheckBox shareRoute = new CheckBox { AutoSize = false, Font = ThemeFonts.Medium(12), ForeColor = ThemeColors.Slate700, BackColor = Color.White };
        private readonly CheckBox terms = new CheckBox { AutoSize = false, Font = ThemeFonts.Regular(11), ForeColor = ThemeColors.Slate600, BackColor = Color.White };
        private readonly Label status = new Label { AutoSize = false, Font = ThemeFonts.Medium(12), ForeColor = ThemeColors.Slate500, TextAlign = ContentAlignment.MiddleLeft };
        private readonly List<string> photos = new List<string>();
        private Poi place;
        private bool busy;

        /// <summary>"visible" (published) or "pending" (waits for an admin) after a successful post.</summary>
        public string ResultStatus { get; private set; }

        private ComposerDialog(ApiFeedService feed, FeedMe me, IReadOnlyList<string> routeIds, Poi suggestedPlace)
            : base("Chia sẻ khoảnh khắc", DialogWidth, "Đăng bài")
        {
            this.feed = feed;
            this.me = me;
            this.routeIds = routeIds ?? new string[0];
            place = suggestedPlace;

            int inner = DialogWidth - Side * 2, y = 0;
            body.SetBounds(0, Ui.S(y), Ui.S(inner), Ui.S(130));
            Content.Controls.Add(body);
            y += 140;

            placeLabel.SetBounds(0, Ui.S(y), Ui.S(inner) - Ui.S(150), Ui.S(36));
            pickPlace.SetBounds(Ui.S(inner) - Ui.S(144), Ui.S(y), Ui.S(144), Ui.S(36));
            Content.Controls.Add(placeLabel);
            Content.Controls.Add(pickPlace);
            y += 44;

            photoLabel.SetBounds(0, Ui.S(y), Ui.S(inner) - Ui.S(290), Ui.S(36));
            addPhoto.SetBounds(Ui.S(inner) - Ui.S(284), Ui.S(y), Ui.S(130), Ui.S(36));
            removePhoto.SetBounds(Ui.S(inner) - Ui.S(148), Ui.S(y), Ui.S(148), Ui.S(36));
            Content.Controls.Add(photoLabel);
            Content.Controls.Add(addPhoto);
            Content.Controls.Add(removePhoto);
            y += 44;

            if (this.routeIds.Count >= 2)
            {
                shareRoute.Text = "Đính kèm lộ trình đang mở (" + this.routeIds.Count + " địa điểm)";
                shareRoute.SetBounds(0, Ui.S(y), Ui.S(inner), Ui.S(28));
                Content.Controls.Add(shareRoute);
                y += 34;
            }
            if (!me.TermsAccepted)
            {
                terms.Text = "Tôi đồng ý quy tắc cộng đồng: không đăng số điện thoại, biển số xe hay ảnh người khác khi chưa được phép; nội dung vi phạm có thể bị gỡ và tài khoản bị khóa.";
                terms.SetBounds(0, Ui.S(y), Ui.S(inner), Ui.S(48));
                Content.Controls.Add(terms);
                y += 54;
            }
            else if (!me.Trusted)
            {
                var note = AccountKit.Hint("Bài đầu tiên của tài khoản mới sẽ được quản trị viên duyệt trước khi hiện công khai.");
                note.SetBounds(0, Ui.S(y), Ui.S(inner), Ui.S(40));
                Content.Controls.Add(note);
                y += 44;
            }
            status.SetBounds(0, Ui.S(y), Ui.S(inner), Ui.S(24));
            Content.Controls.Add(status);
            y += 24;

            pickPlace.Click += (s, e) => PickPlace();
            addPhoto.Click += (s, e) => AddPhoto();
            removePhoto.Click += (s, e) => { if (photos.Count > 0) { photos.RemoveAt(photos.Count - 1); RefreshLabels(); } };
            RefreshLabels();
            Arrange(y);
            FormClosed += (s, e) => closing.Cancel();
        }

        /// <summary>Opens the window; returns "visible", "pending" or null when the person gave up.</summary>
        public static string ShowFor(IWin32Window owner, ApiFeedService feed, FeedMe me, IReadOnlyList<string> routeIds, Poi suggestedPlace)
        {
            using (var dialog = new ComposerDialog(feed, me, routeIds, suggestedPlace))
                return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.ResultStatus : null;
        }

        private void RefreshLabels()
        {
            placeLabel.Text = place == null ? "Chưa gắn địa điểm" : "Địa điểm: " + place.Name;
            pickPlace.Text = place == null ? "Gắn địa điểm" : "Đổi địa điểm";
            photoLabel.Text = "Ảnh: " + photos.Count + " / " + me.MaxMediaPerPost;
            addPhoto.Enabled = photos.Count < me.MaxMediaPerPost && !busy;
            removePhoto.Enabled = photos.Count > 0 && !busy;
        }

        private void PickPlace()
        {
            IReadOnlyList<Poi> chosen = Dialogs.PickPlaces(this, "Gắn địa điểm", 1, place == null ? new string[0] : new[] { place.Id }, "Chọn");
            place = chosen != null && chosen.Count > 0 ? chosen[0] : null;
            RefreshLabels();
        }

        private void AddPhoto()
        {
            using (var dialog = new OpenFileDialog { Title = "Chọn ảnh", Filter = "Ảnh (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png", Multiselect = true })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                foreach (string path in dialog.FileNames)
                    if (photos.Count < me.MaxMediaPerPost && !photos.Contains(path)) photos.Add(path);
            }
            RefreshLabels();
        }

        private void Show(string text, bool error)
        {
            status.ForeColor = error ? ThemeColors.Red500 : ThemeColors.Slate500;
            status.Text = text;
        }

        protected override bool CanAccept()
        {
            var ignored = PostAsync();
            return false;
        }

        private async Task PostAsync()
        {
            if (busy) return;
            string text = body.Text.Trim();
            bool withRoute = shareRoute.Checked && routeIds.Count >= 2;
            if (text.Length == 0 && photos.Count == 0 && !withRoute) { Show("Hãy viết gì đó, thêm ảnh hoặc đính kèm lộ trình.", true); return; }
            if (text.Length > 2000) { Show("Bài viết tối đa 2000 ký tự.", true); return; }
            if (!me.TermsAccepted && !terms.Checked) { Show("Hãy đồng ý quy tắc cộng đồng để đăng bài.", true); return; }

            busy = true;
            Ok.Enabled = false;
            RefreshLabels();
            try
            {
                CancellationToken ct = closing.Token;
                if (!me.TermsAccepted)
                {
                    string error = await feed.AcceptTermsAsync(ct);
                    if (error != null) { Show(error, true); return; }
                    me.TermsAccepted = true;
                }

                var mediaIds = new List<long>();
                for (int i = 0; i < photos.Count; i++)
                {
                    Show("Đang tải ảnh " + (i + 1) + " / " + photos.Count + "…", false);
                    byte[] jpeg = PrepareImage(photos[i]);
                    if (jpeg == null) { Show("Không đọc được ảnh " + Path.GetFileName(photos[i]) + ".", true); return; }
                    ApiResult<long> up = await feed.UploadAsync(jpeg, ct);
                    if (!up.Ok) { Show(up.Error, true); return; }
                    mediaIds.Add(up.Value);
                }

                Show("Đang đăng bài…", false);
                ApiResult<string> made = await feed.CreateAsync(text, place == null ? null : place.Id, withRoute ? routeIds.ToList() : null, mediaIds, ct);
                if (!made.Ok) { Show(made.Error, true); return; }
                ResultStatus = made.Value;
                DialogResult = DialogResult.OK;
            }
            catch (OperationCanceledException) { }
            finally
            {
                busy = false;
                if (!IsDisposed) { Ok.Enabled = true; RefreshLabels(); }
            }
        }

        /// <summary>The picture turned upright, at most 1600 px on its longer side, as a JPEG; null when it cannot be read.</summary>
        internal static byte[] PrepareImage(string path)
        {
            try
            {
                using (Image source = Image.FromFile(path))
                {
                    RotateFlipType turn = RotateFlipType.RotateNoneFlipNone;
                    if (Array.IndexOf(source.PropertyIdList, 0x0112) >= 0)
                    {
                        switch (BitConverter.ToUInt16(source.GetPropertyItem(0x0112).Value, 0))
                        {
                            case 2: turn = RotateFlipType.RotateNoneFlipX; break;
                            case 3: turn = RotateFlipType.Rotate180FlipNone; break;
                            case 4: turn = RotateFlipType.RotateNoneFlipY; break;
                            case 5: turn = RotateFlipType.Rotate90FlipX; break;
                            case 6: turn = RotateFlipType.Rotate90FlipNone; break;
                            case 7: turn = RotateFlipType.Rotate270FlipX; break;
                            case 8: turn = RotateFlipType.Rotate270FlipNone; break;
                        }
                    }
                    double scale = Math.Min(1.0, (double)MaxSide / Math.Max(source.Width, source.Height));
                    int w = Math.Max(1, (int)Math.Round(source.Width * scale)), h = Math.Max(1, (int)Math.Round(source.Height * scale));
                    using (var bitmap = new Bitmap(w, h, PixelFormat.Format24bppRgb))
                    {
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            g.Clear(Color.White);
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.DrawImage(source, 0, 0, w, h);
                        }
                        if (turn != RotateFlipType.RotateNoneFlipNone) bitmap.RotateFlip(turn);
                        ImageCodecInfo jpeg = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                        using (var parameters = new EncoderParameters(1))
                        using (var stream = new MemoryStream())
                        {
                            parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 85L);
                            bitmap.Save(stream, jpeg, parameters);
                            return stream.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is OutOfMemoryException || ex is ArgumentException || ex is IOException || ex is ExternalException || ex is UnauthorizedAccessException)
            {
                return null;   // not a picture, or not readable
            }
        }
    }
}
