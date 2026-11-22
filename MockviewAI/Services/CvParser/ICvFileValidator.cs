namespace MockviewAI.Services.CvParser
{
    /// <summary>
    /// [USER CV] 
    /// Kiểm tra file CV tải lên. Trả về thông báo lỗi, hoặc null nếu hợp lệ.
    /// </summary>
    public interface ICvFileValidator
    {
        Task<string?> ValidateAsync(IFormFile? file, CancellationToken ct = default);
    }
}
