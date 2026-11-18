namespace MockviewAI.Services.Speech.Interfaces
{
    /// <summary>
    /// [SPEECH]
    /// -- Soạn nội dung gợi ý cho Whisper và prompt cho bước chuẩn hoá (tách riêng để dễ chỉnh mà không đụng code gọi API).
    /// </summary>
    public interface ISpeechPromptBuilder
    {
        /// <summary>Gợi ý ngắn cho Whisper: câu hỏi + danh sách thuật ngữ IT (giới hạn độ dài theo quy định của Whisper).</summary>
        string BuildWhisperPrompt(string? question);

        /// <summary>System prompt cố định cho bước chuẩn hoá.</summary>
        string NormalizerSystemPrompt { get; }

        /// <summary>Nội dung gửi kèm cho bước chuẩn hoá: câu hỏi (chỉ để tham khảo) + bản chép thô.</summary>
        string BuildNormalizerUserPrompt(string rawText, string? question);
    }
}
