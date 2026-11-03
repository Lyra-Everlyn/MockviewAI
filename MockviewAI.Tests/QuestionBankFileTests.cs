// Kiểm tra cấu trúc bộ đề (giống tools/question-bank/validate_bank.py): sửa JSON xong chạy dotnet test là biết có hỏng không.

using MockviewAI.Models.Ai;
using Xunit;

namespace MockviewAI.Tests
{
    public class QuestionBankFileTests
    {
        private readonly Services.InterviewQuestions.Implementations.JsonQuestionBankStore _store = TestData.Store;

        [Theory]
        [InlineData("nam", "developer")]
        [InlineData("nam", "tester")]
        [InlineData("nam", "ba")]
        [InlineData("linh", "developer")]
        [InlineData("linh", "tester")]
        [InlineData("linh", "ba")]
        [InlineData("an", "developer")]
        [InlineData("an", "tester")]
        [InlineData("an", "ba")]
        public void EachInterviewAndPosition_HasIntroAndAtLeastTenQuestions(string interviewer, string position)
        {
            var questions = _store.ForInterview(interviewer, position);
            Assert.True(questions.Count >= 10, $"{interviewer}/{position} chỉ có {questions.Count} câu (buổi tối đa 10 câu)");
            Assert.Single(questions, q => q.IsIntro);
        }

        [Fact]
        public void EveryQuestion_HasKnownInterviewer()
        {
            Assert.All(_store.Questions, q => Assert.Contains(q.Interviewer, new[] { "linh", "nam", "an" }));
        }

        [Fact]
        public void EveryQuestion_HasCorrectPath_AndValidCorePointReferences()
        {
            foreach (var q in _store.Questions)
            {
                var core = q.CorePoints.Select(c => c.Id).ToHashSet();
                Assert.True(core.Count >= q.MinCorePoints, q.Id);
                Assert.Contains(q.Paths, p => p.Category == AnswerCategory.Correct);
                foreach (var p in q.Paths)
                {
                    Assert.Contains(p.Category, new[] { AnswerCategory.Correct, AnswerCategory.AlternativePath });
                    Assert.All(p.CoversCorePoints, id => Assert.Contains(id, core));
                    Assert.True(p.CoversCorePoints.Count >= q.MinCorePoints, $"{q.Id}/{p.Id} bao phủ quá ít ý");
                }
            }
        }

        [Fact]
        public void EveryQuestion_HasTwoSamplesPerCategory_PointingToAValidPath()
        {
            foreach (var q in _store.Questions)
            {
                foreach (var category in AnswerCategory.All)
                    Assert.Equal(2, q.Samples.Count(s => s.Category == category));

                foreach (var s in q.Samples)
                {
                    var path = q.Paths.FirstOrDefault(p => p.Id == s.ExpectedPath);
                    bool ok = s.Category switch
                    {
                        AnswerCategory.Correct => path != null && path.Category == s.Category,
                        // Hơi đặc biệt: đường vòng đã biết, HOẶC đường chuẩn nhưng cách giải thích khác thường
                        AnswerCategory.AlternativePath => path != null,
                        AnswerCategory.Special => s.ExpectedPath == AnswerCategory.OutOfBank,
                        AnswerCategory.Wrong => s.ExpectedPath == AnswerCategory.OutOfBank || q.Mistakes.Any(m => m.Id == s.ExpectedPath),
                        _ => false
                    };
                    Assert.True(ok, $"{q.Id}: câu mẫu '{s.Category}' trỏ sai đường '{s.ExpectedPath}'");
                    Assert.False(string.IsNullOrWhiteSpace(s.Style), $"{q.Id}: câu mẫu '{s.Category}' thiếu phong cách");
                }
            }
        }
    }
}
