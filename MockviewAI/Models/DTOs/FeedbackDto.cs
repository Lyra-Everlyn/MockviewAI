using System.ComponentModel.DataAnnotations;

namespace MockviewAI.Models.DTOs
{
    /// <summary>
    /// [FEEDBACK] 
    /// Dữ liệu form Góp ý (POST /Feedback).
    /// </summary>
    public class FeedbackDto
    {
        /// <summary>Mức dễ dùng, 1–5 sao.</summary>
        [Range(1, 5, ErrorMessage = "Vui lòng chọn từ 1 đến 5 sao.")]
        public int EaseRating { get; set; }

        /// <summary>Có | Một chút | Không.</summary>
        [Required(ErrorMessage = "Vui lòng chọn mức độ bớt lo lắng.")]
        [RegularExpression("^(Có|Một chút|Không)$", ErrorMessage = "Lựa chọn không hợp lệ.")]
        public string AnxietyHelp { get; set; } = string.Empty;

        /// <summary>Góp ý tự do, tối đa 1000 ký tự.</summary>
        [StringLength(1000, ErrorMessage = "Góp ý tối đa 1000 ký tự.")]
        public string? Comment { get; set; }
    }
}
