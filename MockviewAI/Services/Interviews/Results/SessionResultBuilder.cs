using MockviewAI.Models;
using MockviewAI.Models.Ai;
using MockviewAI.Models.Entities;
using MockviewAI.Models.ViewModels;
using MockviewAI.Repositories.Interfaces;
using MockviewAI.Services.Ai;
using MockviewAI.Services.Ai.Interfaces;

namespace MockviewAI.Services.Interviews.Results
{
    /// <summary>
    /// [HISTORY] 
    /// Điểm, xếp loại từng câu, giọng nói cả buổi và xu hướng lo lắng so với buổi trước.
    /// </summary>
    public class SessionResultBuilder : ISessionResultBuilder
    {
        #region Fields
        private readonly IRepository<InterviewSession> _sessionRepository;
        private readonly IAnswerQualityChecker _quality;
        #endregion

        #region Constructor
        public SessionResultBuilder(IRepository<InterviewSession> sessionRepository, IAnswerQualityChecker quality)
        {
            _sessionRepository = sessionRepository;
            _quality = quality;
        }
        #endregion

        #region ISessionResultBuilder
        public async Task<SessionResultViewModel> BuildAsync(InterviewSession session)
        {
            var mainAnswers = FollowUps.Main(session.Answers).ToList();

            // [VOICE] Giọng nói cả buổi: trung bình các câu có số đo (cùng cách tính với DeliveryAnalyzer)
            var voiced = session.Answers.Where(a => a.QuietRatio != null).ToList();
            var levels = voiced.Where(a => a.VoiceLevelDb != null).Select(a => a.VoiceLevelDb!.Value).ToList();
            var tones = session.Answers.Where(a => a.PitchVariation != null).Select(a => a.PitchVariation!.Value).ToList();

            return new SessionResultViewModel
            {
                SessionId = session.Id,
                HasVoice = voiced.Count > 0,
                VoiceLevelLabel = VoiceMetrics.LevelLabel(levels.Count > 0 ? levels.Average() : null),
                QuietPercent = voiced.Count > 0 ? (int)Math.Round(voiced.Average(a => a.QuietRatio!.Value) * 100) : null,
                ToneLabel = VoiceMetrics.ToneLabel(tones.Count > 0 ? tones.Average() : null),
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
                PreviousAnxietyIndicator = await PreviousAnxietyAsync(session),
                EmailRequested = session.EmailResult && session.TotalScore != null,
                InvalidAnswerCount = mainAnswers.Count(a => a.AnsweredAt != null && !a.Skipped
                                                            && !string.IsNullOrEmpty(a.Answer) && _quality.FindProblem(a.Answer!) != null),
                CorrectCount = session.Answers.Count(a => a.Category == AnswerCategory.Correct),
                AlternativePathCount = session.Answers.Count(a => a.Category == AnswerCategory.AlternativePath),
                SpecialCount = session.Answers.Count(a => a.Category == AnswerCategory.Special),
                WrongCount = session.Answers.Count(a => a.Category == AnswerCategory.Wrong),
                IsPreliminaryScore = session.ScoringMode == ScoringModes.BankRules,
                IsLive = InterviewModes.IsLive(session.Mode),
                AnswerCategories = mainAnswers.OrderBy(a => a.OrderNo).Select(a => a.Category).ToList(),
                FollowUpCount = session.Answers.Count(a => a.IsFollowUp && a.AnsweredAt != null && !a.Skipped),
                AiSummary = session.AiSummary ?? "",
                Strengths = SplitLines(session.Strengths),
                Improvements = SplitLines(session.Improvements),
                DeliveryTips = SplitLines(session.DeliveryTips)
            };
        }
        #endregion

        #region Helpers
        /// <summary>Dấu hiệu lo lắng của buổi có chấm trước đó (0 nếu chưa có) để hiện xu hướng.</summary>
        private async Task<int> PreviousAnxietyAsync(InterviewSession session)
        {
            var userId = session.UserId;
            var startedAt = session.StartedAt;
            var previous = await _sessionRepository.GetListAsync(
                predicate: s => s.UserId == userId && s.StartedAt < startedAt && s.AnxietyIndicator != null,
                orderBy: q => q.OrderByDescending(s => s.StartedAt),
                take: 1);
            return previous.FirstOrDefault()?.AnxietyIndicator ?? 0;
        }

        private static List<string> SplitLines(string? text) =>
            string.IsNullOrWhiteSpace(text)
                ? new List<string>()
                : text.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        #endregion
    }
}
