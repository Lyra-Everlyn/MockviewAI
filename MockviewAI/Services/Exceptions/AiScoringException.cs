// [AI-MODULE] Lỗi của bộ chấm AI, có thông báo tiếng Việt an toàn để hiển thị cho người dùng.

namespace MockviewAI.Services.Exceptions
{
    public class AiScoringException : Exception
    {
        public AiScoringException(string message, Exception? inner = null) : base(message, inner) { }
    }
}
