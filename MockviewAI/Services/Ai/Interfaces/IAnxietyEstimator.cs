// [AI-MODULE] IAnxietyEstimator
// Ước tính "dấu hiệu lo lắng" 1-10 từ cách nói. Đây là ƯỚC TÍNH từ tín hiệu lời nói, không phải đo lường y khoa.

using MockviewAI.Models.Ai;

namespace MockviewAI.Services.Ai.Interfaces
{
    public interface IAnxietyEstimator
    {
        // Trả về (mức 1-10, các lý do). Mức 0 = chưa đủ dữ liệu để ước tính.
        (int Level, List<string> Reasons) Estimate(DeliveryScoreResult delivery, int questionCount, int skippedCount);
    }
}
