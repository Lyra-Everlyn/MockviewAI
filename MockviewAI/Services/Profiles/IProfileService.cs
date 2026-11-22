using MockviewAI.Models.ViewModels;

namespace MockviewAI.Services.Profiles
{
    /// <summary>
    /// [PROFILE] 
    /// Xem và chỉnh sửa hồ sơ. Mọi hàm trả null khi không tìm thấy tài khoản.
    /// </summary>
    public interface IProfileService
    {
        /// <summary>Dữ liệu trang Hồ sơ, kèm 3 buổi luyện gần nhất.</summary>
        Task<ProfileViewModel?> GetProfileAsync(string email);

        /// <summary>Dữ liệu điền sẵn vào form chỉnh sửa.</summary>
        Task<ProfileEditViewModel?> GetEditAsync(string email);

        /// <summary>Lưu hồ sơ (và ảnh đại diện nếu có).</summary>
        /// <returns>Tên đầy đủ và link ảnh mới, để controller cập nhật cookie đăng nhập.</returns>
        Task<(string FullName, string? AvatarUrl)?> UpdateAsync(string email, ProfileEditViewModel model);
    }
}
