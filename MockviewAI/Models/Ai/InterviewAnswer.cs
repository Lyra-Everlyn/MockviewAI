namespace MockviewAI.Models.Ai
{
    /// <summary>
    /// [AI-SCORING] 
    /// Một câu hỏi và câu trả lời của ứng viên (chữ từ giọng nói hoặc gõ tay), đưa vào bộ chấm.
    /// </summary>
    public class InterviewAnswer
    {
        #region Nội dung
        /// <summary>[QUESTION-BANK] Mã câu trong bộ đề; null = câu kiểu cũ, không có đáp án để so.</summary>
        public string? QuestionId { get; set; }

        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;

        /// <summary>[FOLLOW-UP] Câu hỏi thêm của người phỏng vấn; null = không hỏi thêm.</summary>
        public string? FollowUpQuestion { get; set; }

        /// <summary>[FOLLOW-UP] Câu trả lời thêm; null = không hỏi thêm hoặc bị bỏ qua.</summary>
        public string? FollowUpAnswer { get; set; }

        /// <summary>Văn bản chấm nội dung: câu trả lời đầu + câu trả lời thêm (không gồm câu hỏi thêm).</summary>
        public string ContentText => string.IsNullOrWhiteSpace(FollowUpAnswer) ? Answer : Answer + "\n" + FollowUpAnswer;
        #endregion

        #region [VOICE] Số đo cách nói (null = gõ tay / không đo được)
        /// <summary>Thời gian nói (giây).</summary>
        public double? DurationSeconds { get; set; }

        /// <summary>Các khoảng lặng trong câu trả lời (giây).</summary>
        public List<double> PausesSeconds { get; set; } = new();

        /// <summary>Âm lượng trung vị khi nói (dBFS, số âm).</summary>
        public double? VoiceLevelDb { get; set; }

        /// <summary>Tỉ lệ 0–1 thời gian nói nhỏ hơn mức dễ nghe.</summary>
        public double? QuietRatio { get; set; }

        /// <summary>Độ lên xuống giọng (nửa cung); dưới 1.5 = đều đều.</summary>
        public double? PitchVariation { get; set; }
        #endregion
    }
}
