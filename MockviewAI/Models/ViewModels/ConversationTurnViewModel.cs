namespace MockviewAI.Models.ViewModels
{
    /// <summary>
    /// [HISTORY] 
    /// Một lượt hỏi – đáp trong trang Xem lại hội thoại.
    /// </summary>
    public class ConversationTurnViewModel
    {
        #region Hỏi – đáp
        /// <summary>Số câu.</summary>
        public int Number { get; set; }

        public string Question { get; set; } = string.Empty;
        public string? Answer { get; set; }
        public bool Skipped { get; set; }

        /// <summary>Lý do câu trả lời bị tính sai (vô nghĩa); null = hợp lệ.</summary>
        public string? InvalidReason { get; set; }

        /// <summary>[VOICE] Vd "Âm lượng: Vừa · nói nhỏ 10% thời gian · ngữ điệu: Tự nhiên"; null = gõ chữ / không đo.</summary>
        public string? VoiceText { get; set; }
        #endregion

        #region [QUESTION-BANK] Xếp loại (null = câu kiểu cũ hoặc chưa chấm)
        /// <summary>Mã nhãn, xem AnswerCategory.</summary>
        public string? Category { get; set; }

        public string CategoryName { get; set; } = string.Empty;
        public string CategoryIcon { get; set; } = string.Empty;
        public string CategoryDescription { get; set; } = string.Empty;

        /// <summary>"Hướng trả lời: ..." hoặc "Lỗi hay gặp: ...".</summary>
        public string? MatchedPathText { get; set; }

        /// <summary>Nội dung các ý đã đạt.</summary>
        public List<string> MetPoints { get; set; } = new();

        /// <summary>Nội dung các ý còn thiếu.</summary>
        public List<string> MissingPoints { get; set; } = new();

        /// <summary>Nhận xét ngắn cho câu.</summary>
        public string? Feedback { get; set; }
        #endregion

        #region [FOLLOW-UP] Câu hỏi thêm
        /// <summary>Lượt hỏi thêm sau câu này; null = không hỏi thêm. Nhãn và ý đạt / thiếu ở câu chính đã tính cả câu trả lời thêm.</summary>
        public ConversationTurnViewModel? FollowUp { get; set; }

        /// <summary>Ý cốt lõi mà người phỏng vấn hỏi thêm.</summary>
        public string? FollowUpPointText { get; set; }
        #endregion
    }
}
