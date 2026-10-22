// [AI-INTERVIEW] Câu trả lời bị từ chối (quá ngắn, lặp chữ, vô nghĩa, lạc đề...). Người dùng được nói lại.
// Kế thừa InterviewException nên thông báo tiếng Việt vẫn hiển thị an toàn cho người dùng.

namespace MockviewAI.Services.Exceptions
{
    public class AnswerRejectedException : InterviewException
    {
        public AnswerRejectedException(string message) : base(message) { }
    }
}
