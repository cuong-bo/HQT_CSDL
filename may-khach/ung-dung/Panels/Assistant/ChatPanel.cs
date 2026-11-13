using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Models;
using Hue_travel.Panels.TravelGuide;
using Hue_travel.Services.Data;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Panels.Assistant
{
    /// <summary>A route card the assistant offered; the host opens the route form with it.</summary>
    public sealed class ChatRouteChosenEventArgs : EventArgs
    {
        public ChatRouteChosenEventArgs(string title, IReadOnlyList<Poi> places)
        {
            Title = title;
            Places = places;
        }

        public string Title { get; }
        public IReadOnlyList<Poi> Places { get; }
    }

    /// <summary>
    /// Tab "Trợ lý": a chat with the server's DeepSeek assistant. The panel only sends text and displays the signed reply;
    /// place chips and the route card are built by the server from tool results, never from the model's prose. Offline, or
    /// when the server has no DeepSeek key, it shows the static emergency guide instead.
    /// </summary>
    public sealed class ChatPanel : FloatingPanelForm
    {
        private const int ChatWidth = 384;
        private const int ChatHeight = 640;

        private readonly PanelHeader header = new PanelHeader { IconName = "support_agent", Text = "Trợ lý Huế Travel", Prominent = true };
        private readonly NoAutoScrollPanel body = new NoAutoScrollPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = ThemeColors.Paper };
        private readonly ChatTranscript transcript = new ChatTranscript();
        private readonly Panel topBar = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = ThemeColors.Paper };
        private readonly IconButton btnNew = new IconButton();
        private readonly Label lblRemaining = new Label { AutoSize = false, Font = ThemeFonts.Regular(11), ForeColor = ThemeColors.Slate500, TextAlign = ContentAlignment.MiddleRight };
        private readonly FlowLayoutPanel suggestions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 78, Padding = new Padding(Ui.S(14), Ui.S(4), Ui.S(14), 0), WrapContents = true, AutoScroll = false, BackColor = ThemeColors.Paper };
        private readonly Panel footer = new Panel { Dock = DockStyle.Bottom, Height = 144, BackColor = ThemeColors.Paper };
        private readonly TextBox input = new TextBox { Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.FixedSingle, Font = ThemeFonts.Regular(13), BackColor = ThemeColors.Slate50, ForeColor = ThemeColors.Slate800 };
        private readonly IconButton btnSend = new IconButton();
        private readonly IconButton btnStop = new IconButton();
        private readonly Label lblStatus = new Label { AutoSize = false, Font = ThemeFonts.Regular(11), ForeColor = ThemeColors.Slate500, TextAlign = ContentAlignment.MiddleLeft };
        private readonly Label lblPrivacy = new Label { AutoSize = false, Font = ThemeFonts.Regular(10), ForeColor = ThemeColors.Slate400, Text = "Câu hỏi được gửi tới DeepSeek để xử lý. Đừng nhập thông tin cá nhân.", TextAlign = ContentAlignment.MiddleLeft };
        private readonly System.Windows.Forms.Timer thinkingTimer = new System.Windows.Forms.Timer { Interval = 500 };
        private readonly List<ChatTurn> history = new List<ChatTurn>();

        private ApiChatService service;
        private bool enabled;
        private bool builtChat;
        private bool thinking;
        private int conversation;
        private DateTime thinkingSince;
        private CancellationTokenSource pending;
        private string lastReplyText = "";

        /// <summary>Supplies the place currently open in the app, so "chỗ này" means something.</summary>
        public Func<string> SelectedPoiId { get; set; }

        public event EventHandler<ChatRouteChosenEventArgs> RouteChosen;
        public event EventHandler<string> PlaceChosen;

        public ChatPanel()
        {
            Size = new Size(ChatWidth, ChatHeight);
            header.CloseClicked += (s, e) => RaiseCloseRequested();
            thinkingTimer.Tick += (s, e) =>
            {
                if (!thinking) return;
                lblStatus.Text = "Đang trả lời… " + Math.Max(1, (int)(DateTime.UtcNow - thinkingSince).TotalSeconds) + " s";
            };
            transcript.PlaceChosen += (s, id) => PlaceChosen?.Invoke(this, id);
            transcript.RouteChosen += (s, route) => OpenRoute(route);
            input.KeyDown += InputKeyDown;
            btnSend.Click += SendButtonClick;
            btnStop.Click += StopButtonClick;
            btnNew.Click += (s, e) => NewConversation();
            body.Resize += (s, e) => LayoutChat();
            Resize += (s, e) => { if (builtChat) { LayoutTop(); LayoutFooter(); LayoutChat(); } };
            BuildGuide();
        }

        /// <summary>Null service or disabled assistant: the panel stays on the static guide.</summary>
        public void SetService(ApiChatService value, bool chatEnabled)
        {
            service = value;
            enabled = chatEnabled && value != null;
            if (builtChat != enabled) Build();
        }

        public void SetEnabled(bool chatEnabled)
        {
            enabled = chatEnabled && service != null;
            if (builtChat != enabled) Build();
        }

        private void Build()
        {
            Controls.Clear();
            if (enabled) BuildChat();
            else BuildGuide();
            Controls.Add(header);   // last: the header stays above the body
        }

        // ------------------------------------------------------------------ static guide (offline / assistant off)

        private void BuildGuide()
        {
            builtChat = false;
            body.Controls.Clear();
            var guide = new NoAutoScrollPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = ThemeColors.Paper };
            var title = new Label { AutoSize = false, Font = ThemeFonts.Bold(16), ForeColor = ThemeColors.Slate800, Text = "Cẩm nang nhanh", TextAlign = ContentAlignment.MiddleLeft };
            var hint = new Label { AutoSize = false, Font = ThemeFonts.Regular(12), ForeColor = ThemeColors.Slate500, Text = "Trợ lý chưa sẵn sàng. Bạn vẫn xem được số điện thoại hữu ích và lưu ý khi tham quan.", TextAlign = ContentAlignment.TopLeft };
            var tips = new InfoRows();
            var contacts = new InfoRows();
            tips.SetNotes(GuideContent.Tips);
            contacts.SetNotes(GuideContent.Contacts);
            contacts.CopyClicked += (s, note) => Copy(note.CopyText);
            var tipsTitle = new Label { AutoSize = false, Font = ThemeFonts.Bold(14), ForeColor = ThemeColors.Slate800, Text = "Lưu ý khi tham quan", TextAlign = ContentAlignment.MiddleLeft };
            var contactTitle = new Label { AutoSize = false, Font = ThemeFonts.Bold(14), ForeColor = ThemeColors.Slate800, Text = "Số điện thoại hữu ích", TextAlign = ContentAlignment.MiddleLeft };
            var controls = new Control[] { title, hint, tipsTitle, tips, contactTitle, contacts };
            foreach (Control c in controls) guide.Controls.Add(c);
            guide.Resize += (s, e) =>
            {
                int width = Math.Max(Ui.S(200), guide.ClientSize.Width - (guide.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0));
                int pad = Ui.S(16), y = Ui.S(14);
                title.SetBounds(pad, y, width - pad * 2, Ui.S(26)); y += Ui.S(30);
                hint.SetBounds(pad, y, width - pad * 2, Ui.S(48)); y += Ui.S(54);
                tipsTitle.SetBounds(pad, y, width - pad * 2, Ui.S(24)); y += Ui.S(28);
                tips.SetBounds(0, y, width, tips.HeightFor(width)); y += tips.HeightFor(width) + Ui.S(12);
                contactTitle.SetBounds(pad, y, width - pad * 2, Ui.S(24)); y += Ui.S(28);
                contacts.SetBounds(0, y, width, contacts.HeightFor(width)); y += contacts.HeightFor(width) + Ui.S(16);
                guide.AutoScrollMinSize = new Size(0, y);
            };
            Controls.Add(guide);
        }

        private void Copy(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            try { Clipboard.SetText(text); Notify("Đã sao chép " + text, ToastKind.Success); }
            catch (ExternalException) { Notify("Không sao chép được", ToastKind.Error); }
        }

        // ------------------------------------------------------------------ chat

        private void BuildChat()
        {
            builtChat = true;
            body.Controls.Clear();
            suggestions.Controls.Clear();
            topBar.Controls.Clear();
            footer.Controls.Clear();

            btnNew.AutoWidth = true;
            btnNew.Text = "Cuộc trò chuyện mới";
            btnNew.IconName = "refresh";
            btnNew.IconSize = 15;
            btnNew.Font = ThemeFonts.Medium(12);
            btnNew.ForeColor = ThemeColors.Primary;
            btnNew.IconColor = ThemeColors.Primary;
            btnNew.FillColor = ThemeColors.Sky50;
            btnNew.HoverFillColor = ThemeColors.Sky100;
            btnNew.Radius = ThemeMetrics.RadiusSm;
            btnNew.Height = Ui.S(30);
            topBar.Controls.Add(btnNew);
            topBar.Controls.Add(lblRemaining);

            AddSuggestion("Gợi ý lịch trình 1 ngày", "explore");
            AddSuggestion("Ăn gì ở Huế?", "restaurant");
            AddSuggestion("Giờ mở cửa Đại Nội", "schedule");
            AddSuggestion("Thời tiết hôm nay", "wb_sunny");

            transcript.SetEntries(new List<ChatEntry>());
            AddAssistant("Chào bạn! Mình là trợ lý du lịch Huế. Bạn muốn hỏi về địa điểm, lộ trình hay thời tiết?", null, null);
            var copyMenu = new ContextMenuStrip();
            copyMenu.Items.Add("Sao chép câu trả lời", null, (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(lastReplyText)) return;
                try { Clipboard.SetText(lastReplyText); Notify("Đã sao chép câu trả lời", ToastKind.Success); }
                catch (ExternalException) { Notify("Không sao chép được", ToastKind.Error); }
            });
            transcript.ContextMenuStrip = copyMenu;
            body.Controls.Add(transcript);

            input.Font = ThemeFonts.Regular(13);
            StyleButton(btnSend, "Gửi", "arrow_forward", true);
            StyleButton(btnStop, "Dừng", "close", false);
            btnStop.Visible = false;

            footer.Controls.Add(input);
            footer.Controls.Add(btnSend);
            footer.Controls.Add(btnStop);
            footer.Controls.Add(lblStatus);
            footer.Controls.Add(lblPrivacy);

            Controls.Add(body);
            Controls.Add(footer);
            Controls.Add(suggestions);
            Controls.Add(topBar);
            LayoutChat();
            LayoutTop();
            LayoutFooter();
        }

        private void AddSuggestion(string text, string icon)
        {
            var button = new IconButton
            {
                AutoWidth = true, Text = text, IconName = icon, IconSize = 14, Gap = 5, HorizontalPadding = 8,
                Font = ThemeFonts.Medium(11), ForeColor = ThemeColors.Slate700, IconColor = ThemeColors.Primary,
                FillColor = ThemeColors.Slate50, HoverFillColor = ThemeColors.Sky50, BorderColor = ThemeColors.Slate200, Radius = ThemeMetrics.RadiusSm,
            };
            button.Height = Ui.S(28);
            button.Click += async (s, e) =>
            {
                input.Text = text;
                await SendAsync();
            };
            suggestions.Controls.Add(button);
        }

        private static void StyleButton(IconButton button, string text, string icon, bool primary)
        {
            button.AutoWidth = false;
            button.Text = text;
            button.IconName = icon;
            button.IconSize = 16;
            button.Gap = 6;
            button.Font = ThemeFonts.SemiBold(12);
            button.Radius = ThemeMetrics.RadiusSm;
            button.ForeColor = primary ? Color.White : ThemeColors.Slate700;
            button.IconColor = primary ? Color.White : ThemeColors.Primary;
            button.FillColor = primary ? ThemeColors.Primary : Color.White;
            button.HoverFillColor = primary ? ThemeColors.PrimaryContainer : ThemeColors.Slate100;
            button.BorderColor = primary ? ThemeColors.Primary : ThemeColors.Slate300;
        }

        private void LayoutTop()
        {
            int pad = Ui.S(14);
            btnNew.SetBounds(pad, Ui.S(6), Math.Max(Ui.S(150), btnNew.Width), Ui.S(30));
            lblRemaining.SetBounds(btnNew.Right + Ui.S(8), Ui.S(6), Math.Max(10, ClientSize.Width - btnNew.Right - pad - Ui.S(8)), Ui.S(30));
        }

        private void LayoutFooter()
        {
            int pad = Ui.S(14), gap = Ui.S(8);
            int width = ClientSize.Width;
            int sendWidth = Ui.S(78);
            input.SetBounds(pad, Ui.S(8), Math.Max(Ui.S(120), width - pad * 2 - sendWidth - gap), Ui.S(68));
            btnSend.SetBounds(width - pad - sendWidth, Ui.S(8), sendWidth, Ui.S(36));
            btnStop.SetBounds(width - pad - sendWidth, Ui.S(48), sendWidth, Ui.S(28));
            lblStatus.SetBounds(pad, Ui.S(80), width - pad * 2, Ui.S(20));
            lblPrivacy.SetBounds(pad, Ui.S(102), width - pad * 2, Ui.S(36));
        }

        private void LayoutChat()
        {
            if (!builtChat || body.ClientSize.Width <= 0) return;
            int width = Math.Max(Ui.S(200), body.ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
            int offset = body.AutoScrollPosition.Y;
            transcript.SetBounds(0, offset, width, transcript.WantedHeight);
            body.AutoScrollMinSize = new Size(0, transcript.WantedHeight + Ui.S(10));
            LayoutTop();
            LayoutFooter();
        }

        private void InputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                var ignored = SendAsync();
            }
        }

        private async void SendButtonClick(object sender, EventArgs e) => await SendAsync();

        private void StopButtonClick(object sender, EventArgs e) => pending?.Cancel();

        private async Task SendAsync()
        {
            if (thinking || service == null) return;
            string text = input.Text.Trim();
            if (text.Length == 0) return;
            input.Text = "";
            AddUser(text);
            history.Add(new ChatTurn("user", text));
            SetThinking(true);
            int started = conversation;
            var cts = new CancellationTokenSource();
            pending = cts;
            try
            {
                ChatReply reply = await service.AskAsync(history, SelectedPoiId?.Invoke(), cts.Token).ConfigureAwait(true);
                if (IsDisposed || started != conversation) return;
                if (!reply.Ok)
                {
                    AddAssistant(reply.Error ?? ApiClient.Unreachable, null, null);
                    return;
                }
                lastReplyText = reply.Reply;
                AddAssistant(reply.Reply, reply.Places, reply.Route);
                history.Add(new ChatTurn("assistant", reply.Reply));
                lblRemaining.Text = reply.Remaining >= 0 ? "Còn " + reply.Remaining + " câu hôm nay" : "";
            }
            catch (OperationCanceledException)
            {
                if (!IsDisposed && started == conversation) AddAssistant("Đã dừng câu trả lời.", null, null);
            }
            catch (Exception ex)
            {
                if (!IsDisposed && started == conversation) AddAssistant("Có lỗi: " + ex.Message, null, null);
            }
            finally
            {
                cts.Dispose();
                if (ReferenceEquals(pending, cts)) pending = null;
                if (!IsDisposed && started == conversation) SetThinking(false);
            }
        }

        private void SetThinking(bool value)
        {
            thinking = value;
            input.Enabled = !value;
            btnSend.Enabled = !value;
            btnStop.Visible = value;
            if (value)
            {
                thinkingSince = DateTime.UtcNow;
                lblStatus.Text = "Đang trả lời…";
                thinkingTimer.Start();
            }
            else
            {
                thinkingTimer.Stop();
                lblStatus.Text = "";
            }
        }

        private void NewConversation()
        {
            conversation++;
            pending?.Cancel();
            if (thinking) SetThinking(false);
            history.Clear();
            lastReplyText = "";
            transcript.SetEntries(new List<ChatEntry>());
            AddAssistant("Cuộc trò chuyện mới. Bạn muốn hỏi gì về Huế?", null, null);
            input.Focus();
            LayoutChat();
        }

        private void AddUser(string text)
        {
            var entries = transcript.Entries.ToList();
            entries.Add(new ChatEntry { User = true, Text = text });
            transcript.SetEntries(entries);
            LayoutChat();
            ScrollToBottom();
        }

        private void AddAssistant(string text, IReadOnlyList<ChatPlace> places, ChatRoute route)
        {
            var entries = transcript.Entries.ToList();
            entries.Add(new ChatEntry { User = false, Text = text ?? "", Places = places, Route = route });
            transcript.SetEntries(entries);
            LayoutChat();
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            if (body.ClientSize.Height <= 0) return;
            body.AutoScrollPosition = new Point(0, Math.Max(0, transcript.Height));
        }

        private void OpenRoute(ChatRoute route)
        {
            if (route == null || route.Order == null) return;
            Dictionary<string, Poi> byId = PoiCatalog.All.GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First());
            List<Poi> places = route.Order.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
            if (places.Count < 2) { Notify("Lộ trình này chưa đủ điểm trên bản đồ.", ToastKind.Info); return; }
            RouteChosen?.Invoke(this, new ChatRouteChosenEventArgs("Lộ trình từ trợ lý", places));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (builtChat) { LayoutTop(); LayoutFooter(); LayoutChat(); }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                thinkingTimer.Dispose();
                pending?.Cancel();
                pending?.Dispose();
            }
            base.Dispose(disposing);
        }

        // ------------------------------------------------------------------ transcript

        private sealed class ChatEntry
        {
            public bool User;
            public string Text;
            public IReadOnlyList<ChatPlace> Places;
            public ChatRoute Route;
        }

        private sealed class EntryLayout
        {
            public ChatEntry Entry;
            public Rectangle Bubble;
            public readonly List<KeyValuePair<Rectangle, string>> Chips = new List<KeyValuePair<Rectangle, string>>();
            public Rectangle Route;
        }

        private sealed class ChatTranscript : Control
        {
            private readonly List<ChatEntry> entries = new List<ChatEntry>();
            private readonly List<EntryLayout> layout = new List<EntryLayout>();
            private int laidOutWidth = -1;
            private int wantedHeight;

            public ChatTranscript()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = ThemeColors.Paper;
            }

            public IReadOnlyList<ChatEntry> Entries => entries;

            public event EventHandler<string> PlaceChosen;
            public event EventHandler<ChatRoute> RouteChosen;

            public int WantedHeight
            {
                get
                {
                    BuildLayout();
                    return wantedHeight;
                }
            }

            public void SetEntries(IEnumerable<ChatEntry> value)
            {
                entries.Clear();
                entries.AddRange(value);
                laidOutWidth = -1;
                Invalidate();
            }

            private void BuildLayout()
            {
                int width = Math.Max(Ui.S(180), Width);
                if (laidOutWidth == width) return;
                laidOutWidth = width;
                layout.Clear();
                int y = Ui.S(8);
                const TextFormatFlags wrap = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
                foreach (ChatEntry entry in entries)
                {
                    int maxBubble = Math.Max(Ui.S(120), width - Ui.S(48));
                    int pad = Ui.S(12);
                    Font font = ThemeFonts.Regular(13);
                    Size measured = TextRenderer.MeasureText(entry.Text ?? "", font, new Size(maxBubble - pad * 2, int.MaxValue), wrap);
                    int bubbleWidth = Math.Min(maxBubble, measured.Width + pad * 2);
                    int bubbleHeight = Math.Max(Ui.S(34), measured.Height + pad * 2);
                    int left = entry.User ? width - Ui.S(16) - bubbleWidth : Ui.S(16);
                    var item = new EntryLayout { Entry = entry, Bubble = new Rectangle(left, y, bubbleWidth, bubbleHeight) };
                    y += bubbleHeight + Ui.S(6);

                    if (entry.Places != null && entry.Places.Count > 0)
                    {
                        int x = Ui.S(16), chipTop = y;
                        foreach (ChatPlace place in entry.Places)
                        {
                            Font chipFont = ThemeFonts.SemiBold(11);
                            int chipWidth = TextRenderer.MeasureText(place.Name ?? place.Id, chipFont, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width + Ui.S(20);
                            if (x + chipWidth > width - Ui.S(16) && x > Ui.S(16)) { x = Ui.S(16); chipTop += Ui.S(28); }
                            item.Chips.Add(new KeyValuePair<Rectangle, string>(new Rectangle(x, chipTop, chipWidth, Ui.S(24)), place.Id));
                            x += chipWidth + Ui.S(6);
                        }
                        y = chipTop + Ui.S(28);
                    }
                    if (entry.Route != null)
                    {
                        item.Route = new Rectangle(Ui.S(16), y, width - Ui.S(32), Ui.S(62));
                        y += Ui.S(66);
                    }
                    y += Ui.S(8);
                    layout.Add(item);
                }
                wantedHeight = y + Ui.S(8);
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                laidOutWidth = -1;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                BuildLayout();
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(BackColor);
                foreach (EntryLayout item in layout)
                {
                    Color bubble = item.Entry.User ? ThemeColors.Primary : ThemeColors.Slate100;
                    Color text = item.Entry.User ? Color.White : ThemeColors.Slate800;
                    using (GraphicsPath path = Ui.RoundedRect(item.Bubble, Ui.S(ThemeMetrics.RadiusLg)))
                    using (var brush = new SolidBrush(bubble)) g.FillPath(brush, path);
                    TextRenderer.DrawText(g, item.Entry.Text ?? "", ThemeFonts.Regular(13),
                        Rectangle.Inflate(item.Bubble, -Ui.S(12), -Ui.S(8)), text,
                        TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    foreach (KeyValuePair<Rectangle, string> chip in item.Chips)
                    {
                        using (GraphicsPath path = Ui.RoundedRect(chip.Key, Ui.S(ThemeMetrics.RadiusFull)))
                        using (var brush = new SolidBrush(ThemeColors.Sky50)) g.FillPath(brush, path);
                        TextRenderer.DrawText(g, ChipName(chip.Value), ThemeFonts.SemiBold(11), chip.Key, ThemeColors.Primary,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
                    }
                    if (item.Route != Rectangle.Empty)
                    {
                        using (GraphicsPath path = Ui.RoundedRect(item.Route, Ui.S(ThemeMetrics.RadiusMd)))
                        using (var brush = new SolidBrush(ThemeColors.Slate50)) g.FillPath(brush, path);
                        using (GraphicsPath outline = Ui.RoundedRect(item.Route, Ui.S(ThemeMetrics.RadiusMd)))
                        using (var pen = new Pen(ThemeColors.Slate200)) g.DrawPath(pen, outline);
                        int pad = Ui.S(10);
                        TextRenderer.DrawText(g, "Lộ trình gợi ý", ThemeFonts.Bold(12), new Rectangle(item.Route.X + pad, item.Route.Y + Ui.S(6), item.Route.Width - pad * 2, Ui.S(20)), ThemeColors.Slate800, TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                        ChatRoute route = item.Entry.Route;
                        int stopCount = route.Order == null ? 0 : route.Order.Count;
                        string meta = stopCount + " điểm · " + (route.Meters / 1000).ToString("0.0") + " km · " + Math.Round(route.Seconds / 60) + " phút  ·  Xem trên bản đồ";
                        TextRenderer.DrawText(g, meta, ThemeFonts.Regular(11), new Rectangle(item.Route.X + pad, item.Route.Y + Ui.S(30), item.Route.Width - pad * 2, Ui.S(20)), ThemeColors.Primary, TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
                    }
                }
            }

            private string ChipName(string id)
            {
                foreach (ChatEntry entry in entries)
                {
                    if (entry.Places == null) continue;
                    foreach (ChatPlace place in entry.Places)
                        if (place.Id == id) return place.Name ?? id;
                }
                return id;
            }

            private EntryLayout HitChip(Point p, out string id)
            {
                id = null;
                BuildLayout();
                foreach (EntryLayout item in layout)
                {
                    foreach (KeyValuePair<Rectangle, string> chip in item.Chips)
                        if (chip.Key.Contains(p)) { id = chip.Value; return item; }
                }
                return null;
            }

            private EntryLayout HitRoute(Point p)
            {
                BuildLayout();
                foreach (EntryLayout item in layout)
                    if (item.Route != Rectangle.Empty && item.Route.Contains(p)) return item;
                return null;
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                string id;
                Cursor = HitChip(e.Location, out id) != null || HitRoute(e.Location) != null ? Cursors.Hand : Cursors.Default;
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                if (e.Button != MouseButtons.Left) return;
                string id;
                EntryLayout chip = HitChip(e.Location, out id);
                if (chip != null && id != null) { PlaceChosen?.Invoke(this, id); return; }
                EntryLayout route = HitRoute(e.Location);
                if (route != null) RouteChosen?.Invoke(this, route.Entry.Route);
            }
        }
    }
}
