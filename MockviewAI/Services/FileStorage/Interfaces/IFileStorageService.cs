namespace MockviewAI.Services.FileStorage.Interfaces
{
    /// <summary>
    /// [FILE-STORAGE] 
    /// Lưu và xoá file trên dịch vụ lưu trữ ngoài (hiện là Cloudinary).
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>Tải file lên; ảnh được cắt theo <paramref name="type"/>, file khác lưu nguyên dạng.</summary>
        /// <returns>(Url, PublicId); cả hai rỗng nếu file trống.</returns>
        Task<(string Url, string PublicId)> UploadFileAsync(IFormFile file, string folderName = "uploads", UploadType type = UploadType.General);

        /// <returns>true = đã xoá.</returns>
        Task<bool> DeleteFileAsync(string publicId);
    }
}
