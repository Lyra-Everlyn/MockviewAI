namespace MockviewAI.Services.Interviews.Conversation
{
    /// <summary>
    /// [INTERVIEWER] 
    /// Giữ tóm tắt CV của một buổi phỏng vấn trong thời gian buổi diễn ra (không lưu database).
    /// </summary>
    public interface ICandidateCvStore
    {
        Task SaveAsync(int sessionId, string cvContext, CancellationToken ct = default);

        /// <summary>Rỗng nếu buổi không dùng CV hoặc đã hết hạn.</summary>
        Task<string> GetAsync(int sessionId, CancellationToken ct = default);
    }
}
