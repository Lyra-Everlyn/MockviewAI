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
# Database & Redis (local Docker)
ConnectionStrings__DefaultConnection=Server=127.0.0.1;Port=3306;Database=mockview_database;Uid=root;Pwd=your_local_password
ConnectionStrings__RedisConnection=127.0.0.1:6379

# Cloudflare Turnstile
Turnstile__SiteKey=YOUR_TURNSTILE_SITE_KEY
Turnstile__SecretKey=YOUR_TURNSTILE_SECRET_KEY

# Email (SMTP)
SmtpSettings__Server=smtp.gmail.com
SmtpSettings__Port=587
SmtpSettings__Username=your_email@gmail.com
SmtpSettings__SenderName=MockviewAI
SmtpSettings__SenderEmail=your_email@gmail.com
SmtpSettings__Password=your_gmail_app_password

# Google Authentication
Authentication__Google__ClientId=YOUR_GOOGLE_CLIENT_ID
Authentication__Google__ClientSecret=YOUR_GOOGLE_CLIENT_SECRET

# Cloudinary
CloudinarySettings__CloudName=YOUR_CLOUD_NAME
CloudinarySettings__ApiKey=YOUR_API_KEY
CloudinarySettings__ApiSecret=YOUR_API_SECRET

# AI — set Ai__Provider=Gemini to use real AI; any other value uses the mock scorer
Ai__Provider=Gemini
Gemini__Model=YOUR_GEMINI_TEXT_MODEL      # must be a text model (not -tts / -live / image)
Gemini__ApiKey=YOUR_MAIN_GEMINI_KEY

# Optional: key rotation (being integrated)
Gemini__ApiKeys__0=KEY_1
Gemini__ApiKeys__1=KEY_2
Groq__ApiKeys__0=KEY_1
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