using System.ComponentModel.DataAnnotations;

namespace MockviewAI.Models.DTOs
{
    public class OnboardingDto
    {
        [Required(ErrorMessage = "Please enter your major or field of study.")]
        [MaxLength(100)]
        public string Major { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your target position.")]
        [MaxLength(100)]
        public string TargetPosition { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select your graduation status.")]
        [MaxLength(50)]
        public string GraduationStatus { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select your experience level.")]
        [MaxLength(50)]
        public string ExperienceLevel { get; set; } = string.Empty;
    }
}
