using MockviewAI.Models.DTOs;
using MockviewAI.Services.Helper.Interfaces;
using MockviewAI.Repositories.Interfaces;

namespace MockviewAI.Services.Helper.Implementations
{
    public class OnboardingService : IOnboardingService
    {
        private readonly IUserRepository _userRepository;
        public OnboardingService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        public async Task SaveOnboardingInfoAsync(string email, OnboardingDto dto)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be empty.", nameof(email));
            }

            var users = await _userRepository.GetAllAsync();
            var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                throw new KeyNotFoundException("User account not found.");
            }

            user.Major = dto.Major.Trim();
            user.TargetPosition = dto.TargetPosition.Trim();
            user.GraduationStatus = dto.GraduationStatus.Trim();
            user.ExperienceLevel = dto.ExperienceLevel.Trim();
            user.IsOnboardingCompleted = true;
            user.UpdateAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
        }

        public async Task<bool> IsOnboardingCompletedAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            var users = await _userRepository.GetAllAsync();
            var user = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            return user?.IsOnboardingCompleted ?? false;
        }
    }
}
