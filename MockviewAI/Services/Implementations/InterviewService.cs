// [AI-INTERVIEW] Nối phòng phỏng vấn với module AI.
// Luồng: Start (tạo buổi + câu hỏi) -> SubmitAnswer (lưu từng câu) -> Finish (gọi InterviewScoringService, lưu điểm).

using Microsoft.EntityFrameworkCore;
using MockviewAI.Data;
using MockviewAI.Models.Ai;
using MockviewAI.Models.DTOs;
using MockviewAI.Models.Entities;
using MockviewAI.Repositories.Interfaces;
using MockviewAI.Services.Ai;
using MockviewAI.Services.Interfaces;

namespace MockviewAI.Services.Implementations
{
    public class InterviewService : IInterviewService
    {
        private const int MaxAnswerLength = 2000;      // khớp giới hạn của bộ chấm AI
        private static readonly int[] AllowedQuestionCounts = { 5, 10 };

        private readonly ApplicationDbContext _db;
        private readonly IUserRepository _userRepository;
        private readonly InterviewScoringService _scoring;

        public InterviewService(ApplicationDbContext db, IUserRepository userRepository, InterviewScoringService scoring)
        {
            _db = db;
            _userRepository = userRepository;
            _scoring = scoring;
        }

        #region Start
        public async Task<int> StartAsync(string email, string positionKey, string interviewerKey, int questionCount, int? anxietyBefore)
        {
            var user = await GetUserAsync(email);

            // Chỉ nhận giá trị nằm trong danh sách cho phép
            if (!QuestionBank.Positions.TryGetValue(positionKey ?? "", out var positionName))
                throw new InterviewException("Vị trí ứng tuyển không hợp lệ.");
            if (!QuestionBank.Interviewers.ContainsKey(interviewerKey ?? ""))
                throw new InterviewException("Người phỏng vấn không hợp lệ.");
            if (!AllowedQuestionCounts.Contains(questionCount))
                throw new InterviewException("Số câu hỏi không hợp lệ.");

            var session = new InterviewSession
            {
                UserId = user.Id,
                Position = positionName,
                Interviewer = interviewerKey!,
                QuestionCount = questionCount,
                AnxietyBefore = ClampAnxiety(anxietyBefore)
            };

            // Tạo sẵn các câu hỏi, chưa có câu trả lời
            var questions = QuestionBank.GetQuestions(interviewerKey!, positionName, questionCount);
            for (int i = 0; i < questions.Count; i++)
            {
                session.Answers.Add(new SessionAnswer { OrderNo = i + 1, Question = questions[i] });
            }

            _db.InterviewSessions.Add(session);
            await _db.SaveChangesAsync();
            return session.Id;
        }
        #endregion

        #region Room
        public async Task<InterviewRoomViewModel?> GetRoomAsync(string email, int sessionId)
        {
            var session = await FindSessionAsync(email, sessionId);
            if (session == null || session.Status != "InProgress") return null;

            var current = session.Answers.OrderBy(a => a.OrderNo).FirstOrDefault(a => a.AnsweredAt == null);
            if (current == null) return null;   // đã hết câu, chờ Finish

            var interviewer = QuestionBank.Interviewers[session.Interviewer];
            return new InterviewRoomViewModel
            {
                SessionId = session.Id,
                Position = session.Position,
                InterviewerName = interviewer.Name,
                InterviewerRole = interviewer.Role,
                QuestionNumber = current.OrderNo,
                TotalQuestions = session.QuestionCount,
                QuestionText = current.Question,
                AnsweredCount = CountAnswered(session)
            };
        }

        public async Task<SubmitAnswerResult> SubmitAnswerAsync(string email, SubmitAnswerDto dto)
        {
            var session = await FindSessionAsync(email, dto.SessionId);
            if (session == null || session.Status != "InProgress")
                throw new InterviewException("Không tìm thấy buổi luyện đang diễn ra.");

            var current = session.Answers.OrderBy(a => a.OrderNo).FirstOrDefault(a => a.AnsweredAt == null);
            if (current == null)
                throw new InterviewException("Bạn đã trả lời hết các câu hỏi.");

            if (dto.Skipped)
            {
                current.Skipped = true;
            }
            else
            {
                string answer = (dto.Answer ?? "").Trim();
                if (answer.Length == 0)
                    throw new InterviewException("Vui lòng nói hoặc gõ câu trả lời trước khi gửi.");
                if (answer.Length > MaxAnswerLength)
                    throw new InterviewException($"Câu trả lời tối đa {MaxAnswerLength} ký tự.");

                current.Answer = answer;
                current.DurationSeconds = dto.DurationSeconds is > 0 and < 600 ? dto.DurationSeconds : null;
            }
            current.AnsweredAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Tìm câu tiếp theo
            var next = session.Answers.OrderBy(a => a.OrderNo).FirstOrDefault(a => a.AnsweredAt == null);
            return new SubmitAnswerResult
            {
                Done = next == null,
                AnsweredCount = CountAnswered(session),
                NextNumber = next?.OrderNo,
                NextQuestion = next?.Question
            };
        }
        #endregion

