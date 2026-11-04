namespace MockviewAI.Models.ViewModels
{
    // Một lượt hỏi - đáp trong trang Xem lại hội thoại.
    public class ConversationTurnViewModel
    {
        public int Number { get; set; }
        public string Question { get; set; } = string.Empty;
        public string? Answer { get; set; }
        public bool Skipped { get; set; }
        // Lý do câu trả lời bị tính sai (không có ý nghĩa); null = hợp lệ
        public string? InvalidReason { get; set; }

        // [QUESTION-BANK] Kết quả xếp loại 4 nhãn (null = câu kiểu cũ hoặc chưa chấm)
        public string? Category { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryIcon { get; set; } = string.Empty;
        public string CategoryDescription { get; set; } = string.Empty;
        public string? MatchedPathText { get; set; }          // "Hướng trả lời: ..." hoặc "Lỗi hay gặp: ..."
        public List<string> MetPoints { get; set; } = new();       // nội dung các ý đã đạt
        public List<string> MissingPoints { get; set; } = new();   // nội dung các ý còn thiếu
        public string? Feedback { get; set; }
        // [VOICE] Đợt 7: "Âm lượng: Vừa · nói nhỏ 10% thời gian · ngữ điệu: Tự nhiên" (null = gõ chữ / không đo)
        public string? VoiceText { get; set; }

        // [FOLLOW-UP] Đợt 8: câu hỏi thêm của người phỏng vấn sau câu này (null = không hỏi thêm).
        // Nhãn / ý đạt / ý thiếu ở câu chính đã tính CẢ câu trả lời thêm.
        public ConversationTurnViewModel? FollowUp { get; set; }
        public string? FollowUpPointText { get; set; }   // ý cốt lõi mà người phỏng vấn hỏi thêm
    }
}
