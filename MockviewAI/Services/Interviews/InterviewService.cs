using MockviewAI.Models;
using MockviewAI.Models.Ai;
using MockviewAI.Models.DTOs;
using MockviewAI.Models.Entities;
using MockviewAI.Models.ViewModels;
using MockviewAI.Repositories.Interfaces;
using MockviewAI.Services.Ai;
using MockviewAI.Services.Ai.Interfaces;
using MockviewAI.Models.CvQuestions;
using MockviewAI.Models.InterviewQuestions;
using MockviewAI.Services.CvQuestions.Interfaces;
using MockviewAI.Services.Exceptions;
using MockviewAI.Services.InterviewQuestions;
using MockviewAI.Services.InterviewQuestions.Interfaces;
using MockviewAI.Services.Interviews.Conversation;
using MockviewAI.Services.Interviews.Results;

namespace MockviewAI.Services.Interviews
{
    /// <summary>
    /// [AI-INTERVIEW] 
    /// Vòng đời một buổi luyện: tạo buổi -> phòng phỏng vấn -> nhận câu trả lời (+ nhà tuyển dụng phản hồi,
    /// câu hỏi thêm) -> kết thúc và chấm điểm -> trang Kết quả.
    /// Dựng yêu cầu chấm (ScoreRequestFactory), dữ liệu trang Kết quả (SessionResultBuilder)
    /// và gửi email (ResultEmailSender) nằm ở Services/Interviews/Results.
    /// </summary>
    public class InterviewService : IInterviewService
    {
        #region Fields
        private const int MaxAnswerLength = 2000;

        /// <summary>[QUESTION-BANK] Người dùng chọn 5..10 câu.</summary>
        private const int MinQuestions = 5, MaxQuestions = 10;

        /// <summary>Mỗi buổi gọi AI chấm 1 lần -> giới hạn số buổi mới để không bị spam tốn tiền API.</summary>
        private const int MaxSessionsPerHour = 10;

        /// <summary>
        /// Câu trả lời vô nghĩa được nhắc 1 lần để trả lời lại; lần sau vẫn vậy thì vẫn lưu nhưng tính SAI (0 điểm, không gửi AI).
        /// </summary>
        private const int MaxReminders = 1;

        /// <summary>[INTERVIEWER] Mã ý cốt lõi cho câu hỏi đào sâu theo CV (không thuộc ý nào của bộ đề).</summary>
        private const string CvFollowUpPointId = "cv";

        private const int MaxQuestionLength = 500;

        private readonly IRepository<InterviewSession> _sessionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IInterviewScoringService _scoring;
        private readonly IAnswerQualityChecker _quality;
        private readonly IQuestionBankStore _bank;
        private readonly IQuestionSelector _selector;
        private readonly IFollowUpPlanner _followUps;
        private readonly IInterviewerTurnService _interviewer;
        private readonly ICandidateCvStore _cvStore;
        private readonly IScoreRequestFactory _scoreRequests;
        private readonly ISessionResultBuilder _results;
        private readonly IResultEmailSender _resultEmail;
        private readonly ICvQuestionPipeline _cvPipeline;
        private readonly ICvQuestionStore _cvQuestions;
        #endregion

        #region Constructor
        public InterviewService(
            IRepository<InterviewSession> sessionRepository,
            IUserRepository userRepository,
            IInterviewScoringService scoring,
            IAnswerQualityChecker quality,
            IQuestionBankStore bank,
            IQuestionSelector selector,
            IFollowUpPlanner followUps,
            IInterviewerTurnService interviewer,
            ICandidateCvStore cvStore,
            IScoreRequestFactory scoreRequests,
            ISessionResultBuilder results,
            IResultEmailSender resultEmail,
            ICvQuestionPipeline cvPipeline,
            ICvQuestionStore cvQuestions)
        {
            _sessionRepository = sessionRepository;
            _userRepository = userRepository;
            _scoring = scoring;
            _quality = quality;
            _bank = bank;
            _selector = selector;
            _followUps = followUps;
            _interviewer = interviewer;
            _cvStore = cvStore;
            _scoreRequests = scoreRequests;
            _results = results;
            _resultEmail = resultEmail;
            _cvPipeline = cvPipeline;
            _cvQuestions = cvQuestions;
        }
        #endregion

