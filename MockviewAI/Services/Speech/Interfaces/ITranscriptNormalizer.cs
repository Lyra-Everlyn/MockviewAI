namespace MockviewAI.Services.Speech.Interfaces
{
    /// <summary>
    /// [SPEECH] 
    /// Sửa lỗi bản chép giọng nói (thuật ngữ nghe sai, dấu câu) mà không đổi ý người nói. Lỗi ném ra để dùng lại bản thô.
    /// </summary>
    public interface ITranscriptNormalizer
    {
        /// <param name="rawText">Bản chép của Whisper.</param>
        /// <param name="browserText">Bản nhận dạng của trình duyệt để đối chiếu; null = chỉ sửa bản Whisper.</param>
        /// <param name="question">Câu hỏi đang trả lời (ngữ cảnh). Có thể null.</param>
        Task<string> NormalizeAsync(string rawText, string? browserText, string? question, CancellationToken ct = default);
    }
}
