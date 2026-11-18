using Microsoft.Extensions.Options;
using MockviewAI.Models.Speech;
using MockviewAI.Services.Exceptions;
using MockviewAI.Services.Speech.Interfaces;

namespace MockviewAI.Services.Speech.Implementations
{
    /// <summary>
    /// [SPEECH]
    /// SpeechToTextService
    /// -- Điều phối: Whisper -> kiểm tra ngôn ngữ (sai thì thử model dự phòng, nếu có cấu hình) -> lọc câu "ảo giác"
    ///    -> Gemini chuẩn hoá -> kiểm tra an toàn -> kết quả.
    /// -- Whisper lỗi: ném SpeechException (người dùng thấy thông báo, có thể thử lại hoặc dùng chữ của trình duyệt).
    /// -- Gemini lỗi / sửa quá tay: trả bản thô của Whisper, KHÔNG làm hỏng câu trả lời.
    /// </summary>
    public class SpeechToTextService : ISpeechToTextService
    {
        #region Fields
        // Chữ cái chỉ tiếng Việt mới có (dùng để nhận ra Whisper đã trả về tiếng Anh thay vì tiếng Việt)
        private const string VietnameseLetters = "àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ";
        private const int LanguageCheckMinWords = 6;
        private const double MinVietnameseWordRatio = 0.1;   // văn bản tiếng Việt thường có 40–60% từ mang dấu

        private readonly ISpeechTranscriber _transcriber;
        private readonly IHallucinationFilter _hallucinations;
        private readonly ITranscriptNormalizer _normalizer;
        private readonly ISpeechPromptBuilder _prompts;
        private readonly SpeechOptions _options;
        private readonly ILogger<SpeechToTextService> _logger;
        #endregion

        #region Constructor
        public SpeechToTextService(
            ISpeechTranscriber transcriber,
            IHallucinationFilter hallucinations,
            ITranscriptNormalizer normalizer,
            ISpeechPromptBuilder prompts,
            IOptions<SpeechOptions> options,
            ILogger<SpeechToTextService> logger)
        {
            _transcriber = transcriber;
            _hallucinations = hallucinations;
            _normalizer = normalizer;
            _prompts = prompts;
            _options = options.Value;
            _logger = logger;
        }
        #endregion

        #region ISpeechToTextService
        /// <inheritdoc />
        public async Task<SpeechToTextResult> TranscribeAnswerAsync(AudioClip clip, string? question, CancellationToken ct = default)
        {
            if (clip.Data.Length > _options.MaxAudioBytes)
                throw new SpeechException("File ghi âm quá lớn. Hãy trả lời ngắn gọn hơn rồi thử lại.");

            // 1. Whisper (lỗi -> ném ra cho controller). Gợi ý = câu mẫu văn phong (+ câu hỏi nếu bật).
            var prompt = BuildWhisperPrompt(question);
            var transcript = await _transcriber.TranscribeAsync(clip, prompt, null, ct);

            // 1b. Model chính trả về không phải tiếng Việt (vd turbo "dịch" sang tiếng Anh) -> thử model dự phòng một lần
            if (LooksWrongLanguage(transcript.Text) && !string.IsNullOrWhiteSpace(_options.WhisperFallbackModel)
                && !string.Equals(_options.WhisperFallbackModel, _options.WhisperModel, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Whisper {Model} returned non-Vietnamese text, retrying with {Fallback}", _options.WhisperModel, _options.WhisperFallbackModel);
                try
                {
                    var retry = await _transcriber.TranscribeAsync(clip, prompt, _options.WhisperFallbackModel, ct);
                    if (!string.IsNullOrWhiteSpace(retry.Text)) transcript = retry;
                }
                catch (SpeechException ex)
                {
                    _logger.LogWarning(ex, "Whisper fallback model failed, keeping primary result");
                }
            }

            // 2. Lọc câu ảo giác theo từng đoạn (độ tin cậy Whisper + mẫu câu + lặp lại)
            var raw = _hallucinations.Clean(transcript, prompt);

            if (string.IsNullOrWhiteSpace(raw) || !_options.NormalizeTranscript)
                return new SpeechToTextResult { RawText = raw, Text = raw, Normalized = false };

            // 3. Gemini chuẩn hoá (lỗi -> dùng bản thô)
            string normalized;
            try
            {
                normalized = await _normalizer.NormalizeAsync(raw, question, ct);
            }
            catch (SpeechException ex)
            {
                _logger.LogWarning(ex, "Transcript normalization failed, using raw Whisper text");
                return new SpeechToTextResult { RawText = raw, Text = raw, Normalized = false };
            }

            // 4. Kiểm tra an toàn: rỗng hoặc lệch độ dài quá mức = sửa quá tay -> dùng bản thô
            if (!IsSafeEdit(raw, normalized))
            {
                _logger.LogWarning("Normalized transcript rejected (raw {RawChars} chars, normalized {NormChars} chars)", raw.Length, normalized.Length);
                return new SpeechToTextResult { RawText = raw, Text = raw, Normalized = false };
            }

            return new SpeechToTextResult { RawText = raw, Text = normalized, Normalized = true };
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Ghép gợi ý cho Whisper: câu mẫu văn phong (WhisperStylePrompt) và câu hỏi (nếu UseQuestionAsWhisperPrompt).
        /// null = không gửi gợi ý. Bộ lọc ảo giác dùng lại chuỗi này để bỏ đoạn Whisper chép lại gợi ý.
        /// </summary>
        private string? BuildWhisperPrompt(string? question)
        {
            var parts = new List<string>(2);
            if (!string.IsNullOrWhiteSpace(_options.WhisperStylePrompt)) parts.Add(_options.WhisperStylePrompt.Trim());
            if (_options.UseQuestionAsWhisperPrompt)
            {
                var q = _prompts.BuildWhisperPrompt(question);
                if (!string.IsNullOrWhiteSpace(q)) parts.Add(q);
            }
            return parts.Count == 0 ? null : string.Join(" ", parts);
        }

        /// <summary>
        /// true khi cấu hình là tiếng Việt nhưng văn bản đủ dài lại gần như không có từ nào mang dấu tiếng Việt.
        /// </summary>
        private bool LooksWrongLanguage(string text)
        {
            if (!string.Equals(_options.Language, "vi", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(text))
                return false;

            var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < LanguageCheckMinWords) return false;

            int vietnameseWords = words.Count(w => w.ToLowerInvariant().IndexOfAny(VietnameseLetters.ToCharArray()) >= 0);
            return vietnameseWords / (double)words.Length < MinVietnameseWordRatio;
        }

        /// <summary>
        /// Bản chuẩn hoá hợp lệ khi không rỗng và độ dài không lệch quá MaxLengthChangeRatio so với bản thô.
        /// Câu rất ngắn (dưới 40 ký tự) được nới thêm vì sửa thuật ngữ ("xi thăng" -> "C#") làm đổi tỉ lệ nhiều.
        /// </summary>
        private bool IsSafeEdit(string raw, string normalized)
        {
            if (string.IsNullOrWhiteSpace(normalized)) return false;

            double ratio = Math.Max(0.05, _options.MaxLengthChangeRatio);
            if (raw.Length < 40) ratio = Math.Max(ratio, 0.6);

            double change = Math.Abs(normalized.Length - raw.Length) / (double)Math.Max(1, raw.Length);
            return change <= ratio;
        }
        #endregion
    }
}
