namespace MockviewAI.Models.DTOs
{
    /// [AI-INTERVIEW] 
    // -- Người dùng gửi lên khi trả lời hoặc bỏ qua một câu
    public class SubmitAnswerDto
    {
        public int SessionId { get; set; }
        public string? Answer { get; set; }
        public double? DurationSeconds { get; set; }
        public bool Skipped { get; set; }

        // Các khoảng lặng (giây) trình duyệt đo được khi người dùng nói. Null nếu gõ tay.
        public List<double>? PausesSeconds { get; set; }

        // [VOICE] Đợt 7: số đo giọng nói (null = gõ chữ / không đo được)
        public double? VoiceLevelDb { get; set; }     // mức to nhỏ trung vị khi đang nói (dBFS, âm)
        public double? QuietRatio { get; set; }       // 0-1: phần thời gian nói nhỏ hơn mức dễ nghe
        public double? PitchVariation { get; set; }   // độ lên xuống giọng (nửa cung); < 1.5 = đều đều
    }
}
