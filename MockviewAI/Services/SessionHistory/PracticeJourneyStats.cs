namespace MockviewAI.Services.SessionHistory
{
    /// <summary>
    /// [PROFILE]
    /// Số liệu thẻ "Hành trình của bạn" trên trang Hồ sơ, tính từ các buổi đã lưu.
    /// </summary>
    /// <param name="TotalSessions">Tổng số buổi luyện.</param>
    /// <param name="AnsweredQuestions">Số câu đã trả lời (không tính câu bỏ qua).</param>
    /// <param name="StreakDays">Số ngày luyện liên tiếp tính tới hôm nay (hoặc hôm qua nếu hôm nay chưa luyện).</param>
    public record PracticeJourneyStats(int TotalSessions, int AnsweredQuestions, int StreakDays);
}
