using MockviewAI.Models.Entities;
using MockviewAI.Models.ViewModels;
using MockviewAI.Repositories.Interfaces;
using MockviewAI.Services.Interfaces;

namespace MockviewAI.Services.Implementations
{
    // Xem và sửa hồ sơ. Dữ liệu nằm trong bảng Users (đã có từ bước Onboarding).
    public class ProfileService : IProfileService
    {
        // Tình trạng: giá trị lưu trong DB (giống Onboarding) -> chữ hiển thị
        private static readonly Dictionary<string, string> StatusTexts = new()
        {
            ["Current Student"] = "Đang đi học",
            ["Expecting Graduation"] = "Sắp ra trường",
            ["Graduated"] = "Đã ra trường",
            ["Other"] = "Khác"
        };

        private readonly IUserRepository _userRepository;
        private readonly ISessionHistoryService _history;

        public ProfileService(IUserRepository userRepository, ISessionHistoryService history)
        {
            _userRepository = userRepository;
            _history = history;
        }

        public async Task<ProfileViewModel?> GetProfileAsync(string email)
        {
            var user = await FindUserAsync(email);
            if (user == null) return null;

            string fullName = FullName(user);
            return new ProfileViewModel
            {
                FullName = fullName,
                AvatarInitial = fullName.Length > 0 ? fullName[..1].ToUpper() : "?",
                StatusText = StatusTexts.TryGetValue(user.GraduationStatus ?? "", out var text) ? text : string.Empty,
                Major = user.Major ?? string.Empty,
                TargetPosition = user.TargetPosition ?? string.Empty,
                RecentSessions = await _history.GetHistoryAsync(email, 3)
            };
        }

        public async Task<ProfileEditViewModel?> GetEditAsync(string email)
        {
            var user = await FindUserAsync(email);
            if (user == null) return null;

            return new ProfileEditViewModel
            {
                FullName = FullName(user),
                Major = user.Major ?? string.Empty,
                Position = user.TargetPosition ?? string.Empty,
                StudentStatus = user.GraduationStatus ?? string.Empty
            };
        }

        public async Task<string?> UpdateAsync(string email, ProfileEditViewModel model)
        {
            var user = await FindUserAsync(email);
            if (user == null) return null;

            // Tên hiển thị ở mọi nơi là FirstName + " " + LastName, nên tách theo khoảng trắng đầu tiên
            var name = model.FullName.Trim();
            int space = name.IndexOf(' ');
            user.FirstName = space < 0 ? name : name[..space];
            user.LastName = space < 0 ? string.Empty : name[(space + 1)..].Trim();

            user.Major = model.Major.Trim();
            user.TargetPosition = model.Position.Trim();
            // Chỉ nhận tình trạng nằm trong danh sách, giá trị lạ thì giữ nguyên
            if (StatusTexts.ContainsKey(model.StudentStatus))
                user.GraduationStatus = model.StudentStatus;
            user.UpdateAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            return FullName(user);
        }

        private async Task<User?> FindUserAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            return await _userRepository.GetByEmailAsync(email.Trim().ToLowerInvariant());
        }

        private static string FullName(User user) =>
            (user.FirstName + " " + user.LastName).Trim();
    }
}