        #region Finish
        public async Task FinishAsync(string email, FinishSessionDto dto)
        {
            var session = await FindSessionAsync(email, dto.SessionId);
            if (session == null) throw new InterviewException("Không tìm thấy buổi luyện.");
            if (session.Status != "InProgress") return;   // đã chấm rồi, không chấm lại

            session.FinishedAt = DateTime.UtcNow;
            session.AnxietyAfter = ClampAnxiety(dto.AnxietyAfter);

            // Chỉ chấm các câu đã trả lời (bỏ qua câu "bỏ qua")
            var answered = session.Answers
                .Where(a => a.AnsweredAt != null && !a.Skipped && !string.IsNullOrEmpty(a.Answer))
                .OrderBy(a => a.OrderNo)
                .ToList();

            if (answered.Count == 0)
            {
                session.Status = "Stopped";   // chưa trả lời câu nào nên không có điểm
                await _db.SaveChangesAsync();
                return;
            }

            var request = new ScoreRequest
            {
                Position = session.Position,
                Answers = answered.Select(a => new InterviewAnswer
                {
                    Question = a.Question,
                    Answer = a.Answer!,
                    DurationSeconds = a.DurationSeconds
                }).ToList()
            };

            // Có thể ném AiScoringException (thông báo tiếng Việt), Controller sẽ hiển thị cho người dùng
            var result = await _scoring.ScoreAsync(request);

            session.Status = "Completed";
            session.TotalScore = result.Total;
            session.ContentScore = result.Content.Total;
            session.DeliveryScore = result.Delivery.Total;
            session.AiSummary = result.Content.Summary;
            session.Strengths = string.Join("\n", result.Content.Strengths);
            session.Improvements = string.Join("\n", result.Content.Improvements);
            session.DeliveryTips = string.Join("\n", result.Delivery.Tips);
            await _db.SaveChangesAsync();
        }
        #endregion

        #region Result
        public async Task<SessionResultViewModel?> GetResultAsync(string email, int sessionId)
        {
            var session = await FindSessionAsync(email, sessionId);
            if (session == null || session.Status == "InProgress") return null;

            return new SessionResultViewModel
            {
                SessionId = session.Id,
                Position = session.Position,
                InterviewerName = QuestionBank.Interviewers[session.Interviewer].Name,
                StartedAt = session.StartedAt,
                QuestionCount = session.QuestionCount,
                HasScore = session.TotalScore != null,
                TotalScore = session.TotalScore ?? 0,
                ContentScore = session.ContentScore ?? 0,
                DeliveryScore = session.DeliveryScore ?? 0,
                AnxietyBefore = session.AnxietyBefore ?? 0,
                AnxietyAfter = session.AnxietyAfter ?? 0,
                AiSummary = session.AiSummary ?? "",
                Strengths = SplitLines(session.Strengths),
                Improvements = SplitLines(session.Improvements),
                DeliveryTips = SplitLines(session.DeliveryTips)
            };
        }
        #endregion

        #region Helpers
        private async Task<User> GetUserAsync(string email)
        {
            return await _userRepository.GetByEmailAsync(email.Trim().ToLowerInvariant())
                   ?? throw new InterviewException("Không tìm thấy tài khoản. Vui lòng đăng nhập lại.");
        }

        // Chỉ trả về buổi luyện thuộc về người đang đăng nhập
        private async Task<InterviewSession?> FindSessionAsync(string email, int sessionId)
        {
            var user = await GetUserAsync(email);
            return await _db.InterviewSessions
                .Include(s => s.Answers)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == user.Id);
        }

        private static int CountAnswered(InterviewSession session) =>
            session.Answers.Count(a => a.AnsweredAt != null && !a.Skipped);

        // Mức lo lắng chỉ nhận 1-10, ngoài khoảng đó coi như không có
        private static int? ClampAnxiety(int? value) =>
            value is >= 1 and <= 10 ? value : null;

        private static List<string> SplitLines(string? text) =>
            string.IsNullOrWhiteSpace(text)
                ? new List<string>()
                : text.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        #endregion
    }
}
