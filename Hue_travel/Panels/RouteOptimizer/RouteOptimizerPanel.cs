using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.MapEngine;
using Hue_travel.Models;
using Hue_travel.Panels.Shared;
using Hue_travel.Panels.SearchPoi;
using Hue_travel.Services.Data;
using Hue_travel.Services.Online;
using Hue_travel.Services.Routing;
using Hue_travel.Theme;

namespace Hue_travel.Panels.RouteOptimizer
{
    /// <summary>
    /// "Lộ trình": the visitor picks a start (a place suggested from the list, a typed name or position, or a point clicked on the
    /// map), builds the itinerary from the full list of places and puts it in order, and only when "Tìm đường" is pressed the
    /// route is computed and shown on the map, leg by leg, with a car driving along it.
    /// </summary>
    public sealed class RouteOptimizerPanel : FloatingPanelForm
    {
        private const int MaxStops = 12;
        private const int SuggestionCount = 4;
        private const int PickerRows = 5;
        private const int PickerMaxPlaces = 300;
        private static readonly Regex Coordinates = new Regex(@"^\s*(-?\d+(?:[.,]\d+)?)\s*(?:[;]|,\s|\s)\s*(-?\d+(?:[.,]\d+)?)\s*$", RegexOptions.Compiled);

        private sealed class StartSpot
        {
            public string Name;
            public MapXY Point;
            public double Longitude, Latitude;
            public Poi Place;   // set when the start is one of the listed places
        }

        // ------------------------------------------------------------------ the leg list of a computed trip

        private sealed class LegList : Control
        {
            private const int RowHeight = 58;
            private readonly PressFeedback press;
            private IReadOnlyList<TripLeg> legs = new TripLeg[0];
            private int active = -1;
            private int hover = -1;

            public event EventHandler<int> LegClicked;

            public LegList()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = Color.White;
                Cursor = Cursors.Hand;
                press = new PressFeedback(this);
            }

            public int WantedHeight => legs.Count * Ui.S(RowHeight);

            public void SetLegs(IReadOnlyList<TripLeg> value)
            {
                legs = value ?? new TripLeg[0];
                active = -1;
                Invalidate();
            }

            public void SetActive(int index)
            {
                if (active == index) return;
                active = index;
                Invalidate();
            }

            private int RowAt(Point p)
            {
                if (p.Y < 0) return -1;
                int row = p.Y / Math.Max(1, Ui.S(RowHeight));
                return row < legs.Count ? row : -1;
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                int row = RowAt(e.Location);
                if (row != hover) { hover = row; Invalidate(); }
                base.OnMouseMove(e);
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                hover = -1;
                press.Up();
                Invalidate();
                base.OnMouseLeave(e);
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                int row = RowAt(e.Location);
                if (e.Button == MouseButtons.Left && row >= 0) press.Down(row, e.Location);
                base.OnMouseDown(e);
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                press.Up();
                base.OnMouseUp(e);
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                int row = RowAt(e.Location);
                if (e.Button == MouseButtons.Left && row >= 0) LegClicked?.Invoke(this, row);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) press.Dispose();
                base.Dispose(disposing);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(BackColor);
                int rowPx = Ui.S(RowHeight);
                const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
                for (int i = 0; i < legs.Count; i++)
                {
                    TripLeg leg = legs[i];
                    int top = i * rowPx;
                    var rowRect = new Rectangle(0, top, Width, rowPx);
                    Color rest = i == active ? ThemeColors.Sky50 : (i == hover ? ThemeColors.Slate50 : Color.Transparent);
                    Color fill = press.Fill(i, rest, ThemeColors.Slate200);
                    if (fill.A > 0) using (var brush = new SolidBrush(fill)) g.FillRectangle(brush, rowRect);
                    Color color = RoutePalette.ColorOf(i);
                    if (i == active) using (var bar = new SolidBrush(color)) g.FillRectangle(bar, 0, top + Ui.S(8), Ui.S(4), rowPx - Ui.S(16));

                    int badge = Ui.S(30);
                    var badgeRect = new Rectangle(Ui.S(14), top + (rowPx - badge) / 2, badge, badge);
                    using (var brush = new SolidBrush(color)) g.FillEllipse(brush, badgeRect);
                    TextRenderer.DrawText(g, (i + 1).ToString(), ThemeFonts.Bold(13), badgeRect, Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

                    int textLeft = badgeRect.Right + Ui.S(12), textWidth = Width - textLeft - Ui.S(14);
                    TextRenderer.DrawText(g, ShortName(leg.FromName) + "  →  " + ShortName(leg.ToName), ThemeFonts.SemiBold(13), new Rectangle(textLeft, top + Ui.S(10), textWidth, Ui.S(20)), ThemeColors.Slate800, flags);
                    TextRenderer.DrawText(g, FormatKm(leg.Meters / 1000) + "  ·  " + FormatMinutes((int)Math.Round(leg.Seconds / 60)), ThemeFonts.Regular(12),
                        new Rectangle(textLeft, top + Ui.S(32), textWidth, Ui.S(18)), ThemeColors.Slate500, flags);
                    if (i < legs.Count - 1) using (var pen = new Pen(ThemeColors.Slate100)) g.DrawLine(pen, textLeft, top + rowPx - 1, Width - Ui.S(14), top + rowPx - 1);
                }
            }
        }

