// [AI-INTERVIEW] Lỗi có thông báo tiếng Việt, an toàn để hiển thị cho người dùng.

namespace MockviewAI.Services.Exceptions
{
    public class InterviewException : Exception
    {
        public InterviewException(string message) : base(message) { }
    }
}
