using MockviewAI.Models.ViewModels;

namespace MockviewAI.Services.Profiles
{
    /// [PROFILE]
    public interface IProfileService
    {
        Task<ProfileViewModel?> GetProfileAsync(string email);
        Task<ProfileEditViewModel?> GetEditAsync(string email);
        Task<(string FullName, string? AvatarUrl)?> UpdateAsync(string email, ProfileEditViewModel model);
    }
}
