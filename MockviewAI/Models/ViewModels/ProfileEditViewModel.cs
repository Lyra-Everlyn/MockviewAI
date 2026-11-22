using System.ComponentModel.DataAnnotations;

namespace MockviewAI.Models.ViewModels
{
    /// <summary>
    /// [PROFILE] 
    /// Form chỉnh sửa hồ sơ (Views/Profile/EditProfile.cshtml, POST /Profile/Edit).
    /// </summary>
    public class ProfileEditViewModel
    {
        /// <summary>Ảnh đại diện mới (tải lên Cloudinary); null = giữ ảnh cũ.</summary>
        public IFormFile? AvatarFile { get; set; }

        /// <summary>Họ và tên đệm.</summary>
        [StringLength(50, ErrorMessage = "Họ tối đa 50 ký tự.")]
        public string? LastName { get; set; } = string.Empty;

        /// <summary>Tên (bắt buộc).</summary>
        [Required(ErrorMessage = "Vui lòng nhập tên.")]
        [StringLength(50, ErrorMessage = "Tên tối đa 50 ký tự.")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>Ngành học.</summary>
        [StringLength(100)]
        public string Major { get; set; } = string.Empty;

        /// <summary>Vị trí muốn ứng tuyển.</summary>
        [StringLength(100)]
        public string TargetPosition { get; set; } = string.Empty;

        /// <summary>Tình trạng tốt nghiệp.</summary>
        [StringLength(50)]
        public string GraduationStatus { get; set; } = string.Empty;

        /// <summary>Mức kinh nghiệm.</summary>
        [StringLength(50)]
        public string ExperienceLevel { get; set; } = string.Empty;
    }
}
