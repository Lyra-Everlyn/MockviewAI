namespace MockviewAI.Models.Speech
{
    /// <summary>
    /// [SPEECH] 
    /// Một đoạn ghi âm câu trả lời gửi từ trình duyệt. Giữ byte[] để gửi lại được khi bộ xoay key thử key khác.
    /// </summary>
    /// <param name="Data">Nội dung file âm thanh.</param>
    /// <param name="FileName">Tên file kèm đuôi đúng định dạng (vd answer.webm) — Whisper dựa vào đây để giải mã.</param>
    /// <param name="ContentType">Kiểu MIME (vd audio/webm).</param>
    public record AudioClip(byte[] Data, string FileName, string ContentType);

    /// <summary>
    /// [SPEECH] Kết quả chuyển giọng nói thành chữ của một câu trả lời.
    /// </summary>
    public class SpeechToTextResult
    {
        /// <summary>Bản chép nguyên văn từ Whisper (đã lọc câu "ảo giác").</summary>
        public string RawText { get; init; } = string.Empty;

        /// <summary>Bản dùng để hiển thị và chấm điểm (bản chuẩn hoá, hoặc bản thô nếu không chuẩn hoá được).</summary>
        public string Text { get; init; } = string.Empty;

        public bool Normalized { get; init; }

        /// <summary>Mã ngôn ngữ Whisper nhận ra (vi, en...); rỗng nếu không rõ.</summary>
        public string Language { get; init; } = string.Empty;

        public bool IsEmpty => string.IsNullOrWhiteSpace(Text);
    }

    /// <summary>
    /// [SPEECH] 
    /// Một đoạn Whisper nhận dạng, kèm độ tin cậy do Whisper tính (dùng để lọc câu "ảo giác").
    /// </summary>
    /// <param name="Text">Nội dung đoạn.</param>
    /// <param name="AvgLogprob">Log xác suất trung bình (càng gần 0 càng chắc; dưới -1 là kém).</param>
    /// <param name="NoSpeechProb">Xác suất đoạn này không có lời nói (0–1).</param>
    /// <param name="CompressionRatio">Tỉ lệ nén văn bản; trên 2.4 = lặp bất thường.</param>
    /// <param name="Start">Giây bắt đầu (cho log chẩn đoán).</param>
    /// <param name="End">Giây kết thúc.</param>
    public record WhisperSegment(string Text, double AvgLogprob, double NoSpeechProb, double CompressionRatio, double Start = 0, double End = 0);

    /// <summary>
    /// [SPEECH] 
    /// Kết quả thô của Whisper: toàn văn, các đoạn, ngôn ngữ nhận ra.
    /// </summary>
    public class WhisperTranscript
    {
        public string Text { get; init; } = string.Empty;
        public IReadOnlyList<WhisperSegment> Segments { get; init; } = Array.Empty<WhisperSegment>();

        /// <summary>Độ dài file ghi âm (giây); 0 nếu không có.</summary>
        public double DurationSeconds { get; init; }

        /// <summary>Mã ISO-639-1 Whisper nhận ra (vd "vi", "en"); rỗng nếu nhà cung cấp không trả.</summary>
        public string Language { get; init; } = string.Empty;
    }
}
