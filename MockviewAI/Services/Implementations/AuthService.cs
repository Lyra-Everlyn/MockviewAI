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

<<<<<<< Updated upstream
=======
        // Register
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
>>>>>>> Stashed changes


        // Login
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


        // Hashing and verifying password
        private async Task<bool> VerifyPasswordHashAsync(string inputPassword, string storedHash)
        {
            // Implement your password hash verification logic here
            // For example, you can use a hashing algorithm like BCrypt or PBKDF2
            // This is a placeholder implementation and should be replaced with actual logic
            return await Task.FromResult(inputPassword == storedHash);
        }


        // Recovery
        //public async Task<string> GeneratePasswordResetTokenAsync(string email)
        //{
        //    //var users = await _userRepository.GetAllAsync();
        //    //var user = users.FirstOrDefault(u => u.Email == email);
        //    //if (user == null) { throw new Exception("Account not found."); }
        //    //if (user.Status == "Locked") { throw new Exception("Your account has been locked."); }
        //    //string token = Guid.NewGuid().ToString();
        //    //user.PasswordResetToken = token;
        //    //user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        //    //await _userRepository.UpdateAsync(user);
        //    //return token;
        //}

        //public async Task<bool> VerifyResetTokenAsync(string email, string inputToken)
        //{
        //    //var users = await _userRepository.GetAllAsync();
        //    //var user = users.FirstOrDefault(u => u.Email == email);
        //    ////if (user == null) { throw new Exception("Account not found."); }
        //    ////if (user.Status == "Locked") { throw new Exception("Your account has been locked."); }
        //    ////if (user.PasswordResetToken != inputToken) { return false; }
        //    ////if (user.PasswordResetTokenExpiry < DateTime.UtcNow) { return false; }
        //    //return true;
        //}

        //public async Task ResetPasswordAsync(string email, string inputToken, string newPassword, string confirmPassword)
        //{
        //    //if (newPassword != confirmPassword) { throw new Exception("Passwords do not match."); }
        //    //var users = await _userRepository.GetAllAsync();
        //    //var user = users.FirstOrDefault(u => u.Email == email);
        //    //if (user == null) { throw new Exception("Account not found."); }
        //    //if (user.Status == "Locked") { throw new Exception("Your account has been locked."); }
        //    ////if (user.PasswordResetToken != inputToken) { throw new Exception("Invalid reset token."); }
        //    ////if (user.PasswordResetTokenExpiry < DateTime.UtcNow) { throw new Exception("Reset token has expired."); }
        //    ////string newHashedPassword = await HashPasswordAsync(newPassword);
        //    ////user.PasswordHash = newHashedPassword;
        //    ////user.PasswordResetToken = null;
        //    ////user.PasswordResetTokenExpiry = null;
        //    //await _userRepository.UpdateAsync(user);
        //}
    }
}