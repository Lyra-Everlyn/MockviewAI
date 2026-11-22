namespace MockviewAI.Services.Exceptions
{
    /// <summary>
    /// [AI-SCORING] Lỗi của bộ chấm AI; message tiếng Việt, an toàn để hiện cho người dùng.
    /// </summary>
    public class AiScoringException : Exception
    {
        #region Constructor
        public AiScoringException(string message, Exception? inner = null) : base(message, inner) { }

        public AiScoringException(string message, bool isConfigurationError) : base(message)
        {
            IsConfigurationError = isConfigurationError;
        }
        #endregion

        #region Phân loại lỗi
        /// <summary>Lỗi cấu hình (sai key, sai tên model): bấm "Chấm lại" vô ích, phải sửa cấu hình.</summary>
        public bool IsConfigurationError { get; }

        /// <summary>Lỗi tạm thời (503, 429, mất mạng, quá giờ): thử model dự phòng hoặc thử lại sau.</summary>
        public bool IsTransient { get; init; }

        /// <summary>Model này không nhận yêu cầu (400) hoặc không tồn tại (404): thử model dự phòng khác.</summary>
        public bool CanTryOtherModel { get; init; }
        #endregion
    }
}
