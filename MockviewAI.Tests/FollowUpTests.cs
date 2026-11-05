// [FOLLOW-UP] Test Đợt 8: câu hỏi nối tiếp soạn sẵn theo ý cốt lõi còn thiếu (không gọi AI).

using Microsoft.Extensions.Logging.Abstractions;
using MockviewAI.Models;
using MockviewAI.Models.Ai;
using MockviewAI.Models.Entities;
using MockviewAI.Services.Ai.Implementations;
using MockviewAI.Services.InterviewQuestions.Implementations;
using Xunit;

namespace MockviewAI.Tests
{
    public class FollowUpTests
    {
        private static readonly BankKeywordScorer Scorer = new();
        private static readonly FollowUpPlanner Planner = new(Scorer);

        // ---------- Bộ đề: mỗi ý cốt lõi có câu hỏi thêm, và câu hỏi thêm không "lộ" đáp án ----------

        [Fact]
        public void EveryCorePoint_HasFollowUpQuestion()
        {
            foreach (var q in TestData.Store.Questions)
                foreach (var c in q.CorePoints)
                    Assert.False(string.IsNullOrWhiteSpace(c.FollowUp), $"{q.Id}/{c.Id} chưa có câu hỏi thêm");
        }

        [Fact]
        public void FollowUp_DoesNotLeakItsOwnKeywords()
        {
            // Nếu câu hỏi thêm chứa từ khoá của chính ý đó, người dùng chỉ cần nhắc lại câu hỏi là được tính đạt ý
            var leaks = TestData.Store.Questions
                .SelectMany(q => q.CorePoints.Select(c => (q, c)))
                .Where(x => Scorer.FindMetCorePoints(x.q, x.c.FollowUp!).Contains(x.c.Id))
                .Select(x => $"{x.q.Id}/{x.c.Id}")
                .ToList();
            Assert.True(leaks.Count == 0, "Câu hỏi thêm lộ từ khoá: " + string.Join(", ", leaks));
        }

        // ---------- Chọn câu hỏi thêm ----------

        [Fact]
        public void CompleteAnswer_NoFollowUp()
        {
            var q = TestData.Store.Find("dev-csharp-01")!;
            Assert.Null(Planner.Plan(q, "Em dùng try catch bắt exception cụ thể, ghi log và giải phóng tài nguyên trong finally."));
        }

        [Fact]
        public void MissingCorePoints_AsksAboutFirstMissingPoint()
        {
            var q = TestData.Store.Find("dev-csharp-01")!;
            // chỉ đạt c1 (bắt lỗi cụ thể) -> chưa tới điểm B (cần 2 ý) -> hỏi thêm về c2 (ghi log / báo lỗi)
            var plan = Planner.Plan(q, "Em bọc đoạn code trong try catch và bắt đúng loại exception cụ thể như FormatException.");

            Assert.NotNull(plan);
            Assert.Equal("c2", plan!.PointId);
            Assert.Equal(q.CorePoints.Single(c => c.Id == "c2").FollowUp, plan.Question);
        }

        [Fact]
        public void EmptyAnswer_NoFollowUp()
        {
            Assert.Null(Planner.Plan(TestData.Store.Find("dev-csharp-01")!, "   "));
        }

        // ---------- Thứ tự hỏi + giới hạn ----------

        [Fact]
        public void Next_ReturnsFollowUpBeforeNextMainQuestion()
        {
            var answers = new List<SessionAnswer>
            {
                new() { Id = 2, OrderNo = 2, QuestionId = "dev-sql-01", Question = "Câu 2" },
                new() { Id = 1, OrderNo = 1, QuestionId = "dev-csharp-01", Question = "Câu 1", AnsweredAt = DateTime.UtcNow },
                new() { Id = 3, OrderNo = 1, FollowUpOf = "dev-csharp-01", FollowUpPointId = "c2", Question = "Hỏi thêm" }
            };

            var next = FollowUps.Next(answers);
            Assert.NotNull(next);
            Assert.True(next!.IsFollowUp);
            Assert.Equal(1, next.OrderNo);
            Assert.Equal(2, FollowUps.Main(answers).Count());
        }

