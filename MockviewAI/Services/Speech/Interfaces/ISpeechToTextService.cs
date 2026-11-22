using MockviewAI.Models.Speech;

namespace MockviewAI.Services.Speech.Interfaces
{
    /// <summary>
    /// [SPEECH] 
    /// Điều phối Whisper -> lọc ảo giác -> Gemini gộp với chữ trình duyệt. Whisper lỗi: ném SpeechException.
    /// </summary>
    public interface ISpeechToTextService
    {
        /// <param name="browserText">Chữ trình duyệt tự nhận dạng trong lúc nói; null nếu không có.</param>
        Task<SpeechToTextResult> TranscribeAnswerAsync(AudioClip clip, string? question, string? browserText = null, CancellationToken ct = default);
    }
}
