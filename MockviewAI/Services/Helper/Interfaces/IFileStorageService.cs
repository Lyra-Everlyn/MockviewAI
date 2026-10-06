using Microsoft.AspNetCore.Http;

namespace MockviewAI.Services.Helper.Interfaces
{
    public interface IFileStorageService
    {
        Task<(string Url, string PublicId)> UploadFileAsync(IFormFile file, string folderName = "uploads", UploadType type = UploadType.General);

        Task<bool> DeleteFileAsync(string publicId);
    }
}