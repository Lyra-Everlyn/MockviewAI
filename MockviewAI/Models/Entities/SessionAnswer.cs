using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MockviewAI.Models.Entities
{
    /// <summary>
    /// [AI-INTERVIEW] 
    /// -- Một câu hỏi trong buổi luyện và câu trả lời của người dùng.
    /// -- Câu hỏi được tạo sẵn lúc bắt đầu buổi (Answer còn trống, AnsweredAt = null).
    /// </summary>
    
    public class SessionAnswer
    {
        [Key]
        public int Id { get; set; }

        public int SessionId { get; set; }
        public InterviewSession? Session { get; set; }

        // Thứ tự câu hỏi: 1, 2, 3, ...
        public int OrderNo { get; set; }

        [Required, MaxLength(500)]
        public string Question { get; set; } = string.Empty;

        // Chữ từ giọng nói hoặc gõ tay
        [MaxLength(4000)]
        public string? Answer { get; set; }

        // Thời gian nói (giây). Null nếu gõ tay.
        public double? DurationSeconds { get; set; }

        // Khoảng lặng dài nhất khi nói (giây). Null nếu gõ tay. Dùng để chấm điểm "khoảng lặng".
        public double? LongestPauseSeconds { get; set; }

        // [VOICE] Đợt 7: âm lượng + giọng điệu đo trên trình duyệt (null = gõ chữ / bỏ qua / buổi cũ)
        public double? VoiceLevelDb { get; set; }
        public double? QuietRatio { get; set; }
        public double? PitchVariation { get; set; }

        public bool Skipped { get; set; }

        // Số lần câu trả lời bị từ chối vì quá ngắn / lạc đề / vô nghĩa (tối đa 2 lần, lần 3 tính là bỏ qua)
        public int Attempts { get; set; }

        // Null = chưa trả lời, cũng chưa bỏ qua
        [Precision(3)]
        public DateTime? AnsweredAt { get; set; }

        // [QUESTION-BANK] Mã câu trong bộ đề (null = câu hỏi kiểu cũ) và kết quả xếp loại 4 loại sau khi chấm
        [MaxLength(40)]
        public string? QuestionId { get; set; }

        [MaxLength(20)]
        public string? Category { get; set; }            // AnswerCategory: dung | hoi_dac_biet | dac_biet | sai

        [MaxLength(40)]
        public string? MatchedPath { get; set; }         // chuan / vong1 / loi1 / ngoai_bo

        [MaxLength(100)]
        public string? CorePointsMet { get; set; }       // vd "c1,c3"

        [MaxLength(100)]
        public string? CorePointsMissing { get; set; }

        [MaxLength(1000)]
        public string? Feedback { get; set; }            // nhận xét ngắn cho câu này

        // [FOLLOW-UP] Đợt 8: câu hỏi NỐI TIẾP (người phỏng vấn hỏi thêm vì câu trả lời còn thiếu ý).
        // FollowUpOf = mã câu bộ đề được hỏi thêm (null = câu chính); FollowUpPointId = ý cốt lõi được hỏi thêm (c2...).
        // Câu hỏi thêm có cùng OrderNo với câu chính và đứng ngay sau nó; không tính vào số câu của buổi.
        [MaxLength(40)]
        public string? FollowUpOf { get; set; }

        [MaxLength(10)]
        public string? FollowUpPointId { get; set; }

        [NotMapped]
        public bool IsFollowUp => FollowUpOf != null;
    }
}