        [Fact]
        public void CanAskAfter_OncePerQuestion_MaxPerSession_NotForOldQuestionsOrFollowUps()
        {
            var main = new SessionAnswer { OrderNo = 1, QuestionId = "dev-csharp-01" };
            Assert.True(FollowUps.CanAskAfter(main, new[] { main }));

            // đã hỏi thêm cho câu này rồi
            var asked = new SessionAnswer { OrderNo = 1, FollowUpOf = "dev-csharp-01" };
            Assert.False(FollowUps.CanAskAfter(main, new[] { main, asked }));

            // câu hỏi thêm không được hỏi thêm tiếp; câu kiểu cũ (không thuộc bộ đề) không hỏi thêm
            Assert.False(FollowUps.CanAskAfter(asked, new[] { main, asked }));
            Assert.False(FollowUps.CanAskAfter(new SessionAnswer { OrderNo = 2 }, new[] { main }));

            // đã đủ số câu hỏi thêm tối đa của buổi
            var full = Enumerable.Range(0, FollowUps.MaxPerSession)
                .Select(i => new SessionAnswer { OrderNo = 10 + i, FollowUpOf = "x" + i })
                .Append(main).ToList();
            Assert.False(FollowUps.CanAskAfter(main, full));
        }

        // ---------- Chấm: câu trả lời thêm được gộp vào câu chính ----------

        [Fact]
        public void ScoreWithoutAi_FollowUpAnswer_CountsTowardMainQuestion()
        {
            var store = TestData.Store;
            var service = new InterviewScoringService(
                new MockAiScoringService(store, Scorer), new DeliveryAnalyzer(), new AnxietyEstimator(),
                new AnswerVerdictValidator(store, NullLogger<AnswerVerdictValidator>.Instance), store, Scorer);

            var main = new InterviewAnswer
            {
                QuestionId = "dev-sql-01", Question = "q",
                Answer = "INNER JOIN chỉ lấy những dòng khớp ở cả hai bảng."
            };
            var mainOnly = service.ScoreWithoutAi(new ScoreRequest { QuestionCount = 1, Answers = { main } });
            Assert.Equal(AnswerCategory.Wrong, mainOnly.Content.Verdicts.Single().Category);

            main.FollowUpQuestion = "Còn kiểu nối thứ hai thì sao?";
            main.FollowUpAnswer = "LEFT JOIN thì lấy tất cả dòng bảng bên trái, dòng không khớp thì cột bên phải là NULL.";
            var withExtra = service.ScoreWithoutAi(new ScoreRequest
            {
                QuestionCount = 1,
                Answers = { main },
                FollowUpAnswers = { new InterviewAnswer { Question = main.FollowUpQuestion, Answer = main.FollowUpAnswer, DurationSeconds = 12 } }
            });

            var verdict = withExtra.Content.Verdicts.Single();
            Assert.Equal(AnswerCategory.Correct, verdict.Category);
            Assert.Contains("c2", verdict.MetCorePoints);
            Assert.Equal(1.0, withExtra.Content.ValidAnswerRatio);   // câu hỏi thêm không làm tăng số câu
        }

        [Fact]
        public void Prompt_IncludesFollowUpQuestionAndAnswer()
        {
            var prompt = new ScoringPromptBuilder(TestData.Store).BuildUserPrompt(new ScoreRequest
            {
                Position = "Lập trình viên",
                Answers =
                {
                    new InterviewAnswer
                    {
                        QuestionId = "dev-sql-01", Question = "INNER JOIN và LEFT JOIN khác nhau thế nào?",
                        Answer = "INNER JOIN lấy dòng khớp.",
                        FollowUpQuestion = "Còn kiểu nối thứ hai thì sao?", FollowUpAnswer = "LEFT JOIN lấy hết bảng trái."
                    }
                }
            });

            Assert.Contains("[Câu 1] Hỏi thêm: Còn kiểu nối thứ hai thì sao?", prompt);
            Assert.Contains("[Câu 1] Trả lời thêm: LEFT JOIN lấy hết bảng trái.", prompt);
        }
    }
}
