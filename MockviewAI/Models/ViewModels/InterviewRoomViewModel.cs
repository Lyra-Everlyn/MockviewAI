namespace MockviewAI.Models.ViewModels
{
    /// <summary>
    /// [AI-INTERVIEW] 
    /// Dữ liệu vẽ phòng phỏng vấn cho câu hỏi hiện tại (Views/Home/InterviewRoom.cshtml).
    /// </summary>
    public class InterviewRoomViewModel
    {
        #region Buổi và câu hỏi
        public int SessionId { get; set; }

        /// <summary>Tên vị trí hiển thị.</summary>
        public string Position { get; set; } = string.Empty;

        /// <summary>Tên người phỏng vấn, vd "Linh".</summary>
        public string InterviewerName { get; set; } = string.Empty;

        /// <summary>Vai trò người phỏng vấn, vd "Nhân sự".</summary>
        public string InterviewerRole { get; set; } = string.Empty;

        /// <summary>Số câu đang hỏi, bắt đầu từ 1.</summary>
        public int QuestionNumber { get; set; }

        /// <summary>Tổng số câu chính.</summary>
        public int TotalQuestions { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        /// <summary>Số câu đã trả lời (không tính câu bỏ qua).</summary>
        public int AnsweredCount { get; set; }

        /// <summary>[FOLLOW-UP] Câu đang hỏi là câu hỏi thêm.</summary>
        public bool IsFollowUp { get; set; }

        /// <summary>Phần trăm thanh tiến độ.</summary>
        public int ProgressPercent => TotalQuestions == 0 ? 0 : (QuestionNumber - 1) * 100 / TotalQuestions;
        #endregion

        #region [LIVE-MODE] Chế độ và luật Thực chiến (JS đọc qua data-*)
        /// <summary>practice | live.</summary>
        public string Mode { get; set; } = InterviewModes.Practice;

        public bool IsLive => InterviewModes.IsLive(Mode);
        public string ModeName => InterviewModes.DisplayName(Mode);
        public int ThinkSeconds => InterviewModes.LiveThinkSeconds;
        public int MaxAnswerSeconds => InterviewModes.LiveMaxAnswerSeconds;
        public int MaxReplays => InterviewModes.LiveMaxReplays;
        #endregion
    }
}
