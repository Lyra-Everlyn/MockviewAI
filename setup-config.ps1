# =====================================================================
#  MockviewAI - Cài cấu hình cho máy dev (thay file .bat cũ)
#  Chạy bằng: bấm đúp setup-config.bat (nằm cùng thư mục với file này)
#
#  Script này KHÔNG chứa mật khẩu/key nào. Bạn nhập khi chạy.
#  - Bấm Enter (để trống) = GIỮ NGUYÊN giá trị đang có, không ghi đè rỗng.
#  - Lưu vào user-secrets (cho Visual Studio) và tạo .env ở thư mục gốc (cho Docker).
#  - Cuối cùng kiểm tra: đủ key chưa, MySQL/Redis đã chạy chưa, có migration chưa.
#  Viết cho Windows PowerShell 5.1 (có sẵn trên Windows 10/11).
# =====================================================================

$ErrorActionPreference = 'Continue'   # lỗi của lệnh ngoài (dotnet, docker) được xử lý bằng $LASTEXITCODE
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# ---------- 0. Tìm đúng thư mục (không dùng đường dẫn cứng) ----------
$root = $PSScriptRoot                                   # thư mục chứa script = thư mục gốc repo
$projectDir = Join-Path $root 'MockviewAI'
$csproj = Join-Path $projectDir 'MockviewAI.csproj'

function Say($text, $color = 'Gray') { Write-Host $text -ForegroundColor $color }
function Title($text) { Write-Host ''; Write-Host "=== $text ===" -ForegroundColor Cyan }

if (-not (Test-Path (Join-Path $root 'docker-compose.yml')) -or -not (Test-Path $csproj)) {
    Say "Không tìm thấy docker-compose.yml hoặc MockviewAI\MockviewAI.csproj cạnh script." Red
    Say "Hãy đặt setup-config.bat + setup-config.ps1 ở THƯ MỤC GỐC của repo (chỗ có docker-compose.yml)." Red
    exit 1
}
Say "Thư mục gốc : $root"
Say "Project     : $csproj"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Say "Máy chưa có lệnh 'dotnet'. Cài .NET SDK rồi chạy lại." Red
    exit 1
}

# ---------- 1. Đọc user-secrets đang có ----------
function Get-Secrets {
    $map = @{}
    $lines = & dotnet user-secrets list --project $csproj 2>$null
    foreach ($line in $lines) {
        $i = $line.IndexOf(' = ')
        if ($i -gt 0) { $map[$line.Substring(0, $i).Trim()] = $line.Substring($i + 3) }
    }
    return $map
}

function Set-Secret($key, $value) {
    & dotnet user-secrets set $key $value --project $csproj | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Không đặt được $key" }
}

Title 'Đọc cấu hình hiện có'
$secrets = Get-Secrets
Say ("Đang có {0} mục trong user-secrets." -f $secrets.Count)

# Hỏi một giá trị. Enter = giữ nguyên. $isSecret = ẩn khi gõ.
function Ask($key, $label, $isSecret = $false, $default = '') {
    $has = $secrets.ContainsKey($key) -and $secrets[$key] -ne ''
    $hint = if ($has) { '[đã có - Enter để giữ]' } elseif ($default -ne '') { "[Enter = $default]" } else { '[chưa có - Enter để bỏ qua]' }

    if ($isSecret) {
        $secure = Read-Host "$label $hint" -AsSecureString
        $value = (New-Object System.Net.NetworkCredential('', $secure)).Password
    } else {
        $value = Read-Host "$label $hint"
    }
    $value = $value.Trim()

    if ($value -eq '' -and -not $has -and $default -ne '') { $value = $default }
    if ($value -ne '') {
        Set-Secret $key $value
        $secrets[$key] = $value
        Say "   -> đã lưu $key" DarkGreen
    }
}

