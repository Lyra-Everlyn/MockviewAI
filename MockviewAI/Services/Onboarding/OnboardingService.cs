using MockviewAI.Models.DTOs;
using MockviewAI.Repositories.Interfaces;

namespace MockviewAI.Services.Onboarding
{
    /// <summary>
    /// [ONBOARDING] 
    /// Lưu hồ sơ ban đầu vào bảng Users.
    /// </summary>
    public class OnboardingService : IOnboardingService
    {
        #region Fields
        private readonly IUserRepository _userRepository;
        #endregion

        #region Constructor
        public OnboardingService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        #endregion

        #region IOnboardingService
        public async Task SaveOnboardingInfoAsync(string email, OnboardingDto dto)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be empty.", nameof(email));
            }

            var user = await _userRepository.GetByEmailAsync(email);

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

            var user = await _userRepository.GetByEmailAsync(email);
            return user?.IsOnboardingCompleted ?? false;
        }
        #endregion
    }
}
