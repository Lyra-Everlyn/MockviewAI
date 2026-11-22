using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using MockviewAI.Services.FileStorage.Interfaces;

namespace MockviewAI.Services.FileStorage.Implementations
{
    /// <summary>
    /// [FILE-STORAGE] 
    /// Lưu file trên Cloudinary. Tài khoản Cloudinary đăng ký ở Program.cs (CloudinarySettings__*).
    /// </summary>
    public class CloudinaryStorageService : IFileStorageService
    {
        #region Fields
        private readonly Cloudinary _cloudinary;
        #endregion

        #region Constructor
        public CloudinaryStorageService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }
        #endregion

        #region IFileStorageService
        public async Task<(string Url, string PublicId)> UploadFileAsync(IFormFile file, string folderName = "uploads", UploadType type = UploadType.General)
        {
            if (file == null || file.Length == 0) return (string.Empty, string.Empty);

            using var stream = file.OpenReadStream();

            if (file.ContentType.StartsWith("image/"))
            {
                var imageParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folderName,
                    Transformation = CropFor(type)
                };

                var imageResult = await _cloudinary.UploadAsync(imageParams);
                if (imageResult.Error != null) throw new Exception(imageResult.Error.Message);
                return (imageResult.SecureUrl.ToString(), imageResult.PublicId);
            }

            // File không phải ảnh: lưu nguyên dạng (raw)
            var rawParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folderName
            };

            var rawResult = await _cloudinary.UploadAsync(rawParams);
            if (rawResult.Error != null) throw new Exception(rawResult.Error.Message);

            return (rawResult.SecureUrl.ToString(), rawResult.PublicId);
        }

        public async Task<bool> DeleteFileAsync(string publicId)
        {
            var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));
            return result.Result == "ok";
        }
        #endregion

        #region Helpers
        /// <summary>Kiểu cắt ảnh theo mục đích; null = giữ nguyên.</summary>
        private static Transformation? CropFor(UploadType type) => type switch
        {
            UploadType.Avatar => new Transformation().Height(500).Width(500).Crop("fill").Gravity("face"),
            UploadType.Banner => new Transformation().Height(400).Width(1200).Crop("fill"),
            UploadType.Thumbnail => new Transformation().Height(250).Width(250).Crop("fill"),
            _ => null
        };
        #endregion
    }
}
