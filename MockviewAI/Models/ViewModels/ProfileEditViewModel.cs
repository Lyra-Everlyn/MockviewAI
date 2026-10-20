using System.ComponentModel.DataAnnotations;

namespace MockviewAI.Models.ViewModels
{
    /// [PROFILE]
    public class ProfileEditViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự.")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(100)]
        public string Major { get; set; } = string.Empty;

        [StringLength(100)]
        public string TargetPosition { get; set; } = string.Empty; // Đổi tên từ Position

        [StringLength(50)]
        public string GraduationStatus { get; set; } = string.Empty; // Đổi tên từ StudentStatus

        [StringLength(50)]
        public string ExperienceLevel { get; set; } = string.Empty; // Bổ sung mới
    }
}