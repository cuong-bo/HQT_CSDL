using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Hue_travel.MapEngine;
using Hue_travel.Models;
using Hue_travel.Services.Data;
using Hue_travel.Theme;

namespace Hue_travel.Panels.PoiIdentify
{
    /// <summary>
    /// The detail card of a place: its photos on top (large photo, a strip of small ones, full-size viewer on click), the
    /// group, the name, address / hours / price / phone / website, and the actions (add to route; favourite, claim, report
    /// when signed in). Everything is drawn at the screen's resolution, like the sign-in screen.
    /// </summary>
    public sealed class PoiIdentifyCard : FloatingPanelForm
    {
        private const int DesignWidth = 400;

        /// <summary>The text part under the photos; painted so it wraps and sizes itself to the place.</summary>
        private sealed class InfoView : Control
        {
            private Poi poi;

            public InfoView()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = Color.White;
            }

            public void SetPoi(Poi value)
            {
                poi = value;
                Invalidate();
            }

            public int MeasureHeight(int width) => poi == null ? 0 : Run(null, width);

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.Clear(BackColor);
                if (poi != null) Run(e.Graphics, Width);
            }

            // Lays the text out from the top and paints it when a Graphics is given; returns the height used.
            private int Run(Graphics g, int width)
            {
                int pad = Ui.S(18), inner = width - pad * 2, y = Ui.S(16);
                const TextFormatFlags wrap = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;

                // group chip
                string group = PoiMarkerIcons.NameOf(poi.Category);
                Font chipFont = ThemeFonts.SemiBold(12);
                int chipText = TextRenderer.MeasureText(group, chipFont, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
                var chip = new Rectangle(pad, y, Ui.S(12) + Ui.S(18) + Ui.S(6) + chipText + Ui.S(14), Ui.S(28));
                if (g != null)
                {
                    Color color = PoiMarkerStyle.ColorOf(poi.Category);
                    using (GraphicsPath path = Ui.RoundedRect(chip, chip.Height / 2f))
                    using (var fill = new SolidBrush(Color.FromArgb(28, color))) g.FillPath(fill, path);
                    g.DrawImage(PoiMarkerIcons.Badge(poi.Category, Ui.S(20)), chip.X + Ui.S(7), chip.Y + (chip.Height - Ui.S(20)) / 2, Ui.S(20), Ui.S(20));
                    TextRenderer.DrawText(g, group, chipFont, new Rectangle(chip.X + Ui.S(32), chip.Y, chipText + Ui.S(6), chip.Height), Ui.Lerp(color, Color.Black, 0.25f),
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                }
                y = chip.Bottom + Ui.S(10);

                // name (up to three lines)
                Font nameFont = ThemeFonts.Bold(20);
                int nameHeight = Math.Min(TextRenderer.MeasureText(poi.Name ?? "", nameFont, new Size(inner, int.MaxValue), wrap).Height, nameFont.Height * 3);
                if (g != null) TextRenderer.DrawText(g, poi.Name ?? "", nameFont, new Rectangle(pad, y, inner, nameHeight), ThemeColors.Slate900, wrap);
                y += nameHeight + Ui.S(4);

                if (!string.IsNullOrWhiteSpace(poi.TypeLabel) && !string.Equals(poi.TypeLabel, group, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (g != null) TextRenderer.DrawText(g, poi.TypeLabel, ThemeFonts.Regular(13), new Rectangle(pad, y, inner, Ui.S(20)), ThemeColors.Slate500,
                        TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
                    y += Ui.S(22);
                }
                y += Ui.S(10);
                if (g != null) using (var pen = new Pen(ThemeColors.Slate100)) g.DrawLine(pen, pad, y, width - pad, y);
                y += Ui.S(12);

                // facts: icon, a small caption and the value
                y = Row(g, "pin_drop", "Địa chỉ", Clean(poi.Address), pad, inner, y);
                y = Row(g, "schedule", HoursCaption(poi.Category), Clean(poi.OpeningHours), pad, inner, y);
                y = Row(g, "sell", PriceCaption(poi.Category), Clean(poi.TicketPrice), pad, inner, y);
                y = Row(g, "call", "Điện thoại", HasDigit(poi.Phone) ? Clean(poi.Phone) : null, pad, inner, y);
                y = Row(g, "open_in_new", "Trang web", LooksLikeAddress(poi.Website) ? Clean(poi.Website) : null, pad, inner, y);

                string summary = Clean(!string.IsNullOrWhiteSpace(poi.Summary) ? poi.Summary : poi.Description);
                bool repeatsType = !string.IsNullOrEmpty(summary) && (string.Equals(summary, poi.TypeLabel, StringComparison.CurrentCultureIgnoreCase) || string.Equals(summary, group, StringComparison.CurrentCultureIgnoreCase));
                if (!string.IsNullOrWhiteSpace(summary) && !repeatsType)
                {
                    Font font = ThemeFonts.Regular(13);
                    int h = Math.Min(TextRenderer.MeasureText(summary, font, new Size(inner, int.MaxValue), wrap).Height, font.Height * 4);
                    if (g != null) TextRenderer.DrawText(g, summary, font, new Rectangle(pad, y + Ui.S(4), inner, h), ThemeColors.Slate600, wrap);
                    y += h + Ui.S(10);
                }
                return y + Ui.S(4);
            }

            // Several lines of the data (e.g. two ticket prices) become one line of text with a separator.
            private static string Clean(string text) =>
                string.IsNullOrWhiteSpace(text) ? null : text.Replace("\r\n", " · ").Replace("\n", " · ").Replace("\r", " · ").Trim();

            private static bool HasDigit(string text) => !string.IsNullOrEmpty(text) && text.Any(char.IsDigit);

            // A web address (not a free-text note that the data happens to carry in that column).
            private static bool LooksLikeAddress(string text)
            {
                if (string.IsNullOrWhiteSpace(text) || text.Contains(" ")) return false;
                return text.StartsWith("http", StringComparison.OrdinalIgnoreCase) || text.StartsWith("www.", StringComparison.OrdinalIgnoreCase);
            }

            private int Row(Graphics g, string icon, string caption, string value, int pad, int inner, int y)
            {
                if (string.IsNullOrWhiteSpace(value)) return y;
                int iconPx = Ui.S(18), textLeft = pad + iconPx + Ui.S(12), textWidth = inner - iconPx - Ui.S(12);
                Font valueFont = ThemeFonts.Medium(13);
                int valueHeight = Math.Min(TextRenderer.MeasureText(value, valueFont, new Size(textWidth, int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Height, valueFont.Height * 2);
                if (g != null)
                {
                    g.DrawImage(IconFont.Get(icon, iconPx, ThemeColors.Primary), pad, y + Ui.S(5), iconPx, iconPx);
                    TextRenderer.DrawText(g, caption, ThemeFonts.Regular(11), new Rectangle(textLeft, y, textWidth, Ui.S(16)), ThemeColors.Slate500,
                        TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
                    TextRenderer.DrawText(g, value, valueFont, new Rectangle(textLeft, y + Ui.S(16), textWidth, valueHeight), ThemeColors.Slate800,
                        TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
                }
                return y + Ui.S(16) + valueHeight + Ui.S(10);
            }
        }

        private readonly PhotoGallery gallery = new PhotoGallery();
        private readonly InfoView info = new InfoView();
        private readonly IconButton btnClose = new IconButton();
        private readonly IconButton btnRoute = new IconButton();
        private readonly IconButton btnFavorite = AccountButton("Yêu thích", "star");
        private readonly IconButton btnClaim = AccountButton("Chủ quản", "verified_user");
        private readonly IconButton btnReport = AccountButton("Báo sai", "campaign");
        private Poi shown;
        private bool signedIn;

        /// <summary>Raised by the buttons that need an account; the host knows who is signed in.</summary>
        public event EventHandler<Poi> FavoriteToggled;
        public event EventHandler<Poi> ClaimRequested;
        public event EventHandler<Poi> ReportRequested;
        public event EventHandler<Poi> AddToRouteRequested;

        /// <summary>Ids of the signed-in user's favourites, so the star shows the right state.</summary>
        public ISet<string> FavoriteIds { get; set; } = new HashSet<string>();

        public PoiIdentifyCard()
        {
            Size = new Size(DesignWidth, 600);
            BackColor = Color.White;

            btnClose.AutoWidth = false;
            btnClose.IconName = "close";
            btnClose.IconSize = 16;
            btnClose.IconColor = ThemeColors.Slate700;
            btnClose.FillColor = Color.FromArgb(225, 255, 255, 255);
            btnClose.HoverFillColor = Color.White;
            btnClose.Radius = 17;
            btnClose.Click += (s, e) => Hide();

            btnRoute.AutoWidth = false;
            btnRoute.Text = "Thêm vào lộ trình tham quan";
            btnRoute.IconName = "add";
            btnRoute.IconSize = 18;
            btnRoute.Gap = 8;
            btnRoute.ForeColor = Color.White;
            btnRoute.IconColor = Color.White;
            btnRoute.FillColor = ThemeColors.Primary;
            btnRoute.HoverFillColor = ThemeColors.PrimaryContainer;
            btnRoute.Radius = 12;
            btnRoute.Elevated = true;

            gallery.HeroClicked += (s, i) => PhotoViewer.ShowOver(Owner ?? (Form)this, gallery.Paths, i, shown == null ? "" : shown.Name);
            btnRoute.Click += (s, e) => { if (shown != null) AddToRouteRequested?.Invoke(this, shown); };
            btnFavorite.Click += (s, e) => { if (shown != null) FavoriteToggled?.Invoke(this, shown); };
            btnClaim.Click += (s, e) => { if (shown != null) ClaimRequested?.Invoke(this, shown); };
            btnReport.Click += (s, e) => { if (shown != null) ReportRequested?.Invoke(this, shown); };
            foreach (IconButton b in new[] { btnFavorite, btnClaim, btnReport }) b.Visible = false;

            Controls.Add(info);
            Controls.Add(gallery);
            Controls.Add(btnRoute);
            Controls.Add(btnFavorite);
            Controls.Add(btnClaim);
            Controls.Add(btnReport);
            Controls.Add(btnClose);   // last: drawn above the photo
        }

        public void ShowPoi(Poi poi)
        {
            if (poi == null) return;
            shown = poi;
            gallery.SetPhotos(PoiPhotos.PathsFor(poi.Id), poi.Category);
            info.SetPoi(poi);
            RefreshAccountButtons();
            LayoutCard();
            Visible = true;
        }

        /// <summary>Shows the favourite / claim / report buttons only to a signed-in user.</summary>
        public bool SignedIn
        {
            get { return signedIn; }
            set { signedIn = value; RefreshAccountButtons(); LayoutCard(); }
        }

        public void RefreshAccountButtons()
        {
            bool visible = signedIn && shown != null;
            btnFavorite.Visible = btnClaim.Visible = btnReport.Visible = visible;
            if (!visible) return;
            bool favorite = FavoriteIds != null && FavoriteIds.Contains(shown.Id);
            btnFavorite.Text = favorite ? "Bỏ thích" : "Yêu thích";
            btnFavorite.IconColor = favorite ? ThemeColors.Amber600 : ThemeColors.Primary;
        }

        // Stacks the parts from the top and sizes the card to them; a card that is open keeps its bottom edge.
        private void LayoutCard()
        {
            if (shown == null) return;
            int width = ClientSize.Width;
            if (width <= 0) return;
            gallery.SetBounds(0, 0, width, gallery.WantedHeight);
            int closeSize = Ui.S(34);
            btnClose.SetBounds(width - closeSize - Ui.S(10), Ui.S(10), closeSize, closeSize);

            info.SetBounds(0, gallery.Bottom, width, info.MeasureHeight(width));
            int pad = Ui.S(16);
            btnRoute.Font = ThemeFonts.SemiBold(14);
            btnRoute.SetBounds(pad, info.Bottom, width - pad * 2, Ui.S(48));
            int bottom = btnRoute.Bottom;

            if (signedIn)
            {
                int gap = Ui.S(8), buttonWidth = (width - pad * 2 - gap * 2) / 3, y = btnRoute.Bottom + Ui.S(10), height = Ui.S(38);
                btnFavorite.SetBounds(pad, y, buttonWidth, height);
                btnClaim.SetBounds(pad + buttonWidth + gap, y, buttonWidth, height);
                btnReport.SetBounds(pad + (buttonWidth + gap) * 2, y, buttonWidth, height);
                bottom = y + height;
            }

            int wanted = bottom + Ui.S(16);
            if (Height == wanted) return;
            int oldBottom = Bottom;
            Height = wanted;
            if (Visible) Top = oldBottom - Height;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (shown != null) LayoutCard();
        }

        private static IconButton AccountButton(string text, string icon)
        {
            return new IconButton
            {
                AutoWidth = false, Text = text, IconName = icon, IconSize = 16, Font = ThemeFonts.Medium(12), Radius = 10, Gap = 6, HorizontalPadding = 6,
                ForeColor = ThemeColors.Slate700, IconColor = ThemeColors.Primary, FillColor = Color.White,
                HoverFillColor = ThemeColors.Slate100, BorderColor = ThemeColors.Slate300,
            };
        }

        private static string HoursCaption(PoiCategory category)
        {
            return category == PoiCategory.Stay ? "Giờ nhận / trả phòng" : category == PoiCategory.Transport || category == PoiCategory.Service ? "Giờ hoạt động" : "Giờ mở cửa";
        }

        private static string PriceCaption(PoiCategory category)
        {
            switch (category)
            {
                case PoiCategory.Stay: return "Giá phòng";
                case PoiCategory.Service: return "Giá dịch vụ";
                case PoiCategory.Transport: return "Giá";
                default: return "Giá vé";
            }
        }
    }
}