        // ------------------------------------------------------------------ the totals of a computed trip

        private sealed class SummaryCard : Control
        {
            public string Distance = "", Duration = "", Note = "";
            public bool Estimate;

            public SummaryCard()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = Color.White;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(BackColor);
                var card = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath path = Ui.RoundedRect(card, Ui.S(12)))
                {
                    using (var fill = new SolidBrush(ThemeColors.Sky50)) g.FillPath(fill, path);
                    using (var pen = new Pen(ThemeColors.Sky200)) g.DrawPath(pen, path);
                }
                int half = Width / 2;
                DrawFigure(g, "Quãng đường", Distance, new Rectangle(Ui.S(16), Ui.S(12), half - Ui.S(16), Ui.S(46)));
                DrawFigure(g, "Thời gian di chuyển", Duration, new Rectangle(half, Ui.S(12), half - Ui.S(16), Ui.S(46)));
                TextRenderer.DrawText(g, Note, ThemeFonts.Regular(12), new Rectangle(Ui.S(16), Ui.S(62), Width - Ui.S(32), Height - Ui.S(68)),
                    Estimate ? ThemeColors.Amber700 : ThemeColors.Slate600, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            }

            private static void DrawFigure(Graphics g, string caption, string value, Rectangle area)
            {
                TextRenderer.DrawText(g, caption, ThemeFonts.Regular(11), new Rectangle(area.X, area.Y, area.Width, Ui.S(16)), ThemeColors.Slate500,
                    TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
                TextRenderer.DrawText(g, value, ThemeFonts.Bold(18), new Rectangle(area.X, area.Y + Ui.S(18), area.Width, Ui.S(28)), ThemeColors.Sky800,
                    TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            }
        }

        // ------------------------------------------------------------------ state

        private readonly PanelHeader header = new PanelHeader { IconName = "route", Text = "Lộ trình tham quan", Prominent = true };
        private readonly NoAutoScrollPanel body = new NoAutoScrollPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };

        private readonly Label lblStartTitle = Caption("Điểm bắt đầu");
        private readonly InputField startBox = new InputField { IconName = "pin_drop" };
        private readonly IconButton btnUseText = new IconButton();
        private readonly Label lblStartHint = Hint("Gõ tên địa điểm hoặc toạ độ (vĩ độ, kinh độ), hoặc chọn trực tiếp trên bản đồ. Các địa điểm bên dưới chỉ là gợi ý.");
        private readonly IconButton btnPickOnMap = new IconButton();
        private readonly IconButton btnClearStart = new IconButton();
        private readonly Label lblSuggest = Hint("Gợi ý điểm bắt đầu");
        private readonly PoiRowList suggestList = new PoiRowList { StealFocus = false, EmptyText = "Không có địa điểm nào khớp." };

        private readonly Label lblStopsTitle = Caption("Điểm đến");
        private readonly Label lblStopsCount = Hint("");
        private readonly StopList stopList = new StopList();
        private readonly IconButton btnAddStops = new IconButton();
        private readonly InputField pickerSearch = new InputField { IconName = "search" };
        private readonly Label lblPickerCount = Hint("");
        private readonly PoiRowList pickerList = new PoiRowList { ShowAddButton = true, StealFocus = false, EmptyText = "Không có địa điểm nào khớp." };

        private readonly ToggleSwitch tglOptimize = new ToggleSwitch();
        private readonly Label lblOptimize = new Label { AutoSize = false, Font = ThemeFonts.Medium(13), ForeColor = ThemeColors.Slate700, TextAlign = ContentAlignment.MiddleLeft, Text = "Tự sắp xếp thứ tự ngắn nhất", Cursor = Cursors.Hand };
        private readonly Label lblOptimizeHint = Hint("Tắt: đi đúng thứ tự bạn đã sắp xếp. Bật: máy chủ chọn thứ tự đi nhanh nhất.");
        private readonly IconButton btnFind = new IconButton();

        private readonly SummaryCard summary = new SummaryCard();
        private readonly Label lblLegsTitle = Caption("Các chặng di chuyển");
        private readonly LegList legList = new LegList();
        private readonly IconButton btnPlay = new IconButton();
        private readonly IconButton btnReplay = new IconButton();
        private readonly IconButton btnFit = new IconButton();
        private readonly IconButton btnSave = new IconButton();
        private readonly IconButton btnPrint = new IconButton();

