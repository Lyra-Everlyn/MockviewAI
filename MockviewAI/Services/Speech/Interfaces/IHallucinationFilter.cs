using MockviewAI.Models.Speech;

namespace MockviewAI.Services.Speech.Interfaces
{
    /// <summary>
    /// [SPEECH]
    /// -- Loại các đoạn Whisper tự "bịa" khi đoạn ghi âm im lặng, nói quá nhỏ hoặc chỉ có tiếng ồn
    ///    (vd: "Các bạn hãy đăng kí cho kênh ... để không bỏ lỡ những video hấp dẫn"),
    ///    hoặc chép lại nguyên văn gợi ý (câu hỏi) đã gửi kèm.
    /// </summary>
    public interface IHallucinationFilter
    {
        /// <summary>
        /// Trả về văn bản chỉ gồm các đoạn đáng tin. Chuỗi rỗng = không có lời nói thật.
        /// </summary>
        /// <param name="transcript">Kết quả thô của Whisper.</param>
        /// <param name="prompt">Gợi ý đã gửi cho Whisper (nếu có) — đoạn chỉ lặp lại gợi ý này sẽ bị bỏ.</param>
        string Clean(WhisperTranscript transcript, string? prompt = null);
    }
}
