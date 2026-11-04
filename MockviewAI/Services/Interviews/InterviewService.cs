using MockviewAI.Models;
using MockviewAI.Models.Ai;
using MockviewAI.Models.DTOs;
using MockviewAI.Models.Entities;
using MockviewAI.Models.ViewModels;
using MockviewAI.Repositories.Interfaces;
using MockviewAI.Services.Ai;
using MockviewAI.Services.Ai.Interfaces;
using MockviewAI.Services.Emails;
using MockviewAI.Services.Exceptions;
using MockviewAI.Services.InterviewQuestions.Interfaces;

namespace MockviewAI.Services.Interviews
{
    /// [AI-INTERVIEW]
    public class InterviewService : IInterviewService
    {
        private const int MaxAnswerLength = 2000;
        private const int MinQuestions = 5, MaxQuestions = 10;   // [QUESTION-BANK] người dùng chọn 5..10 câu

        // [#14] Mỗi buổi gọi AI chấm 1 lần -> giới hạn số buổi mới để không bị spam tốn tiền API
        private const int MaxSessionsPerHour = 10;

        // 26/10: câu trả lời không có ý nghĩa được NHẮC 1 lần để trả lời lại; lần sau vẫn không hợp lệ thì
        // vẫn lưu lại nhưng tính là SAI (0 điểm, không gửi AI chấm). Không còn giới hạn số từ tối thiểu.
        private const int MaxReminders = 1;


        private readonly IRepository<InterviewSession> _sessionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IInterviewScoringService _scoring;
        private readonly IAnswerQualityChecker _quality;
        private readonly IEmailService _email;
        private readonly ILogger<InterviewService> _logger;
        private readonly IQuestionBankStore _bank;
        private readonly IQuestionSelector _selector;

        public InterviewService(
            IRepository<InterviewSession> sessionRepository,
            IUserRepository userRepository,
            IInterviewScoringService scoring,
            IAnswerQualityChecker quality,
            IEmailService email,
            ILogger<InterviewService> logger,
            IQuestionBankStore bank,
            IQuestionSelector selector)
        {
            _sessionRepository = sessionRepository;
            _userRepository = userRepository;
            _scoring = scoring;
            _quality = quality;
            _email = email;
            _logger = logger;
            _bank = bank;
            _selector = selector;
        }

        #region Start
        public async Task<int> StartAsync(string email, string positionKey, string interviewerKey, int questionCount, bool emailResult, string? mode = null)
        {
            var user = await GetUserAsync(email);

            if (!QuestionBank.Positions.TryGetValue(positionKey ?? "", out var positionName))
                throw new InterviewException("Vị trí ứng tuyển không hợp lệ.");
            if (!QuestionBank.Interviewers.ContainsKey(interviewerKey ?? ""))
                throw new InterviewException("Người phỏng vấn không hợp lệ.");
            if (questionCount < MinQuestions || questionCount > MaxQuestions)
                throw new InterviewException($"Số câu hỏi phải từ {MinQuestions} đến {MaxQuestions}.");

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
                EmailResult = emailResult,
                Mode = InterviewModes.Normalize(mode)   // [LIVE-MODE] mã lạ -> Luyện tập
            };

