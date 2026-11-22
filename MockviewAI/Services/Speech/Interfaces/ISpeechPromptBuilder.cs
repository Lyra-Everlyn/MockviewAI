namespace MockviewAI.Services.Speech.Interfaces
{
    /// <summary>
    /// [SPEECH] 
    /// Soạn gợi ý cho Whisper và prompt cho bước chuẩn hoá.
    /// </summary>
    public interface ISpeechPromptBuilder
    {
        /// <summary>Gợi ý ngắn từ câu hỏi (giới hạn độ dài của Whisper).</summary>
        string BuildWhisperPrompt(string? question);

        /// <summary>Danh sách thuật ngữ ngắn, không thuộc ngôn ngữ nào; dùng khi Whisper tự nhận ngôn ngữ.</summary>
        string WhisperTermsPrompt { get; }

        string NormalizerSystemPrompt { get; }

        /// <summary>Câu hỏi (tham khảo) + bản Whisper + bản trình duyệt (nếu có).</summary>
        string BuildNormalizerUserPrompt(string rawText, string? browserText, string? question);
    }
}
