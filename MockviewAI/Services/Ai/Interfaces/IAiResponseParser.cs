using MockviewAI.Models.Ai;

namespace MockviewAI.Services.Ai.Interfaces
{
    /// <summary>
    /// [AI-SCORING] 
    /// Đọc phản hồi của AI thành ContentScoreResult (điểm luôn được ép về 0..25).
    /// </summary>
    public interface IAiResponseParser
    {
        /// <exception cref="Exceptions.AiScoringException">Phản hồi không đọc được.</exception>
        ContentScoreResult Parse(string apiResponseJson);
    }
}
