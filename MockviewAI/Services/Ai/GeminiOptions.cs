namespace MockviewAI.Services.Ai
{
    /// <summary>
    /// [AI-SCORING] 
    /// Cấu hình Gemini (mục "Gemini", biến môi trường Gemini__*). Key do GeminiKeyProvider đọc (Gemini__ApiKeys__0..N).
    /// Model ở đây cũng là model mặc định cho CV, chuẩn hoá giọng nói và nhà tuyển dụng khi các mục đó để trống.
    /// </summary>
    public class GeminiOptions
    {
        /// <summary>Model văn bản; không dùng bản -tts / -live / -image.</summary>
        public string Model { get; set; } = "gemini-3.6-flash";

        /// <summary>
        /// Model thử tiếp khi model chính quá tải / hết lượt / quá giờ. null = gemini-3.5-flash-lite.
        /// Không đặt danh sách sẵn ở đây vì trình đọc cấu hình sẽ nối thêm thay vì thay thế.
        /// </summary>
        public string[]? FallbackModels { get; set; }

        /// <summary>Giới hạn token đầu ra khi chấm điểm.</summary>
        public int MaxTokens { get; set; } = 5000;

        /// <summary>Mức "suy nghĩ" khi chấm (minimal / low / medium / high). Thấp hơn = nhanh hơn. Trống = không gửi.</summary>
        public string ThinkingLevel { get; set; } = "low";
    }
}
