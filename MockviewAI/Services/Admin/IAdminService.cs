using MockviewAI.Models.ViewModels.Admin;

namespace MockviewAI.Services.Admin
{
    /// <summary>
    /// [ADMIN] 
    /// Số liệu cho trang quản trị. Chỉ đọc: không có thao tác thêm / sửa / xoá.
    /// </summary>
    public interface IAdminService
    {
        Task<AdminDashboardViewModel> GetDashboardAsync();

        /// <param name="search">Tìm theo email hoặc tên (tối đa 100 ký tự).</param>
        /// <param name="status">Active / Locked / Inactive; giá trị khác = không lọc.</param>
        Task<AdminUserListViewModel> GetUsersAsync(string? search, string? status, int page);

        /// <param name="rating">1–5; giá trị khác = không lọc.</param>
        /// <param name="onlyWithComment">Chỉ lấy góp ý có nội dung.</param>
        Task<AdminFeedbackListViewModel> GetFeedbacksAsync(int? rating, bool onlyWithComment, int page);
    }
}
