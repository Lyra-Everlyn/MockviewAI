// [QUESTION-BANK] Một ý cốt lõi (một phần của "điểm B") mà câu trả lời cần chạm tới.

namespace MockviewAI.Models.InterviewQuestions
{
    public class CorePoint
    {
        public string Id { get; set; } = string.Empty;     // c1, c2, ...
        public string Text { get; set; } = string.Empty;

        // Chấm SƠ BỘ khi AI lỗi: ý được coi là đạt khi câu trả lời có ít nhất MinHits từ khoá khác nhau trong danh sách
        public List<string> Keywords { get; set; } = new();
        public int MinHits { get; set; } = 1;

        // [FOLLOW-UP] Đợt 8: câu hỏi nối tiếp soạn sẵn - hỏi thêm khi câu trả lời chưa đạt ý này.
        // Gợi hướng nhưng không chứa từ khoá của ý (test QuestionBankFileTests kiểm tra).
        public string? FollowUp { get; set; }
    }
}
