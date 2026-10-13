# Hướng dẫn cài đặt MÁY CHỦ (VPS Ubuntu)

Tài liệu này dành cho người **chưa quen quản trị máy chủ**. Làm lần lượt từng bước; mỗi bước có cách kiểm tra kết quả.
Phần phân chia máy chủ / máy khách xem [../tai-lieu/phan-chia-may-chu-may-khach.md](../tai-lieu/phan-chia-may-chu-may-khach.md).

Máy chủ chạy 3 dịch vụ trong Docker: **Caddy** (cổng 80), **API .NET 8**, **PostgreSQL 17 + PostGIS + pgRouting**.
Chỉ cổng 80 (và 22 cho SSH) mở ra ngoài.

**Mọi thứ máy chủ cần đều nằm trong thư mục `may-chu/` của kho mã** (mã API, cơ sở dữ liệu, dữ liệu shapefile, Docker Compose,
script vận hành, tài liệu). Trên VPS chỉ cần `git clone` rồi làm theo hướng dẫn này; **không cần** thư mục `may-khach/`.

> Hiện dùng **HTTP, không HTTPS** (chưa có tên miền). Mật khẩu đi qua mạng dạng chữ thường; đọc mục 11 ("Bảo mật") và đừng
> dùng lại mật khẩu ở nơi khác. Máy chủ có thể ký mọi phản hồi để máy khách phát hiện dữ liệu bị sửa (mục 7).

---

## 1. Chuẩn bị

| Thứ | Yêu cầu |
|---|---|
| VPS | Ubuntu **22.04 hoặc 24.04**, tối thiểu **2 CPU, 2 GB RAM, 20 GB ổ đĩa** (khuyên 4 GB RAM); có địa chỉ IP công khai và ra được Internet (để tải Docker và `git clone`) |
| Máy của bạn | Windows 10/11 có `ssh` (có sẵn trong PowerShell) |
| Nhà cung cấp VPS | Vào trang quản lý, **mở cổng 80 (HTTP) và 22 (SSH)** ở tường lửa của nhà cung cấp nếu họ có tường lửa riêng |

## 2. Kết nối vào VPS

Nhà cung cấp gửi cho bạn IP (ví dụ `203.0.113.10`) và mật khẩu hoặc khóa SSH của người dùng `root`. Trong PowerShell:
```powershell
ssh root@203.0.113.10
```
Lần đầu hỏi "Are you sure you want to continue connecting" → gõ `yes`. Thấy dấu nhắc `root@...:~#` là vào được. Gõ `exit` để thoát.

## 3. Lấy mã từ GitHub và cài môi trường (một lần)

Trên VPS (đang là `root`):
```bash
apt-get update && apt-get install -y git
git clone https://github.com/cuong-bo/HQT_CSDL.git /opt/hue-travel
bash /opt/hue-travel/may-chu/scripts/setup-vps.sh
chown -R deploy:deploy /opt/hue-travel
```
`setup-vps.sh` cài Docker, bật tường lửa (chỉ cổng 22 và 80), fail2ban, tự cập nhật bản vá bảo mật và tạo người dùng `deploy`.
Mất vài phút. **Đọc script trước khi chạy nếu bạn muốn biết nó làm gì.**

