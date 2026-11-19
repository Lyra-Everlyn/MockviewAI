namespace MockviewAI.Services.Exceptions
{
    /// <summary>
    /// [USER CV] Lỗi xử lý CV, message an toàn để hiển thị cho người dùng.
    /// </summary>
    public class CvException : Exception
    {
        public CvException(string message, Exception? inner = null) : base(message, inner) { }

        /// <summary>Lỗi tạm thời: có thể thử model khác hoặc thử lại.</summary>
        public bool IsTransient { get; init; }

        public int StatusCode { get; init; }
    }
}
