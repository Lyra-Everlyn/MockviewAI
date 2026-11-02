// Test "không tin AI": nhãn phải hợp với con đường (chuẩn / đường vòng / lỗi / ngoài bộ đề), bỏ câu lạ, giữ "sai".

using Microsoft.Extensions.Logging.Abstractions;
using MockviewAI.Models.Ai;
using MockviewAI.Services.InterviewQuestions.Implementations;
using Xunit;

namespace MockviewAI.Tests
{
    public class AnswerVerdictValidatorTests
    {
        private const string Q = "dev-csharp-01";   // chuan = dung, vong1 = hoi_dac_biet, loi1/loi2 = lỗi
        private readonly AnswerVerdictValidator _validator = new(TestData.Store, NullLogger<AnswerVerdictValidator>.Instance);

        private AnswerVerdict One(string category, string path, params string[] met) =>
            _validator.Normalize(new[] { new AnswerVerdict { QuestionId = Q, Category = category, MatchedPath = path, MetCorePoints = met.ToList() } },
                                 new[] { Q }).Single();

        [Fact]
        public void StandardPath_KeepsAiChoice_BetweenCorrectAndUnusual()
        {
            Assert.Equal(AnswerCategory.Correct, One(AnswerCategory.Correct, "chuan", "c1", "c2", "c3").Category);
            // Cùng công thức chuẩn nhưng cách giải thích khác thường (vd ví von) -> Hơi đặc biệt
            Assert.Equal(AnswerCategory.AlternativePath, One(AnswerCategory.AlternativePath, "chuan", "c1", "c2").Category);
            // AI nói "đặc biệt" nhưng vẫn khớp công thức chuẩn -> chỉ là Hơi đặc biệt
            Assert.Equal(AnswerCategory.AlternativePath, One(AnswerCategory.Special, "chuan", "c1", "c2").Category);
        }

        [Fact]
        public void KnownAlternativePath_IsAlwaysUnusual()
        {
            Assert.Equal(AnswerCategory.AlternativePath, One(AnswerCategory.Correct, "vong1", "c1", "c2").Category);
        }

        [Fact]
        public void MistakePath_IsWrong()
        {
            Assert.Equal(AnswerCategory.Wrong, One(AnswerCategory.Correct, "loi1").Category);
        }

        [Fact]
        public void AiSaysWrong_StaysWrong_EvenWithCorrectPath()
        {
            Assert.Equal(AnswerCategory.Wrong, One(AnswerCategory.Wrong, "chuan", "c1", "c2").Category);
        }

        [Fact]
        public void UnknownPath_BecomesOutOfBank_Special()
        {
            var v = One(AnswerCategory.Correct, "duong_bia", "c1", "c2");
            Assert.Equal(AnswerCategory.OutOfBank, v.MatchedPath);
            Assert.Equal(AnswerCategory.Special, v.Category);
        }

        [Fact]
        public void TooFewCorePoints_IsWrong()
        {
            Assert.Equal(AnswerCategory.Wrong, One(AnswerCategory.Special, "ngoai_bo", "c1").Category);
        }

        [Fact]
        public void UnknownCorePointIds_AreDropped_AndMissingIsRecomputed()
        {
            var v = One(AnswerCategory.Correct, "chuan", "c1", "c2", "c9");
            Assert.Equal(new[] { "c1", "c2" }, v.MetCorePoints);
            Assert.Equal(new[] { "c3" }, v.MissingCorePoints);
        }

        [Fact]
        public void PathWithoutListedPoints_UsesPathCoverage()
        {
            Assert.Equal(AnswerCategory.Correct, One(AnswerCategory.Correct, "chuan").Category);
        }

        [Fact]
        public void QuestionsNotAsked_AndDuplicates_AreIgnored()
        {
            var raw = new[]
            {
                new AnswerVerdict { QuestionId = Q, Category = AnswerCategory.Correct, MatchedPath = "chuan" },
                new AnswerVerdict { QuestionId = Q, Category = AnswerCategory.Wrong, MatchedPath = "loi1" },
                new AnswerVerdict { QuestionId = "dev-sql-01", Category = AnswerCategory.Correct, MatchedPath = "chuan" },
                new AnswerVerdict { QuestionId = "khong-co", Category = AnswerCategory.Correct, MatchedPath = "chuan" }
            };
            var result = _validator.Normalize(raw, new[] { Q });
            Assert.Single(result);
            Assert.Equal(AnswerCategory.Correct, result[0].Category);
        }
    }
}
