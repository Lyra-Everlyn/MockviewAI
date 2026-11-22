using MockviewAI.Models.Ai;

namespace MockviewAI.Services.Ai.Interfaces
{
    /// <summary>
    /// [AI-SCORING] 
    /// Ước tính "dấu hiệu lo lắng" 1–10 từ cách nói. Là ước tính từ tín hiệu lời nói, không phải đo lường y khoa.
    /// </summary>
    public interface IAnxietyEstimator
    {
        /// <returns>(mức 1–10, các lý do); mức 0 = chưa đủ dữ liệu.</returns>
        (int Level, List<string> Reasons) Estimate(DeliveryScoreResult delivery, int questionCount, int skippedCount);
    }
}
