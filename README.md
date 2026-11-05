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
*   **AI Engine:** Google Gemini & Groq APIs (with Multi-key rotation)
*   **Media Storage:** Cloudinary
*   **Security:** Cloudflare Turnstile, BCrypt
*   **Configuration:** DotNetEnv

---

## 🚀 Getting Started

You can run this project locally using Visual Studio / .NET CLI or fully containerized via Docker Compose. 

**Important:** This project uses `DotNetEnv` for configuration management. We do not use `.NET User Secrets` (`secrets.json`). All environment variables are managed via a single `.env` file.

### Prerequisites
*   [.NET 8.0 SDK](https://dotnet.microsoft.com/download) (or higher)
*   [Docker Desktop](https://www.docker.com/products/docker-desktop) (if using Docker)
*   A MySQL Server and Redis instance (if running locally without Docker)

---

### 1. Environment Configuration (.env)

Whether you run locally or via Docker, create an `.env` file in the root directory of the project. 

*Note: The project uses double underscores (`__`) to map ASP.NET Core's hierarchical configuration data.*

```env
# ==========================================
# ENVIRONMENT VARIABLES (.env)
# ==========================================
# Note: NEVER commit this file to Git. Ensure it is in .gitignore.

# Database & Redis
ConnectionStrings__DefaultConnection=change-me;
ConnectionStrings__RedisConnection=change-me

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
CloudinarySettings__ApiSecret=YOUR_API_SECRET
CloudinarySettings__ApiKey=YOUR_API_KEY

# AI Configuration (Gemini Provider)
Ai__Provider=Gemini
Gemini__Model=gemini-3.8-flash
Gemini__ApiKey=YOUR_MAIN_GEMINI_KEY

# Gemini API Keys Array (For multi-key rotation)
Gemini__ApiKeys__0=KEY_1
Gemini__ApiKeys__1=KEY_2
Gemini__ApiKeys__2=KEY_3

# Groq API Keys Array (For fallback/multi-key rotation)
Groq__ApiKeys__0=KEY_1
Groq__ApiKeys__1=KEY_2
