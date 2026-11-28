using MockviewAI.Models.InterviewQuestions;

namespace MockviewAI.Models.Ai
{
    /// <summary>
    /// [AI-SCORING] 
    /// Dữ liệu đầu vào để chấm cả một buổi luyện.
    /// </summary>
    public class ScoreRequest
    {
        /// <summary>Tên vị trí, vd "Lập trình viên".</summary>
        public string Position { get; set; } = string.Empty;

        /// <summary>Câu trả lời hợp lệ, gửi AI chấm nội dung.</summary>
        public List<InterviewAnswer> Answers { get; set; } = new();

        /// <summary>Câu vô nghĩa (tính SAI): không gửi AI, chỉ dùng đo cách nói và hạ điểm nội dung theo tỉ lệ.</summary>
        public List<InterviewAnswer> InvalidAnswers { get; set; } = new();

        /// <summary>[FOLLOW-UP] Câu trả lời cho câu hỏi thêm, chỉ dùng đo cách nói (nội dung đã gộp vào câu chính).</summary>
        public List<InterviewAnswer> FollowUpAnswers { get; set; } = new();

        /// <summary>Tổng số câu của buổi, dùng ước tính dấu hiệu lo lắng.</summary>
        public int QuestionCount { get; set; }

        /// <summary>Số câu bị bỏ qua, dùng ước tính dấu hiệu lo lắng.</summary>
        public int SkippedCount { get; set; }

        /// <summary>[LIVE-MODE] Mã câu bộ đề bị bỏ qua ở chế độ Thực chiến, tính là SAI (null = câu kiểu cũ). Luyện tập: rỗng.</summary>
        public List<string?> SkippedAsWrong { get; set; } = new();

        /// <summary>[CV-QUESTIONS] Câu hỏi AI soạn theo CV của buổi (mã cv-...), để bộ chấm tìm đáp án như câu bộ đề.</summary>
        public List<BankQuestion> CvQuestions { get; set; } = new();
    }
}
