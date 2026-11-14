using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Panels.TravelGuide;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Panels.Assistant
{
    /// <summary>
    /// The assistant: a conversation with the server's assistant (DeepSeek behind the server), which looks places up, plans routes and tells the
    /// weather through the server. It replaced the static guide. When the server is not there, or the assistant is switched off, the panel
    /// shows the short static guide instead (the emergency numbers must not depend on an AI). Built in code like the other panels.
    /// </summary>
    public sealed class ChatPanel : FloatingPanelForm
    {
        private const int MaxQuestionLength = 2000;

        private static readonly string[] Suggestions =
        {
            "Gợi ý lịch trình 1 ngày ở Huế", "Ăn gì ở Huế?", "Thời tiết hôm nay thế nào?", "Đại Nội mở cửa mấy giờ?",
        };

        private readonly ApiChatService service;
        private readonly PanelHeader header = new PanelHeader { IconName = "assistant", Text = "Trợ lý Huế Travel", Prominent = true };
        private readonly ChatView view = new ChatView { Dock = DockStyle.Fill };
        private readonly Panel inputBar = new Panel { Dock = DockStyle.Bottom, BackColor = Color.White };
        private readonly RoundedPanel inputBox = new RoundedPanel { FillColor = ThemeColors.Slate50, BorderColor = ThemeColors.Slate300 };
        private readonly TextBox txtQuestion = new TextBox { Multiline = true, BorderStyle = BorderStyle.None, BackColor = ThemeColors.Slate50, ForeColor = ThemeColors.Slate800, AcceptsReturn = false };
        private readonly Label lblHint = new Label { AutoSize = false, BackColor = Color.Transparent, ForeColor = ThemeColors.Slate400, Text = "Hỏi về địa điểm, lộ trình, thời tiết…", UseMnemonic = false };
        private readonly IconButton btnSend = new IconButton();
        private readonly IconButton btnNew = new IconButton();
        private readonly Label lblStatus = new Label { AutoSize = false, BackColor = Color.Transparent, ForeColor = ThemeColors.Slate500, UseMnemonic = false };

        // the static guide shown when the assistant cannot answer
        private readonly NoAutoScrollPanel guide = new NoAutoScrollPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White, Visible = false };
        private readonly Label lblNotice = new Label { AutoSize = false, ForeColor = ThemeColors.Slate600, TextAlign = ContentAlignment.TopLeft, UseMnemonic = false };
        private readonly Label lblTipsTitle = new Label { AutoSize = false, Text = "Lưu ý khi tham quan", ForeColor = ThemeColors.Slate800, TextAlign = ContentAlignment.MiddleLeft };
        private readonly InfoRows tipRows = new InfoRows();
        private readonly Label lblContactsTitle = new Label { AutoSize = false, Text = "Số điện thoại hữu ích", ForeColor = ThemeColors.Slate800, TextAlign = ContentAlignment.MiddleLeft };
        private readonly InfoRows contactRows = new InfoRows();

        private readonly List<ChatTurn> turns = new List<ChatTurn>();
        private readonly System.Windows.Forms.Timer pendingTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        private CancellationTokenSource asking;
        private DateTime askedAt;
        private bool busy, statusChecked;

        /// <summary>The id of the place open on the map (so "chỗ này" is understood), or null. Set by the main window.</summary>
        public Func<string> CurrentPlaceId { get; set; }

        // A reply that arrives after the window was closed must not touch its controls.
        private bool Gone => IsDisposed || !IsHandleCreated;

        /// <summary>Raised when a place chip is pressed: the host shows that place on the map.</summary>
        public event EventHandler<string> PlaceChosen;

        /// <summary>Raised when "Xem trên bản đồ" is pressed on a planned route: the host opens the route form with the stops.</summary>
        public event EventHandler<ChatRouteInfo> RouteChosen;

        /// <param name="service">Null when there is no server (the app runs offline): only the static guide shows.</param>
        public ChatPanel(ApiChatService service)
        {
            this.service = service;
            Size = new Size(420, 640);
            header.CloseClicked += (s, e) => RaiseCloseRequested();

            BuildInput();
            BuildGuide();
            Controls.Add(view);
            Controls.Add(guide);
            Controls.Add(inputBar);
            Controls.Add(header);

            view.PlaceClicked += (s, place) => PlaceChosen?.Invoke(this, place.Id);
            view.RouteClicked += (s, route) => RouteChosen?.Invoke(this, route);
            view.SuggestionClicked += (s, text) => { Task ignored = AskAsync(text); };
            view.TextCopied += (s, e) => Notify("Đã sao chép câu trả lời", ToastKind.Success);
            pendingTimer.Tick += (s, e) => ShowPending();
            VisibleChanged += (s, e) => { if (Visible) OnOpened(); };

            if (service == null) ShowGuide("Chưa có kết nối máy chủ nên trợ lý chưa hoạt động. Đây là cẩm nang nhanh, không cần mạng.");
            else NewConversation();
        }

        // ------------------------------------------------------------------ building

        private void BuildInput()
        {
            txtQuestion.Font = ThemeFonts.Regular(13);
            lblHint.Font = ThemeFonts.Regular(13);
            txtQuestion.TextChanged += (s, e) => lblHint.Visible = txtQuestion.TextLength == 0;
            txtQuestion.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift) { e.SuppressKeyPress = true; Send(); }
                else if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtQuestion.SelectedText = Environment.NewLine; }
            };
            lblHint.Click += (s, e) => txtQuestion.Focus();

            btnSend.AutoWidth = false;
            btnSend.IconName = "arrow_forward";
            btnSend.IconSize = 18;
            btnSend.Radius = ThemeMetrics.RadiusFull;
            btnSend.Kind = IconButton.ButtonKind.Primary;
            btnSend.Click += (s, e) => { if (busy) CancelAsking(); else Send(); };

            btnNew.AutoWidth = false;
            btnNew.Text = "Cuộc trò chuyện mới";
            btnNew.IconName = "refresh";
            btnNew.IconSize = 14;
            btnNew.Gap = 6;
            btnNew.Font = ThemeFonts.Medium(12);
            btnNew.Kind = IconButton.ButtonKind.Ghost;
            btnNew.Click += (s, e) => NewConversation();

            lblStatus.Font = ThemeFonts.Regular(11);
            inputBox.Controls.Add(lblHint);
            inputBox.Controls.Add(txtQuestion);
            inputBar.Controls.Add(inputBox);
            inputBar.Controls.Add(btnSend);
            inputBar.Controls.Add(btnNew);
            inputBar.Controls.Add(lblStatus);
            inputBar.Resize += (s, e) => LayoutInput();
            inputBar.Height = Ui.S(106);
            LayoutInput();
        }

        private void LayoutInput()
        {
            int pad = Ui.S(12), send = Ui.S(44), gap = Ui.S(8);
            int boxHeight = Ui.S(50);
            btnSend.SetBounds(inputBar.ClientSize.Width - pad - send, pad + (boxHeight - send) / 2, send, send);
            inputBox.SetBounds(pad, pad, Math.Max(Ui.S(80), btnSend.Left - gap - pad), boxHeight);
            int textHeight = Math.Min(boxHeight - Ui.S(12), txtQuestion.Font.Height * 2 + Ui.S(2));
            txtQuestion.SetBounds(Ui.S(12), (boxHeight - textHeight) / 2, Math.Max(Ui.S(40), inputBox.Width - Ui.S(24)), textHeight);
            lblHint.SetBounds(txtQuestion.Left, txtQuestion.Top, txtQuestion.Width, Ui.S(24));
            int rowTop = pad + boxHeight + Ui.S(8);
            btnNew.SetBounds(pad - Ui.S(4), rowTop, Ui.S(190), Ui.S(26));
            lblStatus.SetBounds(inputBar.ClientSize.Width - pad - Ui.S(160), rowTop, Ui.S(160), Ui.S(26));
            lblStatus.TextAlign = ContentAlignment.MiddleRight;
        }

        private void BuildGuide()
        {
            tipRows.SetNotes(GuideContent.Tips);
            contactRows.SetNotes(GuideContent.Contacts);
            contactRows.CopyClicked += (s, note) =>
            {
                try { Clipboard.SetText(note.CopyText); Notify("Đã sao chép " + note.CopyText, ToastKind.Success); }
                catch (ExternalException) { Notify("Không sao chép được " + note.CopyText, ToastKind.Error); }
            };
            lblNotice.Font = ThemeFonts.Regular(12);
            lblTipsTitle.Font = ThemeFonts.Bold(15);
            lblContactsTitle.Font = ThemeFonts.Bold(15);
            foreach (Control c in new Control[] { lblNotice, lblContactsTitle, contactRows, lblTipsTitle, tipRows }) guide.Controls.Add(c);
            guide.Resize += (s, e) => LayoutGuide();
        }

        // The emergency numbers first: they are what somebody without the assistant may need most.
        private void LayoutGuide()
        {
            if (guide.ClientSize.Width <= 0) return;
            int offset = guide.AutoScrollPosition.Y, pad = Ui.S(16), gap = Ui.S(10);
            int full = Math.Max(Ui.S(240), guide.ClientSize.Width), inner = full - pad * 2, y = Ui.S(14);
            lblNotice.SetBounds(pad, y + offset, inner, Ui.S(54)); y += Ui.S(54) + gap;
            lblContactsTitle.SetBounds(pad, y + offset, inner, Ui.S(26)); y += Ui.S(30);
            int contacts = contactRows.HeightFor(full);
            contactRows.SetBounds(0, y + offset, full, contacts); y += contacts + gap;
            lblTipsTitle.SetBounds(pad, y + offset, inner, Ui.S(26)); y += Ui.S(30);
            int tips = tipRows.HeightFor(full);
            tipRows.SetBounds(0, y + offset, full, tips); y += tips + gap;
            guide.AutoScrollMinSize = new Size(0, y + pad);
        }

        private void ShowGuide(string notice)
        {
            lblNotice.Text = notice;
            view.Visible = false;
            inputBar.Visible = false;
            guide.Visible = true;
            LayoutGuide();
        }

        private void ShowChat()
        {
            guide.Visible = false;
            view.Visible = true;
            inputBar.Visible = true;
        }

        // ------------------------------------------------------------------ the conversation

        private void NewConversation()
        {
            CancelAsking();
            turns.Clear();
            view.Clear();
            view.AddGreeting("Xin chào! Mình là trợ lý Huế Travel. Mình có thể tìm địa điểm, gợi ý và tính lộ trình, cho biết giờ mở cửa, thời tiết và cách dùng ứng dụng. Bạn muốn hỏi gì?", Suggestions);
            lblStatus.Text = "";
            txtQuestion.Clear();
        }

        private void OnOpened()
        {
            if (service == null || statusChecked) return;
            statusChecked = true;
            Task ignored = CheckStatusAsync();
        }

        private async Task CheckStatusAsync()
        {
            bool enabled = false;
            try { enabled = await service.IsEnabledAsync(CancellationToken.None); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("ChatPanel.CheckStatus: " + ex.Message); }
            if (Gone) return;
            if (enabled) { ShowChat(); return; }
            statusChecked = false;   // ask again the next time the panel opens: the server may have been started or the key set
            ShowGuide("Trợ lý chưa sẵn sàng (máy chủ chưa bật hoặc chưa kết nối được). Đây là cẩm nang nhanh; bạn thử lại sau nhé.");
        }

        private void Send()
        {
            string text = txtQuestion.Text.Trim();
            if (text.Length == 0 || busy) return;
            Task ignored = AskAsync(text);
        }

        private async Task AskAsync(string text)
        {
            if (busy || service == null) return;
            text = (text ?? "").Trim();
            if (text.Length == 0) return;
            if (text.Length > MaxQuestionLength) text = text.Substring(0, MaxQuestionLength);

            var question = new ChatTurn { IsUser = true, Text = text };
            turns.Add(question);
            view.AddUser(text);
            txtQuestion.Clear();
            SetBusy(true);

            asking = new CancellationTokenSource();
            CancellationToken token = asking.Token;
            ApiResult<ChatAnswer> result;
            try
            {
                result = await service.AskAsync(turns, CurrentPlaceId == null ? null : CurrentPlaceId(), token).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                result = null;   // the person pressed stop
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ChatPanel.Ask: " + ex);
                result = new ApiResult<ChatAnswer> { Error = "Có lỗi khi hỏi trợ lý. Bạn thử lại nhé." };
            }
            if (Gone) return;

            view.RemovePending();
            SetBusy(false);
            if (result != null && result.Ok)
            {
                turns.Add(new ChatTurn { IsUser = false, Text = result.Value.Reply });
                view.AddAnswer(result.Value);
                lblStatus.Text = result.Value.Remaining.HasValue ? "Còn " + result.Value.Remaining.Value + " câu hôm nay" : "";
            }
            else
            {
                // the question did not get an answer: it leaves the history (so the next one is not a duplicate) and goes back in the box
                turns.Remove(question);
                txtQuestion.Text = text;
                if (result != null) view.AddError(result.Error);
            }
        }

        private void SetBusy(bool value)
        {
            busy = value;
            btnSend.IconName = value ? "close" : "arrow_forward";
            txtQuestion.ReadOnly = value;
            if (value)
            {
                askedAt = DateTime.UtcNow;
                ShowPending();
                pendingTimer.Start();
            }
            else
            {
                pendingTimer.Stop();
            }
        }

        private void ShowPending()
        {
            int seconds = (int)(DateTime.UtcNow - askedAt).TotalSeconds;
            view.SetPending("Đang trả lời… " + seconds + " s");
        }

        private void CancelAsking()
        {
            CancellationTokenSource source = asking;
            if (source != null) { try { source.Cancel(); } catch (ObjectDisposedException) { } }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CancelAsking();
                pendingTimer.Dispose();
                if (asking != null) asking.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
