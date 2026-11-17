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
    /// The window for a new post: text, up to a few pictures, a place, and (when a route is open) that route. Every picture chosen shows
    /// at once as a small preview with its own remove button. The pictures are shrunk here before they go (the server shrinks them again
    /// and removes their metadata).
    /// </summary>
    internal sealed class ComposerDialog : ModalCard
    {
        private const int DialogWidth = 540;
        private const int MaxSide = 1600;
        private const int ThumbSide = 240;
        private readonly ApiFeedService feed;
        private readonly FeedMe me;
        private readonly IReadOnlyList<string> routeIds;
        private readonly CancellationTokenSource closing = new CancellationTokenSource();
        private readonly InputField body = new InputField { IsMultiline = true, PlaceholderText = "Bạn đang thấy gì ở Huế? (tối đa 2000 ký tự)" };
        private readonly Label placeLabel = new Label { AutoSize = false, Font = ThemeFonts.Medium(12), ForeColor = ThemeColors.Slate600, TextAlign = ContentAlignment.MiddleLeft };
        private readonly Label photoLabel = new Label { AutoSize = false, Font = ThemeFonts.Medium(12), ForeColor = ThemeColors.Slate600, TextAlign = ContentAlignment.MiddleLeft };
        private readonly IconButton pickPlace = AccountKit.Secondary("Gắn địa điểm", "place");
        private readonly IconButton addPhoto = AccountKit.Secondary("Thêm ảnh", "add");
        private readonly PhotoStrip strip = new PhotoStrip();
        private readonly CheckBox shareRoute = new CheckBox { AutoSize = false, Font = ThemeFonts.Medium(12), ForeColor = ThemeColors.Slate700, BackColor = Color.White };
        private readonly Label status = new Label { AutoSize = false, Font = ThemeFonts.Medium(12), ForeColor = ThemeColors.Slate500, TextAlign = ContentAlignment.MiddleLeft };
        private readonly List<string> photos = new List<string>();
        private readonly List<Bitmap> thumbs = new List<Bitmap>();   // one square preview per picture, in the same order
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

            photoLabel.SetBounds(0, Ui.S(y), Ui.S(inner) - Ui.S(144), Ui.S(36));
            addPhoto.SetBounds(Ui.S(inner) - Ui.S(144), Ui.S(y), Ui.S(144), Ui.S(36));
            Content.Controls.Add(photoLabel);
            Content.Controls.Add(addPhoto);
            y += 44;

            strip.Slots = me.MaxMediaPerPost;
            strip.SetBounds(0, Ui.S(y), Ui.S(inner), Ui.S(112));
            Content.Controls.Add(strip);
            y += 124;

            if (this.routeIds.Count >= 2)
            {
                shareRoute.Text = "Đính kèm lộ trình đang mở (" + this.routeIds.Count + " địa điểm)";
                shareRoute.SetBounds(0, Ui.S(y), Ui.S(inner), Ui.S(28));
                Content.Controls.Add(shareRoute);
                y += 34;
            }
            if (!me.Trusted)
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
            strip.AddClicked += (s, e) => AddPhoto();
            strip.RemoveClicked += (s, index) => RemovePhoto(index);
            RefreshLabels();
            Arrange(y);
            FormClosed += (s, e) =>
            {
                closing.Cancel();
                foreach (Bitmap thumb in thumbs) thumb.Dispose();
                thumbs.Clear();
            };
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
            photoLabel.Text = photos.Count == 0 ? "Ảnh (tối đa " + me.MaxMediaPerPost + ")" : "Ảnh: " + photos.Count + " / " + me.MaxMediaPerPost + " (bấm ✕ để bỏ)";
            addPhoto.Enabled = photos.Count < me.MaxMediaPerPost && !busy;
            strip.SetThumbs(thumbs);
        }

        private void PickPlace()
        {
            IReadOnlyList<Poi> chosen = Dialogs.PickPlaces(this, "Gắn địa điểm", 1, place == null ? new string[0] : new[] { place.Id }, "Chọn");
            place = chosen != null && chosen.Count > 0 ? chosen[0] : null;
            RefreshLabels();
        }

        private void AddPhoto()
        {
            if (busy || photos.Count >= me.MaxMediaPerPost) return;
            string[] chosen;
            using (var dialog = new OpenFileDialog { Title = "Chọn ảnh", Filter = "Ảnh (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png", Multiselect = true })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                chosen = dialog.FileNames;
            }
            int unreadable = 0;
            foreach (string path in chosen)
            {
                if (photos.Count >= me.MaxMediaPerPost) break;
                if (photos.Contains(path)) continue;
                Bitmap thumb = Thumbnail(path, ThumbSide);   // the preview is made now, so a file that is not a picture is found at once
                if (thumb == null) { unreadable++; continue; }
                photos.Add(path);
                thumbs.Add(thumb);
            }
            Show(unreadable > 0 ? "Có " + unreadable + " tệp không đọc được như một ảnh, đã bỏ qua." : "", unreadable > 0);
            RefreshLabels();
        }

        private void RemovePhoto(int index)
        {
            if (busy || index < 0 || index >= photos.Count) return;
            photos.RemoveAt(index);
            thumbs[index].Dispose();
            thumbs.RemoveAt(index);
            Show("", false);
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

            busy = true;
            Ok.Enabled = false;
            RefreshLabels();
            try
            {
                CancellationToken ct = closing.Token;
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

        // The turn an EXIF orientation asks for (phones store a portrait picture sideways with a flag).
        private static RotateFlipType TurnOf(Image source)
        {
            if (Array.IndexOf(source.PropertyIdList, 0x0112) < 0) return RotateFlipType.RotateNoneFlipNone;
            switch (BitConverter.ToUInt16(source.GetPropertyItem(0x0112).Value, 0))
            {
                case 2: return RotateFlipType.RotateNoneFlipX;
                case 3: return RotateFlipType.Rotate180FlipNone;
                case 4: return RotateFlipType.RotateNoneFlipY;
                case 5: return RotateFlipType.Rotate90FlipX;
                case 6: return RotateFlipType.Rotate90FlipNone;
                case 7: return RotateFlipType.Rotate270FlipX;
                case 8: return RotateFlipType.Rotate270FlipNone;
                default: return RotateFlipType.RotateNoneFlipNone;
            }
        }

        /// <summary>A square preview (the middle of the picture, turned upright) of the given side; null when the file cannot be read as a picture. The caller disposes it.</summary>
        internal static Bitmap Thumbnail(string path, int side)
        {
            try
            {
                using (Image source = Image.FromFile(path))
                {
                    RotateFlipType turn = TurnOf(source);
                    int crop = Math.Min(source.Width, source.Height);
                    var middle = new Rectangle((source.Width - crop) / 2, (source.Height - crop) / 2, crop, crop);
                    var bitmap = new Bitmap(side, side, PixelFormat.Format24bppRgb);
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.Clear(Color.White);
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(source, new Rectangle(0, 0, side, side), middle, GraphicsUnit.Pixel);
                    }
                    if (turn != RotateFlipType.RotateNoneFlipNone) bitmap.RotateFlip(turn);
                    return bitmap;
                }
            }
            catch (Exception ex) when (ex is OutOfMemoryException || ex is ArgumentException || ex is IOException || ex is ExternalException || ex is UnauthorizedAccessException)
            {
                return null;   // not a picture, or not readable
            }
        }

        /// <summary>The picture turned upright, at most 1600 px on its longer side, as a JPEG; null when it cannot be read.</summary>
        internal static byte[] PrepareImage(string path)
        {
            try
            {
                using (Image source = Image.FromFile(path))
                {
                    RotateFlipType turn = TurnOf(source);
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

        /// <summary>
        /// A row of picture slots: each chosen picture is a preview with a round "✕" in its corner (press it to take the picture out); the next
        /// free slot is a dashed box with a "+" (press it to add pictures), the rest are faint empty boxes.
        /// </summary>
        private sealed class PhotoStrip : Control
        {
            private const int Gap = 10;
            private readonly List<Bitmap> items = new List<Bitmap>();
            private int slots = 4, hoverClose = -1, hoverAdd = -1;

            public event EventHandler<int> RemoveClicked;
            public event EventHandler AddClicked;

            public PhotoStrip()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = Color.White;
            }

            public int Slots
            {
                get { return slots; }
                set { slots = Math.Max(1, value); Invalidate(); }
            }

            public void SetThumbs(IEnumerable<Bitmap> thumbs)
            {
                items.Clear();
                items.AddRange(thumbs);
                Invalidate();
            }

            private Rectangle SlotRect(int index)
            {
                int gap = Ui.S(Gap);
                int width = (Width - gap * (slots - 1)) / slots;
                return new Rectangle(index * (width + gap), 0, width, Height - 1);
            }

            private Rectangle CloseRect(int index)
            {
                Rectangle slot = SlotRect(index);
                int d = Ui.S(26);
                return new Rectangle(slot.Right - d - Ui.S(6), slot.Y + Ui.S(6), d, d);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(BackColor);
                for (int i = 0; i < slots; i++)
                {
                    Rectangle r = SlotRect(i);
                    using (GraphicsPath path = Ui.RoundedRect(r, Ui.S(10)))
                    {
                        if (i < items.Count) PaintPicture(g, items[i], r, path, i);
                        else PaintEmpty(g, r, path, i == items.Count);
                    }
                }
            }

            private void PaintPicture(Graphics g, Bitmap picture, Rectangle r, GraphicsPath path, int index)
            {
                GraphicsState state = g.Save();
                g.SetClip(path, CombineMode.Intersect);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                // the middle of the square preview that fills the slot (a slot is nearly square)
                float aspect = r.Width / (float)r.Height;
                int srcW = aspect >= 1 ? picture.Width : (int)(picture.Width * aspect), srcH = aspect >= 1 ? (int)(picture.Height / aspect) : picture.Height;
                g.DrawImage(picture, r, (picture.Width - srcW) / 2, (picture.Height - srcH) / 2, srcW, srcH, GraphicsUnit.Pixel);
                g.Restore(state);
                using (var pen = new Pen(Color.FromArgb(50, 15, 23, 42))) g.DrawPath(pen, path);

                Rectangle close = CloseRect(index);
                bool hot = index == hoverClose;
                using (var shadow = new SolidBrush(Color.FromArgb(60, 15, 23, 42))) g.FillEllipse(shadow, close.X, close.Y + Ui.S(1), close.Width, close.Height);
                using (var fill = new SolidBrush(hot ? ThemeColors.Red500 : Color.FromArgb(235, 255, 255, 255))) g.FillEllipse(fill, close);
                int icon = (int)(close.Width * 0.6);
                g.DrawImage(IconFont.Get("close", icon, hot ? Color.White : ThemeColors.Slate700), close.X + (close.Width - icon) / 2, close.Y + (close.Height - icon) / 2, icon, icon);
            }

            private void PaintEmpty(Graphics g, Rectangle r, GraphicsPath path, bool next)
            {
                if (next && hoverAdd >= 0)
                {
                    using (var fill = new SolidBrush(ThemeColors.Sky50)) g.FillPath(fill, path);
                }
                using (var pen = new Pen(next ? ThemeColors.Primary : ThemeColors.Slate200, Math.Max(1.5f, Ui.S(1.5f))) { DashStyle = DashStyle.Dash })
                {
                    g.DrawPath(pen, path);
                }
                if (!next) return;
                int icon = Ui.S(26);
                g.DrawImage(IconFont.Get("add", icon, ThemeColors.Primary), r.X + (r.Width - icon) / 2, r.Y + r.Height / 2 - icon + Ui.S(4), icon, icon);
                TextRenderer.DrawText(g, "Thêm ảnh", ThemeFonts.Medium(12), new Rectangle(r.X, r.Y + r.Height / 2 + Ui.S(6), r.Width, Ui.S(22)), ThemeColors.Primary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                int close = -1, add = -1;
                for (int i = 0; i < items.Count; i++)
                {
                    Rectangle hit = CloseRect(i);
                    hit.Inflate(Ui.S(4), Ui.S(4));
                    if (hit.Contains(e.Location)) close = i;
                }
                if (items.Count < slots && SlotRect(items.Count).Contains(e.Location)) add = items.Count;
                if (close != hoverClose || add != hoverAdd)
                {
                    hoverClose = close;
                    hoverAdd = add;
                    Cursor = close >= 0 || add >= 0 ? Cursors.Hand : Cursors.Default;
                    Invalidate();
                }
                base.OnMouseMove(e);
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                hoverClose = hoverAdd = -1;
                Cursor = Cursors.Default;
                Invalidate();
                base.OnMouseLeave(e);
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                if (e.Button != MouseButtons.Left) return;
                if (hoverClose >= 0) RemoveClicked?.Invoke(this, hoverClose);
                else if (hoverAdd >= 0) AddClicked?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
