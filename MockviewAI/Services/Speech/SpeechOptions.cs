namespace MockviewAI.Services.Speech
{
    /// <summary>
    /// [SPEECH]
    /// SpeechOptions
    /// -- Cấu hình chuyển giọng nói thành chữ (Groq Whisper) và bước chuẩn hoá bản chép (Gemini).
    /// -- Đọc từ section "Speech" trong appsettings hoặc biến môi trường Speech__*.
    /// -- KHÔNG chứa API key: key lấy từ Groq__ApiKeys / Gemini__ApiKeys qua bộ xoay vòng key.
    /// </summary>
    public class SpeechOptions
    {
        public const string SectionName = "Speech";

        #region Whisper (Groq)
        /// <summary>Model chuyển giọng nói của Groq. Chốt 17/11: large-v3 (turbo kém với tiếng Việt, hay dịch sang tiếng Anh).</summary>
        public string WhisperModel { get; set; } = "whisper-large-v3";

        /// <summary>Mã ngôn ngữ ISO-639-1 gửi kèm cho Whisper (giúp nhận dạng nhanh và đúng hơn).</summary>
        public string Language { get; set; } = "vi";

        /// <summary>Dung lượng tối đa của một file ghi âm (byte). Mặc định 10 MB ≈ hơn 10 phút giọng nói webm/opus.</summary>
        public long MaxAudioBytes { get; set; } = 10 * 1024 * 1024;

        /// <summary>
        /// Model dự phòng khi model chính trả về văn bản KHÔNG phải tiếng Việt
        /// (whisper-large-v3-turbo đôi khi "dịch" câu nói lẫn tiếng Anh thành toàn tiếng Anh). Để trống = tắt.
        /// </summary>
        public string WhisperFallbackModel { get; set; } = string.Empty;

        /// <summary>
        /// Gửi câu hỏi làm gợi ý (prompt) cho Whisper. Chốt 18/11: TẮT — khi đoạn cuối bản ghi im lặng,
        /// Whisper chép lại nguyên câu hỏi vào câu trả lời (vd "Hãy giới thiệu ngắn về bản thân bạn.").
        /// Thuật ngữ IT đã có bước Gemini sửa nên không cần gợi ý.
        /// </summary>
        public bool UseQuestionAsWhisperPrompt { get; set; } = false;

        /// <summary>
        /// Câu mẫu gửi ngầm cho Whisper làm gợi ý văn phong: tiếng Việt có chen thuật ngữ tiếng Anh viết đúng chính tả,
        /// giúp Whisper giữ nguyên từ tiếng Anh thay vì "nghe ra tiếng Việt" (vd "Redis cache" -> "Credit Cacher").
        /// Giữ NGẮN (một câu): gợi ý dài làm Whisper kéo từ thường sang thuật ngữ. Để trống = tắt.
        /// </summary>
        public string WhisperStylePrompt { get; set; } = "Dạ, em làm backend với C#, .NET Core, Docker, Redis cache, PostgreSQL và REST API.";

        /// <summary>Thời gian chờ tối đa cho một lần gọi Whisper (giây).</summary>
        public int TranscribeTimeoutSeconds { get; set; } = 30;
        #endregion

        #region Lọc câu ảo giác (theo độ tin cậy từng đoạn của Whisper)
        /// <summary>Đoạn có xác suất "không có lời nói" cao hơn ngưỡng này VÀ độ tin cậy thấp thì bị bỏ (mặc định của Whisper: 0.6).</summary>
        public double NoSpeechThreshold { get; set; } = 0.6;

        /// <summary>Ngưỡng log xác suất trung bình; thấp hơn = Whisper không chắc (mặc định của Whisper: -1.0).</summary>
        public double LogprobThreshold { get; set; } = -1.0;

        /// <summary>Tỉ lệ nén cao hơn ngưỡng này = lặp lại bất thường, bị bỏ (mặc định của Whisper: 2.4).</summary>
        public double CompressionRatioThreshold { get; set; } = 2.4;
        #endregion

        #region Chuẩn hoá bản chép (Gemini)
        /// <summary>Bật/tắt bước Gemini sửa lỗi đồng âm, thuật ngữ IT và dấu câu. Tắt thì dùng nguyên bản Whisper.</summary>
        public bool NormalizeTranscript { get; set; } = true;

        /// <summary>
        /// Model Gemini chính để chuẩn hoá. Chốt 17/11: bản "lite" vì việc này chỉ là sửa chính tả,
        /// cần nhanh (~1 giây); model "flash" có chế độ suy nghĩ chậm hơn nhiều. Để trống = dùng Gemini:Model.
        /// </summary>
        public string NormalizerModel { get; set; } = "gemini-3.5-flash-lite";

        /// <summary>
        /// Model Gemini dự phòng khi model chính quá tải (503), hết lượt (429), không nhận yêu cầu (400/404) hoặc quá giờ.
        /// Để trống = dùng Gemini:Model (model chấm điểm).
        /// </summary>
        public string NormalizerFallbackModel { get; set; } = string.Empty;

        /// <summary>
        /// Mức "suy nghĩ" của Gemini khi chuẩn hoá: "minimal" (nhanh nhất, mặc định), "low", ... Để trống = không gửi
        /// (model tự quyết, thường chậm hơn nhiều). Model không nhận tham số này (HTTP 400) thì tự gửi lại không kèm.
        /// </summary>
        public string NormalizerThinkingLevel { get; set; } = "minimal";

        /// <summary>Thời gian chờ tối đa cho MỖI model (giây). Quá giờ thì chuyển sang model dự phòng.</summary>
        public int NormalizeAttemptTimeoutSeconds { get; set; } = 8;

        /// <summary>Tổng thời gian chờ tối đa cho bước chuẩn hoá, tính cả model dự phòng (giây). Quá giờ thì dùng bản thô.</summary>
        public int NormalizeTimeoutSeconds { get; set; } = 14;

        /// <summary>
        /// Bản chuẩn hoá chênh lệch độ dài quá tỉ lệ này so với bản thô (vd 0.3 = ±30%) thì bị coi là
        /// "sửa quá tay" (thêm/bớt ý) và hệ thống dùng lại bản thô.
        /// </summary>
        public double MaxLengthChangeRatio { get; set; } = 0.3;
        #endregion
    }
}
