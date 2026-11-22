# MockviewAI 🤖👔

> Practice interviewing with AI before your real interview day.
> 🌐 **Live:** [mockviewai.com](https://mockviewai.com)

MockviewAI is an AI-powered mock interview platform for IT students, upcoming graduates and entry-level candidates. It gives a safe, realistic environment to practice interviews by voice or text, get instant feedback, and track interview anxiety over time.

---

## ✨ Key Features

| Area | What it does |
|---|---|
| **Role-specific interviews** | Choose a target role (Developer, Tester, BA), an interviewer persona (HR, Tech Lead, Project Manager) and 5–10 questions. |
| **Two interview modes** | **Practice** – retry answers, type instead of speak, replay questions. **Live** – think time, answer time limit, no retries; skipped questions count as wrong. |
| **Voice or text answers** | Browser speech-to-text, live volume meter, mic check before the first question, voice delivery metrics (volume, pauses, tone variation). |
| **Follow-up questions** | When an answer misses a core point from the question bank, the interviewer asks one follow-up about the missing point. |
| **AI scoring** | Google Gemini scores content and gives strengths, improvements and a per-question verdict (Correct / Alternative path / Special / Wrong). Falls back to an offline keyword scorer when AI is unavailable. |
| **Anxiety indicator** | Estimated anxiety level per session with a trend compared to the previous session. |
| **History & results** | Session history, detailed transcript review, optional result email. |
| **Light / dark theme** | User-selected theme stored in the browser (`localStorage`), toggle in the desktop nav and on the Profile page. |
| **Secure authentication** | Email + password with OTP verification, Google OAuth 2.0, password recovery, login throttling, Cloudflare Turnstile CAPTCHA. |

> 🚧 **In progress:** CV upload extracts text (PdfPig / Tesseract OCR) but AI parsing and CV-based questions are not finished yet.

---

## 🛠 Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core MVC on **.NET 10** (C#) |
| Database | **MySQL 8.0** via Entity Framework Core + Pomelo provider (migrations run automatically on startup) |
| Cache / sessions | **Redis** |
| AI | **Google Gemini** (text model, with fallback models) · Groq + multi-key rotation (being integrated) |
| CV processing | UglyToad.PdfPig (PDF), Tesseract OCR (`tessdata/vie`, `tessdata/eng`) |
| Media storage | Cloudinary |
| Email | SMTP |
| Security | Google OAuth 2.0, Cloudflare Turnstile, BCrypt, IP rate limiting, login throttling |
| Configuration | DotNetEnv (`.env`) locally · Dokploy Environment tab in production |
| Deployment | Docker Compose on a VPS managed by **Dokploy** (Traefik reverse proxy + automatic HTTPS) |

---

## 📁 Project Structure

```
MockviewAI/
├── Controllers/            # MVC controllers (Auth, Interview, Profile, Cv, ...)
├── Data/
│   ├── ApplicationDbContext.cs
│   └── InterviewQuestions/ # question-bank.v1.json (questions, core points, follow-ups)
├── Middleware/             # Global error handling
├── Migrations/             # EF Core migrations
├── Models/                 # Entities, DTOs, ViewModels, AI models
├── Repositories/           # Generic + user repositories (interfaces / implementations)
├── Services/
│   ├── Ai/                 # Scoring, prompt building, Gemini client, delivery & anxiety analysis
│   ├── InterviewQuestions/ # Question bank, selector, offline scorer, follow-up planner
│   ├── Interviews/         # Interview session workflow
│   ├── KeyManagement/      # API key rotation (Gemini / Groq)
│   ├── Security/           # Turnstile, login throttling, password policy, security headers
│   └── ...                 # Auth, Email, FileStorage, Profiles, Onboarding, Feedback, CvParser
├── Views/                  # Razor views + shared partials (_AppNavigation, _ThemeHead, _ThemeToggle)
├── wwwroot/
│   ├── css/
│   │   ├── themes/         # theme-indigo.css (light), theme-dark-1.css (dark) — ALL colors live here
│   │   ├── base.css        # Typography + layout primitives
│   │   ├── components.css  # Reusable components (structure only)
│   │   └── <Page>.css      # Page-specific layout
│   ├── js/                 # Page scripts + shared helpers (_ThemeToggle.js, _Notify.js, ...)
│   └── ai/                 # Interviewer avatar module (standalone test page: /ai/avatar.html)
├── tessdata/               # OCR language data
└── Program.cs              # DI registration + middleware pipeline
```

---

## 🚀 Getting Started (Local Development)

The recommended local setup runs **MySQL + Redis in Docker** and the **web app from Visual Studio / .NET CLI** for debugging.

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- Visual Studio / VS Code / Rider (optional)

### 1. Start MySQL and Redis

Use the local compose file (`docker-compose.local.yml`). Rename it to `docker-compose.yml` on your machine — **do not commit it over the production file**.

Create a `.env` next to it:

```env
MYSQL_ROOT_PASSWORD=your_local_password
```

```bash
docker compose up -d
```

MySQL is available at `127.0.0.1:3306`, Redis at `127.0.0.1:6379` (bound to localhost only).

### 2. Configure the application (`.env`)

This project uses **DotNetEnv** — no `secrets.json`. Create a `.env` in the solution root. Double underscores (`__`) map to ASP.NET Core configuration sections (`Section__Key` → `Section:Key`).

> ⚠️ Never commit `.env`. Make sure it is listed in `.gitignore`.

```env
# Template .env

# --- Database
MYSQL_ROOT_PASSWORD=
MYSQL_USER_PASSWORD=

DB_HOST=localhost # localhost/mysql_mockview_container
DB_PORT=3306
DB_NAME=mockview_database
DB_USER=root # root/mockview_user

REDIS_HOST=redis_mockview_container # localhost/redis_mockview_container
REDIS_PORT=6379

ConnectionStrings__DefaultConnection=Server=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Uid=${DB_USER};Pwd=${MYSQL_USER_PASSWORD};
ConnectionStrings__RedisConnection=${REDIS_HOST}:${REDIS_PORT}


# --- Cloudflare Turnstile
Turnstile__SiteKey=
Turnstile__SecretKey=

# --- Bảo mật đăng nhập - option
Security__MaxFailedLogins=5
Security__LockoutMinutes=15

# --- Email (SMTP)
SmtpSettings__Server=smtp.gmail.com
SmtpSettings__Port=587
SmtpSettings__Username=
SmtpSettings__SenderName=
SmtpSettings__SenderEmail=
SmtpSettings__Password=

# --- Google Authentication
Authentication__Google__ClientId=
Authentication__Google__ClientSecret=

# --- Cloudinary
CloudinarySettings__CloudName=
CloudinarySettings__ApiSecret=
CloudinarySettings__ApiKey=

# --- AI Score calculator (Gemini)
Ai__Provider=Gemini
Gemini__Model=gemini-3.6-flash
Gemini__FallbackModels__0=gemini-3.5-flash-lite
Gemini__MaxTokens=5000
Gemini__ThinkingLevel=low # (minimal / low / medium / high)


# --- AI Interview (Gemini) - option
Interviewer__Enabled=true # false = available question only
Interviewer__Model=gemini-3.5-flash-lite
Interviewer__ThinkingLevel=minimal
Interviewer__TimeoutSeconds=8
Interviewer__CvContextHours=6

# --- Gemini API
Gemini__ApiKeys__0=
Gemini__ApiKeys__1=
Gemini__ApiKeys__2=
Gemini__ApiKeys__3=
Gemini__ApiKeys__4=
Gemini__ApiKeys__5=

# --- Groq API
Groq__ApiKeys__0=
Groq__ApiKeys__1=
Groq__ApiKeys__2=
Groq__ApiKeys__3=
Groq__ApiKeys__4=
Groq__ApiKeys__5=

# --- Speech Settings - option
Speech__WhisperModel=whisper-large-v3
Speech__WhisperFallbackModel=
Speech__Language=auto
Speech__AllowedLanguages=vi,en
Speech__FallbackLanguage=vi
Speech__MaxAudioBytes=10485760
Speech__TranscribeTimeoutSeconds=30
Speech__NoSpeechThreshold=0.6
Speech__LogprobThreshold=-1.0
Speech__CompressionRatioThreshold=2.4
Speech__UseQuestionAsWhisperPrompt=false
Speech__WhisperStylePrompt="Dạ, em làm backend với C#, .NET Core, Docker, Redis cache, PostgreSQL và REST API."

Speech__NormalizeTranscript=true
Speech__NormalizerModel=gemini-3.5-flash-lite
Speech__NormalizerFallbackModel=
Speech__NormalizerThinkingLevel=minimal
Speech__NormalizeAttemptTimeoutSeconds=8
Speech__NormalizeTimeoutSeconds=14
Speech__MaxLengthChangeRatio=0.3

# --- CV - option
Cv__Model=gemini-3.5-flash-lite
Cv__ThinkingLevel=minimal
Cv__AttemptTimeoutSeconds=30
Cv__ParseTimeoutSeconds=90
Cv__MaxFileBytes=10485760
Cv__MaxPages=6
Cv__MaxTextChars=15000
Cv__MinTextLetters=200
Cv__PendingMinutes=30
Cv__MaxCvsPerUser=3
Cv__BrowserKeepDays=30
```

### 3. Run

```bash
dotnet run --project MockviewAI
```

Or press **F5** in Visual Studio. Database migrations are applied automatically on startup. The app opens on the login page (`/Auth/Login`).

### Run everything in Docker (optional)
Uncomment the `web_mockview` service in the local compose file. The app is then served at `http://localhost:9090`. Inside Docker the connection strings must use service names (`Server=db_mockview;...`, `redis_mockview:6379`) instead of `127.0.0.1`.

---

## ☁️ Production Deployment

Production runs on a VPS managed by **Dokploy** using `docker-compose.yml` at the repository root.

- **3 containers** on one internal network: `web_mockview`, `db_mockview`, `redis_mockview`. MySQL and Redis are not exposed publicly.
- **Traefik** (built into Dokploy) routes `mockviewai.com` to the web container's internal port `8080` and issues SSL certificates automatically.
- **All secrets** (30+ variables: connection strings, API keys, SMTP, OAuth) are managed in Dokploy's **Environment** tab. Nothing sensitive is stored in `appsettings.json` or the repository.
- The app uses a dedicated MySQL user (`mockview_user`), not `root`.
- MySQL data is stored in the `mysql_data_final` volume — **do not rename or delete it**, or the database will be re-created empty.

Additional production variables used by the compose file:

```env
MYSQL_ROOT_PASSWORD=...
MYSQL_USER_PASSWORD=...
```

> Every new configuration key must be added **both** to the `environment:` block of `docker-compose.yml` **and** to Dokploy's Environment tab; otherwise the container will not receive it.

---

## 🎨 Theming Rules

- **Only `wwwroot/css/themes/` decides colors.** `base.css`, `components.css` and page CSS files use CSS variables only (`var(--color-...)`) — no hex, `rgb()` or `white`.
- Need a new color? Add a semantic token to **both** `theme-indigo.css` and `theme-dark-1.css`, then use it in page CSS.
- Main token groups: base palette (`--color-text`, `--color-surface`, `--color-primary`, ...), on-gradient content (`--color-on-gradient-*`), interview modes (`--color-mode-practice-*`, `--color-mode-live-*`), interviewer personas (`--color-persona-*`), answer verdicts (`--color-verdict-*`), status (`success` / `warning` / `danger` / `info` / `special`).
- Pages load themes through `<partial name="Partials/_ThemeHead" />` at the end of `<head>`. The default theme is **light**; the app does not follow the OS color scheme.

---

## 🧭 Coding Conventions

1. Follow **SOLID** principles — controllers depend on service interfaces; each service has one responsibility.
2. Document code with `/// <summary>` and group code with `#region` (C#, JS: `//#region`, CSS: `/* #region */`, Razor: `@* #region *@`).
3. Never commit secrets — use `.env` locally and Dokploy Environment in production.
4. Only show user-safe error messages; system errors are logged, not displayed.

---

## 📄 License

See [LICENSE](LICENSE).