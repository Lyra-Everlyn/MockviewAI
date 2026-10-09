using MockviewAI.Models.DTOs;

namespace MockviewAI.Services.Helper.Interfaces
{
    public interface IOnboardingService
    {
        Task SaveOnboardingInfoAsync(string email, OnboardingDto dto);
        Task<bool> IsOnboardingCompletedAsync(string email);
    }
}
