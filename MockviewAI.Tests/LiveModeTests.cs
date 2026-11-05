// Test Đợt 6 — chế độ Thực chiến: câu bỏ qua tính là SAI (nhãn sai + kéo điểm nội dung), mã chế độ hợp lệ.

using Microsoft.Extensions.Logging.Abstractions;
using MockviewAI.Models;
using MockviewAI.Models.Ai;
using MockviewAI.Services.Ai.Implementations;
using MockviewAI.Services.InterviewQuestions.Implementations;
using Xunit;

namespace MockviewAI.Tests
{
    public class LiveModeTests
    {
        private static InterviewScoringService NewService()
        {
            var store = TestData.Store;
            var scorer = new BankKeywordScorer();
            return new InterviewScoringService(
                new MockAiScoringService(store, scorer), new DeliveryAnalyzer(), new AnxietyEstimator(),
                new AnswerVerdictValidator(store, NullLogger<AnswerVerdictValidator>.Instance), store, scorer);
        }

        private const string GoodAnswer = "Em dùng try catch bắt exception cụ thể, ghi log và giải phóng tài nguyên trong finally.";

        private static ScoreRequest Request(bool skippedCountsAsWrong) => new()
        {
            Position = "Lập trình viên",
            QuestionCount = 2,
            SkippedCount = 1,
            Answers = { new InterviewAnswer { QuestionId = "dev-csharp-01", Question = "q", Answer = GoodAnswer } },
            SkippedAsWrong = skippedCountsAsWrong ? new List<string?> { "dev-sql-01" } : new List<string?>()
        };

        [Fact]
        public void Offline_LiveSkip_IsWrongVerdict_AndHalvesContent()
        {
            var practice = NewService().ScoreWithoutAi(Request(false));
            var live = NewService().ScoreWithoutAi(Request(true));

            Assert.Contains(live.Content.Verdicts, v => v.QuestionId == "dev-sql-01" && v.Category == AnswerCategory.Wrong);
            Assert.DoesNotContain(practice.Content.Verdicts, v => v.QuestionId == "dev-sql-01");
            Assert.Equal(0.5, live.Content.ValidAnswerRatio, 3);       // 1 câu trả lời / (1 + 1 câu bỏ qua)
            Assert.Equal(1.0, practice.Content.ValidAnswerRatio, 3);   // Luyện tập: bỏ qua không kéo điểm nội dung
            Assert.True(live.Content.Total < practice.Content.Total);
        }

        [Fact]
        public async Task Ai_LiveSkip_IsWrongVerdict_AndLowersRatio()
        {
            var live = await NewService().ScoreAsync(Request(true));

            Assert.Contains(live.Content.Verdicts, v => v.QuestionId == "dev-sql-01" && v.Category == AnswerCategory.Wrong);
            Assert.Equal(0.5, live.Content.ValidAnswerRatio, 3);
        }

        [Fact]
        public void LiveSkip_OfOldQuestion_StillCountsInRatio_WithoutVerdict()
        {
            var request = Request(false);
            request.SkippedAsWrong = new List<string?> { null };   // câu kiểu cũ (không có mã bộ đề)
            var live = NewService().ScoreWithoutAi(request);

            Assert.Equal(0.5, live.Content.ValidAnswerRatio, 3);
            Assert.Single(live.Content.Verdicts);
        }

        [Theory]
        [InlineData("live", "live")]
        [InlineData("practice", "practice")]
        [InlineData(null, "practice")]
        [InlineData("", "practice")]
        [InlineData("LIVE", "practice")]   // chỉ nhận đúng mã viết thường
        [InlineData("hack", "practice")]
        public void Mode_IsNormalized(string? input, string expected)
        {
            Assert.Equal(expected, InterviewModes.Normalize(input));
        }

        [Fact]
        public void Mode_DisplayNames()
        {
            Assert.Equal("Thực chiến", InterviewModes.DisplayName(InterviewModes.Live));
            Assert.Equal("Luyện tập", InterviewModes.DisplayName(null));
        }
    }
}
