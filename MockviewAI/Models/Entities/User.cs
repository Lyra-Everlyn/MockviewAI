using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MockviewAI.Models.Entities
{
    public class User
    {
        [Key] 
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }
        public string? AvatarUrl { get; set; }

        [Required]
        [MaxLength(30)]
        public string Role { get; set; } = "User";

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Active";

        [Precision(3)]
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;

        [Precision(3)]
        public DateTime? UpdateAt { get; set; }

        [Precision(3)]
        public DateTime? ConsentAt { get; set; }

        [Precision(3)]
        public DateTime? LastLoginAt { get; set; }

        [MaxLength(100)]
        public string? Major { get; set; }

        [MaxLength(100)]
        public string? TargetPosition { get; set; }
    }
}
