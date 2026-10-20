using MockviewAI.Models.DTOs;

namespace MockviewAI.Services.Onboarding
{
    /// [SETUP PROFILE]
    // -- After login
    public interface IOnboardingService
    {
        Task SaveOnboardingInfoAsync(string email, OnboardingDto dto);
        Task<bool> IsOnboardingCompletedAsync(string email);
    }
}
