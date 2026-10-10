// [AI-INTERVIEW] Các lớp dữ liệu đi giữa giao diện và InterviewService.

namespace MockviewAI.Models.DTOs
{
    // Dữ liệu để vẽ phòng phỏng vấn (một câu hỏi hiện tại)
    public class InterviewRoomViewModel
    {
        public int SessionId { get; set; }
        public string Position { get; set; } = string.Empty;
        public string InterviewerName { get; set; } = string.Empty;     // vd: "Linh"
        public string InterviewerRole { get; set; } = string.Empty;     // vd: "Nhân sự"
        public int QuestionNumber { get; set; }                          // câu đang hỏi (bắt đầu từ 1)
        public int TotalQuestions { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public int AnsweredCount { get; set; }                           // số câu đã trả lời (không tính câu bỏ qua)

        public int ProgressPercent => TotalQuestions == 0 ? 0 : (QuestionNumber - 1) * 100 / TotalQuestions;
    }

    // Người dùng gửi lên khi trả lời hoặc bỏ qua một câu
    public class SubmitAnswerDto
    {
        public int SessionId { get; set; }
        public string? Answer { get; set; }
        public double? DurationSeconds { get; set; }
        public bool Skipped { get; set; }
    }

    // Kết quả sau khi gửi một câu: còn câu tiếp theo hay đã hết
    public class SubmitAnswerResult
    {
        public bool Done { get; set; }
        public int AnsweredCount { get; set; }
        public int? NextNumber { get; set; }
        public string? NextQuestion { get; set; }
    }

    // Người dùng gửi lên khi kết thúc buổi
    public class FinishSessionDto
    {
        public int SessionId { get; set; }
        public int? AnxietyAfter { get; set; }
    }

    // Dữ liệu trang kết quả. Tên thuộc tính khớp với Result.cshtml của giao diện:
    // TotalScore, ContentScore, DeliveryScore, AnxietyBefore, AnxietyAfter, SessionId.
    public class SessionResultViewModel
    {
        public int SessionId { get; set; }
        public string Position { get; set; } = string.Empty;
        public string InterviewerName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public int QuestionCount { get; set; }

        public bool HasScore { get; set; }                  // false nếu chưa trả lời câu nào
        public int TotalScore { get; set; }
        public int ContentScore { get; set; }
        public int DeliveryScore { get; set; }
        public int AnxietyBefore { get; set; }
        public int AnxietyAfter { get; set; }

        public string AiSummary { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new();
        public List<string> Improvements { get; set; } = new();
        public List<string> DeliveryTips { get; set; } = new();
    }
}
