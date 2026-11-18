using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MockviewAI.Models.Speech;
using MockviewAI.Services.Exceptions;
using MockviewAI.Services.KeyManagement.ProviderExtensions;
using MockviewAI.Services.Speech.Interfaces;

namespace MockviewAI.Services.Speech.Implementations
{
    /// <summary>
    /// [SPEECH]
    /// GroqWhisperTranscriber
    /// -- Gửi file ghi âm lên Groq Whisper (POST audio/transcriptions, chuẩn OpenAI) qua HttpClient "GroqClient".
    /// -- Key do GroqKeyHandler gắn và xoay vòng; class này không bao giờ đọc hay ghi log key.
    /// -- Groq nhận trực tiếp webm/ogg/mp4/m4a/wav/mp3/flac nên KHÔNG cần ffmpeg.
    /// -- Dùng response_format=verbose_json để lấy từng đoạn kèm độ tin cậy (lọc câu ảo giác ở SpeechToTextService).
    /// </summary>
    public class GroqWhisperTranscriber : ISpeechTranscriber
    {
        #region Fields
        public const string HttpClientName = "GroqClient";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GroqKeyProvider _keys;
        private readonly SpeechOptions _options;
        private readonly ILogger<GroqWhisperTranscriber> _logger;
        #endregion

        #region Constructor
        public GroqWhisperTranscriber(
            IHttpClientFactory httpClientFactory,
            GroqKeyProvider keys,
            IOptions<SpeechOptions> options,
            ILogger<GroqWhisperTranscriber> logger)
        {
            _httpClientFactory = httpClientFactory;
            _keys = keys;
            _options = options.Value;
            _logger = logger;
        }
        #endregion

        #region ISpeechTranscriber
        /// <inheritdoc />
        public async Task<WhisperTranscript> TranscribeAsync(AudioClip clip, string? prompt, string? model = null, CancellationToken ct = default)
        {
            if (!_keys.HasKeys)
                throw new SpeechException("Chức năng nhận giọng nói chưa được cấu hình (thiếu Groq API key).") { IsConfigurationError = true };
            if (clip.Data.Length == 0)
                return new WhisperTranscript();

            var whisperModel = string.IsNullOrWhiteSpace(model) ? _options.WhisperModel : model;

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _options.TranscribeTimeoutSeconds)));

            // ByteArrayContent: đọc lại được khi bộ xoay key gửi lại request
            using var form = new MultipartFormDataContent();
            var audio = new ByteArrayContent(clip.Data);
            audio.Headers.ContentType = MediaTypeHeaderValue.TryParse(clip.ContentType, out var mediaType)
                ? mediaType
                : new MediaTypeHeaderValue("application/octet-stream");
            form.Add(audio, "file", clip.FileName);
            form.Add(new StringContent(whisperModel), "model");
            form.Add(new StringContent(_options.Language), "language");
            form.Add(new StringContent("0"), "temperature");
            form.Add(new StringContent("verbose_json"), "response_format");
            if (!string.IsNullOrWhiteSpace(prompt))
                form.Add(new StringContent(prompt), "prompt");

            var client = _httpClientFactory.CreateClient(HttpClientName);
            try
            {
                using var response = await client.PostAsync("audio/transcriptions", form, timeout.Token);
                var body = await response.Content.ReadAsStringAsync(timeout.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Groq Whisper returned HTTP {Status} for model {Model} ({Bytes} bytes): {Reason}",
                        (int)response.StatusCode, whisperModel, clip.Data.Length, Truncate(body, 300));
                    throw ToFriendlyException(response.StatusCode);
                }

                var transcript = ReadTranscript(body);
                _logger.LogInformation("Groq Whisper OK ({Model}): {Bytes} bytes, audio {Duration:0.0}s -> {Chars} chars, {Segments} segments",
                    whisperModel, clip.Data.Length, transcript.DurationSeconds, transcript.Text.Length, transcript.Segments.Count);
                return transcript;
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException && !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Groq Whisper request failed (network/timeout)");
                throw new SpeechException("Không kết nối được dịch vụ nhận giọng nói hoặc phản hồi quá lâu. Vui lòng thử lại.", ex) { IsTransient = true };
            }
        }
        #endregion

        #region Helpers
        /// <summary>Đọc "text" và mảng "segments" (text, avg_logprob, no_speech_prob, compression_ratio, start, end) và "duration" trong JSON trả về.</summary>
        private static WhisperTranscript ReadTranscript(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var text = root.TryGetProperty("text", out var t) ? (t.GetString() ?? string.Empty).Trim() : string.Empty;

                var segments = new List<WhisperSegment>();
                if (root.TryGetProperty("segments", out var list) && list.ValueKind == JsonValueKind.Array)
                {
                    foreach (var seg in list.EnumerateArray())
                    {
                        segments.Add(new WhisperSegment(
                            seg.TryGetProperty("text", out var st) ? (st.GetString() ?? string.Empty).Trim() : string.Empty,
                            ReadDouble(seg, "avg_logprob", 0),
                            ReadDouble(seg, "no_speech_prob", 0),
                            ReadDouble(seg, "compression_ratio", 1),
                            ReadDouble(seg, "start", 0),
                            ReadDouble(seg, "end", 0)));
                    }
                }
                return new WhisperTranscript { Text = text, Segments = segments, DurationSeconds = ReadDouble(root, "duration", 0) };
            }
            catch (JsonException ex)
            {
                throw new SpeechException("Dịch vụ nhận giọng nói trả về dữ liệu không đọc được. Vui lòng thử lại.", ex) { IsTransient = true };
            }
        }

        /// <summary>Đọc số thực; thiếu hoặc sai kiểu thì dùng giá trị mặc định (coi như đoạn đáng tin).</summary>
        private static double ReadDouble(JsonElement element, string name, double fallback) =>
            element.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : fallback;

        /// <summary>Đổi mã lỗi HTTP thành thông báo tiếng Việt đúng bản chất (cấu hình / file / quá tải).</summary>
        private static SpeechException ToFriendlyException(HttpStatusCode status)
        {
            int code = (int)status;
            return code switch
            {
                401 or 403 => new SpeechException($"Dịch vụ nhận giọng nói từ chối API key (mã {code}). Quản trị viên cần kiểm tra Groq__ApiKeys.") { IsConfigurationError = true },
                404 => new SpeechException("Không tìm thấy model nhận giọng nói (mã 404). Quản trị viên cần kiểm tra Speech__WhisperModel.") { IsConfigurationError = true },
                413 => new SpeechException("File ghi âm quá lớn. Hãy trả lời ngắn gọn hơn rồi thử lại."),
                400 or 415 or 422 => new SpeechException("Không đọc được file ghi âm. Hãy ghi âm lại hoặc gõ câu trả lời."),
                429 => new SpeechException("Dịch vụ nhận giọng nói đang quá tải. Vui lòng thử lại sau ít giây.") { IsTransient = true },
                >= 500 => new SpeechException($"Dịch vụ nhận giọng nói tạm thời gặp sự cố (mã {code}). Vui lòng thử lại.") { IsTransient = true },
                _ => new SpeechException(string.Create(CultureInfo.InvariantCulture, $"Không chuyển được giọng nói thành chữ (mã {code})."))
            };
        }

        private static string Truncate(string text, int max) =>
            string.IsNullOrEmpty(text) || text.Length <= max ? text : text[..max] + "...";
        #endregion
    }
}
