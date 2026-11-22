using System.ComponentModel.DataAnnotations;

namespace MockviewAI.Models.DTOs
{
    /// <summary>
    /// [ONBOARDING]
    /// Hồ sơ ban đầu người dùng điền sau lần đăng nhập đầu tiên (POST /Onboarding/Complete).
    /// </summary>
    public class OnboardingDto
    {
        /// <summary>Ngành học.</summary>
        [Required(ErrorMessage = "Vui lòng nhập ngành học.")]
        [MaxLength(100)]
        public string Major { get; set; } = string.Empty;

        /// <summary>Vị trí muốn ứng tuyển.</summary>
        [Required(ErrorMessage = "Vui lòng nhập vị trí muốn ứng tuyển.")]
        [MaxLength(100)]
        public string TargetPosition { get; set; } = string.Empty;

        /// <summary>Tình trạng tốt nghiệp.</summary>
        [Required(ErrorMessage = "Vui lòng chọn tình trạng tốt nghiệp.")]
        [MaxLength(50)]
        public string GraduationStatus { get; set; } = string.Empty;

        /// <summary>Mức kinh nghiệm.</summary>
        [Required(ErrorMessage = "Vui lòng chọn mức kinh nghiệm.")]
        [MaxLength(50)]
        public string ExperienceLevel { get; set; } = string.Empty;
    }
}
