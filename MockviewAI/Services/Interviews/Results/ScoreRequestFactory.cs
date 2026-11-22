using MockviewAI.Models;
using MockviewAI.Models.Ai;
using MockviewAI.Models.Entities;
using MockviewAI.Services.Ai.Interfaces;

namespace MockviewAI.Services.Interviews.Results
{
    /// <summary>
    /// [AI-SCORING] 
    /// Chia câu trả lời của buổi thành: hợp lệ (gửi AI), không có ý nghĩa (tính sai), bỏ qua, và câu trả lời thêm.
    /// </summary>
    public class ScoreRequestFactory : IScoreRequestFactory
    {
        #region Fields
        private readonly IAnswerQualityChecker _quality;
        #endregion

        #region Constructor
        public ScoreRequestFactory(IAnswerQualityChecker quality)
        {
            _quality = quality;
        }
        #endregion

        #region IScoreRequestFactory
        public ScoreRequest? Build(InterviewSession session)
        {
            // [FOLLOW-UP] Chỉ câu CHÍNH được chấm như một câu; câu trả lời thêm gộp vào câu chính của nó
            var mainAnswers = FollowUps.Main(session.Answers).ToList();
            var answered = mainAnswers
                .Where(IsAnswered)
                .OrderBy(a => a.OrderNo)
                .ToList();
            if (answered.Count == 0) return null;

            var followUpAnswers = session.Answers.Where(a => a.IsFollowUp && IsAnswered(a)).ToList();

            // Câu không có ý nghĩa (đã được nhắc mà vẫn vậy) tính là SAI: không gửi AI, kéo điểm nội dung xuống theo tỉ lệ
            var invalid = answered.Where(a => _quality.FindProblem(a.Answer!) != null).ToList();
            var valid = answered.Except(invalid).ToList();
            var skipped = mainAnswers.Where(a => a.AnsweredAt != null && a.Skipped).OrderBy(a => a.OrderNo).ToList();

            return new ScoreRequest
            {
                Position = session.Position,
                QuestionCount = session.QuestionCount,
                SkippedCount = skipped.Count,
                // [LIVE-MODE] Thực chiến: câu bỏ qua tính là SAI (Luyện tập: không trừ)
                SkippedAsWrong = InterviewModes.IsLive(session.Mode)
                    ? skipped.Select(a => a.QuestionId).ToList()
                    : new List<string?>(),
                Answers = valid.Select(main => WithFollowUp(main, followUpAnswers)).ToList(),
                InvalidAnswers = invalid.Select(ToInterviewAnswer).ToList(),
                FollowUpAnswers = followUpAnswers.Select(ToInterviewAnswer).ToList()   // chỉ để đo cách nói
            };
        }
        #endregion

        #region Helpers
        private static bool IsAnswered(SessionAnswer a) =>
            a.AnsweredAt != null && !a.Skipped && !string.IsNullOrEmpty(a.Answer);

        /// <summary>Câu trả lời thêm có ý nghĩa -> gộp vào câu chính khi chấm nội dung.</summary>
        private InterviewAnswer WithFollowUp(SessionAnswer main, List<SessionAnswer> followUpAnswers)
        {
            var item = ToInterviewAnswer(main);
            var extra = followUpAnswers.FirstOrDefault(f => f.FollowUpOf == main.QuestionId);
            if (extra != null && _quality.FindProblem(extra.Answer!) == null)
            {
                item.FollowUpQuestion = extra.Question;
                item.FollowUpAnswer = extra.Answer;
            }
            return item;
        }

        private static InterviewAnswer ToInterviewAnswer(SessionAnswer a) => new()
        {
            QuestionId = a.QuestionId,
            Question = a.Question,
            Answer = a.Answer!,
            DurationSeconds = a.DurationSeconds,
            // Có đo khoảng lặng thì đưa vào chấm điểm; gõ tay thì để trống (không bị trừ điểm)
            PausesSeconds = a.LongestPauseSeconds is double pause ? new List<double> { pause } : new List<double>(),
            VoiceLevelDb = a.VoiceLevelDb,
            QuietRatio = a.QuietRatio,
            PitchVariation = a.PitchVariation
        };
        #endregion
    }
}
