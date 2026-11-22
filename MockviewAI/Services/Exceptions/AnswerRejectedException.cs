namespace MockviewAI.Services.Exceptions
{
    /// <summary>
    /// [AI-INTERVIEW] 
    /// Câu trả lời bị từ chối (vô nghĩa, lạc đề...), người dùng được trả lời lại.
    /// Kế thừa InterviewException nên message vẫn được hiển thị nguyên văn.
    /// </summary>
    public class AnswerRejectedException : InterviewException
    {
        public AnswerRejectedException(string message) : base(message) { }
    }
}