> Nếu VPS không vào được GitHub, dùng gói `.tar.gz`: trên máy Windows có kho mã chạy
> `powershell -ExecutionPolicy Bypass -File may-chu\dong-goi.ps1 -Version 1.0.0`, `scp` tệp trong `dist\` lên VPS, rồi
> `mkdir -p /opt/hue-travel && tar -xzf HueTravel-MayChu-1.0.0.tar.gz -C /opt/hue-travel --strip-components=1`.
> Các bước sau giống hệt.

### Khóa SSH (khuyên dùng, làm sau khi mọi thứ chạy)
Dán khóa công khai SSH của bạn vào `/home/deploy/.ssh/authorized_keys`, thử đăng nhập `ssh deploy@IP` ở một cửa sổ mới, rồi chạy
lại `sudo bash /opt/hue-travel/may-chu/scripts/setup-vps.sh --harden-ssh` để tắt đăng nhập bằng mật khẩu.
**Đừng đóng cửa sổ cũ trước khi thử xong.**

**Kiểm tra:** `docker --version` in ra phiên bản; `sudo ufw status` cho thấy chỉ `OpenSSH` và `80/tcp` được phép.

---

## 4. Cấu hình bí mật (`may-chu/.env`)

Đăng nhập bằng người dùng `deploy`:
```bash
ssh deploy@203.0.113.10
cd /opt/hue-travel/may-chu
cp .env.example .env
nano .env
```
Điền **3 giá trị bí mật, mỗi dòng một giá trị khác nhau**. Để tạo giá trị ngẫu nhiên, chạy `openssl rand -base64 32` ba lần:

| Dòng | Ý nghĩa |
|---|---|
| `POSTGRES_PASSWORD` | Mật khẩu quản trị cơ sở dữ liệu |
| `APP_DB_PASSWORD` | Mật khẩu tài khoản cơ sở dữ liệu mà API dùng |
| `JWT_KEY` | Khóa ký phiên đăng nhập, **tối thiểu 32 ký tự** |

Lưu trong `nano`: `Ctrl+O`, Enter, rồi `Ctrl+X`. Các dòng còn lại để mặc định (xem chú thích trong tệp). **Không đưa `.env` lên Git, không gửi cho ai.**

Các dòng `TILE_URL_*`: bản đồ nền hiện vẽ ngay trong ứng dụng từ dữ liệu của dự án nên **để trống**.

## 5. Chạy lần đầu

Mọi lệnh từ đây chạy trong thư mục `may-chu`:
```bash
cd /opt/hue-travel/may-chu
scripts/deploy.sh up        # dựng và khởi động 3 dịch vụ (lần đầu mất 5–10 phút để tải và dựng)
scripts/deploy.sh import    # nạp dữ liệu, dựng đồ thị đường, kiểm tra
scripts/deploy.sh status    # phải in {"status":"ok"}
```
Nếu `deploy.sh` báo "Permission denied", chạy `bash scripts/deploy.sh up` (và tương tự cho các lệnh khác).

**`import` làm gì (mất khoảng 15–60 giây):** áp dụng các tệp cấu trúc trong `co-so-du-lieu/migrations`, nạp các shapefile trong
`du-lieu/shp`, dựng bảng địa điểm, dựng đồ thị đường từ `MangGiaoThong` và `NutMangGiaoThong`, gắn mỗi địa điểm vào đường
gần nhất, rồi kiểm tra. Kết thúc phải thấy bảng số địa điểm theo nhóm và dòng `route Ga Hue -> Dai Noi: ... s`.
Cảnh báo `WARNING: ... POI are more than 150 m from any road` là lỗi dữ liệu đã biết, không làm dừng cài đặt.

## 6. Kiểm tra từ máy khác

Trên máy Windows:
```powershell
curl http://203.0.113.10/healthz
curl "http://203.0.113.10/v1/pois?group=stay"
```
Cần thấy `{"status":"ok"}` và danh sách địa điểm lưu trú. Nếu không kết nối được xem mục 12.

## 7. Ký phản hồi (khuyên dùng khi chạy HTTP)

```bash
cd /opt/hue-travel/may-chu
scripts/gen-signing-key.sh
```
Script in ra **khóa riêng tư** và **khóa công khai**.
1. Dán khóa riêng tư vào dòng `SIGNING_KEY=` trong `.env`, rồi `scripts/deploy.sh up`.
2. Khóa công khai đưa cho máy khách (xem [../may-khach/HUONG-DAN-CAI-DAT.md](../may-khach/HUONG-DAN-CAI-DAT.md)). Máy khách có khóa công khai sẽ từ chối phản hồi giả.
3. Đổi khóa thì phải phát hành lại khóa công khai cho mọi máy khách.

## 8. Tài khoản quản trị đầu tiên

Mở ứng dụng, **đăng ký** một tài khoản, rồi cấp quyền quản trị bằng một lệnh SQL trên máy chủ:
```bash
cd /opt/hue-travel/may-chu
docker compose exec db psql -U huegis -d huegis \
  -c "update app_user set role = 'admin' where username = 'ten_dang_nhap'"
