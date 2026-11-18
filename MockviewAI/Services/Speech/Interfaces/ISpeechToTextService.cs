using MockviewAI.Models.Speech;

namespace MockviewAI.Services.Speech.Interfaces
{
    /// <summary>
    /// [SPEECH]
    /// -- Điều phối toàn bộ quy trình: Whisper -> lọc câu ảo giác -> Gemini chuẩn hoá -> kiểm tra an toàn.
    /// -- Whisper lỗi: ném <see cref="MockviewAI.Services.Exceptions.SpeechException"/>.
    /// -- Gemini lỗi hoặc sửa quá tay: dùng lại bản thô (không làm hỏng câu trả lời của người dùng).
    /// </summary>
    public interface ISpeechToTextService
    {
        Task<SpeechToTextResult> TranscribeAnswerAsync(AudioClip clip, string? question, CancellationToken ct = default);
    }
}
