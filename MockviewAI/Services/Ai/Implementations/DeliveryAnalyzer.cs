using System.Text.RegularExpressions;
using MockviewAI.Models;
using MockviewAI.Models.Ai;
using MockviewAI.Services.Ai.Interfaces;

namespace MockviewAI.Services.Ai.Implementations
{
    /// <summary>
    /// [AI-SCORING] 
    /// Chấm CÁCH NÓI bằng code thuần (không dùng AI: nhanh, miễn phí, giải thích được).
    /// <list type="bullet">
    /// <item>Không có số đo giọng (gõ chữ, buổi cũ): tốc độ 30 · từ đệm 30 · độ dài 25 · khoảng lặng 15.</item>
    /// <item>[VOICE] Có số đo giọng (nói bằng micro): tốc độ 25 · từ đệm 25 · độ dài 20 · khoảng lặng 10 · âm lượng 12 · ngữ điệu 8
    /// (không đo được ngữ điệu thì âm lượng 20).</item>
    /// </list>
    /// Muốn chỉnh ngưỡng thì sửa các số trong Analyze.
    /// </summary>
    public class DeliveryAnalyzer : IDeliveryAnalyzer
    {
        #region Từ đệm
        /// <summary>Từ / cụm từ đệm (tiếng Việt + tiếng Anh), chỉ khớp nguyên từ.</summary>
        private static readonly string[] Fillers =
        {
            "ờ", "ờm", "ừm", "ừ", "à", "ớ", "kiểu", "kiểu như", "thì là", "nói chung là",
            "um", "uh", "er", "like", "you know"
        };

        private static readonly Regex FillerRegex = BuildFillerRegex();

        private static Regex BuildFillerRegex()
        {
            // Cụm dài xét trước để "kiểu như" thắng "kiểu"
            var parts = Fillers.OrderByDescending(f => f.Length).Select(Regex.Escape);
            // (?<!\p{L}) / (?!\p{L}) = ranh giới từ dùng được cho cả chữ tiếng Việt
            return new Regex($@"(?<!\p{{L}})({string.Join("|", parts)})(?!\p{{L}})",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        }
        #endregion

        #region IDeliveryAnalyzer
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

            // --- Điểm thành phần, mỗi điểm 0-100 ---
            // Tốc độ: 110-160 từ/phút là thoải mái, ra ngoài giảm dần. Gõ chữ (không có thời lượng) thì không trừ.
            bool hasPace = result.WordsPerMinute > 0;
            result.PaceScore = hasPace ? RangeScore(result.WordsPerMinute, 110, 160, 60) : 100;

            // Từ đệm: <= 2 / 100 từ là ổn, >= 10 là kém
            result.FillerScore = Linear(result.FillersPer100Words, good: 2, bad: 10);

            // Độ dài: 40-150 từ là câu trả lời tốt
            result.LengthScore = RangeScore(result.AverageAnswerWords, 40, 150, 40);

            // Khoảng lặng dài nhất: <= 2 giây ổn, >= 8 giây kém. Không đo được thì không trừ.
            result.PauseScore = Linear(result.LongestPauseSeconds, good: 2, bad: 8);

            // [VOICE] Âm lượng: nói nhỏ <= 15% thời gian là ổn, >= 60% là kém; to quá (dễ rè) tối đa 60.
            // Ngữ điệu: lên xuống giọng >= 2 nửa cung là tự nhiên, <= 0.5 là đều đều.
            var withVoice = answers.Where(a => a.QuietRatio != null).ToList();
            if (withVoice.Count > 0)
            {
                result.HasVoiceData = true;
                result.QuietRatio = Math.Round(withVoice.Average(a => a.QuietRatio!.Value), 3);
                var levels = withVoice.Where(a => a.VoiceLevelDb != null).ToList();
                result.VoiceLevelDb = levels.Count > 0 ? Math.Round(levels.Average(a => a.VoiceLevelDb!.Value), 1) : null;
                var tones = answers.Where(a => a.PitchVariation != null).ToList();
                result.PitchVariation = tones.Count > 0 ? Math.Round(tones.Average(a => a.PitchVariation!.Value), 1) : null;

                int volume = Linear(result.QuietRatio.Value, good: 0.15, bad: 0.6);
                if (result.VoiceLevelDb > VoiceMetrics.LoudDb) volume = Math.Min(volume, 60);
                result.VolumeScore = volume;
                if (result.PitchVariation != null) result.ToneScore = Rising(result.PitchVariation.Value, bad: 0.5, good: 2.0);
            }

            if (!result.HasVoiceData)
            {
                result.Total = (int)Math.Round(
                    result.PaceScore * 0.30 + result.FillerScore * 0.30 +
                    result.LengthScore * 0.25 + result.PauseScore * 0.15);
            }
            else
            {
                double core = result.PaceScore * 0.25 + result.FillerScore * 0.25 + result.LengthScore * 0.20 + result.PauseScore * 0.10;
                result.Total = (int)Math.Round(result.ToneScore is int tone
                    ? core + result.VolumeScore!.Value * 0.12 + tone * 0.08
                    : core + result.VolumeScore!.Value * 0.20);
            }

            AddTips(result, hasPace);
            return result;
        }
        #endregion

        #region Lời khuyên
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
            // [VOICE]
            if (r.QuietRatio > 0.3)
                r.Tips.Add($"Bạn nói nhỏ ở khoảng {r.QuietRatio * 100:0}% thời gian trả lời, người phỏng vấn có thể không nghe rõ. Hãy nói to, rõ hơn và ngồi gần micro.");
            if (r.VoiceLevelDb > VoiceMetrics.LoudDb)
                r.Tips.Add("Âm thanh hơi to nên dễ bị rè. Hãy ngồi xa micro một chút hoặc giảm âm lượng micro.");
            if (r.PitchVariation < VoiceMetrics.MonotoneSt)
                r.Tips.Add("Giọng khá đều đều. Hãy nhấn giọng ở ý chính và ví dụ để người nghe dễ theo dõi.");
        }
        #endregion

        #region Helpers
        private static int CountWords(string? text) =>
            string.IsNullOrWhiteSpace(text) ? 0 : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

        /// <summary>100 trong [low, high]; ra ngoài thì giảm dần, về 0 khi cách khoảng một đoạn <paramref name="slack"/>.</summary>
        private static int RangeScore(double value, double low, double high, double slack)
        {
            if (value >= low && value <= high) return 100;
            double distance = value < low ? low - value : value - high;
            return (int)Math.Round(Math.Clamp(100 - distance / slack * 100, 0, 100));
        }

        /// <summary>0 khi &lt;= bad, 100 khi &gt;= good (giá trị càng lớn càng tốt).</summary>
        private static int Rising(double value, double bad, double good)
        {
            if (value >= good) return 100;
            if (value <= bad) return 0;
            return (int)Math.Round(100 * (value - bad) / (good - bad));
        }

        /// <summary>100 khi &lt;= good, 0 khi &gt;= bad, đường thẳng ở giữa (giá trị càng nhỏ càng tốt).</summary>
        private static int Linear(double value, double good, double bad)
        {
            if (value <= good) return 100;
            if (value >= bad) return 0;
            return (int)Math.Round(100 * (bad - value) / (bad - good));
        }
        #endregion
    }
}
