namespace MockviewAI.Services.Ai.Interfaces
{
    /// <summary>
    /// [AI-INTERVIEW] 
    /// Kiểm tra nhanh câu trả lời trước khi nhận: lặp chữ, vô nghĩa, thô tục, cố gài lệnh cho AI.
    /// </summary>
    public interface IAnswerQualityChecker
    {
        /// <returns>null = tạm ổn; ngược lại là lý do (tiếng Việt) để nhắc người dùng trả lời lại.</returns>
        string? FindProblem(string answer);
    }
}
