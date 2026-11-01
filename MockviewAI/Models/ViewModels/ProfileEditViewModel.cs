using System.ComponentModel.DataAnnotations;

namespace MockviewAI.Models.ViewModels
{
    /// [PROFILE]
    public class ProfileEditViewModel
    {
        public IFormFile? AvatarFile { get; set; }

        [StringLength(50, ErrorMessage = "Họ tối đa 50 ký tự.")]
        public string? LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên.")]
        [StringLength(50, ErrorMessage = "Tên tối đa 50 ký tự.")]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(100)]
        public string Major { get; set; } = string.Empty;

        [StringLength(100)]
        public string TargetPosition { get; set; } = string.Empty;

        [StringLength(50)]
        public string GraduationStatus { get; set; } = string.Empty;

        [StringLength(50)]
        public string ExperienceLevel { get; set; } = string.Empty;
    }
}