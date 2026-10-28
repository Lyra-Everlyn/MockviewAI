using MockviewAI.Models.Ai;
using MockviewAI.Models.DTOs;
using MockviewAI.Models.Entities;
using MockviewAI.Models.ViewModels;
using MockviewAI.Repositories.Interfaces;
using MockviewAI.Services.Ai;
using MockviewAI.Services.Ai.Interfaces;
using MockviewAI.Services.Emails;
using MockviewAI.Services.Exceptions;

namespace MockviewAI.Services.Interviews
{
    /// [AI-INTERVIEW]
    public class InterviewService : IInterviewService
    {
        private const int MaxAnswerLength = 2000;
        private static readonly int[] AllowedQuestionCounts = { 5, 10 };

        // Mỗi buổi gọi AI chấm 1 lần -> giới hạn số buổi mới để không bị spam tốn tiền API
        private const int MaxSessionsPerHour = 10;

        // Mỗi câu được trả lời tối đa 3 lần: 2 lần đầu bị từ chối thì được nói lại, lần thứ 3 vẫn không ổn thì tính là bỏ qua
        private const int MaxAttemptsPerQuestion = 3;

        private readonly IRepository<InterviewSession> _sessionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IInterviewScoringService _scoring;
        private readonly IAnswerQualityChecker _quality;
        private readonly IEmailService _email;
        private readonly ILogger<InterviewService> _logger;

        public InterviewService(
            IRepository<InterviewSession> sessionRepository,
            IUserRepository userRepository,
            IInterviewScoringService scoring,
            IAnswerQualityChecker quality,
            IEmailService email,
            ILogger<InterviewService> logger)
        {
            _sessionRepository = sessionRepository;
            _userRepository = userRepository;
            _scoring = scoring;
            _quality = quality;
            _email = email;
            _logger = logger;
        }

        #region Start
        public async Task<int> StartAsync(string email, string positionKey, string interviewerKey, int questionCount, bool emailResult)
        {
            var user = await GetUserAsync(email);

            if (!QuestionBank.Positions.TryGetValue(positionKey ?? "", out var positionName))
                throw new InterviewException("Vị trí ứng tuyển không hợp lệ.");
            if (!QuestionBank.Interviewers.ContainsKey(interviewerKey ?? ""))
                throw new InterviewException("Người phỏng vấn không hợp lệ.");
            if (!AllowedQuestionCounts.Contains(questionCount))
                throw new InterviewException("Số câu hỏi không hợp lệ.");

            // Đếm theo tài khoản (không theo IP) vì nhiều người test có thể dùng chung wifi
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var recentSessions = await _sessionRepository.GetListAsync(s => s.UserId == user.Id && s.StartedAt >= oneHourAgo);
            if (recentSessions.Count() >= MaxSessionsPerHour)
                throw new InterviewException($"Bạn đã tạo {MaxSessionsPerHour} buổi luyện trong 1 giờ qua. Hãy nghỉ một chút rồi thử lại nhé.");

            var session = new InterviewSession
            {
                UserId = user.Id,
                Position = positionName,
                Interviewer = interviewerKey!,
                QuestionCount = questionCount,
                EmailResult = emailResult
            };

            var questions = QuestionBank.GetQuestions(interviewerKey!, positionName, questionCount);
            for (int i = 0; i < questions.Count; i++)
            {
                session.Answers.Add(new SessionAnswer { OrderNo = i + 1, Question = questions[i] });
            }

            await _sessionRepository.AddAsync(session);
            return session.Id;
        }
        #endregion

