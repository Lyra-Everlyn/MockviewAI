using MockviewAI.Models.DTOs;
using MockviewAI.Models.Entities;
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
        public async Task<bool> SaveOnboardingInfoAsync(string email, OnboardingDto dto)
        {
            var user = await FindUserAsync(email);
            if (user == null) return false;

            user.Major = dto.Major.Trim();
            user.TargetPosition = dto.TargetPosition.Trim();
            user.GraduationStatus = dto.GraduationStatus.Trim();
            user.ExperienceLevel = dto.ExperienceLevel.Trim();
            user.IsOnboardingCompleted = true;
            user.UpdateAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<bool> IsOnboardingCompletedAsync(string email)
        {
            var user = await FindUserAsync(email);
            return user?.IsOnboardingCompleted ?? false;
        }
        #endregion

        #region Helpers
        /// <summary>Email lưu chữ thường nên chuẩn hoá trước khi tìm (giống các service khác).</summary>
        private async Task<User?> FindUserAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            return await _userRepository.GetByEmailAsync(email.Trim().ToLowerInvariant());
        }
        #endregion
    }
}
