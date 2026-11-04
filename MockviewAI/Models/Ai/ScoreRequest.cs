// [AI-MODULE] Lớp dữ liệu vào/ra của module AI. Mỗi file một lớp.

namespace MockviewAI.Models.Ai
{
    // Input for scoring a whole practice session
    public class ScoreRequest
    {
        public string Position { get; set; } = string.Empty;   // e.g. "Lập trình viên"
        public List<InterviewAnswer> Answers { get; set; } = new();            // câu hợp lệ -> gửi AI chấm nội dung
        // Câu không có ý nghĩa (tính SAI): không gửi AI, chỉ dùng để đo cách nói và hạ điểm nội dung theo tỉ lệ
        public List<InterviewAnswer> InvalidAnswers { get; set; } = new();

        // Dùng để ước tính dấu hiệu lo lắng: tổng số câu của buổi và số câu bị bỏ qua
        public int QuestionCount { get; set; }
        public int SkippedCount { get; set; }
        // [LIVE-MODE] Câu BỎ QUA ở chế độ Thực chiến: tính là SAI (mã câu bộ đề; null = câu kiểu cũ). Luyện tập: để trống.
        public List<string?> SkippedAsWrong { get; set; } = new();
    }
}
