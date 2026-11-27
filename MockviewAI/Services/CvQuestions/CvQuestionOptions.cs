namespace MockviewAI.Services.CvQuestions
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Cấu hình "Hỏi theo CV" (mục "CvQuestions", biến môi trường CvQuestions__*). Mọi giá trị đều có mặc định.
    /// Enabled = false thì tạo buổi y như cũ (100% câu bộ đề).
    /// </summary>
    public class CvQuestionOptions
    {
        public const string SectionName = "CvQuestions";

        public bool Enabled { get; set; } = true;

        /// <summary>Số câu bộ đề được thay bằng câu CV (kẹp 0–3).</summary>
        public int MaxQuestions { get; set; } = 2;

        /// <summary>
        /// Model Gemini soạn câu; trống = Gemini:Model. Mặc định bản lite: đo 27/11 bản lite ~3–4 giây,
        /// gemini-3.6-flash 57–89 giây (vượt giới hạn 25 giây nên luôn hết giờ).
        /// </summary>
        public string? GeneratorModel { get; set; } = "gemini-3.5-flash-lite";

        /// <summary>Mức suy nghĩ gửi kèm khi soạn câu (minimal = nhanh nhất); model không nhận thì gửi lại không kèm.</summary>
        public string ThinkingLevel { get; set; } = "minimal";

        /// <summary>Model Groq kiểm duyệt (khác họ Gemini). llama-3.3-70b-versatile không còn trong gói Groq hiện tại.</summary>
        public string ReviewerModel { get; set; } = "qwen/qwen3.8-27b";

        /// <summary>
        /// Model Groq dự phòng khi model chính hết lượt (429) hoặc quá tải. Gói miễn phí giới hạn token / phút riêng từng model,
        /// nên có model dự phòng thì chịu được nhiều buổi cùng lúc hơn. Trống = không dự phòng.
        /// </summary>
        public string? ReviewerFallbackModel { get; set; } = "openai/gpt-oss-20b";

        /// <summary>Tổng thời gian soạn + duyệt + sửa (giây, kẹp 10–45).</summary>
        public int TimeoutSeconds { get; set; } = 25;

        /// <summary>Số vòng sửa câu bị yêu cầu "fix" (kẹp 0–1).</summary>
        public int MaxFixRounds { get; set; } = 1;

        #region Giá trị đã kẹp
        public int QuestionLimit => Math.Clamp(MaxQuestions, 0, 3);
        public int TimeoutLimit => Math.Clamp(TimeoutSeconds, 10, 45);
        public int FixRoundLimit => Math.Clamp(MaxFixRounds, 0, 1);
        #endregion
    }
}