            // [QUESTION-BANK] Người phỏng vấn + vị trí đã có trong bộ đề -> dùng bộ đề có đáp án (xếp loại 4 nhãn);
            // chưa có (người phỏng vấn / vị trí chưa có trong bộ đề) -> câu hỏi cũ trong QuestionBank.cs
            var picked = _selector.Select(interviewerKey!, positionKey!, questionCount);
            if (picked.Count > 0)
            {
                session.QuestionBankVersion = _bank.Version;
                session.QuestionCount = picked.Count;
                for (int i = 0; i < picked.Count; i++)
                {
                    session.Answers.Add(new SessionAnswer { OrderNo = i + 1, Question = picked[i].Question, QuestionId = picked[i].Id });
                }
            }
            else
            {
                var questions = QuestionBank.GetQuestions(interviewerKey!, positionName, questionCount);
                for (int i = 0; i < questions.Count; i++)
                {
                    session.Answers.Add(new SessionAnswer { OrderNo = i + 1, Question = questions[i] });
                }
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
                AnsweredCount = CountAnswered(session),
                Mode = InterviewModes.Normalize(session.Mode)
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

                // Câu trả lời không có ý nghĩa: nhắc 1 lần (Attempts = số lần đã nhắc).
                // [LIVE-MODE] Thực chiến: nói ra rồi là xong, không cho trả lời lại -> tính sai luôn.
                bool live = InterviewModes.IsLive(session.Mode);
                string? problem = _quality.FindProblem(answer);
                if (problem != null && !live && current.Attempts < MaxReminders)
                {
                    current.Attempts++;
                    await _sessionRepository.UpdateAsync(session);
                    throw new AnswerRejectedException(
                        $"{problem} Bạn có thể trả lời lại câu này 1 lần. Nếu câu trả lời vẫn chưa có ý nghĩa, câu này sẽ được tính là sai.");
                }

                // Hợp lệ, hoặc đã nhắc mà vẫn không hợp lệ -> lưu lại (để xem lại hội thoại); câu không hợp lệ tính sai khi chấm
                current.Answer = answer;
                current.DurationSeconds = dto.DurationSeconds is > 0 and < 600 ? dto.DurationSeconds : null;
                current.LongestPauseSeconds = current.DurationSeconds == null ? null : LongestPause(dto.PausesSeconds);
                if (problem != null)
                    notice = live
                        ? $"{problem} Chế độ Thực chiến không cho trả lời lại nên câu này được tính là sai. Chuyển sang câu tiếp theo."
                        : "Câu trả lời vẫn chưa có ý nghĩa nên câu này được tính là sai (0 điểm). Chuyển sang câu tiếp theo.";
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

            // Câu không có ý nghĩa (đã được nhắc mà vẫn vậy) tính là SAI: không gửi AI, kéo điểm nội dung xuống theo tỉ lệ
            var invalid = answered.Where(a => _quality.FindProblem(a.Answer!) != null).ToList();
            var valid = answered.Except(invalid).ToList();

            var request = new ScoreRequest
            {
                Position = session.Position,
                QuestionCount = session.QuestionCount,
                SkippedCount = session.Answers.Count(a => a.AnsweredAt != null && a.Skipped),
                // [LIVE-MODE] Thực chiến: câu bỏ qua tính là SAI (Luyện tập: không trừ như trước)
                SkippedAsWrong = InterviewModes.IsLive(session.Mode)
                    ? session.Answers.Where(a => a.AnsweredAt != null && a.Skipped).OrderBy(a => a.OrderNo).Select(a => a.QuestionId).ToList()
                    : new List<string?>(),
                Answers = valid.Select(ToInterviewAnswer).ToList(),
                InvalidAnswers = invalid.Select(ToInterviewAnswer).ToList()
            };

            // AI lỗi (AiScoringException) thì để lỗi nổi lên: buổi vẫn còn InProgress để người dùng bấm "Chấm lại".
            // Fallback = true: người dùng chọn xem kết quả ngay, không chờ AI -> chấm sơ bộ theo bộ đề (câu kiểu cũ: chỉ cách nói).
            var result = dto.Fallback
                ? _scoring.ScoreWithoutAi(request)
                : await _scoring.ScoreAsync(request);

            session.Status = SessionStatus.Completed;
            session.TotalScore = result.Total;
            session.ContentScore = result.ContentAvailable ? result.Content.Total : null;
            session.DeliveryScore = result.Delivery.Total;
            session.ScoringMode = result.ScoringMode;
            session.AnxietyIndicator = result.AnxietyIndicator > 0 ? result.AnxietyIndicator : null;
            session.AiSummary = result.Content.Summary;
            session.Strengths = string.Join("\n", result.Content.Strengths);
            session.Improvements = string.Join("\n", result.Content.Improvements);
            session.DeliveryTips = string.Join("\n", result.Delivery.Tips);

            // [QUESTION-BANK] Lưu xếp loại từng câu (câu thuộc bộ đề; AI chấm hoặc chấm sơ bộ theo bộ đề)
            foreach (var verdict in result.Content.Verdicts)
            {
                var answer = session.Answers.FirstOrDefault(a => a.QuestionId == verdict.QuestionId);
                if (answer == null) continue;
                answer.Category = verdict.Category;
                answer.MatchedPath = verdict.MatchedPath;
                answer.CorePointsMet = string.Join(",", verdict.MetCorePoints);
                answer.CorePointsMissing = string.Join(",", verdict.MissingCorePoints);
                answer.Feedback = verdict.Feedback;
            }

            await _sessionRepository.UpdateAsync(session);

            if (session.EmailResult)
                SendResultEmailInBackground(user, session, result);
        }

        private static InterviewAnswer ToInterviewAnswer(SessionAnswer a) => new()
        {
            QuestionId = a.QuestionId,
            Question = a.Question,
            Answer = a.Answer!,
            DurationSeconds = a.DurationSeconds,
            // Có đo khoảng lặng thì đưa vào chấm điểm; gõ tay thì để trống (không bị trừ điểm)
            PausesSeconds = a.LongestPauseSeconds is double pause ? new List<double> { pause } : new List<double>()
        };

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
                InvalidAnswerCount = session.Answers.Count(a => a.AnsweredAt != null && !a.Skipped
                                                                && !string.IsNullOrEmpty(a.Answer) && _quality.FindProblem(a.Answer!) != null),
                CorrectCount = session.Answers.Count(a => a.Category == AnswerCategory.Correct),
                AlternativePathCount = session.Answers.Count(a => a.Category == AnswerCategory.AlternativePath),
                SpecialCount = session.Answers.Count(a => a.Category == AnswerCategory.Special),
                WrongCount = session.Answers.Count(a => a.Category == AnswerCategory.Wrong),
                IsPreliminaryScore = session.ScoringMode == ScoringModes.BankRules,
                IsLive = InterviewModes.IsLive(session.Mode),
                AnswerCategories = session.Answers.OrderBy(a => a.OrderNo).Select(a => a.Category).ToList(),
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
