using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MockviewAI.Models.Speech;
using MockviewAI.Services.Speech.Interfaces;

namespace MockviewAI.Services.Speech.Implementations
{
    /// <summary>
    /// [SPEECH] 
    /// Lọc câu "ảo giác" của Whisper theo TỪNG ĐOẠN (segment), không phải cả bài,
    /// nên câu ảo giác chèn cuối câu trả lời thật cũng bị bỏ. 4 lớp:
    /// <list type="number">
    /// <item>Độ tin cậy: không có lời nói (no_speech_prob) cao + không chắc chắn (avg_logprob) thấp, hoặc lặp bất thường (compression_ratio); cùng ngưỡng Whisper gốc.</item>
    /// <item>Mẫu câu kết video YouTube tiếng Việt mà Whisper học từ phụ đề (đăng ký kênh, cảm ơn đã xem...).</item>
    /// <item>Đoạn trùng lặp liên tiếp (Whisper hay lặp nguyên câu khi bị "kẹt").</item>
    /// <item>Đoạn chỉ chép lại gợi ý (câu hỏi), thường ở đoạn im lặng cuối bản ghi.</item>
    /// </list>
    /// Cuối cùng bỏ ký tự thuộc hệ chữ khác (Devanagari, Hán, Thái...) mà Whisper đôi khi chèn vào (vd "C#ख्ती").
    /// Mỗi đoạn được ghi log (thời điểm, độ tin cậy, giữ/bỏ, lý do) để chẩn đoán khi bản chép thiếu chữ.
    /// So khớp trên chữ đã bỏ dấu nên "đăng kí" / "đăng ký" / "dang ky" đều bị bắt.
    /// </summary>
    public class WhisperHallucinationFilter : IHallucinationFilter
    {
        #region Mẫu câu ảo giác
        /// <summary>Viết trên chữ KHÔNG dấu, chữ thường (xem Fold).</summary>
        private static readonly Regex[] Patterns =
        {
            new(@"\bdang\s*k[iy]\b.{0,20}\bkenh\b", RegexOptions.Compiled),                 // đăng ký (cho) kênh
            new(@"\bsubscribe\b|\blike\s*(va|and)\s*share\b", RegexOptions.Compiled),
            new(@"\bbo\s*lo\b.{0,25}\bvideo\b", RegexOptions.Compiled),                      // không bỏ lỡ những video
            new(@"\bvideo\b.{0,20}\bhap\s*dan\b", RegexOptions.Compiled),                       // video hấp dẫn
            new(@"\bcam\s*on\b.{0,15}\bda\s*(theo\s*doi|xem|lang\s*nghe)\b", RegexOptions.Compiled),
            new(@"\bhen\s*gap\s*lai\b.{0,25}\bvideo\b", RegexOptions.Compiled),
            new(@"\b(phu\s*de|vietsub|sub)\s*(boi|by)\b", RegexOptions.Compiled),
            new(@"\bghien\s*mi\s*go\b|\blalaschool\b", RegexOptions.Compiled)
        };
        #endregion

        #region Fields
        /// <summary>Đoạn ngắn hơn số từ này không bị coi là chép lại gợi ý (tránh bỏ nhầm câu ngắn như "Dạ vâng").</summary>
        private const int PromptEchoMinWords = 4;

        /// <summary>
        /// Đoạn phải trùng ít nhất tỉ lệ này số từ của một câu gợi ý mới bị coi là chép lại
        /// (vd ứng viên nói "em làm backend với C#", trùng một phần câu mẫu, vẫn được giữ).
        /// </summary>
        private const double PromptEchoMinCoverage = 0.6;

        private readonly SpeechOptions _options;
        private readonly ILogger<WhisperHallucinationFilter> _logger;
        #endregion

        #region Constructor
        public WhisperHallucinationFilter(IOptions<SpeechOptions> options, ILogger<WhisperHallucinationFilter> logger)
        {
            _options = options.Value;
            _logger = logger;
        }
        #endregion

