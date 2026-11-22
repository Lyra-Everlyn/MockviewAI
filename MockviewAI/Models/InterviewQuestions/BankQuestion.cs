namespace MockviewAI.Models.InterviewQuestions
{
    /// <summary>
    /// [QUESTION-BANK] 
    /// Một câu hỏi trong bộ đề kèm "điểm B" (ý cốt lõi), các con đường đúng, lỗi hay gặp và câu trả lời mẫu.
    /// </summary>
    public class BankQuestion
    {
        #region Thông tin câu hỏi
        /// <summary>Mã câu, vd "dev-csharp-01"; câu giới thiệu kết thúc bằng "-gen-01".</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Người phỏng vấn phụ trách câu: linh | nam | an.</summary>
        public string Interviewer { get; set; } = "nam";

        /// <summary>Vị trí dùng câu này: developer | tester | ba.</summary>
        public List<string> Positions { get; set; } = new();

        /// <summary>Chủ đề / kỹ năng của câu, để lọc và nối với CV.</summary>
        public List<string> Skills { get; set; } = new();

        /// <summary>Mức độ khó.</summary>
        public string Level { get; set; } = string.Empty;

        public string Question { get; set; } = string.Empty;

        /// <summary>Câu giới thiệu bản thân của vị trí, luôn là câu 1 của buổi.</summary>
        public bool IsIntro => Id.EndsWith("-gen-01", StringComparison.Ordinal);
        #endregion

        #region Đáp án
        /// <summary>Phải đạt ít nhất số ý cốt lõi này mới được coi là "tới điểm B".</summary>
        public int MinCorePoints { get; set; } = 2;

        /// <summary>Các ý cốt lõi (c1, c2...).</summary>
        public List<CorePoint> CorePoints { get; set; } = new();

        /// <summary>Các con đường đúng đến điểm B.</summary>
        public List<AnswerPath> Paths { get; set; } = new();

        /// <summary>Lỗi hay gặp, khớp lỗi thì xếp loại "sai".</summary>
        public List<CommonMistake> Mistakes { get; set; } = new();

        /// <summary>Câu trả lời mẫu, chỉ dùng kiểm chứng độ chính xác; KHÔNG gửi AI khi chấm thật.</summary>
        public List<SampleAnswer> Samples { get; set; } = new();
        #endregion
    }
}
