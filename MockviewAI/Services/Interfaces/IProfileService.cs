using MockviewAI.Models.ViewModels;

namespace MockviewAI.Services.Interfaces
{
    // Hồ sơ người dùng: xem và sửa.
    public interface IProfileService
    {
        Task<ProfileViewModel?> GetProfileAsync(string email);
        Task<ProfileEditViewModel?> GetEditAsync(string email);

        // Trả về họ tên mới để cập nhật lại cookie đăng nhập; null nếu không tìm thấy tài khoản
        Task<string?> UpdateAsync(string email, ProfileEditViewModel model);
    }
}
