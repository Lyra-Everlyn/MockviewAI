using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MockviewAI.Models.Entities
{
    /// <summary>
    /// [AI-INTERVIEW] 
    /// Một câu hỏi trong buổi luyện và câu trả lời của người dùng.
    /// Câu chính được tạo sẵn lúc bắt đầu buổi (Answer trống, AnsweredAt = null); câu hỏi thêm được chèn khi cần.
    /// </summary>
    public class SessionAnswer
    {
        #region Câu hỏi
        [Key]
        public int Id { get; set; }

        public int SessionId { get; set; }
        public InterviewSession? Session { get; set; }

        /// <summary>Số thứ tự câu (1, 2, 3...); câu hỏi thêm mang cùng số với câu chính.</summary>
        public int OrderNo { get; set; }

        /// <summary>Nội dung câu hỏi tại thời điểm hỏi.</summary>
        [Required, MaxLength(500)]
        public string Question { get; set; } = string.Empty;

        /// <summary>[QUESTION-BANK] Mã câu trong bộ đề; null = câu hỏi kiểu cũ.</summary>
        [MaxLength(40)]
        public string? QuestionId { get; set; }
        #endregion

        #region Câu trả lời
        /// <summary>Chữ từ giọng nói hoặc gõ tay.</summary>
        [MaxLength(4000)]
        public string? Answer { get; set; }

        /// <summary>Người dùng bấm Bỏ qua.</summary>
        public bool Skipped { get; set; }

        /// <summary>Số lần bị từ chối vì quá ngắn / lạc đề / vô nghĩa (tối đa 2, lần 3 tính là bỏ qua).</summary>
        public int Attempts { get; set; }

        /// <summary>Lúc trả lời hoặc bỏ qua; null = câu đang chờ.</summary>
        [Precision(3)]
        public DateTime? AnsweredAt { get; set; }
        #endregion

        #region [VOICE] Số đo cách nói (null = gõ tay, bỏ qua hoặc buổi cũ)
        /// <summary>Thời gian nói (giây).</summary>
        public double? DurationSeconds { get; set; }

        /// <summary>Khoảng lặng dài nhất (giây), dùng chấm tiêu chí "khoảng lặng".</summary>
        public double? LongestPauseSeconds { get; set; }

        /// <summary>Âm lượng trung vị khi nói (dBFS, số âm). Xem <see cref="VoiceMetrics"/>.</summary>
        public double? VoiceLevelDb { get; set; }

        /// <summary>Tỉ lệ 0–1 thời gian nói nhỏ hơn mức dễ nghe.</summary>
        public double? QuietRatio { get; set; }

        /// <summary>Độ lên xuống giọng (nửa cung); dưới 1.5 = đều đều.</summary>
        public double? PitchVariation { get; set; }
        #endregion

        #region [QUESTION-BANK] Xếp loại sau khi chấm
        /// <summary>Nhãn: dung | hoi_dac_biet | dac_biet | sai (<see cref="Ai.AnswerCategory"/>).</summary>
        [MaxLength(20)]
        public string? Category { get; set; }

        /// <summary>Con đường khớp: chuan / vong1... / loi1... / ngoai_bo.</summary>
        [MaxLength(40)]
        public string? MatchedPath { get; set; }

        /// <summary>Mã ý cốt lõi đã đạt, ngăn bằng dấu phẩy, vd "c1,c3".</summary>
        [MaxLength(100)]
        public string? CorePointsMet { get; set; }

        /// <summary>Mã ý cốt lõi còn thiếu, cùng định dạng.</summary>
        [MaxLength(100)]
        public string? CorePointsMissing { get; set; }

        /// <summary>Nhận xét ngắn cho câu này.</summary>
        [MaxLength(1000)]
        public string? Feedback { get; set; }
        #endregion

        #region [FOLLOW-UP] Câu hỏi thêm
        /// <summary>Mã câu bộ đề mà câu này hỏi thêm; null = câu chính.</summary>
        [MaxLength(40)]
        public string? FollowUpOf { get; set; }

        /// <summary>Ý cốt lõi được hỏi thêm (c1, c2...); "cv" = câu hỏi theo CV của người dùng.</summary>
        [MaxLength(10)]
        public string? FollowUpPointId { get; set; }

        /// <summary>Câu hỏi thêm, không tính vào số câu của buổi.</summary>
        [NotMapped]
        public bool IsFollowUp => FollowUpOf != null;
        #endregion
    }
}