```
Từ đó quản trị viên tạo tài khoản chủ quản, duyệt yêu cầu nhận địa điểm và duyệt chỉnh sửa. **Nên có ít nhất 2 quản trị viên**
(sai mật khẩu 5 lần khóa tài khoản 15 phút). Mở khóa ngay:
`update app_user set locked_until = null, failed_logins = 0 where username = '...'`

## 9. Sao lưu

Sao lưu hằng đêm:
```bash
crontab -e
# thêm dòng sau rồi lưu
30 2 * * *  /opt/hue-travel/may-chu/scripts/backup.sh >> /var/log/hue-backup.log 2>&1
```
Giữ 7 bản theo ngày và 4 bản theo tuần ở `/var/backups/hue-travel`. Để chép thêm một bản ra máy khác (**bắt buộc** nếu muốn
sống sót khi VPS hỏng), đặt biến `BACKUP_REMOTE=user@may-khac:/duong/dan` trước lệnh.

**Diễn tập khôi phục ít nhất một lần** (trên máy thử, không đụng dữ liệu thật):
```bash
scripts/restore.sh /var/backups/hue-travel/daily/huegis-YYYY-MM-DD.dump
```

## 10. Cập nhật

Khi có bản mới trên GitHub:
```bash
cd /opt/hue-travel/may-chu
scripts/deploy.sh update     # git pull, dựng lại API; tự quay về bản cũ nếu /healthz không trả lời
scripts/deploy.sh import     # chỉ cần khi dữ liệu hoặc cấu trúc cơ sở dữ liệu đổi
scripts/deploy.sh rollback   # (khi cần) quay về bản API trước
```
`import` an toàn khi chạy lại: chỉnh sửa đã làm qua ứng dụng **không bị ghi đè**; địa điểm biến mất khỏi dữ liệu chỉ bị đánh dấu xóa mềm.
Tệp `.env` không nằm trong Git nên `git pull` không đụng tới.

## 11. Bảo mật khi dùng HTTP

| Rủi ro | Cách giảm |
|---|---|
| Mật khẩu và token đi dạng chữ thường | Dùng mật khẩu riêng cho ứng dụng này; token sống 15 phút. Nên chuyển HTTPS khi có tên miền |
| Kẻ ở giữa sửa dữ liệu (giả tuyến, giả vị trí) | Bật ký phản hồi (mục 7) |
| Dò mật khẩu | Giới hạn 10 lần/phút/IP, khóa tài khoản sau 5 lần sai, mật khẩu băm bcrypt |
| Lộ khóa hoặc mật khẩu cơ sở dữ liệu | Chỉ nằm trong `may-chu/.env`; API dùng tài khoản cơ sở dữ liệu giới hạn |
| Mất dữ liệu | Sao lưu hằng đêm và bản ngoài VPS |

**Không bao giờ thêm dòng `ports:` cho `db` hoặc `api`** trong `docker-compose.yml`: Docker mở cổng vượt qua tường lửa.
Quét từ ngoài (`nmap IP_VPS`) chỉ nên thấy cổng 22 và 80.

## 12. Lệnh hữu ích và sự cố thường gặp

Chạy trong `/opt/hue-travel/may-chu`:

| Việc | Lệnh |
|---|---|
| Xem trạng thái | `scripts/deploy.sh status` |
| Xem nhật ký | `scripts/deploy.sh logs` (thoát: `Ctrl+C`) |
| Khởi động lại API | `docker compose restart api` |
| Dung lượng ổ đĩa | `df -h` (ổ đầy là lỗi hay gặp nhất) |
| Vào cơ sở dữ liệu | `docker compose exec db psql -U huegis -d huegis` |

| Triệu chứng | Nguyên nhân thường gặp và cách xử lý |
|---|---|
| `curl http://IP/healthz` không trả lời | Cổng 80 chưa mở ở tường lửa của nhà cung cấp VPS; hoặc `status` cho thấy container chưa chạy (xem `logs`) |
| `deploy.sh up` báo thiếu `POSTGRES_PASSWORD`... | Chưa điền `may-chu/.env` (mục 4) |
| API khởi động rồi dừng, log nhắc `Jwt:Key` | `JWT_KEY` ngắn hơn 32 ký tự |
| `import` báo `missing .../xxx.shp` | Thiếu lớp dữ liệu trong `may-chu/du-lieu/shp`; chạy `git status` / `git pull` để lấy lại |
| `import` báo `staged places are missing` hoặc lỗi ID trùng | Dữ liệu có mã địa điểm trùng giữa các lớp; sửa dữ liệu rồi nạp lại |
| Tìm đường báo "Điểm xuất phát quá xa đường đi" | Điểm bắt đầu cách đường quá 1,5 km; chọn điểm gần đường hơn |
| Tìm đường báo "Không tìm thấy địa điểm" | Địa điểm chưa có trong cơ sở dữ liệu máy chủ hoặc bị ẩn; chạy lại `import` |
| Ứng dụng báo phản hồi không hợp lệ | Khóa công khai trong ứng dụng không khớp khóa riêng tư trên máy chủ (mục 7) |
| Hết dung lượng | Dọn bản sao lưu cũ ở `/var/backups/hue-travel`, `docker system prune` (cẩn thận) |

Vận hành chi tiết hơn: [VAN-HANH.md](VAN-HANH.md). Cơ sở dữ liệu: [co-so-du-lieu/README.md](co-so-du-lieu/README.md).

## 13. Gỡ cài đặt

```bash
cd /opt/hue-travel/may-chu
docker compose down       # dừng, giữ dữ liệu
docker compose down -v    # dừng VÀ XÓA cơ sở dữ liệu (không hoàn tác được)
```
