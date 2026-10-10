// [AI-INTERVIEW] Một câu hỏi trong buổi luyện và câu trả lời của người dùng.
// Câu hỏi được tạo sẵn lúc bắt đầu buổi (Answer còn trống, AnsweredAt = null).

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MockviewAI.Models.Entities
{
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

        public bool Skipped { get; set; }

        // Null = chưa trả lời, cũng chưa bỏ qua
        [Precision(3)]
        public DateTime? AnsweredAt { get; set; }
    }
}
