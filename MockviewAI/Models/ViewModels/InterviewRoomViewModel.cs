// [AI-INTERVIEW] Dữ liệu để vẽ giao diện (ViewModel). Mỗi file một lớp.

namespace MockviewAI.Models.ViewModels
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
}
