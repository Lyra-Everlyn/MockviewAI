// [QUESTION-BANK] Buổi luyện được chấm bằng cách nào (lưu vào InterviewSession.ScoringMode để phân tích dữ liệu nghiên cứu).

namespace MockviewAI.Models.Ai
{
    public static class ScoringModes
    {
        public const string Ai = "ai";                       // Gemini chấm nội dung + xếp loại 4 nhãn
        public const string BankRules = "bank_rules";        // AI lỗi -> chấm SƠ BỘ theo bộ đề bằng code (không cần AI)
        public const string DeliveryOnly = "delivery_only";  // AI lỗi và câu hỏi kiểu cũ (không có bộ đề) -> chỉ tính cách nói
    }
}
