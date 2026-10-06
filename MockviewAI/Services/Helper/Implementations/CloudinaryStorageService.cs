using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using MockviewAI.Services.Helper.Interfaces;

namespace MockviewAI.Services.Implementations
{
    public class CloudinaryStorageService : IFileStorageService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryStorageService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        // Upload file to Cloudinary and return the URL and publicId
        public async Task<(string Url, string PublicId)> UploadFileAsync(IFormFile file, string folderName = "uploads", UploadType type = UploadType.General)
        {
            if (file == null || file.Length == 0) return (string.Empty, string.Empty);

            using var stream = file.OpenReadStream();

            // 1. Only process image files
            if (file.ContentType.StartsWith("image/"))
            {
                var imageParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folderName
                };

                switch (type)
                {
                    case UploadType.Avatar:
                        imageParams.Transformation = new Transformation().Height(500).Width(500).Crop("fill").Gravity("face");
                        break;
                    case UploadType.Banner:
                        imageParams.Transformation = new Transformation().Height(400).Width(1200).Crop("fill");
                        break;
                    case UploadType.Thumbnail:
                        imageParams.Transformation = new Transformation().Height(250).Width(250).Crop("fill");
                        break;
                    case UploadType.General:
                    default:
                        break;
                }

                var imageResult = await _cloudinary.UploadAsync(imageParams);
                if (imageResult.Error != null) throw new Exception(imageResult.Error.Message);
                return (imageResult.SecureUrl.ToString(), imageResult.PublicId);
            }

            // 2. Other file type 
            var rawParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folderName
            };

            var rawResult = await _cloudinary.UploadAsync(rawParams);
            if (rawResult.Error != null) throw new Exception(rawResult.Error.Message);

            return (rawResult.SecureUrl.ToString(), rawResult.PublicId);
        }

        // Delete file from Cloudinary by publicId
        public async Task<bool> DeleteFileAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);

            return result.Result == "ok";
        }

        // Usage
        // var (url, publicId) = await _fileStorageService.UploadFileAsync(file, "avatars", UploadType.Avatar);
        // var isDeleted = await _fileStorageService.DeleteFileAsync(publicId);
    }
}