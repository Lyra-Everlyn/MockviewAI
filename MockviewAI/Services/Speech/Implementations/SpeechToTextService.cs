using Microsoft.Extensions.Options;
using MockviewAI.Models.Speech;
using MockviewAI.Services.Exceptions;
using MockviewAI.Services.Speech.Interfaces;

namespace MockviewAI.Services.Speech.Implementations
{
    /// <summary>
    /// [SPEECH] 
    /// Điều phối: Whisper -> đối chiếu ngôn ngữ với chữ trình duyệt -> lọc câu "ảo giác" -> Gemini gộp hai bản -> kiểm tra an toàn.
    /// Whisper lỗi: ném ra SpeechException. Gemini lỗi hoặc sửa quá tay: trả bản thô, không làm hỏng câu trả lời.
    /// </summary>
    public class SpeechToTextService : ISpeechToTextService
    {
        #region Fields
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
        public async Task<SpeechToTextResult> TranscribeAnswerAsync(AudioClip clip, string? question, string? browserText = null, CancellationToken ct = default)
        {
            if (clip.Data.Length > _options.MaxAudioBytes)
                throw new SpeechException("File ghi âm quá lớn. Hãy trả lời ngắn gọn hơn rồi thử lại.");

            var browser = (browserText ?? string.Empty).Trim();

            // 1. Whisper + sửa ngôn ngữ
            var prompt = BuildWhisperPrompt(question);
            var transcript = await _transcriber.TranscribeAsync(clip, prompt, ct: ct);
            transcript = await FixLanguageAsync(clip, prompt, transcript, ct);
            transcript = await FixTranslationAsync(clip, transcript, browser, ct);

            // 2. Lọc ảo giác. Whisper vẫn dịch nhầm hoặc rỗng -> dùng chữ trình duyệt làm bản chính
            var raw = _hallucinations.Clean(transcript, prompt);
            bool whisperUsable = raw.Length > 0 && !IsMistranslated(raw, browser);
            if (!whisperUsable && browser.Length > 0)
            {
                _logger.LogWarning("[STT] Whisper text unusable (empty or wrong language), using browser text");
                raw = browser;
            }
            var compareWith = whisperUsable ? browser : string.Empty;
            var language = transcript.Language;

            _logger.LogInformation("[STT] source={Source}, language={Language}, whisper {RawChars} chars, browser {BrowserChars} chars",
                whisperUsable ? (browser.Length > 0 ? "whisper+browser" : "whisper") : "browser", language, raw.Length, browser.Length);

            if (string.IsNullOrWhiteSpace(raw) || !_options.NormalizeTranscript)
                return Result(raw, raw, false, language);

            // 3. Gemini sửa / gộp hai bản (lỗi -> bản thô)
            string normalized;
            try
            {
                normalized = await _normalizer.NormalizeAsync(raw, compareWith.Length > 0 ? compareWith : null, question, ct);
            }
            catch (SpeechException ex)
            {
                _logger.LogWarning(ex, "Transcript normalization failed, using raw text");
                return Result(raw, raw, false, language);
            }

            // 4. Rỗng, bị dịch, hoặc lệch độ dài quá mức so với cả hai bản = sửa quá tay -> bản thô
            if (!IsSafeEdit(raw, compareWith, normalized))
            {
                _logger.LogWarning("Normalized transcript rejected (raw {RawChars}, browser {BrowserChars}, normalized {NormChars} chars)",
                    raw.Length, compareWith.Length, normalized.Length);
                return Result(raw, raw, false, language);
            }

            return Result(raw, normalized, true, language);
        }
        #endregion

