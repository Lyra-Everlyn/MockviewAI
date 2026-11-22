using System.ComponentModel.DataAnnotations;

namespace MockviewAI.Models.Entities
{
    /// <summary>
    /// [FEEDBACK] 
    /// Góp ý của người dùng về ứng dụng.
    /// </summary>
    public class Feedback
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        /// <summary>"Ứng dụng có dễ dùng không?" – số sao 1–5.</summary>
        public int EaseRating { get; set; }

        /// <summary>"Buổi luyện có giúp bạn bớt lo lắng không?" – Có | Một chút | Không.</summary>
        [Required]
        [MaxLength(20)]
        public string AnxietyHelp { get; set; } = string.Empty;

        /// <summary>"Bạn muốn nhóm cải thiện điều gì?" (không bắt buộc).</summary>
        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
