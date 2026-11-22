namespace MockviewAI.Models
{
    /// <summary>
    /// [LIVE-MODE] 
    /// Hai chế độ luyện phỏng vấn và luật của chế độ Thực chiến.
    /// <list type="bullet">
    /// <item>practice (Luyện tập): được xoá ghi âm, gõ chữ, nghe lại; nhắc 1 lần khi câu trả lời vô nghĩa.</item>
    /// <item>live (Thực chiến): nói một lần như phỏng vấn thật, có thời gian suy nghĩ và giới hạn trả lời;
    /// không xoá / gõ / sửa, nghe lại câu hỏi 1 lần, bỏ qua hoặc vô nghĩa tính SAI ngay.</item>
    /// </list>
    /// </summary>
    public static class InterviewModes
    {
        #region Mã chế độ
        public const string Practice = "practice";
        public const string Live = "live";
        #endregion

        #region Luật Thực chiến (giao diện đọc qua InterviewRoomViewModel)
        /// <summary>Giây suy nghĩ trước khi micro tự bật.</summary>
        public const int LiveThinkSeconds = 15;

        /// <summary>Giây tối đa để trả lời; hết giờ tự gửi.</summary>
        public const int LiveMaxAnswerSeconds = 120;

        /// <summary>Số lần được nghe lại câu hỏi.</summary>
        public const int LiveMaxReplays = 1;
        #endregion

        #region Helpers
        /// <summary>Mã lạ hoặc trống -> Luyện tập, không bao giờ vô tình bật Thực chiến.</summary>
        public static string Normalize(string? mode) => mode == Live ? Live : Practice;

        public static bool IsLive(string? mode) => mode == Live;

        public static string DisplayName(string? mode) => IsLive(mode) ? "Thực chiến" : "Luyện tập";
        #endregion
    }
}
