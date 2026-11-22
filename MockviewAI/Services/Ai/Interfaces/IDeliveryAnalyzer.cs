using MockviewAI.Models.Ai;

namespace MockviewAI.Services.Ai.Interfaces
{
    /// <summary>
    /// [AI-SCORING] 
    /// Chấm CÁCH NÓI bằng code: tốc độ, từ đệm, độ dài, khoảng lặng, âm lượng, ngữ điệu.
    /// </summary>
    public interface IDeliveryAnalyzer
    {
        /// <param name="answers">Mọi câu đã nói, kể cả câu vô nghĩa và câu trả lời thêm.</param>
        DeliveryScoreResult Analyze(IReadOnlyList<InterviewAnswer> answers);
    }
}
