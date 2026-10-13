// [AI-MODULE] IAiResponseParser
// Giao diện đọc phản hồi của AI thành ContentScoreResult (điểm luôn được ép về 0..25).

using MockviewAI.Models.Ai;

namespace MockviewAI.Services.Ai.Interfaces
{
    public interface IAiResponseParser
    {
        // Ném AiScoringException nếu phản hồi không đọc được
        ContentScoreResult Parse(string apiResponseJson);
    }
}
