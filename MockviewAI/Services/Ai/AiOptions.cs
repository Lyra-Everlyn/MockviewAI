// [AI-MODULE] GeminiOptions
// Cấu hình Gemini: ApiKey, Model, MaxTokens.
// Đọc từ appsettings hoặc biến môi trường Gemini__ApiKey, Gemini__Model. KHÔNG ghi key vào file.

namespace MockviewAI.Services.Ai
{
    // Bound from appsettings / environment variables:  Gemini__ApiKey, Gemini__Model, ...
    public class GeminiOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-2.5-flash";
        public int MaxTokens { get; set; } = 2048;
    }
}
