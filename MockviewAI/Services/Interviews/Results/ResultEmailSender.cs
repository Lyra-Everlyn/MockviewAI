using MockviewAI.Models.Ai;
using MockviewAI.Models.Entities;
using MockviewAI.Services.Ai;
using MockviewAI.Services.Emails;

namespace MockviewAI.Services.Interviews.Results
{
    /// <summary>
    /// [EMAIL] 
    /// Dựng nội dung email kết quả (ResultEmailBuilder) rồi gửi ở nền qua IEmailService.
    /// </summary>
    public class ResultEmailSender : IResultEmailSender
    {
        #region Fields
        private readonly IEmailService _email;
        private readonly ILogger<ResultEmailSender> _logger;
        #endregion

        #region Constructor
        public ResultEmailSender(IEmailService email, ILogger<ResultEmailSender> logger)
        {
            _email = email;
            _logger = logger;
        }
        #endregion

        #region IResultEmailSender
        public void SendInBackground(User user, InterviewSession session, InterviewScoreResult result)
        {
            // Lấy sẵn mọi giá trị trước khi chạy nền: request kết thúc thì entity không còn được theo dõi
            string toEmail = user.Email;
            int sessionId = session.Id;
            var model = BuildModel(user, session, result);
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
                    _logger.LogWarning(ex, "Không gửi được email kết quả buổi luyện {SessionId}.", sessionId);
                }
            });
        }
        #endregion

        #region Helpers
        private static ResultEmailModel BuildModel(User user, InterviewSession session, InterviewScoreResult result) => new()
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
        #endregion
    }
}
