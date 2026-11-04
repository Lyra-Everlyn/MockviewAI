namespace MockviewAI.Models.ViewModels
{
    /// [AI-INTERVIEW]
    // -- Dữ liệu trang kết quả (Result.cshtml).
    public class SessionResultViewModel
    {
        public int SessionId { get; set; }
        // Số câu trả lời không có ý nghĩa (tính sai, 0 điểm)
        public int InvalidAnswerCount { get; set; }
        public string Position { get; set; } = string.Empty;
        public string InterviewerName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public int QuestionCount { get; set; }

        public bool HasScore { get; set; }
        public bool HasContentScore { get; set; }    // false: AI lỗi, đây là kết quả dự phòng chỉ tính cách nói
        public int TotalScore { get; set; }
        public int ContentScore { get; set; }
        public int DeliveryScore { get; set; }

        // Dấu hiệu lo lắng 1-10 do hệ thống ước tính từ cách nói. 0 = chưa đủ dữ liệu.
        public int AnxietyIndicator { get; set; }
        public int PreviousAnxietyIndicator { get; set; }   // buổi trước (0 = chưa có)
        public string AnxietyLevelText => AnxietyIndicator switch
        {
            <= 0 => string.Empty,
            <= 3 => "Thấp",
            <= 6 => "Trung bình",
            _ => "Cao"
        };

        public bool EmailRequested { get; set; }

        // [QUESTION-BANK] Số câu theo 4 loại (chỉ có với buổi dùng bộ đề)
        public int CorrectCount { get; set; }
        public int AlternativePathCount { get; set; }
        public int SpecialCount { get; set; }
        public int WrongCount { get; set; }
        public bool HasCategories => CorrectCount + AlternativePathCount + SpecialCount + WrongCount > 0;
        // [UI-DARK] Nhãn từng câu theo thứ tự câu hỏi (null = câu bỏ qua / chưa xếp loại) -> vẽ "dải băng" ở trang Kết quả
        public List<string?> AnswerCategories { get; set; } = new();

        // [QUESTION-BANK] true = AI lỗi nên phần nội dung được chấm sơ bộ theo bộ đề (không dùng AI)
        public bool IsPreliminaryScore { get; set; }

        // [LIVE-MODE] Buổi chạy ở chế độ Thực chiến
        public bool IsLive { get; set; }

        public string AiSummary { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new();
        public List<string> Improvements { get; set; } = new();
        public List<string> DeliveryTips { get; set; } = new();
    }
}
