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
    }
}
