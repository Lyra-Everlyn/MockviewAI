using MockviewAI.Models.Entities;

namespace MockviewAI.Repositories.Interfaces
{
    /// <summary>
    /// [DATA] 
    /// Truy vấn riêng cho bảng User. Email truyền vào phải đã chuẩn hoá (trim, chữ thường) như lúc lưu.
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        /// <summary>Email đã có tài khoản chưa; email rỗng trả false.</summary>
        Task<bool> EmailExistsAsync(string email);

        /// <summary>Tài khoản theo email (được theo dõi để sửa); không có hoặc email rỗng trả null.</summary>
        Task<User?> GetByEmailAsync(string email);
    }
}
