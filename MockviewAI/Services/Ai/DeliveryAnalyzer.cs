// [AI-MODULE] DeliveryAnalyzer
// Chấm CÁCH NÓI bằng code thuần (không dùng AI): tốc độ, từ đệm, độ dài, khoảng lặng.
// Trọng số: tốc độ 30, từ đệm 30, độ dài 25, khoảng lặng 15. Muốn chỉnh ngưỡng thì sửa các số trong hàm Analyze.

using System.Text.RegularExpressions;
using MockviewAI.Models.Ai;

namespace MockviewAI.Services.Ai
{
    // Scores HOW the candidate speaks, using plain code (fast, free, explainable).
    public class DeliveryAnalyzer
    {
        // Filler words/phrases (Vietnamese + English). Matched as whole words only.
        private static readonly string[] Fillers =
        {
            "ờ", "ờm", "ừm", "ừ", "à", "ớ", "kiểu", "kiểu như", "thì là", "nói chung là",
            "um", "uh", "er", "like", "you know"
        };

        private static readonly Regex FillerRegex = BuildFillerRegex();

        private static Regex BuildFillerRegex()
        {
            // Longest phrases first so "kiểu như" wins over "kiểu"
            var parts = Fillers.OrderByDescending(f => f.Length).Select(Regex.Escape);
            // (?<!\p{L}) / (?!\p{L}) = word boundary that also works for Vietnamese letters
            return new Regex($@"(?<!\p{{L}})({string.Join("|", parts)})(?!\p{{L}})",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        }

        public DeliveryScoreResult Analyze(IReadOnlyList<InterviewAnswer> answers)
        {
            var result = new DeliveryScoreResult();
            if (answers.Count == 0) return result;

            int totalWords = 0, totalFillers = 0;
            double totalSeconds = 0;
            int timedWords = 0;

            foreach (var a in answers)
            {
                int words = CountWords(a.Answer);
                totalWords += words;
                totalFillers += FillerRegex.Matches(a.Answer ?? string.Empty).Count;

                if (a.DurationSeconds is > 0)
                {
                    totalSeconds += a.DurationSeconds.Value;
                    timedWords += words;
                }
                if (a.PausesSeconds.Count > 0)
                    result.LongestPauseSeconds = Math.Max(result.LongestPauseSeconds, a.PausesSeconds.Max());
            }

            result.FillerCount = totalFillers;
            result.AverageAnswerWords = (double)totalWords / answers.Count;
            result.FillersPer100Words = totalWords == 0 ? 0 : totalFillers * 100.0 / totalWords;
            result.WordsPerMinute = totalSeconds > 0 ? timedWords / (totalSeconds / 60.0) : 0;

            // --- Sub-scores, each 0-100 ---
            // Pace: 110-160 words/minute is comfortable; drops linearly outside that range.
            // When the answer was typed (no duration) we skip pace and pause instead of punishing.
            bool hasPace = result.WordsPerMinute > 0;
            result.PaceScore = hasPace ? RangeScore(result.WordsPerMinute, 110, 160, 60) : 100;

            // Fillers: <=2 per 100 words is fine, >=10 is poor.
            result.FillerScore = Linear(result.FillersPer100Words, good: 2, bad: 10);

            // Answer length: 40-150 words is a solid answer.
            result.LengthScore = RangeScore(result.AverageAnswerWords, 40, 150, 40);

            // Longest silence: <=2s fine, >=8s poor. Unknown pauses -> no penalty.
            result.PauseScore = Linear(result.LongestPauseSeconds, good: 2, bad: 8);

            // Weights: pace 30, fillers 30, length 25, pauses 15
            result.Total = (int)Math.Round(
                result.PaceScore * 0.30 + result.FillerScore * 0.30 +
                result.LengthScore * 0.25 + result.PauseScore * 0.15);

            AddTips(result, hasPace);
            return result;
        }

        private static void AddTips(DeliveryScoreResult r, bool hasPace)
        {
            if (hasPace && r.WordsPerMinute > 160)
                r.Tips.Add($"Bạn nói khá nhanh (~{r.WordsPerMinute:0} từ/phút). Thử chậm lại, ngắt nghỉ giữa các ý.");
            if (hasPace && r.WordsPerMinute < 110)
                r.Tips.Add($"Bạn nói khá chậm (~{r.WordsPerMinute:0} từ/phút). Chuẩn bị ý chính trước để nói liền mạch hơn.");
            if (r.FillersPer100Words > 5)
                r.Tips.Add($"Có nhiều từ đệm ({r.FillerCount} lần). Thay bằng một nhịp dừng ngắn.");
            if (r.AverageAnswerWords < 40)
                r.Tips.Add("Câu trả lời hơi ngắn. Thêm lý do hoặc một ví dụ cụ thể.");
            if (r.AverageAnswerWords > 150)
                r.Tips.Add("Câu trả lời hơi dài. Tóm gọn ý chính trong khoảng 1-2 phút.");
            if (r.LongestPauseSeconds > 5)
                r.Tips.Add($"Có khoảng lặng dài (~{r.LongestPauseSeconds:0} giây). Có thể nói 'cho em suy nghĩ một chút' thay vì im lặng.");
        }

        private static int CountWords(string? text) =>
            string.IsNullOrWhiteSpace(text) ? 0 : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

        // 100 inside [low, high]; outside it the score drops to 0 once the value is `slack` away from the range
        private static int RangeScore(double value, double low, double high, double slack)
        {
            if (value >= low && value <= high) return 100;
            double distance = value < low ? low - value : value - high;
            return (int)Math.Round(Math.Clamp(100 - distance / slack * 100, 0, 100));
        }

        // 100 at <= good, 0 at >= bad, straight line in between
        private static int Linear(double value, double good, double bad)
        {
            if (value <= good) return 100;
            if (value >= bad) return 0;
            return (int)Math.Round(100 * (bad - value) / (bad - good));
        }
    }
}
