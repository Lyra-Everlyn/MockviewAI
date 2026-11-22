using MockviewAI.Models.DTOs;

namespace MockviewAI.Services.Onboarding
{
    /// <summary>
    /// [ONBOARDING] 
    /// Hồ sơ ban đầu sau lần đăng nhập đầu tiên.
    /// </summary>
    public interface IOnboardingService
    {
        /// <summary>Lưu ngành học, vị trí, tình trạng tốt nghiệp, kinh nghiệm và đánh dấu đã hoàn tất.</summary>
        /// <returns>false = email trống hoặc không tìm thấy tài khoản.</returns>
        Task<bool> SaveOnboardingInfoAsync(string email, OnboardingDto dto);

        /// <summary>false khi email trống hoặc không tìm thấy tài khoản.</summary>
        Task<bool> IsOnboardingCompletedAsync(string email);
    }
}
