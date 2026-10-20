using MockviewAI.Models.DTOs;
using MockviewAI.Models.Entities;
using MockviewAI.Repositories.Interfaces;

namespace MockviewAI.Services.Feedbacks
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IRepository<Feedback> _feedbackRepository;
        private readonly IUserRepository _userRepository;

        public FeedbackService(IRepository<Feedback> feedbackRepository, IUserRepository userRepository)
        {
            _feedbackRepository = feedbackRepository;
            _userRepository = userRepository;
        }

        public async Task<bool> SubmitAsync(string email, FeedbackDto dto)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var user = await _userRepository.GetByEmailAsync(email.Trim().ToLowerInvariant());
            if (user == null) return false;

            var comment = dto.Comment?.Trim();
            await _feedbackRepository.AddAsync(new Feedback
            {
                UserId = user.Id,
                EaseRating = Math.Clamp(dto.EaseRating, 1, 5),
                AnxietyHelp = dto.AnxietyHelp,
                Comment = string.IsNullOrEmpty(comment) ? null : comment
            });
            return true;
        }
    }
}