# ---------- 2. Nhập từng nhóm cấu hình ----------
Title 'MySQL'
$hasDb = $secrets.ContainsKey('ConnectionStrings:DefaultConnection')
$pw = Read-Host ("Mật khẩu root MySQL (giống MYSQL_ROOT_PASSWORD) {0}" -f $(if ($hasDb) { '[đã có - Enter để giữ]' } else { '[bắt buộc]' })) -AsSecureString
$pw = (New-Object System.Net.NetworkCredential('', $pw)).Password.Trim()
if ($pw -ne '') {
    # Luôn dùng đúng database mockview_database (file .bat cũ ghi nhầm "dbname")
    $conn = "Server=localhost;Port=3306;Database=mockview_database;Uid=root;Pwd=$pw;"
    Set-Secret 'ConnectionStrings:DefaultConnection' $conn
    $secrets['ConnectionStrings:DefaultConnection'] = $conn
    Say '   -> đã lưu ConnectionStrings:DefaultConnection (database: mockview_database)' DarkGreen
} elseif ($hasDb -and $secrets['ConnectionStrings:DefaultConnection'] -notmatch 'Database=mockview_database;') {
    # Sửa lỗi của file .bat cũ (Database=dbname): giữ mật khẩu, chỉ đổi tên database
    $fixed = $secrets['ConnectionStrings:DefaultConnection'] -replace 'Database=[^;]*', 'Database=mockview_database'
    Set-Secret 'ConnectionStrings:DefaultConnection' $fixed
    $secrets['ConnectionStrings:DefaultConnection'] = $fixed
    Say '   -> đã sửa tên database trong chuỗi kết nối thành mockview_database' DarkGreen
}

Title 'Redis'
Set-Secret 'ConnectionStrings:RedisConnection' 'localhost:6379'
$secrets['ConnectionStrings:RedisConnection'] = 'localhost:6379'
Say '   -> ConnectionStrings:RedisConnection = localhost:6379' DarkGreen

Title 'Đăng nhập Google'
Ask 'Authentication:Google:ClientId' 'Google ClientId'
Ask 'Authentication:Google:ClientSecret' 'Google ClientSecret' $true

Title 'Gửi email (Gmail SMTP)'
Ask 'SmtpSettings:SenderName' 'Tên người gửi' $false 'MockviewAI Support'
Ask 'SmtpSettings:SenderEmail' 'Email gửi' $false 'mockviewai@gmail.com'
Ask 'SmtpSettings:Username' 'Tài khoản SMTP' $false 'mockviewai@gmail.com'
Ask 'SmtpSettings:Password' 'App Password Gmail (16 ký tự)' $true

Title 'Cloudinary (lưu ảnh)'
Ask 'CloudinarySettings:CloudName' 'CloudName'
Ask 'CloudinarySettings:ApiKey' 'ApiKey'
Ask 'CloudinarySettings:ApiSecret' 'ApiSecret' $true

Title 'Cloudflare Turnstile (chống bot ở trang đăng nhập)'
Ask 'Turnstile:SiteKey' 'SiteKey'
Ask 'Turnstile:SecretKey' 'SecretKey' $true

Title 'AI chấm điểm'
Say 'Mock = điểm giả, miễn phí, không gửi dữ liệu ra ngoài (khuyên dùng khi cho người test).'
Say 'Gemini = chấm thật, gửi câu trả lời tới Google, cần API key.'
$current = if ($secrets.ContainsKey('Ai:Provider')) { $secrets['Ai:Provider'] } else { 'Mock' }
$provider = (Read-Host "Chọn Mock hoặc Gemini [Enter = $current]").Trim()
if ($provider -eq '') { $provider = $current }
if ($provider -notin @('Mock', 'Gemini')) { Say "   ! '$provider' không hợp lệ, dùng Mock." Yellow; $provider = 'Mock' }
Set-Secret 'Ai:Provider' $provider
$secrets['Ai:Provider'] = $provider
Say "   -> Ai:Provider = $provider" DarkGreen
if ($provider -eq 'Gemini') {
    Ask 'Gemini:ApiKey' 'Gemini API key' $true
    Ask 'Gemini:Model' 'Gemini model' $false 'gemini-2.5-flash'
    $key = $secrets['Gemini:ApiKey']
    if ($key -and -not $key.StartsWith('AIza')) {
        Say '   ! Key Gemini API thường bắt đầu bằng "AIza". Key này có thể không dùng được - hãy thử chấm 1 buổi.' Yellow
    }
}

