// Test chấm khi AI lỗi: câu thuộc bộ đề -> chấm sơ bộ theo bộ đề (có điểm nội dung + nhãn); câu kiểu cũ -> chỉ cách nói.

using Microsoft.Extensions.Logging.Abstractions;
using MockviewAI.Models.Ai;
using MockviewAI.Services.Ai.Implementations;
using MockviewAI.Services.InterviewQuestions.Implementations;
using Xunit;

namespace MockviewAI.Tests
{
    public class ScoreWithoutAiTests
    {
        private static InterviewScoringService NewService()
        {
            var store = TestData.Store;
            var scorer = new BankKeywordScorer();
            return new InterviewScoringService(
                new MockAiScoringService(store, scorer), new DeliveryAnalyzer(), new AnxietyEstimator(),
                new AnswerVerdictValidator(store, NullLogger<AnswerVerdictValidator>.Instance), store, scorer);
        }

        [Fact]
        public void BankQuestions_GetPreliminaryContentScore_AndVerdicts()
        {
            var request = new ScoreRequest
            {
                Position = "Lập trình viên",
                QuestionCount = 2,
                Answers =
                {
                    new InterviewAnswer { QuestionId = "dev-csharp-01", Question = "q", Answer = "Em dùng try catch bắt exception cụ thể, ghi log và giải phóng tài nguyên trong finally." },
                    new InterviewAnswer { QuestionId = "dev-sql-01", Question = "q", Answer = "Hai cái này giống nhau thôi ạ." }
                }
            };
            var result = NewService().ScoreWithoutAi(request);

            Assert.Equal(ScoringModes.BankRules, result.ScoringMode);
            Assert.True(result.ContentAvailable);
            Assert.Equal(2, result.Content.Verdicts.Count);
            Assert.Contains(result.Content.Verdicts, v => v.QuestionId == "dev-csharp-01" && v.Category == AnswerCategory.Correct);
            Assert.Contains(result.Content.Verdicts, v => v.QuestionId == "dev-sql-01" && v.Category == AnswerCategory.Wrong);
            Assert.InRange(result.Content.Total, 1, 99);   // phủ 3/3 ý ở câu 1, 0/3 ở câu 2 -> khoảng 50
        }

        [Fact]
        public void OldQuestions_AreDeliveryOnly()
        {
            var request = new ScoreRequest
            {
                Position = "Lập trình viên",
                QuestionCount = 1,
                Answers = { new InterviewAnswer { Question = "Giới thiệu bản thân", Answer = "Em là sinh viên năm cuối ngành Computing." } }
            };
            var result = NewService().ScoreWithoutAi(request);

            Assert.Equal(ScoringModes.DeliveryOnly, result.ScoringMode);
            Assert.False(result.ContentAvailable);
            Assert.Empty(result.Content.Verdicts);
        }
    }
}
