namespace MockviewAI.Services.Speech.Interfaces
{
    /// <summary>
    /// [SPEECH]
    /// -- Sửa lỗi bản chép giọng nói (đồng âm, thuật ngữ IT bị nghe sai, dấu câu) mà KHÔNG thay đổi ý người nói.
    /// -- Lỗi được ném ra để <see cref="ISpeechToTextService"/> quyết định dùng lại bản thô.
    /// </summary>
    public interface ITranscriptNormalizer
    {
        /// <param name="rawText">Bản chép thô từ Whisper.</param>
        /// <param name="question">Câu hỏi phỏng vấn đang trả lời (ngữ cảnh để sửa đúng thuật ngữ). Có thể null.</param>
        /// <returns>Bản đã chuẩn hoá.</returns>
        Task<string> NormalizeAsync(string rawText, string? question, CancellationToken ct = default);
    }
}
