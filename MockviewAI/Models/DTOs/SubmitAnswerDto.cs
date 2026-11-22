namespace MockviewAI.Models.DTOs
{
    /// <summary>
    /// [AI-INTERVIEW] 
    /// Trình duyệt gửi lên khi trả lời hoặc bỏ qua một câu (POST /Interview/Answer).
    /// </summary>
    public class SubmitAnswerDto
    {
        #region Câu trả lời
        public int SessionId { get; set; }

        /// <summary>Chữ đã chuyển từ giọng nói hoặc gõ tay; rỗng khi bỏ qua.</summary>
        public string? Answer { get; set; }

        /// <summary>true = người dùng bấm Bỏ qua.</summary>
        public bool Skipped { get; set; }
        #endregion

        #region [VOICE] Số đo cách nói (null = gõ tay / không đo được)
        /// <summary>Thời gian nói (giây).</summary>
        public double? DurationSeconds { get; set; }

        /// <summary>Các khoảng lặng (giây) trình duyệt đo được khi người dùng nói.</summary>
        public List<double>? PausesSeconds { get; set; }

        /// <summary>Âm lượng trung vị khi nói (dBFS, số âm).</summary>
        public double? VoiceLevelDb { get; set; }

        /// <summary>Tỉ lệ 0–1 thời gian nói nhỏ hơn mức dễ nghe.</summary>
        public double? QuietRatio { get; set; }

        /// <summary>Độ lên xuống giọng (nửa cung); dưới 1.5 = đều đều.</summary>
        public double? PitchVariation { get; set; }
        #endregion
    }
}
