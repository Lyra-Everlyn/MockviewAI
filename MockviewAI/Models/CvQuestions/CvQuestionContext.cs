namespace MockviewAI.Models.CvQuestions
{
    /// <summary>
    /// [CV-QUESTIONS] [IT-ROLES]
    /// Thông tin gửi người soạn / người kiểm duyệt khi soạn câu hỏi theo CV và câu kỹ thuật theo vị trí.
    /// </summary>
    public class CvQuestionContext
    {
        /// <summary>Khoá vị trí trong ItRoleCatalog, vd developer, devops, game.</summary>
        public string PositionKey { get; set; } = string.Empty;

        /// <summary>Tên vị trí hiển thị, vd "DevOps / Cloud".</summary>
        public string PositionName { get; set; } = string.Empty;

        /// <summary>[IT-ROLES] Mảng kiến thức chính của vị trí (ItRole.Focus), dùng khi soạn câu kỹ thuật theo vị trí.</summary>
        public string PositionFocus { get; set; } = string.Empty;

        /// <summary>linh | nam | an.</summary>
        public string InterviewerKey { get; set; } = string.Empty;

        /// <summary>Tóm tắt CV đã làm sạch (CvContextBuilder), không phải CV gốc. Rỗng = không soạn câu theo CV.</summary>
        public string CvSummary { get; set; } = string.Empty;

        /// <summary>
        /// [IT-ROLES] Số chỗ có thể dành cho câu kỹ thuật AI soạn theo vị trí (số câu của buổi trừ câu giới thiệu);
        /// 0 = không soạn (vị trí đã có bộ đề, hoặc người phỏng vấn không phải Nam). Pipeline trừ chỗ cho câu CV và kẹp theo cấu hình.
        /// </summary>
        public int RoleQuestionCount { get; set; }

        /// <summary>Câu bộ đề đã chọn cho buổi, để tránh trùng.</summary>
        public List<string> ExistingQuestions { get; set; } = new();
    }
}
