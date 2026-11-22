namespace MockviewAI.Services.Speech
{
    /// <summary>
    /// [SPEECH] 
    /// Cấu hình chuyển giọng nói thành chữ (Groq Whisper) và chuẩn hoá bản chép (Gemini).
    /// Mục "Speech", biến môi trường Speech__*.
    /// </summary>
    public class SpeechOptions
    {
        public const string SectionName = "Speech";

        #region Whisper (Groq)
        /// <summary>Model nhận giọng nói. Dùng large-v3 vì bản turbo kém với tiếng Việt.</summary>
        public string WhisperModel { get; set; } = "whisper-large-v3";

        /// <summary>
        /// "auto" = Whisper tự nhận ngôn ngữ từng câu trả lời (ứng viên có thể nói tiếng Việt hoặc tiếng Anh).
        /// Mã ISO-639-1 (vd "vi") = ép mọi câu trả lời theo ngôn ngữ đó.
        /// </summary>
        public string Language { get; set; } = "auto";

        /// <summary>Ngôn ngữ chấp nhận khi Language = auto; nhận ra ngôn ngữ khác (thường do câu quá ngắn) thì chép lại bằng FallbackLanguage.</summary>
        public string AllowedLanguages { get; set; } = "vi,en";

        /// <summary>Ngôn ngữ ép khi chép lại.</summary>
        public string FallbackLanguage { get; set; } = "vi";

        /// <summary>Dung lượng tối đa một file ghi âm (byte). 10 MB ≈ hơn 10 phút webm/opus.</summary>
        public long MaxAudioBytes { get; set; } = 10 * 1024 * 1024;

        /// <summary>Model dự phòng khi ép "vi" mà kết quả không phải tiếng Việt. Trống = tắt.</summary>
        public string WhisperFallbackModel { get; set; } = string.Empty;

        /// <summary>Gửi câu hỏi làm gợi ý cho Whisper. Mặc định tắt vì Whisper hay chép lại câu hỏi ở đoạn im lặng cuối.</summary>
        public bool UseQuestionAsWhisperPrompt { get; set; } = false;

        /// <summary>Câu mẫu văn phong (tiếng Việt chen thuật ngữ tiếng Anh) giúp Whisper giữ đúng từ tiếng Anh. Chỉ dùng khi ép ngôn ngữ. Trống = tắt.</summary>
        public string WhisperStylePrompt { get; set; } = "Dạ, em làm backend với C#, .NET Core, Docker, Redis cache, PostgreSQL và REST API.";

        /// <summary>Giới hạn thời gian một lần gọi Whisper (giây).</summary>
        public int TranscribeTimeoutSeconds { get; set; } = 30;
        #endregion

        #region Lọc câu ảo giác (ngưỡng mặc định của Whisper gốc)
        /// <summary>Xác suất "không có lời nói" trên mức này (kèm Logprob thấp) thì bỏ đoạn.</summary>
        public double NoSpeechThreshold { get; set; } = 0.6;

        /// <summary>Log xác suất trung bình dưới mức này = Whisper không chắc chắn.</summary>
        public double LogprobThreshold { get; set; } = -1.0;

        /// <summary>Tỉ lệ nén trên mức này = lặp bất thường, bỏ đoạn.</summary>
        public double CompressionRatioThreshold { get; set; } = 2.4;
        #endregion

        #region Chuẩn hoá bản chép (Gemini)
        /// <summary>Tắt thì trả thẳng bản Whisper đã lọc.</summary>
        public bool NormalizeTranscript { get; set; } = true;

        /// <summary>Bản lite cho nhanh (khoảng 1 giây). Trống = Gemini:Model.</summary>
        public string NormalizerModel { get; set; } = "gemini-3.5-flash-lite";

        /// <summary>Model thử khi model chính quá tải. Trống = Gemini:Model.</summary>
        public string NormalizerFallbackModel { get; set; } = string.Empty;

        /// <summary>Mức suy nghĩ gửi kèm (minimal = nhanh nhất); model không nhận thì gửi lại không kèm.</summary>
        public string NormalizerThinkingLevel { get; set; } = "minimal";

        /// <summary>Giới hạn thời gian cho mỗi model (giây).</summary>
        public int NormalizeAttemptTimeoutSeconds { get; set; } = 8;

        /// <summary>Giới hạn thời gian cho cả bước chuẩn hoá (giây); quá giờ thì dùng bản thô.</summary>
        public int NormalizeTimeoutSeconds { get; set; } = 14;

        /// <summary>Bản chuẩn hoá lệch độ dài quá tỉ lệ này so với bản thô = sửa quá tay, dùng lại bản thô.</summary>
        public double MaxLengthChangeRatio { get; set; } = 0.3;
        #endregion
    }
}
