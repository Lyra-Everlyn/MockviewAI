using Microsoft.EntityFrameworkCore;
using MockviewAI.Models.Entities;

namespace MockviewAI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }


        public DbSet<User> Users { get; set; }

        // [AI-INTERVIEW] buổi luyện phỏng vấn và câu trả lời
        public DbSet<InterviewSession> InterviewSessions { get; set; }
        public DbSet<SessionAnswer> SessionAnswers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Xóa buổi luyện thì xóa luôn các câu trả lời của buổi đó
            modelBuilder.Entity<InterviewSession>()
                .HasMany(s => s.Answers)
                .WithOne(a => a.Session)
                .HasForeignKey(a => a.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Xóa người dùng thì xóa luôn các buổi luyện của họ
            modelBuilder.Entity<InterviewSession>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Tìm lịch sử theo người dùng nhanh hơn
            modelBuilder.Entity<InterviewSession>().HasIndex(s => s.UserId);
        }
    }
}
