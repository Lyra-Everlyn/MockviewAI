using System.ComponentModel.DataAnnotations;

namespace MockviewAI.Models.DTOs
{
    // Dữ liệu form Góp ý gửi lên. Tên thuộc tính khớp với name="" trong Feedback.cshtml.
    public class FeedbackDto
    {
        [Range(1, 5, ErrorMessage = "Vui lòng chọn từ 1 đến 5 sao.")]
        public int EaseRating { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn mức độ bớt lo lắng.")]
        [RegularExpression("^(Có|Một chút|Không)$", ErrorMessage = "Lựa chọn không hợp lệ.")]
        public string AnxietyHelp { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Góp ý tối đa 1000 ký tự.")]
        public string? Comment { get; set; }
    }
}
