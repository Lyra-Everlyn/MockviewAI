using MockviewAI.Models.Entities;

namespace MockviewAI.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> EmailExistsAsync(string email)
    }
}