# ---------- 3. Tạo .env ở THƯ MỤC GỐC (docker-compose đọc ở đây) ----------
Title 'Tạo .env cho Docker'
function Sec($k) { if ($secrets.ContainsKey($k)) { $secrets[$k] } else { '' } }
$dbConn = Sec 'ConnectionStrings:DefaultConnection'
$mysqlPw = ''
if ($dbConn -match 'Pwd=([^;]*)') { $mysqlPw = $Matches[1] }

$envLines = @(
    '# Tạo bởi setup-config.ps1. KHÔNG commit file này (đã có trong .gitignore).',
    "MYSQL_ROOT_PASSWORD=$mysqlPw",
    '# Trong container phải dùng TÊN SERVICE (db_mockview, redis_mockview), không dùng localhost',
    "ConnectionStrings__DefaultConnection=Server=db_mockview;Port=3306;Database=mockview_database;Uid=root;Pwd=$mysqlPw;",
    'ConnectionStrings__RedisConnection=redis_mockview:6379',
    "Authentication__Google__ClientId=$(Sec 'Authentication:Google:ClientId')",
    "Authentication__Google__ClientSecret=$(Sec 'Authentication:Google:ClientSecret')",
    "SmtpSettings__SenderName=$(Sec 'SmtpSettings:SenderName')",
    "SmtpSettings__SenderEmail=$(Sec 'SmtpSettings:SenderEmail')",
    "SmtpSettings__Username=$(Sec 'SmtpSettings:Username')",
    "SmtpSettings__Password=$(Sec 'SmtpSettings:Password')",
    "CloudinarySettings__CloudName=$(Sec 'CloudinarySettings:CloudName')",
    "CloudinarySettings__ApiKey=$(Sec 'CloudinarySettings:ApiKey')",
    "CloudinarySettings__ApiSecret=$(Sec 'CloudinarySettings:ApiSecret')",
    "Turnstile__SiteKey=$(Sec 'Turnstile:SiteKey')",
    "Turnstile__SecretKey=$(Sec 'Turnstile:SecretKey')",
    "Ai__Provider=$provider",
    "Gemini__ApiKey=$(Sec 'Gemini:ApiKey')",
    "Gemini__Model=$(Sec 'Gemini:Model')"
)
$envPath = Join-Path $root '.env'
if (Test-Path $envPath) {
    # Sao lưu RA NGOÀI repo (thư mục Temp) để không lỡ commit file chứa mật khẩu
    $backup = Join-Path $env:TEMP ("mockviewai-env-backup-{0}.txt" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
    Copy-Item $envPath $backup -Force
    Say "   (đã sao lưu .env cũ vào $backup)"
}
# Ghi UTF-8 KHÔNG có BOM, nếu có BOM docker compose đọc sai dòng đầu
[System.IO.File]::WriteAllLines($envPath, $envLines, (New-Object System.Text.UTF8Encoding($false)))
Say "   -> đã ghi $envPath" DarkGreen

# ---------- 4. Kiểm tra lại: thiếu gì thì báo rõ ----------
Title 'Kiểm tra cấu hình'
$secrets = Get-Secrets
$checks = @(
    @('ConnectionStrings:DefaultConnection', 'App không kết nối được MySQL'),
    @('ConnectionStrings:RedisConnection', 'App không khởi động được'),
    @('CloudinarySettings:CloudName', 'App không khởi động được'),
    @('CloudinarySettings:ApiKey', 'App không khởi động được'),
    @('CloudinarySettings:ApiSecret', 'App không khởi động được'),
    @('Turnstile:SiteKey', 'Ô xác minh ở trang đăng nhập không hiện'),
    @('Turnstile:SecretKey', 'Đăng nhập / đăng ký luôn báo "Security verification failed"'),
    @('Authentication:Google:ClientId', 'Không đăng nhập bằng Google được'),
    @('Authentication:Google:ClientSecret', 'Không đăng nhập bằng Google được'),
    @('SmtpSettings:Password', 'Không gửi được mã xác nhận email khi đăng ký')
)
if ($provider -eq 'Gemini') { $checks += , @('Gemini:ApiKey', 'Không chấm điểm được buổi nào') }

$missing = 0
foreach ($c in $checks) {
    $ok = $secrets.ContainsKey($c[0]) -and $secrets[$c[0]] -ne ''
    if ($ok) { Say ("   [OK]    {0}" -f $c[0]) Green }
    else { Say ("   [THIẾU] {0}  -> {1}" -f $c[0], $c[1]) Red; $missing++ }
}

# ---------- 5. Docker: MySQL + Redis đã chạy chưa ----------
Title 'Docker'
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Say '   Chưa có lệnh docker. Mở Docker Desktop rồi chạy lại.' Yellow
} else {
    $all = & docker ps -a --format '{{.Names}}|{{.Status}}' 2>$null
    if ($LASTEXITCODE -ne 0) {
        Say '   Docker Desktop chưa mở. Mở Docker Desktop rồi chạy lại script.' Yellow
    } else {
        foreach ($name in @('mysql_mockview_container', 'redis_mockview_container')) {
            $row = $all | Where-Object { $_ -like "$name|*" } | Select-Object -First 1
            if (-not $row) {
                if ($name -like 'redis*') {
                    Say "   $name chưa có -> tạo mới" Yellow
                    & docker run -d --name $name -p 127.0.0.1:6379:6379 --restart always redis:alpine | Out-Null
                } else {
                    Say "   $name chưa có -> chạy 'docker compose up -d db_mockview' ở thư mục gốc" Yellow
                    Push-Location $root; & docker compose up -d db_mockview; Pop-Location
                }
            } elseif ($row -notlike '*|Up*') {
                Say "   $name đang tắt -> bật lên" Yellow
                & docker start $name | Out-Null
            } else {
                Say "   [OK] $name đang chạy" Green
            }
        }
    }
}

