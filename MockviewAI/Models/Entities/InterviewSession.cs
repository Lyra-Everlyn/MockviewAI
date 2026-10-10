// [AI-INTERVIEW] Một buổi luyện phỏng vấn của một người dùng.
// Điểm (Total/Content/Delivery) chỉ có sau khi bấm "Dừng và xem kết quả" hoặc trả lời hết câu.

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MockviewAI.Models.Entities
{
    public class InterviewSession
    {
        [Key]
        public int Id { get; set; }

        // Người luyện
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required, MaxLength(100)]
        public string Position { get; set; } = string.Empty;       // vd: "Lập trình viên"

        [Required, MaxLength(20)]
        public string Interviewer { get; set; } = "linh";          // linh | nam | an

        public int QuestionCount { get; set; }

        // InProgress = đang làm, Completed = đã chấm điểm, Stopped = dừng khi chưa trả lời câu nào
        [Required, MaxLength(20)]
        public string Status { get; set; } = "InProgress";

        [Precision(3)]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        [Precision(3)]
        public DateTime? FinishedAt { get; set; }

        // Điểm 0-100 (điểm tham khảo). Null nếu chưa chấm.
        public int? TotalScore { get; set; }
        public int? ContentScore { get; set; }
        public int? DeliveryScore { get; set; }

        // Nhận xét của AI và lời khuyên cách nói. Nhiều ý được ngăn cách bằng dấu xuống dòng.
        public string? AiSummary { get; set; }
        public string? Strengths { get; set; }
        public string? Improvements { get; set; }
        public string? DeliveryTips { get; set; }

        // Mức lo lắng tự đánh giá 1-10 (phục vụ nghiên cứu)
        public int? AnxietyBefore { get; set; }
        public int? AnxietyAfter { get; set; }

        public List<SessionAnswer> Answers { get; set; } = new();
    }
}