        #region Tạo buổi
        public async Task<int> StartAsync(string email, string positionKey, string interviewerKey, int questionCount, bool emailResult, string? mode = null, string? cvJson = null)
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

            // [QUESTION-BANK] Có câu trong bộ đề -> dùng bộ đề có đáp án (xếp loại 4 nhãn);
            // không có -> câu hỏi kiểu cũ trong QuestionBank.cs (hiện bộ đề đã phủ đủ 9 cặp nên nhánh này không chạy)
            var picked = _selector.Select(interviewerKey!, positionKey!, questionCount);

            // [CV-QUESTIONS] Gemini soạn + Groq duyệt câu theo CV; lỗi / hết giờ / không đạt -> rỗng, giữ 100% bộ đề
            string cvSummary = CvContextBuilder.FromBrowserJson(cvJson);
            var cvResult = picked.Count > 0 && cvSummary.Length > 0
                ? await _cvPipeline.RunAsync(new CvQuestionContext
                {
                    PositionKey = positionKey!,
                    PositionName = positionName,
                    InterviewerKey = interviewerKey!,
                    CvSummary = cvSummary,
                    ExistingQuestions = picked.Select(q => q.Question).ToList()
                })
                : CvQuestionPipelineResult.Empty;

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

            // [CV-QUESTIONS] Đã có session.Id -> thay câu cuối bằng câu CV (giữ câu 1), mã cv-{id}-{n}, lưu đáp án vào DB
            if (cvResult.Accepted.Count > 0)
                await _cvQuestions.AttachToSessionAsync(session, cvResult);

            // [INTERVIEWER] CV mặc định trên trình duyệt (nếu người dùng chọn dùng) -> tóm tắt, giữ trong Redis theo buổi
            await _cvStore.SaveAsync(session.Id, cvSummary);
            return session.Id;
        }
        #endregion

