using MockviewAI.Models.Entities;
using MockviewAI.Models.ViewModels;
using MockviewAI.Repositories.Interfaces;
using MockviewAI.Services.FileStorage.Interfaces;
using MockviewAI.Services.SessionHistory;

namespace MockviewAI.Services.Profiles
{
    /// <summary>
    /// [PROFILE] 
    /// Xem và chỉnh sửa hồ sơ; ảnh đại diện lưu trên Cloudinary (file đã qua IAvatarFileValidator ở controller).
    /// </summary>
    public class ProfileService : IProfileService
    {
        #region Fields
        private const int RecentSessionCount = 3;

        /// <summary>Mã tình trạng tốt nghiệp (lưu trong DB) -> chữ hiển thị.</summary>
        private static readonly Dictionary<string, string> StatusTexts = new()
        {
            ["Current Student"] = "Đang đi học",
            ["Expecting Graduation"] = "Sắp ra trường",
            ["Graduated"] = "Đã ra trường",
            ["Other"] = "Khác"
        };

        private readonly IUserRepository _userRepository;
        private readonly ISessionHistoryService _history;
        private readonly IFileStorageService _fileStorage;
        #endregion

        #region Constructor
        public ProfileService(IUserRepository userRepository, ISessionHistoryService history, IFileStorageService fileStorage)
        {
            _userRepository = userRepository;
            _history = history;
            _fileStorage = fileStorage;
        }
        #endregion

        #region Xem hồ sơ
        public async Task<ProfileViewModel?> GetProfileAsync(string email)
        {
            var user = await FindUserAsync(email);
            if (user == null) return null;

            string fullName = FullName(user);
            return new ProfileViewModel
            {
                FullName = fullName,
                AvatarInitial = fullName.Length > 0 ? fullName[..1].ToUpper() : "?",
                StatusText = StatusTexts.TryGetValue(user.GraduationStatus ?? "", out var text) ? text : (user.GraduationStatus ?? ""),
                Major = user.Major ?? string.Empty,
                TargetPosition = user.TargetPosition ?? string.Empty,
                ExperienceLevel = user.ExperienceLevel ?? string.Empty,
                RecentSessions = await _history.GetHistoryAsync(email, RecentSessionCount)
            };
        }
        #endregion

        #region Chỉnh sửa hồ sơ
        public async Task<ProfileEditViewModel?> GetEditAsync(string email)
        {
            var user = await FindUserAsync(email);
            if (user == null) return null;

            return new ProfileEditViewModel
            {
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Major = user.Major ?? string.Empty,
                TargetPosition = user.TargetPosition ?? string.Empty,
                GraduationStatus = user.GraduationStatus ?? string.Empty,
                ExperienceLevel = user.ExperienceLevel ?? string.Empty
            };
        }

        public async Task<(string FullName, string? AvatarUrl)?> UpdateAsync(string email, ProfileEditViewModel model)
        {
            var user = await FindUserAsync(email);
            if (user == null) return null;

            // Tải ảnh mới TRƯỚC; tải lỗi thì ném lỗi ra và hồ sơ vẫn giữ ảnh cũ còn nguyên
            string? replacedAvatarUrl = null;
            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                var (url, _) = await _fileStorage.UploadFileAsync(model.AvatarFile, "MockviewAI/Avatars", UploadType.Avatar);
                if (!string.IsNullOrEmpty(url))
                {
                    replacedAvatarUrl = user.AvatarUrl;
                    user.AvatarUrl = url;
                }
            }

            user.FirstName = (model.FirstName ?? "").Trim();
            user.LastName = (model.LastName ?? "").Trim();
            user.Major = (model.Major ?? "").Trim();
            user.TargetPosition = (model.TargetPosition ?? "").Trim();
            user.GraduationStatus = (model.GraduationStatus ?? "").Trim();
            user.ExperienceLevel = (model.ExperienceLevel ?? "").Trim();
            user.UpdateAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            // Hồ sơ đã trỏ sang ảnh mới mới xoá ảnh cũ
            if (replacedAvatarUrl != null)
                await DeleteOldCloudinaryAvatarAsync(replacedAvatarUrl);

            return (FullName(user), user.AvatarUrl);
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Xoá ảnh cũ trên Cloudinary (ảnh Google thì bỏ qua). Tách publicId từ URL:
        /// .../upload/v123456/MockviewAI/Avatars/abc.jpg -> MockviewAI/Avatars/abc.
        /// </summary>
        private async Task DeleteOldCloudinaryAvatarAsync(string? avatarUrl)
        {
            if (string.IsNullOrEmpty(avatarUrl) || !avatarUrl.Contains("cloudinary.com/"))
                return;

            try
            {
                int uploadIndex = avatarUrl.IndexOf("/upload/");
                if (uploadIndex > 0)
                {
                    string afterUpload = avatarUrl.Substring(uploadIndex + 8);
                    var parts = afterUpload.Split('/');

                    // Bỏ đoạn phiên bản "v123456" nếu có
                    if (parts.Length > 1 && parts[0].StartsWith("v") && parts[0].Length > 1 && char.IsDigit(parts[0][1]))
                    {
                        afterUpload = string.Join("/", parts.Skip(1));
                    }

                    int lastDot = afterUpload.LastIndexOf('.');
                    string publicId = lastDot > 0 ? afterUpload.Substring(0, lastDot) : afterUpload;

                    await _fileStorage.DeleteFileAsync(publicId);
                }
            }
            catch
            {
                // Xoá ảnh cũ là việc phụ: lỗi thì bỏ qua, không chặn việc lưu hồ sơ
            }
        }

        private async Task<User?> FindUserAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            return await _userRepository.GetByEmailAsync(email.Trim().ToLowerInvariant());
        }

        /// <summary>Họ + tên theo kiểu Việt Nam.</summary>
        private static string FullName(User user) => (user.LastName + " " + user.FirstName).Trim();
        #endregion
    }
}
