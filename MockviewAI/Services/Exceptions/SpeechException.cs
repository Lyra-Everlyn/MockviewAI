namespace MockviewAI.Services.Exceptions
{
    /// <summary>
    /// [SPEECH]
    /// -- Lỗi khi chuyển giọng nói thành chữ, có thông báo tiếng Việt an toàn để hiển thị cho người dùng.
    /// -- Không bao giờ chứa API key hay nội dung lỗi thô của nhà cung cấp.
    /// </summary>
    public class SpeechException : Exception
    {
        public SpeechException(string message, Exception? inner = null) : base(message, inner) { }

        /// <summary>
        /// true = lỗi do cấu hình (thiếu / sai key, sai tên model): thử lại cũng không hết, quản trị viên phải sửa cấu hình.
        /// </summary>
        public bool IsConfigurationError { get; init; }

        /// <summary>
        /// true = lỗi tạm thời (quá tải, hết lượt, mất mạng, hết giờ chờ): người dùng có thể thử lại sau ít giây.
        /// </summary>
        public bool IsTransient { get; init; }

        /// <summary>Mã HTTP nhà cung cấp trả về (0 = không có, vd mất mạng / quá giờ). Chỉ dùng nội bộ để quyết định thử lại.</summary>
        public int StatusCode { get; init; }
    }
}
