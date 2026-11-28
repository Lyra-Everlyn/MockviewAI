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

        /// <summary>
        /// Model soạn dự phòng khi GeneratorModel lỗi tạm thời (503 quá tải, 429...), thử lần lượt. Đo 27/11:
        /// gemini-3.1-flash-lite ~5,5 giây. Các model khác trên gói miễn phí đều không hợp làm dự phòng:
        /// gemini-3-flash-preview 15–17 giây, có lần 27,7 giây rồi 503 (ăn hết 30 giây); 3.5-flash / 3.6-flash 12–89 giây;
        /// 3.7/3.8-flash không có lượt; gemini-flash-lite-latest chỉ là tên khác của 3.5-flash-lite; 2.5-flash-lite trả 404.
        /// Để null (không gán sẵn mảng): .NET NỐI biến môi trường vào sau mảng có sẵn chứ không thay, nên mặc định nằm ở GeneratorFallbackList.
        /// </summary>
        public string[]? GeneratorFallbackModels { get; set; }

        /// <summary>Mức suy nghĩ gửi kèm khi soạn câu (minimal = nhanh nhất); model không nhận thì gửi lại không kèm.</summary>
        public string ThinkingLevel { get; set; } = "minimal";

        /// <summary>Model Groq kiểm duyệt (khác họ Gemini). llama-3.3-70b-versatile không còn trong gói Groq hiện tại.</summary>
        public string ReviewerModel { get; set; } = "qwen/qwen3.8-27b";

        /// <summary>
        /// Các model Groq dự phòng, thử lần lượt khi model trước hết lượt (413 / 429), bị gỡ (404) hoặc quá tải.
        /// Gói miễn phí giới hạn token / phút RIÊNG từng model, nên thêm model là thêm lượt.
        /// Đo 27/11: gpt-oss-120b duyệt đúng 9/9 ca thử, ~3 giây. Khai __0 rỗng = không dự phòng.
        /// Để null vì lý do như GeneratorFallbackModels; mặc định nằm ở ReviewerFallbackList.
        /// </summary>
        public string[]? ReviewerFallbackModels { get; set; }

        /// <summary>Tổng thời gian soạn + duyệt + sửa (giây, kẹp 10–45). 30 vì có thể soạn song song cả câu CV lẫn câu theo vị trí.</summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>Số vòng sửa câu bị yêu cầu "fix" (kẹp 0–1).</summary>
        public int MaxFixRounds { get; set; } = 1;

        /// <summary>[IT-ROLES] Số câu kỹ thuật tối đa AI soạn theo vị trí khi bộ đề chưa có câu cho vị trí đó (kẹp 0–6).</summary>
        public int MaxRoleQuestions { get; set; } = 4;

        /// <summary>[IT-ROLES] Thời gian tối đa AI gợi ý vị trí từ CV (giây, kẹp 2–15); quá thì đoán bằng từ khoá.</summary>
        public int RoleSuggestTimeoutSeconds { get; set; } = 6;

        /// <summary>
        /// Model kiểm duyệt báo hết lượt (429 / 413) thì tạm bỏ qua model đó bấy nhiêu giây (kẹp 0–600, 0 = tắt).
        /// Groq gửi kèm thời gian chờ (retry-after) thì dùng số đó, nhưng không quá giá trị này.
        /// Mọi model đều đang nghỉ -> không soạn câu, buổi dùng bộ đề ngay (đỡ tốn lượt Gemini).
        /// </summary>
        public int ReviewerCooldownSeconds { get; set; } = 60;

        #region Danh sách model dự phòng (biến môi trường thay hẳn mặc định)
        private static readonly string[] DefaultGeneratorFallbacks = { "gemini-3.1-flash-lite" };
        private static readonly string[] DefaultReviewerFallbacks = { "openai/gpt-oss-20b", "openai/gpt-oss-120b" };

        public IReadOnlyList<string> GeneratorFallbackList => GeneratorFallbackModels ?? DefaultGeneratorFallbacks;
        public IReadOnlyList<string> ReviewerFallbackList => ReviewerFallbackModels ?? DefaultReviewerFallbacks;
        #endregion

        #region Giá trị đã kẹp
        public int QuestionLimit => Math.Clamp(MaxQuestions, 0, 3);
        public int RoleQuestionLimit => Math.Clamp(MaxRoleQuestions, 0, 6);
        public int RoleSuggestTimeoutLimit => Math.Clamp(RoleSuggestTimeoutSeconds, 2, 15);
        public int TimeoutLimit => Math.Clamp(TimeoutSeconds, 10, 45);
        public int FixRoundLimit => Math.Clamp(MaxFixRounds, 0, 1);
        public int ReviewerCooldownLimit => Math.Clamp(ReviewerCooldownSeconds, 0, 600);
        #endregion
    }
}