        private readonly List<Poi> itinerary = new List<Poi>();
        private readonly List<Control> dividers = new List<Control>();
        private StartSpot start;
        private List<PoiRow> suggestRows = new List<PoiRow>();
        private bool pickerOpen;
        private bool pickingOnMap;
        private bool settingStartText;
        private bool busy;
        private bool carPlaying;
        private TripPlan plan;
        private ApiRouteService routeService;
        private int planVersion;
        private CancellationTokenSource planCts;
        private bool routeShown;

        /// <summary>The route panel asks the server for road routes when this is set (otherwise it estimates).</summary>
        public ApiClient Api
        {
            set { routeService = value == null ? null : new ApiRouteService(value); }
        }

        /// <summary>Converts a position in degrees to map metres (the map knows the projection).</summary>
        public Func<double, double, MapXY?> Projector { get; set; }

        /// <summary>Raised with the computed trip, to draw it on the map.</summary>
        public event EventHandler<TripPlan> RouteComputed;

        /// <summary>Raised when the drawn trip no longer matches the selection.</summary>
        public event EventHandler RouteInvalidated;

        /// <summary>Raised when "Chọn trên bản đồ" is switched on (true) or off (false).</summary>
        public event EventHandler<bool> PickStartRequested;

        public event EventHandler<int> LegSelected;
        public event EventHandler PlayRequested;
        public event EventHandler PauseRequested;
        public event EventHandler ReplayRequested;
        public event EventHandler FitRequested;

        public RouteOptimizerPanel()
        {
            Size = new Size(384, 640);
            header.CloseClicked += (s, e) => RaiseCloseRequested();

            StyleSecondary(btnUseText, "Dùng", "check");
            StyleSecondary(btnPickOnMap, "Chọn trên bản đồ", "place");
            StyleSecondary(btnClearStart, "Xoá", "close");
            StyleSecondary(btnAddStops, "Thêm điểm đến từ danh sách địa điểm", "add");
            StyleSecondary(btnPlay, "Chạy xe", "play_arrow");
            StyleSecondary(btnReplay, "Chạy lại", "refresh");
            StyleSecondary(btnFit, "Toàn tuyến", "route");
            StyleSecondary(btnSave, "Lưu", "save_alt");
            StyleSecondary(btnPrint, "In", "print");
            StylePrimary(btnFind, "Tìm đường", "directions");

            lblOptimize.Click += (s, e) => tglOptimize.Toggle();

            foreach (Control c in new Control[]
            {
                lblStartTitle, startBox, btnUseText, lblStartHint, btnPickOnMap, btnClearStart, lblSuggest, suggestList,
                lblStopsTitle, lblStopsCount, stopList, btnAddStops, pickerSearch, lblPickerCount, pickerList,
                tglOptimize, lblOptimize, lblOptimizeHint, btnFind,
                summary, lblLegsTitle, legList, btnPlay, btnReplay, btnFit, btnSave, btnPrint,
            }) body.Controls.Add(c);
            for (int i = 0; i < 3; i++)
            {
                var line = new Panel { BackColor = ThemeColors.Slate100, Height = 1 };
                dividers.Add(line);
                body.Controls.Add(line);
            }
            Controls.Add(body);
            Controls.Add(header);

            WireEvents();
            ReloadSuggestions();
            ReloadPicker();
            RefreshStops();
            ShowResults(false);
            PoiCatalog.Changed += (s, e) => { if (!IsDisposed) { ReloadSuggestions(); ReloadPicker(); } };
        }

        // ------------------------------------------------------------------ public use

        /// <summary>Place ids of the itinerary in visiting order, for saving it to the account.</summary>
        public IReadOnlyList<string> CurrentStopIds => itinerary.Select(p => p.Id).ToList();

        /// <summary>Adds a place to the itinerary (the card's "Thêm vào lộ trình"); false when it has no position on the map.</summary>
        public bool TrySelectStop(Poi poi)
        {
            if (poi == null || (poi.X == 0 && poi.Y == 0)) return false;
            AddStop(poi);
            return true;
        }

        /// <summary>Sets the start to a point picked on the map.</summary>
        public void SetStartFromMap(MapXY point, double longitude, double latitude)
        {
            SetPicking(false, false);
            string label = "Vị trí đã chọn (" + latitude.ToString("0.0000", CultureInfo.InvariantCulture) + ", " + longitude.ToString("0.0000", CultureInfo.InvariantCulture) + ")";
            SetStart(new StartSpot { Name = label, Point = point, Longitude = longitude, Latitude = latitude });
        }

