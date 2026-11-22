using MockviewAI.Models.Ai;

namespace MockviewAI.Services.Ai.Interfaces
{
    /// <summary>
    /// [AI-SCORING] 
    /// Soạn prompt cho bộ chấm AI.
    /// </summary>
    public interface IScoringPromptBuilder
    {
        /// <summary>Lệnh hệ thống: vai trò, quy tắc chống prompt injection, chỉ trả JSON.</summary>
        string SystemPrompt { get; }

        /// <summary>Vị trí, các cặp hỏi – đáp, bộ đáp án (nếu có) và định dạng JSON mong muốn.</summary>
        string BuildUserPrompt(ScoreRequest request);
    }
}
