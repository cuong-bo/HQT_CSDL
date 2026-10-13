# Triển khai lên VPS Ubuntu (HTTP)

Kiến trúc: máy người dùng (app Windows) → HTTP cổng 80 → **Caddy** → **API .NET 8** → **PostgreSQL 17 + PostGIS + pgRouting**,
cả bốn chạy trong Docker Compose trên một VPS. PostgreSQL và API không có cổng nào mở ra ngoài (chỉ Caddy nghe cổng 80).

> **Quyết định: chỉ dùng HTTP, không HTTPS** (không có tên miền). Ai ở giữa đường truyền đọc được và sửa được dữ liệu.
> Hệ quả và cách giảm rủi ro nằm ở mục "Rủi ro khi dùng HTTP" bên dưới. HTTPS miễn phí vẫn làm được mà không cần mua tên miền
> (hostname miễn phí dạng `ten.duckdns.org` + Let's Encrypt); khi muốn, chỉ cần sửa `Caddyfile` và địa chỉ trong app.

> Hướng dẫn từng bước cho người mới: [HUONG-DAN-CAI-DAT.md](HUONG-DAN-CAI-DAT.md)
> (trong gói máy chủ là `HUONG-DAN-CAI-DAT.md`). Tài liệu này là bản tham chiếu vận hành.

## 1. Chuẩn bị VPS (một lần)

```bash
# trên VPS, quyền root, Ubuntu 22.04 / 24.04
git clone https://github.com/cuong-bo/HQT_CSDL.git /opt/hue-travel
sudo bash /opt/hue-travel/may-chu/scripts/setup-vps.sh            # thêm --harden-ssh sau khi đã dán khóa SSH cho người dùng deploy
```

Script cài Docker, bật tường lửa (chỉ cổng 22 và 80), fail2ban, tự cập nhật bản vá bảo mật, tạo người dùng `deploy`.
Đọc script trước khi chạy. Sau đó đăng nhập bằng `deploy`:

```bash
cd /opt/hue-travel/may-chu && cp .env.example .env      # điền 3 giá trị bí mật: openssl rand -base64 32
```

## 2. Lần chạy đầu

Dữ liệu shapefile nằm trong kho Git ở `may-chu/du-lieu/shp/` (khoảng 60 MB), nên `git clone` trên VPS là đủ; không cần chép riêng.

```bash
scripts/deploy.sh up        # dựng các container
scripts/deploy.sh import    # nạp shapefile, dựng đồ thị đường, kiểm tra (xem co-so-du-lieu/README.md)
scripts/deploy.sh status    # phải in {"status":"ok"}
```

Kiểm tra từ máy khác: `curl http://IP_VPS/healthz` và `curl "http://IP_VPS/v1/pois?group=stay"`.

### Ký phản hồi (khuyên dùng khi chạy HTTP)

Máy chủ có thể ký mọi phản hồi `/v1/*` bằng ECDSA để app phát hiện dữ liệu bị sửa giữa đường (HTTP không mã hóa, nhưng chữ ký chặn
việc giả vị trí, giả tuyến, giả cấu hình). Tạo một cặp khóa một lần:

```bash
scripts/gen-signing-key.sh
```

- Giá trị **riêng tư** đặt vào `.env` dòng `SIGNING_KEY=` rồi `scripts/deploy.sh up`.
- Giá trị **công khai** đặt vào app: `ApiPublicKey` trong `Hue_travel.exe.config`, hoặc file `%ProgramData%\CoDoGIS\public.key`
  (bộ cài có ô nhập), hoặc biến `HUE_API_PUBLIC_KEY`. Khi app có khóa công khai, nó **từ chối** mọi phản hồi thiếu chữ ký hoặc sai chữ ký.
- Chữ ký không che nội dung: mật khẩu và token vẫn đi dạng chữ thường. Đổi khóa thì phải phát hành lại khóa công khai cho mọi máy.

### Bản đồ nền (nhà cung cấp tile hợp lệ)

App mang sẵn địa chỉ tile của Google chỉ để phát triển. Khi có nhà cung cấp hợp lệ (MapTiler, Esri, máy chủ tile riêng), điền các dòng
`TILE_URL_ROADMAP`, `TILE_URL_SATELLITE`, `TILE_URL_HYBRID`, `TILE_URL_TERRAIN` và `TILE_ATTRIBUTION` trong `.env`
rồi `scripts/deploy.sh up`. App đọc `GET /v1/config` lúc khởi động và đổi sang địa chỉ mới, không cần phát hành lại.
Địa chỉ phải là http(s) và chứa `{x}`, `{y}`, `{zoom}`; địa chỉ sai bị bỏ qua và app giữ địa chỉ có sẵn.
Khóa của nhà cung cấp nằm trong địa chỉ này, nên ai chạy app đều thấy được; chọn loại khóa giới hạn theo tên miền hoặc theo mức dùng.

## 3. Cập nhật và quay lui

```bash
scripts/deploy.sh update     # git pull, dựng lại API, tự quay về bản cũ nếu /healthz không trả lời
scripts/deploy.sh rollback   # quay về bản trước theo ý mình
```

Migration mới (`co-so-du-lieu/migrations/V00N__*.sql`) được áp dụng khi chạy `deploy.sh import`; dữ liệu đã sửa qua app không bị ghi đè.

## 4. Tài khoản quản trị đầu tiên

Chưa có quản trị viên khi mới dựng. Đăng ký một tài khoản trong app, rồi cấp quyền một lần bằng SQL:

```bash
docker compose exec db psql -U huegis -d huegis \
  -c "update app_user set role = 'admin' where username = 'ten_dang_nhap'"
```

Từ đó quản trị viên tạo tài khoản chủ quản, duyệt yêu cầu nhận địa điểm và duyệt chỉnh sửa trong app.
Không có "quên mật khẩu" qua email: quản trị viên đặt lại mật khẩu tạm (hiện một lần), người dùng phải đổi ngay.
**Nên có ít nhất 2 quản trị viên**: sai mật khẩu 5 lần khóa tài khoản 15 phút, nên một người biết tên đăng nhập quản trị có thể làm
tài khoản đó tạm khóa. Mở khóa ngay bằng SQL: `update app_user set locked_until = null, failed_logins = 0 where username = '...'`.

## 5. Sao lưu và khôi phục

```bash
crontab -e     # thêm dòng:  30 2 * * *  /opt/hue-travel/may-chu/scripts/backup.sh >> /var/log/hue-backup.log 2>&1
```

Giữ 7 bản theo ngày và 4 bản theo tuần ở `/var/backups/hue-travel`. Đặt `BACKUP_REMOTE=user@may-khac:/duong/dan`
để chép thêm một bản ra ngoài VPS (bắt buộc nếu muốn sống sót khi VPS hỏng).

**Diễn tập khôi phục ít nhất một lần** (không đụng dữ liệu thật):

```bash
scripts/restore.sh /var/backups/hue-travel/daily/huegis-YYYY-MM-DD.dump
```

## 6. Giám sát

- Đăng ký một dịch vụ kiểm tra miễn phí (UptimeRobot, Better Stack) gọi `http://IP_VPS/healthz` mỗi 5 phút.
- `scripts/deploy.sh logs` xem nhật ký; `df -h` định kỳ (ổ đầy là lỗi phổ biến nhất).
- Quét cổng từ ngoài (`nmap IP_VPS`) chỉ nên thấy 22 và 80.
- **Không thêm dòng `ports:` cho `db` hoặc `api`** trong compose: Docker mở cổng vượt qua tường lửa `ufw`.

## Rủi ro khi dùng HTTP

| Rủi ro | Cách giảm đã làm / nên làm |
|---|---|
| Mật khẩu và token đi dạng chữ thường trên đường truyền | Mật khẩu chỉ dùng cho app này, **không dùng lại mật khẩu ở nơi khác**; token truy cập sống 15 phút; làm mới token dùng một lần. Nên chuyển HTTPS khi có thể |
| Kẻ ở giữa sửa dữ liệu trả về (giả vị trí, giả tuyến, giả `/v1/config`) | Phản hồi được ký ECDSA P-256 (mục "Ký phản hồi"); app có khóa công khai thì từ chối phản hồi không hợp lệ |
| Dò mật khẩu | Giới hạn 10 lần/phút/IP cho đăng ký-đăng nhập-đổi mật khẩu, khóa tài khoản 15 phút sau 5 lần sai, bcrypt |
| Giả địa chỉ IP để né giới hạn tốc độ | API chỉ tin `X-Forwarded-For` từ mạng nội bộ của Caddy và không mở cổng ra ngoài |
| Lộ khóa JWT hoặc mật khẩu CSDL | Chỉ ở `.env` (không vào Git), API dùng role CSDL giới hạn (`huegis_app`, không sửa được nhật ký kiểm toán) |
| Mất dữ liệu | Sao lưu hằng đêm + bản ngoài VPS + diễn tập khôi phục |
| Dịch vụ ngừng | `/healthz` được giám sát; `deploy.sh rollback`; app có chế độ ngoại tuyến đọc dữ liệu đi kèm |
