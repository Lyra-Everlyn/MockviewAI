// [AI-MODULE] IAnswerQualityChecker
// Kiểm tra nhanh câu trả lời TRƯỚC khi nhận: quá ngắn, lặp chữ, vô nghĩa, chửi thề, cố gài lệnh cho AI.

namespace MockviewAI.Services.Ai.Interfaces
{
    public interface IAnswerQualityChecker
    {
        // Trả về null nếu câu trả lời tạm ổn; ngược lại trả về lý do (tiếng Việt) để nhắc người dùng nói lại
        string? FindProblem(string answer);
    }
}
