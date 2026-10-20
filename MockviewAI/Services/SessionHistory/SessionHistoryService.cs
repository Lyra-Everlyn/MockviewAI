using MockviewAI.Models.Entities;
using MockviewAI.Models.ViewModels;
using MockviewAI.Repositories.Interfaces;
using MockviewAI.Services.Ai;

namespace MockviewAI.Services.SessionHistory
{
    public class SessionHistoryService : ISessionHistoryService
    {
        private readonly IRepository<InterviewSession> _sessionRepository;
        private readonly IUserRepository _userRepository;

        public SessionHistoryService(IRepository<InterviewSession> sessionRepository, IUserRepository userRepository)
        {
            _sessionRepository = sessionRepository;
            _userRepository = userRepository;
        }

        public async Task<List<RecentSessionViewModel>> GetHistoryAsync(string email, int? take = null)
        {
            var user = await FindUserAsync(email);
            if (user == null) return new List<RecentSessionViewModel>();

            var sessions = await _sessionRepository.GetListAsync(
                predicate: s => s.UserId == user.Id,
                orderBy: q => q.OrderByDescending(s => s.StartedAt),
                take: take
            );

            return sessions.Select(ToCard).ToList();
        }

        public async Task<InterviewDetailViewModel?> GetDetailAsync(string email, int sessionId)
        {
            var session = await FindSessionAsync(email, sessionId);
            if (session == null) return null;

            return new InterviewDetailViewModel
            {
                SessionId = session.Id,
                Position = session.Position,
                InterviewerName = InterviewerName(session.Interviewer),
                StartedAt = session.StartedAt.ToLocalTime(),
                QuestionCount = session.QuestionCount,
                HasScore = session.TotalScore != null,
                TotalScore = session.TotalScore ?? 0,
                ContentScore = session.ContentScore ?? 0,
                DeliveryScore = session.DeliveryScore ?? 0,
                AnxietyBefore = session.AnxietyBefore ?? 0,
                AnxietyAfter = session.AnxietyAfter ?? 0,
                AiSummary = session.AiSummary ?? string.Empty,
                Improvements = SplitLines(session.Improvements),

                Turns = session.Answers
                    .Where(a => a.AnsweredAt != null)
                    .OrderBy(a => a.OrderNo)
                    .Select(a => new ConversationTurnViewModel
                    {
                        Number = a.OrderNo,
                        Question = a.Question,
                        Answer = a.Answer,
                        Skipped = a.Skipped
                    })
                    .ToList()
            };
        }

        public async Task<bool> DeleteAsync(string email, int sessionId)
        {
            var session = await FindSessionAsync(email, sessionId);
            if (session == null) return false;

            await _sessionRepository.DeleteAsync(session.Id);
            return true;
        }

        #region Helpers
        private async Task<User?> FindUserAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            return await _userRepository.GetByEmailAsync(email.Trim().ToLowerInvariant());
        }

        private async Task<InterviewSession?> FindSessionAsync(string email, int sessionId)
        {
            var user = await FindUserAsync(email);
            if (user == null) return null;

            return await _sessionRepository.GetSingleAsync(
                predicate: s => s.Id == sessionId && s.UserId == user.Id,
                includes: s => s.Answers
            );
        }

        private static RecentSessionViewModel ToCard(InterviewSession s)
        {
            bool hasAnxiety = s.AnxietyBefore.HasValue && s.AnxietyAfter.HasValue;
            return new RecentSessionViewModel
            {
                SessionId = s.Id,
                RoleName = s.Position,
                DateFormatted = s.StartedAt.ToLocalTime().ToString("dd/MM"),
                QuestionCount = s.QuestionCount,
                Score = s.TotalScore ?? 0,
                AnxietyText = hasAnxiety ? $"Lo lắng {s.AnxietyBefore} → {s.AnxietyAfter}" : string.Empty,
                AnxietyImproved = hasAnxiety && s.AnxietyBefore - s.AnxietyAfter >= 2,
                HasScore = s.TotalScore != null,
                IsInProgress = s.Status == "InProgress",
                StatusText = s.Status == "InProgress" ? "Chưa xong" : s.TotalScore == null ? "Đã dừng" : string.Empty
            };
        }

        private static string InterviewerName(string key) =>
            QuestionBank.Interviewers.TryGetValue(key ?? "", out var info) ? info.Name : (key ?? "Unknown");

        private static List<string> SplitLines(string? text) =>
            string.IsNullOrWhiteSpace(text)
                ? new List<string>()
                : text.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        #endregion
    }
}