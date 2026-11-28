namespace MockviewAI.Services.CvQuestions.Interfaces
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Nhớ model kiểm duyệt nào vừa báo hết lượt (429 / 413) để tạm bỏ qua trong một lúc,
    /// đỡ tốn lượt Gemini soạn câu mà chắc chắn không duyệt được. Dùng chung cho mọi request (Singleton).
    /// </summary>
    public interface IReviewerCooldown
    {
        /// <summary>true = model đang tạm nghỉ, chưa nên gọi.</summary>
        bool IsCooling(string model);

        /// <summary>Cho model nghỉ trong khoảng thời gian này (gọi lại thì lấy mốc xa hơn).</summary>
        void Trip(string model, TimeSpan duration);
    }
}