        #region IHallucinationFilter
        /// <inheritdoc />
        public string Clean(WhisperTranscript transcript, string? prompt = null)
        {
            // Nhà cung cấp không trả segment: coi toàn văn là một đoạn (chỉ áp dụng lớp 2, 3, 4)
            var segments = transcript.Segments.Count > 0
                ? transcript.Segments
                : new[] { new WhisperSegment(transcript.Text, 0, 0, 1) };

            // Gợi ý có thể gồm nhiều câu (câu mẫu văn phong + câu hỏi): so riêng từng câu
            var promptKeys = string.IsNullOrWhiteSpace(prompt)
                ? new List<string>()
                : Regex.Split(prompt, @"[.?!]+(?=\s|$)").Select(Fold).Where(k => k.Length > 0).ToList();
            var kept = new List<string>();
            string? previousKey = null;
            int dropped = 0;

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                var text = segment.Text.Trim();
                if (text.Length == 0) continue;

                var key = Fold(text);
                var reason = DropReason(segment, key, previousKey, promptKeys);

                _logger.LogInformation("[STT] seg {Index} {Start:0.0}-{End:0.0}s nsp={NoSpeech:0.00} lp={Logprob:0.00} cr={Compression:0.00} {Verdict} \"{Text}\"",
                    i, segment.Start, segment.End, segment.NoSpeechProb, segment.AvgLogprob, segment.CompressionRatio,
                    reason is null ? "KEEP" : "DROP:" + reason, text);

                if (reason is not null) { dropped++; continue; }
                kept.Add(text);
                previousKey = key;
            }

            if (dropped > 0)
                _logger.LogInformation("Hallucination filter dropped {Dropped}/{Total} Whisper segments", dropped, segments.Count);

            return RemoveForeignScripts(string.Join(" ", kept));
        }
        #endregion

        #region Luật lọc
        /// <summary>Lý do bỏ đoạn (null = giữ). Thứ tự: độ tin cậy, mẫu câu YouTube, lặp liên tiếp, chép lại gợi ý.</summary>
        private string? DropReason(WhisperSegment segment, string key, string? previousKey, IReadOnlyList<string> promptKeys)
        {
            if (IsLowConfidence(segment)) return "low-confidence";
            if (IsKnownHallucination(key)) return "pattern";
            if (key == previousKey) return "repeat";
            if (promptKeys.Any(p => IsPromptEcho(key, p))) return "prompt-echo";
            return null;
        }

        /// <summary>
        /// Lớp 4: đoạn nằm trọn trong một câu gợi ý và chiếm phần lớn câu đó (vd gợi ý "Chào bạn, hãy giới thiệu ngắn về bản thân bạn."
        /// và đoạn "Hãy giới thiệu ngắn về bản thân bạn."). Câu trả lời thật có nhắc lại câu hỏi thì luôn dài hơn / khác gợi ý nên không bị bỏ.
        /// </summary>
        private static bool IsPromptEcho(string key, string promptKey)
        {
            if (promptKey.Length == 0 || key.Length == 0) return false;
            int keyWords = key.Split(' ').Length;
            int promptWords = promptKey.Split(' ').Length;
            if (keyWords < Math.Max(PromptEchoMinWords, (int)Math.Ceiling(promptWords * PromptEchoMinCoverage))) return false;
            return promptKey.Contains(key, StringComparison.Ordinal);
        }

        /// <summary>Lớp 1: luật của Whisper gốc, im lặng mà vẫn "nghe" ra chữ, hoặc lặp bất thường.</summary>
        private bool IsLowConfidence(WhisperSegment s) =>
            (s.NoSpeechProb > _options.NoSpeechThreshold && s.AvgLogprob < _options.LogprobThreshold)
            || s.CompressionRatio > _options.CompressionRatioThreshold;

        /// <summary>Lớp 2: mẫu câu kết video YouTube.</summary>
        private static bool IsKnownHallucination(string folded) => Patterns.Any(p => p.IsMatch(folded));
        #endregion

        #region Helpers
        /// <summary>
        /// Bỏ chữ cái / dấu thuộc hệ chữ không phải Latin (tiếng Việt và tiếng Anh đều là Latin).
        /// Giữ nguyên số, dấu câu, ký hiệu (#, +, ., @...) và khoảng trắng.
        /// </summary>
        private static string RemoveForeignScripts(string text)
        {
            var sb = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                bool isLetterOrMark = char.IsLetter(c)
                    || category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark;
                if (isLetterOrMark && !IsLatin(c)) continue;
                sb.Append(c);
            }
            return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }

        /// <summary>Latin cơ bản + mở rộng (A, B), dấu kết hợp, và khối Latin mở rộng bổ sung chứa chữ Việt (ạ, ế, ữ...).</summary>
        private static bool IsLatin(char c) =>
            c <= '\u024F' || (c >= '\u0300' && c <= '\u036F') || (c >= '\u1E00' && c <= '\u1EFF');

        /// <summary>Bỏ dấu tiếng Việt, đưa về chữ thường và gộp khoảng trắng để so khớp mẫu / so trùng.</summary>
        private static string Fold(string text)
        {
            var normalized = text.ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
            }
            return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }
        #endregion
    }
}
