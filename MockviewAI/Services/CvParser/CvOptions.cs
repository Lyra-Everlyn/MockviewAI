namespace MockviewAI.Services.CvParser
{
    /// <summary>
    /// [USER CV] Cấu hình xử lý CV (section "Cv"). Mọi giá trị đều có mặc định.
    /// </summary>
    public class CvOptions
    {
        public const string SectionName = "Cv";

        #region File
        public int MaxFileBytes { get; set; } = 10 * 1024 * 1024;
        public int MaxPages { get; set; } = 6;
        public int MaxTextChars { get; set; } = 15000;

        /// <summary>Ít chữ cái hơn ngưỡng này thì coi là PDF scan, gửi nguyên file cho Gemini.</summary>
        public int MinTextLetters { get; set; } = 200;
        #endregion

        #region AI
        /// <summary>Model thử đầu tiên; sau đó tới Gemini:Model và Gemini:FallbackModels.</summary>
        public string? Model { get; set; } = "gemini-3.5-flash-lite";
        public string ThinkingLevel { get; set; } = "minimal";
        public int AttemptTimeoutSeconds { get; set; } = 30;
        public int ParseTimeoutSeconds { get; set; } = 90;
        #endregion

        #region Storage
        /// <summary>Thời gian giữ bản chờ xác nhận trong Redis.</summary>
        public int PendingMinutes { get; set; } = 30;
        public int MaxCvsPerUser { get; set; } = 3;
        public int BrowserKeepDays { get; set; } = 30;
        #endregion
    }
}
