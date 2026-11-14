using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using Hue_travel.Account;
using Hue_travel.Auth;
using Hue_travel.Models;
using Hue_travel.Services.Online;
using Hue_travel.Theme;

namespace Hue_travel.Welcome
{
    /// <summary>
    /// The sign-in screen shown before the main window. The picture (photo, logo, the card's shape, 1536 x 1024 design, stored at
    /// twice that size) is only the backdrop: every word, box and button of the card is a real control drawn at the screen's
    /// own resolution, laid over the picture's card so it looks like the supplied design but stays sharp. The window fills the
    /// screen and has minimise / maximise / close buttons like the main window.
    /// Result: <see cref="DialogResult.OK"/> once somebody is signed in (or chose to continue offline), otherwise Cancel.
    /// </summary>
    public sealed class LoginForm : Form
    {
        // The design's coordinates (a 1536 x 1024 picture); R() maps them to the window.
        private const float ImageWidth = 1536f;
        private const float ImageHeight = 1024f;

        private enum Mode { Login, Register, Offline }

        private static readonly RectangleF CardRect = new RectangleF(1006, 80, 488, 774);

        // The tab strip at the top of the card: a rounded track holding the three tabs (picture pixels).
        private static readonly RectangleF TabTrack = new RectangleF(1054, 116, 392, 58);

        private readonly AppSession session;
        private readonly Bitmap picture = AppImages.LoginBackground();
        private Bitmap backdrop;

        private readonly Label lblTitle = new Label();
        private readonly Label lblSubtitle = new Label();
        private readonly Label lblFooter = new Label();
        private readonly Label lblMessage = new Label();
        private readonly Label lblInfo = new Label();
        private readonly Label lnkForgot = new Label();
        private readonly BrandCheckBox chkRemember = new BrandCheckBox();
        private readonly AuthField fieldName = new AuthField();
        private readonly AuthField fieldUser = new AuthField();
        private readonly AuthField fieldPass = new AuthField();
        private readonly AuthField fieldPass2 = new AuthField();
        private readonly IconButton btnTypeUser = new IconButton();
        private readonly IconButton btnTypeOwner = new IconButton();
        private readonly IconButton btnPlaces = new IconButton();
        private readonly IconButton btnPrimary = new IconButton();
        private readonly IconButton btnTabLogin = new IconButton();
        private readonly IconButton btnTabRegister = new IconButton();
        private readonly IconButton btnTabGuest = new IconButton();
        private readonly Label lblGuestHint = new Label();
        private readonly IconButton btnMinimize = new IconButton();
        private readonly IconButton btnMaximize = new IconButton();
        private readonly IconButton btnClose = new IconButton();

        private const int MaxOwnerPlaces = 5;

        private Mode mode;
        private bool busy;
        private bool ownerAccount;                                       // registering as a place owner (needs places)
        private IReadOnlyList<Poi> ownerPlaces = new List<Poi>();        // the places the owner asked to manage
        private float scale = 1f, offsetX, offsetY;

        private const int WmNcLButtonDown = 0xA1;
        private const int HtCaption = 0x2;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        // Shown over the main window to a guest who pressed something that needs an account (see MainForm.RunSignedIn): the sentence says
        // what for, "Quay lại" replaces "Khách" and nothing is restored from a saved session. Null for the start-up sign-in screen.
        private readonly string reason;

        private bool IsModal => reason != null;

        public LoginForm(AppSession session) : this(session, null)
        {
        }

        public LoginForm(AppSession session, string reason)
        {
            this.session = session;
            this.reason = reason;
            Text = "Hue Travel";
            Icon = AppImages.AppIcon();
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.None;
            KeyPreview = true;
            BackColor = ThemeColors.Slate100;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            int width = Math.Min(Ui.S(1280), (int)(area.Width * 0.96));
            ClientSize = new Size(width, (int)Math.Round(width * ImageHeight / ImageWidth));   // the restored (not maximised) size
            MaximizedBounds = area;                                                              // a borderless window must not cover the taskbar
            WindowState = FormWindowState.Maximized;

            BuildControls();
            if (IsModal)
            {
                lblGuestHint.Text = reason;
                btnTabGuest.Text = "Quay lại";   // back to the map, not into the app
            }
            SetMode(session.IsOnline ? Mode.Login : Mode.Offline);
        }

