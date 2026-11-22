namespace MockviewAI.Models.Ai
{
    /// <summary>
    /// [AI-SCORING] 
    /// Điểm nội dung do AI chấm: 4 tiêu chí × 25 điểm = 100.
    /// </summary>
    public class ContentScoreResult
    {
        #region Tiêu chí (0–25 mỗi tiêu chí)
        /// <summary>Trả lời đúng câu hỏi.</summary>
        public int Relevance { get; set; }

        /// <summary>Bố cục, mạch lạc.</summary>
        public int Structure { get; set; }

        /// <summary>Ví dụ, số liệu cụ thể.</summary>
        public int Specificity { get; set; }

        /// <summary>Rõ ràng, dễ hiểu.</summary>
        public int Clarity { get; set; }

        /// <summary>Tỉ lệ câu hợp lệ / tổng câu đã trả lời (câu vô nghĩa = 0 điểm). 1 = không ảnh hưởng.</summary>
        public double ValidAnswerRatio { get; set; } = 1.0;

        /// <summary>Tổng 4 tiêu chí nhân tỉ lệ câu hợp lệ.</summary>
        public int Total => (int)Math.Round((Relevance + Structure + Specificity + Clarity) * ValidAnswerRatio);
        #endregion

        #region Nhận xét
        /// <summary>Nhận xét tổng quát.</summary>
        public string Summary { get; set; } = string.Empty;

        public List<string> Strengths { get; set; } = new();
        public List<string> Improvements { get; set; } = new();

        /// <summary>[QUESTION-BANK] Xếp loại từng câu thuộc bộ đề, đã qua AnswerVerdictValidator.</summary>
        public List<AnswerVerdict> Verdicts { get; set; } = new();
        #endregion
    }
}
