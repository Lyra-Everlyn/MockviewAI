namespace MockviewAI.Models
{
    /// <summary>
    /// [SYSTEM] 
    /// Dữ liệu trang lỗi chung (Views/Shared/Error.cshtml).
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>Mã truy vết của request, để đối chiếu với log.</summary>
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
