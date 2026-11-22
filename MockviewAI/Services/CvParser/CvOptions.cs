namespace MockviewAI.Services.CvParser
{
    /// <summary>
    /// [USER CV] 
    /// Cấu hình xử lý CV (mục "Cv", biến môi trường Cv__*). Mọi giá trị đều có mặc định.
    /// </summary>
    public class CvOptions
    {
        public const string SectionName = "Cv";

        #region File
        /// <summary>Dung lượng tối đa của file CV (byte), mặc định 10 MB.</summary>
        public int MaxFileBytes { get; set; } = 10 * 1024 * 1024;

        /// <summary>Chỉ đọc chữ của chừng này trang đầu.</summary>
        public int MaxPages { get; set; } = 6;

        /// <summary>Cắt văn bản gửi AI ở độ dài này.</summary>
        public int MaxTextChars { get; set; } = 15000;

        /// <summary>Ít chữ cái hơn ngưỡng này thì coi là PDF scan, gửi nguyên file cho Gemini.</summary>
        public int MinTextLetters { get; set; } = 200;
        #endregion

        #region AI
        /// <summary>Model thử đầu tiên; sau đó tới Gemini:Model và Gemini:FallbackModels.</summary>
        public string? Model { get; set; } = "gemini-3.5-flash-lite";

        /// <summary>Mức suy nghĩ gửi kèm (minimal = nhanh nhất); model không nhận thì gửi lại không kèm.</summary>
        public string ThinkingLevel { get; set; } = "minimal";

        /// <summary>Giới hạn thời gian cho mỗi model (giây).</summary>
        public int AttemptTimeoutSeconds { get; set; } = 30;

        /// <summary>Giới hạn thời gian cho cả lần phân tích, gồm mọi model (giây).</summary>
        public int ParseTimeoutSeconds { get; set; } = 90;
        #endregion

        #region Lưu trữ
        /// <summary>Thời gian giữ bản chờ xác nhận trong Redis (phút).</summary>
        public int PendingMinutes { get; set; } = 30;

        /// <summary>Số CV tối đa mỗi người lưu trên trình duyệt (kẹp 1–10).</summary>
        public int MaxCvsPerUser { get; set; } = 3;

        /// <summary>Số ngày trình duyệt giữ CV (kẹp 1–365).</summary>
        public int BrowserKeepDays { get; set; } = 30;
        #endregion
    }
}
