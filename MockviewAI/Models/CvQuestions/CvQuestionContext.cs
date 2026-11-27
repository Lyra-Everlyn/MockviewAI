namespace MockviewAI.Models.CvQuestions
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Thông tin gửi người soạn / người kiểm duyệt khi soạn câu hỏi theo CV.
    /// </summary>
    public class CvQuestionContext
    {
        /// <summary>developer | tester | ba.</summary>
        public string PositionKey { get; set; } = string.Empty;

        /// <summary>Tên vị trí hiển thị, vd "Lập trình viên".</summary>
        public string PositionName { get; set; } = string.Empty;

        /// <summary>linh | nam | an.</summary>
        public string InterviewerKey { get; set; } = string.Empty;

        /// <summary>Tóm tắt CV đã làm sạch (CvContextBuilder), không phải CV gốc.</summary>
        public string CvSummary { get; set; } = string.Empty;

        /// <summary>Câu bộ đề đã chọn cho buổi, để tránh trùng.</summary>
        public List<string> ExistingQuestions { get; set; } = new();
    }
}
