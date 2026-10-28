using System.ComponentModel.DataAnnotations;

namespace MockviewAI.Models.Entities
{
    /// <summary>
    /// [FEEDBACK]
    /// -- Phản hồi của người dùng dành cho ứng dụng
    /// </summary>

    public class Feedback
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        // "Ứng dụng có dễ dùng không?" - số sao 1..5
        public int EaseRating { get; set; }

        // "Buổi luyện có giúp bạn bớt lo lắng không?" - Có / Một chút / Không
        [Required]
        [MaxLength(20)]
        public string AnxietyHelp { get; set; } = string.Empty;

        // "Bạn muốn nhóm cải thiện điều gì?"
        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
