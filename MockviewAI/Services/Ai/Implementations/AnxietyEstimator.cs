using MockviewAI.Models.Ai;
using MockviewAI.Services.Ai.Interfaces;

namespace MockviewAI.Services.Ai.Implementations
{
    /// <summary>
    /// [AI-MODULE] 
    /// AnxietyEstimator
    /// -- Ước tính dấu hiệu lo lắng bằng code thuần (không dùng AI), dựa trên 5 tín hiệu có thể quan sát:
    /// -- từ đệm 30% - khoảng lặng dài 25% - nói quá nhanh/chậm 15% - câu trả lời quá ngắn 15% - số câu bỏ qua 15%.
    /// -- Tín hiệu nào không đo được (vd: gõ tay không có tốc độ/khoảng lặng) thì bỏ ra và chia lại trọng số.
    /// -- LƯU Ý khi viết báo cáo: đây là chỉ số ước tính từ lời nói (proxy), không thay thế thang đo tự đánh giá hay chẩn đoán.
    /// </summary>
    public class AnxietyEstimator : IAnxietyEstimator
    {
        public (int Level, List<string> Reasons) Estimate(DeliveryScoreResult d, int questionCount, int skippedCount)
        {
            var reasons = new List<string>();
            if (questionCount <= 0) return (0, reasons);

            // (mức 0..1, trọng số) của từng tín hiệu đo được
            var signals = new List<(double Value, double Weight)>();

            // Từ đệm: FillerScore thấp = nhiều từ đệm
            signals.Add(((100 - d.FillerScore) / 100.0, 0.30));
            if (d.FillersPer100Words > 5) reasons.Add("dùng khá nhiều từ đệm (ờ, ừm, kiểu như...)");

            // Khoảng lặng và tốc độ chỉ có khi trả lời bằng giọng nói
            bool hasSpeechSignals = d.WordsPerMinute > 0;
            if (hasSpeechSignals)
            {
                signals.Add(((100 - d.PauseScore) / 100.0, 0.25));
                signals.Add(((100 - d.PaceScore) / 100.0, 0.15));
                if (d.LongestPauseSeconds > 5) reasons.Add("có khoảng lặng dài khi suy nghĩ");
                if (d.WordsPerMinute > 160) reasons.Add("nói nhanh hơn mức thoải mái");
                if (d.WordsPerMinute < 110) reasons.Add("nói chậm, ngắt quãng");
            }

            // Câu trả lời ngắn: dưới 40 từ càng ngắn càng cao
            signals.Add((Math.Clamp((40 - d.AverageAnswerWords) / 40.0, 0, 1), 0.15));
            if (d.AverageAnswerWords < 25) reasons.Add("câu trả lời khá ngắn");

            // Tỉ lệ câu bỏ qua
            double skipRatio = Math.Clamp(skippedCount / (double)questionCount, 0, 1);
            signals.Add((skipRatio, 0.15));
            if (skippedCount > 0) reasons.Add($"bỏ qua {skippedCount} câu");

            double totalWeight = signals.Sum(s => s.Weight);
            double score = signals.Sum(s => s.Value * s.Weight) / totalWeight;   // 0..1

            int level = Math.Clamp((int)Math.Round(1 + 9 * score), 1, 10);
            return (level, reasons);
        }
    }
}
