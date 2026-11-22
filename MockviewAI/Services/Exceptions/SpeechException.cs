namespace MockviewAI.Services.Exceptions
{
    /// <summary>
    /// [SPEECH] 
    /// Lỗi chuyển giọng nói thành chữ; message tiếng Việt, không bao giờ chứa API key hay lỗi thô của nhà cung cấp.
    /// </summary>
    public class SpeechException : Exception
    {
        #region Constructor
        public SpeechException(string message, Exception? inner = null) : base(message, inner) { }
        #endregion

        #region Phân loại lỗi
        /// <summary>Lỗi cấu hình (thiếu / sai key, sai model): thử lại vô ích, quản trị viên phải sửa.</summary>
        public bool IsConfigurationError { get; init; }

        /// <summary>Lỗi tạm thời (quá tải, hết lượt, mất mạng, quá giờ): người dùng thử lại sau ít giây.</summary>
        public bool IsTransient { get; init; }

        /// <summary>Mã HTTP nhà cung cấp trả về (0 = không có, vd mất mạng). Chỉ dùng nội bộ để quyết định thử lại.</summary>
        public int StatusCode { get; init; }
        #endregion
    }
}
