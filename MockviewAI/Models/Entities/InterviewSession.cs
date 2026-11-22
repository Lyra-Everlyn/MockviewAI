using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MockviewAI.Models.Entities
{
    /// <summary>
    /// [AI-INTERVIEW] 
    /// Một buổi luyện phỏng vấn của một người dùng.
    /// Điểm và nhận xét chỉ có sau khi buổi được chấm (Status = Completed).
    /// </summary>
    public class InterviewSession
    {
        #region Thiết lập buổi
        [Key]
        public int Id { get; set; }

        /// <summary>Người luyện.</summary>
        public int UserId { get; set; }
        public User? User { get; set; }

        /// <summary>Tên vị trí hiển thị, vd "Lập trình viên".</summary>
        [Required, MaxLength(100)]
        public string Position { get; set; } = string.Empty;

        /// <summary>Mã người phỏng vấn: linh | nam | an.</summary>
        [Required, MaxLength(20)]
        public string Interviewer { get; set; } = "linh";

        /// <summary>Số câu chính của buổi (không tính câu hỏi thêm).</summary>
        public int QuestionCount { get; set; }

        /// <summary>[LIVE-MODE] practice (Luyện tập) | live (Thực chiến); null = buổi cũ, coi là Luyện tập.</summary>
        [MaxLength(20)]
        public string? Mode { get; set; }

        /// <summary>Người dùng tick "Gửi kết quả qua email" ở trang Tạo buổi.</summary>
        public bool EmailResult { get; set; }

        /// <summary>[QUESTION-BANK] Phiên bản bộ đề đã dùng; null = buổi cũ, câu hỏi kiểu cũ.</summary>
        [MaxLength(30)]
        public string? QuestionBankVersion { get; set; }
        #endregion

        #region Trạng thái và thời gian
        /// <summary>Xem <see cref="SessionStatus"/>.</summary>
        [Required, MaxLength(20)]
        public string Status { get; set; } = SessionStatus.InProgress;

        [Precision(3)]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        [Precision(3)]
        public DateTime? FinishedAt { get; set; }
        #endregion

        #region Kết quả chấm
        /// <summary>Điểm tổng 0–100 (điểm tham khảo); null = chưa chấm.</summary>
        public int? TotalScore { get; set; }

        /// <summary>Điểm nội dung 0–100.</summary>
        public int? ContentScore { get; set; }

        /// <summary>Điểm cách nói 0–100.</summary>
        public int? DeliveryScore { get; set; }

        /// <summary>[AI-SCORING] Cách chấm: ai | bank_rules | delivery_only (<see cref="Ai.ScoringModes"/>); null = buổi cũ.</summary>
        [MaxLength(20)]
        public string? ScoringMode { get; set; }

        /// <summary>Nhận xét tổng quát của AI.</summary>
        public string? AiSummary { get; set; }

        /// <summary>Điểm mạnh, mỗi ý một dòng.</summary>
        public string? Strengths { get; set; }

        /// <summary>Điểm cần cải thiện, mỗi ý một dòng.</summary>
        public string? Improvements { get; set; }

        /// <summary>Lời khuyên cách nói, mỗi ý một dòng.</summary>
        public string? DeliveryTips { get; set; }

        /// <summary>Dấu hiệu lo lắng 1–10 hệ thống ước tính từ cách nói; null = chưa đủ dữ liệu.</summary>
        public int? AnxietyIndicator { get; set; }
        #endregion

        #region [KHÔNG DÙNG] Cột cũ giữ lại để không mất dữ liệu
        /// <summary>[KHÔNG DÙNG] Mức lo lắng tự đánh giá trước buổi (1–10); người dùng không còn tự chấm.</summary>
        public int? AnxietyBefore { get; set; }

        /// <summary>[KHÔNG DÙNG] Mức lo lắng tự đánh giá sau buổi (1–10).</summary>
        public int? AnxietyAfter { get; set; }
        #endregion

        /// <summary>Các câu hỏi của buổi, gồm cả câu hỏi thêm.</summary>
        public List<SessionAnswer> Answers { get; set; } = new();
    }
}