        // ------------------------------------------------------------------ geometry

        // Cover the window with the picture; the card stays fully visible by shifting the picture vertically when the window is wide.
        private void ComputeTransform()
        {
            int w = Math.Max(1, ClientSize.Width), h = Math.Max(1, ClientSize.Height);
            scale = Math.Max(w / ImageWidth, h / ImageHeight);
            offsetX = (w - ImageWidth * scale) / 2f;
            float wanted = h / 2f - (CardRect.Y + CardRect.Height / 2f) * scale;
            offsetY = Math.Min(0f, Math.Max(h - ImageHeight * scale, wanted));
        }

        private Rectangle R(RectangleF image) =>
            Rectangle.Round(new RectangleF(offsetX + image.X * scale, offsetY + image.Y * scale, image.Width * scale, image.Height * scale));

        private Rectangle R(float x, float y, float w, float h) => R(new RectangleF(x, y, w, h));

        // Design pixels (what ThemeFonts takes) for a size given in picture pixels; whole numbers so the font cache stays small.
        private float FontPx(float imagePx) => (float)Math.Round(imagePx * scale / Ui.Scale);

        // ------------------------------------------------------------------ building

        private void BuildControls()
        {
            lblInfo.Text = "Bạn vẫn xem được bản đồ, tra cứu địa điểm và lên lộ trình tham quan. Tài khoản, địa điểm yêu thích và lộ trình đã lưu cần có máy chủ.";
            lblInfo.ForeColor = Color.FromArgb(73, 69, 98);
            lblInfo.TextAlign = ContentAlignment.MiddleLeft;
            lblInfo.Visible = false;
            lblGuestHint.Text = "Chỉ muốn xem bản đồ? Chọn \"Khách\" để vào ứng dụng ngay, không cần tài khoản.";
            lblGuestHint.ForeColor = Color.FromArgb(110, 106, 130);
            lblGuestHint.TextAlign = ContentAlignment.TopCenter;
            foreach (Label l in new[] { lblTitle, lblSubtitle, lblFooter, lblMessage, lblInfo, lnkForgot, lblGuestHint })
            {
                l.AutoSize = false;
                l.UseMnemonic = false;
                l.BackColor = Color.Transparent;
            }
            lblTitle.ForeColor = Color.FromArgb(58, 29, 90);
            lblSubtitle.ForeColor = Color.FromArgb(73, 69, 98);
            lblFooter.ForeColor = Color.FromArgb(110, 106, 130);
            lblFooter.Text = "Hue Travel – Hành trình lưu giữ những khoảnh khắc đẹp";
            lblMessage.TextAlign = ContentAlignment.MiddleCenter;
            lnkForgot.Text = "Quên mật khẩu?";
            lnkForgot.ForeColor = ThemeColors.BrandPurple;
            lnkForgot.TextAlign = ContentAlignment.MiddleRight;
            lnkForgot.Cursor = Cursors.Hand;
            lnkForgot.MouseEnter += (s, e) => lnkForgot.ForeColor = ThemeColors.BrandPurpleDark;
            lnkForgot.MouseLeave += (s, e) => lnkForgot.ForeColor = ThemeColors.BrandPurple;
            lnkForgot.Click += (s, e) =>
                ShowMessage("Hệ thống chưa dùng email nên chưa tự đặt lại mật khẩu được. Hãy liên hệ quản trị viên để được cấp mật khẩu tạm.", false);

            chkRemember.Text = "Ghi nhớ đăng nhập";
            chkRemember.Checked = true;

            fieldName.IconName = "account_circle"; fieldName.Hint = "Họ và tên";
            fieldUser.IconName = "account_circle"; fieldUser.Hint = "Tên đăng nhập";
            fieldPass.IconName = "lock"; fieldPass.Hint = "Mật khẩu"; fieldPass.IsPassword = true;
            fieldPass2.IconName = "lock"; fieldPass2.Hint = "Nhập lại mật khẩu"; fieldPass2.IsPassword = true;
            fieldName.EnterPressed += (s, e) => fieldUser.FocusInput();
            fieldUser.EnterPressed += async (s, e) =>
            {
                if (mode == Mode.Login) fieldPass.FocusInput(); else fieldPass.FocusInput();
                await Task.CompletedTask;
            };
            fieldPass.EnterPressed += async (s, e) =>
            {
                if (mode == Mode.Login) await LoginAsync(); else fieldPass2.FocusInput();
            };
            fieldPass2.EnterPressed += async (s, e) => await RegisterAsync();

            StyleButton(btnPrimary, true);
            btnTabLogin.Text = "Đăng nhập";
            btnTabRegister.Text = "Đăng ký";
            btnTabGuest.Text = "Khách";
            btnTabLogin.Click += (s, e) => { if (!busy) SetMode(Mode.Login); };
            btnTabRegister.Click += (s, e) => { if (!busy) SetMode(Mode.Register); };
            btnTabGuest.Click += (s, e) =>
            {
                if (busy) return;
                if (IsModal) DialogResult = DialogResult.Cancel;   // the guest changes their mind: back to the map
                else EnterApp(true);                               // no sign-in: straight into the app
            };
            StyleChoice(btnTypeUser, "Người dùng", "account_circle", true);
            StyleChoice(btnTypeOwner, "Chủ quản địa điểm", "pin_drop", false);
            StyleChoice(btnPlaces, "Chọn địa điểm bạn quản lý", "add", false);
            btnTypeUser.Click += (s, e) => SetAccountType(false);
            btnTypeOwner.Click += (s, e) => SetAccountType(true);
            btnPlaces.Click += (s, e) => ChoosePlaces();
            btnPrimary.Click += async (s, e) => await PrimaryAsync();

            // Window buttons like the main window's title bar (minimise, maximise / restore, close).
            StyleWindowButton(btnMinimize, "minimize", false);
            StyleWindowButton(btnMaximize, "crop_square", false);
            StyleWindowButton(btnClose, "close", true);
            btnMinimize.Click += (s, e) => WindowState = FormWindowState.Minimized;
            btnMaximize.Click += (s, e) => ToggleMaximize();
            btnClose.Click += (s, e) => DialogResult = DialogResult.Cancel;

            Control[] all =
            {
                lblTitle, lblSubtitle, lblFooter, lblMessage, lblInfo, lnkForgot, chkRemember, fieldName, fieldUser, fieldPass, fieldPass2,
                btnTypeUser, btnTypeOwner, btnPlaces, btnPrimary, lblGuestHint, btnTabLogin, btnTabRegister, btnTabGuest,
                btnMinimize, btnMaximize, btnClose,
            };
            foreach (Control c in all) Controls.Add(c);
        }

