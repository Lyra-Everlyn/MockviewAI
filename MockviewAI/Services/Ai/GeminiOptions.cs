namespace MockviewAI.Services.Ai
{
    /// <summary>
    /// [AI-MODULE] 
    /// Cấu hình Gemini cho chấm điểm. Key do GeminiKeyProvider đọc (Gemini__ApiKeys__0..N).
    /// </summary>
    public class GeminiOptions
    {
        /// <summary>Model văn bản; không dùng bản -tts / -live / -image.</summary>
        public string Model { get; set; } = "gemini-3.8-flash";

        /// <summary>
        /// Model thử tiếp khi model chính quá tải / hết lượt / quá giờ. null = gemini-3.5-flash-lite.
        /// Không đặt danh sách sẵn ở đây vì trình đọc cấu hình sẽ nối thêm thay vì thay thế.
        /// </summary>
        public string[]? FallbackModels { get; set; }

        public int MaxTokens { get; set; } = 2048;

        /// <summary>Mức "suy nghĩ" khi chấm (minimal / low / medium / high). Thấp hơn = nhanh hơn. Trống = không gửi.</summary>
        public string ThinkingLevel { get; set; } = "low";
    }
}