        #region Phòng phỏng vấn
        public async Task<InterviewRoomViewModel?> GetRoomAsync(string email, int sessionId)
        {
            var user = await GetUserAsync(email);
            var session = await FindSessionAsync(user, sessionId);
            if (session == null || session.Status != SessionStatus.InProgress) return null;

            var current = FollowUps.Next(session.Answers);   // [FOLLOW-UP] câu hỏi thêm đứng ngay sau câu chính
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
                IsFollowUp = current.IsFollowUp,
                Mode = InterviewModes.Normalize(session.Mode)
            };
        }

        public async Task<string> GetCurrentQuestionAsync(string email, int sessionId)
        {
            var user = await GetUserAsync(email);
            var session = await FindSessionAsync(user, sessionId);
            if (session == null || session.Status != SessionStatus.InProgress)
                throw new InterviewException("Không tìm thấy buổi luyện đang diễn ra.");

            var current = FollowUps.Next(session.Answers)
                ?? throw new InterviewException("Bạn đã trả lời hết các câu hỏi.");
            return current.Question;
        }
        #endregion

        #region Nhận câu trả lời
        public async Task<SubmitAnswerResultDto> SubmitAnswerAsync(string email, SubmitAnswerDto dto)
        {
            var user = await GetUserAsync(email);
            var session = await FindSessionAsync(user, dto.SessionId);
            if (session == null || session.Status != SessionStatus.InProgress)
                throw new InterviewException("Không tìm thấy buổi luyện đang diễn ra.");

            var current = FollowUps.Next(session.Answers);
            if (current == null)
                throw new InterviewException("Bạn đã trả lời hết các câu hỏi.");

            string? notice = null;
            bool meaningful = false;   // [FOLLOW-UP] chỉ câu trả lời có ý nghĩa mới được xét hỏi thêm

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

                // [VOICE] Chỉ lưu số đo giọng khi trả lời bằng giọng nói; giá trị lạ thì bỏ
                bool spoken = current.DurationSeconds != null;
                current.VoiceLevelDb = spoken ? VoiceMetrics.CleanLevel(dto.VoiceLevelDb) : null;
                current.QuietRatio = spoken ? VoiceMetrics.CleanRatio(dto.QuietRatio) : null;
                current.PitchVariation = spoken ? VoiceMetrics.CleanPitch(dto.PitchVariation) : null;

                meaningful = problem == null;
                if (problem != null)
                    notice = live
                        ? $"{problem} Chế độ Thực chiến không cho trả lời lại nên câu này được tính là sai. Chuyển sang câu tiếp theo."
                        : "Câu trả lời vẫn chưa có ý nghĩa nên câu này được tính là sai (0 điểm). Chuyển sang câu tiếp theo.";
            }
            current.AnsweredAt = DateTime.UtcNow;

            // [INTERVIEWER] Nhà tuyển dụng phản hồi; câu hỏi thêm theo ý còn thiếu (bộ đề) hoặc đào sâu theo CV
            string? reaction = dto.Skipped ? InterviewerFallback.Skipped : null;
            if (meaningful)
                reaction = await ReplyAndPlanFollowUpAsync(session, current);

            await _sessionRepository.UpdateAsync(session);

            var next = FollowUps.Next(session.Answers);
            return new SubmitAnswerResultDto
            {
                Done = next == null,
                AnsweredCount = CountAnswered(session),
                NextNumber = next?.OrderNo,
                NextQuestion = next?.Question,
                NextIsFollowUp = next?.IsFollowUp ?? false,
                Notice = notice,
                Reaction = reaction
            };
        }

        /// <summary>
        /// [INTERVIEWER] Một lần gọi AI: lời phản hồi + câu hỏi thêm (nếu còn lượt). Bộ đề vẫn quyết định ý nào còn thiếu;
        /// AI chỉ diễn đạt lại cho tự nhiên, hoặc hỏi đào sâu theo CV khi câu trả lời đã đủ ý.
        /// </summary>
        private async Task<string> ReplyAndPlanFollowUpAsync(InterviewSession session, SessionAnswer current)
        {
            bool canAsk = FollowUps.CanAskAfter(current, session.Answers);
            var plan = canAsk && await FindQuestionAsync(session, current.QuestionId) is { } bankQuestion
                ? _followUps.Plan(bankQuestion, current.Answer!)
                : null;

            // [CV-QUESTIONS] Buổi đã có câu chính soạn theo CV: câu bộ đề CHỈ hỏi thêm khi thiếu ý cốt lõi (không tự đào sâu),
            // để không hỏi trùng dự án với câu CV và để dành lượt hỏi thêm (tối đa FollowUps.MaxPerSession) cho câu CV,
            // nơi người phỏng vấn được hỏi "vì sao / nếu thay đổi thì sao" để kiểm tra ứng viên hiểu việc mình làm.
            bool sessionHasCvQuestions = session.Answers.Any(a => QuestionBankStoreExtensions.IsCvQuestionId(a.QuestionId));
            bool useCvContext = !sessionHasCvQuestions || QuestionBankStoreExtensions.IsCvQuestionId(current.QuestionId);
            if (!useCvContext && plan == null) canAsk = false;

            var interviewer = QuestionBank.Interviewers[session.Interviewer];
            var turn = await _interviewer.NextTurnAsync(new InterviewerTurnRequest
            {
                InterviewerName = interviewer.Name,
                InterviewerRole = interviewer.Role,
                Position = session.Position,
                Question = current.Question,
                Answer = current.Answer!,
                AllowFollowUp = canAsk,
                MissingPointQuestion = plan?.Question,
                CvContext = useCvContext ? await _cvStore.GetAsync(session.Id) : string.Empty
            });

            if (canAsk && !string.IsNullOrWhiteSpace(turn.FollowUp))
            {
                var question = turn.FollowUp.Trim();
                session.Answers.Add(new SessionAnswer
                {
                    OrderNo = current.OrderNo,
                    Question = question.Length > MaxQuestionLength ? question[..MaxQuestionLength] : question,
                    FollowUpOf = current.QuestionId,
                    FollowUpPointId = plan?.PointId ?? CvFollowUpPointId
                });
            }

            return turn.Reaction;
        }
        #endregion

        #region Kết thúc và chấm điểm
        public async Task FinishAsync(string email, FinishSessionDto dto)
        {
            var user = await GetUserAsync(email);
            var session = await FindSessionAsync(user, dto.SessionId);
            if (session == null) throw new InterviewException("Không tìm thấy buổi luyện.");
            if (session.Status != SessionStatus.InProgress) return;

            session.FinishedAt = DateTime.UtcNow;

            var request = _scoreRequests.Build(session);
            if (request == null)
            {
                session.Status = SessionStatus.Stopped;
                await _sessionRepository.UpdateAsync(session);
                return;
            }

            // [CV-QUESTIONS] Đáp án câu theo CV nằm trong DB -> đưa vào yêu cầu chấm (buổi không có câu CV: không truy vấn thêm)
            if (session.Answers.Any(a => QuestionBankStoreExtensions.IsCvQuestionId(a.QuestionId)))
                request.CvQuestions = await _cvQuestions.GetForSessionAsync(session.Id, session.UserId);

            // AI lỗi (AiScoringException) thì để lỗi nổi lên: buổi vẫn còn InProgress để người dùng bấm "Chấm lại".
            // Fallback = true: người dùng chọn xem kết quả ngay -> chấm sơ bộ theo bộ đề (câu kiểu cũ: chỉ cách nói).
            var result = dto.Fallback
                ? _scoring.ScoreWithoutAi(request)
                : await _scoring.ScoreAsync(request);

            ApplyScore(session, result);
            await _sessionRepository.UpdateAsync(session);

            if (session.EmailResult)
                _resultEmail.SendInBackground(user, session, result);
        }

        /// <summary>Ghi điểm, nhận xét và xếp loại từng câu chính (câu thuộc bộ đề) vào buổi.</summary>
        private static void ApplyScore(InterviewSession session, InterviewScoreResult result)
        {
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

            // [QUESTION-BANK] Lưu xếp loại từng câu (AI chấm hoặc chấm sơ bộ theo bộ đề)
            var mainAnswers = FollowUps.Main(session.Answers).ToList();
            foreach (var verdict in result.Content.Verdicts)
            {
                var answer = mainAnswers.FirstOrDefault(a => a.QuestionId == verdict.QuestionId);
                if (answer == null) continue;
                answer.Category = verdict.Category;
                answer.MatchedPath = verdict.MatchedPath;
                answer.CorePointsMet = string.Join(",", verdict.MetCorePoints);
                answer.CorePointsMissing = string.Join(",", verdict.MissingCorePoints);
                answer.Feedback = verdict.Feedback;
            }
        }
        #endregion

        #region Trang kết quả
        public async Task<SessionResultViewModel?> GetResultAsync(string email, int sessionId)
        {
            var user = await GetUserAsync(email);
            var session = await FindSessionAsync(user, sessionId);
            if (session == null || session.Status == SessionStatus.InProgress) return null;

            return await _results.BuildAsync(session);
        }
        #endregion

        #region Helpers
        private async Task<User> GetUserAsync(string email)
        {
            return await _userRepository.GetByEmailAsync(email.Trim().ToLowerInvariant())
                   ?? throw new InterviewException("Không tìm thấy tài khoản. Vui lòng đăng nhập lại.");
        }

        /// <summary>Chỉ trả buổi thuộc tài khoản này (kèm danh sách câu hỏi, có theo dõi thay đổi để lưu).</summary>
        private async Task<InterviewSession?> FindSessionAsync(User user, int sessionId)
        {
            return await _sessionRepository.GetSingleAsync(
                predicate: s => s.Id == sessionId && s.UserId == user.Id,
                includes: s => s.Answers
            );
        }

        /// <summary>[CV-QUESTIONS] Câu bộ đề hoặc câu theo CV (mã cv-..., đọc từ DB của đúng buổi / người dùng).</summary>
        private async Task<BankQuestion?> FindQuestionAsync(InterviewSession session, string? questionId)
        {
            if (!QuestionBankStoreExtensions.IsCvQuestionId(questionId)) return _bank.Find(questionId);
            var cvQuestions = await _cvQuestions.GetForSessionAsync(session.Id, session.UserId);
            return cvQuestions.FirstOrDefault(q => q.Id == questionId);
        }

        /// <summary>Số câu CHÍNH đã trả lời (câu hỏi thêm không tính vào "x / tổng số câu").</summary>
        private static int CountAnswered(InterviewSession session) =>
            FollowUps.Main(session.Answers).Count(a => a.AnsweredAt != null && !a.Skipped);

        /// <summary>Khoảng lặng dài nhất; bỏ số vô lý (&lt;= 0 hoặc &gt;= 120 giây) vì dữ liệu đến từ trình duyệt.</summary>
        private static double LongestPause(List<double>? pauses)
        {
            var valid = (pauses ?? new List<double>()).Where(p => p > 0 && p < 120).ToList();
            return valid.Count == 0 ? 0 : Math.Round(valid.Max(), 1);
        }
        #endregion
    }
}