        /// <summary>The car started or stopped on the map.</summary>
        public void SetCarPlaying(bool playing)
        {
            carPlaying = playing;
            btnPlay.Text = playing ? "Tạm dừng" : "Chạy xe";
            btnPlay.IconName = playing ? "pause" : "play_arrow";
        }

        /// <summary>The car entered another leg: that leg is highlighted in the list.</summary>
        public void SetActiveLeg(int leg) => legList.SetActive(leg);

        // ------------------------------------------------------------------ building the controls

        private static Label Caption(string text) =>
            new Label { AutoSize = false, Text = text, Font = ThemeFonts.Bold(15), ForeColor = ThemeColors.Slate800, TextAlign = ContentAlignment.MiddleLeft };

        private static Label Hint(string text) =>
            new Label { AutoSize = false, Text = text, Font = ThemeFonts.Regular(12), ForeColor = ThemeColors.Slate500, TextAlign = ContentAlignment.TopLeft };

        private static void StyleSecondary(IconButton b, string text, string icon)
        {
            b.AutoWidth = false;
            b.Text = text;
            b.IconName = icon;
            b.IconSize = 16;
            b.Gap = 6;
            b.HorizontalPadding = 8;
            b.Font = ThemeFonts.Medium(13);
            b.Radius = 10;
            b.ForeColor = ThemeColors.Slate700;
            b.IconColor = ThemeColors.Primary;
            b.FillColor = Color.White;
            b.HoverFillColor = ThemeColors.Slate100;
            b.BorderColor = ThemeColors.Slate300;
        }

        private static void StylePrimary(IconButton b, string text, string icon)
        {
            b.AutoWidth = false;
            b.Text = text;
            b.IconName = icon;
            b.IconSize = 18;
            b.Gap = 8;
            b.Font = ThemeFonts.SemiBold(14);
            b.Radius = 12;
            b.ForeColor = Color.White;
            b.IconColor = Color.White;
            b.FillColor = ThemeColors.Primary;
            b.HoverFillColor = ThemeColors.PrimaryContainer;
            b.Elevated = true;
        }

        private void WireEvents()
        {
            startBox.TextChanged += (s, e) => OnStartTextChanged();
            btnUseText.Click += (s, e) => UseTypedStart();
            btnPickOnMap.Click += (s, e) => SetPicking(!pickingOnMap, true);
            btnClearStart.Click += (s, e) => { SetPicking(false, true); ClearStart(); };
            suggestList.RowClicked += (s, row) => SetStart(FromPlace(row.Poi));

            stopList.MoveRequested += (s, move) => MoveStop(move.Key, move.Value);
            stopList.RemoveRequested += (s, index) => RemoveStop(index);
            btnAddStops.Click += (s, e) => { pickerOpen = !pickerOpen; if (pickerOpen) ReloadPicker(); LayoutAll(); };
            pickerSearch.TextChanged += (s, e) => ReloadPicker();
            pickerList.AddClicked += (s, row) => TogglePlace(row);
            pickerList.RowClicked += (s, row) => TogglePlace(row);

            tglOptimize.CheckedChanged += (s, e) => InvalidateTrip();
            btnFind.Click += async (s, e) => await FindRouteAsync();

            legList.LegClicked += (s, i) => { LegSelected?.Invoke(this, i); legList.SetActive(i); };
            btnPlay.Click += (s, e) => { if (carPlaying) PauseRequested?.Invoke(this, EventArgs.Empty); else PlayRequested?.Invoke(this, EventArgs.Empty); };
            btnReplay.Click += (s, e) => ReplayRequested?.Invoke(this, EventArgs.Empty);
            btnFit.Click += (s, e) => FitRequested?.Invoke(this, EventArgs.Empty);
            btnSave.Click += (s, e) => SaveItinerary();
            btnPrint.Click += (s, e) => PrintItinerary();
        }

        // ------------------------------------------------------------------ the start

        private StartSpot FromPlace(Poi poi) =>
            new StartSpot { Name = poi.Name, Point = new MapXY(poi.X, poi.Y), Longitude = poi.Longitude, Latitude = poi.Latitude, Place = poi };

        private void SetStart(StartSpot spot)
        {
            start = spot;
            settingStartText = true;
            startBox.Text = spot == null ? "" : spot.Name;
            settingStartText = false;
            InvalidateTrip();
            ReloadSuggestions();
            LayoutAll();
        }

        private void ClearStart() => SetStart(null);

        // Typing makes the old start void; the suggestions follow the text.
        private void OnStartTextChanged()
        {
            if (settingStartText) return;
            if (start != null) { start = null; InvalidateTrip(); }
            ReloadSuggestions();
            LayoutAll();
        }