# ---------- 6. Migration (main hiện gọi Migrate() nhưng repo chưa có thư mục Migrations) ----------
Title 'Database migration'
if (Test-Path (Join-Path $projectDir 'Migrations')) {
    Say '   [OK] Đã có thư mục Migrations. F5 sẽ tự cập nhật bảng.' Green
} else {
    Say '   Chưa có thư mục Migrations -> F5 sẽ báo lỗi "pending model changes".' Yellow
    Say '   Cách sửa: xoá database thử cũ rồi tạo migration InitialCreate (CHỈ dùng trên máy dev).' Yellow
    $answer = Read-Host '   Gõ XOA để làm ngay (mất dữ liệu thử trong mockview_database), Enter để bỏ qua'
    if ($answer -eq 'XOA') {
        & dotnet ef --version *> $null
        if ($LASTEXITCODE -ne 0) {
            Say '   Chưa có công cụ dotnet-ef. Cài bằng: dotnet tool install --global dotnet-ef  rồi chạy lại.' Red
        } else {
            Push-Location $projectDir
            & dotnet ef database drop -f
            & dotnet ef migrations add InitialCreate
            Pop-Location
            if ($LASTEXITCODE -eq 0) { Say '   [OK] Đã tạo migration. Bấm F5, app sẽ tự tạo bảng.' Green }
            else { Say '   Tạo migration lỗi. Gửi dòng lỗi ở trên cho Claude.' Red }
        }
    }
}

# ---------- Kết thúc: nói thật, không báo SUCCESS khi còn thiếu ----------
Write-Host ''
if ($missing -eq 0) {
    Say 'XONG: đủ cấu hình. Đóng tab Visual Studio (Don''t Save) rồi bấm F5.' Green
} else {
    Say "CHƯA XONG: còn thiếu $missing mục (xem [THIẾU] ở trên). Chạy lại script và nhập các mục đó." Yellow
}
