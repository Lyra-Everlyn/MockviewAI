namespace MockviewAI.Services.Speech
{
    /// <summary>
    /// [SPEECH] 
    /// Cấu hình chuyển giọng nói thành chữ (Groq Whisper) và chuẩn hoá bản chép (Gemini). Section "Speech".
    /// </summary>
    public class SpeechOptions
    {
        public const string SectionName = "Speech";

        #region Whisper (Groq)
        /// <summary>Model nhận giọng nói. Chốt 17/11: large-v3 (turbo kém với tiếng Việt).</summary>
        public string WhisperModel { get; set; } = "whisper-large-v3";

        /// <summary>
        /// "auto" = Whisper tự nhận ngôn ngữ của từng câu trả lời (ứng viên có thể trả lời tiếng Việt hoặc tiếng Anh).
        /// Mã ISO-639-1 (vd "vi") = ép mọi câu trả lời theo ngôn ngữ đó.
        /// </summary>
        public string Language { get; set; } = "auto";

        /// <summary>Ngôn ngữ chấp nhận khi Language = auto. Nhận ra ngôn ngữ khác (thường do câu quá ngắn) thì chép lại bằng FallbackLanguage.</summary>
        public string AllowedLanguages { get; set; } = "vi,en";

        public string FallbackLanguage { get; set; } = "vi";

        /// <summary>Dung lượng tối đa một file ghi âm (byte). 10 MB ≈ hơn 10 phút webm/opus.</summary>
        public long MaxAudioBytes { get; set; } = 10 * 1024 * 1024;

        /// <summary>Model dự phòng khi ép "vi" mà kết quả không phải tiếng Việt. Trống = tắt.</summary>
        public string WhisperFallbackModel { get; set; } = string.Empty;

        /// <summary>Gửi câu hỏi làm gợi ý cho Whisper. Chốt 18/11: tắt (Whisper chép lại câu hỏi ở đoạn im lặng cuối).</summary>
        public bool UseQuestionAsWhisperPrompt { get; set; } = false;

        /// <summary>Câu mẫu văn phong (tiếng Việt chen thuật ngữ tiếng Anh) giúp Whisper giữ đúng từ tiếng Anh. Trống = tắt.</summary>
        public string WhisperStylePrompt { get; set; } = "Dạ, em làm backend với C#, .NET Core, Docker, Redis cache, PostgreSQL và REST API.";

        public int TranscribeTimeoutSeconds { get; set; } = 30;
        #endregion

        #region Lọc câu ảo giác (ngưỡng mặc định của Whisper)
        public double NoSpeechThreshold { get; set; } = 0.6;
        public double LogprobThreshold { get; set; } = -1.0;
        public double CompressionRatioThreshold { get; set; } = 2.4;
        #endregion

        #region Chuẩn hoá bản chép (Gemini)
        public bool NormalizeTranscript { get; set; } = true;

        /// <summary>Chốt 17/11: bản lite cho nhanh (~1 giây). Trống = Gemini:Model.</summary>
        public string NormalizerModel { get; set; } = "gemini-3.5-flash-lite";

        /// <summary>Trống = Gemini:Model.</summary>
        public string NormalizerFallbackModel { get; set; } = string.Empty;

        public string NormalizerThinkingLevel { get; set; } = "minimal";
        public int NormalizeAttemptTimeoutSeconds { get; set; } = 8;
        public int NormalizeTimeoutSeconds { get; set; } = 14;

        /// <summary>Bản chuẩn hoá lệch độ dài quá tỉ lệ này so với bản thô = sửa quá tay, dùng lại bản thô.</summary>
        public double MaxLengthChangeRatio { get; set; } = 0.3;
        #endregion
    }
}
