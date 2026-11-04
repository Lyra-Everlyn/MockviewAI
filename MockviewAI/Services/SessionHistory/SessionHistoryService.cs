using MockviewAI.Models;
using MockviewAI.Models.Entities;
using MockviewAI.Models.ViewModels;
using MockviewAI.Repositories.Interfaces;
using MockviewAI.Services.Ai;
using MockviewAI.Models.Ai;
using MockviewAI.Services.Ai.Interfaces;
using MockviewAI.Services.InterviewQuestions.Interfaces;

namespace MockviewAI.Services.SessionHistory
{
    public class SessionHistoryService : ISessionHistoryService
    {
        private readonly IRepository<InterviewSession> _sessionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAnswerQualityChecker _quality;
        private readonly IQuestionBankStore _bank;

        public SessionHistoryService(IRepository<InterviewSession> sessionRepository, IUserRepository userRepository,
                                     IAnswerQualityChecker quality, IQuestionBankStore bank)
        {
            _sessionRepository = sessionRepository;
            _userRepository = userRepository;
            _quality = quality;
            _bank = bank;
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
                IsLive = InterviewModes.IsLive(session.Mode),
                HasScore = session.TotalScore != null,
                HasContentScore = session.ContentScore != null,
                TotalScore = session.TotalScore ?? 0,
                ContentScore = session.ContentScore ?? 0,
                DeliveryScore = session.DeliveryScore ?? 0,
                AnxietyIndicator = session.AnxietyIndicator ?? 0,
                AiSummary = session.AiSummary ?? string.Empty,
                Improvements = SplitLines(session.Improvements),

                // [FOLLOW-UP] Mỗi lượt = một câu chính; câu hỏi thêm (nếu có) nằm trong lượt của câu chính
                Turns = FollowUps.Main(session.Answers)
                    .Where(a => a.AnsweredAt != null)
                    .OrderBy(a => a.OrderNo)
                    .Select(a => ToTurnWithFollowUp(a, session.Answers))
                    .ToList()
            };
        }

        private ConversationTurnViewModel ToTurnWithFollowUp(SessionAnswer main, IEnumerable<SessionAnswer> all)
        {
            var turn = ToTurn(main);
            var extra = all.FirstOrDefault(a => a.IsFollowUp && a.FollowUpOf == main.QuestionId && a.AnsweredAt != null);
            if (extra == null) return turn;

            turn.FollowUp = new ConversationTurnViewModel
            {
                Number = main.OrderNo,
                Question = extra.Question,
                Answer = extra.Answer,
                Skipped = extra.Skipped,
                // Câu hỏi thêm không bị tính sai riêng: chỉ ghi chú khi câu trả lời thêm chưa có ý nghĩa
                InvalidReason = !extra.Skipped && !string.IsNullOrEmpty(extra.Answer) ? _quality.FindProblem(extra.Answer) : null,
                VoiceText = VoiceMetrics.Describe(extra.VoiceLevelDb, extra.QuietRatio, extra.PitchVariation)
            };
            turn.FollowUpPointText = _bank.Find(main.QuestionId)?.CorePoints.FirstOrDefault(c => c.Id == extra.FollowUpPointId)?.Text;
            return turn;
        }

        // Một lượt hỏi - đáp + kết quả xếp loại 4 loại (đổi mã c1, loi1... thành nội dung để người dùng đọc được)
        private ConversationTurnViewModel ToTurn(SessionAnswer a)
        {
            var turn = new ConversationTurnViewModel
            {
                Number = a.OrderNo,
                Question = a.Question,
                Answer = a.Answer,
                Skipped = a.Skipped,
                InvalidReason = !a.Skipped && !string.IsNullOrEmpty(a.Answer) ? _quality.FindProblem(a.Answer) : null,
                Category = AnswerCategory.IsValid(a.Category) ? a.Category : null,
                CategoryName = AnswerCategory.DisplayName(a.Category),
                CategoryIcon = AnswerCategory.Icon(a.Category),
                CategoryDescription = AnswerCategory.Description(a.Category),
                Feedback = a.Feedback,
                VoiceText = VoiceMetrics.Describe(a.VoiceLevelDb, a.QuietRatio, a.PitchVariation)   // [VOICE]
            };

            var question = _bank.Find(a.QuestionId);
            if (question == null || turn.Category == null) return turn;

            var path = question.Paths.FirstOrDefault(p => p.Id == a.MatchedPath);
            var mistake = question.Mistakes.FirstOrDefault(m => m.Id == a.MatchedPath);
            turn.MatchedPathText = path != null ? "Hướng trả lời: " + path.Summary
                                 : mistake != null ? "Lỗi hay gặp: " + mistake.Summary
                                 : null;
            turn.MetPoints = PointTexts(question.CorePoints, a.CorePointsMet);
            turn.MissingPoints = PointTexts(question.CorePoints, a.CorePointsMissing);
            return turn;
        }

        private static List<string> PointTexts(List<Models.InterviewQuestions.CorePoint> points, string? ids)
        {
            var wanted = (ids ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return points.Where(p => wanted.Contains(p.Id)).Select(p => p.Text).ToList();
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
            bool hasAnxiety = s.AnxietyIndicator.HasValue;
            return new RecentSessionViewModel
            {
                SessionId = s.Id,
                RoleName = s.Position,
                DateFormatted = s.StartedAt.ToLocalTime().ToString("dd/MM"),
                QuestionCount = s.QuestionCount,
                Score = s.TotalScore ?? 0,
                AnxietyText = hasAnxiety ? $"Dấu hiệu lo lắng {s.AnxietyIndicator}/10" : string.Empty,
                AnxietyImproved = hasAnxiety && s.AnxietyIndicator <= 3,   // badge xanh khi thấp
                HasScore = s.TotalScore != null,
                IsInProgress = s.Status == SessionStatus.InProgress,
                StatusText = s.Status == SessionStatus.InProgress ? "Chưa xong" : s.TotalScore == null ? "Đã dừng" : string.Empty,
                IsLive = InterviewModes.IsLive(s.Mode)
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