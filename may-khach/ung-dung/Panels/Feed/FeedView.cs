using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Panels.Feed
{
    /// <summary>
    /// The posts of the feed, drawn by one control at the screen's resolution (posts differ in height, so only the ones in view cost anything).
    /// Pictures are small thumbnails fetched one by one as they come into view and kept in a small cache; the host loads them (<see cref="ThumbNeeded"/>).
    /// </summary>
    internal sealed class FeedView : Control
    {
        private enum Hit { None, Like, Comments, More, Place, Route, Photo }

        private sealed class Layout
        {
            public int Width, Height;
            public Rectangle Body, Place, Route, Like, Comment, More;
            public Rectangle[] Tiles = new Rectangle[0];
        }

        private const TextFormatFlags TextFlags = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
        private const TextFormatFlags OneLine = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
        private const int MaxCachedThumbs = 80;

        private readonly List<FeedPostInfo> posts = new List<FeedPostInfo>();
        private readonly Dictionary<FeedPostInfo, Layout> layouts = new Dictionary<FeedPostInfo, Layout>();
        private readonly Dictionary<long, Image> thumbs = new Dictionary<long, Image>();
        private readonly Queue<long> thumbOrder = new Queue<long>();
        private readonly HashSet<long> asked = new HashSet<long>();
        private int scroll;
        private FeedPostInfo hoverPost;
        private Hit hoverHit;
        private string footer;

        public event Action<long> ThumbNeeded;
        public event EventHandler<FeedPostInfo> LikeClicked;
        public event EventHandler<FeedPostInfo> CommentsClicked;
        public event EventHandler<FeedPostInfo> MoreClicked;
        public event EventHandler<FeedPostInfo> RouteClicked;
        public event EventHandler<string> PlaceClicked;
        public event EventHandler<PhotoClick> PhotoClicked;

        /// <summary>Raised when the list is scrolled near its end: the host fetches the next page.</summary>
        public event EventHandler EndReached;

        internal sealed class PhotoClick : EventArgs
        {
            public FeedPostInfo Post;
            public int Index;
        }

        public FeedView()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            BackColor = Color.White;
        }

        public IReadOnlyList<FeedPostInfo> Posts => posts;

        /// <summary>The line under the last post ("Đang tải…", "Hết bài viết", or empty).</summary>
        public string Footer { get { return footer; } set { footer = value; Invalidate(); } }

        // ------------------------------------------------------------------ content

        public void Clear()
        {
            posts.Clear();
            layouts.Clear();
            scroll = 0;
            Invalidate();
        }

        public void Append(IEnumerable<FeedPostInfo> items)
        {
            foreach (FeedPostInfo post in items)
                if (posts.All(p => p.Id != post.Id)) posts.Add(post);   // a cursor never repeats a post, but a refresh may meet one again
            Invalidate();
        }

        public void Replace(FeedPostInfo before, FeedPostInfo after)
        {
            int i = posts.IndexOf(before);
            if (i < 0) return;
            posts[i] = after;
            layouts.Remove(before);
            Invalidate();
        }

        public void Remove(FeedPostInfo post)
        {
            posts.Remove(post);
            layouts.Remove(post);
            Invalidate();
        }

        public void SetThumb(long id, byte[] jpeg)
        {
            if (jpeg == null) return;
            Image image;
            try
            {
                using (var stream = new MemoryStream(jpeg)) image = new Bitmap(Image.FromStream(stream));
            }
            catch (ArgumentException) { return; }   // not a picture
            thumbs[id] = image;
            thumbOrder.Enqueue(id);
            while (thumbOrder.Count > MaxCachedThumbs)
            {
                long old = thumbOrder.Dequeue();
                Image dropped;
                if (thumbs.TryGetValue(old, out dropped)) { thumbs.Remove(old); asked.Remove(old); dropped.Dispose(); }
            }
            Invalidate();
        }

        // ------------------------------------------------------------------ layout

        private static int Pad => Ui.S(14);

        private static Font BodyFont => ThemeFonts.Regular(13);

        private Layout LayoutOf(FeedPostInfo post)
        {
            Layout l;
            if (layouts.TryGetValue(post, out l) && l.Width == Width) return l;
            l = new Layout { Width = Width };
            int inner = Width - Pad * 2 - Ui.S(12), y = Pad + Ui.S(36) + Ui.S(8);
            int left = Pad + Ui.S(6);
            if (!string.IsNullOrEmpty(post.Body))
            {
                int h = TextRenderer.MeasureText(post.Body, BodyFont, new Size(inner, int.MaxValue), TextFlags).Height;
                h = Math.Min(h, BodyFont.Height * 12);   // very long posts are cut here
                l.Body = new Rectangle(left, y, inner, h);
                y += h + Ui.S(8);
            }
            int count = Math.Min(4, post.MediaIds.Count);
            if (count > 0)
            {
                int gap = Ui.S(6), tile = (inner - (count - 1) * gap) / count, height = count == 1 ? Ui.S(210) : Math.Min(tile, Ui.S(140));
                l.Tiles = Enumerable.Range(0, count).Select(i => new Rectangle(left + i * (tile + gap), y, tile, height)).ToArray();
                y += height + Ui.S(8);
            }
            int chipX = left, chipH = Ui.S(28);
            if (post.Poi != null)
            {
                int w = Math.Min(inner, TextRenderer.MeasureText(post.Poi.Name ?? "", ThemeFonts.Medium(12), Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width + Ui.S(40));
                l.Place = new Rectangle(chipX, y, w, chipH);
                chipX += w + Ui.S(6);
            }
            if (post.Route.Count >= 2)
            {
                int w = Ui.S(170);
                if (chipX + w > left + inner && chipX > left) { chipX = left; y += chipH + Ui.S(6); }
                l.Route = new Rectangle(chipX, y, w, chipH);
            }
            if (!l.Place.IsEmpty || !l.Route.IsEmpty) y += chipH + Ui.S(8);
            int actionH = Ui.S(34);
            l.Like = new Rectangle(left - Ui.S(6), y, Ui.S(84), actionH);
            l.Comment = new Rectangle(l.Like.Right + Ui.S(6), y, Ui.S(96), actionH);
            l.More = new Rectangle(Width - Pad - Ui.S(44), y, Ui.S(40), actionH);
            y += actionH + Ui.S(6);
            l.Height = y;
            layouts[post] = l;
            return l;
        }

        private int TotalHeight()
        {
            int total = Ui.S(6);
            foreach (FeedPostInfo p in posts) total += LayoutOf(p).Height + Ui.S(8);
            return total + (string.IsNullOrEmpty(footer) ? 0 : Ui.S(48));
        }

        private int MaxScroll => Math.Max(0, TotalHeight() - Height);

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            layouts.Clear();
            scroll = Math.Min(scroll, MaxScroll);
        }

        // ------------------------------------------------------------------ input

        private Hit HitTest(Point p, out FeedPostInfo post, out int photoIndex)
        {
            post = null;
            photoIndex = -1;
            int y = Ui.S(6) - scroll;
            foreach (FeedPostInfo candidate in posts)
            {
                Layout l = LayoutOf(candidate);
                if (p.Y >= y && p.Y < y + l.Height)
                {
                    post = candidate;
                    var q = new Point(p.X, p.Y - y);
                    for (int i = 0; i < l.Tiles.Length; i++) if (l.Tiles[i].Contains(q)) { photoIndex = i; return Hit.Photo; }
                    if (l.Like.Contains(q)) return Hit.Like;
                    if (l.Comment.Contains(q)) return Hit.Comments;
                    if (l.More.Contains(q)) return Hit.More;
                    if (!l.Place.IsEmpty && l.Place.Contains(q)) return Hit.Place;
                    if (!l.Route.IsEmpty && l.Route.Contains(q)) return Hit.Route;
                    return Hit.None;
                }
                y += l.Height + Ui.S(8);
            }
            return Hit.None;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            FeedPostInfo post; int photo;
            Hit hit = HitTest(e.Location, out post, out photo);
            Cursor = hit == Hit.None ? Cursors.Default : Cursors.Hand;
            if (post != hoverPost || hit != hoverHit) { hoverPost = post; hoverHit = hit; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hoverPost = null; hoverHit = Hit.None; Cursor = Cursors.Default;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            scroll = Math.Max(0, Math.Min(MaxScroll, scroll - Math.Sign(e.Delta) * Ui.S(80)));
            Invalidate();
            if (scroll >= MaxScroll - Ui.S(200)) EndReached?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;
            FeedPostInfo post; int photo;
            switch (HitTest(e.Location, out post, out photo))
            {
                case Hit.Like: LikeClicked?.Invoke(this, post); break;
                case Hit.Comments: CommentsClicked?.Invoke(this, post); break;
                case Hit.More: MoreClicked?.Invoke(this, post); break;
                case Hit.Place: PlaceClicked?.Invoke(this, post.Poi.Id); break;
                case Hit.Route: RouteClicked?.Invoke(this, post); break;
                case Hit.Photo: PhotoClicked?.Invoke(this, new PhotoClick { Post = post, Index = photo }); break;
            }
        }

        // ------------------------------------------------------------------ painting

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            scroll = Math.Max(0, Math.Min(scroll, MaxScroll));
            int y = Ui.S(6) - scroll;
            foreach (FeedPostInfo post in posts)
            {
                Layout l = LayoutOf(post);
                if (y + l.Height >= 0 && y <= Height) PaintPost(g, post, l, y);
                y += l.Height + Ui.S(8);
            }
            if (!string.IsNullOrEmpty(footer))
                TextRenderer.DrawText(g, footer, ThemeFonts.Regular(12), new Rectangle(0, y, Width, Ui.S(48)), ThemeColors.Slate500,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            PaintScrollBar(g);
        }

        private void PaintPost(Graphics g, FeedPostInfo post, Layout l, int top)
        {
            var card = new Rectangle(Ui.S(6), top, Width - Ui.S(12), l.Height);
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(card.X + 0.5f, card.Y + 0.5f, card.Width - 1f, card.Height - 1f), Ui.S(ThemeMetrics.RadiusMd)))
            using (var pen = new Pen(ThemeColors.Slate200))
            {
                g.DrawPath(pen, path);
            }

            int avatar = Ui.S(36);
            var circle = new Rectangle(Pad + Ui.S(6), top + Pad, avatar, avatar);
            using (var brush = new SolidBrush(ThemeColors.Sky50)) g.FillEllipse(brush, circle);
            string initial = string.IsNullOrEmpty(post.AuthorName) ? "?" : post.AuthorName.Substring(0, 1).ToUpperInvariant();
            TextRenderer.DrawText(g, initial, ThemeFonts.Bold(14), circle, ThemeColors.Primary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            int textLeft = circle.Right + Ui.S(10), textWidth = Width - textLeft - Pad - Ui.S(6);
            TextRenderer.DrawText(g, post.AuthorName + (post.Mine ? " (bạn)" : ""), ThemeFonts.SemiBold(13), new Rectangle(textLeft, top + Pad, textWidth, Ui.S(18)), ThemeColors.Slate800, OneLine);
            string meta = post.TimeText + (post.Status == "pending" ? " · Chờ duyệt" : post.Status == "hidden" ? " · Đang bị ẩn" : "");
            TextRenderer.DrawText(g, meta, ThemeFonts.Regular(11), new Rectangle(textLeft, top + Pad + Ui.S(18), textWidth, Ui.S(18)),
                post.Status == "visible" ? ThemeColors.Slate500 : ThemeColors.Red500, OneLine);

            if (!l.Body.IsEmpty)
            {
                Rectangle body = l.Body;
                body.Offset(0, top);
                TextRenderer.DrawText(g, post.Body, BodyFont, body, ThemeColors.Slate800, TextFlags);
            }

            for (int i = 0; i < l.Tiles.Length; i++)
            {
                Rectangle tile = l.Tiles[i];
                tile.Offset(0, top);
                PaintThumb(g, post.MediaIds[i], tile);
            }

            if (!l.Place.IsEmpty) PaintChip(g, l.Place, top, post.Poi.Name, "place", hoverPost == post && hoverHit == Hit.Place);
            if (!l.Route.IsEmpty) PaintChip(g, l.Route, top, "Lộ trình " + post.Route.Count + " điểm", "route", hoverPost == post && hoverHit == Hit.Route);

            PaintAction(g, l.Like, top, post.LikeCount > 0 ? post.LikeCount.ToString() : "Thích", post.LikedByMe, true, hoverPost == post && hoverHit == Hit.Like);
            PaintAction(g, l.Comment, top, post.CommentCount > 0 ? post.CommentCount.ToString() : "Bình luận", false, false, hoverPost == post && hoverHit == Hit.Comments);
            Rectangle more = l.More;
            more.Offset(0, top);
            bool moreHot = hoverPost == post && hoverHit == Hit.More;
            using (var brush = new SolidBrush(moreHot ? ThemeColors.Primary : ThemeColors.Slate500))
                for (int i = -1; i <= 1; i++) g.FillEllipse(brush, more.X + more.Width / 2 - Ui.S(2) + i * Ui.S(8), more.Y + more.Height / 2 - Ui.S(2), Ui.S(4), Ui.S(4));
        }

        private void PaintThumb(Graphics g, long id, Rectangle tile)
        {
            using (GraphicsPath path = Ui.RoundedRect(tile, Ui.S(ThemeMetrics.RadiusSm)))
            {
                Image image;
                if (thumbs.TryGetValue(id, out image))
                {
                    // cover the tile (crop the longer side)
                    float scale = Math.Max((float)tile.Width / image.Width, (float)tile.Height / image.Height);
                    int w = (int)(image.Width * scale), h = (int)(image.Height * scale);
                    Region saved = g.Clip;
                    g.SetClip(path, CombineMode.Intersect);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(image, tile.X + (tile.Width - w) / 2, tile.Y + (tile.Height - h) / 2, w, h);
                    g.Clip = saved;
                }
                else
                {
                    using (var brush = new SolidBrush(ThemeColors.Slate100)) g.FillPath(brush, path);
                    if (asked.Add(id)) BeginInvoke(new Action(() => ThumbNeeded?.Invoke(id)));   // once per picture, and never inside the paint itself
                }
            }
        }

        private static void PaintChip(Graphics g, Rectangle chip, int top, string text, string icon, bool hot)
        {
            Rectangle box = chip;
            box.Offset(0, top);
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(box.X + 0.5f, box.Y + 0.5f, box.Width - 1f, box.Height - 1f), box.Height / 2f))
            using (var brush = new SolidBrush(hot ? ThemeColors.Sky50 : Color.White))
            using (var pen = new Pen(hot ? ThemeColors.Primary : ThemeColors.Slate300))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }
            int size = Ui.S(16);
            g.DrawImage(IconFont.Get(icon, size, ThemeColors.Primary), box.X + Ui.S(8), box.Y + (box.Height - size) / 2, size, size);
            TextRenderer.DrawText(g, text ?? "", ThemeFonts.Medium(12), new Rectangle(box.X + Ui.S(28), box.Y, box.Width - Ui.S(36), box.Height), ThemeColors.Slate700, OneLine);
        }

        private static void PaintAction(Graphics g, Rectangle area, int top, string text, bool active, bool heart, bool hot)
        {
            Rectangle box = area;
            box.Offset(0, top);
            Color color = active ? ThemeColors.Red500 : hot ? ThemeColors.Primary : ThemeColors.Slate600;
            int icon = Ui.S(16), x = box.X + Ui.S(6), cy = box.Y + box.Height / 2;
            if (heart) PaintHeart(g, new Rectangle(x, cy - icon / 2, icon, icon), color, active);
            else PaintBubble(g, new Rectangle(x, cy - icon / 2, icon, icon), color);
            TextRenderer.DrawText(g, text, ThemeFonts.Medium(12), new Rectangle(x + icon + Ui.S(6), box.Y, box.Width - icon - Ui.S(12), box.Height), color, OneLine);
        }

        private static void PaintHeart(Graphics g, Rectangle r, Color color, bool filled)
        {
            using (var path = new GraphicsPath())
            {
                float w = r.Width, h = r.Height, x = r.X, y = r.Y;
                path.AddBezier(x + w / 2, y + h, x - w * 0.15f, y + h * 0.45f, x + w * 0.12f, y - h * 0.1f, x + w / 2, y + h * 0.28f);
                path.AddBezier(x + w / 2, y + h * 0.28f, x + w * 0.88f, y - h * 0.1f, x + w * 1.15f, y + h * 0.45f, x + w / 2, y + h);
                path.CloseFigure();
                if (filled) using (var brush = new SolidBrush(color)) g.FillPath(brush, path);
                using (var pen = new Pen(color, Math.Max(1.2f, Ui.S(1.4f)))) g.DrawPath(pen, path);
            }
        }

        private static void PaintBubble(Graphics g, Rectangle r, Color color)
        {
            using (GraphicsPath path = Ui.RoundedRect(new RectangleF(r.X, r.Y + 1, r.Width, r.Height * 0.78f), Ui.S(4)))
            using (var pen = new Pen(color, Math.Max(1.2f, Ui.S(1.4f))))
            {
                g.DrawPath(pen, path);
                g.DrawLines(pen, new[] { new PointF(r.X + r.Width * 0.3f, r.Y + r.Height * 0.78f), new PointF(r.X + r.Width * 0.22f, r.Bottom), new PointF(r.X + r.Width * 0.5f, r.Y + r.Height * 0.78f) });
            }
        }

        private void PaintScrollBar(Graphics g)
        {
            int max = MaxScroll;
            if (max <= 0) return;
            int track = Height - Ui.S(8);
            int thumb = Math.Max(Ui.S(28), (int)((long)track * Height / (Height + max)));
            int top = Ui.S(4) + (int)((long)(track - thumb) * Math.Min(scroll, max) / max);
            using (GraphicsPath path = Ui.RoundedRect(new Rectangle(Width - Ui.S(8), top, Ui.S(5), thumb), Ui.S(3)))
            using (var brush = new SolidBrush(ThemeColors.Slate300)) g.FillPath(brush, path);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) foreach (Image image in thumbs.Values) image.Dispose();
            base.Dispose(disposing);
        }
    }
}