        // Tab look: the chosen tab is a filled purple pill, the others sit flat on the track and light up on hover.
        private static void StyleTab(IconButton tab, bool selected)
        {
            tab.AutoWidth = false;
            tab.TabStop = false;
            tab.Radius = 14;
            tab.HorizontalPadding = 4;
            tab.ForeColor = selected ? Color.White : ThemeColors.BrandPurple;
            tab.FillColor = selected ? ThemeColors.BrandPurple : ThemeColors.BrandPurpleLight;
            tab.HoverFillColor = selected ? Color.FromArgb(110, 54, 163) : Color.White;
            tab.BorderColor = Color.Transparent;
            tab.Elevated = selected;
        }

        // Leaves the sign-in screen for the main window; a guest has no account and uses what needs none.
        private void EnterApp(bool guest)
        {
            session.IsGuest = guest;
            DialogResult = DialogResult.OK;
        }

        private static void StyleButton(IconButton button, bool primary)
        {
            button.AutoWidth = false;
            button.Radius = 14;
            button.Gap = 10;
            button.IconSize = 22;
            button.TrailingIconSize = 22;
            if (primary)
            {
                button.ForeColor = Color.White;
                button.FillColor = ThemeColors.BrandPurple;
                button.HoverFillColor = Color.FromArgb(110, 54, 163);
                button.TrailingIconColor = Color.White;
                button.TrailingIconName = "arrow_forward";
                button.Elevated = true;
            }
            else
            {
                button.ForeColor = ThemeColors.BrandPurple;
                button.IconColor = ThemeColors.BrandPurple;
                button.FillColor = Color.White;
                button.HoverFillColor = ThemeColors.BrandPurpleLight;
                button.BorderColor = ThemeColors.BrandPurple;
            }
        }