        #region Room
        public async Task<InterviewRoomViewModel?> GetRoomAsync(string email, int sessionId)
        {
            var user = await GetUserAsync(email);
            var session = await FindSessionAsync(user, sessionId);
            if (session == null || session.Status != SessionStatus.InProgress) return null;

            var current = session.Answers.OrderBy(a => a.OrderNo).FirstOrDefault(a => a.AnsweredAt == null);
            if (current == null) return null;

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

        public async Task<SubmitAnswerResultDto> SubmitAnswerAsync(string email, SubmitAnswerDto dto)
        {
            var user = await GetUserAsync(email);
            var session = await FindSessionAsync(user, dto.SessionId);
            if (session == null || session.Status != SessionStatus.InProgress)
                throw new InterviewException("Không tìm thấy buổi luyện đang diễn ra.");

            var current = session.Answers.OrderBy(a => a.OrderNo).FirstOrDefault(a => a.AnsweredAt == null);
            if (current == null)
                throw new InterviewException("Bạn đã trả lời hết các câu hỏi.");

            string? notice = null;

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

                // Chặn câu trả lời vớ vẩn: cho nói lại, quá số lần thì tính là bỏ qua (không có điểm)
                string? problem = _quality.FindProblem(answer);
                if (problem != null)
                {
                    current.Attempts++;
                    if (current.Attempts < MaxAttemptsPerQuestion)
                    {
                        await _sessionRepository.UpdateAsync(session);
                        int left = MaxAttemptsPerQuestion - current.Attempts;
                        throw new AnswerRejectedException($"{problem} Bạn còn {left} lần để trả lời lại câu này.");
                    }

                    current.Skipped = true;
                    notice = "Câu này được tính là bỏ qua vì câu trả lời chưa phù hợp. Hãy thử câu tiếp theo nhé.";
                }
                else
                {
                    current.Answer = answer;
                    current.DurationSeconds = dto.DurationSeconds is > 0 and < 600 ? dto.DurationSeconds : null;
                    current.LongestPauseSeconds = current.DurationSeconds == null ? null : LongestPause(dto.PausesSeconds);
                }
            }
            current.AnsweredAt = DateTime.UtcNow;

            await _sessionRepository.UpdateAsync(session);

            var next = session.Answers.OrderBy(a => a.OrderNo).FirstOrDefault(a => a.AnsweredAt == null);
            return new SubmitAnswerResultDto
            {
                Done = next == null,
                AnsweredCount = CountAnswered(session),
                NextNumber = next?.OrderNo,
                NextQuestion = next?.Question,
                Notice = notice
            };
        }
        #endregion

        #region Finish
        public async Task FinishAsync(string email, FinishSessionDto dto)
        {
            var user = await GetUserAsync(email);
            var session = await FindSessionAsync(user, dto.SessionId);
            if (session == null) throw new InterviewException("Không tìm thấy buổi luyện.");
            if (session.Status != SessionStatus.InProgress) return;

            session.FinishedAt = DateTime.UtcNow;

            var answered = session.Answers
                .Where(a => a.AnsweredAt != null && !a.Skipped && !string.IsNullOrEmpty(a.Answer))
                .OrderBy(a => a.OrderNo)
                .ToList();

            if (answered.Count == 0)
            {
                session.Status = SessionStatus.Stopped;
                await _sessionRepository.UpdateAsync(session);
                return;
            }

            var request = new ScoreRequest
            {
                Position = session.Position,
                QuestionCount = session.QuestionCount,
                SkippedCount = session.Answers.Count(a => a.AnsweredAt != null && a.Skipped),
                Answers = answered.Select(a => new InterviewAnswer
                {
                    Question = a.Question,
                    Answer = a.Answer!,
                    DurationSeconds = a.DurationSeconds,
                    // Có đo khoảng lặng thì đưa vào chấm điểm; gõ tay thì để trống (không bị trừ điểm)
                    PausesSeconds = a.LongestPauseSeconds is double pause ? new List<double> { pause } : new List<double>()
                }).ToList()
            };

            // AI lỗi (AiScoringException) thì để lỗi nổi lên: buổi vẫn còn InProgress để người dùng bấm "Chấm lại".
            // Fallback = true: người dùng đồng ý xem kết quả dự phòng (chỉ tính cách nói).
            var result = dto.Fallback
                ? _scoring.ScoreDeliveryOnly(request)
                : await _scoring.ScoreAsync(request);

            session.Status = SessionStatus.Completed;
            session.TotalScore = result.Total;
            session.ContentScore = result.ContentAvailable ? result.Content.Total : null;
            session.DeliveryScore = result.Delivery.Total;
            session.AnxietyIndicator = result.AnxietyIndicator > 0 ? result.AnxietyIndicator : null;
            session.AiSummary = result.Content.Summary;
            session.Strengths = string.Join("\n", result.Content.Strengths);
            session.Improvements = string.Join("\n", result.Content.Improvements);
            session.DeliveryTips = string.Join("\n", result.Delivery.Tips);

            await _sessionRepository.UpdateAsync(session);

            if (session.EmailResult)
                SendResultEmailInBackground(user, session, result);
        }