        // "Dùng": the typed position, or else the first place that matches the typed name.
        private void UseTypedStart()
        {
            string text = startBox.Text.Trim();
            if (text.Length == 0) return;
            Match m = Coordinates.Match(text);
            if (m.Success && TryStartFromCoordinates(m)) return;
            PoiRow first = suggestRows.FirstOrDefault();
            if (first != null) SetStart(FromPlace(first.Poi));
            else Notify("Không tìm thấy địa điểm nào khớp với \"" + text + "\"", ToastKind.Info);
        }

        private bool TryStartFromCoordinates(Match m)
        {
            double a, b;
            if (!double.TryParse(m.Groups[1].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out a)) return false;
            if (!double.TryParse(m.Groups[2].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out b)) return false;
            double latitude = a, longitude = b;
            if (Math.Abs(latitude) > 90 && Math.Abs(longitude) <= 90) { latitude = b; longitude = a; }
            if (Math.Abs(latitude) > 90 || Math.Abs(longitude) > 180) return false;
            MapXY? point = Projector == null ? null : Projector(longitude, latitude);
            if (point == null)
            {
                Notify("Không đổi được toạ độ sang vị trí trên bản đồ", ToastKind.Error);
                return true;
            }
            SetStart(new StartSpot
            {
                Name = "Vị trí (" + latitude.ToString("0.0000", CultureInfo.InvariantCulture) + ", " + longitude.ToString("0.0000", CultureInfo.InvariantCulture) + ")",
                Point = point.Value, Longitude = longitude, Latitude = latitude,
            });
            return true;
        }

        private void SetPicking(bool on, bool raise)
        {
            if (pickingOnMap == on) return;
            pickingOnMap = on;
            btnPickOnMap.Text = on ? "Bấm vào bản đồ… (Huỷ)" : "Chọn trên bản đồ";
            btnPickOnMap.FillColor = on ? ThemeColors.Sky50 : Color.White;
            btnPickOnMap.BorderColor = on ? ThemeColors.Primary : ThemeColors.Slate300;
            if (raise) PickStartRequested?.Invoke(this, on);
        }

        // The places offered as a start: the transport hubs and the best known places while nothing is typed, else the places that match.
        private void ReloadSuggestions()
        {
            string text = startBox.Text.Trim();
            IEnumerable<Poi> source = PoiCatalog.All.Where(p => p.X != 0 || p.Y != 0);
            IEnumerable<Poi> picked;
            if (text.Length == 0 || start != null)
            {
                picked = source.Where(p => p.Category == PoiCategory.Transport).Take(2)
                    .Concat(source.Where(p => p.Category == PoiCategory.Heritage).Take(2));
            }
            else
            {
                picked = PoiSearchFilter.Apply(source, text, null).Take(SuggestionCount);
            }
            suggestRows = picked.Distinct().Take(SuggestionCount).Select(p => new PoiRow(p) { Subtitle = PoiMarkerIcons.Describe(p) }).ToList();
            suggestList.SetRows(suggestRows);
            lblSuggest.Text = text.Length == 0 || start != null ? "Gợi ý điểm bắt đầu" : "Kết quả cho \"" + text + "\"";
        }

        // ------------------------------------------------------------------ the itinerary

        private void AddStop(Poi poi)
        {
            if (itinerary.Any(p => p.Id == poi.Id)) return;
            if (itinerary.Count >= MaxStops)
            {
                Notify("Một lộ trình có tối đa " + MaxStops + " điểm đến", ToastKind.Info);
                return;
            }
            itinerary.Add(poi);
            AfterStopsChanged();
        }

        private void RemoveStop(int index)
        {
            if (index < 0 || index >= itinerary.Count) return;
            itinerary.RemoveAt(index);
            AfterStopsChanged();
        }

        private void MoveStop(int index, int direction)
        {
            int target = index + direction;
            if (index < 0 || target < 0 || index >= itinerary.Count || target >= itinerary.Count) return;
            Poi poi = itinerary[index];
            itinerary.RemoveAt(index);
            itinerary.Insert(target, poi);
            AfterStopsChanged();
        }

        private void TogglePlace(PoiRow row)
        {
            if (itinerary.Any(p => p.Id == row.Poi.Id)) itinerary.RemoveAll(p => p.Id == row.Poi.Id);
            else
            {
                if (itinerary.Count >= MaxStops)
                {
                    Notify("Một lộ trình có tối đa " + MaxStops + " điểm đến", ToastKind.Info);
                    return;
                }
                itinerary.Add(row.Poi);
            }
            AfterStopsChanged();
        }

        private void AfterStopsChanged()
        {
            InvalidateTrip();
            RefreshStops();
            LayoutAll();
        }

        private void RefreshStops()
        {
            stopList.SetStops(itinerary);
            lblStopsCount.Text = itinerary.Count + " / " + MaxStops + " điểm";
            for (int i = 0; i < pickerList.RowCount; i++)
            {
                PoiRow row = pickerList.RowAt(i);
                row.Added = itinerary.Any(p => p.Id == row.Poi.Id);
            }
            pickerList.Repaint();
        }

