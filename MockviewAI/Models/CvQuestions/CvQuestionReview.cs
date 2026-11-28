namespace MockviewAI.Models.CvQuestions
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Kết quả kiểm duyệt một câu: pass (dùng được), fix (sửa 1 lần), reject (bỏ).
    /// </summary>
    public class CvQuestionReview
    {
        public const string Pass = "pass";
        public const string Fix = "fix";
        public const string Reject = "reject";

        /// <summary>Đạt sau khi sửa (chỉ dùng để lưu DB).</summary>
        public const string Fixed = "fixed";

        /// <summary>Vị trí câu trong danh sách gửi duyệt (0, 1, 2...).</summary>
        public int Index { get; set; }

        public string Verdict { get; set; } = Reject;

        /// <summary>Lý do không đạt / cần sửa.</summary>
        public List<string> Issues { get; set; } = new();
    }
}
