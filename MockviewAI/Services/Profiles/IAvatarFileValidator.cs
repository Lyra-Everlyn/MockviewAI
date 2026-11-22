namespace MockviewAI.Services.Profiles
{
    /// <summary>
    /// [PROFILE] 
    /// Kiểm tra file ảnh đại diện trước khi tải lên Cloudinary.
    /// </summary>
    public interface IAvatarFileValidator
    {
        /// <returns>Thông báo lỗi tiếng Việt, hoặc null nếu hợp lệ.</returns>
        Task<string?> ValidateAsync(IFormFile file, CancellationToken ct = default);
    }
}
