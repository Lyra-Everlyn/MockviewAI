using BCrypt.Net;
using MockviewAI.Models.Entities;
using MockviewAI.Repositories.Interfaces;
using MockviewAI.Services.Interfaces;

namespace MockviewAI.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task RegisterAsync(string email, string password, string firstName, string lastName)
        {
            bool isEmailExist = await _userRepository.EmailExistsAsync(email);
            if (isEmailExist)
            {
                throw new Exception("Email has already been registered.");
            }

            string passwordHash = await HashPasswordAsync(password);
            var newUser = new User
            {
                Email = email,
                PasswordHash = passwordHash,
                FirstName = firstName,
                LastName = lastName,
                Role = "User",
                Status = "Active",
                CreateAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(newUser);
        }

        public async Task<User> RegisterGoogleAsync(string email, string firstName, string lastName, string? avatarUrl)
        {
            string dummyPassword = Guid.NewGuid().ToString();
            string dummyPasswordHash = await HashPasswordAsync(dummyPassword);

            var newUser = new User
            {
                Email = email,
                PasswordHash = dummyPasswordHash,
                FirstName = firstName,
                LastName = lastName,
                AvatarUrl = avatarUrl,
                Role = "User",
                Status = "Active",
                CreateAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(newUser);
            return newUser;
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

        public async Task<User> AuthenticateGoogleUserAsync(string email, string firstName, string lastName, string? avatarUrl)
        {
            var users = await _userRepository.GetAllAsync();
            var existingUser = users.FirstOrDefault(u => u.Email == email);

            // NOTE: Auto create a new user if the Google account is not found in the database
            if (existingUser == null)
            {
                return await RegisterGoogleAsync(email, firstName, lastName, avatarUrl);
            }

            if (existingUser.Status == "Locked") { throw new Exception("Your account has been locked."); }
            if (existingUser.Status == "Inactive") { throw new Exception("Your account has been temporarily suspended."); }

            bool isUpdated = false;
            if (!string.IsNullOrEmpty(avatarUrl) && existingUser.AvatarUrl != avatarUrl)
            {
                existingUser.AvatarUrl = avatarUrl;
                isUpdated = true;
            }

            if (string.IsNullOrWhiteSpace(existingUser.FirstName) && string.IsNullOrWhiteSpace(existingUser.LastName))
            {
                if (!string.IsNullOrWhiteSpace(firstName) || !string.IsNullOrWhiteSpace(lastName))
                {
                    existingUser.FirstName = !string.IsNullOrWhiteSpace(firstName) ? firstName.Trim() : "Google";
                    existingUser.LastName = !string.IsNullOrWhiteSpace(lastName) ? lastName.Trim() : "User";
                }
                else
                {
                    string emailName = email.Split('@')[0];
                    existingUser.FirstName = char.ToUpper(emailName[0]) + emailName.Substring(1);
                    existingUser.LastName = "User";
                }

                isUpdated = true;
            }

            if (isUpdated)
            {
                await _userRepository.UpdateAsync(existingUser);
            }

            return existingUser;
        }

        private async Task<bool> VerifyPasswordHashAsync(string inputPassword, string storedHash)
        {
            return await Task.Run(() => BCrypt.Net.BCrypt.Verify(inputPassword, storedHash));
        }

        private async Task<string> HashPasswordAsync(string password)
        {
            return await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(password));
        }
    }
}