        // The two account types and the place button share one look: filled purple when chosen, white with a purple edge otherwise.
        private static void StyleChoice(IconButton button, string text, string icon, bool selected)
        {
            button.AutoWidth = false;
            button.Text = text;
            button.IconName = icon;
            button.IconSize = 20;
            button.Gap = 8;
            button.Radius = 12;
            button.HorizontalPadding = 8;
            button.ForeColor = selected ? Color.White : ThemeColors.BrandPurple;
            button.IconColor = selected ? Color.White : ThemeColors.BrandPurple;
            button.FillColor = selected ? ThemeColors.BrandPurple : Color.White;
            button.HoverFillColor = selected ? Color.FromArgb(110, 54, 163) : ThemeColors.BrandPurpleLight;
            button.BorderColor = ThemeColors.BrandPurple;
        }

        private void SetAccountType(bool owner)
        {
            ownerAccount = owner;
            StyleChoice(btnTypeUser, "Người dùng", "account_circle", !owner);
            StyleChoice(btnTypeOwner, "Chủ quản địa điểm", "pin_drop", owner);
            lblSubtitle.Text = owner
                ? "Chủ quản chọn địa điểm mình quản lý; quản trị viên sẽ xác minh rồi duyệt."
                : "Lưu lộ trình và đánh giá địa điểm";
            ShowMessage(null, false);
            ApplyModeLayout();
            ApplyRegisterVisibility();
            Invalidate();
        }

        private void ApplyRegisterVisibility()
        {
            bool register = mode == Mode.Register;
            btnTypeUser.Visible = btnTypeOwner.Visible = register;
            btnPlaces.Visible = register && ownerAccount;
        }

        private void ChoosePlaces()
        {
            IReadOnlyList<Poi> chosen = Dialogs.PickPlaces(this, "Địa điểm bạn quản lý", MaxOwnerPlaces, ownerPlaces.Select(p => p.Id), "Xong");
            if (chosen == null) return;
            ownerPlaces = chosen;
            string text = chosen.Count == 0 ? "Chọn địa điểm bạn quản lý" : chosen.Count == 1 ? chosen[0].Name : "Đã chọn " + chosen.Count + " địa điểm";
            StyleChoice(btnPlaces, text, chosen.Count == 0 ? "add" : "check", chosen.Count > 0);
            ShowMessage(null, false);
        }

        private static void StyleWindowButton(IconButton button, string icon, bool isClose)
        {
            button.AutoWidth = false;
            button.TabStop = false;
            button.IconName = icon;
            button.IconSize = 16;
            button.Radius = 0;
            button.IconColor = ThemeColors.Slate700;
            button.FillColor = Color.FromArgb(150, 255, 255, 255);
            button.HoverFillColor = isClose ? ThemeColors.Error : Color.FromArgb(235, 255, 255, 255);
            if (isClose) button.HoverIconColor = Color.White;
        }

