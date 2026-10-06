using MockviewAI.Models.Entities;
using MockviewAI.Repositories.Interfaces;
using MockviewAI.Services.Interfaces;

namespace MockviewAI.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<User> _userRepository;

        public AuthService(IRepository<User> userRepository)
        {
            _userRepository = userRepository;
        }


        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            var users = await _userRepository.GetAllAsync();
            var user = users.FirstOrDefault(u => u.Email == email);

            if (user == null) { throw new Exception("Account not found."); }
            if (user.Status == "Locked") { throw new Exception("Your account has been locked."); }

            bool isCorrectPasswords = await VerifyPasswordHashAsync(password, user.PasswordHash);

            if (user.Status == "Inactive") { throw new Exception("Your account has been temporarily suspended."); }
            if (!isCorrectPasswords) { throw new Exception("Incorrect password"); }

            return user;
        }

        public async Task<User> AuthenticateGoogleUserAsync(string email, string fullName)
        {
            var users = await _userRepository.GetAllAsync();
            var existingUser = users.FirstOrDefault(u => u.Email == email);

            if (existingUser == null) { throw new Exception("Account not found."); }
            if (existingUser.Status == "Locked") { throw new Exception("Your account has been locked."); }
            if (existingUser.Status == "Inactive") { throw new Exception("Your account has been temporarily suspended."); }

            // Update the user's full name if it's different
            if (existingUser.FirstName == null || existingUser.LastName == null)
            {
                var names = fullName.Split(' ');
                existingUser.FirstName = names[0];
                existingUser.LastName = names.Length > 1 ? string.Join(" ", names.Skip(1)) : string.Empty;
            }

            return existingUser;
        }

        private async Task<bool> VerifyPasswordHashAsync(string inputPassword, string storedHash)
        {
            // Implement your password hash verification logic here
            // For example, you can use a hashing algorithm like BCrypt or PBKDF2
            // This is a placeholder implementation and should be replaced with actual logic
            return await Task.FromResult(inputPassword == storedHash);
        }
    }
}