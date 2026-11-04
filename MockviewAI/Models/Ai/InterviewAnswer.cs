// [AI-MODULE] Lớp dữ liệu vào/ra của module AI. Mỗi file một lớp.

namespace MockviewAI.Models.Ai
{
    // One question + the candidate's answer (text from speech-to-text or typed)
    public class InterviewAnswer
    {
        // [QUESTION-BANK] Mã câu trong bộ đề (null = câu hỏi kiểu cũ, không có đáp án để so)
        public string? QuestionId { get; set; }

        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;

        // How long the candidate spoke (seconds). Null when the answer was typed.
        public double? DurationSeconds { get; set; }

        // Silent gaps inside the answer, in seconds (optional, from audio analysis)
        public List<double> PausesSeconds { get; set; } = new();

        // [VOICE] Đợt 7: số đo giọng nói (null = gõ chữ / không đo được)
        public double? VoiceLevelDb { get; set; }     // mức to nhỏ trung vị khi đang nói (dBFS, âm)
        public double? QuietRatio { get; set; }       // 0-1: phần thời gian nói nhỏ hơn mức dễ nghe
        public double? PitchVariation { get; set; }   // độ lên xuống giọng (nửa cung); < 1.5 = đều đều

        // [FOLLOW-UP] Đợt 8: người phỏng vấn hỏi thêm và câu trả lời thêm (null = không hỏi thêm / bỏ qua).
        // Nội dung câu được chấm trên CẢ câu trả lời đầu lẫn câu trả lời thêm.
        public string? FollowUpQuestion { get; set; }
        public string? FollowUpAnswer { get; set; }

        // Văn bản dùng để chấm nội dung (câu trả lời đầu + câu trả lời thêm, KHÔNG gồm câu hỏi thêm)
        public string ContentText => string.IsNullOrWhiteSpace(FollowUpAnswer) ? Answer : Answer + "\n" + FollowUpAnswer;
    }
}
