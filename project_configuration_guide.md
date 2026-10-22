# Hướng Dẫn Cấu Hình Dự Án MockviewAI

Tài liệu này cung cấp hướng dẫn chi tiết, rõ ràng về cách thiết lập các thông số cấu hình và biến môi trường cho dự án **MockviewAI**. 

## PHẦN 1: Dành cho Local Development (Chạy trực tiếp qua Visual Studio / .NET CLI)

Nếu bạn nhấn nút **F5** trên Visual Studio để code và debug, hãy làm theo các bước sau **đúng một lần duy nhất**:

### Bước 1: Mở Terminal tại thư mục dự án
Mở cửa sổ dòng lệnh (Terminal / Command Prompt / PowerShell) và di chuyển vào thư mục chứa file `.csproj`:
```bash
cd MockviewAI/MockviewAI
```

### Bước 2: Chạy các lệnh thiết lập User Secrets
Sao chép và chạy lần lượt các lệnh sau (nhớ thay thế các chuỗi `YOUR_...` bằng thông tin thực tế của bạn):

```bash
# 1. Cơ sở dữ liệu và Redis (Local)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=mockview_database;Uid=root;Pwd=change-me;"
dotnet user-secrets set "ConnectionStrings:RedisConnection" "localhost:6379"

# 2. Google Authentication
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_GOOGLE_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_GOOGLE_CLIENT_SECRET"

# 3. Cấu hình gửi Email (SMTP)
dotnet user-secrets set "SmtpSettings:Server" "smtp.gmail.com"
dotnet user-secrets set "SmtpSettings:Port" "587"
dotnet user-secrets set "SmtpSettings:SenderName" "MockviewAI"
dotnet user-secrets set "SmtpSettings:SenderEmail" "your_email@gmail.com"
dotnet user-secrets set "SmtpSettings:Username" "your_email@gmail.com"
dotnet user-secrets set "SmtpSettings:Password" "your_gmail_app_password"

# 4. Cloudinary (Lưu trữ ảnh)
dotnet user-secrets set "CloudinarySettings:CloudName" "YOUR_CLOUD_NAME"
dotnet user-secrets set "CloudinarySettings:ApiKey" "YOUR_API_KEY"
dotnet user-secrets set "CloudinarySettings:ApiSecret" "YOUR_API_SECRET"

# 5. AI Scoring (Gemini)
dotnet user-secrets set "Ai:Provider" "Gemini"
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_API_KEY"
dotnet user-secrets set "Gemini:Model" "gemini-3.8-flash"

# 6. Cloudflare Turnstile (Captcha)
dotnet user-secrets set "Turnstile:SiteKey" "YOUR_TURNSTILE_SITE_KEY"
dotnet user-secrets set "Turnstile:SecretKey" "YOUR_TURNSTILE_SECRET_KEY"
```

Sau khi chạy xong, bạn có thể thoải mái bấm F5 trên Visual Studio mà không cần bận tâm về cấu hình nữa.

---

## PHẦN 2: Dành cho Docker Compose (Chạy toàn bộ qua Docker)

Nếu bạn chọn chạy dự án bằng lệnh `docker-compose up`, hệ thống sẽ tự động đọc cấu hình từ file `.env`.

### Quy ước cú pháp quan trọng:
Trong file cấu hình C# (`appsettings.json`), các cấp độ được phân tách bằng dấu hai chấm (`:`). Tuy nhiên, khi chuyển sang biến môi trường của Docker, dấu `:` bắt buộc phải được thay thế bằng **dấu gạch dưới kép (`__`)**. 
* *Ví dụ:* `Ai:Provider` trong C# sẽ trở thành `Ai__Provider` trong Docker.

### Bước 1: Tạo file `.env`
Tại thư mục gốc của dự án (nơi chứa file `docker-compose.yml`), tạo một file mới đặt tên là `.env` và điền nội dung sau vào:

```env
# ==========================================
# CẤU HÌNH DOCKER COMPOSE & BIẾN MÔI TRƯỜNG
# ==========================================
# Lưu ý: Tuyệt đối không đưa file .env thực tế lên Git.

# Mật khẩu gốc cho container MySQL
MYSQL_ROOT_PASSWORD=change-me

# --- Kết nối hạ tầng trong mạng Docker ---
ConnectionStrings__DefaultConnection=Server=mysql;Port=3306;Database=mockview_database;Uid=root;Pwd=change-me;
ConnectionStrings__RedisConnection=redis:6379

# --- Google Authentication ---
Authentication__Google__ClientId=YOUR_GOOGLE_CLIENT_ID
Authentication__Google__ClientSecret=YOUR_GOOGLE_CLIENT_SECRET

# --- Cấu hình SMTP ---
SmtpSettings__Server=smtp.gmail.com
SmtpSettings__Port=587
SmtpSettings__SenderEmail=your_email@gmail.com
SmtpSettings__Password=your_gmail_app_password

# --- Cloudinary ---
CloudinarySettings__CloudName=YOUR_CLOUD_NAME
CloudinarySettings__ApiKey=YOUR_API_KEY
CloudinarySettings__ApiSecret=YOUR_API_SECRET

# --- AI Scoring (Gemini) ---
Ai__Provider=Gemini
Gemini__ApiKey=YOUR_GEMINI_API_KEY
Gemini__Model=gemini-2.5-flash

# --- Cloudflare Turnstile ---
Turnstile__SiteKey=YOUR_TURNSTILE_SITE_KEY
Turnstile__SecretKey=YOUR_TURNSTILE_SECRET_KEY
```

### Bước 2: Khởi chạy hệ thống
Mở terminal tại thư mục chứa file `docker-compose.yml` và chạy lệnh:
```bash
docker-compose up -d --build
```
Docker sẽ tự động đọc file `.env`, thiết lập toàn bộ biến môi trường và khởi chạy ứng dụng một cách hoàn chỉnh.