        // Gửi email ở nền: lỗi SMTP chỉ ghi log, không làm hỏng việc xem kết quả của người dùng
        private void SendResultEmailInBackground(User user, InterviewSession session, InterviewScoreResult result)
        {
            string toEmail = user.Email;
            var model = new ResultEmailModel
            {
                FirstName = user.FirstName,
                Position = session.Position,
                InterviewerName = QuestionBank.Interviewers[session.Interviewer].Name,
                StartedAt = session.StartedAt.ToLocalTime(),
                QuestionCount = session.QuestionCount,
                TotalScore = result.Total,
                ContentScore = result.ContentAvailable ? result.Content.Total : null,
                DeliveryScore = result.Delivery.Total,
                AnxietyIndicator = result.AnxietyIndicator,
                AnxietyReasons = result.AnxietyReasons,
                Summary = result.Content.Summary,
                Strengths = result.Content.Strengths,
                Improvements = result.Content.Improvements,
                DeliveryTips = result.Delivery.Tips
            };

            string subject = ResultEmailBuilder.BuildSubject(model);
            string body = ResultEmailBuilder.BuildHtml(model);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _email.SendEmailAsync(toEmail, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không gửi được email kết quả buổi luyện {SessionId}.", session.Id);
                }
            });
        }
        #endregion

        #region Result
        public async Task<SessionResultViewModel?> GetResultAsync(string email, int sessionId)
        {
            var user = await GetUserAsync(email);
            var session = await FindSessionAsync(user, sessionId);
            if (session == null || session.Status == SessionStatus.InProgress) return null;

            // Buổi trước đó (nếu có) để hiện xu hướng dấu hiệu lo lắng
            var startedAt = session.StartedAt;
            var previous = await _sessionRepository.GetListAsync(
                predicate: s => s.UserId == user.Id && s.StartedAt < startedAt && s.AnxietyIndicator != null,
                orderBy: q => q.OrderByDescending(s => s.StartedAt),
                take: 1);

            return new SessionResultViewModel
            {
                SessionId = session.Id,
                Position = session.Position,
                InterviewerName = QuestionBank.Interviewers[session.Interviewer].Name,
                StartedAt = session.StartedAt.ToLocalTime(),
                QuestionCount = session.QuestionCount,
                HasScore = session.TotalScore != null,
                HasContentScore = session.ContentScore != null,
                TotalScore = session.TotalScore ?? 0,
                ContentScore = session.ContentScore ?? 0,
                DeliveryScore = session.DeliveryScore ?? 0,
                AnxietyIndicator = session.AnxietyIndicator ?? 0,
                PreviousAnxietyIndicator = previous.FirstOrDefault()?.AnxietyIndicator ?? 0,
                EmailRequested = session.EmailResult && session.TotalScore != null,
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

        private async Task<InterviewSession?> FindSessionAsync(User user, int sessionId)
        {
            return await _sessionRepository.GetSingleAsync(
                predicate: s => s.Id == sessionId && s.UserId == user.Id,
                includes: s => s.Answers
            );
        }

        private static int CountAnswered(InterviewSession session) =>
            session.Answers.Count(a => a.AnsweredAt != null && !a.Skipped);

        // Lấy khoảng lặng dài nhất; bỏ số vô lý (<= 0 hoặc >= 120 giây) vì dữ liệu đến từ trình duyệt
        private static double LongestPause(List<double>? pauses)
        {
            var valid = (pauses ?? new List<double>()).Where(p => p > 0 && p < 120).ToList();
            return valid.Count == 0 ? 0 : Math.Round(valid.Max(), 1);
        }

        private static List<string> SplitLines(string? text) =>
            string.IsNullOrWhiteSpace(text)
                ? new List<string>()
                : text.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        #endregion
    }
}
