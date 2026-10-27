# MockviewAI 🤖👔

> Practice interviewing with AI before your real interview day.

MockviewAI is an AI-powered mock interview platform designed specifically for upcoming graduates and entry-level professionals. It provides a safe, realistic environment to practice interview skills, receive instant feedback, and reduce interview anxiety.

## ✨ Key Features

*   **Role-Specific Interviews:** Choose your target job role and interviewer persona.
*   **Flexible Interaction:** Respond to interview questions via voice or text.
*   **Comprehensive Analytics:** Receive an overall score, detailed feedback, and an anxiety level assessment after each session.
*   **Secure Authentication:** Includes Local Login, Google OAuth, OTP-based Registration, and Password Recovery.
*   **Advanced Security:** Built-in rate limiting, Redis caching, and Cloudflare Turnstile CAPTCHA integration.

## 🛠 Tech Stack

*   **Framework:** ASP.NET Core MVC (C#)
*   **Database:** MySQL (via Entity Framework Core)
*   **Caching & Rate Limiting:** Redis
*   **AI Engine:** Google Gemini (gemini-3.8-flash / gemini-2.5-flash)
*   **Media Storage:** Cloudinary
*   **Security:** Cloudflare Turnstile, BCrypt

---

## 🚀 Getting Started

You can run this project locally using Visual Studio / .NET CLI or fully containerized via Docker Compose.

### Prerequisites
*   [.NET 8.0 SDK](https://dotnet.microsoft.com/download) (or higher)
*   [Docker Desktop](https://www.docker.com/products/docker-desktop) (if using Docker)
*   A MySQL Server and Redis instance (if running locally without Docker)

---

### Option A: Local Development (Visual Studio / .NET CLI)

If you are running the project directly via Visual Studio (F5) or the .NET CLI, you must set up **User Secrets** so your sensitive credentials are not committed to source control.

**1. Open your terminal in the project directory**
```bash
cd MockviewAI/MockviewAI
```

**2. Set up User Secrets**
Run the following commands one by one, replacing the `YOUR_...` placeholders with your actual API keys and credentials:

```bash
# Database and Redis (Local)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=mockview_database;Uid=root;Pwd=change-me;"
dotnet user-secrets set "ConnectionStrings:RedisConnection" "localhost:6379"

# Google Authentication
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_GOOGLE_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_GOOGLE_CLIENT_SECRET"

# Email Configuration (SMTP)
dotnet user-secrets set "SmtpSettings:Server" "smtp.gmail.com"
dotnet user-secrets set "SmtpSettings:Port" "587"
dotnet user-secrets set "SmtpSettings:SenderName" "MockviewAI"
dotnet user-secrets set "SmtpSettings:SenderEmail" "your_email@gmail.com"
dotnet user-secrets set "SmtpSettings:Username" "your_email@gmail.com"
dotnet user-secrets set "SmtpSettings:Password" "your_gmail_app_password"

# Cloudinary (Media Storage)
dotnet user-secrets set "CloudinarySettings:CloudName" "YOUR_CLOUD_NAME"
dotnet user-secrets set "CloudinarySettings:ApiKey" "YOUR_API_KEY"
dotnet user-secrets set "CloudinarySettings:ApiSecret" "YOUR_API_SECRET"

# AI Scoring (Gemini)
dotnet user-secrets set "Ai:Provider" "Gemini"
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_API_KEY"
dotnet user-secrets set "Gemini:Model" "gemini-3.8-flash"

# Cloudflare Turnstile (Captcha)
dotnet user-secrets set "Turnstile:SiteKey" "YOUR_TURNSTILE_SITE_KEY"
dotnet user-secrets set "Turnstile:SecretKey" "YOUR_TURNSTILE_SECRET_KEY"
```

Once configured, simply press **F5** in Visual Studio or run `dotnet run`.

---

### Option B: Docker Compose

If you prefer to run the entire stack (App, MySQL, Redis) via Docker, the system will read configurations from an `.env` file.

*Note: When converting ASP.NET Core `appsettings.json` hierarchy to Docker environment variables, the colon (`:`) is replaced by a double underscore (`__`).*

**1. Create the `.env` file**
In the root directory of the project (where `docker-compose.yml` is located), create a file named `.env` and add the following content (replace placeholders with real values):

```env
# ==========================================
# DOCKER COMPOSE & ENVIRONMENT VARIABLES
# ==========================================
# Note: NEVER commit the actual .env file to Git.

# MySQL Root Password for the container
MYSQL_ROOT_PASSWORD=change-me

# --- Internal Docker Network Connections ---
ConnectionStrings__DefaultConnection=Server=mysql;Port=3306;Database=mockview_database;Uid=root;Pwd=change-me;
ConnectionStrings__RedisConnection=redis:6379

# --- Google Authentication ---
Authentication__Google__ClientId=YOUR_GOOGLE_CLIENT_ID
Authentication__Google__ClientSecret=YOUR_GOOGLE_CLIENT_SECRET

# --- SMTP Configuration ---
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

**2. Launch the application**
Open your terminal in the directory containing `docker-compose.yml` and run:
```bash
docker-compose up -d --build
```
Docker will automatically provision the database, start Redis, and launch the web application.

---

## 📂 Project Structure

*   `Controllers/`: ASP.NET Core MVC Controllers handling HTTP requests.
*   `Services/`: Core business logic (Auth, AI, Interviews, Evaluation).
*   `Models/`: Data entities, DTOs, and ViewModels.
*   `Views/`: UI templates using Razor syntax (Vietnamese UI).

## 📄 License
This project is created for academic research purposes.
