namespace MockviewAI.Models
{
    /// <summary>
    /// [KHÔNG DÙNG] [SYSTEM] 
    /// Dữ liệu trang lỗi chung (Views/Shared/Error.cshtml).
    /// Chỉ còn HomeController.Error dùng; trang lỗi thực tế do ErrorHandlingMiddleware dựng.
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>Mã truy vết của request, để đối chiếu với log.</summary>
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}