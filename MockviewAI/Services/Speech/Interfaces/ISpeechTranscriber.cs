using MockviewAI.Models.Speech;

namespace MockviewAI.Services.Speech.Interfaces
{
    /// <summary>
    /// [SPEECH] 
    /// Chuyển một đoạn ghi âm thành văn bản thô (hiện dùng Groq Whisper)
    /// </summary>
    public interface ISpeechTranscriber
    {
        /// <param name="clip">File ghi âm.</param>
        /// <param name="prompt">Gợi ý văn phong / ngữ cảnh; null = không gửi.</param>
        /// <param name="model">Model cho lần gọi này; null = model trong cấu hình.</param>
        /// <param name="language">Mã ngôn ngữ ép cho lần gọi này; null = theo cấu hình ("auto" = Whisper tự nhận).</param>
        Task<WhisperTranscript> TranscribeAsync(AudioClip clip, string? prompt, string? model = null, string? language = null, CancellationToken ct = default);
    }
}