        #region Language
        /// <summary>
        /// Auto: ngôn ngữ nhận ra không thuộc AllowedLanguages (câu ngắn hay bị nhầm) -> chép lại bằng FallbackLanguage.
        /// Ép "vi": kết quả gần như không có chữ tiếng Việt -> thử WhisperFallbackModel (nếu có).
        /// </summary>
        private async Task<WhisperTranscript> FixLanguageAsync(AudioClip clip, string? prompt, WhisperTranscript transcript, CancellationToken ct)
        {
            if (IsAutoLanguage())
            {
                if (transcript.Language.Length == 0 || IsAllowed(transcript.Language)) return transcript;

                _logger.LogWarning("Whisper detected unexpected language {Language}, retrying as {Fallback}", transcript.Language, _options.FallbackLanguage);
                return await RetryAsync(clip, prompt, null, _options.FallbackLanguage, transcript, ct);
            }

            bool wrongLanguage = string.Equals(_options.Language, "vi", StringComparison.OrdinalIgnoreCase)
                                 && VietnameseText.LooksEnglish(transcript.Text);
            if (wrongLanguage && !string.IsNullOrWhiteSpace(_options.WhisperFallbackModel)
                && !string.Equals(_options.WhisperFallbackModel, _options.WhisperModel, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Whisper {Model} returned non-Vietnamese text, retrying with {Fallback}", _options.WhisperModel, _options.WhisperFallbackModel);
                return await RetryAsync(clip, prompt, _options.WhisperFallbackModel, null, transcript, ct);
            }

            return transcript;
        }

        /// <summary>Trình duyệt nghe ra tiếng Anh mà Whisper trả tiếng Việt = Whisper dịch nhầm -> chép lại, ép "en".</summary>
        private async Task<WhisperTranscript> FixTranslationAsync(AudioClip clip, WhisperTranscript transcript, string browser, CancellationToken ct)
        {
            if (!IsMistranslated(transcript.Text, browser)) return transcript;

            _logger.LogWarning("[STT] Browser heard English but Whisper returned Vietnamese, retrying with language=en");
            return await RetryAsync(clip, _prompts.WhisperTermsPrompt, null, "en", transcript, ct);
        }

        private static bool IsMistranslated(string whisperText, string browserText) =>
            VietnameseText.LooksEnglish(browserText) && VietnameseText.LooksVietnamese(whisperText);

        private async Task<WhisperTranscript> RetryAsync(AudioClip clip, string? prompt, string? model, string? language, WhisperTranscript original, CancellationToken ct)
        {
            try
            {
                var retry = await _transcriber.TranscribeAsync(clip, prompt, model, language, ct);
                return string.IsNullOrWhiteSpace(retry.Text) ? original : retry;
            }
            catch (SpeechException ex)
            {
                _logger.LogWarning(ex, "Whisper retry failed, keeping the first result");
                return original;
            }
        }

        private bool IsAutoLanguage()
        {
            var v = (_options.Language ?? string.Empty).Trim();
            return v.Length == 0 || v.Equals("auto", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsAllowed(string language) =>
            (_options.AllowedLanguages ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(language, StringComparer.OrdinalIgnoreCase);
        #endregion

        #region Helpers
        /// <summary>
        /// Auto: chỉ danh sách thuật ngữ không dấu (câu mẫu tiếng Việt làm Whisper dịch tiếng Anh sang tiếng Việt).
        /// Ép "vi": câu mẫu văn phong (+ câu hỏi nếu bật).
        /// </summary>
        private string? BuildWhisperPrompt(string? question)
        {
            if (IsAutoLanguage()) return _prompts.WhisperTermsPrompt;

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
        /// Không rỗng, không bị dịch khác ngôn ngữ bản gốc, và độ dài nằm trong MaxLengthChangeRatio của ít nhất một bản (câu dưới 40 ký tự nới tới 60%).
        /// </summary>
        private bool IsSafeEdit(string raw, string browser, string normalized)
        {
            if (string.IsNullOrWhiteSpace(normalized)) return false;
            if (VietnameseText.LooksEnglish(raw) && VietnameseText.LooksVietnamese(normalized)) return false;
            return WithinRatio(raw, normalized) || (browser.Length > 0 && WithinRatio(browser, normalized));
        }

        private bool WithinRatio(string source, string normalized)
        {
            double ratio = Math.Max(0.05, _options.MaxLengthChangeRatio);
            if (source.Length < 40) ratio = Math.Max(ratio, 0.6);
            double change = Math.Abs(normalized.Length - source.Length) / (double)Math.Max(1, source.Length);
            return change <= ratio;
        }

        private static SpeechToTextResult Result(string raw, string text, bool normalized, string language) =>
            new() { RawText = raw, Text = text, Normalized = normalized, Language = language };
        #endregion
    }
}
