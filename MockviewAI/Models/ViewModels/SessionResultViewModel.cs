namespace MockviewAI.Models.ViewModels
{
    /// <summary>
    /// [AI-INTERVIEW] 
    /// Dữ liệu trang Kết quả (Views/Interview/Result.cshtml).
    /// </summary>
    public class SessionResultViewModel
    {
        #region Thông tin buổi
        public int SessionId { get; set; }
        public string Position { get; set; } = string.Empty;
        public string InterviewerName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }

        /// <summary>Số câu chính của buổi.</summary>
        public int QuestionCount { get; set; }

        /// <summary>[LIVE-MODE] Buổi chạy ở chế độ Thực chiến.</summary>
        public bool IsLive { get; set; }

        /// <summary>Người dùng đã yêu cầu gửi kết quả qua email.</summary>
        public bool EmailRequested { get; set; }
        #endregion

        #region Điểm
        /// <summary>false = buổi chưa có điểm.</summary>
        public bool HasScore { get; set; }

        /// <summary>false = AI lỗi, kết quả dự phòng chỉ tính cách nói.</summary>
        public bool HasContentScore { get; set; }

        /// <summary>[QUESTION-BANK] true = AI lỗi, phần nội dung được chấm sơ bộ theo bộ đề.</summary>
        public bool IsPreliminaryScore { get; set; }

        public int TotalScore { get; set; }
        public int ContentScore { get; set; }
        public int DeliveryScore { get; set; }

        /// <summary>Số câu trả lời vô nghĩa (tính sai, 0 điểm).</summary>
        public int InvalidAnswerCount { get; set; }
        #endregion

        #region Dấu hiệu lo lắng
        /// <summary>1–10 do hệ thống ước tính từ cách nói; 0 = chưa đủ dữ liệu.</summary>
        public int AnxietyIndicator { get; set; }

        /// <summary>Chỉ số của buổi trước để so sánh; 0 = chưa có.</summary>
        public int PreviousAnxietyIndicator { get; set; }

        /// <summary>Thấp (1–3) / Trung bình (4–6) / Cao (7–10); rỗng nếu chưa có.</summary>
        public string AnxietyLevelText => AnxietyIndicator switch
        {
            <= 0 => string.Empty,
            <= 3 => "Thấp",
            <= 6 => "Trung bình",
            _ => "Cao"
        };
        #endregion

        #region [QUESTION-BANK] Xếp loại (chỉ có với buổi dùng bộ đề)
        public int CorrectCount { get; set; }
        public int AlternativePathCount { get; set; }
        public int SpecialCount { get; set; }
        public int WrongCount { get; set; }

        public bool HasCategories => CorrectCount + AlternativePathCount + SpecialCount + WrongCount > 0;

        /// <summary>Nhãn từng câu theo thứ tự (null = bỏ qua / chưa xếp loại), dùng vẽ dải băng.</summary>
        public List<string?> AnswerCategories { get; set; } = new();

        /// <summary>[FOLLOW-UP] Số câu hỏi thêm đã trả lời trong buổi.</summary>
        public int FollowUpCount { get; set; }
        #endregion

        #region [VOICE] Giọng nói cả buổi (trung bình các câu nói bằng micro)
        public bool HasVoice { get; set; }

        /// <summary>Nhỏ / Vừa / Quá to; "—" nếu không có số đo.</summary>
        public string VoiceLevelLabel { get; set; } = "—";

        /// <summary>Phần trăm thời gian nói nhỏ.</summary>
        public int? QuietPercent { get; set; }

        /// <summary>Đều đều / Tự nhiên / Nhấn nhá nhiều; "—" nếu không có số đo.</summary>
        public string ToneLabel { get; set; } = "—";
        #endregion

        #region Nhận xét
        public string AiSummary { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new();
        public List<string> Improvements { get; set; } = new();
        public List<string> DeliveryTips { get; set; } = new();
        #endregion
    }
}
