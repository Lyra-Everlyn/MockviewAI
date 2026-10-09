using Microsoft.EntityFrameworkCore;
using MockviewAI.Data;
using MockviewAI.Models.Entities;
using MockviewAI.Repositories.Interfaces;

namespace MockviewAI.Repositories.Implementations
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return await _dbSet.AnyAsync(u => u.Email == email);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