        // Every place with a position, filtered by what is typed in the search box of the list.
        private void ReloadPicker()
        {
            IEnumerable<Poi> source = PoiCatalog.All.Where(p => p.X != 0 || p.Y != 0);
            IReadOnlyList<Poi> found = PoiSearchFilter.Apply(source, pickerSearch.Text, null);
            var rows = found.OrderBy(p => p.Category).ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).Take(PickerMaxPlaces)
                .Select(p => new PoiRow(p) { Subtitle = PoiMarkerIcons.Describe(p), Added = itinerary.Any(i => i.Id == p.Id) }).ToList();
            pickerList.SetRows(rows);
            lblPickerCount.Text = found.Count + " địa điểm" + (found.Count > PickerMaxPlaces ? " (hiện " + PickerMaxPlaces + " đầu tiên)" : "");
        }

        // ------------------------------------------------------------------ computing the route

        // Any change of the selection makes the trip on the map (and one still on its way) obsolete.
        private void InvalidateTrip()
        {
            planVersion++;
            if (planCts != null) planCts.Cancel();
            if (plan == null && !routeShown) return;
            plan = null;
            if (routeShown)
            {
                routeShown = false;
                RouteInvalidated?.Invoke(this, EventArgs.Empty);
            }
            SetCarPlaying(false);
            ShowResults(false);
            LayoutAll();
        }

        private async Task FindRouteAsync()
        {
            if (busy) return;
            if (start == null)
            {
                Notify("Hãy chọn điểm bắt đầu: gõ tên, gõ toạ độ hoặc chọn trên bản đồ", ToastKind.Info);
                return;
            }
            if (itinerary.Count == 0)
            {
                Notify("Hãy thêm ít nhất một điểm đến vào lộ trình", ToastKind.Info);
                return;
            }

            bool optimize = tglOptimize.Checked;
            List<Poi> places = itinerary.ToList();
            StartSpot from = start;
            InvalidateTrip();
            int version = planVersion;
            planCts = new CancellationTokenSource();
            CancellationToken token = planCts.Token;
            SetBusy(true);
            TripPlan result = null;
            string problem = null;
            try
            {
                if (routeService != null)
                {
                    var ids = places.Select(p => p.Id).ToList();
                    bool byPlace = from.Place != null && !ids.Contains(from.Place.Id);
                    Func<bool, Task<ApiResult<OnlineRoute>>> ask = opt => byPlace
                        ? routeService.PlanAsync(from.Place.Id, ids, opt, token)
                        : routeService.PlanFromPointAsync(from.Point.X, from.Point.Y, ids, opt, token);
                    Task<ApiResult<OnlineRoute>> bestCall = ask(optimize);
                    Task<ApiResult<OnlineRoute>> listedCall = optimize ? ask(false) : null;   // only to show how much the best order saves
                    ApiResult<OnlineRoute> best = await bestCall;
                    ApiResult<OnlineRoute> listed = listedCall == null ? null : await listedCall;
                    if (version != planVersion) return;   // the selection changed while the server was working
                    if (best.Ok) result = TripPlanner.FromOnline(best.Value, from.Name, from.Point, places, listed != null && listed.Ok ? listed.Value : null);
                    else problem = best.Error;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                SetBusy(false);
            }

            if (version != planVersion) return;
            if (result == null)
            {
                result = TripPlanner.Estimate(from.Name, from.Longitude, from.Latitude, from.Point, places, optimize);
                if (problem != null) Notify(problem + " Đang hiển thị số liệu ước tính.", ToastKind.Error);
            }
            ApplyPlan(result, optimize);
        }

        private void SetBusy(bool value)
        {
            busy = value;
            btnFind.Enabled = !value;
            btnFind.Text = value ? "Đang tìm đường…" : "Tìm đường";
        }

        private void ApplyPlan(TripPlan result, bool reorder)
        {
            plan = result;
            if (reorder)
            {
                itinerary.Clear();
                itinerary.AddRange(result.Stops);
                RefreshStops();
            }
            summary.Distance = FormatKm(result.TotalMeters / 1000);
            summary.Duration = FormatMinutes((int)Math.Round(result.TotalSeconds / 60));
            summary.Estimate = result.IsEstimate;
            int saved = (int)Math.Round(result.SavedSeconds / 60);
            summary.Note = result.IsEstimate
                ? "Số liệu ước tính theo đường chim bay (chưa có dữ liệu đường bộ từ máy chủ)."
                : saved > 0 ? "Tiết kiệm " + saved + " phút di chuyển so với thứ tự đã chọn." : "Theo đường bộ thật, đúng thứ tự " + (tglOptimize.Checked ? "ngắn nhất." : "bạn đã sắp xếp.");
            legList.SetLegs(result.Legs);
            SetCarPlaying(false);
            ShowResults(true);
            LayoutAll();
            routeShown = true;
            RouteComputed?.Invoke(this, result);
            Notify("Đã tìm đường: " + summary.Distance + ", " + summary.Duration, ToastKind.Success);
        }

        private void ShowResults(bool visible)
        {
            foreach (Control c in new Control[] { summary, lblLegsTitle, legList, btnPlay, btnReplay, btnFit, btnSave, btnPrint }) c.Visible = visible;
        }

        // ------------------------------------------------------------------ layout

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutAll();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) LayoutAll();
        }

        // Stacks the sections from the top (device pixels) and sets how far the body scrolls.
        private void LayoutAll()
        {
            if (body == null || dividers.Count < 3 || body.ClientSize.Width <= 0) return;   // the first resizes come before the controls exist
            int offset = body.AutoScrollPosition.Y;
            int pad = Ui.S(16), gap = Ui.S(10);
            int full = Math.Max(Ui.S(240), ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
            int inner = full - pad * 2;
            int y = Ui.S(16);

            Action<Control, int> put = (c, h) => { c.SetBounds(pad, y + offset, inner, h); y += h; };

            // start
            put(lblStartTitle, Ui.S(26)); y += Ui.S(4);
            bool typed = startBox.Text.Trim().Length > 0 && start == null;
            int useWidth = Ui.S(72);
            startBox.SetBounds(pad, y + offset, typed ? inner - useWidth - gap : inner, Ui.S(40));
            btnUseText.Visible = typed;
            btnUseText.SetBounds(pad + inner - useWidth, y + offset, useWidth, Ui.S(40));
            y += Ui.S(40) + gap;
            put(lblStartHint, Ui.S(52)); y += gap;
            int clearWidth = Ui.S(92);
            btnPickOnMap.SetBounds(pad, y + offset, inner - clearWidth - gap, Ui.S(40));
            btnClearStart.SetBounds(pad + inner - clearWidth, y + offset, clearWidth, Ui.S(40));
            y += Ui.S(40) + gap;
            bool showSuggest = start == null;
            lblSuggest.Visible = suggestList.Visible = showSuggest;
            if (showSuggest)
            {
                put(lblSuggest, Ui.S(20)); y += Ui.S(2);
                put(suggestList, Math.Max(1, suggestRows.Count) * Ui.S(60));
                y += gap;
            }
            y += Divider(0, pad, inner, y, offset);

            // itinerary
            lblStopsCount.SetBounds(pad, y + offset + Ui.S(4), inner, Ui.S(20));
            lblStopsCount.TextAlign = ContentAlignment.TopRight;
            put(lblStopsTitle, Ui.S(26)); y += Ui.S(4);
            put(stopList, stopList.WantedHeight); y += gap;
            btnAddStops.Text = pickerOpen ? "Đóng danh sách địa điểm" : "Thêm điểm đến từ danh sách địa điểm";
            btnAddStops.IconName = pickerOpen ? "expand_less" : "add";
            put(btnAddStops, Ui.S(42)); y += gap;
            pickerSearch.Visible = lblPickerCount.Visible = pickerList.Visible = pickerOpen;
            if (pickerOpen)
            {
                put(pickerSearch, Ui.S(40)); y += Ui.S(6);
                put(lblPickerCount, Ui.S(20)); y += Ui.S(2);
                put(pickerList, PickerRows * Ui.S(60)); y += gap;
            }
            y += Divider(1, pad, inner, y, offset);

            // options and the button that computes
            int toggleWidth = Ui.S(ToggleSwitch.DesignWidth), toggleHeight = Ui.S(ToggleSwitch.DesignHeight);
            lblOptimize.SetBounds(pad, y + offset, inner - toggleWidth - gap, Ui.S(28));
            tglOptimize.SetBounds(pad + inner - toggleWidth, y + offset + (Ui.S(28) - toggleHeight) / 2, toggleWidth, toggleHeight);
            y += Ui.S(30);
            put(lblOptimizeHint, Ui.S(40)); y += gap;
            btnFind.SetBounds(pad, y + offset, inner, Ui.S(48));
            y += Ui.S(48) + gap;

            // results
            dividers[2].Visible = summary.Visible;
            if (summary.Visible)
            {
                y += Divider(2, pad, inner, y, offset);
                put(summary, Ui.S(110)); y += gap;
                put(lblLegsTitle, Ui.S(26)); y += Ui.S(2);
                put(legList, legList.WantedHeight); y += gap;
                int half = (inner - gap) / 2;
                btnPlay.SetBounds(pad, y + offset, half, Ui.S(40));
                btnReplay.SetBounds(pad + half + gap, y + offset, half, Ui.S(40));
                y += Ui.S(40) + gap;
                int small = (half - gap) / 2;
                btnFit.SetBounds(pad, y + offset, half, Ui.S(40));
                btnSave.SetBounds(pad + half + gap, y + offset, small, Ui.S(40));
                btnPrint.SetBounds(pad + half + gap + small + gap, y + offset, half - small - gap, Ui.S(40));
                y += Ui.S(40) + gap;
            }
            body.AutoScrollMinSize = new Size(0, y + pad);
        }

        // A thin line across the body; returns the space it takes.
        private int Divider(int index, int pad, int inner, int y, int offset)
        {
            Control line = dividers[index];
            line.SetBounds(pad, y + offset, inner, 1);
            line.Visible = true;
            return Ui.S(14);
        }

        // ------------------------------------------------------------------ saving and printing

        private string BuildItinerary()
        {
            var text = new StringBuilder();
            text.AppendLine("LỘ TRÌNH THAM QUAN - CỐ ĐÔ GIS");
            text.AppendLine(new string('=', 40));
            text.AppendLine("Điểm bắt đầu: " + (plan != null ? plan.StartName : (start == null ? "" : start.Name)));
            text.AppendLine();
            text.AppendLine("Các điểm tham quan:");
            IReadOnlyList<Poi> stops = plan != null ? plan.Stops : (IReadOnlyList<Poi>)itinerary;
            for (int i = 0; i < stops.Count; i++)
            {
                text.AppendLine((i + 1) + ". " + stops[i].Name);
                text.AppendLine("   " + PoiMarkerIcons.Describe(stops[i]));
            }
            if (plan != null)
            {
                text.AppendLine();
                text.AppendLine("Tổng quãng đường: " + summary.Distance);
                text.AppendLine("Thời gian dự kiến: " + summary.Duration);
                text.AppendLine();
                text.AppendLine("Chi tiết các chặng di chuyển:");
                for (int i = 0; i < plan.Legs.Count; i++)
                {
                    TripLeg leg = plan.Legs[i];
                    text.AppendLine("Chặng " + (i + 1) + ": " + ShortName(leg.FromName) + " → " + ShortName(leg.ToName) + " - " + FormatKm(leg.Meters / 1000) + " (khoảng " + (int)Math.Round(leg.Seconds / 60) + " phút)");
                }
                text.AppendLine();
                text.AppendLine(plan.IsEstimate ? "(Quãng đường ước tính theo đường chim bay nhân hệ số 1,3; thời gian tính ở tốc độ trung bình 25 km/h.)" : "(Quãng đường và thời gian tính theo đường bộ thật từ máy chủ.)");
            }
            return text.ToString();
        }

        private void SaveItinerary()
        {
            using (var dialog = new SaveFileDialog { Filter = "Tệp văn bản (*.txt)|*.txt", FileName = "LoTrinhThamQuan.txt", Title = "Lưu lịch trình vào máy" })
            {
                if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
                try
                {
                    File.WriteAllText(dialog.FileName, BuildItinerary(), new UTF8Encoding(true));
                    Notify("Đã lưu lịch trình: " + Path.GetFileName(dialog.FileName), ToastKind.Success);
                }
                catch (IOException ex)
                {
                    Notify("Không lưu được tệp: " + ex.Message, ToastKind.Error);
                }
                catch (UnauthorizedAccessException)
                {
                    Notify("Không có quyền ghi vào vị trí đã chọn", ToastKind.Error);
                }
            }
        }

        private void PrintItinerary()
        {
            string itineraryText = BuildItinerary();
            using (var document = new PrintDocument())
            using (var preview = new PrintPreviewDialog { Document = document, Width = 900, Height = 700 })
            using (var font = new Font("Segoe UI", 11f))
            {
                document.PrintPage += (s, e) => e.Graphics.DrawString(itineraryText, font, Brushes.Black, e.MarginBounds);
                preview.ShowDialog(FindForm());
            }
        }

        // ------------------------------------------------------------------ text helpers

        // "Ga Huế (Đường Bùi Thị Xuân)" -> "Ga Huế"
        private static string ShortName(string name) => string.IsNullOrEmpty(name) ? "" : Regex.Replace(name, @"\s*\(.*\)\s*$", "");

        private static string FormatKm(double km) => km.ToString("0.0", CultureInfo.InvariantCulture) + " km";

        private static string FormatMinutes(int minutes) =>
            minutes >= 60 ? (minutes / 60) + " giờ " + (minutes % 60) + " phút" : minutes + " phút";

        protected override void Dispose(bool disposing)
        {
            if (disposing && planCts != null) planCts.Cancel();
            base.Dispose(disposing);
        }
    }
}
