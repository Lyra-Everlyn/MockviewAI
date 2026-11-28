namespace MockviewAI.Services.Exceptions
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Lỗi khi soạn / kiểm duyệt câu hỏi theo CV. Không bao giờ tới người dùng:
    /// CvQuestionPipeline bắt lại và quay về câu bộ đề.
    /// </summary>
    public class CvQuestionException : Exception
    {
        public CvQuestionException(string message, Exception? inner = null) : base(message, inner) { }

        /// <summary>Lỗi tạm thời (quá tải, mạng, model không có): có thể thử model khác.</summary>
        public bool IsTransient { get; init; }

        /// <summary>Mã HTTP nhà cung cấp trả về (0 = không có).</summary>
        public int StatusCode { get; init; }
    }
}
