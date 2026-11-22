namespace MockviewAI.Models.Ai
{
    /// <summary>
    /// [AI-SCORING] 
    /// Buổi luyện được chấm bằng cách nào; lưu vào InterviewSession.ScoringMode để phân tích dữ liệu nghiên cứu.
    /// </summary>
    public static class ScoringModes
    {
        /// <summary>Gemini chấm nội dung và xếp loại 4 nhãn.</summary>
        public const string Ai = "ai";

        /// <summary>AI lỗi: chấm sơ bộ theo bộ đề bằng code, không cần AI.</summary>
        public const string BankRules = "bank_rules";

        /// <summary>AI lỗi và câu hỏi kiểu cũ (không có bộ đề): chỉ tính cách nói.</summary>
        public const string DeliveryOnly = "delivery_only";
    }
}
