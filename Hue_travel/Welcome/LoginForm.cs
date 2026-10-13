using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Hue_travel.Auth;
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

        // Parts of the picture's card that hold baked text or controls: hidden behind a patch of the card's own colour.
        private static readonly RectangleF[] LoginPatches =
        {
            new RectangleF(1046, 140, 270, 150),   // title and subtitle
            new RectangleF(1050, 296, 400, 66),    // user name box
            new RectangleF(1050, 380, 400, 66),    // password box
            new RectangleF(1050, 455, 400, 44),    // remember / forgot row
            new RectangleF(1050, 512, 398, 66),    // sign-in button
            new RectangleF(1050, 598, 398, 30),    // "or" divider
            new RectangleF(1050, 642, 398, 66),    // register button
            new RectangleF(1086, 774, 400, 36),    // footer text
        };

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
        private readonly IconButton btnPrimary = new IconButton();
        private readonly IconButton btnSecondary = new IconButton();
        private readonly IconButton btnMinimize = new IconButton();
        private readonly IconButton btnMaximize = new IconButton();
        private readonly IconButton btnClose = new IconButton();

        private Mode mode;
        private bool busy;
        private float scale = 1f, offsetX, offsetY;

        private const int WmNcLButtonDown = 0xA1;
        private const int HtCaption = 0x2;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        public LoginForm(AppSession session)
        {
            this.session = session;
            Text = "Hue Travel";
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

        private Color Tint(float imageX, float imageY)
        {
            float k = picture.Width / ImageWidth;
            int x = Math.Max(0, Math.Min(picture.Width - 1, (int)(imageX * k)));
            int y = Math.Max(0, Math.Min(picture.Height - 1, (int)(imageY * k)));
            return picture.Width > 1 ? picture.GetPixel(x, y) : Color.White;
        }

        // ------------------------------------------------------------------ building

        private void BuildControls()
        {
            lblInfo.Text = "Bạn vẫn xem được bản đồ, tra cứu địa điểm và lên lộ trình tham quan. Tài khoản, địa điểm yêu thích và lộ trình đã lưu cần có máy chủ.";
            lblInfo.ForeColor = Color.FromArgb(73, 69, 98);
            lblInfo.TextAlign = ContentAlignment.MiddleLeft;
            lblInfo.Visible = false;
            foreach (Label l in new[] { lblTitle, lblSubtitle, lblFooter, lblMessage, lblInfo, lnkForgot })
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
            StyleButton(btnSecondary, false);
            btnPrimary.Click += async (s, e) => await PrimaryAsync();
            btnSecondary.Click += (s, e) => SetMode(mode == Mode.Login ? Mode.Register : Mode.Login);

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
                btnPrimary, btnSecondary, btnMinimize, btnMaximize, btnClose,
            };
            foreach (Control c in all) Controls.Add(c);
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

            Font title = ThemeFonts.Bold(FontPx(46));
            Font sub = ThemeFonts.Regular(FontPx(23));
            Font field = ThemeFonts.Regular(FontPx(21));
            Font button = ThemeFonts.SemiBold(FontPx(23));
            lblTitle.Font = title;
            lblSubtitle.Font = sub;
            lblFooter.Font = ThemeFonts.Regular(FontPx(15));
            lblMessage.Font = ThemeFonts.SemiBold(FontPx(18));
            lnkForgot.Font = ThemeFonts.SemiBold(FontPx(18));
            chkRemember.Font = ThemeFonts.Medium(FontPx(17));
            foreach (AuthField f in new[] { fieldName, fieldUser, fieldPass, fieldPass2 }) f.Font = field;
            btnPrimary.Font = btnSecondary.Font = button;
            ApplyModeLayout();
        }

        private void ApplyModeLayout()
        {
            lblTitle.Bounds = R(1054, 160, 400, 70);
            lblFooter.TextAlign = mode == Mode.Login ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter;
            if (mode == Mode.Login)
            {
                lblSubtitle.Bounds = R(1054, 236, 400, 36);
                fieldUser.Bounds = R(1054, 300, 392, 57);
                fieldPass.Bounds = R(1054, 384, 392, 57);
                chkRemember.Bounds = R(1054, 458, 250, 36);
                lnkForgot.Bounds = R(1290, 458, 158, 36);
                btnPrimary.Bounds = R(1054, 517, 392, 57);
                btnSecondary.Bounds = R(1054, 647, 392, 54);
                lblMessage.Bounds = R(1030, 708, 440, 62);
                lblFooter.Bounds = R(1088, 778, 396, 32);
            }
            else if (mode == Mode.Register)
            {
                lblSubtitle.Bounds = R(1054, 232, 400, 60);
                fieldName.Bounds = R(1054, 300, 392, 57);
                fieldUser.Bounds = R(1054, 372, 392, 57);
                fieldPass.Bounds = R(1054, 444, 392, 57);
                fieldPass2.Bounds = R(1054, 516, 392, 57);
                btnPrimary.Bounds = R(1054, 596, 392, 57);
                btnSecondary.Bounds = R(1054, 746, 392, 54);
                lblMessage.Bounds = R(1030, 658, 440, 44);
                lblFooter.Bounds = R(1006, 812, 488, 28);
            }
            else
            {
                // Offline: the same card as the sign-in (title, one box of explanation, the button in the same place).
                lblSubtitle.Font = ThemeFonts.Regular(FontPx(21));
                lblSubtitle.Bounds = R(1054, 236, 392, 130);
                lblInfo.Font = ThemeFonts.Regular(FontPx(18));
                lblInfo.Bounds = R(1078, 396, 344, 164);
                btnPrimary.Bounds = R(1054, 592, 392, 57);
                lblMessage.Bounds = R(1030, 668, 440, 40);
                lblFooter.TextAlign = ContentAlignment.MiddleLeft;
                lblFooter.Bounds = R(1088, 778, 396, 32);
            }
            if (mode != Mode.Offline) lblSubtitle.Font = ThemeFonts.Regular(FontPx(23));
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
            chkRemember.Visible = lnkForgot.Visible = login;
            btnPrimary.Visible = true;
            btnSecondary.Visible = login || register;
            lblInfo.Visible = next == Mode.Offline;

            if (login)
            {
                lblTitle.Text = "Đăng nhập";
                lblSubtitle.Text = "Cùng khám phá vẻ đẹp Huế";
                fieldPass.Hint = "Mật khẩu";
                btnPrimary.IconName = ""; btnPrimary.Text = "Đăng nhập";
                btnSecondary.Text = "Đăng ký"; btnSecondary.IconName = "account_circle";
                fieldUser.FocusInput();
            }
            else if (register)
            {
                lblTitle.Text = "Đăng ký";
                lblSubtitle.Text = "Tạo tài khoản để lưu lộ trình và địa điểm yêu thích";
                fieldPass.Hint = "Mật khẩu (tối thiểu " + AuthValidation.MinPasswordLength + " ký tự)";
                btnPrimary.IconName = ""; btnPrimary.Text = "Tạo tài khoản";
                btnSecondary.Text = "Đăng nhập"; btnSecondary.IconName = "account_circle";
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
            chkRemember.Enabled = btnSecondary.Enabled = btnPrimary.Enabled = !value;
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
            if (!string.IsNullOrEmpty(session.Notice)) ShowMessage(session.Notice, false);

            // A remembered session goes straight in.
            SetBusy(true, "Đang kiểm tra phiên đăng nhập…");
            try
            {
                if (await session.Auth.RestoreAsync())
                {
                    if (!IsDisposed) DialogResult = DialogResult.OK;
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
            else DialogResult = DialogResult.OK;
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
            DialogResult = DialogResult.OK;
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

            ShowMessage(null, false);
            SetBusy(true, "Đang tạo tài khoản…");
            string error;
            try { error = await session.Auth.RegisterAsync(fieldUser.Text.Trim(), fieldPass.Text, fieldName.Text.Trim(), true); }
            catch (Exception ex) { error = ex.Message; }
            if (IsDisposed) return;
            SetBusy(false);
            if (error != null) { ShowMessage(error, true); return; }
            DialogResult = DialogResult.OK;
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

            if (mode == Mode.Register) PaintCardCover(g);
            else PaintLoginPatches(g);
        }

        // The picture's card already shows the sign-in design; only the parts with baked text get a patch of its own colour.
        private void PaintLoginPatches(Graphics g)
        {
            foreach (RectangleF patch in LoginPatches)
            {
                // The card's colour at the patch's top-left and bottom-left corners (clear of the baked text), blended downwards.
                Color top = Tint(patch.X + 2, patch.Y + 2), bottom = Tint(patch.X + 2, patch.Bottom - 3);
                Rectangle r = R(patch);
                using (var brush = new LinearGradientBrush(new Rectangle(r.X, r.Y - 1, r.Width, r.Height + 2), top, bottom, LinearGradientMode.Vertical))
                {
                    g.FillRectangle(brush, r);
                }
            }

            if (mode == Mode.Offline)
            {
                // The explanation sits in a soft purple box where the fields were.
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
                return;
            }

            // "Hoặc" between the two buttons.
            Rectangle band = R(1054, 598, 392, 30);
            string text = "Hoặc";
            Font font = ThemeFonts.Regular(FontPx(19));
            Size size = TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            int centerX = band.X + band.Width / 2, y = band.Y + band.Height / 2;
            using (var pen = new Pen(Color.FromArgb(200, 180, 176, 190)))
            {
                g.DrawLine(pen, band.X, y, centerX - size.Width / 2 - Ui.S(14), y);
                g.DrawLine(pen, centerX + size.Width / 2 + Ui.S(14), y, band.Right, y);
            }
            TextRenderer.DrawText(g, text, font, new Point(centerX - size.Width / 2, y - size.Height / 2), Color.FromArgb(120, 116, 138), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }

        // Register / offline: the whole card of the picture is hidden behind a plain card of the same colour and shape.
        private void PaintCardCover(Graphics g)
        {
            Rectangle card = R(CardRect);
            using (GraphicsPath path = Ui.RoundedRect(card, 34f * scale))
            using (var fill = new SolidBrush(Color.FromArgb(252, 249, 248)))
            using (var pen = new Pen(Color.FromArgb(200, 255, 255, 255), 1.5f))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }
            if (mode != Mode.Register) return;

            Rectangle band = R(1054, 724, 392, 1);
            string text = "Đã có tài khoản?";
            Font font = ThemeFonts.Regular(FontPx(19));
            Size size = TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            int centerX = band.X + band.Width / 2, y = band.Y;
            using (var pen = new Pen(ThemeColors.Slate300))
            {
                g.DrawLine(pen, band.X, y, centerX - size.Width / 2 - Ui.S(14), y);
                g.DrawLine(pen, centerX + size.Width / 2 + Ui.S(14), y, band.Right, y);
            }
            TextRenderer.DrawText(g, text, font, new Point(centerX - size.Width / 2, y - size.Height / 2), ThemeColors.Slate500, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }
    }
}
