namespace MockviewAI.Services.Exceptions
{
    /// <summary>
    /// [AI-INTERVIEW] 
    /// Lỗi nghiệp vụ của buổi phỏng vấn; message tiếng Việt, an toàn để hiện cho người dùng.
    /// </summary>
    public class InterviewException : Exception
    {
        public InterviewException(string message) : base(message) { }
    }
}
