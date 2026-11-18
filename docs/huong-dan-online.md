# Hướng dẫn: bật đăng nhập Google và đưa MockViewAI lên mạng

> Mọi bước có mật khẩu hoặc mã bí mật (Client Secret) **học viên tự làm**. Không gửi các mã này vào chat AI, không dán vào code, không đưa lên GitHub.

Thông tin dự án:
- Supabase project: `mockviewai` (ref `xqbonoetvwfzucczuuka`, Singapore)
- Địa chỉ quay về của Google (callback): `https://xqbonoetvwfzucczuuka.supabase.co/auth/v1/callback`
- Chạy thử trên máy: Live Server → `http://127.0.0.1:5500`

## Phần A — Bật đăng nhập Google

### A1. Màn hình đồng ý trên Google Cloud
1. Vào https://console.cloud.google.com → tạo project mới (ví dụ `MockViewAI`).
2. Mở **Google Auth Platform → Branding**: tên ứng dụng `MockViewAI`, email hỗ trợ là email của bạn.
3. **Audience**: chọn **External**. Ở **Test users** thêm email của bạn và các thành viên nhóm.
   - Khi ứng dụng ở chế độ *Testing*, chỉ các email trong danh sách này đăng nhập Google được. Muốn ai cũng dùng được thì bấm *Publish app* (chỉ xin quyền email cơ bản nên thường không cần Google duyệt).

### A2. Tạo OAuth Client
1. **Google Auth Platform → Clients → Create client** → loại **Web application**.
2. **Authorized JavaScript origins**: `http://127.0.0.1:5500` và `http://localhost:5500`.
3. **Authorized redirect URIs**: `https://xqbonoetvwfzucczuuka.supabase.co/auth/v1/callback`
4. Bấm **Create**, giữ lại **Client ID** và **Client Secret**.

### A3. Bật Google trong Supabase
1. Supabase → project `mockviewai` → **Authentication → Sign In / Providers → Google**.
2. Bật **Enable**, dán Client ID và Client Secret → **Save**.

### A4. Khai báo địa chỉ trang
1. **Authentication → URL Configuration**.
2. **Site URL**: tạm đặt `http://127.0.0.1:5500` (đổi thành link Vercel ở Phần B).
3. **Redirect URLs**: thêm `http://127.0.0.1:5500/**` và `http://localhost:5500/**`.
4. Giữ **Confirm email** đang bật (Authentication → Sign In / Providers → Email).

### A5. Thử
Mở trang bằng Live Server → **Đăng nhập → Tiếp tục với Google** → chọn email nằm trong Test users → quay về trang, checklist mở khóa, tích thử 1 mục, F5 vẫn còn.

| Lỗi thường gặp | Cách sửa |
|---|---|
| Trang báo "Đăng nhập Google chưa được bật trên máy chủ" | Chưa làm A3 hoặc chưa bấm Save |
| Google báo `redirect_uri_mismatch` | Sai dòng callback ở A2 bước 3 (phải đúng từng ký tự) |
| Google báo không có quyền truy cập (access blocked) | Email chưa nằm trong Test users (A1 bước 3) |
| Đăng nhập xong quay về trang Supabase hoặc trang lạ | Thiếu địa chỉ trang trong Redirect URLs (A4) |
| Trang báo cần mở qua địa chỉ web | Đang mở bằng nhấp đúp file; dùng Live Server |

## Phần B — Đưa lên GitHub và Vercel

### B1. GitHub (tài khoản LoveKurumi1000)
1. Tạo repository mới, ví dụ `mockviewai` (Public hoặc Private đều được với Vercel).
2. Đưa toàn bộ thư mục này lên (GitHub Desktop: *Add existing repository* → *Publish*).
3. `.gitignore` đã chặn `.env` và tài liệu khóa học. Trước khi đẩy, kiểm tra lại: trong code chỉ có **publishable key** (`sb_publishable_...`), không có `service_role` hay mã bí mật nào.

### B2. Vercel
1. Vào https://vercel.com → đăng nhập bằng GitHub → **Add New → Project** → chọn repo `mockviewai`.
2. Framework Preset: **Other**. Không cần Build Command, Output Directory để trống (trang tĩnh).
3. **Deploy** → nhận link, ví dụ `https://mockviewai.vercel.app`.

### B3. Cập nhật địa chỉ sau khi có link Vercel
- Supabase → URL Configuration: **Site URL** = link Vercel; **Redirect URLs** thêm `https://<link-vercel>/**`.
- Google Cloud → OAuth Client: **Authorized JavaScript origins** thêm `https://<link-vercel>`.
- Ghi link Vercel vào `docs/progress.md`.

Sau đó chạy toàn bộ bài test trong `kiem-thu-va-demo.md` **trên link Vercel**.