        private void ToggleMaximize()
        {
            WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        }

        // ------------------------------------------------------------------ layout

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (btnMaximize != null) btnMaximize.IconName = WindowState == FormWindowState.Maximized ? "filter_none" : "crop_square";
            RebuildBackdrop();
            LayoutControls();
            Invalidate();
        }

        private void RebuildBackdrop()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
            ComputeTransform();
            var next = new Bitmap(ClientSize.Width, ClientSize.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(next))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.DrawImage(picture, new RectangleF(offsetX, offsetY, ImageWidth * scale, ImageHeight * scale));
            }
            Bitmap old = backdrop;
            backdrop = next;
            if (old != null) old.Dispose();
        }

        private void LayoutControls()
        {
            if (btnClose == null) return;
            int buttonWidth = Ui.S(46), buttonHeight = Ui.S(36);
            btnClose.SetBounds(ClientSize.Width - buttonWidth, 0, buttonWidth, buttonHeight);
            btnMaximize.SetBounds(btnClose.Left - buttonWidth, 0, buttonWidth, buttonHeight);
            btnMinimize.SetBounds(btnMaximize.Left - buttonWidth, 0, buttonWidth, buttonHeight);

            Font title = ThemeFonts.Bold(FontPx(36));   // 46 was taller than its box and lost the feet of the letters
            Font sub = ThemeFonts.Regular(FontPx(21));
            Font field = ThemeFonts.Regular(FontPx(21));
            Font button = ThemeFonts.SemiBold(FontPx(23));
            lblTitle.Font = title;
            lblSubtitle.Font = sub;
            lblFooter.Font = ThemeFonts.Regular(FontPx(15));
            lblMessage.Font = ThemeFonts.SemiBold(FontPx(18));
            lnkForgot.Font = ThemeFonts.SemiBold(FontPx(18));
            chkRemember.Font = ThemeFonts.Medium(FontPx(17));
            foreach (AuthField f in new[] { fieldName, fieldUser, fieldPass, fieldPass2 }) f.Font = field;
            btnPrimary.Font = button;
            btnTabLogin.Font = btnTabRegister.Font = btnTabGuest.Font = ThemeFonts.SemiBold(FontPx(19));
            lblGuestHint.Font = ThemeFonts.Regular(FontPx(17));
            btnTypeUser.Font = btnTypeOwner.Font = btnPlaces.Font = ThemeFonts.SemiBold(FontPx(16));   // "Chủ quản địa điểm" must fit beside its icon
            ApplyModeLayout();
        }

        private void ApplyModeLayout()
        {
            // The three tabs share the track, each with a 4 px margin inside it.
            btnTabLogin.Bounds = R(1058, 120, 128, 50);
            btnTabRegister.Bounds = R(1186, 120, 128, 50);
            btnTabGuest.Bounds = R(1314, 120, 128, 50);
            lblFooter.TextAlign = ContentAlignment.MiddleCenter;
            lblFooter.Bounds = R(1006, 812, 488, 28);
            // Vertical rhythm (picture pixels, the card is 80..854): every box is tall enough for its text, and the boxes of the
            // forms are 14-24 apart (they were 6-8, which looked crowded) with the leftover room spent between the blocks.
            if (mode == Mode.Login)
            {
                lblTitle.Bounds = R(1054, 200, 400, 64);
                lblSubtitle.Bounds = R(1054, 268, 400, 40);
                fieldUser.Bounds = R(1054, 340, 392, 60);
                fieldPass.Bounds = R(1054, 424, 392, 60);       // 24 below the user name box
                chkRemember.Bounds = R(1054, 510, 250, 36);
                lnkForgot.Bounds = R(1290, 510, 158, 36);
                btnPrimary.Bounds = R(1054, 572, 392, 60);
                lblMessage.Bounds = R(1030, 644, 440, 54);
                lblGuestHint.Bounds = R(1070, 726, 360, 60);
            }
            else if (mode == Mode.Register)
            {
                lblTitle.Bounds = R(1054, 188, 400, 60);
                lblSubtitle.Bounds = R(1054, 250, 400, 68);     // two lines of text plus the room the label keeps above the first
                btnTypeUser.Bounds = R(1054, 326, 192, 46);
                btnTypeOwner.Bounds = R(1254, 326, 192, 46);
                fieldName.Bounds = R(1054, 384, 392, 52);
                fieldUser.Bounds = R(1054, 450, 392, 52);       // 14 between the boxes
                fieldPass.Bounds = R(1054, 516, 392, 52);
                fieldPass2.Bounds = R(1054, 582, 392, 52);
                if (ownerAccount)
                {
                    btnPlaces.Bounds = R(1054, 648, 392, 44);
                    btnPrimary.Bounds = R(1054, 704, 392, 52);
                    lblMessage.Bounds = R(1030, 762, 440, 40);
                }
                else
                {
                    btnPrimary.Bounds = R(1054, 664, 392, 58);
                    lblMessage.Bounds = R(1030, 730, 440, 60);
                }
            }
            else
            {
                // Offline: no tabs (there are no accounts), so the title sits higher; one box of explanation and one button.
                lblTitle.Bounds = R(1054, 160, 400, 70);
                lblSubtitle.Font = ThemeFonts.Regular(FontPx(21));
                lblSubtitle.Bounds = R(1054, 236, 392, 130);
                lblInfo.Font = ThemeFonts.Regular(FontPx(18));
                lblInfo.Bounds = R(1078, 396, 344, 164);
                btnPrimary.Bounds = R(1054, 592, 392, 57);
                lblMessage.Bounds = R(1030, 668, 440, 40);
            }
            // The register subtitle is a sentence of two lines; a smaller type keeps both inside their box.
            if (mode == Mode.Register) lblSubtitle.Font = ThemeFonts.Regular(FontPx(18));
            else if (mode == Mode.Login) lblSubtitle.Font = ThemeFonts.Regular(FontPx(21));
        }

        // ------------------------------------------------------------------ modes

        private void SetMode(Mode next)
        {
            mode = next;
            ShowMessage(null, false);
            bool login = next == Mode.Login, register = next == Mode.Register;
            lblSubtitle.Visible = lblTitle.Visible = lblFooter.Visible = true;
            fieldUser.Visible = fieldPass.Visible = login || register;
            fieldName.Visible = fieldPass2.Visible = register;
            if (!register) ownerAccount = false;
            ApplyRegisterVisibility();
            chkRemember.Visible = lnkForgot.Visible = lblGuestHint.Visible = login;
            btnPrimary.Visible = true;
            btnTabLogin.Visible = btnTabRegister.Visible = btnTabGuest.Visible = login || register;
            StyleTab(btnTabLogin, login);
            StyleTab(btnTabRegister, register);
            StyleTab(btnTabGuest, false);   // never stays chosen: it leaves the screen
            lblInfo.Visible = next == Mode.Offline;

            if (login)
            {
                lblTitle.Text = "Chào mừng trở lại";
                lblSubtitle.Text = "Cùng khám phá vẻ đẹp Huế";
                fieldPass.Hint = "Mật khẩu";
                btnPrimary.IconName = ""; btnPrimary.Text = "Đăng nhập";
                fieldUser.FocusInput();
            }
            else if (register)
            {
                lblTitle.Text = "Tạo tài khoản";
                lblSubtitle.Text = ownerAccount ? "Chủ quản chọn địa điểm mình quản lý; quản trị viên sẽ xác minh rồi duyệt." : "Lưu lộ trình và đánh giá địa điểm";
                fieldPass.Hint = "Mật khẩu (tối thiểu " + AuthValidation.MinPasswordLength + " ký tự)";
                btnPrimary.IconName = ""; btnPrimary.Text = "Tạo tài khoản";
                fieldName.FocusInput();
            }
            else
            {
                lblTitle.Text = "Chào mừng";
                lblSubtitle.Text = "Chưa cấu hình địa chỉ máy chủ nên chưa có tài khoản. Ứng dụng sẽ chạy ngoại tuyến với dữ liệu đi kèm.";
                btnPrimary.TrailingIconName = "arrow_forward";
                btnPrimary.IconName = ""; btnPrimary.Text = "Tiếp tục ngoại tuyến";
            }
            if (backdrop != null) { ApplyModeLayout(); Invalidate(); }
        }

        private void ShowMessage(string text, bool isError)
        {
            lblMessage.Text = text ?? "";
            lblMessage.ForeColor = isError ? Color.FromArgb(180, 35, 24) : ThemeColors.BrandPurple;
            lblMessage.Visible = !string.IsNullOrEmpty(text);
        }

        private void SetBusy(bool value, string text = "")
        {
            busy = value;
            foreach (AuthField f in new[] { fieldName, fieldUser, fieldPass, fieldPass2 }) f.Enabled = !value;
            chkRemember.Enabled = btnPrimary.Enabled = !value;
            btnTabLogin.Enabled = btnTabRegister.Enabled = btnTabGuest.Enabled = !value;
            btnTypeUser.Enabled = btnTypeOwner.Enabled = btnPlaces.Enabled = !value;
            if (value) { btnPrimary.Text = text; btnPrimary.TrailingIconName = ""; }
            else
            {
                btnPrimary.TrailingIconName = "arrow_forward";
                btnPrimary.Text = mode == Mode.Login ? "Đăng nhập" : mode == Mode.Register ? "Tạo tài khoản" : "Tiếp tục ngoại tuyến";
            }
        }

        // ------------------------------------------------------------------ sign in / register

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (mode == Mode.Offline) return;
            if (IsModal) { fieldUser.FocusInput(); return; }   // asked to sign in over the app: no saved session to restore
            if (!string.IsNullOrEmpty(session.Notice)) ShowMessage(session.Notice, false);

            // A remembered session goes straight in.
            SetBusy(true, "Đang kiểm tra phiên đăng nhập…");
            try
            {
                if (await session.Auth.RestoreAsync())
                {
                    if (!IsDisposed) EnterApp(false);
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoginForm.Restore: " + ex.Message);
            }
            if (IsDisposed) return;
            SetBusy(false);
            if (mode == Mode.Login) fieldUser.FocusInput();
        }

        private async Task PrimaryAsync()
        {
            if (mode == Mode.Login) await LoginAsync();
            else if (mode == Mode.Register) await RegisterAsync();
            else EnterApp(false);   // offline: no accounts, the data that ships with the app
        }

        private async Task LoginAsync()
        {
            if (busy || mode != Mode.Login) return;
            string user = fieldUser.Text.Trim();
            fieldUser.HasError = !AuthValidation.IsUsername(user);
            fieldPass.HasError = fieldPass.Text.Length == 0;
            if (fieldUser.HasError) { ShowMessage("Tên đăng nhập gồm 3 đến 40 ký tự: chữ, số, _ hoặc .", true); fieldUser.FocusInput(); return; }
            if (fieldPass.HasError) { ShowMessage("Hãy nhập mật khẩu", true); fieldPass.FocusInput(); return; }

            ShowMessage(null, false);
            SetBusy(true, "Đang đăng nhập…");
            string error;
            try { error = await session.Auth.LoginAsync(user, fieldPass.Text, chkRemember.Checked); }
            catch (Exception ex) { error = ex.Message; }
            if (IsDisposed) return;
            SetBusy(false);
            if (error != null) { ShowMessage(error, true); return; }
            fieldPass.Text = "";
            EnterApp(false);
        }

        private async Task RegisterAsync()
        {
            if (busy || mode != Mode.Register) return;
            fieldName.HasError = !AuthValidation.IsFullName(fieldName.Text);
            fieldUser.HasError = !AuthValidation.IsUsername(fieldUser.Text);
            fieldPass.HasError = !AuthValidation.IsStrongEnoughPassword(fieldPass.Text);
            fieldPass2.HasError = fieldPass.Text != fieldPass2.Text;
            if (fieldName.HasError) { ShowMessage("Vui lòng nhập họ và tên", true); return; }
            if (fieldUser.HasError) { ShowMessage("Tên đăng nhập gồm 3 đến 40 ký tự: chữ, số, _ hoặc .", true); return; }
            if (fieldPass.HasError) { ShowMessage("Mật khẩu cần tối thiểu " + AuthValidation.MinPasswordLength + " ký tự", true); return; }
            if (fieldPass2.HasError) { ShowMessage("Hai mật khẩu chưa giống nhau", true); return; }
            if (ownerAccount && ownerPlaces.Count == 0) { ShowMessage("Chủ quản cần chọn ít nhất một địa điểm mình quản lý", true); return; }

            ShowMessage(null, false);
            SetBusy(true, "Đang tạo tài khoản…");
            string error;
            try { error = await session.Auth.RegisterAsync(fieldUser.Text.Trim(), fieldPass.Text, fieldName.Text.Trim(), true, ownerAccount ? ownerPlaces.Select(p => p.Id) : null); }
            catch (Exception ex) { error = ex.Message; }
            if (IsDisposed) return;
            SetBusy(false);
            if (error != null) { ShowMessage(error, true); return; }
            EnterApp(false);
        }

        // ------------------------------------------------------------------ window behaviour

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || WindowState == FormWindowState.Maximized) return;
            ReleaseCapture();   // the empty part of the picture drags the window
            SendMessage(Handle, WmNcLButtonDown, HtCaption, 0);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Button == MouseButtons.Left) ToggleMaximize();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; DialogResult = DialogResult.Cancel; }
            base.OnKeyDown(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && backdrop != null) backdrop.Dispose();
            base.Dispose(disposing);
        }

        // ------------------------------------------------------------------ painting

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (backdrop != null) g.DrawImageUnscaled(backdrop, 0, 0);
            else g.Clear(BackColor);

            PaintCard(g);
            if (mode == Mode.Offline) PaintOfflineBox(g);
            else PaintTabTrack(g);
        }

        // The soft rounded strip the three tabs sit on.
        private void PaintTabTrack(Graphics g)
        {
            using (GraphicsPath path = Ui.RoundedRect(R(TabTrack), 18f * scale))
            using (var fill = new SolidBrush(ThemeColors.BrandPurpleLight))
            {
                g.FillPath(fill, path);
            }
        }

        // Offline: the explanation sits in a soft purple box where the fields would be.
        private void PaintOfflineBox(Graphics g)
        {
            Rectangle box = R(1054, 386, 392, 184);
            using (GraphicsPath path = Ui.RoundedRect(box, 14f * scale))
            using (var fill = new SolidBrush(ThemeColors.BrandPurpleLight))
            using (var pen = new Pen(Color.FromArgb(70, 91, 42, 134), 1.2f))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }
            int bar = Math.Max(3, Ui.S(4));
            using (var accent = new SolidBrush(ThemeColors.BrandPurple)) g.FillRectangle(accent, box.X + Ui.S(14), box.Y + Ui.S(18), bar, box.Height - Ui.S(36));
        }

        // The whole card baked into the picture is hidden behind a plain card of the same colour and shape, so the card is
        // ours in every mode and nothing of the picture's own text shows through.
        private void PaintCard(Graphics g)
        {
            Rectangle card = R(CardRect);
            using (GraphicsPath path = Ui.RoundedRect(card, 34f * scale))
            using (var fill = new SolidBrush(Color.FromArgb(252, 249, 248)))
            using (var pen = new Pen(Color.FromArgb(200, 255, 255, 255), 1.5f))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }
        }
    }
}
