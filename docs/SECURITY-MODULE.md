# Module Bảo mật (MockviewAI)

Tìm nhanh trong VS Code: `Ctrl+Shift+F` rồi gõ `[SECURITY-MODULE]` để thấy mọi chỗ liên quan, kể cả các dòng nhỏ nằm trong file của người khác.

## Code bảo mật nằm ở đâu

| File | Việc |
|---|---|
| `Models/Security/SecurityOptions.cs` | Cấu hình: số lần sai tối đa, thời gian khóa |
| `Services/Security/PasswordPolicy.cs` | Quy tắc mật khẩu mạnh (8-72 ký tự, hoa, thường, số) |
| `Services/Security/LoginThrottleService.cs` | Khóa tạm khi đăng nhập sai nhiều lần (theo email + IP) |
| `Services/Security/SecurityExtensions.cs` | `AddAppSecurity`, `HardenCookie`, `UseAppSecurity` (security headers) |

## Chỗ phải nằm trong file của người khác (chỉ vài dòng, có thẻ `[SECURITY-MODULE]`)

| File | Thay đổi |
|---|---|
| `Program.cs` | Gọi `AddAppSecurity`, `HardenCookie`, `UseAppSecurity`; `UseAuthentication()` đặt trước `UseAuthorization()` |
| `Controllers/AuthController.cs` | Gọi throttle khi đăng nhập; gọi `PasswordPolicy.Validate` khi đăng ký |
| `Services/Implementations/AuthService.cs` | Một thông báo lỗi chung cho "sai email" và "sai mật khẩu"; hash giả để thời gian phản hồi như nhau (chống dò email) |
| `Views/Auth/Login.cshtml`, `wwwroot/js/handle-input-error.js` | Chống XSS khi hiện thông báo lỗi |
| `appsettings.json`, `.gitignore`, `.env.example` | Không để secret trong repo; mục `Security` |

## Danh sách biện pháp

1. Mật khẩu băm bằng BCrypt; yêu cầu mật khẩu mạnh.
2. Chống dò email và dò mật khẩu: thông báo chung, hash giả, khóa tạm sau 5 lần sai trong 15 phút.
3. Cookie đăng nhập: HttpOnly, SameSite=Lax, hết hạn 8 giờ.
4. Form có AntiForgeryToken (chống CSRF).
5. Từ chối tài khoản Google chưa xác minh email.
6. Security headers: nosniff, chống nhúng iframe, Referrer-Policy, Permissions-Policy.
7. Secret nằm trong `.env`/biến môi trường, không commit.

## Chưa làm (nên nêu trong phần đánh giá hạn chế)

- Content-Security-Policy (giao diện đang dùng script/style nội tuyến).
- Throttle lưu trong bộ nhớ: khởi động lại thì đếm lại; chạy nhiều bản app thì mỗi bản đếm riêng.
- Giới hạn số lần gọi AI theo người dùng.
- Quên mật khẩu, xác minh email khi đăng ký.
