using MockviewAI.Models.Entities;

namespace MockviewAI.Repositories.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<bool> EmailExistsAsync(string email);
        Task<User?> GetByEmailAsync(string email);   // one indexed query instead of loading every user
    }
}
