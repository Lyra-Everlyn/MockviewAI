# MockviewAI 🤖👔

> Practice interviewing with AI before your real interview day.
> 🌐 **Live:** [mockviewai.com](https://mockviewai.com)

MockviewAI is an AI-powered mock interview platform for IT students, upcoming graduates and entry-level candidates. It gives a safe, realistic environment to practice technical interviews by voice or text, get a short reaction and a follow-up question after every answer, receive a scored report, and track interview anxiety over time.

---

## ✨ Key Features

| Area | What it does |
|---|---|
| **Role-specific interviews** | Choose a target role (Developer, Tester, BA), an interviewer persona (HR, Tech Lead, Project Manager) and 5–10 questions from a curated question bank. |
| **Two interview modes** | **Practice** – retry answers, type instead of speak, replay questions. **Live** – think time, answer time limit, no retries; skipped questions count as wrong. |
| **Voice answers (STT)** | Live text from the browser's speech recognition + the recorded answer transcribed by **Groq Whisper**, hallucination filtering, then **Gemini** merges both versions and fixes IT terms (e.g. "xi thăng" → C#). Recordings are never stored. |
| **Conversational interviewer** | After each answer the AI interviewer gives a 1–2 sentence reaction and may ask a follow-up question, using the question bank's core points and (optionally) the candidate's CV. Falls back to canned transitions when AI is slow or unavailable. |
| **CV-based questions** | Upload a PDF CV → text extracted with PdfPig (scanned PDFs are sent to Gemini directly) → Gemini parses it → the candidate reviews and saves it **in the browser**. A short CV summary is used during the interview. |
| **AI scoring** | Gemini scores content and gives strengths, improvements and a per-question verdict (Correct / Alternative path / Special / Wrong). Falls back to an offline keyword scorer when AI is unavailable. |
| **Delivery & anxiety** | Volume, pauses and pitch variation are measured in the browser; an anxiety indicator is estimated per session and compared with the previous one. |
| **History & results** | Session history, transcript review, optional result email. |
| **Light / dark theme** | User-selected theme stored in `localStorage`, toggle in the desktop nav and on the Profile page. |
| **Admin (read-only)** | `/Admin` shows user / session / feedback statistics, the user list and all feedback. No edit or delete actions. Access is granted by email list in configuration (`Admin__Emails__N`), not stored in the database. |
| **Secure authentication** | Email + password with OTP verification, Google OAuth 2.0, password recovery, login throttling, Cloudflare Turnstile CAPTCHA, IP rate limiting. |

---

## 🛠 Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core MVC on **.NET 10** (C#) |
| Database | **MySQL 8.0** via Entity Framework Core + Pomelo provider (migrations run automatically on startup) |
| Cache / temporary data | **Redis** — OTP codes, pending CV (30 min), per-session CV summary (6 h) |
| AI | **Google Gemini** (scoring, interviewer, transcript normalisation, CV parsing) · **Groq Whisper** (speech-to-text) · multi-key rotation for both providers |
| CV processing | UglyToad.PdfPig (PDF text extraction) — no OCR library |
| Media storage | Cloudinary (avatars only) |
| Email | SMTP (MailKit) |
| Security | Google OAuth 2.0, Cloudflare Turnstile, BCrypt, IP rate limiting, login throttling, security headers |
| Configuration | DotNetEnv (`.env`) locally · Dokploy Environment tab in production |
| Deployment | Docker Compose on a VPS managed by **Dokploy** (Traefik reverse proxy + automatic HTTPS) |

---

## 🧩 Architecture

```
Browser ──HTTPS──▶ Traefik (Dokploy) ──▶ web_mockview :8080 ──┬──▶ db_mockview    (MySQL 8.0)
                                                             ├──▶ redis_mockview (Redis)
                                                             ├──▶ Gemini API     (key rotation)
                                                             ├──▶ Groq API       (key rotation)
                                                             └──▶ Cloudinary · SMTP · Google OAuth · Turnstile
```

**One answer, end to end**

1. The browser records the answer and shows live text (Web Speech API).
2. `SpeechToTextService`: Groq Whisper → `WhisperHallucinationFilter` → `GeminiTranscriptNormalizer` (merge with browser text; on error, keep the raw text).
3. `InterviewService` saves the answer; `GeminiInterviewerTurnService` returns a short reaction and an optional follow-up (`FollowUpPlanner` + CV summary from Redis).
4. When the session ends, `InterviewScoringService` scores content with Gemini (or `BankKeywordScorer` offline) and delivery/anxiety in code, then the result page and optional email are produced.

**Key rotation** — `ApiKeyRotationHandler` attaches a key to every Gemini / Groq request. A key-related error (401, 402, 403, 429) puts that key on a 1-minute cooldown and the request is retried once with the next key; 5xx errors are retried once after 1.5 s.

**Errors** — `ErrorHandlingMiddleware` is the only global error handler: it logs the exception and returns JSON for `fetch` requests or a self-contained Vietnamese error page otherwise.

---

## 📁 Project Structure

```
MockviewAI/
├── Controllers/            # Auth, Home, Interview, Cv, Profile, Onboarding
├── Data/
│   ├── ApplicationDbContext.cs
│   └── InterviewQuestions/ # question-bank.v1.json (questions, core points, answer paths, follow-ups)
├── Middleware/             # ErrorHandlingMiddleware
├── Migrations/             # EF Core migrations
├── Models/                 # Entities, DTOs, ViewModels, AI / speech / question-bank models
├── Repositories/           # Generic + user repositories (interfaces / implementations)
├── Services/
│   ├── Ai/                 # Scoring, prompt building, Gemini client, delivery & anxiety analysis
│   ├── CvParser/           # PDF extraction, Gemini CV parsing, pending CV store (Redis)
│   ├── InterviewQuestions/ # Question bank, selector, offline scorer, follow-up planner
│   ├── Interviews/         # Session workflow + Conversation/ (AI interviewer, CV context)
│   ├── KeyManagement/      # API key rotation (Gemini / Groq)
│   ├── Speech/             # Groq Whisper, hallucination filter, Gemini normaliser
│   ├── Security/           # Turnstile, login throttling, password policy, security headers
│   └── ...                 # Auth, Emails, FileStorage, Profiles, Onboarding, Feedbacks, SessionHistory
├── Views/                  # Razor views + shared partials (_AppNavigation, _ThemeHead, _ThemeToggle, ...)
├── wwwroot/
│   ├── css/
│   │   ├── themes/         # theme-indigo.css (light), theme-dark-1.css (dark) — ALL colors live here
│   │   ├── base.css        # Typography + layout primitives
│   │   ├── components.css  # Reusable components (structure only)
│   │   └── <Page>.css      # Page-specific layout
│   ├── js/                 # Page scripts + shared helpers (_ThemeToggle.js, _Notify.js, CvStorage.js, ...)
│   └── ai/                 # [KHÔNG DÙNG] experimental interviewer avatar (/ai/avatar.html)
└── Program.cs              # DI registration + middleware pipeline
```

Code that is kept but no longer used is tagged `[KHÔNG DÙNG]`; development-only code is tagged `[CHỈ DEVELOPMENT]`.

---

## 🚀 Getting Started (Local Development)

The recommended local setup runs **MySQL + Redis in Docker** and the **web app from Visual Studio / .NET CLI** for debugging.

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- A Chromium-based browser (Chrome / Edge) for live speech text. Opera and Brave may block the browser speech service; answers are still transcribed by Whisper.

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


# --- Admin
Admin__Emails__0=


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

## ⚙️ Optional Settings

Every setting below has a default in code / `appsettings.json`; set the environment variable only to change it.

| Prefix | Purpose | Main keys (default) |
|---|---|---|
| `Speech__` | Whisper + transcript normalisation | `WhisperModel` (`whisper-large-v3`), `Language` (`auto`; production compose uses `vi`), `AllowedLanguages` (`vi,en`), `FallbackLanguage` (`vi`), `NormalizeTranscript` (`true`), `NormalizerModel` (`gemini-3.5-flash-lite`), `MaxAudioBytes` (10 MB) |
| `Interviewer__` | AI interviewer reaction + follow-up | `Enabled` (`true`), `Model` (`gemini-3.5-flash-lite`), `TimeoutSeconds` (`8`), `CvContextHours` (`6`) |
| `Cv__` | CV upload and parsing | `MaxFileBytes` (10 MB), `MaxPages` (`6`), `PendingMinutes` (`30`), `MaxCvsPerUser` (`3`), `BrowserKeepDays` (`30`) |
| `Security__` | Login lockout | `MaxFailedLogins` (`5`), `LockoutMinutes` (`15`) |
| `DataProtection__KeysPath` | Persist cookie / anti-forgery keys | unset locally; `/app/keys` in production |

---

## ☁️ Production Deployment

Production runs on a VPS managed by **Dokploy** using `docker-compose.yml` at the repository root.

- **3 containers** on one internal network: `web_mockview`, `db_mockview`, `redis_mockview`. MySQL and Redis are not exposed publicly.
- **Traefik** (built into Dokploy) routes `mockviewai.com` to the web container's internal port `8080` and issues SSL certificates automatically.
- **All secrets** (30+ variables: connection strings, API keys, SMTP, OAuth) are managed in Dokploy's **Environment** tab. Nothing sensitive is stored in `appsettings.json` or the repository.
- The app uses a dedicated MySQL user (`mockview_user`), not `root`.
- Volumes — **do not rename or delete**:
  - `mysql_data_final` — MySQL data (renaming re-creates an empty database).
  - `dataprotection_keys` — cookie / anti-forgery keys (losing them signs every user out).

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
3. Tag the module in every summary (`[AUTH]`, `[AI-SCORING]`, `[SPEECH]`, `[USER CV]`, ...); tag unused code `[KHÔNG DÙNG]`.
4. Never commit secrets — use `.env` locally and Dokploy Environment in production.
5. Only show user-safe error messages; system errors are logged, not displayed.

---

## 📄 License

MIT — see [LICENSE](LICENSE).
