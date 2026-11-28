using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MockviewAI.Models.Entities
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Câu hỏi chính do AI soạn theo CV (Gemini soạn, Groq duyệt), lưu lâu dài để chấm điểm và xem lại buổi cũ.
    /// Chỉ lưu câu hỏi + đáp án đã soạn, KHÔNG lưu CV gốc. Xoá buổi thì xoá theo (cascade).
    /// </summary>
    public class CvGeneratedQuestion
    {
        /// <summary>Mã câu dạng cv-{sessionId}-{n}, trùng SessionAnswer.QuestionId.</summary>
        [Key, MaxLength(40)]
        public string Id { get; set; } = string.Empty;

        public int SessionId { get; set; }
        public InterviewSession? Session { get; set; }

        /// <summary>BankQuestion dạng JSON (ý cốt lõi, con đường đúng, lỗi hay gặp).</summary>
        [Required]
        public string PayloadJson { get; set; } = string.Empty;

        /// <summary>pass = đạt ngay; fixed = đạt sau 1 lần sửa.</summary>
        [Required, MaxLength(10)]
        public string ReviewVerdict { get; set; } = string.Empty;

        /// <summary>Lý do người kiểm duyệt yêu cầu sửa (nếu có), để nhóm xem lại / làm số liệu.</summary>
        [MaxLength(1000)]
        public string? ReviewNotes { get; set; }

        /// <summary>Model soạn câu (Gemini).</summary>
        [MaxLength(60)]
        public string GeneratorModel { get; set; } = string.Empty;

        /// <summary>Model kiểm duyệt (Groq).</summary>
        [MaxLength(60)]
        public string ReviewerModel { get; set; } = string.Empty;

        [Precision(3)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
