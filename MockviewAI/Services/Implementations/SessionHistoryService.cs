using Microsoft.EntityFrameworkCore;
using MockviewAI.Data;
using MockviewAI.Models.Entities;
using MockviewAI.Models.ViewModels;
using MockviewAI.Repositories.Interfaces;
using MockviewAI.Services.Ai;
using MockviewAI.Services.Interfaces;

namespace MockviewAI.Services.Implementations
{
    // Đọc lịch sử buổi luyện của người đang đăng nhập.
    // Mọi truy vấn đều lọc theo UserId: người dùng chỉ thấy và xoá được buổi của chính mình.
    public class SessionHistoryService : ISessionHistoryService
    {
        private readonly ApplicationDbContext _db;
        private readonly IUserRepository _userRepository;

        public SessionHistoryService(ApplicationDbContext db, IUserRepository userRepository)
        {
            _db = db;
            _userRepository = userRepository;
        }

        public async Task<List<RecentSessionViewModel>> GetHistoryAsync(string email, int? take = null)
        {
            var user = await FindUserAsync(email);
            if (user == null) return new List<RecentSessionViewModel>();

            // Chỉ lấy buổi đã chấm điểm, mới nhất trước
            var query = _db.InterviewSessions
                .Where(s => s.UserId == user.Id && s.Status == "Completed")
                .OrderByDescending(s => s.StartedAt)
                .AsQueryable();
            if (take.HasValue) query = query.Take(take.Value);

            var sessions = await query.ToListAsync();
            return sessions.Select(ToCard).ToList();
        }

        public async Task<InterviewDetailViewModel?> GetDetailAsync(string email, int sessionId)
        {
            var session = await FindSessionAsync(email, sessionId);
            if (session == null || session.Status != "Completed") return null;

            return new InterviewDetailViewModel
            {
                SessionId = session.Id,
                Position = session.Position,
                InterviewerName = InterviewerName(session.Interviewer),
                StartedAt = session.StartedAt.ToLocalTime(),
                QuestionCount = session.QuestionCount,
                TotalScore = session.TotalScore ?? 0,
                ContentScore = session.ContentScore ?? 0,
                DeliveryScore = session.DeliveryScore ?? 0,
                AnxietyBefore = session.AnxietyBefore ?? 0,
                AnxietyAfter = session.AnxietyAfter ?? 0,
                AiSummary = session.AiSummary ?? string.Empty,
                Improvements = SplitLines(session.Improvements),
                // Chỉ hiện các câu đã được hỏi (đã trả lời hoặc bỏ qua)
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

            _db.InterviewSessions.Remove(session);   // câu trả lời bị xoá theo (cascade)
            await _db.SaveChangesAsync();
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

            return await _db.InterviewSessions
                .Include(s => s.Answers)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == user.Id);
        }

        // Một thẻ trong danh sách lịch sử (dùng chung cho Trang chủ, Hồ sơ, Lịch sử)
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
                // Giảm từ 2 bậc trở lên thì hiện màu xanh, còn lại màu cam
                AnxietyImproved = hasAnxiety && s.AnxietyBefore - s.AnxietyAfter >= 2
            };
        }

        private static string InterviewerName(string key) =>
            QuestionBank.Interviewers.TryGetValue(key, out var info) ? info.Name : key;

        private static List<string> SplitLines(string? text) =>
            string.IsNullOrWhiteSpace(text)
                ? new List<string>()
                : text.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        #endregion
    }
}
