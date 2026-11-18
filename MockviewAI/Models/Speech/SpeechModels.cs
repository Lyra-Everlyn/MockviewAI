namespace MockviewAI.Models.Speech
{
    /// <summary>
    /// [SPEECH]
    /// -- Một đoạn ghi âm câu trả lời gửi từ trình duyệt (webm/opus, mp4/aac, ogg...).
    /// -- Dữ liệu giữ ở dạng byte[] để có thể gửi lại khi bộ xoay key đổi key và thử lại.
    /// </summary>
    /// <param name="Data">Nội dung file âm thanh.</param>
    /// <param name="FileName">Tên file kèm phần mở rộng đúng định dạng (vd: answer.webm) — Whisper dựa vào đây để giải mã.</param>
    /// <param name="ContentType">Kiểu MIME (vd: audio/webm).</param>
    public record AudioClip(byte[] Data, string FileName, string ContentType);

    /// <summary>
    /// [SPEECH]
    /// -- Kết quả chuyển giọng nói thành chữ của một câu trả lời.
    /// </summary>
    public class SpeechToTextResult
    {
        /// <summary>Bản chép nguyên văn từ Whisper (đã lọc câu "ảo giác" khi im lặng).</summary>
        public string RawText { get; init; } = string.Empty;

        /// <summary>Bản cuối cùng dùng để hiển thị và gửi chấm điểm (bản chuẩn hoá, hoặc bản thô nếu không chuẩn hoá được).</summary>
        public string Text { get; init; } = string.Empty;

        /// <summary>true nếu <see cref="Text"/> là bản đã được Gemini chuẩn hoá.</summary>
        public bool Normalized { get; init; }

        /// <summary>true nếu không nhận ra lời nói nào (im lặng, chỉ có tiếng ồn).</summary>
        public bool IsEmpty => string.IsNullOrWhiteSpace(Text);
    }

    /// <summary>
    /// [SPEECH]
    /// -- Một đoạn (segment) Whisper nhận dạng, kèm độ tin cậy do chính Whisper tính.
    /// -- Dùng để loại các đoạn "ảo giác" (Whisper tự bịa khi im lặng / tiếng nhỏ / tiếng ồn).
    /// </summary>
    /// <param name="Text">Nội dung đoạn.</param>
    /// <param name="AvgLogprob">Log xác suất trung bình của các token (càng gần 0 càng chắc; dưới -1 là kém).</param>
    /// <param name="NoSpeechProb">Xác suất đoạn này KHÔNG có lời nói (0–1).</param>
    /// <param name="CompressionRatio">Tỉ lệ nén văn bản; cao (trên 2.4) = lặp đi lặp lại bất thường.</param>
    /// <param name="Start">Giây bắt đầu của đoạn trong file ghi âm (dùng cho log chẩn đoán).</param>
    /// <param name="End">Giây kết thúc của đoạn trong file ghi âm.</param>
    public record WhisperSegment(string Text, double AvgLogprob, double NoSpeechProb, double CompressionRatio, double Start = 0, double End = 0);

    /// <summary>
    /// [SPEECH]
    /// -- Kết quả thô của Whisper: toàn văn + danh sách đoạn (có thể rỗng nếu nhà cung cấp không trả segment).
    /// </summary>
    public class WhisperTranscript
    {
        public string Text { get; init; } = string.Empty;
        public IReadOnlyList<WhisperSegment> Segments { get; init; } = Array.Empty<WhisperSegment>();

        /// <summary>Độ dài file ghi âm (giây) do Whisper báo về; 0 nếu không có. Dùng để phát hiện bản ghi bị cắt sớm.</summary>
        public double DurationSeconds { get; init; }
    }
}
