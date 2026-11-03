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
        public string Model { get; set; } = "gemini-3.8-flash";   // model VĂN BẢN; không dùng bản -tts/-live/-image

        // Model thử tiếp khi model chính quá tải (503), hết lượt (429) hoặc quá giờ chờ.
        // Để trống (null) = dùng mặc định gemini-3.5-flash-lite. Không để danh sách sẵn ở đây vì
        // trình đọc cấu hình sẽ NỐI thêm vào danh sách có sẵn thay vì thay thế.
        public string[]? FallbackModels { get; set; }

        public int MaxTokens { get; set; } = 2048;
    }
}
