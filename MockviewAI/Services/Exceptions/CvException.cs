namespace MockviewAI.Services.Exceptions
{
    /// <summary>
    /// [USER CV] 
    /// Lỗi xử lý CV; message tiếng Việt, an toàn để hiện cho người dùng.
    /// </summary>
    public class CvException : Exception
    {
        public CvException(string message, Exception? inner = null) : base(message, inner) { }

        /// <summary>Lỗi tạm thời: có thể thử model khác hoặc thử lại.</summary>
        public bool IsTransient { get; init; }

        /// <summary>Mã HTTP nhà cung cấp trả về (0 = không có).</summary>
        public int StatusCode { get; init; }
    }
}
