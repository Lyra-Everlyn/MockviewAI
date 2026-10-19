using System.ComponentModel.DataAnnotations;

namespace MockviewAI.Models.ViewModels
{
    // Dữ liệu form Sửa hồ sơ (EditProfile.cshtml). Tên thuộc tính khớp với name="" trong form.
    public class ProfileEditViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự.")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(100)]
        public string Major { get; set; } = string.Empty;

        [StringLength(100)]
        public string Position { get; set; } = string.Empty;

        [StringLength(50)]
        public string StudentStatus { get; set; } = string.Empty;
    }
}
