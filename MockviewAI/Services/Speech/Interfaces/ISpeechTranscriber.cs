using MockviewAI.Models.Speech;

namespace MockviewAI.Services.Speech.Interfaces
{
    /// <summary>
    /// [SPEECH]
    /// -- Chuyển một đoạn ghi âm thành văn bản thô (nhà cung cấp hiện tại: Groq Whisper).
    /// -- Lỗi được ném ra dưới dạng <see cref="MockviewAI.Services.Exceptions.SpeechException"/>.
    /// </summary>
    public interface ISpeechTranscriber
    {
        /// <summary>
        /// Gửi file ghi âm lên dịch vụ nhận dạng giọng nói.
        /// </summary>
        /// <param name="clip">File ghi âm.</param>
        /// <param name="prompt">Gợi ý ngữ cảnh (câu hỏi, thuật ngữ IT) giúp nhận đúng từ chuyên ngành. Có thể null.</param>
        /// <param name="model">Model dùng cho lần gọi này; null = model mặc định trong cấu hình.</param>
        /// <returns>Toàn văn + các đoạn kèm độ tin cậy (để lọc câu ảo giác).</returns>
        Task<WhisperTranscript> TranscribeAsync(AudioClip clip, string? prompt, string? model = null, CancellationToken ct = default);
    }
}
