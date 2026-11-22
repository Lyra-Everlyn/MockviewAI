using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MockviewAI.Models.Entities
{
    /// <summary>
    /// [AUTH] [ONBOARDING] [PROFILE] Tài khoản người dùng (bảng Users).
    /// </summary>
    public class User
    {
        #region Định danh và đăng nhập
        [Key]
        public int Id { get; set; }

        /// <summary>Email đăng nhập, lưu chữ thường. Chưa có unique index ở database.</summary>
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        /// <summary>Mật khẩu băm BCrypt; tài khoản tạo qua Google mang băm của một chuỗi ngẫu nhiên.</summary>
        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>Quyền: "User" hoặc "Admin".</summary>
        [Required]
        [MaxLength(30)]
        public string Role { get; set; } = "User";

        /// <summary>Trạng thái: Active | Locked (bị khoá) | Inactive (đình chỉ tạm thời).</summary>
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Active";
        #endregion

        #region Thông tin cá nhân
        /// <summary>Tên (bắt buộc).</summary>
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>Họ và tên đệm.</summary>
        [MaxLength(50)]
        public string? LastName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        public DateTime? DateOfBirth { get; set; }

        /// <summary>Link ảnh đại diện (Cloudinary hoặc ảnh Google).</summary>
        public string? AvatarUrl { get; set; }
        #endregion

        #region Hồ sơ nghề nghiệp (Onboarding)
        /// <summary>Ngành học.</summary>
        [MaxLength(100)]
        public string? Major { get; set; }

        /// <summary>Vị trí muốn ứng tuyển.</summary>
        [MaxLength(100)]
        public string? TargetPosition { get; set; }

        /// <summary>Tình trạng tốt nghiệp.</summary>
        [MaxLength(50)]
        public string? GraduationStatus { get; set; }

        /// <summary>Mức kinh nghiệm.</summary>
        [MaxLength(50)]
        public string? ExperienceLevel { get; set; }

        /// <summary>false: lần đăng nhập sau sẽ chuyển tới trang Onboarding.</summary>
        public bool IsOnboardingCompleted { get; set; } = false;
        #endregion

        #region Mốc thời gian (UTC)
        [Precision(3)]
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;

        [Precision(3)]
        public DateTime? UpdateAt { get; set; }

        /// <summary>Lúc đồng ý Điều khoản và Chính sách bảo mật (ghi khi tạo tài khoản).</summary>
        [Precision(3)]
        public DateTime? ConsentAt { get; set; }

        [Precision(3)]
        public DateTime? LastLoginAt { get; set; }
        #endregion
    }
}
