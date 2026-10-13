// [AI-MODULE] IScoringPromptBuilder
// Giao diện soạn prompt cho bộ chấm AI (lệnh hệ thống + nội dung buổi phỏng vấn).

using MockviewAI.Models.Ai;

namespace MockviewAI.Services.Ai.Interfaces
{
    public interface IScoringPromptBuilder
    {
        // Lệnh hệ thống: vai trò, quy tắc chống prompt injection, chỉ trả JSON
        string SystemPrompt { get; }

        // Nội dung gửi kèm: vị trí, câu hỏi, câu trả lời, định dạng JSON mong muốn
        string BuildUserPrompt(ScoreRequest request);
    }
}
