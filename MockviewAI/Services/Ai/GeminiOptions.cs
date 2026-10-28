namespace MockviewAI.Services.Ai
{
    /// <summary>
    /// [AI-MODULE] 
    /// GeminiOptions
    /// -- Cấu hình Gemini: ApiKey, Model, MaxTokens.
    /// -- Đọc từ appsettings hoặc biến môi trường Gemini__ApiKey, Gemini__Model. KHÔNG ghi key vào file.
    /// </summary>
    
    public class GeminiOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-3.8-flash"; // model VĂN BẢN; không dùng bản -tts/-live/-image
        public int MaxTokens { get; set; } = 2048;
    }
}
